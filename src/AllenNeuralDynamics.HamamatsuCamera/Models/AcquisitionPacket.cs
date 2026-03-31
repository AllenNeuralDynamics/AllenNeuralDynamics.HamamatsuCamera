using AllenNeuralDynamics.HamamatsuCamera.API;
using System;

namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Packets used to pass metadata from the acquisition thread to the processing thread.
    /// </summary>
    internal readonly struct AcquisitionPacket
    {
        public readonly int FrameId;
        public readonly DCAM_TIMESTAMP Timestamp;
        public readonly IntPtr DataPtr;
        public readonly int RowBytes;
        public readonly int Left;
        public readonly int Top;
        public readonly int Width;
        public readonly int Height;

        internal AcquisitionPacket(int frameId, DCAM_TIMESTAMP timestamp, IntPtr dataPtr, int rowBytes, int left, int top, int width, int height)
        {
            FrameId = frameId;
            Timestamp = timestamp;
            DataPtr = dataPtr;
            RowBytes = rowBytes;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return $"{FrameId},{Timestamp.sec},{Timestamp.microsec}";
        }
    }
}
