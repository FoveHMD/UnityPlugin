using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

using System.Linq;

namespace Fove.Unity
{
    // FoveManager needs to reference a gameObject in order to create coroutines.
    public partial class FoveManager
    {
        // Static members
        private static FoveManager sInstance;

        // static awaiters
        private static WaitUntil m_sWaitForHardwareConnected = new WaitUntil(() => IsHardwareConnected());
        private static WaitUntil m_sWaitForHardwareDisconnected = new WaitUntil(() => !IsHardwareConnected());
        private static WaitUntil m_sWaitForHardwareReady = new WaitUntil(() => IsHardwareReady());
        private static WaitUntil m_sWaitForCalibrationStart = new WaitUntil(() => IsEyeTrackingCalibrating());
        private static WaitUntil m_sWaitForCalibrationEnd = new WaitUntil(() => !IsEyeTrackingCalibrating());
        private static WaitUntil m_sWaitForCalibrationCalibrated = new WaitUntil(() => IsEyeTrackingCalibrated());
        private static WaitUntil m_sWaitForUser = new WaitUntil(() => IsUserPresent());

        // The headset instance
        private Headset headset;
    
        // Caches
        private Pose hmdPose = Pose.Null;
        private Quaternion headRotation = Quaternion.identity;
        private Vector3 headPosition;
        private Vector3 standingPosition;

        // Previous frame values cached to identify state changes
        private Stereo<EyeState> previousEyeStates = new Stereo<EyeState>(EyeState.NotDetected);
        private bool wasHardwareConnected = false;
        private bool wasHardwareReady = false;
        private bool wasCalibrating = false;
        private bool wasShiftingAttention = false;
        private bool wasUserPresent = false;
        private bool wasHmdAdjustmentVisible = false;

        // Settings cache for runtime
        private float worldScale = 1.0f;
        private float renderScale = 1.0f;
    
        // Rendering/submission native pointers
        private IntPtr submitNativeFunc;
        private IntPtr wfrpNativeFunc;

        // Capabilities
        private ClientCapabilities currentCapabilities = ClientCapabilities.None;
        private ClientCapabilities enforcedCapabilities = ClientCapabilities.None;

