namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Data used by the <see cref="Processing"/> nodes visualizer when the <see cref="C13440"/> node has bunding disabled.
    /// The <see cref="Visualizers.ProcessingVisualizer"/> batches of these to the <see cref="Visualizers.ProcessingView"/>.
    /// </summary>
    internal sealed class VisualizerData
    {
        internal VisualizerData(Frame f)
        {
            FrameCounter = f.FrameCounter;
            Timestamp = f.Timestamp;
            DeinterleaveCount = f.DeinterleaveCount;
            RegionData = f.RegionData;
        }

        internal ulong FrameCounter { get; set; }
        internal double Timestamp { get; set; }
        internal int DeinterleaveCount { get; set; }
        internal double[] RegionData { get; set; }
    }
}
