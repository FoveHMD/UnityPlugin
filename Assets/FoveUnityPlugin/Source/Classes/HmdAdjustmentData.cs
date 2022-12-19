using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Provide all the HMD positioning data needed to render the current state of the HMD adjustment process
    /// </summary>
    public struct HmdAdjustmentData
    {
        /// <summary>
        /// The translation offset of the HMD in relative units ([-1, 1])
        /// </summary>
        public Vector2 translation;
        /// <summary>
        /// The rotation of HMD to the eye line in radian
        /// </summary>
        public float rotation;
        /// <summary>
        ///  Indicate whether the HMD adjustment GUI should be displayed to correct user HMD alignment
        /// </summary>
        public bool adjustmentNeeded;
        /// <summary>
        /// Indicate if the adjustment process has timeout in which case the GUI should close
        /// </summary>
        public bool hasTimeout;

        /// <summary>
        /// Implicit conversion to Unity GazeConvergenceData
        /// </summary>
        public static explicit operator HmdAdjustmentData(Fove.HmdAdjustmentData data)
        {
            return new HmdAdjustmentData 
            { 
                translation = data.translation.ToVector2(), 
                rotation = data.rotation, 
                adjustmentNeeded = data.adjustmentNeeded, 
                hasTimeout = data.hasTimeout 
            };
        }
    }
}
