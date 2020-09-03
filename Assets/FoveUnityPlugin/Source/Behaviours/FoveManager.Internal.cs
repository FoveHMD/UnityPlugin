using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine.Events;
using UnityEngine;

using System.Linq;

namespace Fove.Unity
{
    // FoveManager needs to reference a gameObject in order to create coroutines.
    public partial class FoveManager
    {
        // Static members
        private static FoveManager m_sInstance;
        private Headset m_headset;

        // Data update events
        private static PoseUpdateEvent _sPoseEvt = new PoseUpdateEvent();
        private static EyePositionEvent _sEyePositionEvt = new EyePositionEvent();
        private static GazeEvent _sGazeEvt = new GazeEvent();

        // Matrix update events don't include the projection matrix because each camera may have a different
        // near/far plane setting and so the matrix itself cannot be used. Instead, each subscriber must
        // call into FoveManager.Instance.GetProjectionMatrices itself on this event.
        private static EyeProjectionEvent _sEyeProjectionEvt = new EyeProjectionEvent();
    
        // Static caches
        private static Pose m_sLastPose = Pose.Null;
        private static Quaternion m_sHeadRotation = Quaternion.identity;
        private static Vector3 m_sHeadPosition;
        private static Vector3 m_sStandingPosition;

        private static Result<Stereo<Vector3>> m_sEyeOffsets = new Result<Stereo<Vector3>>(new Stereo<Vector3>(Vector3.zero), ErrorCode.Data_NoUpdate);
        private static Result<Eye> m_sEyeClosed = new Result<Eye>(Eye.Both, ErrorCode.Data_NoUpdate);

