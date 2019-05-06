
using System;
using System.Collections.Generic;

using UnityEngine;

using UnityRay = UnityEngine.Ray;
using UnityQuaternion = UnityEngine.Quaternion;

namespace Fove.Unity
{
	[Serializable]
	public enum FovePoseToUse
	{
		Sitting,
		Standing,
	}

	[RequireComponent(typeof(Camera))]
	public class FoveInterface : MonoBehaviour
	{
		/**
		 * MonoBehaviour Implementation
		 * 
		 * The pieces required and used by the MonoBehaviour subclass when attached to a GameObject in the
		 * scene. This behaviour does not function unless there is a single instance of this class
		 * instantiated in the scene.
		 */
		[Tooltip("Turns off gaze tracking")]
		[SerializeField] protected bool gaze = true;
		[Tooltip("Turns off orientation tracking")]
		[SerializeField] protected bool orientation = true;

		[Tooltip("Turns off position tracking (turns on position tracking when on)")]
		[SerializeField] protected bool position = true;

		[Tooltip("Which eye(s) to render to.\n\nSelecting 'Left' or 'Right' will cause this camera to ONLY render to that eye. 'Both' is default. 'Neither' will prevent this camera from rendering to the HMD.\n\nNOTE: 'Neither' will not prevent the normal camera view from displaying.")]
		[SerializeField] private Eye eyeTargets = Eye.Both;
		[Tooltip("How to interpret the HMD position:\n\nSitting: (Old default) The HMD is positioned relative to the tracking camera.\n\nStanding: An (system-configurable) offset is added to the 'Sitting' position. This option is more compatible with systems like SteamVR.")]
		[SerializeField] private FovePoseToUse poseType = FovePoseToUse.Standing;

		// Compositor options
		[Tooltip(
			"Check this to disable time warp on images rendered and sent to the compositor. This is useful if you disable orientation to avoid any jitter due to frame latency.")]
		[SerializeField] protected bool disableTimewarp = false;
		[SerializeField] protected bool disableFading = false;
		[SerializeField] protected bool disableDistortion = false;
		/*[SerializeField] */protected CompositorLayerType layerType = CompositorLayerType.Base; // enforce the use of the base layer for the moment

		[Tooltip("Don't draw any of the selected layers to the left eye.")]
		[SerializeField] private LayerMask cullMaskLeft;
		[Tooltip("Don't draw any of the selected layers to the right eye.")]
		[SerializeField] private LayerMask cullMaskRight;

		public bool TimewarpDisabled { get { return disableTimewarp; } }
		public bool FadingDisabled { get { return disableFading; } }
		public bool DistortionDisabled { get { return disableDistortion; } }
		public CompositorLayerType LayerType { get { return layerType; } }

		public Camera Camera { get { return _cam; } }
		protected Camera _cam;

		private struct StereoEyeData
		{
			public Matrix4x4 projection;
			public Vector3 position;
		}
		private StereoEyeData[] _stereoData = new StereoEyeData[2];

		private struct PoseData
		{
			public Vector3 position;
			public UnityQuaternion orientation;
		}
		private PoseData _poseData = new PoseData();


		private GazeConvergenceData _eyeConverge = new GazeConvergenceData(new UnityRay(Vector3.zero, Vector3.forward), 7f);
		protected UnityRay _eyeRayLeft = new UnityRay(Vector3.zero, Vector3.forward);
		protected UnityRay _eyeRayRight = new UnityRay(Vector3.zero, Vector3.forward);

		protected float _usedIOD;
		public float UsedIOD { get { return _usedIOD; } }

		// Private callbacks and support for HMD events
		private void RegisterEventCallbacks()
		{
			var createInfo = new CompositorLayerCreateInfo
			{
				alphaMode = AlphaMode.Auto,
				disableDistortion = disableDistortion,
				disableFading = disableFading,
				disableTimewarp = disableTimewarp,
				type = layerType
			};
			FoveManager.RegisterInterface(createInfo, this);
			FoveManager.PoseUpdate.AddListener(UpdatePoseData);
			FoveManager.EyeProjectionUpdate.AddListener(UpdateGazeMatrices);
			FoveManager.EyePositionUpdate.AddListener(UpdateEyePosition);
			FoveManager.GazeUpdate.AddListener(UpdateGaze);
		}

