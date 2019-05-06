using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine.Events;
using UnityEngine;

using UnityRay = UnityEngine.Ray;
using UnityQuaternion = UnityEngine.Quaternion;

namespace Fove.Unity
{
	/****************************************************************************************************\
		* Support structs/classes
	\****************************************************************************************************/
	public static class FovePluginVersion
	{
		public const int MAJOR = 3;
		public const int MINOR = 0;
		public const int RELEASE = 0;
	}

	/// <summary>
	/// A basic struct which contains two UnityEngine rays, one each for left and right eyes, indicating
	/// where the user is gazing in world space.
	/// </summary>
	/// <remarks>Ray objects -- one representing each eye's
	/// gaze direction. Will not be sufficient for people/devices with more than two eyes.
	/// </remarks>
	public struct EyeRays
	{
		/// <summary>
		/// The left eye's gaze ray.
		/// </summary>
		public UnityRay left;
		/// <summary>
		/// The right eye's gaze ray.
		/// </summary>
		public UnityRay right;

		public EyeRays(UnityRay l, UnityRay r)
		{
			left = l;
			right = r;
		}
	}

	/// <summary>
	/// Struct representing the vector pointing where the user is looking.
	/// </summary>
	/// <remarks>
	/// The vector (from the center of the player's head in world space) that can be used to approximate the point
	/// that the user is looking at.</remarks>
	public struct GazeConvergenceData
	{
		/// <summary>
		/// Contructor to set Gaze Convergence data based on a FOVE native Ray instance
		/// </summary>
		/// <param name="ray">A FOVE-native Ray structure</param>
		/// <param name="distance">The distance from start, Range: 0 to inf</param>
		/// in the values presented.</param>
		public GazeConvergenceData(Ray ray, float distance)
		{
			this.ray = new UnityRay(new Vector3(ray.origin.x, ray.origin.y, ray.origin.z), new Vector3(ray.direction.x, ray.direction.y, ray.direction.z));
			this.distance = distance;
		}

		/// <summary>
		/// Contructor to set Gaze Convergence data based on a Unity native Ray instance
		/// </summary>
		/// <param name="ray">Unity's Ray structure</param>
		/// <param name="distance">The distance from start, Range: 0 to inf</param>
		/// in the values presented.</param>
		public GazeConvergenceData(UnityRay ray, float distance)
		{
			this.ray = ray;
			this.distance = distance;
		}

		/// <summary>
		/// A normalized (1 unit long) ray indicating the starting reference point and direction of the user's gaze
		/// </summary>
		public UnityRay ray;
		/// <summary>
		/// How far out along the normalized ray the user's eyes are converging.
		/// </summary>
		public float distance;
	}

	/****************************************************************************************************\
	 * FoveManager Class
	\****************************************************************************************************/
	// FoveManager needs to reference a gameObject in order to create coroutines.
	public class FoveManager : MonoBehaviour
	{
		// Static members
		private static FoveManager m_sInstance;
		public static FoveManager Instance
		{
			get
			{
				if (m_sInstance == null)
				{
					m_sInstance = GameObject.FindObjectOfType<FoveManager>();
					if (m_sInstance == null)
						m_sInstance = new GameObject("FOVE Manager (dynamic)").AddComponent<FoveManager>();

					m_worldScale = FoveSettings.WorldScale;
					m_renderScale = FoveSettings.RenderScale;

					m_submitNativeFunc = GetSubmitFunctionPtr();
					m_wfrpNativeFunc = GetWfrpFunctionPtr();
					m_resetNativeFunc = GetResetFunctionPtr();
				}

				return m_sInstance;
			}
		}

		public static Headset GetFVRHeadset()
		{
			return Instance.m_headset;
		}

		// Initialization and state
		private Headset m_headset;
		public static Headset Headset { get { return Instance.m_headset; } }
		private static Pose _sLastPose;
		public static Pose LastSFVRPose { get { return _sLastPose; } }

		// Data update events
		public class PoseUpdateEvent : UnityEvent<Vector3, Vector3, UnityQuaternion> { }
		private static PoseUpdateEvent _sPoseEvt;
		public static PoseUpdateEvent PoseUpdate
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
		public class EyeProjectionEvent : UnityEvent { }
		private static EyeProjectionEvent _sEyeProjectionEvt;
		public static EyeProjectionEvent EyeProjectionUpdate
		{
			get
			{
				if (_sEyeProjectionEvt == null)
					_sEyeProjectionEvt = new EyeProjectionEvent();
				return _sEyeProjectionEvt;
			}
		}