        // Values users may ask for
        private static Result<Stereo<Vector3>> m_sEyeVectors = new Result<Stereo<Vector3>>(new Stereo<Vector3>(Vector3.forward), ErrorCode.Data_NoUpdate);
        private static Result<GazeConvergenceData> m_sConvergenceData = new Result<GazeConvergenceData>(GazeConvergenceData.ForwardToInfinity, ErrorCode.Data_NoUpdate);
        private static Result<float> m_sPupilDilation = new Result<float>(1.0f, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsHardwareConnected = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsHardwareReady = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsCalibrating = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsCalibrated = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<CalibrationState> m_sCalibationState = new Result<CalibrationState>(CalibrationState.NotStarted, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsGazeFixated = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsUserPresent = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<bool> m_sIsHmdAdjustmentVisible = new Result<bool>(false, ErrorCode.Data_NoUpdate);
        private static Result<int> m_sGazedObjectId = new Result<int>(Fove.GazableObject.IdInvalid, ErrorCode.Data_NoUpdate);

        // Settings cache for runtime
        private static float m_worldScale = 1.0f;
        private static float m_renderScale = 1.0f;
    
        // Rendering/submission native pointers
        private static IntPtr m_submitNativeFunc;
        private static IntPtr m_wfrpNativeFunc;

        private Material m_screenBlitMaterial;

        private static ClientCapabilities m_sCurrentCapabilities = ClientCapabilities.None;
        private static ClientCapabilities m_sEnforcedCapabilities = ClientCapabilities.None;

        // static awaiters
        private static WaitUntil m_sWaitForHardwareConnected = new WaitUntil(() => IsHardwareConnected());
        private static WaitUntil m_sWaitForHardwareDisconnected = new WaitUntil(() => !IsHardwareConnected());
        private static WaitUntil m_sWaitForHardwareReady = new WaitUntil(() => IsHardwareReady());
        private static WaitUntil m_sWaitForCalibrationStart = new WaitUntil(() => IsEyeTrackingCalibrating());
        private static WaitUntil m_sWaitForCalibrationEnd = new WaitUntil(() => !IsEyeTrackingCalibrating());
        private static WaitUntil m_sWaitForCalibrationCalibrated = new WaitUntil(() => IsEyeTrackingCalibrated());
        private static WaitUntil m_sWaitForUser = new WaitUntil(() => IsUserPresent());

        private class EyeTextures
        {
            public RenderTexture left;
            public RenderTexture right;
            public bool areNew;
        }
        private static Dictionary<int, EyeTextures> m_sEyeTextures = new Dictionary<int, EyeTextures>();

        /// <summary>
        /// The fove manager instance that is communicating and managing the HMD.
        /// </summary>
        internal static FoveManager Instance
        {
            get {
                if (m_sInstance == null)
                    InitializeStaticMembers();

                return m_sInstance;
            }
        }

        private static void InitializeStaticMembers()
        {
            m_sInstance = FindObjectOfType<FoveManager>();
            if (m_sInstance == null)
            {
                m_sInstance = new GameObject("~FOVE Manager").AddComponent<FoveManager>();
                DontDestroyOnLoad(m_sInstance);
            }

            m_worldScale = FoveSettings.WorldScale;
            m_renderScale = FoveSettings.RenderScale;

            m_submitNativeFunc = UnityFuncs.GetSubmitFunctionPtr();
            m_wfrpNativeFunc = UnityFuncs.GetWfrpFunctionPtr();
        }

        // Data update events
        internal class PoseUpdateEvent : UnityEvent<Vector3, Vector3, Quaternion> { }
        internal static PoseUpdateEvent PoseUpdate { get { return _sPoseEvt; } }

        // Matrix update events don't include the projection matrix because each camera may have a different
        // near/far plane setting and so the matrix itself cannot be used. Instead, each subscriber must
        // call into FoveManager.Instance.GetProjectionMatrices itself on this event.
        internal class EyeProjectionEvent : UnityEvent { }
        internal static EyeProjectionEvent EyeProjectionUpdate { get { return _sEyeProjectionEvt; } }

        internal class EyePositionEvent : UnityEvent<Result<Stereo<Vector3>>> { }
        internal static EyePositionEvent EyePositionUpdate { get { return _sEyePositionEvt; } }

        internal class GazeEvent : UnityEvent<Result<GazeConvergenceData>, Result<Stereo<Vector3>>> { }
        internal static GazeEvent GazeUpdate { get { return _sGazeEvt; } }

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

            m_sEyeTextures[layerId] = result;

            return result;
        }

        private EyeTextures GetEyeTextures(int layerId)
        {
            EyeTextures result;
        
            Vec2i dims = new Vec2i(1, 1);
            UnityFuncs.GetIdealLayerDimensions(layerId, ref dims);

            dims.x = (int)(dims.x * m_renderScale);
            dims.y = (int)(dims.y * m_renderScale);

            if (m_sEyeTextures.ContainsKey(layerId))
            {
                result = m_sEyeTextures[layerId];

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

        internal static void RegisterInterface(CompositorLayerCreateInfo info, FoveInterface xface)
        {
            if (Instance == null) // query forces it to exist
                return;
            
            m_sUnregisteredInterfaces.Add(new InterfaceInfo{info = info, xface = xface});
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
                if(xface.isActiveAndEnabled)
                {
                    if (xface.fetchGaze)
                        capabilities |= ClientCapabilities.Gaze;
                    if (xface.fetchOrientation)
                        capabilities |= ClientCapabilities.Orientation;
                    if (xface.fetchPosition)
                        capabilities |= ClientCapabilities.Position;
                }
                return capabilities;
            };

            var aggregatedCaps = ClientCapabilities.None;

            foreach(var interfaceList in m_sInterfaceStacks.Values)
                foreach (var interfaceInfo in interfaceList)
                    aggregatedCaps |= getCapabilities(interfaceInfo.xface);

            // Also take into account unregistered interface in order to avoid to have 1 frame of delay
            // (even if internally add/removing a new capability may take more than 1 frame anyway)
            foreach(var interfaceInfo in m_sUnregisteredInterfaces)
                aggregatedCaps |= getCapabilities(interfaceInfo.xface);

            return aggregatedCaps;
        }
        
        /*******************************************************************************\
         * MonoBehaviour / instance methods                                            *
        \*******************************************************************************/
        private FoveManager()
        {
            if (m_sInstance != null)
                Debug.LogError("Found an existing instance");

            var logSinkDelegatePtr = Marshal.GetFunctionPointerForDelegate(logSinkDelegate);
            UnityFuncs.SetLogSinkFunction(logSinkDelegatePtr);

            m_headset = new Headset(m_sCurrentCapabilities);
        }

        void Awake()
        {
            m_headset.GazeCastPolicy = FoveSettings.GazeCastPolicy;

            UnityFuncs.ResetNativeState();
            m_screenBlitMaterial = new Material(Shader.Find("Fove/EyeShader"));

            if (FoveSettings.AutomaticObjectRegistration)
                GazableObject.CreateFromSceneColliders();
        }

        void Start()
        {
            StartCoroutine(CheckServiceRunningCoroutine());
        }

        private void OnDestroy()
        {
            m_headset.Dispose();
            UnityFuncs.DestroyNativeResources();
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
            switch (err.value)
            {
                case ErrorCode.None:
                    return true;
                case ErrorCode.Connect_ClientVersionTooOld:
                    Debug.LogError("Plugin client version is too old; please seek a newer plugin package.");
                    return true;
                case ErrorCode.Connect_RuntimeVersionTooOld:
                    Debug.LogError("Fove runtime version is too old; please update your runtime.");
                    return true;
                case ErrorCode.Server_General:
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
                if (IsHardwareConnected(true))
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
                if(!UpdateHmdData()) // hmd got disconnected
                    break;

                // update the headset capabilities if changed
                UpdateCapabilities();

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

                    UnityFuncs.SetPoseForSubmit(layerId, m_sLastPose);

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
                    GL.IssuePluginEvent(m_submitNativeFunc, layerId);

                    if (!FoveSettings.CustomDesktopView)
                    {
                        // this code works only because we only have one single layer (base) allowed for the moment
                        // TODO: Adapt this code as soon as we allow several layers
                        RenderTexture.active = oldCurrent;
                        m_screenBlitMaterial.SetTexture("_TexLeft", eyeTx.left);
                        m_screenBlitMaterial.SetTexture("_TexRight", eyeTx.right);
                        Graphics.Blit(null, m_screenBlitMaterial);
                    }
                }
                GL.Flush();

                // Wait for render pose
                GL.IssuePluginEvent(m_wfrpNativeFunc, 0);

                yield return endOfFrameWait;
            }

            StartCoroutine(CheckForHeadsetCoroutine());
        }

        private static void UpdateCapabilities()
        {
            var newCapabilities = ComputeInterfacesCapabilities() | m_sEnforcedCapabilities;
            if (m_sCurrentCapabilities != newCapabilities)
            {
                var removedCaps = m_sCurrentCapabilities & ~newCapabilities;
                if (removedCaps != ClientCapabilities.None)
                {
                    UnityFuncs.UnregisterCapabilities(removedCaps);
                    Headset.UnregisterCapabilities(removedCaps);
                }

                var addedCaps = newCapabilities & ~m_sCurrentCapabilities;
                if (addedCaps != ClientCapabilities.None)
                {
                    UnityFuncs.RegisterCapabilities(addedCaps);
                    Headset.RegisterCapabilities(addedCaps);
                }

                m_sCurrentCapabilities = newCapabilities;
            }
        }

        private static bool UpdateHmdData()
        {
            var isHmdConnectedChanged = false;
            var isGazeFixedChanged = false;
            var eyeClosedChanged = false;
            var hardwareReadyChanged = false;
            var isCalibratingChanged = false;
            var isCalibratedChanged = false;
            var isUserPresentChanged = false;
            var isHmdAdjustmentGuiChanged = false;

            // First chech and update the HMD connection status
            // If the HMD happens to be disconnected we trigger the associated event and abort the update process
            // If the HMD status changed to connected, we delay the trigger of the associated event after the update of other data
            {
                bool isHmdConnected;
                var error = Headset.IsHardwareConnected(out isHmdConnected);
                if (error == ErrorCode.Hardware_Disconnected) // this returns an error whereas it is was we are querying, so we just ignore it...
                    error = ErrorCode.None;

                if (isHmdConnected != m_sIsHardwareConnected)
                {
                    isHmdConnectedChanged = true;
                    m_sIsHardwareConnected.error = error;
                    m_sIsHardwareConnected.value = isHmdConnected;
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

            // HMD pose
            m_sLastPose = UnityFuncs.GetLastPose();
            m_sHeadPosition = m_sLastPose.position.ToVector3() * m_worldScale;
            m_sStandingPosition = m_sLastPose.standingPosition.ToVector3() * m_worldScale;
            m_sHeadRotation = m_sLastPose.orientation.ToQuaternion();

            // Gaze vectors
            {
                GazeVector lGaze, rGaze;
                m_sEyeVectors.error = Headset.GetGazeVectors(out lGaze, out rGaze);
                if (m_sEyeVectors.Succeeded)
                {
                    m_sEyeVectors.value.left = lGaze.vector.ToVector3();
                    m_sEyeVectors.value.right = rGaze.vector.ToVector3();
                }
            }

            // Gaze convergence
            {
                Fove.GazeConvergenceData conv;
                var errConv = Headset.GetGazeConvergence(out conv);

                m_sIsGazeFixated.error = errConv;
                m_sPupilDilation.error = errConv;
                m_sGazedObjectId.error = errConv;
                m_sConvergenceData.error = errConv;

                if (errConv == ErrorCode.None)
                {
                    isGazeFixedChanged = m_sIsGazeFixated != conv.attention;
                    
                    var convRay = conv.ray.ToRay();
                    m_sPupilDilation.value = conv.pupilDilation;
                    m_sIsGazeFixated.value = conv.attention;
                    m_sGazedObjectId.value = conv.gazedObjectId;
                    m_sConvergenceData.value.distance = m_worldScale * conv.distance;
                    m_sConvergenceData.value.ray = new Ray(m_worldScale * convRay.origin, convRay.direction);
                }
            }

            // Eye offset vectors
            {
                Matrix44 lEyeMx, rEyeMx;
                m_sEyeOffsets.error = Headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
                if (m_sEyeOffsets.Succeeded)
                {
                    GetEyeOffsetVector(ref lEyeMx, out m_sEyeOffsets.value.left);
                    GetEyeOffsetVector(ref rEyeMx, out m_sEyeOffsets.value.right);
                }
            }

            // Eye closed
            {
                Eye eyeClosed;
                m_sEyeClosed.error = Headset.CheckEyesClosed(out eyeClosed);
                if (m_sEyeClosed.Succeeded)
                {
                    eyeClosedChanged = eyeClosed != m_sEyeClosed;
                    m_sEyeClosed.value = eyeClosed;
                }
            }

            // Hardware Ready
            {
                bool isReady;
                m_sIsHardwareReady.error = Headset.IsHardwareReady(out isReady);
                if (m_sIsHardwareReady.Succeeded)
                {
                    hardwareReadyChanged = m_sIsHardwareReady != isReady;
                    m_sIsHardwareReady.value = isReady;
                }
            }

            // Is Calibrating
            {
                bool isCalibrating;
                m_sIsCalibrating.error = Headset.IsEyeTrackingCalibrating(out isCalibrating);
                if (m_sIsCalibrating.Succeeded)
                {
                    isCalibratingChanged = m_sIsCalibrating != isCalibrating;
                    m_sIsCalibrating.value = isCalibrating;
                }
            }

            // Is Calibrated
            {
                bool isCalibrated;
                m_sIsCalibrated.error = Headset.IsEyeTrackingCalibrated(out isCalibrated);
                if (m_sIsCalibrated.Succeeded)
                {
                    isCalibratedChanged = m_sIsCalibrated != isCalibrated;
                    m_sIsCalibrated.value = isCalibrated;
                }
            }

            // Calibration state
            {
                CalibrationState state;
                m_sCalibationState.error = Headset.GetEyeTrackingCalibrationState(out state);
                if (m_sCalibationState.Succeeded)
                    m_sCalibationState.value = state;
            }

            // User presence
            {
                bool userPresent;
                m_sIsUserPresent.error = Headset.IsUserPresent(out userPresent);
                if (m_sIsUserPresent.Succeeded)
                {
                    isUserPresentChanged = m_sIsUserPresent != userPresent;
                    m_sIsUserPresent.value = userPresent;
                }
            }

            // Hmd Adjustment Gui
            {
                bool guiVisible;
                m_sIsHmdAdjustmentVisible.error = Headset.IsHmdAdjustmentGuiVisible(out guiVisible);
                if (m_sIsHmdAdjustmentVisible.Succeeded)
                {
                    isHmdAdjustmentGuiChanged = m_sIsHmdAdjustmentVisible != guiVisible;
                    m_sIsHmdAdjustmentVisible.value = guiVisible;
                }
            }

            // Trigger the different internal registered data updates
            PoseUpdate.Invoke(m_sHeadPosition, m_sStandingPosition, m_sHeadRotation);
            EyePositionUpdate.Invoke(m_sEyeOffsets);
            EyeProjectionUpdate.Invoke();
            GazeUpdate.Invoke(m_sConvergenceData, m_sEyeVectors);

            // Call user custom data update callbacks
            var addInUpdateCallback = AddInUpdate;
            if (addInUpdateCallback != null)
                addInUpdateCallback();

            // Trigger the event callbacks now that have updated all the HMD data
            if (isHmdConnectedChanged)
            {
                if (!m_sIsHardwareConnected)
                    throw new Exception("Internal error: Unexpected hmd connection status");

                var handler = HardwareConnected;
                if (handler != null)
                    handler.Invoke();
            }

            if (hardwareReadyChanged && m_sIsHardwareReady)
            {
                var handler = HardwareIsReady;
                if (handler != null)
                    handler.Invoke();
            }

            if (isCalibratingChanged)
            {
                if (m_sIsCalibrating)
                {
                    var handler = EyeTrackingCalibrationStarted;
                    if (handler != null)
                        handler.Invoke();
                }
                else
                {
                    var handler = EyeTrackingCalibrationEnded;
                    if (handler != null)
                        handler.Invoke(m_sCalibationState.value);
                }
            }

            if (isGazeFixedChanged)
            {
                var handler = IsGazeFixatedChanged;
                if (handler != null)
                    handler.Invoke(m_sIsGazeFixated.value);
            }

            if (eyeClosedChanged)
            {
                var handler = EyesClosedChanged;
                if (handler != null)
                    handler.Invoke(m_sEyeClosed.value);
            }

            if (isUserPresentChanged)
            {
                var handler = UserPresenceChanged;
                if (handler != null)
                    handler.Invoke(m_sIsUserPresent.value);
            }

            if (isHmdAdjustmentGuiChanged)
            {
                var handler = HmdAdjustmentGuiVisibilityChanged;
                if (handler != null)
                    handler.Invoke(m_sIsHmdAdjustmentVisible.value);
            }

            return true;
        }

        private static void GetEyeOffsetVector(ref Matrix44 eyeToHeadMatrix, out Vector3 eyeOffsetVector)
        {
            float iod = eyeToHeadMatrix.m03;
            float eyeHeight = eyeToHeadMatrix.m13;
            float eyeForward = eyeToHeadMatrix.m23;
            eyeOffsetVector = new Vector3(iod, eyeHeight, eyeForward) * m_worldScale;
        }

        private static bool CompositorReadyCheck()
        {
            var isCompositorReady = false; 
            UnityFuncs.IsCompositorReady(ref isCompositorReady);
            return isCompositorReady;
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

        #endregion
    }
}
