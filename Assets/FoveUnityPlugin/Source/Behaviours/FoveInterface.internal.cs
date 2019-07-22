using System;
using UnityEngine;

namespace Fove.Unity
{
	[RequireComponent(typeof(Camera))]
	public partial class FoveInterface : MonoBehaviour
	{
		// Compositor options
		[Tooltip("Check this to disable time warp on images rendered and sent to the compositor. This is useful if you disable orientation to avoid any jitter due to frame latency.")]
		[SerializeField] protected bool disableTimewarp = false;
		[SerializeField] protected bool disableFading = false;
		[SerializeField] protected bool disableDistortion = false;
		/*[SerializeField] */protected CompositorLayerType layerType = CompositorLayerType.Base; // enforce the use of the base layer for the moment
		
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
			public Quaternion orientation;
		}
		private PoseData _poseData = new PoseData();

		private GazeConvergenceData _eyeConverge = new GazeConvergenceData(new Ray(Vector3.zero, Vector3.forward), 7f);
		protected Ray _eyeRayLeft = new Ray(Vector3.zero, Vector3.forward);
		protected Ray _eyeRayRight = new Ray(Vector3.zero, Vector3.forward);

		// Private callbacks and support for HMD events
		private void RegisterCallbacks()
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

		private void UpdatePoseData(Vector3 position, Vector3 standingPosition, Quaternion orientation)
		{
			if (fetchOrientation)
				_poseData.orientation = orientation;

			if (fetchPosition) {
				switch (poseType)
				{
					case PlayerPose.Standing:
						_poseData.position = standingPosition;
						break;
					case PlayerPose.Sitting:
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
			// converge data
			var localConvDist = 0f;
			var localConvRay = new Ray(Vector3.zero, Vector3.forward);
			if(fetchGaze)
			{
				localConvRay = conv.ray;
				localConvDist = conv.distance;
			}
			_eyeConverge = GetWorldSpaceConvergence(ref localConvRay, localConvDist);
			
			// eye rays
			var usedDirLeft = fetchGaze ? vecLeft : Vector3.forward;
			var usedDirRight = fetchGaze ? vecRight : Vector3.forward;
			CalculateGazeRays(ref _stereoData[0].position, ref _stereoData[1].position, ref usedDirLeft, ref usedDirRight, out _eyeRayLeft, out _eyeRayRight);
		}

		private GazeConvergenceData GetWorldSpaceConvergence(ref Ray ray, float distance)
		{
			var worldOrigin = transform.TransformPoint(ray.origin);
			var worldEnd = transform.TransformPoint(ray.origin + distance * ray.direction);
			var worldDirection = transform.TransformDirection(ray.direction);
			var worldDistance = (worldEnd - worldOrigin).magnitude;
			var worldRay = new Ray(worldOrigin, worldDirection);

			return new GazeConvergenceData(worldRay, worldDistance);
		}

		private void CalculateGazeRays(ref Vector3 offsetLeft, ref Vector3 offsetRight, ref Vector3 dirLeft, ref Vector3 dirRight, out Ray leftRay, out Ray rightRay)
		{
			var hmdToWorldMat = transform.localToWorldMatrix;
			Utils.CalculateGazeRays(ref hmdToWorldMat, ref dirLeft, ref dirRight, ref offsetLeft, ref offsetRight, out leftRay, out rightRay);
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
		protected void CalculateGazeRays(out Ray leftRay, out Ray rightRay, Vector3 vecLeft, Vector3 vecRight)
		{
			var origin = transform.position;
			var lPosition = origin + _poseData.orientation * _stereoData[0].position;
			var rPosition = origin + _poseData.orientation * _stereoData[1].position;
			// TODO remove this functions!!!
			leftRay = new Ray(lPosition, transform.TransformDirection(vecLeft.normalized));
			rightRay = new Ray(rPosition, transform.TransformDirection(vecRight.normalized));
		}

		/// <summary>
		/// Position this interface to one eye or the other. If a value other than left/right is sent
		/// in it resets the position to zero for you. This is here for internal use, but it's public
		/// because you may come up with a reason to need this.
		/// </summary>
		/// <param name="which">The eye you'd like the interface's position to match</param>
		private void PositionToEye(Eye which)
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
		internal virtual void RenderEye(Eye which, RenderTexture rt)
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
				parent.transform.rotation = transform.rotation;

				transform.parent = parent.transform;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
			_cam = GetComponent<Camera>();
            if (!FoveSettings.CustomDesktopView)
				_cam.enabled = false;
		}

		private void OnEnable()
		{
			RegisterCallbacks();
		}

		protected void OnDisable()
		{
			UnregisterCallbacks();
		}
		
		private bool CanSee()
		{
			var closedEyes = FoveManager.CheckEyesClosed();
			switch (gazeCastPolicy)
			{
				case GazeCastPolicy.DismissWhenBothEyesClosed:
					return closedEyes != Eye.Both;
				case GazeCastPolicy.DismissWhenOneEyeClosed:
					return closedEyes == Eye.Neither;
				case GazeCastPolicy.NeverDismiss:
					return true;
			}

			throw new NotImplementedException("Unknown gaze cast policy '" + gazeCastPolicy + "'");
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
	}
}