		public class EyePositionEvent : UnityEvent<Vector3, Vector3> { }
		private static EyePositionEvent _sEyePositionEvt;
		public static EyePositionEvent EyePositionUpdate
		{
			get
			{
				if (_sEyePositionEvt == null)
					_sEyePositionEvt = new EyePositionEvent();
				return _sEyePositionEvt;
			}
		}

		public class GazeEvent : UnityEvent<GazeConvergenceData, Vector3, Vector3> { }
		private static GazeEvent _sGazeEvt;
		public static GazeEvent GazeUpdate
		{
			get
			{
				if (_sGazeEvt == null)
					_sGazeEvt = new GazeEvent();
				return _sGazeEvt;
			}
		}
	
		public delegate void AcceptAddInCallback(Headset headset);
		public static void RegisterAddIn(AcceptAddInCallback callback)
		{
			callback(m_sInstance.m_headset);
		}

		public delegate void AddInDelegate();
		public static event AddInDelegate AddInUpdate;

		// Static caches
		private static UnityQuaternion m_sHeadRotation;
		private static Vector3 m_sHeadPosition;
		private static Vector3 m_sStandingPosition;

		private static Vector3 m_sLeftEyeOffset;
		private static Vector3 m_sRightEyeOffset;

		// Values users may ask for
		private static Vector3 m_sEyeVecLeft = Vector3.forward;
		private static Vector3 m_sEyeVecRight = Vector3.forward;
		private static GazeConvergenceData m_sConvergenceData = new GazeConvergenceData(new UnityRay(Vector3.zero, Vector3.forward), Mathf.Infinity);
		private static float m_sPupilDilation = 1.0f;
		private static bool m_sGazeFixated = false;
	
		private static bool m_isHmdConnected = false;

		// Settings cache for runtime
		private static float m_worldScale = 1.0f;
		private static float m_renderScale = 1.0f;
	
		// Rendering/submission native pointers
		private static IntPtr m_submitNativeFunc;
		private static IntPtr m_wfrpNativeFunc;
		private static IntPtr m_resetNativeFunc;

        private Material m_screenBlitMaterial;

        private class EyeTextures
		{
			public RenderTexture left;
			public RenderTexture right;
			public bool areNew;
		}
		private static Dictionary<int, EyeTextures> m_eyeTextures;

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

		// Manage static FoveInterfaces list for initialization and rendering; adding and removing
		private static Dictionary<int, FoveInterface> m_topInterfaces;
		private static Dictionary<int, List<FoveInterface>> m_interfaceStacks;

		private static void EnsureInterfaceDictKey(int layerId)
		{
			if (m_interfaceStacks == null)
				m_interfaceStacks = new Dictionary<int, List<FoveInterface>>();

			if (!m_interfaceStacks.ContainsKey(layerId))
				m_interfaceStacks.Add(layerId, new List<FoveInterface>());
		}

		private struct RegistrantHolder
		{
			public CompositorLayerCreateInfo info;
			public FoveInterface xface;
		}
		private static List<RegistrantHolder> unregisteredInterfaces = new List<RegistrantHolder>();
		public static void RegisterInterface(CompositorLayerCreateInfo info, FoveInterface xface)
		{
			if (Instance == null) // query forces it to exist
				return;
			
			unregisteredInterfaces.Add(new RegistrantHolder{info = info, xface = xface});
		}

		private static void RegisterHelper(RegistrantHolder reg)
		{
			var layerId = GetLayerForCreateInfo(reg.info);

			EnsureInterfaceDictKey(layerId);

			var theStack = m_interfaceStacks[layerId];
			if (theStack.Contains(reg.xface))
				return;

			int idx = theStack.Count;
			for (int i = 0; i < theStack.Count; ++i)
			{
				if (theStack[i].Camera.depth > reg.xface.Camera.depth)
				{
					idx = i;
					break;
				}
			}

			theStack.Insert(idx, reg.xface);

			if (m_topInterfaces == null)
				m_topInterfaces = new Dictionary<int, FoveInterface>();
			m_topInterfaces[layerId] = theStack[theStack.Count - 1];
		}

