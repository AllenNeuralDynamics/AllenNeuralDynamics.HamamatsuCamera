using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to start capturing frames.
    /// </summary>
    [Serializable]
    public class StartCaptureException : Exception
    {
        public StartCaptureException()
            : base("Failed to start capturing frames.")
        { }

        public StartCaptureException(string message)
            : base(message)
        { }

        public StartCaptureException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected StartCaptureException(System.Runtime.Serialization.SerializationInfo info,
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
