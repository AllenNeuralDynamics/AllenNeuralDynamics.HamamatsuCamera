namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Auto: The camera's crop will be automatically determined by the defined <see cref="RegionOfInterest"/>
    /// Manual: The camera's crop will manually be set in the <see cref="Calibration.CalibrationForm"/> or by the subarray properties
    /// in a settings file.
    /// </summary>
    public enum CropMode
    {
        /// <summary>
        /// Represents Auto Crop.
        /// </summary>
        Auto,

        /// <summary>
        /// Represents Manual Crop.
        /// </summary>
        Manual
    }
}