		private void UnregisterCallbacks()
		{
			FoveManager.UnregisterInterface(this);
			FoveManager.PoseUpdate.RemoveListener(UpdatePoseData);
			FoveManager.EyeProjectionUpdate.RemoveListener(UpdateGazeMatrices);
			FoveManager.EyePositionUpdate.RemoveListener(UpdateEyePosition);
			FoveManager.GazeUpdate.RemoveListener(UpdateGaze);
		}

		private void UpdatePoseData(Vector3 position, Vector3 standingPosition, UnityQuaternion orientation)
		{
			if (this.orientation)
				_poseData.orientation = orientation;
			if (this.position) {
				switch (poseType)
				{
					case FovePoseToUse.Standing:
						_poseData.position = standingPosition;
						break;
					case FovePoseToUse.Sitting:
						_poseData.position = position;
						break;
				}
			}

			transform.localPosition = _poseData.position;
			transform.localRotation = _poseData.orientation;
		}

		private void UpdateGazeMatrices()
		{
			FoveManager.GetProjectionMatrices(_cam.nearClipPlane, _cam.farClipPlane, ref _stereoData[0].projection, ref _stereoData[1].projection);
		}

		private void UpdateEyePosition(Vector3 left, Vector3 right)
		{
			_stereoData[0].position = left;
			_stereoData[1].position = right;
		}

		private void UpdateGaze(GazeConvergenceData conv, Vector3 vecLeft, Vector3 vecRight)
		{
			if (gaze) {
				_eyeConverge.distance = conv.distance;
				_eyeConverge.ray = TransformLocalRay(conv.ray);
				_eyeConverge.ray.direction.Normalize();
			}
			else
			{
				var r = new UnityRay(Vector3.zero, Vector3.forward);
				_eyeConverge.ray = TransformLocalRay(r);
			}

			CalculateGazeRays(out _eyeRayLeft, out _eyeRayRight, vecLeft, vecRight);
		}

		private UnityRay TransformLocalRay(UnityRay ray)
		{
			var result = new UnityRay(transform.TransformPoint(ray.origin), transform.TransformDirection(ray.direction).normalized);
			return result;
		}

		/// <summary>
		/// Position this interface to one eye or the other. If a value other than left/right is sent
		/// in it resets the position to zero for you. This is here for internal use, but it's public
		/// because you may come up with a reason to need this.
		/// </summary>
		/// <param name="which">The eye you'd like the interface's position to match</param>
		public void PositionToEye(Eye which)
		{
			var targetPosition = Vector3.zero;
			if (which == Eye.Left || which == Eye.Right)
			{
				var eyeData = _stereoData[(int)which - 1];
				targetPosition = _poseData.position + _poseData.orientation * eyeData.position;
			}

			transform.localPosition = targetPosition;
			transform.localRotation = _poseData.orientation;

			//var actual = transform.position;
			//var delta = Vector3.right * 0.01f;
			//Debug.DrawLine(actual - delta, actual + delta, Color.black);
		}

		protected bool ShouldRenderEye(Eye which)
		{
			if (which == Eye.Neither || which == Eye.Both)
				return false;

			if (((int)eyeTargets & (int)which) == 0)
				return false;

			return true;
		}

		/// <summary>
		/// Render the specified eye onto the texture provided. This is used internally and likely
		/// won't be needed for the vast majority of applications. If you use Neither or Both, it
		/// returns immediately.
		/// </summary>
		/// <param name="which">The eye you want to render to the provided texture</param>
		/// <param name="rt">The target texture to use for rendering the specified eye.</param>
		public virtual void RenderEye(Eye which, RenderTexture rt)
		{
			if (!ShouldRenderEye(which))
				return;

			var eyeData = _stereoData[(int)which - 1];

			var origCullMask = _cam.cullingMask;
			var eyeCullMask = which == Eye.Left ? cullMaskLeft : cullMaskRight;
			_cam.cullingMask = origCullMask & ~eyeCullMask;

			PositionToEye(which);

			_cam.projectionMatrix = eyeData.projection;
			_cam.targetTexture = rt;

			_cam.Render();

			_cam.cullingMask = origCullMask;
			_cam.targetTexture = null;
			_cam.ResetProjectionMatrix();
			transform.localPosition = _poseData.position;
			transform.localRotation = _poseData.orientation;
		}

