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
        /*[SerializeField]*/ protected bool disableFading = false;
        /*[SerializeField]*/ protected bool disableDistortion = false;
        /*[SerializeField]*/ protected CompositorLayerType layerType = CompositorLayerType.Base; // enforce the use of the base layer for the moment

        private static int instanceCount = 0;

        private int id = instanceCount++;

        protected int Id { get { return id; } }

        protected ObjectPose pose;
        protected bool cameraObjectRegistered;

        private Camera _cam;

        protected struct StereoEyeData
        {
            public Stereo<Matrix4x4> projections;
            public Stereo<Vector3> offsets;

            public Vector3 GetOffset(Eye eye) { return eye == Eye.Left ? offsets.left : offsets.right; }
            public Matrix4x4 GetProjection(Eye eye) { return eye == Eye.Left ? projections.left : projections.right; }
        }
        protected StereoEyeData _stereoEyeData = new StereoEyeData();

        protected struct PoseData
        {
            public Vector3 position;
            public Quaternion orientation;
        }
        protected PoseData _poseData = new PoseData { orientation = Quaternion.identity };

        protected Result<GazeConvergenceData> _eyeConverge = new Result<GazeConvergenceData>(GazeConvergenceData.ForwardToInfinity, ErrorCode.Data_NoUpdate);

        protected static readonly Ray Forward = new Ray(Vector3.zero, Vector3.forward);
        protected Result<Stereo<Ray>> _eyeRays = new Result<Stereo<Ray>>(new Stereo<Ray>(Forward), ErrorCode.Data_NoUpdate);
        
        Vector3 lastPosition;
                
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
            _stereoEyeData.projections = FoveManager.GetProjectionMatrices(_cam.nearClipPlane, _cam.farClipPlane);
        }

        virtual protected void UpdateEyePosition(Result<Stereo<Vector3>> eyeVectors)
        {
            if (eyeVectors.Succeeded)
                _stereoEyeData.offsets = eyeVectors.value;
        }

        virtual protected void UpdateGaze(Result<GazeConvergenceData> hmdGazeConv, Result<Stereo<Vector3>> eyeVectors)
        {
            if (!fetchGaze)
            {
                // override the provided value with default ones
                var error = ErrorCode.Data_NoUpdate;
                hmdGazeConv = new Result<GazeConvergenceData>(GazeConvergenceData.ForwardToInfinity, error);
                eyeVectors = new Result<Stereo<Vector3>>(new Stereo<Vector3>(Vector3.forward), error);
            }

            // convergence
            _eyeConverge = GetWorldSpaceConvergence(ref hmdGazeConv);
            
            // eye rays
            _eyeRays.error = eyeVectors.error; // left and right error are the same as both are fetch by the same function
            CalculateGazeRays(ref _stereoEyeData.offsets, ref eyeVectors.value, out _eyeRays.value);
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

            var eyePosOffset = _stereoEyeData.GetOffset(which);

            // move the camera to the eye position
            transform.localPosition = _poseData.position + _poseData.orientation * eyePosOffset;
            transform.localRotation = _poseData.orientation;

            // move camera children inversely to keep the stereo projection effect
            foreach (Transform child in transform)
                child.localPosition -= eyePosOffset;

            _cam.projectionMatrix = _stereoEyeData.GetProjection(which);
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

        private void RecreateLayer()
        {
            FoveManager.UnregisterInterface(this);

            var createInfo = new CompositorLayerCreateInfo
            {
                alphaMode = AlphaMode.Auto,
                disableDistortion = disableDistortion,
                disableFading = disableFading,
                disableTimewarp = disableTimewarp,
                type = layerType
            };

            FoveManager.RegisterInterface(createInfo, this);
        }

        virtual protected void OnEnable()
        {
            RecreateLayer();
            FoveManager.PoseUpdate.AddListener(UpdatePoseData);
            FoveManager.EyeProjectionUpdate.AddListener(UpdateGazeMatrices);
            FoveManager.EyePositionUpdate.AddListener(UpdateEyePosition);
            FoveManager.GazeUpdate.AddListener(UpdateGaze);
            if (registerCameraObject)
                RegisterCameraObject();
        }

        virtual protected void OnApplicationQuit()
        {
            // Do not remove the camera object when the application quits.
            // Unfortunately Unity do not warranty any order of destruction for the objects and
            // this generates issues with the FoveManager singleton that can already be already destroyed
            // This is fine as all objects are currently registered on the client side and the client is recreated
            // at each execution. It may be needed to revise this in the future if registration moves on the runtime side though.
            cameraObjectRegistered = false;
        }

        virtual protected void OnDisable()
        {
            FoveManager.UnregisterInterface(this);

            FoveManager.PoseUpdate.RemoveListener(UpdatePoseData);
            FoveManager.EyeProjectionUpdate.RemoveListener(UpdateGazeMatrices);
            FoveManager.EyePositionUpdate.RemoveListener(UpdateEyePosition);
            FoveManager.GazeUpdate.RemoveListener(UpdateGaze);
            if (registerCameraObject)
                RemoveCameraObject();
        }

        virtual protected void UpdatePose()
        {
            pose.position = Utils.ToVec3(transform.position);
            pose.rotation = Utils.ToQuat(transform.rotation);
            pose.scale = new Vec3(1, 1, 1);
            pose.velocity = Utils.ToVec3((transform.position - lastPosition) / Time.deltaTime);
            lastPosition = transform.position;
        }

        virtual protected void LateUpdate()
        {
            if (!cameraObjectRegistered)
                return;

            UpdatePose();
            var res = FoveManager.Headset.UpdateCameraObject(Id, ref pose);
            if (res != ErrorCode.None)
                Debug.LogError("Update camera object pose failed. Error code=" + res);
        }

        virtual protected bool CanSee()
        {
            var closedEyesResult = FoveManager.CheckEyesClosed();
            if (_eyeConverge.HasError || closedEyesResult.HasError)
                return false;

            var closedEyes = closedEyesResult.value;
            var gazeCastPolicy = FoveManager.GazeCastPolicy;
            switch (gazeCastPolicy)
            {
                case GazeCastPolicy.DismissBothEyeClosed:
                    return closedEyes != Eye.Both;
                case GazeCastPolicy.DismissOneEyeClosed:
                    return closedEyes == Eye.Neither;
                case GazeCastPolicy.NeverDismiss:
                    return true;
            }

            throw new NotImplementedException("Unknown gaze cast policy '" + gazeCastPolicy + "'");
        }

        /****************************************************************************************************\
         * HELPER METHODS
        \****************************************************************************************************/

        protected Result<GazeConvergenceData> GetWorldSpaceConvergence(ref Result<GazeConvergenceData> localGaze)
        {
            var ray = localGaze.value.ray;
            var distance = localGaze.value.distance;
            var worldOrigin = transform.TransformPoint(ray.origin);
            var worldEnd = transform.TransformPoint(ray.origin + distance * ray.direction);
            var worldDirection = transform.TransformDirection(ray.direction);
            var worldDistance = (worldEnd - worldOrigin).magnitude;
            var worldRay = new Ray(worldOrigin, worldDirection);
            var worldGazeConvergence = new GazeConvergenceData(worldRay, worldDistance);

            return new Result<GazeConvergenceData>(worldGazeConvergence, localGaze.error);
        }

        protected void CalculateGazeRays(ref Stereo<Vector3> eyeOffsets, ref Stereo<Vector3> eyeVectors, out Stereo<Ray> gazeRays)
        {
            var hmdToWorldMat = transform.localToWorldMatrix;
            Utils.CalculateGazeRays(ref hmdToWorldMat, ref eyeOffsets, ref eyeVectors, out gazeRays);
        }

        protected bool InternalGazecastHelperSingle(Collider col, out RaycastHit hit, float maxDistance)
        {
            var ray = _eyeConverge.value.ray;
            bool eyesInsideCollider = col.bounds.Contains(ray.origin);

            if (eyesInsideCollider || !CanSee())
            {
                hit = new RaycastHit();
                return false;
            }
            
            if (col.Raycast(ray, out hit, maxDistance))
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
            var ray = _eyeConverge.value.ray;
            Debug.DrawRay(ray.origin, ray.direction, Color.blue, 2.0f);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask, queryTriggers);
        }

        protected RaycastHit[] InternalGazecastHelperAll(float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
        {
            if (!CanSee())
                return null;

            return Physics.RaycastAll(_eyeConverge.value.ray, maxDistance, layerMask, queryTriggers);
        }

        protected int InternalGazecastHelperNonAlloc(RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
        {
            if (!CanSee())
                return 0;

            return Physics.RaycastNonAlloc(_eyeConverge.value.ray, results, maxDistance, layerMask, queryTriggers);
        }
    }
}