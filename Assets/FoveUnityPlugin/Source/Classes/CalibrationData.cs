using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Represent a calibration target of the calibration process
    /// </summary>
    public struct CalibrationTarget
    {
        /// <summary>
        /// The position of the calibration target in the 3D world space
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The recommended size for the calibration target in world space unit.
        /// </summary>
        /// <remarks>A recommended size of 0 means that the display of the target is not recommended at the current time</remarks>
        public float recommendedSize;

        /// <summary>
        /// Implicit conversion to Unity GazeConvergenceData
        /// </summary>
        public static explicit operator CalibrationTarget(Fove.CalibrationTarget target)
        {
            return new CalibrationTarget { position = target.position.ToVector3(), recommendedSize = target.recommendedSize };
        }
    }

    /// <summary>
    /// Provide all the calibration data needed to render the current state of the calibration process
    /// </summary>
    public struct CalibrationData
    {
        /// <summary>
        /// The calibration method currently used
        /// </summary>
        public CalibrationMethod method;

        /// <summary>
        /// The current state of the calibration
        /// </summary>
        public CalibrationState state;

        /// <summary>
        /// The current calibration target to display for the left and right eyes
        /// </summary>
        public Stereo<CalibrationTarget> targets;

        /// <summary>
        /// Implicit conversion to Unity GazeConvergenceData
        /// </summary>
        public static explicit operator CalibrationData(Fove.CalibrationData data)
        {
            var targetL = (CalibrationTarget)data.targetL;
            var targetR = (CalibrationTarget)data.targetR;
            return new CalibrationData { method = data.method, state= data.state, targets = new Stereo<CalibrationTarget>(targetL, targetR)};
        }
    }
}