		/****************************************************************************************************\
		 * GameObject lifecycle methods
		\****************************************************************************************************/
		private void Awake()
		{
			if (transform.parent == null)
			{
				var parent = new GameObject(name + " BASE");
				parent.transform.position = transform.position;

				transform.parent = parent.transform;
				transform.localPosition = Vector3.zero;
			}
			_cam = GetComponent<Camera>();
			_cam.enabled = false;
		}

		private void Start()
		{
			RegisterEventCallbacks();
		}

		protected void OnApplicationQuit()
		{
			UnregisterCallbacks();
		}

		private void OnDestroy()
		{
			UnregisterCallbacks();
		}

		/****************************************************************************************************\
		 * Interface Methods
		\****************************************************************************************************/
		public void CreateGazeRaysFromScreenPoints(Vector2 lScreenPt, Vector2 rScreenPt, out UnityRay leftRay, out UnityRay rightRay)
		{
			var origin = transform.position;
			var lPosition = origin + _stereoData[0].position;
			var rPosition = origin + _stereoData[1].position;

			Vector3 lDirection, rDirection;
			lDirection = _stereoData[0].projection.inverse.MultiplyPoint(lScreenPt);
			rDirection = _stereoData[1].projection.inverse.MultiplyPoint(rScreenPt);

			leftRay = new UnityRay(lPosition, transform.TransformDirection(lDirection * -1));
			rightRay = new UnityRay(rPosition, transform.TransformDirection(rDirection * -1));
		}

		public void Make2DFromVector(GazeVector inL, GazeVector inR, out Vector2 outL, out Vector2 outR)
		{
			var lv = Utils.GetUnityVector(inL.vector);
			var rv = Utils.GetUnityVector(inR.vector);
			
			outL = _stereoData[0].projection.MultiplyPoint(lv);
			outR = _stereoData[1].projection.MultiplyPoint(rv);
		}

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
		protected void CalculateGazeRays(out UnityRay leftRay, out UnityRay rightRay, Vector3 vecLeft, Vector3 vecRight)
		{
			var origin = transform.position;
			var lPosition = origin + _poseData.orientation * _stereoData[0].position;
			var rPosition = origin + _poseData.orientation * _stereoData[1].position;

			var usedVecLeft = gaze ? vecLeft : Vector3.forward;
			var usedVecRight = gaze ? vecRight : Vector3.forward;

			leftRay = new UnityRay(lPosition, transform.TransformDirection(usedVecLeft.normalized));
			rightRay = new UnityRay(rPosition, transform.TransformDirection(usedVecRight.normalized));
		}

		#region Gazecasting
		private bool CanSee()
		{
			return FoveManager.CheckEyesClosed() != Eye.Both;
		}

		private bool InternalGazecastHelperSingle(Collider col, out RaycastHit hit, float maxDistance)
		{
			bool eyesInsideCollider = (col.bounds.Contains(_eyeConverge.ray.origin));

			if (eyesInsideCollider || !CanSee())
			{
				hit = new RaycastHit();
				return false;
			}
			
			if (col.Raycast(_eyeConverge.ray, out hit, maxDistance))
				return true;

			return false;
		}

		private bool InternalGazecastHelper(out RaycastHit hit, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
			{
				hit = new RaycastHit();
				return false;
			}
			Debug.DrawRay(_eyeConverge.ray.origin, _eyeConverge.ray.direction, Color.blue, 2.0f);
			return Physics.Raycast(_eyeConverge.ray, out hit, maxDistance, layerMask, queryTriggers);
		}

		private RaycastHit[] InternalGazecastHelper_All(float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
			{
				return null;
			}

			return Physics.RaycastAll(_eyeConverge.ray, maxDistance, layerMask, queryTriggers);
		}

