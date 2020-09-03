
using System.Collections.Generic;
using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Specify the outline of a user eye in the eye image.
    /// </summary>
    /// <seealso cref="FoveResearch.GetEyesImage"/>
    public struct EyeShape
    {
        /// <summary>
        /// The number of points composing the eye outline
        /// </summary>
        public const int OutlinePointCount = Fove.EyeShape.OutlinePointCount;

        /// <summary>
        /// The inside extremity point position in pixels
        /// </summary>
        public Vector2 outlinePoint0;
        /// <summary>
        /// The 1st lower eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint1;
        /// <summary>
        /// The 2nd lower eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint2;
        /// <summary>
        /// The 3rd lower eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint3;
        /// <summary>
        /// The 4th lower eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint4;
        /// <summary>
        /// The 5th lower eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint5;
        /// <summary>
        /// The outside extremity point position in pixels
        /// </summary>
        public Vector2 outlinePoint6;
        /// <summary>
        /// The 1st upper eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint7;
        /// <summary>
        /// The 2nd upper eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint8;
        /// <summary>
        /// The 3rd upper eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint9;
        /// <summary>
        /// The 4th upper eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint10;
        /// <summary>
        /// The 5th upper eyelid point position in pixels
        /// </summary>
        public Vector2 outlinePoint11;

        /// <summary>
        /// Return the eye outline, it is composed of 12 points as follow:
        /// <list type="bullet">
        /// <item>Point 0: the inside extremity of the outline (the point the closest to the nose)</item>
        /// <item>Point 1 to 5: bottom eyelid points going from the inside to the outside of the eye</item>
        /// <item>Point 6: the outside extremity of the outline (the point the furthest from the nose)</item>
        /// <item>Point 7 to 11: top eyelid points going from the outside to the inside of the eye</item>
        /// </list>
        /// </summary>
        public IEnumerable<Vector2> Outline
        {
            get
            {
                yield return outlinePoint0;
                yield return outlinePoint1;
                yield return outlinePoint2;
                yield return outlinePoint3;
                yield return outlinePoint4;
                yield return outlinePoint5;
                yield return outlinePoint6;
                yield return outlinePoint7;
                yield return outlinePoint8;
                yield return outlinePoint9;
                yield return outlinePoint10;
                yield return outlinePoint11;
            }
        }

        /// <summary>
        /// Explicit conversion from Fove internal EyeShape type into Unity EyeShape type
        /// </summary>
        /// <param name="eyeShape"></param>
        public static explicit operator EyeShape(Fove.EyeShape eyeShape)
        {
            return new EyeShape
            {
                outlinePoint0 = eyeShape.outlinePoint0.ToVector2(),
                outlinePoint1 = eyeShape.outlinePoint1.ToVector2(),
                outlinePoint2 = eyeShape.outlinePoint2.ToVector2(),
                outlinePoint3 = eyeShape.outlinePoint3.ToVector2(),
                outlinePoint4 = eyeShape.outlinePoint4.ToVector2(),
                outlinePoint5 = eyeShape.outlinePoint5.ToVector2(),
                outlinePoint6 = eyeShape.outlinePoint6.ToVector2(),
                outlinePoint7 = eyeShape.outlinePoint7.ToVector2(),
                outlinePoint8 = eyeShape.outlinePoint8.ToVector2(),
                outlinePoint9 = eyeShape.outlinePoint9.ToVector2(),
                outlinePoint10 = eyeShape.outlinePoint10.ToVector2(),
                outlinePoint11 = eyeShape.outlinePoint11.ToVector2(),
            };
        }
    }
}
