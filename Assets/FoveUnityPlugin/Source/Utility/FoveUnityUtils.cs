
using UnityEngine;

namespace Fove.Unity
{
	public class Utils
	{
		public static bool ReportErrorCodeIfNotNone(ErrorCode code, string funcName)
		{
			if (code == ErrorCode.None)
			{
				return true;
			}

			Debug.LogWarning(string.Format("[FOVE] {0} returned error code: {1}", funcName, code));
			return false;
		}

		/// <summary>
		/// Convert a FOVE 4x4 matrix into a Unity 4x4 matrix.
		/// </summary>
		/// <param name="fmat">The Fove matrix to convert</param>
		/// <returns>An equivalent Unity 4x4 matrix</returns>
		public static Matrix4x4 GetUnityMx(Matrix44 fmat)
		{
			var m = new Matrix4x4
			{
				m00 = fmat.m00,
				m01 = fmat.m01,
				m02 = fmat.m02,
				m03 = fmat.m03,
				m10 = fmat.m10,
				m11 = fmat.m11,
				m12 = fmat.m12,
				m13 = fmat.m13,
				m20 = fmat.m20,
				m21 = fmat.m21,
				m22 = fmat.m22,
				m23 = fmat.m23,
				m30 = fmat.m30,
				m31 = fmat.m31,
				m32 = fmat.m32,
				m33 = fmat.m33
			};

			return m;
		}

		/// <summary>
		/// Convert a FOVE Vec3 into a Unity Vector3.
		/// </summary>
		/// <param name="vec">The FOVE vector to convert</param>
		/// <returns>An equivalent Unity Vector3 object</returns>
		public static Vector3 GetUnityVector(Vec3 vec)
		{
			return new Vector3(vec.x, vec.y, vec.z);
		}

		/// <summary>
		/// Convert a FOVE Vec2 into a Unity Vector3.
		/// </summary>
		/// <param name="vec">The FOVE vector to convert</param>
		/// <returns>An equivalent Unity Vector2 object</returns>
		public static Vector2 GetUnityVector(Vec2 vec)
		{
			return new Vector2(vec.x, vec.y);
		}

		/// <summary>
		/// Convert a FOVE SFVR_Ray into a Unity Ray.
		/// </summary>
		/// <param name="ray">The FOVE ray to convert</param>
		/// <returns>An equivalent Unity Ray object</returns>
		public static UnityEngine.Ray GetUnityRect(Ray ray)
		{
			return new UnityEngine.Ray(GetUnityVector(ray.origin), GetUnityVector(ray.direction));
		}

		/// <summary>
		/// Convert a Unity Vector3 to a FOVE Vec3.
		/// </summary>
		/// <param name="vec">The Unity vector to convert</param>
		/// <returns>An equivalent FOVE Vec3 object</returns>
		public static Vec3 GetFoveVector(Vector3 vec)
		{
			return new Vec3(vec.x, vec.y, vec.z);
		}

		/// <summary>
		/// Convert a Unity Vector2 to a FOVE Vec3.
		/// </summary>
		/// <param name="vec">The Unity vector to convert.</param>
		/// <returns>An equivalent FOVE Vec2 object</returns>
		public static Vec2 GetFoveVector(Vector2 vec)
		{
			return new Vec2(vec.x, vec.y);
		}

		/// <summary>
		/// Convert a Unity Ray to a FOVE SFVR_Ray.
		/// </summary>
		/// <param name="ray">The Unity Ray to convert</param>
		/// <returns>An equivalent FOVE SFVR_Ray object</returns>
		public static Ray GetFoveRay(UnityEngine.Ray ray)
		{
			// TODO: Update once SFVR_Ray gets a better constructor
			var result = new Ray();
			result.origin = GetFoveVector(ray.origin);
			result.direction = GetFoveVector(ray.direction);
			return result;
		}
	}
}
