using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine.Events;
using UnityEngine;

using System.Linq;

namespace Fove.Unity
{
	public partial class FoveManager
	{
		// Static members
		private static FoveManager m_sInstance;
		private Headset m_headset;

		// Data update events
		private static PoseUpdateEvent _sPoseEvt;

		// Matrix update events don't include the projection matrix because each camera may have a different
		// near/far plane setting and so the matrix itself cannot be used. Instead, each subscriber must
		// call into FoveManager.Instance.GetProjectionMatrices itself on this event.
		private static EyeProjectionEvent _sEyeProjectionEvt;

		private static EyePositionEvent _sEyePositionEvt;
		private static GazeEvent _sGazeEvt;
	
		// Static caches
		private static Pose _sLastPose = new Pose { orientation = new Quat() };
		private static Quaternion m_sHeadRotation = Quaternion.identity;
		private static Vector3 m_sHeadPosition;
		private static Vector3 m_sStandingPosition;

		private static Vector3 m_sLeftEyeOffset;
		private static Vector3 m_sRightEyeOffset;
		private static Eye m_sEyeClosed = Eye.Both;

		// Values users may ask for
		private static Vector3 m_sEyeVecLeft = Vector3.forward;
		private static Vector3 m_sEyeVecRight = Vector3.forward;
		private static GazeConvergenceData m_sConvergenceData = new GazeConvergenceData(new Ray(Vector3.zero, Vector3.forward), Mathf.Infinity);
		private static float m_sPupilDilation = 1.0f;
		private static bool m_sGazeFixated = false;
	
		private static bool m_isHmdConnected = false;

		// Settings cache for runtime
		private static float m_worldScale = 1.0f;
		private static float m_renderScale = 1.0f;
	
		// Rendering/submission native pointers
		private static IntPtr m_submitNativeFunc;
		private static IntPtr m_wfrpNativeFunc;

        private Material m_screenBlitMaterial;

        private class EyeTextures
		{
			public RenderTexture left;
			public RenderTexture right;
			public bool areNew;
		}
		private static Dictionary<int, EyeTextures> m_eyeTextures;

		/// <summary>
		/// The fove manager instance that is communicating and managing the HMD.
		/// </summary>
		internal static FoveManager Instance
		{
			get
			{
				if (m_sInstance == null)
				{
					m_sInstance = FindObjectOfType<FoveManager>();
					if (m_sInstance == null)
					{
						m_sInstance = new GameObject("FOVE Manager (dynamic)").AddComponent<FoveManager>();
						DontDestroyOnLoad(m_sInstance);
					}

					m_worldScale = FoveSettings.WorldScale;
					m_renderScale = FoveSettings.RenderScale;

					m_submitNativeFunc = GetSubmitFunctionPtr();
					m_wfrpNativeFunc = GetWfrpFunctionPtr();
				}

				return m_sInstance;
			}
		}

		// Data update events
		internal class PoseUpdateEvent : UnityEvent<Vector3, Vector3, Quaternion> { }
		internal static PoseUpdateEvent PoseUpdate
		{
			get
			{
				if (_sPoseEvt == null)
					_sPoseEvt = new PoseUpdateEvent();
				return _sPoseEvt;
			}
		}

		// Matrix update events don't include the projection matrix because each camera may have a different
		// near/far plane setting and so the matrix itself cannot be used. Instead, each subscriber must
		// call into FoveManager.Instance.GetProjectionMatrices itself on this event.
		internal class EyeProjectionEvent : UnityEvent { }
		internal static EyeProjectionEvent EyeProjectionUpdate
		{
			get
			{
				if (_sEyeProjectionEvt == null)
					_sEyeProjectionEvt = new EyeProjectionEvent();
				return _sEyeProjectionEvt;
			}
		}

		internal class EyePositionEvent : UnityEvent<Vector3, Vector3> { }
		internal static EyePositionEvent EyePositionUpdate
		{
			get
			{
				if (_sEyePositionEvt == null)
					_sEyePositionEvt = new EyePositionEvent();
				return _sEyePositionEvt;
			}
		}

		internal class GazeEvent : UnityEvent<GazeConvergenceData, Vector3, Vector3> { }
		internal static GazeEvent GazeUpdate
		{
			get
			{
				if (_sGazeEvt == null)
					_sGazeEvt = new GazeEvent();
				return _sGazeEvt;
			}
		}

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

			m_eyeTextures[layerId] = result;

			return result;
		}

