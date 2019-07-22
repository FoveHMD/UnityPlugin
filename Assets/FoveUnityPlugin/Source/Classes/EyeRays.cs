
using UnityEngine;

namespace Fove.Unity
{
	/// <summary>
	/// A basic struct which contains two UnityEngine rays, one each for left and right eyes, indicating
	/// where the user is gazing in world space.
	/// </summary>
	/// <remarks>Ray objects -- one representing each eye's
	/// gaze direction. Will not be sufficient for people/devices with more than two eyes.
	/// </remarks>
	public struct EyeRays
	{
		/// <summary>
		/// The left eye's gaze ray.
		/// </summary>
		public Ray left;
		/// <summary>
		/// The right eye's gaze ray.
		/// </summary>
		public Ray right;

		public EyeRays(Ray l, Ray r)
		{
			left = l;
			right = r;
		}
	}
}
