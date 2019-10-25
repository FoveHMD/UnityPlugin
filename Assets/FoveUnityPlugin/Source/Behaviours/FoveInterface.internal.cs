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
		
		private Camera _cam;

		protected struct StereoEyeData
		{
			public Matrix4x4 projection;
			public Vector3 position;
		}
		protected StereoEyeData[] _stereoData = new StereoEyeData[2];

		protected struct PoseData
		{
			public Vector3 position;
			public Quaternion orientation;
		}
		protected PoseData _poseData = new PoseData { orientation = Quaternion.identity };

		protected GazeConvergenceData _eyeConverge = new GazeConvergenceData(new Ray(Vector3.zero, Vector3.forward), 7f);
		protected Ray _eyeRayLeft = new Ray(Vector3.zero, Vector3.forward);
		protected Ray _eyeRayRight = new Ray(Vector3.zero, Vector3.forward);
		
		virtual protected void UpdatePoseData(Vector3 position, Vector3 standingPosition, Quaternion orientation)
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

		virtual protected void UpdateGazeMatrices()
		{
			FoveManager.GetProjectionMatrices(_cam.nearClipPlane, _cam.farClipPlane, ref _stereoData[0].projection, ref _stereoData[1].projection);
		}

		virtual protected void UpdateEyePosition(Vector3 left, Vector3 right)
		{
			_stereoData[0].position = left;
			_stereoData[1].position = right;
		}

		virtual protected void UpdateGaze(GazeConvergenceData conv, Vector3 vecLeft, Vector3 vecRight)
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

		virtual protected bool ShouldRenderEye(Eye which)
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
		/// <param name="targetTexture">The target texture to use for rendering the specified eye.</param>
		internal protected virtual void RenderEye(Eye which, RenderTexture targetTexture)
		{
			if (!ShouldRenderEye(which))
				return;

			var origCullMask = _cam.cullingMask;
			var eyeCullMask = which == Eye.Left ? cullMaskLeft : cullMaskRight;
			_cam.cullingMask = origCullMask & ~eyeCullMask;

			var eyeData = _stereoData[(int)which - 1];
			var eyePosOffset = eyeData.position;

			// move the camera to the eye position
			transform.localPosition = _poseData.position + _poseData.orientation * eyePosOffset;
			transform.localRotation = _poseData.orientation;

			// move camera children inversly to keep the stereo projection effect
			foreach (Transform child in transform)
				child.localPosition -= eyePosOffset;

			_cam.projectionMatrix = eyeData.projection;
			_cam.targetTexture = targetTexture;

			_cam.Render();

			_cam.cullingMask = origCullMask;
			_cam.targetTexture = null;
			_cam.ResetProjectionMatrix();

			// reset camera position
			transform.localPosition = _poseData.position;
			transform.localRotation = _poseData.orientation;

			// reset camera children position
			foreach (Transform child in transform)
				child.localPosition += eyePosOffset;
		}

		/****************************************************************************************************\
		 * GameObject lifecycle methods
		\****************************************************************************************************/

		virtual protected void Awake()
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
            if (!FoveSettings.CustomDesktopView && !FoveSettings.IsUsingOpenVR)
				_cam.enabled = false;
		}

		virtual protected void OnEnable()
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

		virtual protected void OnDisable()
		{
			FoveManager.UnregisterInterface(this);
			FoveManager.PoseUpdate.RemoveListener(UpdatePoseData);
			FoveManager.EyeProjectionUpdate.RemoveListener(UpdateGazeMatrices);
			FoveManager.EyePositionUpdate.RemoveListener(UpdateEyePosition);
			FoveManager.GazeUpdate.RemoveListener(UpdateGaze);
		}
		
		virtual protected bool CanSee()
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

		/****************************************************************************************************\
		 * HELPER METHODS
		\****************************************************************************************************/

		protected GazeConvergenceData GetWorldSpaceConvergence(ref Ray ray, float distance)
		{
			var worldOrigin = transform.TransformPoint(ray.origin);
			var worldEnd = transform.TransformPoint(ray.origin + distance * ray.direction);
			var worldDirection = transform.TransformDirection(ray.direction);
			var worldDistance = (worldEnd - worldOrigin).magnitude;
			var worldRay = new Ray(worldOrigin, worldDirection);

			return new GazeConvergenceData(worldRay, worldDistance);
		}

		protected void CalculateGazeRays(ref Vector3 offsetLeft, ref Vector3 offsetRight, ref Vector3 dirLeft, ref Vector3 dirRight, out Ray leftRay, out Ray rightRay)
		{
			var hmdToWorldMat = transform.localToWorldMatrix;
			Utils.CalculateGazeRays(ref hmdToWorldMat, ref dirLeft, ref dirRight, ref offsetLeft, ref offsetRight, out leftRay, out rightRay);
		}

		protected bool InternalGazecastHelperSingle(Collider col, out RaycastHit hit, float maxDistance)
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

		protected bool InternalGazecastHelper(out RaycastHit hit, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
			{
				hit = new RaycastHit();
				return false;
			}
			Debug.DrawRay(_eyeConverge.ray.origin, _eyeConverge.ray.direction, Color.blue, 2.0f);
			return Physics.Raycast(_eyeConverge.ray, out hit, maxDistance, layerMask, queryTriggers);
		}

		protected RaycastHit[] InternalGazecastHelperAll(float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
				return null;

			return Physics.RaycastAll(_eyeConverge.ray, maxDistance, layerMask, queryTriggers);
		}

		protected int InternalGazecastHelperNonAlloc(RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
		{
			if (!CanSee())
				return 0;

			return Physics.RaycastNonAlloc(_eyeConverge.ray, results, maxDistance, layerMask, queryTriggers);
		}
	}
}