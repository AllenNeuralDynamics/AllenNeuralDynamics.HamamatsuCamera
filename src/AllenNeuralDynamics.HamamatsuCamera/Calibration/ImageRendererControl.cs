using AllenNeuralDynamics.HamamatsuCamera.Models;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    /// <summary>
    /// Used to efficiently display images from the <see cref="C13440"/> and provide
    /// features for drawing and displaying regions of interest. Additionally, calculates
    /// the crop for <see cref="CropMode.Auto"/>.
    /// </summary>
    public class ImageRendererControl : Control
    {
        private const float MinFontSize = 8.0f;
        private const float LabelFontScale = 0.8f;
        private readonly object _lock = new object();

        private volatile bool _isPainting;
        private Bitmap _displayBitmap;
        private int[,] _srcMap;
        private int _lastInWidth, _lastInHeight, _lastOutWidth, _lastOutHeight;
        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        private const float Luminance = 0.5f;
        private const float PenWidth = 2f;
        private readonly Color NextInsideColor = Color.Yellow;


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<RegionOfInterest> Regions { get; set; } = new List<RegionOfInterest>();
        public Point CurrentCropLocation { get; set; }
        public Rectangle NextCrop { get; set; }
        public CropMode CropMode { get; set; }
        public int SelectedRegion { get; set; }
        internal int ImageFullWidth { get; set; }
        internal int ImageFullHeight { get; set; }


        /// <summary>
        /// Configure the control to allow for efficient painting of images.
        /// </summary>
        public ImageRendererControl()
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
        /// reallocations. Its dimensions are dependent on the ClientRectangle and <see cref="CropMode"/>.
        /// A source map is used to precompute the destinations of the input pixels in the output bitmap.
        /// This is also stored to minimize reallocations. During the copy step, nearest neighbor interpolation
        /// is conducted and pixel values are scaled.
        /// </summary>
        /// <param name="image">New input image.</param>
        /// <param name="imageScale">Pixel scaling factor.</param>
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

                    int displayWidth, displayHeight;
                    if(CropMode == CropMode.Auto)
                    {
                        displayWidth = Math.Max(1, (int)((double)inWidthInPixels / ImageFullWidth * ClientRectangle.Width));
                        displayHeight = Math.Max(1, (int)((double)inHeightInPixels / ImageFullHeight * ClientRectangle.Height));
                    }
                    else
                    {
                        displayWidth = ClientRectangle.Width;
                        displayHeight = ClientRectangle.Height;
                    }
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
                    else if(image.Depth == IplDepth.U16)
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
            catch(InvalidOperationException)
            {
                ConsoleLogger.SuppressError();
            }
        }

        /// <summary>
        /// Paints the pre-scaled display bitmap. Draws the regions of interest
        /// and optionally draws the next crop rectangle if in AutoCrop mode.
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
                    {
                        if(CropMode == CropMode.Auto)
                            e.Graphics.DrawImageUnscaled(_displayBitmap, ScalePoint(CurrentCropLocation));
                        else
                            e.Graphics.DrawImageUnscaled(_displayBitmap, 0, 0);
                    }

                    if(Regions != null)
                    {
                        if (Regions.Count <= 0) return;

                        // Using graphics, draw rectangles and labels over the image where the regions are located. Use a different pen for the selected region if one exists
                        var penWidth = (e.Graphics.DpiX / 96f * PenWidth);
                        using (var nonSelectedRegionPen = new Pen(Color.FromArgb(Color.Red.A, (int)(Color.Red.R * Luminance), (int)(Color.Red.G * Luminance), (int)(Color.Red.B * Luminance)), penWidth))
                        using (var selectedRegionPen = new Pen(Color.FromArgb(Color.Yellow.A, (int)(Color.Yellow.R * Luminance), (int)(Color.Yellow.G * Luminance), (int)(Color.Yellow.B * Luminance)), penWidth))
                        using (var nextCropPen = new Pen(Color.FromArgb(255, NextInsideColor), penWidth))
                        using (var format = new StringFormat())
                        {
                            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            format.Alignment = StringAlignment.Center;
                            format.LineAlignment = StringAlignment.Center;

                            // For each region
                            for (var i = 0; i < Regions.Count; i++)
                            {
                                var region = Regions[i];
                                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                                var scaledRect = ScaleRectangle(rect);
                                var regionPen = nonSelectedRegionPen;
                                var fontSize = Math.Max(Math.Min(scaledRect.Height * LabelFontScale,
                                                        scaledRect.Width * LabelFontScale), MinFontSize);
                                var labelFont = new System.Drawing.Font(Font.Name, fontSize, Font.Style, Font.Unit);
                                if (i == SelectedRegion) regionPen = selectedRegionPen;
                                e.Graphics.DrawRectangle(regionPen, scaledRect);
                                e.Graphics.DrawString(i.ToString(), labelFont, Brushes.White, scaledRect, format);
                            }
                            if (CropMode == CropMode.Auto) e.Graphics.DrawRectangle(nextCropPen, ScaleRectangle(NextCrop));
                        }
                    }
                }
                finally
                {
                    _isPainting = false;
                }
            }
        }

        /// <summary>
        /// Scales a rectangle from input image coordinates to output image coordinates.
        /// </summary>
        /// <param name="roi">Region of Interest</param>
        /// <returns></returns>
        private Rectangle ScaleRectangle(Rectangle roi)
        {
            return new Rectangle(
                    (int)(roi.X / scaleX),
                    (int)(roi.Y / scaleY),
                    (int)(roi.Width / scaleX),
                    (int)(roi.Height / scaleY)
                );
        }

        /// <summary>
        /// Scales a point from input image coordinates to output image coordinates.
        /// </summary>
        /// <param name="loc">Input location.</param>
        /// <returns></returns>
        private Point ScalePoint(Point loc)
        {
            return new Point(
                    (int)(loc.X / scaleX),
                    (int)(loc.Y / scaleY)
                );
        }

        /// <summary>
        /// Diposes the display bitmap
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
