using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Exceptions
{
    /// <summary>
    /// Thrown when a property does not exist.
    /// </summary>
    [Serializable]
    public class PropertyException : Exception
    {
        public PropertyException()
            : base("Property does not exist.")
        { }

        public PropertyException(string message)
            : base(message)
        { }

        public PropertyException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected PropertyException(System.Runtime.Serialization.SerializationInfo info,
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}
