
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Static class used to control the Fove system.
    /// <para>Contains all camera-unrelated queries and commands</para>
    /// </summary>
    public partial class FoveManager : MonoBehaviour
    {
        /// <summary>
        /// Get a reference to the Fove headset class.
        /// </summary>
        public static Headset Headset { get { return Instance.m_headset; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is connected
        /// See <see cref="IsHardwareConnected(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareConnected { get { return m_sWaitForHardwareConnected; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is disconnected
        /// See <see cref="IsHardwareConnected(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareDisconnected { get { return m_sWaitForHardwareDisconnected; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is ready to be used.
        /// See <see cref="IsHardwareReady(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareReady { get { return m_sWaitForHardwareReady; } }

        /// <summary>
        /// Use this to pause a coroutine until a eye tracking calibration process is started.
        /// See <see cref="IsEyeTrackingCalibrating(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrationStart { get { return m_sWaitForCalibrationStart; } }

        /// <summary>
        /// Use this to pause a coroutine until a eye tracking calibration process is ended.
        /// See <see cref="IsEyeTrackingCalibrating(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrationEnd { get { return m_sWaitForCalibrationEnd; } }

        /// <summary>
        /// Use this to pause a coroutine until the eye tracking system is calibrated.
        /// See <see cref="IsEyeTrackingCalibrated(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrated { get { return m_sWaitForCalibrationCalibrated; } }

        /// <summary>
        /// Use this to pause a coroutine until the user is wearing the headset.
        /// See <see cref="IsUserPresent(bool)"/> for more details.
        /// </summary>
        public static IEnumerator WaitForUser { get { return m_sWaitForUser; } }

        /// <summary>
        /// Use this event to register add-in update callbacks.
        /// The event is triggered after the HMD data update at the very end of each frame.
        /// </summary>
        public static event Action AddInUpdate;

        /// <summary>
        /// Triggered when the hardware get connected  (e.g. when the value return by <see cref="IsHardwareConnected"/> changes to <c>true</c>).
        /// </summary>
        public static event Action HardwareConnected;

        /// <summary>
        /// Triggered when the hardware get disconnected  (e.g. when the value return by <see cref="IsHardwareConnected"/> changes to <c>false</c>).
        /// </summary>
        public static event Action HardwareDisconnected;

        /// <summary>
        /// Triggered when the hardware is ready to be used (e.g. when the value return by <see cref="IsHardwareReady"/> changes to <c>true</c>).
        /// </summary>
        public static event Action HardwareIsReady;

        /// <summary>
        /// Triggered when the eye tracking calibration process starts (e.g. when the value returned by <see cref="IsEyeTrackingCalibrating"/> changes to <c>true</c>).
        /// </summary>
        public static event Action EyeTrackingCalibrationStarted;

        /// <summary>
        /// Triggered when the eye tracking calibration process ends (e.g. the value returned by <see cref="IsEyeTrackingCalibrating"/> changes to <c>false</c>).
        /// It provides the state of the calibration at the end of the process. This can be used to handle calibration failure or success quality cases.
        /// </summary>
        public static event Action<CalibrationState> EyeTrackingCalibrationEnded;

        /// <summary>
        /// Triggered when the user starts or stops fixating (e.g. when the value returned by <see cref="IsGazeFixated(bool)"/> changes).
        /// </summary>
        public static event Action<bool> IsGazeFixatedChanged;

        /// <summary>
        /// Triggered when the user eye closed status changes (e.g. when the value returned by <see cref="CheckEyesClosed(bool)"/> changes).
        /// </summary>
        public static event Action<Eye> EyesClosedChanged;

        /// <summary>
        /// Triggered when the user put or remove the headset (e.g. when the value returned by <see cref="IsUserPresent(bool)"/> changes).
        /// </summary>
        public static event Action<bool> UserPresenceChanged;

        /// <summary>
        /// Triggered when the HMD adjusment Gui visibility status changed (e.g. when the value returned by <see cref="IsHmdAdjustementGuiVisible(bool)"/> changes).
        /// </summary>
        public static event Action<bool> HmdAdjustmentGuiVisibilityChanged;

        /// <summary>
        /// Get or set the world scale. Default value <c>1</c>.
        /// </summary>
        public static float WorldScale
        {
            get { return m_worldScale; }
            set { m_worldScale = Mathf.Max(float.Epsilon, value); }
        }

        /// <summary>
        /// Get or set the render scale. 
        /// </summary>
        public static float RenderScale
        {
            get { return m_renderScale; }
            set { m_renderScale = Mathf.Max(float.Epsilon, value); }
        }

        /// <summary>
        /// Specify how gaze cast collision should be dismissed based on the user closed eye state.
        /// </summary>
        /// <remarks>Default value is taken from the Fove settings.</remarks>
        public static GazeCastPolicy GazeCastPolicy
        {
            get { return Headset.GazeCastPolicy; }
            set { Headset.GazeCastPolicy = value; }
        }

        /// <summary>
        /// Check which eyes are closed. Returns Left/Right/Both/None accordingly.
        /// </summary>
        /// <returns>The eyes that are closed</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<Eye> CheckEyesClosed(bool immediate = false)
        {
            if (!immediate)
                return m_sEyeClosed;

            Result<Eye> result;
            result.error = Headset.CheckEyesClosed(out result.value);
            if (result.HasError)
                result.value = m_sEyeClosed;

            return result;
        }

        /// <summary>
        /// Returns the pupil dilation value as a ratio relative to a baseline. 1 means average. Range: 0 to Infinity
        /// </summary>
        /// <returns>The average pupil dilation value of the two eyes</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<float> GetPupilDilation(bool immediate = false)
        {
            if (!immediate)
                return m_sPupilDilation;

            Fove.GazeConvergenceData convergence;
            var error = Headset.GetGazeConvergence(out convergence);

            if (error != ErrorCode.None)
                return new Result<float>(m_sPupilDilation, error);

            return new Result<float>(convergence.pupilDilation);
        }

        /// <summary>
        /// Returns <c>true</c> when the user is looking at something (fixation or pursuit) and <c>false</c> when saccading between objects.
        /// This could be used to suppress eye input during large eye motions. 
        /// </summary>
        /// <returns>True if the user gaze is fixated on something, false otherwise.</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<bool> IsGazeFixated(bool immediate = false)
        {
            if(!immediate)
                return m_sIsGazeFixated;

            Fove.GazeConvergenceData convergence;
            var error = Headset.GetGazeConvergence(out convergence);

            if (error != ErrorCode.None)
                return new Result<bool>(m_sIsGazeFixated, error);

            return new Result<bool>(convergence.attention);
        }

        /// <summary>
        /// Returns <c>true</c> when the user is wearing the headset
        /// </summary>
        /// <remarks>When user is not present Eye tracking values shouldn't be used as invalid.</remarks>
        /// <returns>True if the user has been detected, false otherwise</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<bool> IsUserPresent(bool immediate = false)
        {
            if (!immediate)
                return m_sIsUserPresent;

            bool userPresent;
            var error = Headset.IsUserPresent(out userPresent);

            if (error != ErrorCode.None)
                return new Result<bool>(m_sIsUserPresent, error);

            return new Result<bool>(userPresent);
        }

        /// <summary>
        /// Returns <c>true</c> when the GUI that asks the user to adjust their headset is being displayed
        /// </summary>
        /// <remarks>It is best practice to pause the gameplay of the application when the HMD adjustment GUI is being displayed.</remarks>
        /// <returns>True if the HMD adjustment GUI is currently displayed to the user, false otherwise</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<bool> IsHmdAdjustementGuiVisible(bool immediate = false)
        {
            if (!immediate)
                return m_sIsHmdAdjustmentVisible;

            bool guiVisible;
            var error = Headset.IsHmdAdjustmentGuiVisible(out guiVisible);

            if (error != ErrorCode.None)
                return new Result<bool>(m_sIsHmdAdjustmentVisible, error);

            return new Result<bool>(guiVisible);
        }

        /// <summary>
        /// Get the game object currently gazed by the user.
        /// </summary>
        /// <returns>The gazed game object if any. Null otherwise.</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<GameObject> GetGazedObject(bool immediate = false)
        {
            var resultId = m_sGazedObjectId;

            if (immediate)
            {
                Fove.GazeConvergenceData convergence;
                var error = Headset.GetGazeConvergence(out convergence);
                if (error != ErrorCode.None)
                    return new Result<GameObject>(null, error);

                resultId = new Result<int>(convergence.gazedObjectId);
            }

            var gazableObject = GazableObject.FindGazableObject(resultId);
            var gameObject = gazableObject != null ? gazableObject.gameObject : null;

            return new Result<GameObject>(gameObject, resultId.error);
        }

        /// <summary>
        /// Get the headset current pose in local coordinate space.
        /// </summary>
        /// <remarks>
        /// This value is automatically applied to the interface's GameObject and is only exposed here for reference and out-of-sync access
        /// </remarks>
        /// <returns>The pose in the headset local coordinate space</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<Pose> GetHMDPose(bool immediate = false)
        {
            if (!immediate)
                return new Result<Pose>(m_sLastPose);

            Result<Pose> result;
            result.error = Headset.GetLatestPose(out result.value);
            if (result.HasError)
                result.value = m_sLastPose;

            return result;
        }

        /// <summary>
        /// Get the headset current rotation in local coordinate space.
        /// </summary>
        /// <remarks>
        /// This value is automatically applied to the interface's GameObject and is only exposed  here for reference and out-of-sync access
        /// </remarks>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>The rotation in the headset local coordinate space</returns>
        public static Result<Quaternion> GetHMDRotation(bool immediate = false)
        {
            if (!immediate)
                return new Result<Quaternion>(m_sHeadRotation);

            var poseResult = GetHMDPose(true);
            var orientation = poseResult.value.orientation.ToQuaternion(); // contains the same value as m_sHeadRotation in the case of failure
            return new Result<Quaternion>(orientation, poseResult.error);
        }

        /// <summary>
        /// Get the headset current position in local coordinates.
        /// </summary>
        /// <remarks>
        /// This value is automatically applied to the interface's GameObject and is only exposed here for reference and out-of-sync access
        /// </remarks>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>The position in the headset local coordinate space.</returns>
        public static Result<Vector3> GetHMDPosition(bool immediate = false)
        {
            if (!immediate)
                return new Result<Vector3>(m_sHeadPosition);

            var poseResult = GetHMDPose(true);
            var position = m_worldScale * poseResult.value.position.ToVector3();
            return new Result<Vector3>(position, poseResult.error);
        }

        /// <summary>
        /// Returns user's eyes current convergence point in local coordinate space.
        /// </summary>
        /// <remarks>
        /// To get user gaze in world space coordinate use the <see cref="FoveInterface.GetGazeConvergence(bool)"/>. 
        /// This value is exposed here only for reference and out-of-sync access
        /// </remarks>
        /// <returns>The gaze convergence information in the headset local coordinate space.</returns>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <seealso cref="FoveInterface.GetGazeConvergence(bool)"/>
        public static Result<GazeConvergenceData> GetHMDGazeConvergence(bool immediate = false)
        {
            if (!immediate)
                return m_sConvergenceData;

            Fove.GazeConvergenceData convergence;
            var error = Headset.GetGazeConvergence(out convergence);
            if (error != ErrorCode.None)
                return new Result<GazeConvergenceData>(m_sConvergenceData, error);

            return new Result<GazeConvergenceData>((GazeConvergenceData)convergence);
        }

        /// <summary>
        /// Get the direction the left and right eyes are currently looking at.
        /// </summary>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>The direction of the left and right eye gaze in the headset local coordinate space.</returns>
        public static Result<Stereo<Vector3>> GetEyeVectors(bool immediate = false)
        {
            if (!immediate)
                return m_sEyeVectors;

            GazeVector lGaze, rGaze;
            var error = Headset.GetGazeVectors(out lGaze, out rGaze);
            if (error != ErrorCode.None)
                return new Result<Stereo<Vector3>>(m_sEyeVectors, error);

            var eyeVectors = new Stereo<Vector3>(lGaze.vector.ToVector3(), rGaze.vector.ToVector3());
            return new Result<Stereo<Vector3>>(eyeVectors);
        }

        /// <summary>
        /// Get the offsets to the origin of the headset local space of the left and right eyes.
        /// </summary>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>The position of left and right eye in the headset local coordinate space.</returns>
        public static Result<Stereo<Vector3>> GetEyeOffsets(bool immediate = false)
        {
            if (!immediate)
                return m_sEyeOffsets;

            Matrix44 lEyeMx, rEyeMx;
            var error = Headset.GetEyeToHeadMatrices(out lEyeMx, out rEyeMx);
            if (error != ErrorCode.None)
                return new Result<Stereo<Vector3>>(m_sEyeOffsets, error);

            Vector3 eyeOffsetLeft, eyeOffsetRight;
            GetEyeOffsetVector(ref lEyeMx, out eyeOffsetLeft);
            GetEyeOffsetVector(ref lEyeMx, out eyeOffsetRight);
            return new Result<Stereo<Vector3>>(new Stereo<Vector3>(eyeOffsetLeft, eyeOffsetRight));
        }

        /// <summary>
        /// Return left and right eyes projection matrices given a near and far clipping plane value. 
        /// This can change from frame to frame, so it's good practice to query this before rendering VR cameras.
        /// </summary>
        /// <remarks>
        /// This is called automatically by each FoveInterface before rendering is done for that frame, and so
        /// you shouldn't need to call this method directly
        /// </remarks>
        /// <param name="near">Distance to the near-clip plane of the projection frustum</param>
        /// <param name="far">Distance to the far-clip plane of the projection frustum</param>
        /// <returns>The left and right eye projection matrices</returns>
        public static Result<Stereo<Matrix4x4>> GetProjectionMatrices(float near, float far)
        {
            Matrix44 projLeft, projRight;
            var error = Headset.GetProjectionMatricesRH(near, far, out projLeft, out projRight);
            var matrices = new Stereo<Matrix4x4>(projLeft.ToMatrix4x4(), projRight.ToMatrix4x4());
            return new Result<Stereo<Matrix4x4>>(matrices, error);
        }

        /// <summary>
        /// Reset the headset orientation.
        /// <para>
        /// This sets the HMD's current rotation as a "zero" orientation, essentially resetting their
        /// orientation to that set in the editor.
        /// </para>
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result TareOrientation()
        {
            var error = Headset.TareOrientationSensor();
            return new Result(error);
        }

        /// <summary>
        /// Reset the headset position.
        /// <para>
        /// This sets the HMD's current position relative to the tracking camera as a "zero" position,
        /// essentially jumping the headset back to the interface's origin.
        /// </para>
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result TarePosition()
        {
            var error = Headset.TarePositionSensors();
            return new Result(error);
        }

        /// <summary>
        /// Start the eye tracking calibration process.
        /// </summary>
        /// <remarks>
        /// After calling this function you should assume that the user cannot see your game until 
        /// <see cref="IsEyeTrackingCalibrating"/> returns false.
        /// </remarks>
        /// <param name="calibrationOptions">Specify the calibration options for the new calibration process to run</param>
        /// <returns>The result of the operation</returns>
        public static Result StartEyeTrackingCalibration(CalibrationOptions calibrationOptions = null)
        {
            var error = Headset.StartEyeTrackingCalibration(calibrationOptions);
            return new Result(error);
        }

        /// <summary>
        /// Stops a running calibration process. Does nothing if no calibration process is currently running.
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result StopEyeTrackingCalibration()
        {
            var error = Headset.StopEyeTrackingCalibration();
            return new Result(error);
        }

        /// <summary>
        /// Return the state of the current calibration process.
        /// </summary>
        public static Result<CalibrationState> GetEyeTrackingCalibrationState()
        {
            return m_sCalibationState;
        }

        /// <summary>
        /// Tick the current calibration process and retrieve data information to render the current calibration state.
        /// <para>
        /// This function is how the client declares to the calibration system that is available to render calibration.
        /// The calibration system determines which of the available renderers has the highest priority,
        /// and returns to that render the information needed to render calibration via the outTarget parameter.
        /// Even while ticking this, you may get no result because either no calibration is running,
        /// or a calibration is running but some other higher priority renderer is doing the rendering.
        /// </para>
        /// <para>
        /// Note that it is perfectly fine not to call this function, in which case the Fove service will automatically render the calibration process for you.
        /// </para>
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last rendered frame</param>
        /// <param name="isVisible">Indicate to the calibration system that something is being drawn to the screen. 
        /// This allows the calibration renderer to take as much time as it wants to display success/failure messages 
        /// and animate away before the calibration processes is marked as completed by the `IsEyeTrackingCalibrating` function.</param>
        /// <returns>
        /// The calibration current state information or one of the following errors:
        /// <list type="bullet">
        /// <item>License_FeatureAccessDenied: if you don't have the license level required to access this feature</item>
        /// <item>Calibration_OtherRendererPrioritized: if another process has currently the priority for rendering calibration process</item>
        /// </list>
        /// </returns>
        /// <remarks>This feature requires a license</remarks>
        public static Result<CalibrationData> TickEyeTrackingCalibration(float deltaTime, bool isVisible)
        {
            Fove.CalibrationData foveData;
            var error = Headset.TickEyeTrackingCalibration(deltaTime, isVisible, out foveData);
            return new Result<CalibrationData>((CalibrationData)foveData, error);
        }

        /// <summary>
        /// Connect to the FOVE Compositor system. This will hard reset the compositor connection even if there is
        /// already a valid connection. You could call this after a call to `DisconnectCompositor` if you found any
        /// significant reason to do so.
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result ConnectCompositor()
        {
            foreach (var eyeTextures in m_sEyeTextures.Values)
                eyeTextures.areNew = true; // force to reset eye texture

            UnityFuncs.ResetNativeState();
            RegisterCapabilities(m_sCurrentCapabilities);
            return new Result(ErrorCode.None);
        }

        /// <summary>
        /// Disconnect and destroy the underlying compositor system. This leaves your game in a state where no data is
        /// being sent on to the FOVE compositor. You might call this in situations where you want to disable VR, or if
        /// you know that another program will be trying to take control of the HMD's screen. If you want to reconnect,
        /// you would call `ConnectCompositor`.
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result DisconnectCompositor()
        {
            foreach (var stack in m_sInterfaceStacks.Values)
                m_sUnregisteredInterfaces.AddRange(stack);

            UnityFuncs.DestroyNativeResources();
            return new Result(ErrorCode.None);
        }

        /// <summary>
        /// Query whether or not a headset is physically connected to the computer
        /// </summary>
        /// <returns>Whether or not a headset is present on the machine.</returns>
        public static Result<bool> IsHardwareConnected(bool immediate = false)
        {
            if (!immediate)
                return m_sIsHardwareConnected;

            Result<bool> result;
            result.error = Headset.IsHardwareConnected(out result.value);
            if (result.HasError)
                result.value = m_sIsHardwareConnected;

            return result;
        }

        /// <summary>
        /// Query whether the headset has all requested features booted up and running (position tracking, eye tracking,
        /// orientation, etc...).
        /// </summary>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>Whether you can expect valid data from all headset functions.</returns>
        public static Result<bool> IsHardwareReady(bool immediate = false)
        {
            if (!immediate)
                return m_sIsHardwareReady;

            Result<bool> result;
            result.error = Headset.IsHardwareReady(out result.value);
            if (result.HasError)
                result.value = m_sIsHardwareReady;

            return result;
        }

        /// <summary>
        /// Query whether eye tracking is calibrated and usable or not.
        /// </summary>
        /// <remarks>If you are using profiles to save calibration results, the eye tracking system is already calibrated at the launch of the application/service</remarks>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>Whether eye tracking has been calibrated.</returns>
        public static Result<bool> IsEyeTrackingCalibrated(bool immediate = false)
        {
            if (!immediate)
                return m_sIsCalibrated;

            Result<bool> result;
            result.error = Headset.IsEyeTrackingCalibrated(out result.value);
            if (result.HasError)
                result.value = m_sIsCalibrated;

            return result;
        }

        /// <summary>
        /// Query whether eye tracking is currently calibrating, meaning that the user likely
        /// cannot see your game due to the calibration process. While this is true, you should
        /// refrain from showing any interactions that would respond to eye gaze.
        /// </summary>
        /// <remarks>
        /// You should carefully check the value returned by this function as the calibration process
        /// can be manually started by the user at any time during your game. 
        /// Other than user manual triggering, times when calibration will occur are:
        /// <list type="number">
        /// <item>At the launch of you application, if you check 'Force Calibration' in the fove settings.</item>
        /// <item>Whenever you call <see cref="EnsureEyeTrackingCalibration(CalibrationMethod)"/> 
        /// or <see cref="StartEyeTrackingCalibration(bool, CalibrationMethod)"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        /// <returns>Whether eye tracking is calibrating.</returns>
        public static Result<bool> IsEyeTrackingCalibrating(bool immediate = false)
        {
            if (!immediate)
                return m_sIsCalibrating;

            Result<bool> result;
            result.error = Headset.IsEyeTrackingCalibrating(out result.value);
            if (result.HasError)
                result.value = m_sIsCalibrating;

            return result;
        }

        /// <summary>
        /// Get the version of the Fove client library. This returns "[major].[minor].[build]".
        /// </summary>
        /// <returns>A string representing the client library version</returns>
        public static Result<string> GetClientVersion()
        {
            Versions versions;
            var error = Headset.GetSoftwareVersions(out versions);

            var versionString = error == ErrorCode.None 
                ? "" + versions.clientMajor + "." + versions.clientMinor + "." + versions.clientBuild
                : "Unknown";

            return new Result<string>(versionString, error);
        }

        /// <summary>
        /// Get the version of the installed runtime service. This returns "[major].[minor].[build]".
        /// </summary>
        /// <returns>A string representing the runtime library version</returns>
        public static Result<string> GetRuntimeVersion()
        {
            Versions versions;
            var error = Headset.GetSoftwareVersions(out versions);

            var versionString = error == ErrorCode.None
                ? "" + versions.runtimeMajor + "." + versions.runtimeMinor + "." + versions.runtimeBuild
                : "Unknown";

            return new Result<string>(versionString, error);
        }

        /// <summary>
        /// Check whether the runtime and client versions are compatible and are expected to work correctly.
        /// </summary>
        /// <returns>Any errors that occurred when checking the runtime and client versions, or None if 
        /// everything seems fine.</returns>
        /// <remarks>Newer runtime versions are designed to be compatible with older client versions, however new
        /// client versions are not designed to be compatible with old runtime versions.</remarks>
        public static Result<ErrorCode> CheckSoftwareVersions()
        {
            return new Result<ErrorCode>(Headset.CheckSoftwareVersions());
        }

        /// <summary>
        /// Register a headset capability independently from the <see cref="FoveInterface"/> needs.
        /// </summary>
        /// <param name="capabilities">The capabilities to add</param>
        public static void RegisterCapabilities(ClientCapabilities capabilities)
        {
            m_sEnforcedCapabilities |= capabilities;
            UpdateCapabilities();
        }

        /// <summary>
        /// Unregister a capability previously registered with <see cref="RegisterCapabilities(ClientCapabilities)"/>.
        /// </summary>
        /// <remarks>The capability will be effectively unregistered only if not needed by the <see cref="FoveInterface"/> game instances</remarks>
        /// <param name="capabilities">The capabilities to remove </param>
        public static void UnregisterCapabilities(ClientCapabilities capabilities)
        {
            m_sEnforcedCapabilities &= ~capabilities;
            UpdateCapabilities();
        }



        /// <summary>
        /// Creates a new profile
        /// <para>
        /// The FOVE system keeps a set of profiles, such that users.
        /// Eye tracking calibration, for example, is saved to the users profile.
        /// Profiles persist to disk and survive restart, etc.
        /// Third party applications can control the profile system and store data within it.
        /// </para>
        /// </summary>
        /// <remarks>This function creates a new profile, but does not add any data or switch to it.</remarks>
        /// <param name="newName">Unique name of the profile to create</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the profile was successfully created</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Profile_InvalidName"/> if newName was invalid</item>
        /// <item><see cref="ErrorCode.Profile_NotAvailable"/> if the name is already taken</item>
        /// <item><see cref="ErrorCode.API_NullInPointer"/> if newName is null</item>
        /// </list>
        /// </returns>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetCurrentProfile()"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result CreateProfile(string newName)
        {
            return new Result(Headset.CreateProfile(newName));
        }

        /// <summary>
        /// Renames an existing profile. This works on the current profile as well.
        /// </summary>
        /// <param name="newName">name of the profile to be renamed</param>
        /// <param name="oldName">unique new name of the profile</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the profile was successfully renamed</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Profile_DoesntExist"/> if the requested profile at oldName doesn't exist</item>
        /// <item><see cref="ErrorCode.Profile_NotAvailable"/> if the name is already taken</item>
        /// <item><see cref="ErrorCode.API_InvalidArgument"/> if the old name and new name are the same</item>
        /// <item><see cref="ErrorCode.API_NullInPointer"/> if oldName or newName is null</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetCurrentProfile()"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result RenameProfile(string oldName, string newName)
        {
            return new Result(Headset.RenameProfile(oldName, newName));
        }


        /// <summary>
        /// Deletes an existing profile. This works on the current profile as well.
        /// <para>
        /// If the deleted profile is the current profile, then no current profile is set after this returns.
        /// In such a case, it is undefined whether any existing profile data loaded into memory may be kept around.
        /// </para>
        /// </summary>
        /// <param name="profileName">name of the profile to be deleted</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the profile was successfully deleted</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Profile_DoesntExist"/> if the requested profile at profileName doesn't exist</item>
        /// <item><see cref="ErrorCode.API_NullInPointer"/> if profileName is null</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetCurrentProfile()"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result DeleteProfile(string profileName)
        {
            return new Result(Headset.DeleteProfile(profileName));
        }

        /// <summary>
        /// Lists all existing profiles
        /// </summary>
        /// <returns>
        /// The existing profile name list or one of the following errors:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the profile list was successfully filled</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetCurrentProfile()"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result<List<string>> ListProfiles()
        {
            var result = new Result<List<string>>();
            result.error = Headset.ListProfiles(out result.value);
            return result;
        }

        /// <summary>
        /// Sets the current profile
        /// <para>
        /// When changing profile, the FOVE system will load up data, such as calibration data, if it is available.
        /// If loading a profile with no calibration data, whether or not the FOVE system keeps old data loaded into memory is undefined.
        /// </para>
        /// </summary>
        /// <remarks>Please note that no-ops are OK but you should check for <see cref="ErrorCode.Profile_NotAvailable"/>.
        /// </remarks>
        /// <param name="profileName">Name of the profile to make current</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the profile was successfully set as the current profile</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Profile_DoesntExist"/> if there is no such profile</item>
        /// <item><see cref="ErrorCode.Profile_NotAvailable"/> if the requested profile is the current profile</item>
        /// <item><see cref="ErrorCode.API_InvalidArgument"/> if the old name and new name are the same</item>
        /// <item><see cref="ErrorCode.API_NullInPointer"/> if profileName is null</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="GetCurrentProfile()"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result SetCurrentProfile(string profileName)
        {
            return new Result(Headset.SetCurrentProfile(profileName));
        }

        /// <summary>
        /// Gets the current profile
        /// </summary>
        /// <returns>
        /// The name of current profile or one of the following errors:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> if the profile name was successfully get</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetProfileDataPath(string)"/>
        public static Result<string> GetCurrentProfile()
        {
            var result = new Result<string>();
            result.error = Headset.GetCurrentProfile(out result.value);
            return result;
        }

        /// <summary>
        /// Gets the data folder for a given profile
        /// <para>Allows you to retrieve a filesytem directory where third party apps can write data associated with this profile. This directory will be created before return.</para>
        /// <para>Since multiple applications may write stuff to a profile, please prefix any files you create with something unique to your application.</para>
        /// <para>There are no special protections on profile data, and it may be accessible to any other app on the system. Do not write sensitive data here.</para>
        /// <para>This is intended for simple uses. For advanced uses that have security concerns, or want to sync to a server, etc,
        /// third party applications are encouraged to use their own separate data store keyed by profile name.
        /// They will need to test for profile name changes and deletions manually in that case.</para>
        /// </summary>
        /// <remarks>Please note that no-ops are OK but you should check for <see cref="ErrorCode.Profile_NotAvailable"/>.
        /// </remarks>
        /// <param name="profileName">The name of the profile to be queried, or an empty string if no profile is set</param>
        /// <returns>
        /// The data path associated to the provided profile or one of the following errors:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/>if the data path was successfully created and returned</item>
        /// <item><see cref="ErrorCode.Profile_DoesntExist"/> if there is no such profile</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Profile_NotAvailable"/> if the requested profile is the current profile</item>
        /// <item><see cref="ErrorCode.API_NullInPointer"/> if profileName is null</item>
        /// </list>
        /// </returns>
        /// <seealso cref="CreateProfile(string)"/>
        /// <seealso cref="RenameProfile(string, string)"/>
        /// <seealso cref="DeleteProfile(string)"/>
        /// <seealso cref="ListProfiles()"/>
        /// <seealso cref="SetCurrentProfile(string)"/>
        /// <seealso cref="GetCurrentProfile()"/>
        public static Result<string> GetProfileDataPath(string profileName)
        {
            var result = new Result<string>();
            result.error = Headset.GetProfileDataPath(profileName, out result.value);
            return result;
        }
    }
}
