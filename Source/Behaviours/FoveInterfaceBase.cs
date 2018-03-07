using Fove.Managed;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace UnityEngine
{
	public abstract class FoveInterfaceBase : MonoBehaviour
	{
		public static class Version
		{
			public const int MAJOR = 2;
			public const int MINOR = 1;
			public const int RELEASE = 2;
		}

		protected static bool ReportErrorCodeIfNotNone(EFVR_ErrorCode code, string funcName)
		{
			if (code == EFVR_ErrorCode.None)
			{
				return true;
			}

			Debug.LogWarning(string.Format("[FOVE] {0} returned error code: {1}", funcName, code));
			return false;
		}

		/**
		 * MonoBehaviour Implementation
		 * 
		 * The pieces required and used by the MonoBehaviour subclass when attached to a GameObject in the
		 * scene. This behaviour does not function unless there is a single instance of this class
		 * instantiated in the scene.
		 */
		[Tooltip("INTERMEDIATE: Override the default distance between the \"eyes\" of the cameras")]
		[SerializeField] protected float interOcularDistance = 0.06f;

		[SerializeField] protected float eyeHeight = 0.0f;
		[SerializeField] protected float eyeForward = -0.04f;
		[SerializeField] protected bool useCustomEyePlacement = false;

		[Tooltip(
			"Multiplier int Unity-units per metre.\n\nFor example, if you use 100 units = 1m, set this to 100\n\nIf you use 1 unit = 1 foot, set this to 0.3048, etc...")]
		[SerializeField] protected float worldScale = 1.0f;

		[Tooltip(
			"ADVANCED: Use this to scale individual axes from the position tracking system. Use at your own risk.")]
		[SerializeField] protected bool useCustomPositionScaling = false;
		[SerializeField] protected Vector3 positionScales = new Vector3(1, 1, 1);

		[SerializeField] protected bool suppressProjectionUpdates = false;

		[Range(0.01f, 2.5f)]
		[SerializeField] protected float oversamplingRatio = 1.0f;

		[Tooltip("Turns off gaze tracking")]
		[SerializeField] protected bool gaze = true;
		[Tooltip("Turns off orientation tracking")]
		[SerializeField] protected bool orientation = true;

		[Tooltip("Turns off position tracking (turns on position tracking when on)")]
		[SerializeField] protected bool position = true;

		[Tooltip(
			"Tell the plugin to skip the automatic calibration check on start; if you use this you should make sure to call EnsureEyeCalibrationCheck() in your code when desired.")]
		[SerializeField] protected bool skipAutoCalibrationCheck = false;

		// Compositor options
		[Tooltip(
			"Check this to disable time warp on images rendered and sent to the compositor. This is useful if you disable orientation to avoid any jitter due to frame latency.")]
		[SerializeField] protected bool disableTimewarp = false;
		[SerializeField] protected bool disableFading = false;
		[SerializeField] protected bool disableDistortion = false;
		[SerializeField] protected EFVR_ClientType layerType = EFVR_ClientType.Base;

		public bool TimewarpDisabled { get { return disableTimewarp; } }
		public bool FadingDisabled { get { return disableFading; } }
		public bool DistortionDisabled { get { return disableDistortion; } }
		public EFVR_ClientType LayerType { get { return layerType; } }

		// Make sure that values can't get... bad...
		void OnValidate()
		{
			interOcularDistance = Mathf.Clamp(interOcularDistance, 0.0f, float.MaxValue);
			worldScale = Mathf.Clamp(worldScale, 0.0f, float.MaxValue);
		}

		//[Tooltip("Customize the near clip plane.")]
		protected float _nearClip = 0.1f;
		protected float _farClip = 1000f;

		protected Ray _eyeRayLeft, _eyeRayRight, _eyeConverge;

		protected FVRCompositor _compositor;
		protected SFVR_CompositorLayer _compositorLayer;

		protected Material _screenBlitMaterial;

		protected bool _isAuthoritative = true;
		protected bool _localDataIsCurrent;

		// Static members
		public static FVRHeadset GetFVRHeadset()
		{
			return _sHeadset;
		}
		
		protected static bool _sIsStaticInitialized;
		protected static bool _sHasUpdatedStaticData;
		protected static bool _sNeedsNewRenderPose;

		protected static FVRHeadset _sHeadset;
		protected static SFVR_Pose _sLastPose;

		protected static bool _sHasBeenCalibratedOnce = false;

		// Locally stored data, prepped for sending to Unity
		protected static Quaternion _sHeadRotation;

		protected static Vector3 _sHeadPosition;
		protected static Vector3 _sEyeVecLeft = Vector3.forward;
		protected static Vector3 _sEyeVecRight = Vector3.forward;
		protected static GazeConvergenceData _sGazeConvergence;
		protected static float _sPupilDilation;
		protected static bool _sAttention;

		protected static float usedIOD;

		private static bool _hasWarnedVsyncOn = false;

		protected static bool _vrEnabled = false;
		protected static bool _shouldTryFoveRender = false;
		protected static bool _isHmdConnected = false;

		/****************************************************************************************************\
		 * GameObject lifecycle methods
		\****************************************************************************************************/
		private void Awake() // called before any Start methods
		{
#if UNITY_2017_2_OR_NEWER
			_vrEnabled = XRSettings.enabled;
			string device = XRSettings.loadedDeviceName;
#else
			_vrEnabled = VRSettings.enabled;
			string device = VRSettings.loadedDeviceName;
#endif

			if (device.Equals("split"))
			{
				// For now, if we are enabled and appear to be the active VR device, assume that we're meant
				// to render and be the authoritative source of pose data for this camera.
				_shouldTryFoveRender = true;
				_isAuthoritative = true;
			}
		}

		/// <summary>
		/// Force the FoveClient to reload its underlying connection to the SDK. This should never be necessary, but if
		/// for some reason you need to change settings and reload the base interface, here is where you would do that.
		/// </summary>
		public void ReloadFoveClient()
		{
			EFVR_ClientCapabilities capabilities = 0;
			if (gaze)
			{
				capabilities |= EFVR_ClientCapabilities.Gaze;
			}
			if (orientation)
			{
				capabilities |= EFVR_ClientCapabilities.Orientation;
			}
			if (position)
			{
				capabilities |= EFVR_ClientCapabilities.Position;
			}

			_sHeadset = new FVRHeadset(capabilities);
			Console.WriteLine("_sHeadset: " + _sHeadset);

			//Check the software versions and spit out an error if the client is newer than the runtime
			var softwareVersionOkay = _sHeadset.CheckSoftwareVersions();
			if (softwareVersionOkay != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("FoveInterface failed software version check");

				if (softwareVersionOkay == EFVR_ErrorCode.Connect_RuntimeVersionTooOld)
				{
					SFVR_Versions versions;
					ReportErrorCodeIfNotNone(_sHeadset.GetSoftwareVersions(out versions), "GetSoftwareVersions");
					string client = "" + versions.clientMajor + "." + versions.clientMinor + "." + versions.clientBuild;
					string service = "" + versions.runtimeMajor + "." + versions.runtimeMinor + "." + versions.runtimeBuild;
					Debug.LogError("Please update your FOVE runtime: (client - " + client + " | runtime - " + service + ")");
				}
			}

			_sIsStaticInitialized = true;
		}

		/// <summary>
		/// Update the various Unity scene components and objects. This is used internally to allow live updating of
		/// various attributes in the inspector, but shouldn't need to be called outside of the plugin. If you do some-
		/// thing unusual and need to update things like the separation and projection matrices for the eyes, you could
		/// theoretically call this method to do so.
		/// </summary>
		public virtual bool RefreshSetup()
		{
			if (QualitySettings.vSyncCount > 0 && !_hasWarnedVsyncOn)
			{
				Debug.LogWarning("FOVE: Vsync is enabled, which can degrade performance.");
				_hasWarnedVsyncOn = true;
			}

			if (!_sIsStaticInitialized || _sHeadset == null)
			{
				// No headset has been created yet, which means that the connect coroutine should still
				// be running and this function will be called again once a connection is established.
				return false;
			}

			// Get the right-eye position so that interocular (x-axis) is positive; otherwise the eyes are placed
			// symmetrically
			SFVR_Matrix44 offsetMatrixLeft, offsetMatrixRight;
			var err = _sHeadset.GetEyeToHeadMatrices(out offsetMatrixLeft, out offsetMatrixRight);
			if (err != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("Couldn't get eye to head matrix...");
			}

			float sdk_iod = offsetMatrixRight.mat[0 + 3 * 4] * 2;
			float sdk_eyeHeight = offsetMatrixRight.mat[1 + 3 * 4];
			float sdk_eyeForward = offsetMatrixRight.mat[2 + 3 * 4];

			if (!useCustomEyePlacement)
			{
				interOcularDistance = sdk_iod;
				eyeHeight = sdk_eyeHeight;
				eyeForward = sdk_eyeForward;
			}
			usedIOD = interOcularDistance;

			return true;
		}

		private void Start()
		{
			StartHelper();
		}

		protected virtual void StartHelper()
		{
			ReloadFoveClient();
			EnsureLocalDataConcurrency();

			// Automatically trigger calibration check at start of applications unless devs check this advanced feature
			if (!skipAutoCalibrationCheck && gaze && !_sHasBeenCalibratedOnce)
			{
				Debug.Log("Starting automatic calibration check.");

				EnsureEyeTrackingCalibration();
			}

			StartCoroutine(CheckForHeadsetCoroutine());
		}

		protected void OnApplicationQuit()
		{
			Debug.Log("Quitting interface.");
		}

		/// <summary>
		/// Connect to the FOVE Compositor system. This will hard reset the compositor connection even if there is
		/// already a valid connection. You could call this after a call to `DisconnectCompositor` if you found any
		/// significant reason to do so.
		/// </summary>
		public virtual bool ConnectCompositor()
		{
			if (_compositor == null)
			{
				SFVR_ClientInfo clientInfo;
				clientInfo.api = EFVR_GraphicsAPI.DirectX;
				switch (SystemInfo.graphicsDeviceType)
				{
					case Rendering.GraphicsDeviceType.Direct3D11:
					case Rendering.GraphicsDeviceType.Direct3D12:
					case Rendering.GraphicsDeviceType.Direct3D9:
						break;
#if UNITY_5_4
					case Rendering.GraphicsDeviceType.OpenGL2:
#endif
					case Rendering.GraphicsDeviceType.OpenGLCore:
					case Rendering.GraphicsDeviceType.OpenGLES2:
					case Rendering.GraphicsDeviceType.OpenGLES3:
						clientInfo.api = EFVR_GraphicsAPI.OpenGL;
						break;
					default:
						Debug.LogError("Unrecognized device type, unable to grant compositor support: " + SystemInfo.graphicsDeviceType);
						break;
				}
				Debug.Log("Detected rendering api: " + clientInfo.api);

				// Create the compositor and submit layer at the same time
				_compositor = new FVRCompositor();
			}

			var createInfo = new SFVR_CompositorLayerCreateInfo
			{
				alphaMode = EFVR_AlphaMode.Auto,
				disableDistortion = disableDistortion,
				disableFading = disableFading,
				disableTimewarp = disableTimewarp,
				type = layerType
			};

			var err = _compositor.CreateLayer(createInfo, out _compositorLayer);
			if (err != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("compositor could not get a layer: " + err);
				return false;
			}

			Debug.Log("FOVE Compositor layer acquired: " + _compositorLayer.layerId);

			return true;
		}

		/// <summary>
		/// Disconnect and destroy the underlying compositor system. This leaves your game in a state where no data is
		/// being sent on to the FOVE compositor. You might call this in situations where you want to disable VR, or if
		/// you know that another program will be trying to take control of the HMD's screen. If you want to reconnect,
		/// you would call `ConnectCompositor`.
		/// </summary>
		public virtual void DisconnectCompositor()
		{
			_compositor.DisconnectImmediately();
			_compositor = null;
		}

		// Most functionality is implemnented in the CheckConcurrency method, which runs the first time
		// each frame that something tries to access FOVE data. This ensures that this object is always
		// updated before other objects which rely on its functioning.
		protected virtual void Update()
		{
			_sNeedsNewRenderPose = true;
		}

		private IEnumerator CheckForHeadsetCoroutine()
		{
			while (true)
			{
				EFVR_ErrorCode err;
				// See if the headset interface has a real HMD attached
				err = _sHeadset.IsHardwareConnected(out _isHmdConnected);
				if (err != EFVR_ErrorCode.None)
				{
					Debug.Log("An error occurred checking for hardware connected: " + err);
				}

				if (_isHmdConnected)
				{
					if (RefreshSetup())
						break;
				}

				// Try again in a half second
				yield return new WaitForSecondsRealtime(0.5f);
			}

			Debug.Log("Connected to FOVE hardware.");
			StartCoroutine(UpdateHeadsetCoroutine());
		}

		// TODO: I think this stuff might be able to be merged in as part of WaitForRenderPose_IfNeeded()...
		private IEnumerator UpdateHeadsetCoroutine()
		{
			while (true)
			{
				yield return null; //is this better timing wise for eye info? we are not syncing it so might be losing stuff...

				var err = _sHeadset.IsHardwareConnected(out _isHmdConnected);
				if (err != EFVR_ErrorCode.None)
				{
					Debug.Log("Error checking hardware connection state: " + err);
					break;
				}

				if (!_isHmdConnected)
				{
					Debug.Log("HMD was disconnected.");
					break;
				}

				_localDataIsCurrent = false;
				_sHasUpdatedStaticData = false;
			}

			Debug.Log("Reconnecting...");
			StartCoroutine(CheckForHeadsetCoroutine());
		}

		protected void WaitForRenderPose_IfNeeded()
		{
			if (_sNeedsNewRenderPose)
			{
				_sLastPose = _compositor.WaitForRenderPose();
				EnsureLocalDataConcurrency();

				_sHasUpdatedStaticData = false;
				_sNeedsNewRenderPose = false;
			}
		}

		/****************************************************************************************************\
		 * Interface Structures
		\****************************************************************************************************/

		/// <summary>
		/// A basic struct which contains two UnityEngine rays, one each for left and right eyes, indicating where the user is gazing in world space.
		/// </summary>
		/// <remarks>Ray objects -- one representing each eye's
		/// gaze direction. Will not be sufficient for people/devices with more than two eyes.
		/// </remarks>
		public struct EyeRays
		{
			/// <summary>
			/// The left eye's gaze ray.
			/// </summary>
			public Ray left;
			/// <summary>
			/// The right eye's gaze ray.
			/// </summary>
			public Ray right;

			public EyeRays(Ray l, Ray r)
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
			/// Contructor to set Gaze Convergence data based on a FOVE native SFVR_Ray instance
			/// </summary>
			/// <param name="ray">A FOVE-native SFVR_Ray structure</param>
			/// <param name="distance">The distance from start, Range: 0 to inf</param>
			/// in the values presented.</param>
			public GazeConvergenceData(SFVR_Ray ray, float distance)
			{
				this.ray = new Ray(new Vector3(ray.origin.x, ray.origin.y, ray.origin.z), new Vector3(ray.direction.x, ray.direction.y, ray.direction.z));
				this.distance = distance;
			}

			/// <summary>
			/// Contructor to set Gaze Convergence data based on a Unity native Ray instance
			/// </summary>
			/// <param name="ray">Unity's Ray structure</param>
			/// <param name="distance">The distance from start, Range: 0 to inf</param>
			/// in the values presented.</param>
			public GazeConvergenceData(Ray ray, float distance)
			{
				this.ray = ray;
				this.distance = distance;
			}

			/// <summary>
			/// A normalized (1 unit long) ray indicating the starting reference point and direction of the user's gaze
			/// </summary>
			public Ray ray;
			/// <summary>
			/// How far out along the normalized ray the user's eyes are converging.
			/// </summary>
			public float distance;
		}

		/****************************************************************************************************\
		 * Interface Methods
		\****************************************************************************************************/
		protected abstract Vector3 GetLeftEyePosition();
		protected abstract Vector3 GetRightEyePosition();

		/// <summary>
		/// Calculate Unity Ray objects based on the origin points of the user's eyes in 3D and the calculated
		/// gaze direction.
		/// </summary>
		/// <param name="leftRay"></param>
		/// <param name="rightRay"></param>
		/// <param name="immediate"></param>
		/// <remarks>Gaze can either be enabled or disabled, and different FoveInterface subclasses could have
		/// different ways that are better for getting origin positions, so we call into abstract methods
		/// for those and let the implementations themselves provide the correct position.</remarks>
		protected void CalculateGazeRays(out Ray leftRay, out Ray rightRay, bool immediate)
		{
			var lPosition = GetLeftEyePosition();// new Vector4(-usedIOD * 0.5f, eyeHeight, eyeForward) * worldScale;
			var rPosition = GetRightEyePosition();// new Vector4(usedIOD * 0.5f, eyeHeight, eyeForward) * worldScale;

			Vector3 lDirection, rDirection;
			if (gaze)
			{
				if (immediate)
				{
					lDirection = GetLeftEyeVector_Immediate();
					rDirection = GetRightEyeVector_Immediate();
				}
				else
				{
					lDirection = _sEyeVecLeft;
					rDirection = _sEyeVecRight;
				}
			}
			else
			{
				lDirection = Vector3.forward;
				rDirection = Vector3.forward;
			}

			leftRay = new Ray(lPosition, transform.TransformDirection(lDirection));
			rightRay = new Ray(rPosition, transform.TransformDirection(rDirection));
		}

		protected void UpdateGazeInternal()
		{
			CalculateGazeRays(out _eyeRayLeft, out _eyeRayRight, false);
		}

		/// <summary>
		/// Ensure that the data we retrieve each frame has been updated in that frame. Sensor data changes
		/// so rapidly that data could change mid-frame, which could cause things to become inconsistent.
		/// We plan on returning predicted data for head rotation and position and eye position anyway, so
		/// updating mid-frame should't really offer any perceivable improvement.
		///	</summary>
		protected void EnsureLocalDataConcurrency()
		{
			if (!enabled)
				return;

			CheckStaticConcurrency();

			if (_localDataIsCurrent)
				return;

			if (_isAuthoritative)
			{
				// position head
				if (position)
				{
					Vector3 posiTemp = _sHeadPosition * worldScale;
					if (useCustomPositionScaling)
					{
						posiTemp.x *= positionScales.x;
						posiTemp.y *= positionScales.y;
						posiTemp.z *= positionScales.z;
					}

					gameObject.transform.localPosition = posiTemp;
				}

				// rotate head
				if (orientation)
				{
					gameObject.transform.localRotation = _sHeadRotation;
				}
			}

			// Gaze should be updated last
			UpdateGazeInternal();

			_localDataIsCurrent = true;
		}

		private bool InternalGazecastHelperSingle(Collider col, out RaycastHit hit)
		{
			bool eyesInvalid = (_eyeRayLeft.origin == _eyeRayLeft.direction || _eyeRayRight.origin == _eyeRayRight.direction);
			bool eyesInsideCollider = (col.bounds.Contains(_eyeRayLeft.origin) || col.bounds.Contains(_eyeRayRight.origin));

			if (eyesInvalid || eyesInsideCollider)
			{
				hit = new RaycastHit();
				return false;
			}

			var localConvergence = GetGazeConvergence();
			if (col.Raycast(localConvergence.ray, out hit, _farClip))
				return true;

			return false;
		}

		private RaycastHit[] InternalGazecastHelper_Multi(int layerMask, QueryTriggerInteraction queryTriggers)
		{
			bool eyesInvalid = (_eyeRayLeft.origin == _eyeRayLeft.direction || _eyeRayRight.origin == _eyeRayRight.direction);

			if (eyesInvalid)
			{
				return null;
			}

			var localConvergence = GetGazeConvergence();
			return Physics.RaycastAll(localConvergence.ray, _farClip, layerMask, queryTriggers);
		}

		/// <summary>
		/// Determine if a specified collider intersects the user's gaze.
		/// </summary>
		/// <param name="col">The collider to check against the user's gaze.</param>
		/// <returns>Whether or not the referenced collider is being looked at.</returns>
		public bool Gazecast(Collider col)
		{
			EnsureLocalDataConcurrency();
			RaycastHit hit;
			return InternalGazecastHelperSingle(col, out hit);
		}

		/// <summary>
		/// Determine if any colliders in the provided collection intersect the user's gaze.
		/// </summary>
		/// <param name="col">The set of colliders to check against the user's gaze.</param>
		/// <returns>Whether or not any of the referenced colliders are being looked at.</returns>
		public bool Gazecast(IEnumerable<Collider> cols)
		{
			EnsureLocalDataConcurrency();
			RaycastHit hit;
			foreach (var c in cols)
			{
				if (InternalGazecastHelperSingle(c, out hit))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determine if any colliders in the provided set intersect the user's gaze, and return
		/// the collider most likely to be the user's gaze focus.</summary>
		/// <param name="col">The set of colliders to check against the user's gaze.</param>
		/// <param name="which">The collider which is most likely intersecting the user's gaze,
		/// or `null` if the user is not looking at any of the colliders.</param>
		/// <returns>Whether or not any of the referenced colliders are being looked at.</returns>
		public bool Gazecast(IEnumerable<Collider> cols, out Collider which)
		{
			EnsureLocalDataConcurrency();
			RaycastHit hit;
			float distance = float.MaxValue;
			which = null;

			foreach (var c in cols)
			{
				if (InternalGazecastHelperSingle(c, out hit))
				{
					if (which == null || hit.distance < distance)
					{
						// For now, we use distance to determine if the user is actually looking at the collider.
						// TODO: We should use the convergence point and determine likelihood by how much the
						// convergence point is in or near the collider rather than simple finding the closest
						// collider along the path.
						which = c;
						distance = hit.distance;
						// Don't shortcut this, as we need to select the most likely of a set,
						// not simply whether a collider in the set is being viewed.
					}
				}
			}

			return which != null;
		}

		/// <summary>
		/// Determine if any colliders flagged with the supplied layer mask intersect the user's gaze, and return
		/// the collider most likely to be the user's gaze focus.</summary>
		/// <param name="layerMask">The mask value used to filter for colliders to check. This value should be the
		/// Unity-specified layer mask the same as you would use for physics racasting.</param>
		/// <param name="which">The collider which is most likely intersecting the user's gaze,
		/// or `null` if the user is not looking at any of the colliders.</param>
		/// <param name="queryTriggers">Optional. You can supply a value from Unity's QueryTriggerInteraction  enum
		/// to indicate how you want Trigger colliders to be treated for this gazecast. (See Unity's documentation
		/// for more information: https://docs.unity3d.com/ScriptReference/QueryTriggerInteraction.html) </param>
		/// <returns>Whether or not any of the colliders with the specified mask are being looked at.</returns>
		public bool Gazecast(int layerMask, out Collider which, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal)
		{
			EnsureLocalDataConcurrency();
			RaycastHit[] hits = InternalGazecastHelper_Multi(layerMask, queryTriggers);

			float distance = float.MaxValue;
			which = null;

			foreach (var hit in hits)
			{
				if (hit.distance < distance)
				{
					distance = hit.distance;
					which = hit.collider;
				}
			}

			return which != null;
		}

		/// <summary>
		/// Get a structure containing Unity Ray objects which describe where in the scene each of the user's eyes are
		/// looking.</summary>
		/// <returns>The set of Unity Ray objects describing the user's eye gaze.</returns>
		/// <remarks>These rays are overwritten each frame, so you should not retain references to them across frames.
		/// </remarks>
		public EyeRays GetGazeRays()
		{
			EnsureLocalDataConcurrency();
			return new EyeRays(_eyeRayLeft, _eyeRayRight);
		}

		/// <summary>
		/// Get a set of Unity Ray objects which describe where in the scene each of the user's eyes are looking right
		/// now.</summary>
		/// <returns>The set of Unity Ray objects describing the user's eye gaze.</returns>
		/// <remarks>Typically gaze data is cached at the start of each frame to yield consistent results for each
		/// frame. This function ignores the cached value and creates a new struct every time, so be careful.</remarks>
		public EyeRays GetGazeRays_Immediate()
		{
			Ray left, right;
			CalculateGazeRays(out left, out right, true);

			return new EyeRays(left, right);
		}

		/// <summary>
		/// Immediately recalibrate the user's current gaze center to be the supplied 3D vector.
		/// </summary>
		/// <param name="vec">The point where the user is believed to be looking in local 3D space.</param>
		/// <remarks>This method should only be called once and only when you have given the user adequate time to make
		/// certain that they are looking exactly at the point you indicate. The point is in HMD-relative coordinate
		/// space, NOT world space, so care should be taken to perform the necessary conversions before calling this
		/// method.</remarks>
		public void ManualDriftCorrection3D(Vector3 vec)
		{
			_sHeadset.ManualDriftCorrection3D(new SFVR_Vec3() { x = vec.x, y = vec.y, z = vec.z });
		}

		/****************************************************************************************************\
		 * Static interface methods
		\****************************************************************************************************/

		//==============================================================
		// IMMEDIATE ACCESS

		/// <summary>
		/// Get the current HMD immediate rotation as a Unity quaternion.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns
		/// <remarks>This value is automatically applied to the interface's GameObject during the main update process
		/// and is only exposed here for reference and out-of-sync access to updated orientation data.</remarks>
		public Quaternion GetHMDRotation_Immediate()
		{
			SFVR_Pose pose;
			_sHeadset.GetHMDPose(out pose);
			var q = pose.orientation;
			return new Quaternion(q.x, q.y, q.z, q.w);
		}

		/// <summary>
		/// Get the current HMD position in local coordinates as a Unity Vector3.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		/// <remarks>This value is automatically applied to the interface's GameObject and is exposed for reference and
		/// out-of-sync access to updated position data.</remarks>
		public Vector3 GetHMDPosition_Immediate()
		{
			SFVR_Pose pose;
			_sHeadset.GetHMDPose(out pose);
			var p = pose.position;
			return new Vector3(p.x, p.y, p.z);
		}

		/// <summary>
		/// Returns the direction the left eye is looking right now.
		/// </summary>
		/// <returns>The gaze direction</returns>
		/// <remarks>The internal values are updated automatically and should be used for primary reference. This value
		/// is exposed fot reference and out-of-sync access to updated gaze data.</remarks>
		public Vector3 GetLeftEyeVector_Immediate()
		{
			SFVR_GazeVector gaze;
			_sHeadset.GetGazeVector(EFVR_Eye.Left, out gaze);
			return new Vector3(gaze.vector.x, gaze.vector.y, gaze.vector.z);
		}

		/// <summary>
		/// Returns the direction the right eye is looking right now.
		/// </summary>
		/// <returns>The gaze direction</returns>
		/// <remarks>The internal values are updated automatically and should be used for primary reference. This value
		/// is exposed fot reference and out-of-sync access to updated gaze data.</remarks>
		public Vector3 GetRightEyeVector_Immediate()
		{
			SFVR_GazeVector gaze;
			_sHeadset.GetGazeVector(EFVR_Eye.Right, out gaze);
			return new Vector3(gaze.vector.x, gaze.vector.y, gaze.vector.z);
		}

		/// <summary>
		/// Query whther or not a headset is physically connected to the computer
		/// </summary>
		/// <returns>Whether or not a headset is present on the machine.</returns>
		public bool IsHardwareConnected()
		{
			bool b;
			_sHeadset.IsHardwareConnected(out b);
			return b;
		}

		/// <summary>
		/// Query whether the headset has all requested features booted up and running (position tracking, eye tracking,
		/// orientation, etc...).
		/// </summary>
		/// <returns>Whether you can expect valid data from all HMD functions.</returns>
		public bool IsHardwareReady()
		{
			bool b;
			_sHeadset.IsHardwareReady(out b);
			return b;
		}

		/// <summary>
		/// Query whether eye tracking has been calibrated and should be usable or not.
		/// </summary>
		/// <returns>Whether eye tracking has been calibrated.</returns>
		public bool IsEyeTrackingCalibrated()
		{
			bool b;
			_sHeadset.IsEyeTrackingCalibrated(out b);
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
		public bool IsEyeTrackingCalibrating()
		{
			bool b;
			_sHeadset.IsEyeTrackingCalibrating(out b);
			return b;
		}

		/// <summary>
		/// Get the current version of the active Fove client library. This returns "[major].[minor].[build]".
		/// </summary>
		/// <returns>A string representing the current client library version.</returns>
		public string GetClientVersion()
		{
			SFVR_Versions versions;
			_sHeadset.GetSoftwareVersions(out versions);
			string result = "" + versions.clientMajor + "." + versions.clientMinor + "." + versions.clientBuild;

			return result;
		}

		/// <summary>
		/// Get the current version of the installed runtime service. This returns "[major].[minor].[build]".
		/// </summary>
		/// <returns>A string version of the current runtime version.</returns>
		public string GetRuntimeVersion()
		{
			SFVR_Versions versions;
			_sHeadset.GetSoftwareVersions(out versions);
			string result = "" + versions.runtimeMajor + "." + versions.runtimeMinor + "." + versions.runtimeBuild;

			return result;
		}

		/// <summary>
		/// Check whether the runtime and client versions are compatible and should be expected to work correctly.
		/// </summary>
		/// <param name="error">A human-readable representation of the error thrown, or "None".</param>
		/// <returns>Whether or not the interface should work given the current versions.</returns>
		/// <remarks>Newer runtime versions are designed to be compatible with older client versions, however new
		/// client versions are not designed to be compatible with old runtime versions.</remarks>
		public bool CheckSoftwareVersions(out string error)
		{
			var err = _sHeadset.CheckSoftwareVersions();

			switch (err)
			{
				case EFVR_ErrorCode.None:
					break;
				case EFVR_ErrorCode.Connect_ClientVersionTooOld:
					Debug.LogWarning("Plugin client version is too old; please seek a newer plugin package");
					break;
				case EFVR_ErrorCode.Connect_RuntimeVersionTooOld:
					Debug.LogWarning("Fove runtime version is too old; please update your runtime.");
					break;
				case EFVR_ErrorCode.Server_General:
					Debug.LogError("An unhandled exception was thrown by Fove CheckSoftwareVersions");
					break;
				default:
					Debug.LogError("An unknown error was returned by Fove CheckSoftwareVersions");
					break;
			}

			error = err.ToString();
			return (err == EFVR_ErrorCode.None);
		}

		/// <summary>
		/// Call this function to initiate an eye tracking calibration check. After calling this function
		/// you should assume that the user cannot see your game until FoveInterface.IsEyeTrackingCalibrating
		/// returns false.
		/// </summary>
		/// <returns>Whether the user has completed validating their calibration.</returns>
		public bool EnsureEyeTrackingCalibration()
		{
			if (!gaze)
			{
				Debug.Log("[FOVE] EnsureEyeTrackingCalibration was called, but eye tracking is set to be disabled. Skipping.");
				return true;
			}

			var err = _sHeadset.EnsureEyeTrackingCalibration();
			_sHasBeenCalibratedOnce = true;

			switch (err)
			{
				case EFVR_ErrorCode.None:
					break;
				default:
					Debug.LogError("An unknown error was returned by Fove EnsureEyeTrackingCalibration: " + err.ToString());
					break;
			}

			//error = err.ToString();
			return err == EFVR_ErrorCode.None;
		}

		/// <summary>
		/// Reset headset orientation.
		/// 
		/// <para>This sets the HMD's current rotation as a "zero" orientation, essentially resetting their
		/// orientation to that set in the editor.</para>
		/// </summary>
		public void TareOrientation()
		{
			var err =_sHeadset.TareOrientationSensor();
			if (err != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("TareOrientation returned an error: " + err);
			}
		}

		/// <summary>
		/// Reset headset position.
		/// </summary>
		/// <remarks>This sets the HMD's current position relative to the tracking camera as a "zero" position,
		/// essentially jumping the headset back to the interface's origin.</remarks>
		public void TarePosition()
		{
			var err = _sHeadset.TarePositionSensors();
			if (err != EFVR_ErrorCode.None)
			{
				Debug.LogWarning("TarePosition returned an error: " + err);
			}
		}

		/// <summary>
		/// Checks which eyes are closed.
		/// </summary>
		/// <returns>EFVR_Eye of what is closed</returns>
		public EFVR_Eye CheckEyesClosed()
		{
			EFVR_Eye result;
			_sHeadset.CheckEyesClosed(out result);
			return result;
		}

		//==============================================================
		// RENDER-SYNCED ACCESS
		private void CheckStaticConcurrency()
		{
			// Skip this function if it's already been run this frame
			if (_sHasUpdatedStaticData)
			{
				return;
			}

			if (_isHmdConnected)
			{
				_sHeadPosition = new Vector3(_sLastPose.position.x, _sLastPose.position.y, _sLastPose.position.z);
				_sHeadRotation = new Quaternion(_sLastPose.orientation.x, _sLastPose.orientation.y, _sLastPose.orientation.z,
					_sLastPose.orientation.w);

				SFVR_GazeVector lGaze, rGaze;

				var errLeft = _sHeadset.GetGazeVector(EFVR_Eye.Left, out lGaze);
				var errRight = _sHeadset.GetGazeVector(EFVR_Eye.Right, out rGaze);
				if (errLeft == EFVR_ErrorCode.None && errRight == EFVR_ErrorCode.None)
				{
					_sEyeVecLeft = new Vector3(lGaze.vector.x, lGaze.vector.y, lGaze.vector.z);
					_sEyeVecRight = new Vector3(rGaze.vector.x, rGaze.vector.y, rGaze.vector.z);
				}
			}

			SFVR_GazeConvergenceData conv;
			var convErr = _sHeadset.GetGazeConvergence(out conv);

			_sGazeConvergence = new GazeConvergenceData(conv.ray, conv.distance);
			_sPupilDilation = conv.pupilDilation;
			_sAttention = conv.attention;

			_sHasUpdatedStaticData = true;
		}

		/// <summary>
		/// Get the HMD rotation for this frame as a Unity quaternion. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns>
		public Quaternion GetHMDRotation()
		{
			CheckStaticConcurrency();
			return _sHeadRotation;
		}

		/// <summary>
		/// Get the HMD position for this frame in local coordinates as a Unity Vector3. This value is
		/// automatically applied to the interface's GameObject and is exposed for reference.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		public Vector3 GetHMDPosition()
		{
			CheckStaticConcurrency();
			return _sHeadPosition;
		}

		/// <summary>
		/// Returns the direction the left eye is looking this frame.
		/// </summary>
		/// <returns>The gaze direction</returns>
		public Vector3 GetLeftEyeVector()
		{
			CheckStaticConcurrency();
			return _sEyeVecLeft;
		}

		/// <summary>
		/// Returns the direction the right eye is looking this frame.
		/// </summary>
		/// <returns>The gaze direction</returns>
		public Vector3 GetRightEyeVector()
		{
			CheckStaticConcurrency();
			return _sEyeVecRight;
		}

		/// <summary>
		/// Returns the data that describes the convergence point of the eyes this frame. See the description of the
		/// `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in HMD-relative space.</returns>
		public GazeConvergenceData GetGazeConvergence_Raw()
		{
			CheckStaticConcurrency();

			return new GazeConvergenceData(_sGazeConvergence.ray, _sGazeConvergence.distance);
		}

		/// <summary>
		/// Returns the data that describes the convergence point of the eyes this frame. See the description of the
		/// `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in world space.</returns>
		public GazeConvergenceData GetGazeConvergence()
		{
			CheckStaticConcurrency();

			Vector3 origin = _sGazeConvergence.ray.origin + transform.position;
			Vector3 direction = transform.TransformDirection(_sGazeConvergence.ray.direction);
			Ray worldRay = new Ray(origin, direction);

			return new GazeConvergenceData(worldRay, _sGazeConvergence.distance);
		}

		/// <summary>
		/// Returns how big the user's pupils are, generally. This is as a ratio compared to when they calibrated. So if
		/// the user's pupils are bigger than during calibration this will be greater than 1. If smaller, then it will be
		/// between 0 and 1. Hard limits are 0 and Infinity (though neither is likely).
		/// </summary>
		/// <returns>A value indicating the user's pupil size.</returns>
		public float GetPupilDilation()
		{
			CheckStaticConcurrency();

			return _sPupilDilation;
		}

		/// <summary>
		/// Returns whether or not the user is focusing on what they're looking at. This is generally an assessment of
		/// whether their eyes are moving around rapidly or focusing generally on something that seems consistent.
		/// </summary>
		/// <returns>Whether or not the user appears to be visually focused on something.</returns>
		public bool GetGazeAttention()
		{
			CheckStaticConcurrency();

			return _sAttention;
		}

		/// <summary>
		/// Returns the complete last pose gathered for the current frame.
		/// </summary>
		/// <returns></returns>
		public SFVR_Pose GetLastPose()
		{
			CheckStaticConcurrency();
			return _sLastPose;
		}
	}
}