		public static void UnregisterInterface(FoveInterface xface)
		{
			if (m_interfaceStacks == null)
				return;

			int layerId = -1;
			List<FoveInterface> theStack = null;
			foreach (var list in m_interfaceStacks) {
				if (list.Value.Contains(xface))
				{
					layerId = list.Key;
					theStack = list.Value;
					theStack.Remove(xface);
					return;
				}
			}

			if (layerId > -1) {
				if (m_topInterfaces == null)
					m_topInterfaces = new Dictionary<int, FoveInterface>();
				m_topInterfaces[layerId] = theStack[theStack.Count - 1];
			}
		}

		public static bool IsLast(int layerId, FoveInterface xface)
		{
			return m_topInterfaces[layerId] == xface;
		}

		#region MonoBehaviour/Instance Methods
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

		public float WorldScale
		{
			get { return m_worldScale; }
			set { m_worldScale = value; }
		}

		public float RenderScale
		{
			get { return m_renderScale; }
			set
			{
				if (value > 0)
					m_renderScale = value;
			}
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

			StartCoroutine(CheckForHeadsetCoroutine());
		}

		private void OnApplicationQuit()
		{
			GL.IssuePluginEvent(m_resetNativeFunc, 0);
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
				{
					continue;
				}

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

					foreach (var xface in list.Value)
					{
						bool isLast = IsLast(layerId, xface);

						xface.RenderEye(Eye.Left, eyeTx.left);
						xface.RenderEye(Eye.Right, eyeTx.right);

						if (isLast)
						{
                            GL.Flush();
                            GL.IssuePluginEvent(m_submitNativeFunc, layerId);

                            // this code works only because we only have one single layer (base) allowed for the moment
                            // TODO: Adapt this code as soon as we allow several layers
                            RenderTexture.active = oldCurrent;
                            m_screenBlitMaterial.SetTexture("_Tex1", eyeTx.left);
                            m_screenBlitMaterial.SetTexture("_Tex2", eyeTx.right);
                            Graphics.Blit(null, m_screenBlitMaterial);
                        }
                    }
                }
                GL.Flush();

                // Wait for render pose
                GL.IssuePluginEvent(m_wfrpNativeFunc, 0);
            }

