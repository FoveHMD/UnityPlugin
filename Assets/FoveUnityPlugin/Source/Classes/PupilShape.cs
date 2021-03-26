
using System.Collections.Generic;
using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Specity the shape of a pupil as an ellipse
    /// Coordinates are in eye-image pixels from (0,0) to (camerawidth, cameraheight), with (0,0) being the top left.
    /// </summary>
    /// <seealso cref="Fove.PupilShape"/>
    public struct PupilShape
    {
        /// <summary>
        /// The center of the ellipse
        /// </summary>
        public Vector2 center;
        /// <summary>
        /// The width and height of the ellipse
        /// </summary>
        public Vector2 size;
        /// <summary>
        /// A clockwise rotation of the ellipse axes around the center, in degrees
        /// </summary>
        public float angle;

        /// <summary>
        /// Explicit conversion from Fove internal PupilShape type into Unity PupilShape type
        /// </summary>
        /// <param name="pupilShape"></param>
        public static explicit operator PupilShape(Fove.PupilShape pupilShape)
        {
            return new PupilShape
            {
                center = pupilShape.center.ToVector2(),
                size = pupilShape.size.ToVector2(),
                angle = pupilShape.angle,
            };
        }
    }
}