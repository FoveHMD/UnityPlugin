using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Struct representing the convergence point that the user is looking at.
    /// </summary>
    /// <remarks>
    /// The gaze ray is starting from the center of the player's head in world space.
    /// </remarks>
    public struct GazeConvergenceData
    {
        /// <summary>
        /// Constructor to set Gaze Convergence data using a Unity Ray and convergence distance
        /// </summary>
        /// <param name="ray">The ray specifying the direction of the gaze</param>
        /// <param name="distance">The distance at which the user is looking at</param>
        public GazeConvergenceData(Ray ray, float distance)
        {
            this.ray = ray;
            this.distance = distance;
        }

        /// <summary>
        /// A normalized (1 unit long) ray indicating the starting reference point and direction of the user's gaze
        /// </summary>
        public Ray ray;

        /// <summary>
        /// How far out along the normalized ray the user's eyes are converging.
        /// </summary>
        public float distance;

        /// <summary>
        /// Gaze convergence data pointing forward to the infinity
        /// </summary>
        public readonly static GazeConvergenceData ForwardToInfinity = new GazeConvergenceData(new Ray(Vector3.zero, Vector3.forward), Mathf.Infinity);

        /// <summary>
        /// Explicit conversion from Fove GazeConvergenceData internal data structure
        /// </summary>
        public static explicit operator GazeConvergenceData(Fove.GazeConvergenceData data)
        {
            return new GazeConvergenceData(data.ray.ToRay(), data.distance);
        }
    }
}