		private int InternalGazecastHelper_NonAlloc(RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
			{
				return 0;
			}

			return Physics.RaycastNonAlloc(_eyeConverge.ray, results, maxDistance, layerMask, queryTriggers);
		}

		/// <summary>
		/// Check the user's gaze for collision with objects in the scene.
		/// 
		/// This wraps the built-in Physics.Raycast for this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
		/// </summary>
		/// <param name="maxDistance">The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <param name="layerMask">The mask value used to filter for colliders to check. This value should be the
		/// Unity-specified layer mask the same as you would use for physics racasting.</param
		/// <param name="queryTriggerInteraction">Optional. You can supply a value from Unity's QueryTriggerInteraction  enum
		/// to indicate how you want Trigger colliders to be treated for this gazecast. (See Unity's documentation
		/// for more information: https://docs.unity3d.com/ScriptReference/QueryTriggerInteraction.html) </param>
		/// <returns>Whether or not any colliders are being looked at.</returns>
		public bool Gazecast(float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			RaycastHit hit;
			return InternalGazecastHelper(out hit, maxDistance, layerMask, queryTriggerInteraction);
		}

		/// <summary>
		/// Check the user's gaze for collision with objects in the scene.
		/// 
		/// This wraps the built-in Physics.Raycast for this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
		/// </summary>
		/// <param name="hit">Information about where a collider was hit.</param>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <param name="layerMask">Optional. The mask value used to filter for colliders to check. This value should be the
		/// Unity-specified layer mask the same as you would use for physics racasting.</param
		/// <param name="queryTriggerInteraction">Optional. You can supply a value from Unity's QueryTriggerInteraction  enum
		/// to indicate how you want Trigger colliders to be treated for this gazecast. (See Unity's documentation
		/// for more information: https://docs.unity3d.com/ScriptReference/QueryTriggerInteraction.html) </param>
		/// <returns>Whether or not any colliders are being looked at.</returns>
		public bool Gazecast(out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			return InternalGazecastHelper(out hit, maxDistance, layerMask, queryTriggerInteraction);
		}

		/// <summary>
		/// Determine if a specified collider intersects the user's gaze.
		/// 
		/// This wraps the built-in Physics.Raycast for this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
		/// </summary>
		/// <param name="col">The collider to check against the user's gaze.</param>
		/// <param name="maxDistance">The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <returns>Whether or not the referenced collider is being looked at.</returns>
		public bool Gazecast(Collider col, float maxDistance = Mathf.Infinity)
		{
			RaycastHit hit;
			return InternalGazecastHelperSingle(col, out hit, maxDistance);
		}

		/// <summary>
		/// Determine if a specified collider intersects the user's gaze.
		/// 
		/// This wraps Collider.Raycast for the specified collider and this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Collider.Raycast.html
		/// </summary>
		/// <param name="col">The collider to check against the user's gaze.</param>
		/// <param name="hit">The out information about the point that was hit.</param>
		/// <returns>Whether or not the referenced collider is being looked at.</returns>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <returns>Whether or not any of the referenced collider is being looked at.</returns>
		public bool Gazecast(Collider col, out RaycastHit hit, float maxDistance = Mathf.Infinity)
		{
			return InternalGazecastHelperSingle(col, out hit, maxDistance);
		}

		/// <summary>
		/// Determine if any colliders in the provided collection intersect the user's gaze.
		/// 
		/// This wraps Collider.Raycast for a set of colliders and this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Collider.Raycast.html
		/// </summary>
		/// <param name="cols">The set of colliders to check against the user's gaze.</param>
		/// <returns>Whether or not any of the referenced colliders are being looked at.</returns>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <returns>Whether or not any of the referenced colliders are being looked at.</returns>
		public bool Gazecast(IEnumerable<Collider> cols, float maxDistance = Mathf.Infinity)
		{
			RaycastHit hit;
			return Gazecast(cols, out hit, maxDistance);
		}

