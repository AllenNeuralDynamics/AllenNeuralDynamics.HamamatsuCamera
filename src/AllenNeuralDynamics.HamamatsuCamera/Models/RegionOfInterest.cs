namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Represents a region of interest in the frame.
    /// </summary>
    public struct RegionOfInterest
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public RegionOfInterest(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
