
using UnityEngine;

namespace Fove.Unity
{
	// FoveManager needs to reference a gameObject in order to create coroutines.
	public partial class FoveManager : MonoBehaviour
	{
		/// <summary>
		/// Get a reference to the Fove headset class.
		/// </summary>
		public static Headset Headset { get { return Instance.m_headset; } }
		
		public delegate void AddInDelegate();

		/// <summary>
		/// Use this event to register add-in update callback delegates.
		/// The delegate is called every end of HMD data update.
		/// </summary>
		public static event AddInDelegate AddInUpdate;
		
		/// <summary>
		/// Get or set the world scale. Default value <c>1</c>.
		/// </summary>
		public float WorldScale
		{
			get { return m_worldScale; }
			set { m_worldScale = Mathf.Max(float.Epsilon, value); }
		}

		/// <summary>
		/// Get or set the render scale. 
		/// </summary>
		public float RenderScale
		{
			get { return m_renderScale; }
			set { m_renderScale = Mathf.Max(float.Epsilon, value); }
		}

		/// <summary>
		/// Check which eyes are closed. Returns Left/Right/Both/None accordingly.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>Eyes that are closed</returns>
		public static Eye CheckEyesClosed(bool immediate = false)
		{
			if (!immediate)
				return m_sEyeClosed;

			Eye result;
			Instance.m_headset.CheckEyesClosed(out result);
			return result;
		}

		/// <summary>
		/// Returns the pupil dilation value as a ratio relative to a baseline. 1 means average. Range: 0 to Infinity
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The average pupil dilation value of both eyes</returns>
		public static float GetPupilDilation(bool immediate = false)
		{
			if (!immediate)
				return m_sPupilDilation;

			Fove.GazeConvergenceData convergence;
			Instance.m_headset.GetGazeConvergence(out convergence);
			return convergence.pupilDilation;
		}

		/// <summary>
		/// Returns <c>true</c> when the user is looking at something (fixation or pursuit), rather than saccading between objects.
		/// This could be used to suppress eye input during large eye motions. 
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>Whether the uers's gaze is fixated on something, as opposed to saccading.</returns>
		public static bool IsGazeFixated(bool immediate = false)
		{
			if(!immediate)
				return m_sGazeFixated;

			Fove.GazeConvergenceData convergence;
			Instance.m_headset.GetGazeConvergence(out convergence);
			return convergence.attention;
		}

		/// <summary>
		/// Get the HMD current pose. By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <remarks>
		/// This value is automatically applied to the interface's GameObject and is only exposed 
		/// here for reference and out-of-sync access
		/// </remarks>
		/// <returns>The HMD current pose</returns>
		public static Pose GetHMDPose(bool immediate = false)
		{
			if (!immediate)
				return _sLastPose;

			Pose pose;
			Instance.m_headset.GetLatestPose(out pose);
			return pose;
		}

		/// <summary>
		/// Get the HMD current rotation. 
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <remarks>
		/// This value is automatically applied to the interface's GameObject and is only exposed 
		/// here for reference and out-of-sync access
		/// </remarks>
		/// <returns>The rotation in the HMD local coordinate space</returns>
		public static Quaternion GetHMDRotation(bool immediate = false)
		{
			if (!immediate)
				return m_sHeadRotation;

			var pose = GetHMDPose(true);
			return pose.orientation.ToQuaternion();
		}

		/// <summary>
		/// Get the HMD current position in local coordinates. 
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <remarks>
		/// This value is automatically applied to the interface's GameObject and is only exposed 
		/// here for reference and out-of-sync access
		/// </remarks>
		/// <returns>The position in the HMD local coordinate space.</returns>
		public static Vector3 GetHMDPosition(bool immediate = false)
		{
			if (!immediate)
				return m_sHeadPosition;

			var pose = GetHMDPose(true);
			return pose.position.ToVector3();
		}

		/// <summary>
		/// Returns the data that describes the current convergence point of the eyes.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The gaze convergence information in the HMD local coordinate space.</returns>
		public static GazeConvergenceData GetHMDGazeConvergence(bool immediate = false)
		{
			if (!immediate)
				return m_sConvergenceData;

			Fove.GazeConvergenceData convergence;
			Instance.m_headset.GetGazeConvergence(out convergence);
			return convergence;
		}

		/// <summary>
		/// Get the current direction the left eye is looking at.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The direction of left eye gaze in the HMD local coordinate space.</returns>
		public static Vector3 GetLeftEyeVector(bool immediate = false)
		{
			if (!immediate)
				return m_sEyeVecLeft;

			GazeVector lGaze, rGaze;
			Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
			return lGaze.vector.ToVector3();
		}

		/// <summary>
		/// Get the current direction the right eye is looking at.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The direction of right eye gaze in the HMD local coordinate space.</returns>
		public static Vector3 GetRightEyeVector(bool immediate = false)
		{
			if (!immediate)
				return m_sEyeVecRight;

			GazeVector lGaze, rGaze;
			Instance.m_headset.GetGazeVectors(out lGaze, out rGaze);
			return rGaze.vector.ToVector3();
		}

		/// <summary>
		/// Get the current position offset of the left eye.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The position of left eye in the HMD local coordinate space.</returns>
		public static Vector3 GetLeftEyeOffset(bool immediate = false)
		{
			if (!immediate)
				return m_sLeftEyeOffset;

			Vector3 eyeOffset;
			Matrix44 lEyeMx, rEyeMx;
			Instance.m_headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
			GetEyeOffsetVector(ref lEyeMx, out eyeOffset);
			return eyeOffset;
		}

		/// <summary>
		/// Get the current position offset of the right eye.
		/// By default, it returns the value of the current frame.
		/// Set <paramref name="immediate"/> to <c>true</c> to re-query the latest value to the HMD.
		/// </summary>
		/// <returns>The position of right eye in the HMD local coordinate space.</returns>
		public static Vector3 GetRightEyeOffset(bool immediate = false)
		{
			if (!immediate)
				return m_sRightEyeOffset;

			Vector3 eyeOffset;
			Matrix44 lEyeMx, rEyeMx;
			Instance.m_headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
			GetEyeOffsetVector(ref rEyeMx, out eyeOffset);
			return eyeOffset;
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

			left = fv_l.ToMatrix4x4();
			right = fv_r.ToMatrix4x4();
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
		/// Connect to the FOVE Compositor system. This will hard reset the compositor connection even if there is
		/// already a valid connection. You could call this after a call to `DisconnectCompositor` if you found any
		/// significant reason to do so.
		/// </summary>
		public static void ConnectCompositor()
		{
			foreach (var eyeTextures in m_eyeTextures.Values)
				eyeTextures.areNew = true; // force to reset eye texture

			ResetNativeState();
		}

		/// <summary>
		/// Disconnect and destroy the underlying compositor system. This leaves your game in a state where no data is
		/// being sent on to the FOVE compositor. You might call this in situations where you want to disable VR, or if
		/// you know that another program will be trying to take control of the HMD's screen. If you want to reconnect,
		/// you would call `ConnectCompositor`.
		/// </summary>
		public static void DisconnectCompositor()
		{
			foreach (var stack in m_interfaceStacks.Values)
				unregisteredInterfaces.AddRange(stack);

			DestroyNativeResources();
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
	}
}