		/// <summary>
		/// Determine if any colliders in the provided collection intersect the user's gaze.
		/// 
		/// This wraps Collider.Raycast for a set of colliders and this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Collider.Raycast.html
		/// </summary>
		/// <param name="cols">The set of colliders to check against the user's gaze.</param>
		/// <param name="hit">The out information about the point that was hit.</param>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <returns>Whether or not any of the referenced colliders are being looked at.</returns>
		public bool Gazecast(IEnumerable<Collider> cols, out RaycastHit hit, float maxDistance = Mathf.Infinity)
		{
			foreach (var c in cols)
			{
				if (InternalGazecastHelperSingle(c, out hit, maxDistance))
				{
					return true;
				}
			}

			hit = new RaycastHit();
			return false;
		}

		/// <summary>
		/// Find and return all colliders hit along the user's gaze convergence ray.
		/// 
		/// This wraps the built-in Physics.RaycastAll for this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.RaycastAll.html
		/// </summary>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <param name="layerMask">Optional. The mask value used to filter for colliders to check. This value should be the
		/// Unity-specified layer mask the same as you would use for physics racasting.</param
		/// <param name="queryTriggerInteraction">Optional. You can supply a value from Unity's QueryTriggerInteraction  enum
		/// to indicate how you want Trigger colliders to be treated for this gazecast. (See Unity's documentation
		/// for more information: https://docs.unity3d.com/ScriptReference/QueryTriggerInteraction.html) </param>
		/// <returns>Whether or not any of the colliders with the specified mask are being looked at.</returns>
		public RaycastHit[] GazecastAll(float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			return InternalGazecastHelper_All(maxDistance, layerMask, queryTriggerInteraction);
		}

		/// <summary>
		/// Determine if any colliders intersect the user's gaze, and write them collisions into the supplied, pre-
		/// allocated array object.
		/// 
		/// This wraps the built-in Physics.RaycastNonAlloc for this FoveInterface's gaze ray.
		/// See: https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html
		/// </summary>
		/// <param name="results">The buffer to store the hits into.</param>
		/// <param name="maxDistance">Optional. The max distance the rayhit is allowed to be from the convergence origin.</param>
		/// <param name="layerMask">Optional. The mask value used to filter for colliders to check. This value should be the
		/// Unity-specified layer mask the same as you would use for physics racasting.</param
		/// <param name="queryTriggerInteraction">Optional. You can supply a value from Unity's QueryTriggerInteraction  enum
		/// to indicate how you want Trigger colliders to be treated for this gazecast. (See Unity's documentation
		/// for more information: https://docs.unity3d.com/ScriptReference/QueryTriggerInteraction.html) </param>
		/// <returns>Whether or not any of the colliders with the specified mask are being looked at.</returns>
		public int GazecastNonAlloc(RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			return InternalGazecastHelper_NonAlloc(results, maxDistance, layerMask, queryTriggerInteraction);
		}
		#endregion

		/// <summary>
		/// Get a structure containing Unity Ray objects which describe where in the scene each of the user's eyes are
		/// looking.</summary>
		/// <returns>The set of Unity Ray objects describing the user's eye gaze.</returns>
		/// <remarks>These rays are overwritten each frame, so you should not retain references to them across frames.
		/// </remarks>
		public EyeRays GetGazeRays()
		{
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
			UnityRay left, right;
			CalculateGazeRays(out left, out right, FoveManager.GetLeftEyeVector_Immediate(), FoveManager.GetRightEyeVector_Immediate());

			return new EyeRays(left, right);
		}

		/// <summary>
		/// Returns the data that describes the convergence point of the eyes this frame. See the description of the
		/// `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in HMD-relative space.</returns>
		public GazeConvergenceData GetGazeConvergence_Immediate()
		{
			GazeConvergenceData localConvergence = FoveManager.GetLocalGazeConvergence_Immediate();
			var localRay = localConvergence.ray;
			return new GazeConvergenceData(TransformLocalRay(localRay), localConvergence.distance);
		}

		/// <summary>
		/// Returns the data that describes the convergence point of the eyes this frame. See the description of the
		/// `GazeConvergenceData` for more detail on how to use this information.
		/// </summary>
		/// <returns>A struct describing the gaze convergence in world space.</returns>
		public GazeConvergenceData GetGazeConvergence()
		{
			return _eyeConverge;
		}
	}
}
