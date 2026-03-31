using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to initialize or uninitialize the DCAM API.
    /// </summary>
    [Serializable]
    public class ApiInitializationException : Exception
    {
        /// <summary>
        /// Thrown when failing to initialize the DCAM API
        /// </summary>
        public ApiInitializationException()
            : base("Failed to initialize DCAM API.")
        { }

        /// <summary>
        /// Thrown when failing to uninitialize the DCAM API
        /// </summary>
        public ApiInitializationException(string message)
            : base(message)
        { }
    }
}
