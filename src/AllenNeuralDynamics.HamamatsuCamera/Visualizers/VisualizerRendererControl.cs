using OpenCV.Net;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    /// <summary>
    /// Used to efficiently display images in the <see cref="ProcessingView"/>.
    /// </summary>
    public class VisualizerRendererControl : Control
    {
        private readonly object _lock = new();

        private volatile bool _isPainting;
        private Bitmap _displayBitmap;
        private int[,] _srcMap;
        private int _lastInWidth, _lastInHeight, _lastOutWidth, _lastOutHeight;
        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        /// <summary>
        /// Configure the control to allow for efficient painting of images.
        /// </summary>
        public VisualizerRendererControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        /// <summary>
        /// Checks if the source map is invalid.
        /// </summary>
        /// <param name="inWidthInPixels">Latest input image width in pixels</param>
        /// <param name="inHeightInPixels">Latest input image height in pixels</param>
        /// <param name="outWidthInPixels">Latest output image width in pixels</param>
        /// <param name="outHeightInPixels">Latest output image height in pixels</param>
        /// <returns>True if the source map is null, or if the latest widths/heights do not match what is stored.</returns>
        private bool IsSourceMapInvalid(int inWidthInPixels, int inHeightInPixels, int outWidthInPixels, int outHeightInPixels)
        {
            return _srcMap == null || _lastInWidth != inWidthInPixels || _lastInHeight != inHeightInPixels || _lastOutWidth != outWidthInPixels || _lastOutHeight != outHeightInPixels;
        }

        /// <summary>
        /// Copies the Mono8 or Mono16 input image to the display bitmap. The display bitmap is stored to minimize
        /// reallocations. Its dimensions are dependent on the ClientRectangle.
        /// A source map is used to precompute the destinations of the input pixels in the output bitmap.
        /// This is also stored to minimize reallocations. During the copy step, nearest neighbor interpolation
        /// is conducted.
        /// </summary>
        /// <param name="image">New input image.</param>
        /// <param name="imageScale">Multiplication factor for pixel values.</param>
        public unsafe void ProcessImage(IplImage image, double imageScale)
        {
            try
            {
                lock (_lock)
                {
                    // Return early if no input image or zero size client rectangle.
                    if (image == null || ClientRectangle.Width == 0 || ClientRectangle.Height == 0 || _isPainting)
                        return;

                    var inWidthInPixels = image.Width;
                    var inHeightInPixels = image.Height;
                    var inBytesPerPixel = image.WidthStep / inWidthInPixels;

                    var displayWidth = ClientRectangle.Width;
                    var displayHeight = ClientRectangle.Height;
                    var outWidthInPixels = displayWidth;
                    var outHeightInPixels = displayHeight;

                    // Allocate or reuse display bitmap
                    if (_displayBitmap == null || _displayBitmap.Width != outWidthInPixels || _displayBitmap.Height != outHeightInPixels)
                    {
                        _displayBitmap?.Dispose();
                        _displayBitmap = new Bitmap(outWidthInPixels, outHeightInPixels, PixelFormat.Format24bppRgb);
                    }

                    // Precompute destination -> source map if size changed
                    var srcMapInvalid = IsSourceMapInvalid(inWidthInPixels, inHeightInPixels, outWidthInPixels, outHeightInPixels);
                    if (srcMapInvalid)
                    {
                        _srcMap = new int[outHeightInPixels, outWidthInPixels];
                        scaleX = (float)inWidthInPixels / outWidthInPixels;
                        scaleY = (float)inHeightInPixels / outHeightInPixels;
                        Parallel.For(0, outHeightInPixels, outY =>
                        {
                            int srcY = Math.Min((int)(outY * scaleY), inHeightInPixels - 1);
                            for (int outX = 0; outX < outWidthInPixels; outX++)
                            {
                                int srcX = Math.Min((int)(outX * scaleX), inWidthInPixels - 1);
                                _srcMap[outY, outX] = (srcY << 16) | srcX;
                            }
                        });

                        _lastInWidth = inWidthInPixels;
                        _lastInHeight = inHeightInPixels;
                        _lastOutWidth = outWidthInPixels;
                        _lastOutHeight = outHeightInPixels;
                    }

                    // Nearest Neighbor Interpolation
                    var outBitmapData = _displayBitmap.LockBits(
                        new Rectangle(0, 0, outWidthInPixels, outHeightInPixels),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format24bppRgb);

                    if (image.Depth == IplDepth.U8)
                    {
                        byte* inBase = (byte*)image.ImageData;
                        byte* outBase = (byte*)outBitmapData.Scan0;

                        int inStride = image.WidthStep;
                        int outStride = outBitmapData.Stride;

                        Parallel.For(0, outHeightInPixels, outY =>
                        {
                            byte* outRow = outBase + outY * outStride;

                            for (int outX = 0; outX < outWidthInPixels; outX++)
                            {
                                int packed = _srcMap[outY, outX];
                                int srcY = packed >> 16;
                                int srcX = packed & 0xFFFF;

                                byte* inPixel = inBase + srcY * inStride + srcX * inBytesPerPixel;
                                byte pixelValue = *inPixel;

                                byte scaledValue = (byte)Math.Min(pixelValue * imageScale, byte.MaxValue);

                                int outOffset = outX * 3;
                                outRow[outOffset + 0] = scaledValue; // B
                                outRow[outOffset + 1] = scaledValue; // G
                                outRow[outOffset + 2] = scaledValue; // R
                            }
                        });
                    }
                    else if (image.Depth == IplDepth.U16)
                    {
                        ushort* inBase = (ushort*)image.ImageData;
                        byte* outBase = (byte*)outBitmapData.Scan0;

                        int outStride = outBitmapData.Stride;

                        Parallel.For(0, outHeightInPixels, outY =>
                        {
                            byte* outRow = outBase + outY * outStride;

                            for (int outX = 0; outX < outWidthInPixels; outX++)
                            {
                                int packed = _srcMap[outY, outX];
                                int srcY = packed >> 16;
                                int srcX = packed & 0xFFFF;

                                ushort* inPixel = inBase + srcY * image.Width + srcX;
                                byte pixelValue = (byte)((*inPixel) >> 8);

                                byte scaledValue = (byte)Math.Min(pixelValue * imageScale, byte.MaxValue);

                                int outOffset = outX * 3;
                                outRow[outOffset + 0] = scaledValue; // B
                                outRow[outOffset + 1] = scaledValue; // G
                                outRow[outOffset + 2] = scaledValue; // R
                            }
                        });
                    }

                    _displayBitmap.UnlockBits(outBitmapData);

                    // Invalidate
                    if (IsHandleCreated)
                    {
                        try
                        {
                            BeginInvoke((MethodInvoker)(() => Invalidate()));
                        }
                        catch (ObjectDisposedException)
                        {
                            // Suppress Error
                        }
                    }

                    image.Dispose();
                }
            }
            catch (InvalidOperationException)
            {
                ConsoleLogger.SuppressError();
            }
        }

        /// <summary>
        /// Paints the pre-scaled display bitmap
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            lock (_lock)
            {
                _isPainting = true;
                try
                {
                    if (_displayBitmap != null)
                        e.Graphics.DrawImageUnscaled(_displayBitmap, 0, 0);
                }
                finally
                {
                    _isPainting = false;
                }
            }
        }

        /// <summary>
        /// Disposes the display bitmap
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _displayBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
