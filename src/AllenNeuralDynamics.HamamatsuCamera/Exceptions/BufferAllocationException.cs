using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to allocate or release buffer.
    /// </summary>
    [Serializable]
    public class BufferAllocationException : Exception
    {
        /// <summary>
        /// Thrown when failing to allocate buffer.
        /// </summary>
        public BufferAllocationException()
            : base("Failed to allocate buffer.")
        { }

        /// <summary>
        /// Thrown when failing to release buffer.
        /// </summary>
        public BufferAllocationException(string message)
            : base(message)
        { }
    }
}
