using System;
using System.Collections.Generic;

using UnityEngine;
using Fove.Managed;

namespace UnityEngine
{
	public class FoveUnityUtils
	{
		/// <summary>
		/// Convert a FOVE 4x4 matrix into a Unity 4x4 matrix.
		/// </summary>
		/// <param name="fmat">The Fove matrix to convert</param>
		/// <returns>An equivalent Unity 4x4 matrix</returns>
		public static Matrix4x4 GetUnityMx(SFVR_Matrix44 fmat)
		{
			var m = new Matrix4x4
			{
				m00 = fmat.mat[0 + 0 * 4],
				m01 = fmat.mat[0 + 1 * 4],
				m02 = fmat.mat[0 + 2 * 4],
				m03 = fmat.mat[0 + 3 * 4],
				m10 = fmat.mat[1 + 0 * 4],
				m11 = fmat.mat[1 + 1 * 4],
				m12 = fmat.mat[1 + 2 * 4],
				m13 = fmat.mat[1 + 3 * 4],
				m20 = fmat.mat[2 + 0 * 4],
				m21 = fmat.mat[2 + 1 * 4],
				m22 = fmat.mat[2 + 2 * 4],
				m23 = fmat.mat[2 + 3 * 4],
				m30 = fmat.mat[3 + 0 * 4],
				m31 = fmat.mat[3 + 1 * 4],
				m32 = fmat.mat[3 + 2 * 4],
				m33 = fmat.mat[3 + 3 * 4]
			};

			return m;
		}

		/// <summary>
		/// Convert a FOVE SFVR_Vec3 into a Unity Vector3.
		/// </summary>
		/// <param name="vec">The FOVE vector to convert</param>
		/// <returns>An equivalent Unity Vector3 object</returns>
		public static Vector3 GetUnityVector(SFVR_Vec3 vec)
		{
			return new Vector3(vec.x, vec.y, vec.z);
		}

		/// <summary>
		/// Convert a FOVE SFVR_Vec2 into a Unity Vector3.
		/// </summary>
		/// <param name="vec">The FOVE vector to convert</param>
		/// <returns>An equivalent Unity Vector2 object</returns>
		public static Vector2 GetUnityVector(SFVR_Vec2 vec)
		{
			return new Vector2(vec.x, vec.y);
		}

		/// <summary>
		/// Convert a FOVE SFVR_Ray into a Unity Ray.
		/// </summary>
		/// <param name="ray">The FOVE ray to convert</param>
		/// <returns>An equivalent Unity Ray object</returns>
		public static Ray GetUnityRect(SFVR_Ray ray)
		{
			return new Ray(GetUnityVector(ray.origin), GetUnityVector(ray.direction));
		}

		/// <summary>
		/// Convert a Unity Vector3 to a FOVE SFVR_Vec3.
		/// </summary>
		/// <param name="vec">The Unity vector to convert</param>
		/// <returns>An equivalent FOVE SFVR_Vec3 object</returns>
		public static SFVR_Vec3 GetFoveVector(Vector3 vec)
		{
			return new SFVR_Vec3(vec.x, vec.y, vec.z);
		}

		/// <summary>
		/// Convert a Unity Vector2 to a FOVE SFVR_Vec3.
		/// </summary>
		/// <param name="vec">The Unity vector to convert.</param>
		/// <returns>An equivalent FOVE SFVR_Vec2 object</returns>
		public static SFVR_Vec2 GetFoveVector(Vector2 vec)
		{
			return new SFVR_Vec2(vec.x, vec.y);
		}

		/// <summary>
		/// Convert a Unity Ray to a FOVE SFVR_Ray.
		/// </summary>
		/// <param name="ray">The Unity Ray to convert</param>
		/// <returns>An equivalent FOVE SFVR_Ray object</returns>
		public static SFVR_Ray GetFoveRay(Ray ray)
		{
			// TODO: Update once SFVR_Ray gets a better constructor
			var result = new SFVR_Ray();
			result.origin = GetFoveVector(ray.origin);
			result.direction = GetFoveVector(ray.direction);
			return result;
		}
	}
}
