using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to allocate buffer.
    /// </summary>
    [Serializable]
    public class BufferAllocationException : Exception
    {
        public BufferAllocationException()
            : base("Failed to allocate buffer.")
        { }

        public BufferAllocationException(string message)
            : base(message)
        { }

        public BufferAllocationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected BufferAllocationException(System.Runtime.Serialization.SerializationInfo info,
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
