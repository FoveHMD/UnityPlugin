using UnityEngine;

namespace Fove.Unity
{
	/// <summary>
	/// Struct representing the vector pointing where the user is looking.
	/// </summary>
	/// <remarks>
	/// The vector (from the center of the player's head in world space) that can be used to approximate the point
	/// that the user is looking at.</remarks>
	public struct GazeConvergenceData
	{
		/// <summary>
		/// Contructor to set Gaze Convergence data based on a Unity native Ray instance
		/// </summary>
		/// <param name="ray">Unity's Ray structure</param>
		/// <param name="distance">The distance from start, Range: 0 to inf</param>
		/// in the values presented.</param>
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
		/// Implicit convertion to Unity GazeConvergenceData
		/// </summary>
		public static implicit operator GazeConvergenceData(Fove.GazeConvergenceData data)
		{
			return new GazeConvergenceData(data.ray.ToRay(), data.distance);
		}
	}
}
