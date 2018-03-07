using Fove.Managed;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
	/// <summary>
	/// A controller interface to the FOVE SDK. One (and only one) should be attached to an object in each scene.
	/// ---
	/// <para>This class doubles as a static class to get the state of the headset (where the user is looking, head
	/// orientation and position, etc...), and also as a component controller for a game object.</para>
	/// 
	/// <para>As a GameObject component, this class constructs stereo eye cameras in the scene in its Awake method.
	/// In the absence of a template camera, it will construct suitable cameras on its own. However a template
	/// camera can be supplied if you want to be able to have control of what other behaviours may be on the created
	/// cameras. For instance, if you have a set of image filters, AA, bloom, depth of field and so on, you could
	/// create that camera as a prefab and then attach it as the reference camera. You may also use a camera that
	/// exists in the scene, in which case the camera will be duplicated and adjusted for each eye (and the original
	/// will be disabled).</para>
	/// 
	/// <para>You can completely override both cameras by creating them in the scene and setting this object's left and
	/// right camera properties directly, however this will override all automatic hierarchy management on thie
	/// object, so make sure that if you want those cameras to rotate with the headset that they are positioned as
	/// children of this component's GameObject in the scene hierarchy.</para>
	/// 
	/// <para>As a static interface, this class offers access to a variety of helper functions for getting Unity-compatible
	/// information such as the headset's orientation and position, as well as eye gaze and convenience functions for
	/// determining if the user is looking at a given collider.</para>
	/// </summary>
	public sealed class FoveInterface : FoveInterfaceBase
	{
		[Tooltip(
			"INTERMEDIATE: Use this prefab as a template for creating eye cameras. Useful if you want custom shaders, etc...")]
		[SerializeField] private Camera eyeCameraPrototype = null;

		[SerializeField] private bool useCameraPrefab = false;

		[Tooltip(
			"ADVANCED: Use this if you are wanting to take (almost) full control of the camera system... use at own risk")]
		[SerializeField] private Camera leftEyeOverride = null;

		[Tooltip(
			"ADVANCED: Use this if you are wanting to take (almost) full control of the camera system... use at own risk")]
		[SerializeField] private Camera rightEyeOverride = null;

		[Tooltip(
			"ADVANCED: Use this if you are wanting to take (almost) full control of the camera system... use at own risk")]
		[SerializeField] private bool useCameraOverride = false;

		[SerializeField] private int antialiasSampleCount = 1;
		[SerializeField] private bool overrideAntialiasing = false;

		[SerializeField] private bool enableRendering = true;

		private Camera _leftCamera, _rightCamera;
		private GameObject _leftCameraObject, _rightCameraObject;
		private bool _hasCameras = false;
		private FoveEyeCamera _leftFoveEye, _rightFoveEye;

		private static List<FoveInterface> _sAllLegacyInterfaces;

		/****************************************************************************************************\
		 * Private functions to setup multi-camera-based rendering
		\****************************************************************************************************/
		private GameObject SetupDummyViewCameraObject(float ipd, EFVR_Eye whichEye)
		{
			GameObject temp = new GameObject();
			temp.name = string.Format("FOVE Eye ({0})", whichEye);
			temp.transform.parent = transform;
			temp.transform.localPosition = new Vector3(ipd, eyeHeight, eyeForward) * worldScale;
			temp.transform.localRotation = UnityEngine.Quaternion.identity;
			return temp;
		}

		/// <summary>
		/// Set up one FOVE view camera in the live scene at runtime.
		/// </summary>
		/// <param name="ipd">How far apart the cameras should be to create the stereoscopic effect.</param>
		/// <param name="whichEye">The eye are you creating this time.</param>
		/// <param name="go">The Unity GameObject instance to which the FOVE camera is attached.</param>
		/// <param name="ec">The FoveEyeCamera instance which is attached to the GameObject.</param>
		/// <returns>The Unity Camera instance which is attached to the GameObject.</returns>
		/// <remarks>We try to support a number of options to be flexible for different users' needs.
		/// As such, the process goes through some checks and differing setup tasks in corresponding
		/// cases. The three paths are: 1) no camera prefabs are referenced, 2) a single camera prefab is
		/// referenced, and 3) both cameras are overridden by objects already placed in the scene.
		/// The third option should only be done for people who *really* need to have two separate cameras
		/// with separate effects for each eye, and accept the consequences that showing different images
		/// to each eye can very easily cause disorientation and sickness in most people.
		/// 
		/// In no case is it strictly necessary to add your own FoveEyeCamera behaviour to a game object,
		/// and we recomment nod doing so, as it can be easy to accidentally get your effects in the wrong
		/// order. (FoveEyeCamera should be the last behaviour in the list in order to get all the image
		/// effects reliably.)</remarks>
		private Camera SetupFoveViewCamera(float ipd, EFVR_Eye whichEye, out GameObject go, out FoveEyeCamera ec)
		{
			GameObject temp = null;
			Camera cam = null;
			Camera mirror = GetComponent<Camera>();

			if (useCameraOverride && useCameraPrefab)
			{
				Debug.LogError("You can not use a prefab and override cameras at the same time!");
			}

			if (useCameraOverride)
			{
				if (leftEyeOverride != null && rightEyeOverride != null)
				{
					if (whichEye == EFVR_Eye.Left)
					{
						cam = leftEyeOverride;
					}
					else if (whichEye == EFVR_Eye.Right)
					{
						cam = rightEyeOverride;
					}
					else
					{
						Debug.LogError("Camera Override in unforseen state");
					}
					temp = cam.gameObject;
					_nearClip = leftEyeOverride.nearClipPlane;
					_farClip = leftEyeOverride.farClipPlane;

					//sanity check
					if (leftEyeOverride.nearClipPlane != rightEyeOverride.nearClipPlane ||
						leftEyeOverride.farClipPlane != rightEyeOverride.farClipPlane)
					{
						Debug.LogWarning("Left and Right eye clip planes differ, using left plane for VR!");
					}

					//the mirror camera is the portal/preview view for unity etc - useful for use when there is no compositor.
					if (mirror != null)
					{
						mirror.nearClipPlane = _nearClip;
						mirror.farClipPlane = _farClip;
					}
				}
				else
				{
					Debug.LogError("Both Camera Overrides must be assiged if using override mode.");
				}
			}

			// Use a camera prefab if set to do so and one is available
			if (useCameraPrefab)
			{
				if (eyeCameraPrototype != null)
				{
					if (eyeCameraPrototype.GetComponent<FoveInterface>() != null)
					{
						Debug.LogError("FoveInterface's eye camera prototype has another FoveInterface component attached. " + whichEye);
						go = null;
						ec = null;
						return null;
					}
					cam = Instantiate(eyeCameraPrototype);
					_nearClip = cam.nearClipPlane;
					_farClip = cam.farClipPlane;

					temp = cam.gameObject;

					if (mirror != null)
					{
						mirror.nearClipPlane = _nearClip;
						mirror.farClipPlane = _farClip;
					}
				}
			}

			if (cam == null)
			{
				temp = new GameObject();
				cam = temp.AddComponent<Camera>();
				if (mirror != null)
				{
					_nearClip = mirror.nearClipPlane;
					_farClip = mirror.farClipPlane;

					// Copy over camera properties
					cam.cullingMask = mirror.cullingMask;
					cam.depth = mirror.depth;
					cam.renderingPath = mirror.renderingPath;
					cam.useOcclusionCulling = mirror.useOcclusionCulling;
#if UNITY_5_6_OR_NEWER
					cam.allowHDR = mirror.allowHDR;
#else
					cam.hdr = mirror.hdr;
#endif
					cam.backgroundColor = mirror.backgroundColor;
					cam.clearFlags = mirror.clearFlags;
				}

				cam.nearClipPlane = _nearClip;
				cam.farClipPlane = _farClip;
			}

			cam.fieldOfView = 95.0f;

			ec = temp.GetComponent<FoveEyeCamera>();
			if (ec == null)
			{
				ec = temp.AddComponent<FoveEyeCamera>();
			}

			ec.whichEye = whichEye;
			ec.suppressProjectionUpdates = suppressProjectionUpdates;
			ec.foveInterfaceBase = this;


			temp.name = string.Format("FOVE Eye ({0})", whichEye);
			temp.transform.parent = transform;

			UpdateCameraSettings(temp, ec, ipd);

			go = temp;
			return cam;
		}

		private void UpdateCameraSettings(GameObject cameraObject, FoveEyeCamera eyeCam, float iod)
		{
			cameraObject.transform.localPosition = new Vector3(iod, eyeHeight, eyeForward) * worldScale;
			cameraObject.transform.localRotation = Quaternion.identity;

			eyeCam.resolutionScale = oversamplingRatio;

			if (overrideAntialiasing)
			{
				eyeCam.antiAliasing = antialiasSampleCount;
			}
			else
			{
				if (QualitySettings.antiAliasing > 0)
				{
					eyeCam.antiAliasing = QualitySettings.antiAliasing;
				}
				else
				{
					eyeCam.antiAliasing = 1;
				}
			}
		}

		/****************************************************************************************************\
		 * GameObject lifecycle methods
		\****************************************************************************************************/
		public override bool RefreshSetup() // called before any Start methods
		{
			if (!base.RefreshSetup())
				return false;

			// Initialize the extra-legacy static list of all legacy interfaces
			if (_sAllLegacyInterfaces == null)
			{
				_sAllLegacyInterfaces = new List<FoveInterface>();
			}
			if (!_sAllLegacyInterfaces.Contains(this))
			{
				_sAllLegacyInterfaces.Add(this);
			}

			if (!ConnectCompositor())
				return false;

			// Use the legacy screen blit shader, but make sure to use the right version to fix (or not)
			// a screen inversion bug that happened until version 5.6.
			{
				var unityVersion = Application.unityVersion;
				var versionBits = unityVersion.Split('.');

				if (versionBits.Length < 2)
				{
					Debug.LogWarning("FoveInterface: Unrecognized Unity version: " + unityVersion);
				}

				var uMajor = int.Parse(versionBits[0]);
				var uMinor = int.Parse(versionBits[1]);

				string shaderString = "Fove/EyeShader";

				// Before Unity 5.6, there was a bug with antialiasing when blitting to the screen that would invert
				// the texture. If running on Unity below 5.6, swap the blit texture if AA detected.
				if (QualitySettings.antiAliasing != 0 && uMajor < 6 && uMinor < 6)
				{
					shaderString = "Fove/EyeShaderInverted";
				}
				_screenBlitMaterial = new Material(Shader.Find(shaderString));
			}

			return true;
		}

		protected override void StartHelper()
		{
			base.StartHelper();

			if (enableRendering)
			{
				StartCoroutine(RenderCoroutine());
			}
		}

		//! Perform a connection to the Fove Compositor
		public override bool ConnectCompositor()
		{
			if (!base.ConnectCompositor())
				return false;
			
			// Set up cameras for the legacy plugin interface
			if (!_hasCameras)
			{
				// LEFT eye
				if (_leftCamera == null)
				{
					_leftCamera = SetupFoveViewCamera(-usedIOD * 0.5f, EFVR_Eye.Left, out _leftCameraObject, out _leftFoveEye);
				}
				else
				{
					UpdateCameraSettings(_leftCameraObject, _leftFoveEye, -usedIOD * 0.5f);
				}

				// RIGHT eye
				if (_rightCamera == null)
				{
					_rightCamera = SetupFoveViewCamera(usedIOD * 0.5f, EFVR_Eye.Right, out _rightCameraObject, out _rightFoveEye);
				}
				else
				{
					UpdateCameraSettings(_rightCameraObject, _rightFoveEye, usedIOD * 0.5f);
				}

				if (_leftCamera == null || _rightCamera == null)
				{
					Debug.LogError("Error refreshing FoveInterface (Legacy) setup: One or more eye cameras weren't initialized.");
					enabled = false;
					return false;
				}

				// Disable eye cameras if we aren't meant to be rendering anything
				if (!enableRendering)
				{
					_leftFoveEye.enabled = false;
					_rightFoveEye.enabled = false;
				}

				// Disable the mirror camera
				Camera mirror = GetComponent<Camera>();
				if (mirror != null)
				{
					mirror.cullingMask = 0;
					mirror.stereoTargetEye = StereoTargetEyeMask.None;
				}

				if (eyeCameraPrototype != null)
				{
					if (eyeCameraPrototype.gameObject.activeInHierarchy)
					{
						eyeCameraPrototype.gameObject.SetActive(false);
					}
				}
				_hasCameras = true;
			}
			else
			{
				_leftCameraObject.SetActive(true);
				_rightCameraObject.SetActive(true);
			}

			_leftFoveEye.Compositor = _compositor;
			_rightFoveEye.Compositor = _compositor;
			
			return true;
		}

		//! Disconnect the Fove Compositor
		public override void DisconnectCompositor()
		{
			base.DisconnectCompositor();

			_leftCameraObject.SetActive(false);
			_rightCameraObject.SetActive(false);

			_leftFoveEye.Compositor = null;
			_rightFoveEye.Compositor = null;
		}

		private IEnumerator RenderCoroutine()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();

				if (null == _compositor || !enableRendering)
				{
					continue;
				}

				_leftCamera.Render();
				_rightCamera.Render();
			}
		}

		private void OnPreCull()
		{
			WaitForRenderPose_IfNeeded();
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (enableRendering && _hasCameras)
			{
				_screenBlitMaterial.SetTexture("_Tex1", _leftCamera.targetTexture);
				_screenBlitMaterial.SetTexture("_Tex2", _rightCamera.targetTexture);
				Graphics.Blit(source, destination, _screenBlitMaterial);
			}
			else
			{
				Graphics.Blit(source, destination);
			}
		}

		private void OnDestroy()
		{
			_sAllLegacyInterfaces.Remove(this);
		}

		/****************************************************************************************************\
		 * Interface Methods
		\****************************************************************************************************/
		protected override Vector3 GetLeftEyePosition()
		{
			if (_hasCameras)
				return _leftCameraObject.transform.position;

			return transform.position;
		}

		protected override Vector3 GetRightEyePosition()
		{
			if (_hasCameras)
				return _rightCameraObject.transform.position;

			return transform.position;
		}

		/****************************************************************************************************\
		 * Legacy instance methods
		\****************************************************************************************************/
		/// <summary>
		/// Get a reference to the camera used to render the right-eye view.
		/// This object remains consistent between frames unless deleted or the scene changes.
		/// </summary>
		/// <param name="whichEye">Which eye's camera to retrieve. This must either be "Left" or "Right".</param>
		/// <returns>The Camera object for the specified eye, or `null` if passed an argument other than "Left" or
		/// "Right".</returns>
		public Camera GetEyeCamera(EFVR_Eye whichEye)
		{
			EnsureLocalDataConcurrency();
			switch (whichEye)
			{
				case EFVR_Eye.Left:
					return _leftCamera;
				case EFVR_Eye.Right:
					return _rightCamera;
				default:
					Debug.LogWarning("GetEyeCamera called with a non-left/right argument, which makes no sense.");
					break;
			}

			return null;
		}

		/// <summary>
		/// Returns the position of the supplied Vector3 in normalized viewport space for whichever
		/// eye is specified. This is a convenience function wrapping Unity's built-in
		/// Camera.WorldToViewportPoint without the need to acquire references to each camera by hand.
		/// 
		/// <para>In most cases, it is sufficient to query only one eye at a time, however both are accessible
		/// for advanced use cases.</para>
		/// </summary>
		/// <param name="pos">The position in 3D world space to project to viewport space.</param>
		/// <param name="eye">Which Fove.Eye (Fove.Eye.Left or Fove.Eye.Right) to project onto.</param>
		/// <returns>The vector indicating where the 3D world point appears in the specified eye viewport.</returns>
		public Vector3 GetNormalizedViewportPointForEye(Vector3 pos, EFVR_Eye eye)
		{
			if (eye == EFVR_Eye.Left)
			{
				return _leftCamera.WorldToViewportPoint(pos);
			}
			if (eye == EFVR_Eye.Right)
			{
				return _rightCamera.WorldToViewportPoint(pos);
			}
			return new Vector3(0, 0, 0);
		}

		public GazeConvergenceData GetWorldGazeConvergence()
		{
			GazeConvergenceData localConvergence = GetGazeConvergence_Raw();
			Ray localRay = localConvergence.ray;

			Vector3 rOrigin = localRay.origin + transform.position;
			Vector3 rDirect = transform
				.TransformDirection(new Vector3(localRay.direction.x, localRay.direction.y, localRay.direction.z))
				.normalized;
			return new GazeConvergenceData(new Ray(rOrigin, rDirect), localConvergence.distance);
		}

		/****************************************************************************************************\
		 * Static interface methods
		\****************************************************************************************************/

		//==============================================================
		// IMMEDIATE ACCESS

		/// <summary>
		/// Get the current HMD rotation as a Unity quaternion. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns>
		public new static Quaternion GetHMDRotation_Immediate()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).GetHMDRotation_Immediate();
			}
			return Quaternion.identity;
		}

		/// <summary>
		/// Get the current HMD position in local coordinates as a Unity Vector3. This value is
		/// automatically applied to the interface's GameObject and is exposed for reference.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		public new static Vector3 GetHMDPosition_Immediate()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).GetHMDPosition_Immediate();
			}
			return Vector3.zero;
		}

		/// <summary>
		/// Returns the direction the left eye is looking towards
		/// </summary>
		/// <returns>The gaze direction</returns>
		public new static Vector3 GetLeftEyeVector_Immediate()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).GetLeftEyeVector_Immediate();
			}
			return Vector3.forward;
		}

		/// <summary>
		/// Returns the direction the right eye is looking towards
		/// </summary>
		/// <returns>The gaze direction</returns>
		public new static Vector3 GetRightEyeVector_Immediate()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).GetRightEyeVector_Immediate();
			}
			return Vector3.forward;
		}

		/// <summary>
		/// Query whther or not a headset is physically connected to the computer
		/// </summary>
		/// <returns>Whether or not a headset is present on the machine.</returns>
		public new static bool IsHardwareConnected()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).IsHardwareConnected();
			}
			return false;
		}

		/// <summary>
		/// Query whether the headset has all requested features booted up and running (position tracking, eye tracking,
		/// orientation, etc...).
		/// </summary>
		/// <returns>Whether you can expect valid data from all HMD functions.</returns>
		public new static bool IsHardwareReady()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).IsHardwareReady();
			}
			return false;
		}

		/// <summary>
		/// Query whether eye tracking has been calibrated and should be usable or not.
		/// </summary>
		/// <returns>Whether eye tracking has been calibrated.</returns>
		public new static bool IsEyeTrackingCalibrated()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).IsEyeTrackingCalibrated();
			}
			return false;
		}

		/// <summary>
		/// Query whether eye tracking is currently calibrating, meaning that the user likely
		/// cannot see your game due to the calibration process. While this is true, you should
		/// refrain from showing any interactions that would respond to eye gaze.
		/// 
		/// Calibration will only occur at specific points during your program, however, unless
		/// manually triggered by the user externally. Those points are:
		/// 1) If you do not set the 'skipAutoCalibrationCheck' flag, it will trigger calibration
		/// the first time any FoveInterface object is created in a scene.
		/// 2) Whenever you call `EnsureEyeTrackingCalibration`.
		/// </summary>
		/// <returns>Whether eye tracking is calibrating.</returns>
		public new static bool IsEyeTrackingCalibrating()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).IsEyeTrackingCalibrating();
			}
			return false;
		}

		/// <summary>
		/// Get the current version of the active Fove client library. This returns "[major].[minor].[build]".
		/// 
		/// </summary>
		/// <returns>A string representing the current client library version.</returns>
		public new static string GetClientVersion()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetClientVersion();
			}
			return "Unable to get client version";
		}

		/// <summary>
		/// Get the current version of the installed runtime service. This returns "[major].[minor].[build]".
		/// 
		/// </summary>
		/// <returns>A string version of the current runtime version.</returns>
		public new static string GetRuntimeVersion()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetRuntimeVersion();
			}
			return "Unable to get runtime version";
		}

		/// <summary>
		/// Check whether the runtime and client versions are compatible and should be expected to work correctly.
		/// </summary>
		/// <param name="error">A human-readable representation of the error thrown, or "None".</param>
		/// <returns>Whether or not the interface should work given the current versions.</returns>
		public new static bool CheckSoftwareVersions(out string error)
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).CheckSoftwareVersions(out error);
			}
			error = "Unable to check software versions";
			return false;
		}

		/// <summary>
		/// Call this function to initiate an eye tracking calibration check. After calling this function
		/// you should assume that the user cannot see your game until FoveInterface.IsEyeTrackingCalibrating
		/// returns false.
		/// </summary>
		/// <returns>Whether the user has completed validating their calibration.</returns>
		public new static bool EnsureEyeTrackingCalibration()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase) _sAllLegacyInterfaces[0]).EnsureEyeTrackingCalibration();
			}
			return false;
		}

		/// <summary>
		/// Reset headset orientation.
		/// 
		/// <para>This sets the HMD's current rotation as a "zero" orientation, essentially resetting their
		/// orientation to that set in the editor.</para>
		/// </summary>
		public new static void TareOrientation()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				((FoveInterfaceBase)_sAllLegacyInterfaces[0]).TareOrientation();
			}
		}

		/// <summary>
		/// Reset headset position.
		/// 
		/// <para>This sets the HMD's current position relative to the tracking camera as a "zero" position,
		/// essentially jumping the headset back to the interface's origin as set in the editor.</para>
		/// </summary>
		public new static void TarePosition()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				((FoveInterfaceBase)_sAllLegacyInterfaces[0]).TarePosition();
			}
		}

		/// <summary>
		/// Checks which eyes are closed
		/// </summary>
		/// <returns>EFVR_Eye of what is closed</returns>
		public new static EFVR_Eye CheckEyesClosed()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).CheckEyesClosed();
			}
			return EFVR_Eye.Neither;
		}

		//==============================================================
		// RENDER-SYNCED ACCESS

		/// <summary>
		/// Get the current HMD rotation as a Unity quaternion. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns>
		public new static Quaternion GetHMDRotation()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetHMDRotation();
			}
			return Quaternion.identity;
		}

		/// <summary>
		/// Get the current HMD position in local coordinates as a Unity Vector3. This value is
		/// automatically applied to the interface's GameObject and is exposed for reference.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		public new static Vector3 GetHMDPosition()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetHMDPosition();
			}
			return Vector3.zero;
		}

		// Used only for internal debug, this function should not be relied on to continue to exist
		// as it does currently for projects.
		//
		// Will likely be removed/replaced in release
		/// <summary>
		/// Returns the direction the left eye is looking towards
		/// </summary>
		/// <returns>The gaze direction</returns>
		public new static Vector3 GetLeftEyeVector()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetLeftEyeVector();
			}
			return Vector3.forward;
		}

		/// <summary>
		/// Returns the direction the right eye is looking towards
		/// </summary>
		/// <returns>The gaze direction</returns>
		public new static Vector3 GetRightEyeVector()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetRightEyeVector();
			}
			return Vector3.forward;
		}

		/// <summary>
		/// Returns the data that describes the convergence point of the eyes. See the description of the
		/// `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in HMD-relative space.</returns>
		public new static GazeConvergenceData GetGazeConvergence()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetGazeConvergence();
			}
			Ray r = new Ray(Vector3.zero, Vector3.forward);
			return new GazeConvergenceData(r, 1);
		}

		/// <summary>
		/// Returns the complete last pose gathered per-frame.
		/// </summary>
		/// <returns></returns>
		public new static SFVR_Pose GetLastPose()
		{
			if (_sAllLegacyInterfaces.Count > 0)
			{
				return ((FoveInterfaceBase)_sAllLegacyInterfaces[0]).GetLastPose();
			}
			return new SFVR_Pose();
		}
	}
}
