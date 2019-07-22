using System;
using System.Collections.Generic;

using UnityEngine;

namespace Fove.Unity
{
	[RequireComponent(typeof(Camera))]
	public partial class FoveInterface : MonoBehaviour
	{
		[Serializable]
		public enum PlayerPose
		{
			Sitting,
			Standing,
		}

		[Serializable]
		public enum GazeCastPolicy
		{
			DismissWhenBothEyesClosed,
			DismissWhenOneEyeClosed,
			NeverDismiss,
		}

		/*
		 * MonoBehaviour Implementation
		 * 
		 * The pieces required and used by the MonoBehaviour subclass when attached to a GameObject in the
		 * scene. This behaviour does not function unless there is a single instance of this class
		 * instantiated in the scene.
		 */

		/// <summary>
		/// When true, fetch the gaze information from the fove HMD
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
		[SerializeField] public Eye eyeTargets = Eye.Both;

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
		/// Specify how gaze cast collision should be dismissed based on the user closed eye state. 
		/// </summary>
		[Tooltip("Specify how gaze cast collisions should be dismissed when the user closed his eyes")]
		[SerializeField] public GazeCastPolicy gazeCastPolicy;

		public bool TimewarpDisabled { get { return disableTimewarp; } }
		public bool FadingDisabled { get { return disableFading; } }
		public bool DistortionDisabled { get { return disableDistortion; } }
		public CompositorLayerType LayerType { get { return layerType; } }

		public Camera Camera { get { return _cam; } }

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
		/// Get eye rays describing where in the scene each of the user's eyes are looking.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>Eye rays describing the user's gaze in world space.</returns>
		public EyeRays GetGazeRays(bool immediate=false)
		{
			if(!immediate)
				return new EyeRays(_eyeRayLeft, _eyeRayRight);

			var leftEyeVector = fetchGaze? FoveManager.GetLeftEyeVector(true): Vector3.forward;
			var rightEyeVector = fetchGaze? FoveManager.GetRightEyeVector(true): Vector3.forward;
			var leftEyeOffset = FoveManager.GetLeftEyeOffset(true);
			var rightEyeOffset = FoveManager.GetRightEyeVector(true);

			Ray left, right;
			CalculateGazeRays(ref leftEyeOffset, ref rightEyeOffset, ref leftEyeVector, ref rightEyeVector, out left, out right);

			return new EyeRays(left, right);
		}

		/// <summary>
		/// Returns the current convergence point of the eyes. See the description of the
		/// <see cref="GazeConvergenceData"/> for more detail on how to use this information.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The gaze convergence in world space.</returns>
		public GazeConvergenceData GetGazeConvergence(bool immediate = false)
		{
			if(!immediate)
				return _eyeConverge;

			var localGaze = FoveManager.GetHMDGazeConvergence(true);
			return GetWorldSpaceConvergence(ref localGaze.ray, localGaze.distance);
		}
	}
}
