
namespace Fove.Unity
{
    /// <summary>
    /// The result of a Fove function without return value.
    /// </summary>
    public struct Result
    {
        /// <summary>
        /// The error code returned by function. <see cref="ErrorCode.None"/> is returned when the function is successful.
        /// </summary>
        public ErrorCode error;

        /// <summary>
        /// Returns true if the function call succeeded, false otherwise.
        /// </summary>
        public bool Succeeded { get { return error == ErrorCode.None; } }

        /// <summary>
        /// Return true if the function returned an error, false otherwise.
        /// </summary>
        public bool HasError { get { return error != ErrorCode.None; } }

        internal Result(ErrorCode code)
        {
            error = code;
        }
    }

    /// <summary>
    /// The result of a Fove function returning a value.
    /// <para>It contains the return value of the function and an error code.</para>
    /// </summary>
    /// <typeparam name="T">The function return type</typeparam>
    public struct Result<T>
    {
        /// <summary>
        /// The value returned by the function in case of success.
        /// </summary>
        /// <remarks>In case of failure the returned value is either the previous or the default value.</remarks>
        public T value;

        /// <summary>
        /// The error code returned by function. <see cref="ErrorCode.None"/> is returned when the function is successful.
        /// </summary>
        public ErrorCode error;

        /// <summary>
        /// Returns true if the function call succeeded, false otherwise.
        /// </summary>
        public bool Succeeded { get { return error == ErrorCode.None; } }

        /// <summary>
        /// Return true if the function returned an error, false otherwise.
        /// </summary>
        public bool HasError { get { return error != ErrorCode.None; } }

        internal Result(T value, ErrorCode error = ErrorCode.None)
        {
            this.value = value;
            this.error = error;
        }

        public static implicit operator T(Result<T> result) { return result.value; }
    }
}
