using System.Collections.Generic;
using System.Drawing;

namespace AllenNeuralDynamics.HamamatsuCamera.Models
{

    /// <summary>
    /// Used to store an AutoCrop and a ManualCrop.
    /// </summary>
    internal sealed class CropSettings
    {
        internal double HPos { get; set; }
        internal double HSize { get; set; }
        internal double VPos { get; set; }
        internal double VSize { get; set; }
        internal bool Mode { get; set; }

        internal IEnumerable<double> GetCrop()
        {
            yield return HPos;
            yield return HSize;
            yield return VPos;
            yield return VSize;
        }

        internal void SetCrop(Rectangle crop, int binning)
        {
            HPos = crop.X * binning;
            HSize = crop.Width * binning;
            VPos = crop.Y * binning;
            VSize = crop.Height * binning;
        }
    }
}
