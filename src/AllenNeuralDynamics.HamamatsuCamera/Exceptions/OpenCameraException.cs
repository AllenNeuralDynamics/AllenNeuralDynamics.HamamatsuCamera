using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when failing to open the camera.
    /// </summary>
    [Serializable]
    public class OpenCameraException : Exception
    {
        public OpenCameraException()
            : base("Failed to open camera.")
        { }

        public OpenCameraException(string message)
            : base(message)
        { }

        public OpenCameraException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected OpenCameraException(System.Runtime.Serialization.SerializationInfo info,
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