            StartCoroutine(CheckForHeadsetCoroutine());
		}
		#endregion

		/// <summary>
		/// Get the current HMD immediate rotation as a Unity quaternion.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns
		/// <remarks>This value is automatically applied to the interface's GameObject during the main update process
		/// and is only exposed here for reference and out-of-sync access to updated orientation data.</remarks>
		public static UnityQuaternion GetHMDRotation_Immediate()
		{
			Pose pose;
			Instance.m_headset.GetLatestPose(out pose);
			var q = pose.orientation;
			return new UnityQuaternion(q.x, q.y, q.z, q.w);
		}

		/// <summary>
		/// Get the current HMD position in local coordinates as a Unity Vector3.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		/// <remarks>This value is automatically applied to the interface's GameObject and is exposed for reference and
		/// out-of-sync access to updated position data.</remarks>
		public static Vector3 GetHMDPosition_Immediate()
		{
			Pose pose;
			Instance.m_headset.GetLatestPose(out pose);
			var p = pose.position;
			return new Vector3(p.x, p.y, p.z);
		}

		/// <summary>
		/// Returns the direction the left eye is looking right now.
		/// </summary>
		/// <returns>The gaze direction</returns>
		/// <remarks>The internal values are updated automatically and should be used for primary reference. This value
		/// is exposed fot reference and out-of-sync access to updated gaze data.</remarks>
		public static Vector3 GetLeftEyeVector_Immediate()
		{
			GazeVector lGaze, rGaze;
			Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
			return new Vector3(lGaze.vector.x, lGaze.vector.y, lGaze.vector.z);
		}

		/// <summary>
		/// Returns the direction the right eye is looking right now.
		/// </summary>
		/// <returns>The gaze direction</returns>
		/// <remarks>The internal values are updated automatically and should be used for primary reference. This value
		/// is exposed fot reference and out-of-sync access to updated gaze data.</remarks>
		public static Vector3 GetRightEyeVector_Immediate()
        {
            GazeVector lGaze, rGaze;
            Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
            return new Vector3(rGaze.vector.x, rGaze.vector.y, rGaze.vector.z);
		}

		/// <summary>
		/// Query whther or not a headset is physically connected to the computer
		/// </summary>
		/// <returns>Whether or not a headset is present on the machine.</returns>
		public static bool IsHardwareConnected()
		{
			bool b;
			Instance.m_headset.IsHardwareConnected(out b);
			return b;
		}

		/// <summary>
		/// Query whether the headset has all requested features booted up and running (position tracking, eye tracking,
		/// orientation, etc...).
		/// </summary>
		/// <returns>Whether you can expect valid data from all HMD functions.</returns>
		public static bool IsHardwareReady()
		{
			bool b;
			Instance.m_headset.IsHardwareReady(out b);
			return b;
		}

		/// <summary>
		/// Query whether eye tracking has been calibrated and should be usable or not.
		/// </summary>
		/// <returns>Whether eye tracking has been calibrated.</returns>
		public static bool IsEyeTrackingCalibrated()
		{
			bool b;
			Instance.m_headset.IsEyeTrackingCalibrated(out b);
			return b;
		}

		/// <summary>
		/// Query whether eye tracking is currently calibrating, meaning that the user likely
		/// cannot see your game due to the calibration process. While this is true, you should
		/// refrain from showing any interactions that would respond to eye gaze.
		/// </summary>
		/// <remarks>
		/// Calibration will only occur at specific points during your program, however, unless
		/// manually triggered by the user externally. Those points are:
		/// 1) If you do not set the 'skipAutoCalibrationCheck' flag, it will trigger calibration
		/// the first time any FoveInterface object is created in a scene.
		/// 2) Whenever you call `EnsureEyeTrackingCalibration`.
		/// </remarks>
		/// <returns>Whether eye tracking is calibrating.</returns>
		public static bool IsEyeTrackingCalibrating()
		{
			bool b;
			Instance.m_headset.IsEyeTrackingCalibrating(out b);
			return b;
		}

		/// <summary>
		/// Get the current version of the active Fove client library. This returns "[major].[minor].[build]".
		/// </summary>
		/// <returns>A string representing the current client library version.</returns>
		public static string GetClientVersion()
		{
			Versions versions;
			Instance.m_headset.GetSoftwareVersions(out versions);
			string result = "" + versions.clientMajor + "." + versions.clientMinor + "." + versions.clientBuild;

			return result;
		}

		/// <summary>
		/// Get the current version of the installed runtime service. This returns "[major].[minor].[build]".
		/// </summary>
		/// <returns>A string version of the current runtime version.</returns>
		public static string GetRuntimeVersion()
		{
			Versions versions;
			Instance.m_headset.GetSoftwareVersions(out versions);
			string result = "" + versions.runtimeMajor + "." + versions.runtimeMinor + "." + versions.runtimeBuild;

			return result;
		}

		/// <summary>
		/// Check whether the runtime and client versions are compatible and should be expected to work correctly.
		/// </summary>
		/// <returns>Any errors that occured when checking the runtime and client versions, or None if 
		/// everything seems fine.</returns>
		/// <remarks>Newer runtime versions are designed to be compatible with older client versions, however new
		/// client versions are not designed to be compatible with old runtime versions.</remarks>
		public static ErrorCode CheckSoftwareVersions()
		{
			return Instance.m_headset.CheckSoftwareVersions();
		}

		/// <summary>
		/// Call this function to initiate an eye tracking calibration check. After calling this function
		/// you should assume that the user cannot see your game until FoveInterface.IsEyeTrackingCalibrating
		/// returns false.
		/// </summary>
		/// <returns>Whether the user has completed validating their calibration.</returns>
		public static bool EnsureEyeTrackingCalibration()
		{
			var err = Instance.m_headset.EnsureEyeTrackingCalibration();

			switch (err)
			{
				case ErrorCode.None:
					break;
				default:
					Debug.LogError("An unknown error was returned by Fove EnsureEyeTrackingCalibration: " + err.ToString());
					break;
			}

			//error = err.ToString();
			return err == ErrorCode.None;
		}

		/// <summary>
		/// Check which eyes are closed. Returns Left/Right/Both/None accordingly.
		/// </summary>
		/// <returns>Eye of what is closed</returns>
		public static Eye CheckEyesClosed()
		{
            Eye result;
			Instance.m_headset.CheckEyesClosed(out result);
			return result;
		}

		/// <summary>
		/// Returns the pupil dilation value as a ratio relative to a baseline. 1 means average. Range: 0 to Infinity
		/// </summary>
		/// <returns>The average pupil dilation value of both eyes</returns>
		public static float GetPupilDilation()
		{
			return m_sPupilDilation;
		}

		/// <summary>
		/// Returns when the user is looking at something (fixation or pursuit), rather than saccading between objects.
		/// This could be used to suppress eye input during large eye motions.
		/// </summary>
		/// <returns>Whether the uers's gaze is fixated on something, as opposed to saccading.</returns>
		public static bool IsGazeFixated()
		{
			return m_sGazeFixated;
		}

		/// <summary>
		/// Get the HMD local rotation for this frame as a Unity quaternion. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns>
		public static UnityQuaternion GetLocalHMDRotation()
		{
			return m_sHeadRotation;
		}

		/// <summary>
		/// Get the HMD local position for this frame in local coordinates as a Unity Vector3. This value is
		/// automatically applied to the interface's GameObject and is exposed for reference.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view cameras inside Unity.</returns>
		public static Vector3 GetLocalHMDPosition()
		{
			return m_sHeadPosition;
		}

		/// <summary>
		/// Returns the HMD local direction the left eye is looking this frame.
		/// </summary>
		/// <returns>The gaze direction</returns>
		public static Vector3 GetLocalLeftEyeVector()
		{
			return m_sEyeVecLeft;
		}

		/// <summary>
		/// Returns the HMD local direction the right eye is looking this frame.
		/// </summary>
		/// <returns>The gaze direction</returns>
		public static Vector3 GetLocalRightEyeVector()
		{
			return m_sEyeVecRight;
		}

		/// <summary>
		/// Returns the data that describes the HMD-relative convergence point of the eyes this frame.
		/// See the description of the `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in HMD-relative space.</returns>
		public static GazeConvergenceData GetLocalGazeConvergence()
		{
			return m_sConvergenceData;
		}

		/// <summary>
		/// Returns the data that describes the HMD-relative convergence point of the eyes right now.
		/// See the description of the `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in HMD-relative space.</returns>
		public static GazeConvergenceData GetLocalGazeConvergence_Immediate()
		{
			Fove.GazeConvergenceData convergence;
			Instance.m_headset.GetGazeConvergence(out convergence);
			return new GazeConvergenceData(convergence.ray, convergence.distance);
		}

		/// <summary>
		/// Reset headset orientation.
		/// 
		/// <para>This sets the HMD's current rotation as a "zero" orientation, essentially resetting their
		/// orientation to that set in the editor.</para>
		/// </summary>
		public static void TareOrientation()
		{
			var err = Instance.m_headset.TareOrientationSensor();
			if (err != ErrorCode.None)
			{
				Debug.LogWarning("TareOrientation returned an error: " + err);
			}
		}

		/// <summary>
		/// Reset headset position.
		/// </summary>
		/// <remarks>This sets the HMD's current position relative to the tracking camera as a "zero" position,
		/// essentially jumping the headset back to the interface's origin.</remarks>
		public static void TarePosition()
		{
			var err = Instance.m_headset.TarePositionSensors();
			if (err != ErrorCode.None)
			{
				Debug.LogWarning("TarePosition returned an error: " + err);
			}
		}

		/// <summary>
		/// Get projection matrices for left and right eyes given a near and far clipping plane value. This can change
		/// from frame to frame, so it's good to query this before rendering VR cameras.
		/// 
		/// This is called automatically by each FoveInterface before rendering is done for that frame, and so
		/// you shouldn't need to call this method directly
		/// </summary>
		/// <param name="near">Distance to the near-clip plane of the projection frustum</param>
		/// <param name="far">Distance to the far-clip plane of the projection frustum</param>
		/// <param name="left">The matrix to write out the left eye's projection</param>
		/// <param name="right">The matrix to write out the right eye's projection</param>
		public static void GetProjectionMatrices(float near, float far, ref Matrix4x4 left, ref Matrix4x4 right)
		{
			Matrix44 fv_l, fv_r;
			var err = Instance.m_headset.GetProjectionMatricesRH(near, far, out fv_l, out fv_r);
			if (err != ErrorCode.None)
			{
				Debug.Log("Error on GetProjectionMatricesRH: " + err);
				return;
			}

			left = Utils.GetUnityMx(fv_l);
			right = Utils.GetUnityMx(fv_r);
		}

		private static void UpdateHmdData()
		{
			_sLastPose = GetLastPose();

			m_sHeadPosition = Utils.GetUnityVector(_sLastPose.position) * m_worldScale;
			m_sStandingPosition = Utils.GetUnityVector(_sLastPose.standingPosition) * m_worldScale;
			m_sHeadRotation = new UnityQuaternion(_sLastPose.orientation.x, _sLastPose.orientation.y, _sLastPose.orientation.z,
				_sLastPose.orientation.w);

			{
				GazeVector lGaze, rGaze;
				Fove.GazeConvergenceData conv;

				var errVector = Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
				var errConv = Instance.m_headset.GetGazeConvergence(out conv);

				if (errVector == ErrorCode.None && errConv == ErrorCode.None)
				{
					m_sEyeVecLeft = new Vector3(lGaze.vector.x, lGaze.vector.y, lGaze.vector.z);
					m_sEyeVecRight = new Vector3(rGaze.vector.x, rGaze.vector.y, rGaze.vector.z);
					m_sConvergenceData = new GazeConvergenceData(conv.ray, conv.distance);
					m_sPupilDilation = conv.pupilDilation;
					m_sGazeFixated = conv.attention;
				}
			}
		
			Matrix44 lEyeMx, rEyeMx;
			Instance.m_headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
			{
				float lIOD = lEyeMx.m03;
				float lEyeHeight = lEyeMx.m13;
				float lEyeForward = lEyeMx.m23;
				m_sLeftEyeOffset = new Vector3(lIOD, lEyeHeight, lEyeForward) * m_worldScale;
			}
			{
				float rIOD = rEyeMx.m03;
				float rEyeHeight = rEyeMx.m13;
				float rEyeForward = rEyeMx.m23;
				m_sRightEyeOffset = new Vector3(rIOD, rEyeHeight, rEyeForward) * m_worldScale;
			}

			if (AddInUpdate != null)
				AddInUpdate();
		}

		#region Native bindings
		[DllImport("FoveUnityFuncs", EntryPoint = "getSubmitFunctionPtr")]
		private static extern IntPtr GetSubmitFunctionPtr();
		[DllImport("FoveUnityFuncs", EntryPoint = "getWfrpFunctionPtr")]
		private static extern IntPtr GetWfrpFunctionPtr();
		[DllImport("FoveUnityFuncs", EntryPoint = "getResetFunctionPtr")]
		private static extern IntPtr GetResetFunctionPtr();
		
		[DllImport("FoveUnityFuncs", EntryPoint = "setLeftEyeTexture")]
		private static extern void SetLeftEyeTexture(int layerId, IntPtr texPtr);
		[DllImport("FoveUnityFuncs", EntryPoint = "setRightEyeTexture")]
		private static extern void SetRightEyeTexture(int layerId, IntPtr texPtr);
		[DllImport("FoveUnityFuncs", EntryPoint = "setPoseForSubmit")]
		private static extern void SetPoseForSubmit(int layerId, Pose pose);
	
		[DllImport("FoveUnityFuncs", EntryPoint = "getLastPose")]
		private static extern Pose GetLastPose();
		[DllImport("FoveUnityFuncs", EntryPoint = "isCompositorReady")]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern ErrorCode IsCompositorReady(out bool isReady);
		
		[DllImport("FoveUnityFuncs", EntryPoint = "getLayerForCreateInfo")]
		private static extern int GetLayerForCreateInfo(CompositorLayerCreateInfo info);
		[DllImport("FoveUnityFuncs", EntryPoint = "getIdealLayerDimensions")]
		private static extern void GetIdealLayerDimensions(int layerId, ref Vec2i dims);
		#endregion

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
	}
}
