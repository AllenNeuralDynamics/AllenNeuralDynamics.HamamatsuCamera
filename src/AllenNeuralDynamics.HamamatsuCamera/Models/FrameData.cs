namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Metadata version of a <see cref="Frame"/> to be used inside of
    /// <see cref="FrameBundle"/>
    /// </summary>
    public class FrameData
    {
        /// <summary>
        /// Frame counter of frame.
        /// </summary>
        public ulong FrameCounter { get; set; }

        /// <summary>
        /// Timestamp of frame.
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Left bound of crop.
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Top bound of crop.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Width of crop.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of crop.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Number of signals to deinterleave into.
        /// </summary>
        public int DeinterleaveCount { get; set; }

        /// <summary>
        /// Activity data for each <see cref="RegionOfInterest"/>.
        /// </summary>
        public double[] RegionData { get; set; }
    }
}
