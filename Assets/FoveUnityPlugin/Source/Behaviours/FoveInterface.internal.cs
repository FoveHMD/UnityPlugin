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
        /*[SerializeField]*/
        protected bool disableFading = false;
        /*[SerializeField]*/
        protected bool disableDistortion = false;
        /*[SerializeField]*/
        protected CompositorLayerType layerType = CompositorLayerType.Base; // enforce the use of the base layer for the moment

        protected static readonly Result<Ray> DefaultRayResult = new Result<Ray>(new Ray(Vector3.zero, Vector3.forward), ErrorCode.Data_NoUpdate);

        private static int instanceCount = 0;

        private int id = instanceCount++;

        protected int Id { get { return id; } }

        protected Vector3 previousPosition;
        protected ObjectPose previousPose;
        protected bool cameraObjectRegistered;

        protected Stereo<Matrix4x4> projectionMatrices;
        protected Stereo<Vector3> eyeOffsets;

        protected Vector3 hmdPosition;
        protected Quaternion hmdOrientation = Quaternion.identity;

        protected Result<Ray> eyeConvergeRay = DefaultRayResult;
        protected Stereo<Result<Ray>> eyeRays = new Stereo<Result<Ray>>(DefaultRayResult, DefaultRayResult);

        private Camera cam;

        virtual protected void UpdateData()
        {
            UpdatePoseData();
            UpdateViewParameters();
            UpdateGaze();
        }

        virtual protected void UpdatePoseData()
        {
            if (fetchOrientation)
                hmdOrientation = FoveManager.GetHmdRotation();

            if (fetchPosition)
                hmdPosition = FoveManager.GetHmdPosition(poseType == PlayerPose.Standing);

            transform.localPosition = hmdPosition;
            transform.localRotation = hmdOrientation;
        }

        virtual protected void UpdateViewParameters()
        {
            eyeOffsets = FoveManager.GetEyeOffsets();
            projectionMatrices = FoveManager.GetProjectionMatrices(cam.nearClipPlane, cam.farClipPlane);
        }

        virtual protected void UpdateGaze()
        {
            if (!fetchGaze)
            {
                eyeConvergeRay.error = ErrorCode.Data_NoUpdate;
                eyeRays.left.error = ErrorCode.Data_NoUpdate;
                eyeRays.right.error = ErrorCode.Data_NoUpdate;
            }

            eyeConvergeRay = GetWorldSpaceConvergence(FoveManager.GetHmdCombinedGazeRay());
            eyeRays.left = CalculateWorldGazeVector(eyeOffsets.left, FoveManager.GetHmdGazeVector(Eye.Left));
            eyeRays.right = CalculateWorldGazeVector(eyeOffsets.right, FoveManager.GetHmdGazeVector(Eye.Right));
        }

        virtual protected bool ShouldRenderEye(Eye which)
        {
            if (eyeTargets == EyeTarget.Neither)
                return false;

            return eyeTargets != EyeTarget.Right && which == Eye.Left
                || eyeTargets != EyeTarget.Left && which == Eye.Right;
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

            var origCullMask = cam.cullingMask;
            var eyeCullMask = which == Eye.Left ? cullMaskLeft : cullMaskRight;
            cam.cullingMask = origCullMask & ~eyeCullMask;

            var eyePosOffset = eyeOffsets[which];

            // move the camera to the eye position
            transform.localPosition = hmdPosition + hmdOrientation * eyePosOffset;
            transform.localRotation = hmdOrientation;

            // move camera children inversely to keep the stereo projection effect
            foreach (Transform child in transform)
                child.localPosition -= eyePosOffset;

            cam.projectionMatrix = projectionMatrices[which];
            cam.targetTexture = targetTexture;

            cam.Render();

            cam.cullingMask = origCullMask;
            cam.targetTexture = null;
            cam.ResetProjectionMatrix();

            // reset camera position
            transform.localPosition = hmdPosition;
            transform.localRotation = hmdOrientation;

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
            cam = GetComponent<Camera>();
            if (!FoveSettings.CustomDesktopView && !FoveSettings.IsUsingOpenVR)
                cam.enabled = false;
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
            FoveManager.AddInUpdate += UpdateData;
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
            FoveManager.AddInUpdate -= UpdateData;
            if (registerCameraObject)
                RemoveCameraObject();
        }

        virtual protected void UpdatePose()
        {
            previousPose.position = Utils.ToVec3(transform.position);
            previousPose.rotation = Utils.ToQuat(transform.rotation);
            previousPose.scale = new Vec3(1, 1, 1);
            previousPose.velocity = Utils.ToVec3((transform.position - previousPosition) / Time.deltaTime);
            previousPosition = transform.position;
        }

        virtual protected void LateUpdate()
        {
            if (!cameraObjectRegistered)
                return;

            UpdatePose();
            var res = FoveManager.Headset.UpdateCameraObject(Id, ref previousPose);
            if (res.Failed)
                Debug.LogError("Update camera object pose failed. Error code=" + res);
        }

        virtual protected bool CanSee()
        {
            if (!eyeConvergeRay.IsValid)
                return false;

            var left = FoveManager.GetEyeState(Eye.Left);
            var right = FoveManager.GetEyeState(Eye.Right);

            var gazeCastPolicy = FoveManager.GazeCastPolicy;
            switch (gazeCastPolicy)
            {
                case GazeCastPolicy.DismissBothEyeClosed:
                    return (left.IsValid && left.value == EyeState.Opened) || (right.IsValid && right.value == EyeState.Opened);
                case GazeCastPolicy.DismissOneEyeClosed:
                    return left.IsValid && right.IsValid && left.value == EyeState.Opened && right.value == EyeState.Opened;
                case GazeCastPolicy.NeverDismiss:
                    return true;
            }

            throw new NotImplementedException("Unknown gaze cast policy '" + gazeCastPolicy + "'");
        }

        /****************************************************************************************************\
         * HELPER METHODS
        \****************************************************************************************************/

        protected Result<Ray> GetWorldSpaceConvergence(Result<Ray> localGaze)
        {
            var ray = localGaze.value;
            var worldOrigin = transform.TransformPoint(ray.origin);
            var worldDirection = transform.TransformDirection(ray.direction);
            var worldRay = new Ray(worldOrigin, worldDirection);

            return new Result<Ray>(worldRay, localGaze.error);
        }

        protected Result<Ray> CalculateWorldGazeVector(Vector3 eyeOffset, Result<Vector3> eyeVector)
        {
            var hmdToWorldMat = transform.localToWorldMatrix;
            return Utils.CalculateWorldGazeVector(ref hmdToWorldMat, ref eyeOffset, ref eyeVector);
        }

        protected bool InternalGazecastHelperSingle(Collider col, out RaycastHit hit, float maxDistance)
        {
            var ray = eyeConvergeRay.value;
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
            var ray = eyeConvergeRay.value;
            Debug.DrawRay(ray.origin, ray.direction, Color.blue, 2.0f);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask, queryTriggers);
        }

        protected RaycastHit[] InternalGazecastHelperAll(float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
        {
            if (!CanSee())
                return null;

            return Physics.RaycastAll(eyeConvergeRay.value, maxDistance, layerMask, queryTriggers);
        }

        protected int InternalGazecastHelperNonAlloc(RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggers)
        {
            if (!CanSee())
                return 0;

            return Physics.RaycastNonAlloc(eyeConvergeRay.value, results, maxDistance, layerMask, queryTriggers);
        }
    }
}