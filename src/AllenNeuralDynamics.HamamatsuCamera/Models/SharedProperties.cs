namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Stores the <see cref="Processing"/> node's properties in a place that can
    /// be accessed by the <see cref="CameraCapture"/>
    /// </summary>
    internal static class ProcessingShared
    {
        public static byte DeinterleaveCount { get; set; }
    }

    /// <summary>
    /// Stores the <see cref="CsvWriter"/> node's properties in a place that can
    /// be accessed by the <see cref="CameraCapture"/>
    /// </summary>
    internal static class CsvWriterShared
    {
        internal static CsvWriterProperties Properties { get; set; }
    }

    /// <summary>
    /// Stores the <see cref="TiffWriter"/> node's properties in a place that can
    /// be accessed by the <see cref="CameraCapture"/>
    /// </summary>
    internal static class TiffWriterShared
    {
        internal static TiffWriterProperties Properties { get; set; }
    }

}