		private EyeTextures GetEyeTextures(int layerId)
		{
			if (m_eyeTextures == null)
				m_eyeTextures = new Dictionary<int, EyeTextures>();

			EyeTextures result;
		
			Vec2i dims = new Vec2i(1, 1);
			GetIdealLayerDimensions(layerId, ref dims);

			dims.x = (int)(dims.x * m_renderScale);
			dims.y = (int)(dims.y * m_renderScale);

			if (m_eyeTextures.ContainsKey(layerId))
			{
				result = m_eyeTextures[layerId];

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
		private static Dictionary<int, List<InterfaceInfo>> m_interfaceStacks = new Dictionary<int, List<InterfaceInfo>>();

		private static List<InterfaceInfo> unregisteredInterfaces = new List<InterfaceInfo>();

		internal static void RegisterInterface(CompositorLayerCreateInfo info, FoveInterface xface)
		{
			if (Instance == null) // query forces it to exist
				return;
			
			unregisteredInterfaces.Add(new InterfaceInfo{info = info, xface = xface});
		}

		private static void RegisterHelper(InterfaceInfo reg)
		{
			var layerId = GetLayerForCreateInfo(reg.info);

			if (!m_interfaceStacks.ContainsKey(layerId))
				m_interfaceStacks.Add(layerId, new List<InterfaceInfo>());

			var theStack = m_interfaceStacks[layerId];
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
			var unregisteredMatch = unregisteredInterfaces.FirstOrDefault(i => i.xface == xface);
			if (unregisteredMatch != null)
				unregisteredInterfaces.Remove(unregisteredMatch);

			foreach (var list in m_interfaceStacks.Values)
			{
				var registeredMatch = list.FirstOrDefault(i => i.xface == xface);
				if (registeredMatch != null)
					list.Remove(registeredMatch);
			}
		}
		
		/*******************************************************************************\
		 * MonoBehaviour / instance methods                                            *
		\*******************************************************************************/
		private FoveManager()
		{
			if (m_sInstance != null)
			{
				Debug.Log("Found an existing instance");
			}

			ClientCapabilities capabilities = 0;
			capabilities |= ClientCapabilities.Gaze;
			capabilities |= ClientCapabilities.Orientation;
			capabilities |= ClientCapabilities.Position;

			m_headset = new Headset(capabilities);
		}

		void Awake()
		{
			var err = CheckSoftwareVersions();
			switch (err)
			{
				case ErrorCode.None:
					break;
				case ErrorCode.Connect_ClientVersionTooOld:
					Debug.LogError("Plugin client version is too old; please seek a newer plugin package.");
					break;
				case ErrorCode.Connect_RuntimeVersionTooOld:
					Debug.LogError("Fove runtime version is too old; please update your runtime.");
					break;
				case ErrorCode.Server_General:
					Debug.LogError("An unhandled exception was thrown by Fove CheckSoftwareVersions");
					break;
				case ErrorCode.Connect_NotConnected:
					Debug.Log("[FOVE] No runtime service found; disabling.");
					return;
				default:
					Debug.LogError("An unknown error was returned by Fove CheckSoftwareVersions: " + err);
					break;
			}

			m_eyeTextures = null;
            
            m_screenBlitMaterial = new Material(Shader.Find("Fove/EyeShader"));

			ResetNativeState();
			StartCoroutine(CheckForHeadsetCoroutine());
		}

		private void OnApplicationQuit()
		{
			DestroyNativeResources();
		}

		private IEnumerator CheckForHeadsetCoroutine()
		{
			var wait = new WaitForSecondsRealtime(0.5f);
			while (true)
			{
				ErrorCode err;
				err = m_headset.IsHardwareConnected(out m_isHmdConnected);
				if (err != ErrorCode.None && err != ErrorCode.Connect_NotConnected)
				{
					Debug.Log("An error occurred checking for hardware connected: " + err);
				}

				if (m_isHmdConnected)
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
			{
				var err = m_headset.EnsureEyeTrackingCalibration();
				if (err != ErrorCode.None)
				{
					Debug.Log("Error on EnsureEyeTrackingCalibration: " + err);
				}
			}

			var endOfFrameWait = new WaitForEndOfFrame();

            while (Application.isPlaying)
            {
				yield return endOfFrameWait;

				ErrorCode err;
				err = m_headset.IsHardwareConnected(out m_isHmdConnected);
				if (err != ErrorCode.None)
				{
					Debug.Log("Error checking hardware connection state: " + err);
					break;
				}

				if (!m_isHmdConnected)
				{
					Debug.Log("HMD was disconnected.");
					break;
                }

                UpdateHmdData();
				PoseUpdate.Invoke(m_sHeadPosition, m_sStandingPosition, m_sHeadRotation);
				EyePositionUpdate.Invoke(m_sLeftEyeOffset, m_sRightEyeOffset);
				EyeProjectionUpdate.Invoke();
				GazeUpdate.Invoke(m_sConvergenceData, m_sEyeVecLeft, m_sEyeVecRight);

				// Don't do any rendering code (below) if the compositor isn't ready
				if (!CompositorReadyCheck())
					continue;

				// On first run and in case any new FoveInterfaces have been created
				if (unregisteredInterfaces.Count > 0) {
					foreach (var reg in unregisteredInterfaces)
						RegisterHelper(reg);
					unregisteredInterfaces.Clear();
				}

				// Render all cameras, one eye at a time
				RenderTexture oldCurrent = RenderTexture.active;
				foreach (var list in m_interfaceStacks)
				{
					int layerId = list.Key;
					var eyeTx = GetEyeTextures(layerId);

					SetPoseForSubmit(layerId, _sLastPose);

					if (eyeTx.areNew)
					{
                        var texPtrLeft = eyeTx.left.GetNativeTexturePtr();
                        var texPtrRight = eyeTx.right.GetNativeTexturePtr();

                        // texture native ptr get valid only after first flush
                        if (texPtrLeft != IntPtr.Zero && texPtrRight != IntPtr.Zero)
                        {
                            SetLeftEyeTexture(layerId, texPtrLeft);
                            SetRightEyeTexture(layerId, texPtrRight);
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
            }

            StartCoroutine(CheckForHeadsetCoroutine());
		}

		private static void UpdateHmdData()
		{
			_sLastPose = GetLastPose();

			m_sHeadPosition = _sLastPose.position.ToVector3() * m_worldScale;
			m_sStandingPosition = _sLastPose.standingPosition.ToVector3() * m_worldScale;
			m_sHeadRotation = _sLastPose.orientation.ToQuaternion();

			{
				GazeVector lGaze, rGaze;
				Fove.GazeConvergenceData conv;

				var errVector = Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
				var errConv = Instance.m_headset.GetGazeConvergence(out conv);

				if (errVector == ErrorCode.None && errConv == ErrorCode.None)
				{
					var convRay = conv.ray.ToRay();
					m_sEyeVecLeft = lGaze.vector.ToVector3();
					m_sEyeVecRight = rGaze.vector.ToVector3();
					m_sConvergenceData.distance = m_worldScale * conv.distance;
					m_sConvergenceData.ray = new Ray(m_worldScale * convRay.origin, convRay.direction);
					m_sPupilDilation = conv.pupilDilation;
					m_sGazeFixated = conv.attention;
				}
			}
		
			Matrix44 lEyeMx, rEyeMx;
			Instance.m_headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
			GetEyeOffsetVector(ref lEyeMx, out m_sLeftEyeOffset);
			GetEyeOffsetVector(ref rEyeMx, out m_sRightEyeOffset);

			m_sEyeClosed = CheckEyesClosed(true);

			if (AddInUpdate != null)
				AddInUpdate();
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
			bool isCompositorReady;
			var err = IsCompositorReady(out isCompositorReady);
			if (err != ErrorCode.None)
			{
				Debug.Log("[FOVE] Error checking compositor state: " + err);
			}
			return isCompositorReady;
		}

		#region Native bindings

		[DllImport("FoveUnityFuncs", EntryPoint = "getSubmitFunctionPtr")]
		private static extern IntPtr GetSubmitFunctionPtr();
		[DllImport("FoveUnityFuncs", EntryPoint = "getWfrpFunctionPtr")]
		private static extern IntPtr GetWfrpFunctionPtr();

		[DllImport("FoveUnityFuncs", EntryPoint = "resetState")]
		private static extern IntPtr ResetNativeState();
		[DllImport("FoveUnityFuncs", EntryPoint = "destroyResources")]
		private static extern IntPtr DestroyNativeResources();

		[DllImport("FoveUnityFuncs", EntryPoint = "isCompositorReady")]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern ErrorCode IsCompositorReady(out bool isReady);
		[DllImport("FoveUnityFuncs", EntryPoint = "getLayerForCreateInfo")]
		private static extern int GetLayerForCreateInfo(CompositorLayerCreateInfo info);
		[DllImport("FoveUnityFuncs", EntryPoint = "getIdealLayerDimensions")]
		private static extern void GetIdealLayerDimensions(int layerId, ref Vec2i dims);

		[DllImport("FoveUnityFuncs", EntryPoint = "setLeftEyeTexture")]
		private static extern void SetLeftEyeTexture(int layerId, IntPtr texPtr);
		[DllImport("FoveUnityFuncs", EntryPoint = "setRightEyeTexture")]
		private static extern void SetRightEyeTexture(int layerId, IntPtr texPtr);
		[DllImport("FoveUnityFuncs", EntryPoint = "setPoseForSubmit")]
		private static extern void SetPoseForSubmit(int layerId, Pose pose);
	
		[DllImport("FoveUnityFuncs", EntryPoint = "getLastPose")]
		private static extern Pose GetLastPose();

		#endregion
	}
}