        // Camera Images & Textures
        private Bitmap eyesImage;
        private Bitmap positionImage;
        private Result<Texture2D> eyesTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);
        private Result<Texture2D> positionTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);
        private Result<Texture2D> mirrorTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);

        private class EyeTextures
        {
            public RenderTexture left;
            public RenderTexture right;
            public bool areNew;
        }
        private Dictionary<int, EyeTextures> eyeTextures = new Dictionary<int, EyeTextures>();

        private Material screenBlitMaterial;

        /// <summary>
        /// The fove manager instance that is communicating and managing the HMD.
        /// </summary>
        internal static FoveManager Instance
        {
            get 
            {
                if (sInstance == null)
                {
                    sInstance = FindObjectOfType<FoveManager>();
                    if (sInstance == null)
                    {
                        sInstance = new GameObject("~FOVE Manager").AddComponent<FoveManager>();
                        DontDestroyOnLoad(Instance);
                    }
                }

                return sInstance;
            }
        }

        private enum LogLevel
        {
            Error,   // Lowest level, this should contain only errors
            Warning, // Warnings should be logged here               
            Debug,   // Generic debugging info can go here          
        };

        private static void LogSinkCallback(IntPtr logLevel, string str)
        {
            str = "[FOVE] " + str;

            switch ((LogLevel)logLevel)
            {
                case LogLevel.Error:
                    Debug.LogError(str);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(str);
                    break;
                default:
                    break;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LogSinkDelegate(IntPtr logLevel, string str);

        private LogSinkDelegate logSinkDelegate = new LogSinkDelegate(LogSinkCallback);

        private EyeTextures MakeNewEyeTextures(int layerId, Vec2i dims)
        {
            EyeTextures result;
#if UNITY_2017_1_OR_NEWER
            RenderTextureDescriptor desc = new RenderTextureDescriptor (
                dims.x, dims.y, RenderTextureFormat.Default, 32 );

            result = new EyeTextures
            {
                left = new RenderTexture(desc),
                right = new RenderTexture(desc),
                areNew = true
            };
#else
            result = new EyeTextures
            {
                left = new RenderTexture(dims.x, dims.y, 32, RenderTextureFormat.Default),
                right = new RenderTexture(dims.x, dims.y, 32, RenderTextureFormat.Default),
                areNew = true
            };
#endif

            eyeTextures[layerId] = result;

            return result;
        }

        private EyeTextures GetEyeTextures(int layerId)
        {
            EyeTextures result;
        
            Vec2i dims = new Vec2i(1, 1);
            UnityFuncs.GetIdealLayerDimensions(layerId, ref dims);

            dims.x = (int)(dims.x * renderScale);
            dims.y = (int)(dims.y * renderScale);

            if (eyeTextures.ContainsKey(layerId))
            {
                result = eyeTextures[layerId];

                if (dims.x != result.left.width || dims.y != result.left.height)
                    result = MakeNewEyeTextures(layerId, dims);
            }
            else {
                result = MakeNewEyeTextures(layerId, dims);
            }

            return result;
        }

        private class InterfaceInfo
        {
            public CompositorLayerCreateInfo info;
            public FoveInterface xface;
        }

        // Manage static FoveInterfaces list for initialization and rendering; adding and removing
        private static Dictionary<int, List<InterfaceInfo>> m_sInterfaceStacks = new Dictionary<int, List<InterfaceInfo>>();

        private static List<InterfaceInfo> m_sUnregisteredInterfaces = new List<InterfaceInfo>();
        
        /*******************************************************************************\
         * MonoBehaviour / instance methods                                            *
        \*******************************************************************************/
        private FoveManager()
        {
            if (sInstance != null)
                Debug.LogError("Found an existing instance");

            var logSinkDelegatePtr = Marshal.GetFunctionPointerForDelegate(logSinkDelegate);
            UnityFuncs.SetLogSinkFunction(logSinkDelegatePtr);

            headset = new Headset(currentCapabilities);

            worldScale = FoveSettings.WorldScale;
            renderScale = FoveSettings.RenderScale;

            submitNativeFunc = UnityFuncs.GetSubmitFunctionPtr();
            wfrpNativeFunc = UnityFuncs.GetWfrpFunctionPtr();
        }

        void Awake()
        {
            headset.GazeCastPolicy = FoveSettings.GazeCastPolicy;

            UnityFuncs.ResetNativeState();
            screenBlitMaterial = new Material(Shader.Find("Fove/EyeShader"));

            if (FoveSettings.AutomaticObjectRegistration)
                GazableObject.CreateFromSceneColliders();
        }

        void Start()
        {
            StartCoroutine(CheckServiceRunningCoroutine());
        }

        private void OnDestroy()
        {
            headset.Dispose();
            UnityFuncs.DestroyNativeResources();
        }

        internal static void RegisterInterface(CompositorLayerCreateInfo info, FoveInterface xface)
        {
            if (Instance == null) // query forces it to exist
                return;

            m_sUnregisteredInterfaces.Add(new InterfaceInfo { info = info, xface = xface });
        }

        private static void RegisterHelper(InterfaceInfo reg)
        {
            var layerId = UnityFuncs.GetLayerForCreateInfo(reg.info);

            if (!m_sInterfaceStacks.ContainsKey(layerId))
                m_sInterfaceStacks.Add(layerId, new List<InterfaceInfo>());

            var theStack = m_sInterfaceStacks[layerId];
            if (theStack.Contains(reg))
                return;

            int idx = theStack.Count;
            for (int i = 0; i < theStack.Count; ++i)
            {
                if (theStack[i].xface.Camera.depth > reg.xface.Camera.depth)
                {
                    idx = i;
                    break;
                }
            }

            theStack.Insert(idx, reg);
        }

        internal static void UnregisterInterface(FoveInterface xface)
        {
            var unregisteredMatch = m_sUnregisteredInterfaces.FirstOrDefault(i => i.xface == xface);
            if (unregisteredMatch != null)
                m_sUnregisteredInterfaces.Remove(unregisteredMatch);

            var layerIds = m_sInterfaceStacks.Keys.ToList();
            foreach (var layerId in layerIds)
            {
                var interfaces = m_sInterfaceStacks[layerId];
                var registeredMatch = interfaces.FirstOrDefault(i => i.xface == xface);
                if (registeredMatch != null)
                    interfaces.Remove(registeredMatch);

                if (interfaces.Count == 0)
                {
                    UnityFuncs.DeleteLayer(layerId);
                    m_sInterfaceStacks.Remove(layerId);
                }
            }
        }

        private static ClientCapabilities ComputeInterfacesCapabilities()
        {
            Func<FoveInterface, ClientCapabilities> getCapabilities = xface =>
            {
                var capabilities = ClientCapabilities.None;
                if (xface.isActiveAndEnabled)
                {
                    if (xface.fetchGaze)
                        capabilities |= ClientCapabilities.EyeTracking | ClientCapabilities.GazeDepth;
                    if (xface.fetchOrientation)
                        capabilities |= ClientCapabilities.OrientationTracking;
                    if (xface.fetchPosition)
                        capabilities |= ClientCapabilities.PositionTracking;
                }
                return capabilities;
            };

            var aggregatedCaps = ClientCapabilities.None;

            foreach (var interfaceList in m_sInterfaceStacks.Values)
                foreach (var interfaceInfo in interfaceList)
                    aggregatedCaps |= getCapabilities(interfaceInfo.xface);

            // Also take into account unregistered interface in order to avoid to have 1 frame of delay
            // (even if internally add/removing a new capability may take more than 1 frame anyway)
            foreach (var interfaceInfo in m_sUnregisteredInterfaces)
                aggregatedCaps |= getCapabilities(interfaceInfo.xface);

            return aggregatedCaps;
        }

        private IEnumerator CheckServiceRunningCoroutine()
        {
            if(!CheckServiceRunning(true))
            {
                var wait = new WaitForSecondsRealtime(0.5f);
                while (CheckServiceRunning(false))
                    yield return wait;
            }
            StartCoroutine(CheckForHeadsetCoroutine());
        }

        private bool CheckServiceRunning(bool logNotConnected)
        {
            var err = CheckSoftwareVersions();
            switch (err.error)
            {
                case ErrorCode.None:
                    return true;
                case ErrorCode.Connect_ClientVersionTooOld:
                    Debug.LogError("Plugin client version is too old; please seek a newer plugin package.");
                    return true;
                case ErrorCode.Connect_RuntimeVersionTooOld:
                    Debug.LogError("Fove runtime version is too old; please update your runtime.");
                    return true;
                case ErrorCode.UnknownError:
                    Debug.LogError("An unhandled exception was thrown by Fove CheckSoftwareVersions");
                    return true;
                case ErrorCode.Connect_NotConnected:
                    if(logNotConnected)
                        Debug.Log("No runtime service found. Please start the fove runtime service.");
                    return false;
                default:
                    Debug.LogError("An unknown error was returned by Fove CheckSoftwareVersions: " + err);
                    return true;
            }
        }

        private IEnumerator CheckForHeadsetCoroutine()
        {
            var wait = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                if (IsHardwareConnected())
                    break;

                // Try again in a half second
                yield return wait;
            }

            Debug.Log("Connected to FOVE hardware.");
            StartCoroutine(FoveUpdateCoroutine());
        }

        private IEnumerator FoveUpdateCoroutine()
        {
            if (FoveSettings.ShouldForceCalibration)
                StartEyeTrackingCalibration(new CalibrationOptions { lazy = true });

            var endOfFrameWait = new WaitForEndOfFrame();

            while (Application.isPlaying)
            {
                // update the headset capabilities if changed
                UpdateCapabilities();

                if (!UpdateHmdDataInternal()) // hmd got disconnected
                    break;

                // Don't do any rendering code (below) if the compositor isn't ready
                if (!CompositorReadyCheck())
                    continue;

                // On first run and in case any new FoveInterfaces have been created
                if (m_sUnregisteredInterfaces.Count > 0)
                {
                    foreach (var reg in m_sUnregisteredInterfaces)
                        RegisterHelper(reg);
                    m_sUnregisteredInterfaces.Clear();
                }

                // Render all cameras, one eye at a time
                RenderTexture oldCurrent = RenderTexture.active;
                foreach (var list in m_sInterfaceStacks)
                {
                    if (list.Value.Count == 0)
                        continue;

                    int layerId = list.Key;
                    var eyeTx = GetEyeTextures(layerId);

                    UnityFuncs.SetPoseForSubmit(layerId, hmdPose);

                    if (eyeTx.areNew)
                    {
                        var texPtrLeft = eyeTx.left.GetNativeTexturePtr();
                        var texPtrRight = eyeTx.right.GetNativeTexturePtr();

                        // texture native ptr get valid only after first flush
                        if (texPtrLeft != IntPtr.Zero && texPtrRight != IntPtr.Zero)
                        {
                            UnityFuncs.SetLeftEyeTexture(layerId, texPtrLeft);
                            UnityFuncs.SetRightEyeTexture(layerId, texPtrRight);
                            eyeTx.areNew = false;
                        }
                        else
                        {
                            // force the creation of the new render targets
                            Graphics.SetRenderTarget(eyeTx.left);
                            Graphics.SetRenderTarget(eyeTx.right);
                            GL.Flush();
                            break;
                        }
                    }

                    Graphics.SetRenderTarget(eyeTx.left);
                    GL.Clear(true, true, Color.clear);
                    Graphics.SetRenderTarget(eyeTx.right);
                    GL.Clear(true, true, Color.clear);

                    foreach (var interfaceInfo in list.Value)
                    {
                        interfaceInfo.xface.RenderEye(Eye.Left, eyeTx.left);
                        interfaceInfo.xface.RenderEye(Eye.Right, eyeTx.right);
                    }

                    GL.Flush();
                    GL.IssuePluginEvent(submitNativeFunc, layerId);

                    if (!FoveSettings.CustomDesktopView)
                    {
                        // this code works only because we only have one single layer (base) allowed for the moment
                        // TODO: Adapt this code as soon as we allow several layers
                        RenderTexture.active = oldCurrent;
                        screenBlitMaterial.SetTexture("_TexLeft", eyeTx.left);
                        screenBlitMaterial.SetTexture("_TexRight", eyeTx.right);
                        Graphics.Blit(null, screenBlitMaterial);
                    }
                }
                GL.Flush();

                // Wait for render pose
                GL.IssuePluginEvent(wfrpNativeFunc, 0);

                yield return endOfFrameWait;
            }

            StartCoroutine(CheckForHeadsetCoroutine());
        }

        private void UpdateCapabilities()
        {
            var newCapabilities = ComputeInterfacesCapabilities() | enforcedCapabilities;
            if (currentCapabilities != newCapabilities)
            {
                var removedCaps = currentCapabilities & ~newCapabilities;
                if (removedCaps != ClientCapabilities.None)
                {
                    UnityFuncs.UnregisterCapabilities(removedCaps);
                    Headset.UnregisterCapabilities(removedCaps);
                }

                var addedCaps = newCapabilities & ~currentCapabilities;
                if (addedCaps != ClientCapabilities.None)
                {
                    UnityFuncs.RegisterCapabilities(addedCaps);
                    Headset.RegisterCapabilities(addedCaps);
                }

                currentCapabilities = newCapabilities;
            }
        }

        private bool UpdateHmdDataInternal()
        {
            var isHmdConnectedChanged = false;

            // First chech and update the HMD connection status
            // If the HMD happens to be disconnected we trigger the associated event and abort the update process
            // If the HMD status changed to connected, we delay the trigger of the associated event after the update of other data
            {
                var isHmdConnected = Headset.IsHardwareConnected();
                if (isHmdConnected.error == ErrorCode.Hardware_Disconnected) // this returns an error whereas it is was we are querying, so we just ignore it...
                    isHmdConnected.error = ErrorCode.None;

                if (isHmdConnected != wasHardwareConnected)
                {
                    isHmdConnectedChanged = true;
                    wasHardwareConnected = isHmdConnected;
                    if (!isHmdConnected)
                    {
                        // trigger the hmd disconnected event before returning
                        var handler = HardwareDisconnected;
                        if (handler != null)
                            handler.Invoke();

                        return false; // abort the update and mark it as failure
                    }
                }
            }

            // Fetch the new eye tracking data from the service
            var fetchResult = Headset.FetchEyeTrackingData();
            if (fetchResult.error == ErrorCode.Connect_NotConnected)
                return false;

            // Fetch the new pose data from the service
            fetchResult = Headset.FetchPoseData();
            if (fetchResult.error == ErrorCode.Connect_NotConnected)
                return false;

            // HMD pose
            // It is taken from the UnityFunction native plugin to be sure to return
            // the same pose as the one used to perform the rendering
            hmdPose = UnityFuncs.GetLastPose();
            headPosition = hmdPose.position.ToVector3() * worldScale;
            standingPosition = hmdPose.standingPosition.ToVector3() * worldScale;
            headRotation = hmdPose.orientation.ToQuaternion();

            // Update the eyes & position textures
            TryUpdateTexture(Headset.GetEyesImage, ref eyesImage, ref eyesTexture);
            TryUpdateTexture(Headset.GetPositionImage, ref positionImage, ref positionTexture);

            // update the mirror texture native pointer if is exists
            // we need this update because of the rolling buffer
            if (mirrorTexture.value != null)
            {
                int dummy;
                IntPtr texPtr;
                GetMirrorTexturePtr(out texPtr, out dummy, out dummy);
                mirrorTexture.value.UpdateExternalTexture(texPtr);
            }

            // Call user custom data update callbacks
            var addInUpdateCallback = AddInUpdate;
            if (addInUpdateCallback != null)
                addInUpdateCallback();

            // Trigger the event callbacks now that have updated all the HMD data
            if (isHmdConnectedChanged)
            {
                if (!wasHardwareConnected)
                    throw new Exception("Internal error: Unexpected hmd connection status");

                var handler = HardwareConnected;
                if (handler != null)
                    handler.Invoke();
            }

            var hwdReady = IsHardwareReady();
            if (hwdReady.IsValid && wasHardwareReady != hwdReady)
            {
                wasHardwareReady = hwdReady;
                var handler = hwdReady ? HardwareIsReady : null;
                if (handler != null)
                    handler.Invoke();
            }

            var isCalibrating = IsEyeTrackingCalibrating();
            if (isCalibrating.IsValid && isCalibrating != wasCalibrating)
            {
                if (wasCalibrating)
                {
                    var handler = EyeTrackingCalibrationStarted;
                    if (handler != null)
                        handler.Invoke();
                }
                else
                {
                    var handler = EyeTrackingCalibrationEnded;
                    if (handler != null)
                        handler.Invoke(GetEyeTrackingCalibrationState());
                }
                wasCalibrating = isCalibrating;
            }

            var userAttention = IsUserShiftingAttention();
            if (userAttention.IsValid && wasShiftingAttention != userAttention)
            {
                wasShiftingAttention = userAttention;
                var handler = IsUserShiftingAttentionChanged;
                if (handler != null)
                    handler.Invoke(wasShiftingAttention);
            }

            UpdateEyeState(Eye.Left);
            UpdateEyeState(Eye.Right);

            var userPresent = IsUserPresent();
            if (userPresent.IsValid && wasUserPresent != userPresent)
            {
                wasUserPresent = userPresent;
                var handler = UserPresenceChanged;
                if (handler != null)
                    handler.Invoke(wasUserPresent);
            }

            var adjusmentGuiVisible = IsHmdAdjustmentGuiVisible();
            if (adjusmentGuiVisible.IsValid && wasHmdAdjustmentVisible != adjusmentGuiVisible)
            {
                wasHmdAdjustmentVisible = adjusmentGuiVisible;
                var handler = HmdAdjustmentGuiVisibilityChanged;
                if (handler != null)
                    handler.Invoke(wasHmdAdjustmentVisible);
            }

            return true;
        }

        private void UpdateEyeState(Eye eye)
        {
            var eyeState = GetEyeState(eye);
            if (eyeState.IsValid && previousEyeStates[eye] != eyeState)
            {
                previousEyeStates[eye] = eyeState;
                var handler = EyeStateChanged;
                if (handler != null)
                    handler.Invoke(eye, eyeState);
            }
        }

        private Vector3 GetEyeOffsetVector(Matrix44 eyeToHeadMatrix)
        {
            float iod = eyeToHeadMatrix.m03;
            float eyeHeight = eyeToHeadMatrix.m13;
            float eyeForward = eyeToHeadMatrix.m23;
            return new Vector3(iod, eyeHeight, eyeForward) * worldScale;
        }

        private static bool CompositorReadyCheck()
        {
            var isCompositorReady = false; 
            UnityFuncs.IsCompositorReady(ref isCompositorReady);
            return isCompositorReady;
        }

        private static void TryUpdateTexture(Func<Result<Bitmap>> getImageFunc, ref Bitmap cacheImg, ref Result<Texture2D> texResult)
        {
            try
            {
                var imgResult = getImageFunc();

                texResult.error = imgResult.error;
                if (texResult.Failed)
                    return;

                if (cacheImg != null && imgResult.value.Timestamp <= cacheImg.Timestamp)
                    return;

                cacheImg = imgResult.value;
                if (cacheImg.Width == 0 || cacheImg.Height == 0)
                    return;

                var tex = texResult.value;
                if (tex != null && (tex.width != cacheImg.Width || tex.height != cacheImg.Height))
                {
                    Texture2D.Destroy(tex);
                    tex = null;
                }

                if (tex == null)
                    tex = new Texture2D(cacheImg.Width, cacheImg.Height, TextureFormat.RGB24, false);

                tex.LoadRawTextureData(cacheImg.ImageData.data, (int)cacheImg.ImageData.length);
                tex.Apply();

                texResult.value = tex;
            }
            catch (Exception e)
            {
                Debug.Log("Error trying to load image bitmap: " + e);
            }
        }

        public Result<Texture2D> GetMirrorTextureInternal()
        {
            if (mirrorTexture.value == null)
            {
                IntPtr texPtr;
                int texWidth, texHeight;
                GetMirrorTexturePtr(out texPtr, out texWidth, out texHeight);
                if (texPtr != IntPtr.Zero) // the mirror texture doesn't exist yet
                {
                    mirrorTexture.value = Texture2D.CreateExternalTexture(texWidth, texHeight, TextureFormat.RGBA32, false, false, texPtr);
                    mirrorTexture.error = ErrorCode.None;
                }
            }

            return mirrorTexture;
        }

        #region Native bindings

        private static class UnityFuncs
        {
            [DllImport("FoveUnityFuncs", EntryPoint = "getSubmitFunctionPtr")]
            public static extern IntPtr GetSubmitFunctionPtr();
            [DllImport("FoveUnityFuncs", EntryPoint = "getWfrpFunctionPtr")]
            public static extern IntPtr GetWfrpFunctionPtr();

            [DllImport("FoveUnityFuncs")]
            public static extern void SetLogSinkFunction(IntPtr fp);
            [DllImport("FoveUnityFuncs", EntryPoint = "resetState")]
            public static extern void ResetNativeState();
            [DllImport("FoveUnityFuncs", EntryPoint = "destroyResources")]
            public static extern void DestroyNativeResources();

            [DllImport("FoveUnityFuncs", EntryPoint = "registerCapabilities")]
            public static extern void RegisterCapabilities(ClientCapabilities caps);
            [DllImport("FoveUnityFuncs", EntryPoint = "unregisterCapabilities")]
            public static extern void UnregisterCapabilities(ClientCapabilities caps);

            [DllImport("FoveUnityFuncs", EntryPoint = "isCompositorReady")]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern ErrorCode IsCompositorReady(ref bool isReady);
            [DllImport("FoveUnityFuncs", EntryPoint = "getLayerForCreateInfo")]
            public static extern int GetLayerForCreateInfo(CompositorLayerCreateInfo info);
            [DllImport("FoveUnityFuncs", EntryPoint = "deleteLayer")]
            public static extern int DeleteLayer(int layerId);
            [DllImport("FoveUnityFuncs", EntryPoint = "getIdealLayerDimensions")]
            public static extern void GetIdealLayerDimensions(int layerId, ref Vec2i dims);

            [DllImport("FoveUnityFuncs", EntryPoint = "setLeftEyeTexture")]
            public static extern void SetLeftEyeTexture(int layerId, IntPtr texPtr);
            [DllImport("FoveUnityFuncs", EntryPoint = "setRightEyeTexture")]
            public static extern void SetRightEyeTexture(int layerId, IntPtr texPtr);
            [DllImport("FoveUnityFuncs", EntryPoint = "setPoseForSubmit")]
            public static extern void SetPoseForSubmit(int layerId, Pose pose);

            [DllImport("FoveUnityFuncs", EntryPoint = "getLastPose")]
            public static extern Pose GetLastPose();
        }

        [DllImport("FoveUnityFuncs", EntryPoint = "getMirrorTexturePtr")]
        private static extern void GetMirrorTexturePtr(out IntPtr texPtr, out int texWidth, out int texHeight);

        #endregion
    }
}
