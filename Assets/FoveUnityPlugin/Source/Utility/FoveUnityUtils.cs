
using UnityEngine;

namespace Fove.Unity
{
    public static class Utils
    {
        /// <summary>
        /// Return true if the calibration state is successful
        /// </summary>
        public static bool IsSuccessful(this CalibrationState state)
        {
            return state == CalibrationState.Successful_HighQuality || state == CalibrationState.Successful_MediumQuality || state == CalibrationState.Successful_LowQuality;
        }

        /// <summary>
        /// Return true if the calibration state is failure
        /// </summary>
        public static bool IsFailure(this CalibrationState state)
        {
            return state == CalibrationState.Failed_Aborted
                || state == CalibrationState.Failed_InaccurateData
                || state == CalibrationState.Failed_NoRenderer
                || state == CalibrationState.Failed_NoRenderer
                || state == CalibrationState.Failed_NoUser
                || state == CalibrationState.Failed_Unknown;
        }

        #region internal

        /// <summary>
        /// Convert a FOVE 4x4 matrix into a Unity 4x4 matrix.
        /// </summary>
        /// <param name="fmat">The Fove matrix to convert</param>
        /// <returns>An equivalent Unity 4x4 matrix</returns>
        internal static Matrix4x4 ToMatrix4x4(this Matrix44 fmat)
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
        internal static Vector3 ToVector3(this Vec3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// Convert a FOVE Vec2 into a Unity Vector3.
        /// </summary>
        /// <param name="vec">The FOVE vector to convert</param>
        /// <returns>An equivalent Unity Vector2 object</returns>
        internal static Vector2 ToVector2(this Vec2 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        /// <summary>
        /// Convert a FOVE SFVR_Ray into a Unity Ray.
        /// </summary>
        /// <param name="ray">The FOVE ray to convert</param>
        /// <returns>An equivalent Unity Ray object</returns>
        internal static Ray ToRay(this EyeRay ray)
        {
            return new Ray(ray.origin.ToVector3(), ray.direction.ToVector3());
        }

        /// <summary>
        /// Convert a Unity Vector3 to a FOVE Vec3.
        /// </summary>
        /// <param name="vec">The Unity vector to convert</param>
        /// <returns>An equivalent FOVE Vec3 object</returns>
        internal static Vec3 ToVec3(this Vector3 vec)
        {
            return new Vec3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// Convert a Unity Vector2 to a FOVE Vec3.
        /// </summary>
        /// <param name="vec">The Unity vector to convert.</param>
        /// <returns>An equivalent FOVE Vec2 object</returns>
        internal static Vec2 ToVec2(this Vector2 vec)
        {
            return new Vec2(vec.x, vec.y);
        }

        /// <summary>
        /// Convert a Unity Ray to a FOVE SFVR_Ray.
        /// </summary>
        /// <param name="ray">The Unity Ray to convert</param>
        /// <returns>An equivalent FOVE SFVR_Ray object</returns>
        internal static EyeRay ToEyeRay(this Ray ray)
        {
            return new EyeRay
            {
                origin = ToVec3(ray.origin),
                direction = ToVec3(ray.direction)
            };
        }

        /// <summary>
        /// Convert a Fove quaternion into a Unity quaternion
        /// </summary>
        /// <param name="q">The Fove quaternion</param>
        /// <returns>The Unity quaternion</returns>
        internal static Quaternion ToQuaternion(this Quat q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        /// <summary>
        /// Convert a Unity quaternion into a Fove quaternion
        /// </summary>
        /// <param name="q">The Unity quaternion</param>
        /// <returns>The Fove quaternion</returns>
        internal static Quat ToQuat(this Quaternion q)
        {
            return new Quat(q.x, q.y, q.z, q.w);
        }

        /// <summary>
        /// Convert Unity Bounds to a Fove bounding box
        /// </summary>
        /// <param name="bounds">The Unity Bounds</param>
        /// <returns>The Fove bounding box</returns>
        internal static BoundingBox ToBoundingBox(this Bounds bounds)
        {
            return new BoundingBox { center = ToVec3(bounds.center), extend = ToVec3(bounds.extents) };
        }

        internal static Result<Ray> CalculateWorldGazeVector(ref Matrix4x4 transform, ref Vector3 eyeOffset, ref Result<Vector3> eyeVector)
        {
            var position = transform.MultiplyPoint(eyeOffset);
            var direction = transform.MultiplyVector(eyeVector.value).normalized;
            return new Result<Ray>(new Ray(position, direction), eyeVector.error);
        }

        #endregion
    }
}
