using System;
using System.Collections.Generic;

using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Fove interface class for all camera related queries and services.
    /// </summary>
    /// <remarks>This component requires a Camera component to work properly</remarks>
    [RequireComponent(typeof(Camera))]
    public partial class FoveInterface : MonoBehaviour
    {
        /// <summary>
        /// The player pose for the application
        /// </summary>
        [Serializable]
        public enum PlayerPose
        {
            Sitting,
            Standing,
        }

        /// <summary>
        /// Specify zero, one or several target eye(s).
        /// </summary>
        [Serializable]
        public enum EyeTarget
        {
            Neither,
            Left,
            Right,
            Both,
        }

        /// <summary>
        /// When true, fetch the gaze information from the fove HMD and convert it to world space
        /// </summary>
        [Tooltip("Turns off gaze tracking")]
        [SerializeField] public bool fetchGaze = true;

        /// <summary>
        /// When true, fetch the HMD orientation and push it to the game object transform
        /// </summary>
        [Tooltip("Turns off orientation tracking")]
        [SerializeField] public bool fetchOrientation = true;

        /// <summary>
        /// When true, fetch the HMD position and push it to the game object transform
        /// </summary>
        [Tooltip("Turns off position tracking (turns on position tracking when on)")]
        [SerializeField] public bool fetchPosition = true;

        /// <summary>
        /// Specify which eye this interface should render to.
        /// </summary>
        [Tooltip("Which eye(s) to render to.\n\nSelecting 'Left' or 'Right' will cause this camera to ONLY render to that eye. 'Both' is default. 'Neither' will prevent this camera from rendering to the HMD.\n\nNOTE: 'Neither' will not prevent the normal camera view from displaying.")]
        [SerializeField] public EyeTarget eyeTargets = EyeTarget.Both;

        /// <summary>
        /// Specify the current user pose (Standing or Siting)
        /// </summary>
        [Tooltip("How to interpret the HMD position:\n\nSitting: (Old default) The HMD is positioned relative to the tracking camera.\n\nStanding: An (system-configurable) offset is added to the 'Sitting' position. This option is more compatible with systems like SteamVR.")]
        [SerializeField] public PlayerPose poseType = PlayerPose.Standing;
        
        /// <summary>
        /// Unity layers to cull when rendering the left eye.
        /// </summary>
        [Tooltip("Don't draw any of the selected layers to the left eye.")]
        [SerializeField] public LayerMask cullMaskLeft;

        /// <summary>
        /// Unity layers to cull when rendering the right eye.
        /// </summary>
        [Tooltip("Don't draw any of the selected layers to the right eye.")]
        [SerializeField] public LayerMask cullMaskRight;

        /// <summary>
        /// Unity layers to ignore when performing gaze object detection.
        /// </summary>
        [Tooltip("Don't perform gaze object detection for the selected layers.")]
        [SerializeField] public LayerMask gazeCastCullMask;

        /// <summary>
        /// If true, automatically register (resp. unregister) camera object when this object get enabled (resp. disabled)
        /// </summary>
        [Tooltip("Automatically register the camera object associated to the FoveInterface")]
        [SerializeField] public bool registerCameraObject = true;

        /// <summary>
        /// If true, disable the compositor timewarp feature for the layer
        /// </summary>
        public bool TimewarpDisabled 
        {
            get { return disableTimewarp; }
            set 
            {
                if (disableTimewarp != value)
                {
                    disableTimewarp = value;
                    RecreateLayer();
                }
            }
        }

        /// <summary>
        /// Get the compositor rendering layer type
        /// </summary>
        public CompositorLayerType LayerType { get { return layerType; } }

        /// <summary>
        /// Get the camera used to render this layer to the HMD
        /// </summary>
        public Camera Camera { get { return cam; } }

        #region Gazecasting

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
                    return true;
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
            return InternalGazecastHelperAll(maxDistance, layerMask, queryTriggerInteraction);
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
            return InternalGazecastHelperNonAlloc(results, maxDistance, layerMask, queryTriggerInteraction);
        }

        #endregion

        /// <summary>
        /// Get eye rays describing where in the scene each of the user's eyes is looking.
        /// </remarks>
        /// <param name="immediate">If true re-query the value to the HMD, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>Eye rays describing the user's gaze in world space.</returns>
        public Result<Ray> GetGazeVector(Eye eye)
        {
            return eyeRays[eye];
        }

        /// <summary>
        /// Returns eyes gaze ray resulting from the two eye gazes combined together, in the world coordinate space.
        /// <para>
        /// To get individual eye rays use <see cref="GetGazeVector(Eye)"/> instead
        /// </para>
        /// <para>
        /// To get the user gaze in the headse coordinate space use the <see cref="FoveManager.GetCombinedGazeRay()"/> instead. 
        /// </para>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The combined gaze ray in the world coordinate space, and the call success status: 
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public Result<Ray> GetCombinedGazeRay()
        {
            return eyeConvergeRay;
        }

        /// <summary>
        /// Returns the depth of the user gaze in the world coordinate space 
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.GazeDepth"/> should be registered to use this function.</remarks>
        /// <returns>The depth of the user in the world coordinate space, and the call success status: 
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public Result<float> GetCombinedGazeDepth()
        {
            return FoveManager.Headset.GetCombinedGazeDepth();
        }

        /// <summary>
        /// Register the camera associated to this interface into the object registry.
        /// <para>Call this function if you want the Fove system to perform gaze object detection from this fove interface camera point of view</para>
        /// </summary>
        /// <remarks>You don't need to call this function if <see cref="registerCameraObject"/> is set to true</remarks>
        virtual public Result RegisterCameraObject()
        {
            UpdatePose();

            var camera = new CameraObject
            {
                id = Id,
                pose = previousPose,
                groupMask = (ObjectGroup)(Camera.cullingMask & ~gazeCastCullMask)
            };

            var result = FoveManager.Headset.RegisterCameraObject(camera);
            cameraObjectRegistered = result.Succeeded;
            previousPosition = transform.position;

            return result;
        }

        /// <summary>
        /// Update the registered the camera object. 
        /// <para>Should you call this function if you modified some of the properties of the object after registering it (e.g. gaze cull mask, etc.)</para>
        /// </summary>
        virtual public Result UpdateCameraObject()
        {
            var resultRemove = RemoveCameraObject();
            if (resultRemove.Failed)
                return resultRemove;

            return RegisterCameraObject();
        }

        /// <summary>
        /// Remove the camera object associated to this fove interface from the object registry
        /// </summary>
        virtual public Result RemoveCameraObject()
        {
            if (!cameraObjectRegistered)
                return new Result();

            var result = FoveManager.Headset.RemoveCameraObject(Id);
            if (result.Succeeded)
                cameraObjectRegistered = false;

            return result;
        }
    }
}
