using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to initialize the DCAM API.
    /// </summary>
    [Serializable]
    public class ApiInitializationException : Exception
    {
        public ApiInitializationException()
            : base("Failed to initialize DCAM API.")
        { }

        public ApiInitializationException(string message)
            : base(message)
        { }

        public ApiInitializationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected ApiInitializationException(System.Runtime.Serialization.SerializationInfo info,
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
