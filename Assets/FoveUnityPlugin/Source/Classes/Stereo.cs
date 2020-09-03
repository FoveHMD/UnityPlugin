
using System;
using System.Collections;
using System.Collections.Generic;

namespace Fove.Unity
{
    /// <summary>
    /// Data structure composed of 2 elements of same type, one for each eye.
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    [Serializable]
    public struct Stereo<T> : IEnumerable<T>
    {
        /// <summary>
        /// Return he number of element contained in a stereo structure
        /// </summary>
        public int ElementCount { get { return 2; } }

        /// <summary>
        /// Data corresponding to the left eye (index 0)
        /// </summary>
        public T left;

        /// <summary>
        /// Data corresponding to the right eye (index 1)
        /// </summary>
        public T right;

        /// <summary>
        /// Create a new instance with left and right initialized to the same value
        /// </summary>
        public Stereo(T value)
        {
            left = value;
            right = value;
        }

        /// <summary>
        /// Create a new instance with given left and right values 
        /// </summary>
        public Stereo(T left, T right)
        {
            this.left = left;
            this.right = right;
        }

        /// <summary>
        /// Get or sets the value at the given index (0 => left, 1 => right)
        /// </summary>
        public T this[int idx]
        {
            get 
            {
                if (idx == 0)
                    return left;
                else if (idx == 1)
                    return right;

                throw new ArgumentOutOfRangeException("idx", idx, "Stereo index should be < 2");
            }
            set
            {
                if (idx == 0)
                    left = value;
                else if (idx == 1)
                    right = value;

                throw new ArgumentOutOfRangeException("idx", idx, "Stereo index should be < 2");
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return left;
            yield return right;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return left;
            yield return right;
        }
    }
}
