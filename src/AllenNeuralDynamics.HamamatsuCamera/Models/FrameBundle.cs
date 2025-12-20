using OpenCV.Net;

namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Used in pipelines that downsample the image data while aggregating the
    /// metadata. A single frame bundle contains one <see cref="IplImage"/> from each interleaved channel
    /// while containing the <see cref="FrameData"/> from all frames contained in the bundle
    /// </summary>
    public sealed class FrameBundle : IFrameContainer
    {
        /// <summary>
        /// Downsampled <see cref="IplImage"/>, one for each interleaved signal.
        /// </summary>
        public IplImage[] Images { get; set; }

        /// <summary>
        /// Aggregated <see cref="FrameData"/> for all of the frames contained in a bundle.
        /// </summary>
        public FrameData[] Frames { get; set; }
    }
}
