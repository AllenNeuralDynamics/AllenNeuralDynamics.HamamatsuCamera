using OpenCV.Net;

namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Contains the <see cref="IplImage"/> and metadata for a given image from the camera.
    /// Used to pass all data frames through Rx pipelines.
    /// </summary>
    public sealed class Frame : IFrameContainer
    {
        /// <summary>
        /// Image data represented by an <see cref="IplImage"/>.
        /// </summary>
        public IplImage Image { get; set; }

        /// <summary>
        /// Frame count of the current image.
        /// </summary>
        public ulong FrameCounter { get; set; }

        /// <summary>
        /// Timestamp of the current image.
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Left bound of the crop.
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Top bound of the crop.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Width of the crop.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the crop.
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
