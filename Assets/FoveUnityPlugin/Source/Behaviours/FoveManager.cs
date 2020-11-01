
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
        public static Headset Headset { get { return Instance.headset; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is connected
        /// See <see cref="IsHardwareConnected()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareConnected { get { return m_sWaitForHardwareConnected; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is disconnected
        /// See <see cref="IsHardwareConnected()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareDisconnected { get { return m_sWaitForHardwareDisconnected; } }

        /// <summary>
        /// Use this to pause a coroutine until the HMD hardware is ready to be used.
        /// See <see cref="IsHardwareReady()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForHardwareReady { get { return m_sWaitForHardwareReady; } }

        /// <summary>
        /// Use this to pause a coroutine until a eye tracking calibration process is started.
        /// See <see cref="IsEyeTrackingCalibrating()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrationStart { get { return m_sWaitForCalibrationStart; } }

        /// <summary>
        /// Use this to pause a coroutine until a eye tracking calibration process is ended.
        /// See <see cref="IsEyeTrackingCalibrating()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrationEnd { get { return m_sWaitForCalibrationEnd; } }

        /// <summary>
        /// Use this to pause a coroutine until the eye tracking system is calibrated.
        /// See <see cref="IsEyeTrackingCalibrated()"/> for more details.
        /// </summary>
        public static IEnumerator WaitForEyeTrackingCalibrated { get { return m_sWaitForCalibrationCalibrated; } }

        /// <summary>
        /// Use this to pause a coroutine until the user is wearing the headset.
        /// See <see cref="IsUserPresent()"/> for more details.
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
        /// Triggered when the user starts or stops fixating (e.g. when the value returned by <see cref="IsUserShiftingAttention()"/> changes).
        /// </summary>
        public static event Action<bool> IsUserShiftingAttentionChanged;

        /// <summary>
        /// Triggered when the user eye state status changes (e.g. when the value returned by <see cref="GetEyeState(Eye)"/> changes).
        /// </summary>
        public static event Action<Eye, EyeState> EyeStateChanged;

        /// <summary>
        /// Triggered when the user put or remove the headset (e.g. when the value returned by <see cref="IsUserPresent()"/> changes).
        /// </summary>
        public static event Action<bool> UserPresenceChanged;

        /// <summary>
        /// Triggered when the HMD adjusment Gui visibility status changed (e.g. when the value returned by <see cref="IsHmdAdjustementGuiVisible()"/> changes).
        /// </summary>
        public static event Action<bool> HmdAdjustmentGuiVisibilityChanged;

        /// <summary>
        /// Get or set the world scale. Default value <c>1</c>.
        /// </summary>
        public static float WorldScale
        {
            get { return Instance.worldScale; }
            set { Instance.worldScale = Mathf.Max(float.Epsilon, value); }
        }

        /// <summary>
        /// Get or set the render scale. 
        /// </summary>
        public static float RenderScale
        {
            get { return Instance.renderScale; }
            set { Instance.renderScale = Mathf.Max(float.Epsilon, value); }
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
        /// Query whether or not a headset is physically connected to the computer
        /// </summary>
        /// <returns>Whether or not a headset is present on the machine, and the call success status.</returns>
        public static Result<bool> IsHardwareConnected()
        {
            return Headset.IsHardwareConnected();
        }

        /// <summary>
        /// Query whether the headset has all requested features booted up and running (position tracking, eye tracking,
        /// orientation, etc...).
        /// </summary>
        /// <returns>Whether you can expect valid data from all headset functions, and the call success status.</returns>
        public static Result<bool> IsHardwareReady()
        {
            return Headset.IsHardwareReady();
        }

        /// <summary>
        /// Query whether the motion tracking hardware (IMU) has started
        /// </summary>
        /// <returns>Whether you can expect valid orientation pose data, and the call success status.</returns>
        public static Result<bool> IsMotionReady()
        {
            return Headset.IsMotionReady();
        }

        /// <summary>
        /// Check whether the runtime and client versions are compatible and are expected to work correctly.
        /// </summary>
        /// <returns>Any errors that occurred when checking the runtime and client versions, or None if 
        /// everything seems fine.</returns>
        /// <remarks>Newer runtime versions are designed to be compatible with older client versions, however new
        /// client versions are not designed to be compatible with old runtime versions.</remarks>
        public static Result CheckSoftwareVersions()
        {
            return Headset.CheckSoftwareVersions();
        }

        /// <summary>
        /// Get the version of the Fove client library. This returns "[major].[minor].[build]".
        /// </summary>
        /// <returns>A string representing the client library version, and the call success status</returns>
        public static Result<string> GetClientVersion()
        {
            var result = Headset.GetSoftwareVersions();

            var versionString = result.IsValid
                ? "" + result.value.clientMajor + "." + result.value.clientMinor + "." + result.value.clientBuild
                : "Unknown";

            return new Result<string>() { value = versionString, error = result.error };
        }

        /// <summary>
        /// Get the version of the installed runtime service. This returns "[major].[minor].[build]".
        /// </summary>
        /// <returns>A string representing the runtime library version, and the call success status</returns>
        public static Result<string> GetRuntimeVersion()
        {
            var result = Headset.GetSoftwareVersions();

            var versionString = result.IsValid
                ? "" + result.value.runtimeMajor + "." + result.value.runtimeMinor + "." + result.value.runtimeBuild
                : "Unknown";

            return new Result<string>() { value = versionString, error = result.error };
        }

        /// <summary>
        /// Register a headset capability independently from the <see cref="FoveInterface"/> needs.
        /// </summary>
        /// <param name="capabilities">The capabilities to add</param>
        public static void RegisterCapabilities(ClientCapabilities capabilities)
        {
            Instance.enforcedCapabilities |= capabilities;
            Instance.UpdateCapabilities();
        }

        /// <summary>
        /// Unregister a capability previously registered with <see cref="RegisterCapabilities(ClientCapabilities)"/>.
        /// </summary>
        /// <remarks>The capability will be effectively unregistered only if not needed by the <see cref="FoveInterface"/> game instances</remarks>
        /// <param name="capabilities">The capabilities to remove </param>
        public static void UnregisterCapabilities(ClientCapabilities capabilities)
        {
            Instance.enforcedCapabilities &= ~capabilities;
            Instance.UpdateCapabilities();
        }

        /// <summary>
        /// Get the direction of the gaze of the specified eye, in the HMD coordinate space.
        /// <para>
        /// To get the eye gaze in world space coordinate use the <see cref="FoveInterface.GetGazeVector()" instead/>. 
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The 3D gaze vector of the specified eye in the HMD coordinate space, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<Vector3> GetHmdGazeVector(Eye eye)
        {
            var result = Headset.GetGazeVector(eye);
            return new Result<Vector3>(result.value.ToVector3(), result.error);
        }

        /// <summary>
        /// Returns the user's 2D gaze position on the screens seen through the HMD's lenses
        /// <para>
        /// The use of lenses and distortion correction creates a screen in front of each eye.
        /// This function returns 2D vectors representing where on each eye's screen the user
        /// is looking.
        /// </para>
        /// <para>
        /// The vectors are normalized in the range [-1, 1] along both X and Y axes such that the
        /// following points are true. Center: (0, 0), Bottom-Left: (-1, -1),Top-Right: (1, 1).
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The 2D screen position of the specified eye gaze, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<Vector3> GetGazeScreenPosition(Eye eye)
        {
            var result = Headset.GetGazeVector(eye);
            return new Result<Vector3>(result.value.ToVector3(), result.error);
        }

        /// <summary>
        /// Returns eyes gaze ray resulting from the two eye gazes combined together, in the HMD coordinate space.
        /// <para>
        /// To get individual eye rays use <see cref="GetHmdGazeVector(Eye)"/> instead
        /// </para>
        /// <para>
        /// To get the user gaze in world space coordinate use the <see cref="FoveInterface.GetCombinedGazeRay()"/> instead. 
        /// </para>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The combined gaze ray in the HMD coordinate space, and the call success status: 
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<Ray> GetHmdCombinedGazeRay()
        {
            var result = Headset.GetCombinedGazeRay();
            return new Result<Ray>(result.value.ToRay(), result.error);
        }

        /// <summary>
        /// Returns eyes gaze depth resulting from the two eye gazes combined together
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.GazeDepth"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The depth of the combine Gaze, and the call success status: 
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetCombinedGazeDepth()
        {
            var result = Headset.GetCombinedGazeDepth();
            return new Result<float>(Instance.renderScale * result.value, result.error);
        }

        /// <summary>
        /// Returns whether the user is shifting its attention between objects or looking at something specific (fixation or pursuit).
        /// <para>This can be used to ignore eye data during large eye motions when the user is not looking at anything specific.</para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.UserAttentionShift"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the user is shifting attention, and the call success status: 
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsUserShiftingAttention()
        {
            return Headset.IsUserShiftingAttention();
        }

        /// <summary>
        /// Returns the state of an individual eye
        /// <para>This can be used to ignore eye data during large eye motions when the user is not looking at anything specific.</para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The state of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<EyeState> GetEyeState(Eye eye)
        {
            return Headset.GetEyeState(eye);
        }

        /// <summary>
        /// Returns whether eye tracking is calibrated and usable
        /// <para></para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the eye tracking system is calibrated, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsEyeTrackingCalibrated()
        {
            return Headset.IsEyeTrackingCalibrated();
        }

        /// <summary>
        /// Query whether eye tracking is currently calibrating, meaning that the user likely
        /// cannot see your game due to the calibration process. While this is true, you should
        /// refrain from showing any interactions that would respond to eye gaze.
        /// <para>
        /// You should carefully check the value returned by this function as the calibration process
        /// can be manually started by the user at any time during your game. 
        /// Other than user manual triggering, times when calibration will occur are:
        /// <list type="number">
        /// <item>At the launch of you application, if you check 'Force Calibration' in the fove settings.</item>
        /// <item>Whenever you call <see cref="StartEyeTrackingCalibration(bool, CalibrationMethod)"/>.</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the eye tracking system is calibrating, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsEyeTrackingCalibrating()
        {
            return Headset.IsEyeTrackingCalibrating();
        }

        /// <summary>
        /// Returns whether the eye tracking system is currently calibrated for glasses.
        /// <para>
        /// This basically indicates if the user was wearing glasses during the calibration or not.
        /// This function returns <see cref="ErrorCode.Data_Uncalibrated"/> if the eye tracking system 
        /// has not been calibrated yet
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the eye tracking system is calibrated for glasses, and the call success status:
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Uncalibrated"/> if the eye tracking system is currently uncalibrated</item>
        /// </returns>
        public static Result<bool> IsEyeTrackingCalibratedForGlasses()
        {
            return Headset.IsEyeTrackingCalibratedForGlasses();
        }

        /// <summary>
        /// Returns <c>true</c> when the GUI that asks the user to adjust their headset is being displayed
        /// <para>
        /// It is best practice to pause the gameplay of the application when the HMD adjustment GUI is being displayed.
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the Headset position adjustment GUI is visible on the screen, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsHmdAdjustmentGuiVisible()
        {
            return Headset.IsHmdAdjustmentGuiVisible();
        }

        /// <summary>
        /// Returns whether or not the GUI that asks the user to adjust their headset was hidden by timeout
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the Headset position adjustment GUI has timeout, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> HasHmdAdjustmentGuiTimeout()
        {
            return Headset.HasHmdAdjustmentGuiTimeout();
        }


        /// <summary>
        /// Returns whether eye tracking is actively tracking eyes
        /// <para>In other words, it returns `true` only when the hardware is ready and eye tracking is calibrated.</para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the eye tracking system is ready, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsEyeTrackingReady()
        {
            return Headset.IsEyeTrackingReady();
        }

        /// <summary>
        /// Returns <c>true</c> when the user is wearing the headset
        /// <para>When user is not present Eye tracking values shouldn't be used as invalid.</para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.UserPresence"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the user is wearing the headset, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsUserPresent()
        {
            return Headset.IsUserPresent();
        }

        /// <summary>
        /// Returns the eyes camera image texture
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyesImage"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The Eye camera image, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreadable"/> if the data couldn't be read properly from memory</item>
        /// </list>
        /// </returns>
        public static Result<Texture2D> GetEyesImage()
        {
            return Instance.eyesTexture;
        }

        /// <summary>
        /// Returns the user IPD (Inter Pupillary Distance), in meters
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.UserIPD"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The user IPD value, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetUserIPD()
        {
            return Headset.GetUserIPD();
        }

        /// <summary>
        /// Returns the user IOD (Inter Occular Distance), in meters
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.UserIOD"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The user IOD value, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetUserIOD()
        {
            return Headset.GetUserIOD();
        }

        /// <summary>
        /// Returns the user pupils radius, in meters
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.PupilRadius"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The pupil radius of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetPupilRadius(Eye eye)
        {
            return Headset.GetPupilRadius(eye);
        }

        /// <summary>
        /// Returns the user iris radius, in meters
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.IrisRadius"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The iris radius of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetIrisRadius(Eye eye)
        {
            return Headset.GetIrisRadius(eye);
        }

        /// <summary>
        /// Returns the user eyeball radius, in meters
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeballRadius"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The eyeball radius of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetEyeballRadius(Eye eye)
        {
            return Headset.GetEyeballRadius(eye);
        }

        /// <summary>
        /// Returns the user eye torsion, in degrees
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTorsion"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The torsion angle of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<float> GetEyeTorsion(Eye eye)
        {
            return Headset.GeEyeTorsion(eye);
        }

        /// <summary>
        /// Returns the outline shape of the specified user eye in the Eyes camera image.
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeShape"/> should be registered to use this function.</remarks>
        /// <param name="eye">Specify which eye to get the value for</param>
        /// <returns>
        /// The shape of the specified eye, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<EyeShape> GetEyeShape(Eye eye)
        {
            var result = Headset.GetEyeShape(eye);
            return new Result<EyeShape>((EyeShape)result.value, result.error);
        }

        /// <summary>
        /// Starts eye tracking calibration
        /// <para>
        /// After calling this function you should assume that the user cannot see your game until 
        /// <see cref="IsEyeTrackingCalibrating"/> returns false.
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <param name="calibrationOptions">Specify the calibration options for the new calibration process to run</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success.</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.License_FeatureAccessDenied"/> if any of the enabled options require a license that is not active on this machine</item>
        /// </list>
        /// </returns>
        public static Result StartEyeTrackingCalibration(CalibrationOptions calibrationOptions = null)
        {
            return Headset.StartEyeTrackingCalibration(calibrationOptions);
        }

        /// <summary>
        /// Stops eye tracking calibration if it's running, does nothing if it's not running
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success.</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// </list>
        /// </returns>
        public static Result StopEyeTrackingCalibration()
        {
            return Headset.StopEyeTrackingCalibration();
        }

        /// <summary>
        /// Get the state of the currently running calibration process
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The eye tracking system calibration state, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<CalibrationState> GetEyeTrackingCalibrationState()
        {
            return Headset.GetEyeTrackingCalibrationState();
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
        /// <para>It is perfectly fine not to call this function, in which case the Fove service will automatically render the calibration process for you.</para>
        /// </summary>
        /// <remarks>
        /// <remarks><see cref="ClientCapabilities.EyeTracking"/> should be registered to use this function.</remarks></remarks>
        /// <param name="deltaTime">The time elapsed since the last rendered frame</param>
        /// <param name="isVisible">Indicate to the calibration system that something is being drawn to the screen.
        /// This allows the calibration renderer to take as much time as it wants to display success/failure messages
        /// and animate away before the calibration processes is marked as completed by the `IsEyeTrackingCalibrating` function.
        /// </param>
        /// <returns>
        /// The current calibration data, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success.</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Calibration_OtherRendererPrioritized"/> if another process has currently the priority for rendering calibration process</item>
        /// <item><see cref="ErrorCode.License_FeatureAccessDenied"/> if you don't have the license level required to access this feature</item>
        /// </list>
        /// </returns>
        public static Result<CalibrationData> TickEyeTrackingCalibration(float deltaTime, bool isVisible)
        {
            var result = Headset.TickEyeTrackingCalibration(deltaTime, isVisible);
            return new Result<CalibrationData>((CalibrationData)result.value, result.error);
        }

        /// <summary>
        /// Get the game object currently gazed by the user.
        /// <para>
        /// In order to be detected game object need to have the <see cref="GazableObject"/> component attached.
        /// If the user is currently not looking at any specific object <c>null</c> is returned.
        /// </para>
        /// </summary>
        /// <remarks>
        /// To use this function, you need to register the <see cref="ClientCapabilities.GazedObjectDetection"/> first.
        /// </remarks>
        /// <returns>
        /// The game object currently gazed at, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<GameObject> GetGazedObject()
        {
            var result = Headset.GetGazedObjectId();
            var gazableObject = GazableObject.FindGazableObject(result.value);
            var gameObject = gazableObject != null ? gazableObject.gameObject : null;
            return new Result<GameObject>(gameObject, result.error);
        }

        /// <summary>
        /// Reset the headset orientation.
        /// <para>
        /// This sets the HMD's current rotation as a "zero" orientation, essentially resetting their
        /// orientation to that set in the editor.
        /// </para>
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.OrientationTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// </list>
        /// </returns>
        public static Result TareOrientation()
        {
            return Headset.TareOrientationSensor();
        }

        /// <summary>
        /// Returns whether position tracking hardware has started
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.PositionTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// Whether the position tracking headset is ready, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<bool> IsPositionReady()
        {
            return Headset.IsPositionReady();
        }

        /// <summary>
        /// Reset the headset position.
        /// <para>
        /// This sets the HMD's current position relative to the tracking camera as a "zero" position,
        /// essentially jumping the headset back to the interface's origin.
        /// </para>
        /// </summary>        /// <remarks><see cref="ClientCapabilities.PositionTracking"/> should be registered to use this function.</remarks>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// </list>
        /// </returns>
        public static Result TarePosition()
        {
            return Headset.TarePositionSensors();
        }

        /// <summary>
        /// Get the headset current rotation in local coordinate space.
        /// </summary>
        /// <remarks>
        /// This value is automatically applied to the interface's GameObject and is only exposed  here for reference
        /// </remarks>
        /// <returns>
        /// The Headset pose, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<Quaternion> GetHmdRotation()
        {
            return new Result<Quaternion>(Instance.headRotation);
        }

        /// <summary>
        /// Get the headset current position in local coordinates.
        /// </summary>
        /// <remarks>
        /// This value is automatically applied to the interface's GameObject and is only exposed here for reference
        /// </remarks>
        /// <param name="isUserStanding">Indicate if you want to query the seating or standing position</param>
        /// <returns>
        /// The Headset pose, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreliable"/> if the returned data is too unreliable to be used</item>
        /// <item><see cref="ErrorCode.Data_LowAccuracy"/> if the returned data is of low accuracy</item>
        /// </list>
        /// </returns>
        public static Result<Vector3> GetHmdPosition(bool isUserStanding)
        {
            return new Result<Vector3>(isUserStanding ? Instance.standingPosition : Instance.headPosition);
        }

        /// <summary>
        /// Returns the position camera image texture
        /// </summary>
        /// <remarks><see cref="ClientCapabilities.PositionImage"/> should be registered to use this function.</remarks>
        /// <returns>
        /// The position camera image, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.API_NotRegistered"/> if the required capability has not been registered prior to this call</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// <item><see cref="ErrorCode.Data_Unreadable"/> if the data couldn't be read properly from memory</item>
        /// </list>
        /// </returns>
        public static Result<Texture2D> GetPositionImage()
        {
            return Instance.positionTexture;
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
        /// <returns>
        /// The 4x4 projection left and right matrices (left-handed), and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<Stereo<Matrix4x4>> GetProjectionMatrices(float near, float far)
        {
            var result = Headset.GetProjectionMatricesRH(near, far);
            var matrices = new Stereo<Matrix4x4>(result.value.left.ToMatrix4x4(), result.value.right.ToMatrix4x4());
            return new Result<Stereo<Matrix4x4>>(matrices, result.error);
        }

        /// <summary>
        /// Get the offsets to the origin of the headset local space of the left and right eyes.
        /// </summary>
        /// <returns>
        /// The position of left and right eye in the headset local coordinate space, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<Stereo<Vector3>> GetEyeOffsets()
        {
            var result = Headset.GetEyeToHeadMatrices();
            var offsets = new Stereo<Vector3>(
                Instance.GetEyeOffsetVector(result.value.left),
                Instance.GetEyeOffsetVector(result.value.right));
            return new Result<Stereo<Vector3>>(offsets, result.error);
        }

        /// <summary>
        /// Interocular distance to use for rendering in meters
        /// <para>
        /// This is an estimation of the distance between centers of the left and right eyeballs.
        /// Half of the IOD can be used to displace the left and right cameras for stereoscopic rendering.
        /// We recommend calling this each frame when doing stereoscopic rendering.
        /// Future versions of the FOVE service may update the IOD during runtime as needed.
        /// </para>
        /// </summary>
        /// <returns>
        /// A floating point value describing the IOD, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Connect_NotConnected"/> if not connected to the service</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if the capability is registered but no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<float> GetRenderIOD()
        {
            return Headset.GetRenderIOD();
        }

        /// <summary>
        /// Connect to the FOVE Compositor system. This will hard reset the compositor connection even if there is
        /// already a valid connection. You could call this after a call to `DisconnectCompositor` if you found any
        /// significant reason to do so.
        /// </summary>
        /// <returns>The result of the operation</returns>
        public static Result ConnectCompositor()
        {
            foreach (var eyeTextures in Instance.eyeTextures.Values)
                eyeTextures.areNew = true; // force to reset eye texture

            UnityFuncs.ResetNativeState();
            RegisterCapabilities(Instance.currentCapabilities);
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
        /// Re-fetch and update all the data from the Headset.
        /// <para>
        /// This function is automatically called at the beginning of each frame, so in most case you won't need it. 
        /// It can be usefull when the frame processing time is high and you want to be sure to work with the 
        /// latest data at some specific time point.
        /// </para>
        /// </summary>
        /// <returns>True in case of success, false otherwise</returns>
        public static bool UpdateHeadsetData()
        {
            return Instance.UpdateHmdDataInternal();
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
            return Headset.CreateProfile(newName);
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
            return Headset.RenameProfile(oldName, newName);
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
            return Headset.DeleteProfile(profileName);
        }

        /// <summary>
        /// Lists all existing profiles
        /// </summary>
        /// <returns>
        /// The list of existing profile names, and the call success status:
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
            return Headset.ListProfiles();
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
            return Headset.SetCurrentProfile(profileName);
        }

        /// <summary>
        /// Gets the current profile
        /// </summary>
        /// <returns>
        /// The name of the profile currently used, and the call success status:
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
            return Headset.GetCurrentProfile();
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
        /// The data path associated to the provided profile, and the call success status:
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
            return Headset.GetProfileDataPath(profileName);
        }

        /// <summary>
        /// Returns the mirror client texture
        /// </summary>
        /// <returns>
        /// The mirror client texture, and the call success status:
        /// <list type="bullet">
        /// <item><see cref="ErrorCode.None"/> on success</item>
        /// <item><see cref="ErrorCode.Data_NoUpdate"/> if no valid data has been returned by the service yet</item>
        /// </list>
        /// </returns>
        public static Result<Texture2D> GetMirrorTexture()
        {
            return Instance.GetMirrorTextureInternal();
        }
    }
}
