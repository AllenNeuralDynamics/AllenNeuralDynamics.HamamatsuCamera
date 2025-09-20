using AllenNeuralDynamics.HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    public partial class ImageVisualizer : UserControl
    {
        #region Constants

        private const float MinFontSize = 8.0f;
        private const int FillOpacity = 85;
        private const float Luminance = 0.5f;
        private const float LabelFontScale = 0.8f;
        private const double MaxImageScale = 50.0;
        private const double MinImageScale = 1.0;
        //private const int DisplayBPP = 3;
        private const double TargetPeriod = 0.025; // Millis
        private const int PenWidth = 10;



        #endregion

        #region Variables


        private readonly Dictionary<DCAM_PIXELTYPE, int> NumChansByPixelType = new Dictionary<DCAM_PIXELTYPE, int>()
        {
            { DCAM_PIXELTYPE.MONO8      , 1 },
            { DCAM_PIXELTYPE.MONO16     , 1 },
            { DCAM_PIXELTYPE.MONO12     , 1 },
            { DCAM_PIXELTYPE.MONO12P    , 1 },
            { DCAM_PIXELTYPE.RGB24      , 3 },
            { DCAM_PIXELTYPE.RGB48      , 3 },
            { DCAM_PIXELTYPE.BGR24      , 3 },
            { DCAM_PIXELTYPE.BGR48      , 3 },
            { DCAM_PIXELTYPE.NONE       , 3 }
        };

        /*********** MISCELLANEOUS ***********/
        private object Lock = new object();
        private bool FirstFrame;

        /*********** SENSOR INFORMATION ***********/
        public int NumPixelsHorz;
        public int NumPixelsVert;
        public int Binning;

        /*********** DISPLAYED IMAGE ***********/
        private const int DispImg_BPP = 3;
        private int DispImg_WidthBins;
        private int DispImg_HeightBins;
        private Bitmap DispImg;


        /*********** MASK IMAGE ***********/
        private Bitmap MaskImg;
        private const double MaskAlpha = 0.5;
        private Color MaskImg_Color = Color.Gray;
        

        private double ImageScale = 11.0;
        private double ScaleIncrement = 5.0;
        private Bitmap NextInsideMask;
        private Color NextInsideColor = Color.Yellow;

        /*********** ACQUIRED IMAGE ***********/
        private int AcqImg_BPP;
        private int AcqImg_NumChan;
        private Rectangle AcqImg_Crop;


        /*********** REGIONS OF INTEREST ***********/
        private Point lastPoint = Point.Empty;

        private bool Dragging;
        private bool Moving;
        private bool Drawing;

        private int SelectedRegion;
        private Point DragStartPosition;

        /*********** FRAMES PER SECOND DISPLAY ***********/
        private double CurrentTimestamp;
        private double PrevTimestamp;
        private int CurrentFramestamp;
        private int PrevFramestamp;

        public Dictionary<int, int> LookupTable;


        #endregion

        #region Form Access

        public event EventHandler RegionsChanged;

        public List<Rectangle> Regions = new List<Rectangle>();

        public Rectangle NextCrop;
        public Rectangle PrevCrop;


        public Rectangle Display;

        public CropMode CropMode = CropMode.Auto;

        public ImageVisualizer()
        {
            try
            {
                InitializeComponent();
                FirstFrame = true;
                LookupTable = new Dictionary<int, int>();
                for(int i = 0; i <= ushort.MaxValue; i++)
                {
                    LookupTable[i] = i;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ImageVisualizer\nMessage: {ex.Message}");
            }
        }

        public void UpdateFrame(Frame frame)
        {
            try
            {
                if (frame.CameraTimestamp - PrevTimestamp > TargetPeriod)
                {
                    lock (Lock)
                    {
                        InitFirstFrame(frame);
                        ReadMetadata(frame);
                        UpdateDisplayedImage(frame);

                        var clone = GetMaskedClone();

                        if (Regions.Count > 0)
                            DrawRegions(ref clone);


                        // Update the displayed image
                        if (Image_PictureBox.Image != null)
                            Image_PictureBox.Image.Dispose();
                        Image_PictureBox.Image = clone;

                        UpdateFPS(frame);
                    }
                }
            }
            catch(Exception ex)
            {
                //Console.WriteLine($"Error: UpdateFrame\nMessage: {ex.Message}");
            }
        }
        

        private Bitmap GetMaskedClone()
        {
            Bitmap clone = new Bitmap(DispImg_WidthBins, DispImg_HeightBins, PixelFormat.Format24bppRgb);
            try
            {
                if (CropMode == CropMode.Auto)
                {
                    // Mask pixels outside the Next Crop and Current Crop
                    unsafe
                    {
                        BitmapData dispImg_bitmapData = DispImg.LockBits(new Rectangle(0, 0, DispImg_WidthBins, DispImg_HeightBins), ImageLockMode.ReadOnly, DispImg.PixelFormat);
                        BitmapData clone_bitmapData = clone.LockBits(new Rectangle(0, 0, DispImg_WidthBins, DispImg_HeightBins), ImageLockMode.WriteOnly, clone.PixelFormat);

                        var widthInBytes = DispImg_WidthBins * DispImg_BPP;

                        byte* dispImg_ptrFirstPixel = (byte*)dispImg_bitmapData.Scan0;
                        byte* clone_ptrFirstPixel = (byte*)clone_bitmapData.Scan0;

                        Parallel.For(0, DispImg_HeightBins, y =>
                        {
                            var dispImg_ptrCurrLine = dispImg_ptrFirstPixel + y * widthInBytes;
                            var clone_ptrCurrLine = clone_ptrFirstPixel + y * widthInBytes;

                            bool isNextCropRow = y >= NextCrop.Y && y <= NextCrop.Y + NextCrop.Height;
                            bool isAcqCropRow = y >= AcqImg_Crop.Y && y <= AcqImg_Crop.Y + AcqImg_Crop.Height;
                            if (!isNextCropRow && !isAcqCropRow)
                            {
                                for (var x = 0; x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                            else if (!isNextCropRow && isAcqCropRow)
                            {
                                // Before Crop
                                for (var x = 0; x < AcqImg_Crop.X; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }

                                //During Crop
                                for (var x = AcqImg_Crop.X; x < AcqImg_Crop.X + AcqImg_Crop.Width; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];

                                }

                                // After Crop
                                for (var x = AcqImg_Crop.X + AcqImg_Crop.Width; x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                            else if (isNextCropRow && !isAcqCropRow)
                            {
                                // Before Crop
                                for (var x = 0; x < NextCrop.X; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }

                                //During Crop
                                for (var x = NextCrop.X; x < NextCrop.X + NextCrop.Width; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];

                                }

                                // After Crop
                                for (var x = NextCrop.X + NextCrop.Width; x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                            else if (isNextCropRow && isAcqCropRow)
                            {
                                // Before Crops
                                for (var x = 0; x < Math.Min(AcqImg_Crop.X, NextCrop.X); x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }

                                bool isDisjoint = (NextCrop.X > AcqImg_Crop.X + AcqImg_Crop.Width) || (AcqImg_Crop.X > NextCrop.X + NextCrop.Width);
                                if (isDisjoint)
                                {
                                    // Left most crop
                                    for (var x = Math.Min(AcqImg_Crop.X, NextCrop.X); x < Math.Min(AcqImg_Crop.X + AcqImg_Crop.Width, NextCrop.X + NextCrop.Width); x++)
                                    {
                                        clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                        clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                        clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];
                                    }

                                    // Between crops
                                    for (var x = Math.Min(AcqImg_Crop.X + AcqImg_Crop.Width, NextCrop.X + NextCrop.Width); x < Math.Max(AcqImg_Crop.X, NextCrop.X); x++)
                                    {
                                        clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                        clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                        clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                    }

                                    // Right most crop
                                    for (var x = Math.Max(AcqImg_Crop.X, NextCrop.X); x < Math.Max(AcqImg_Crop.X + AcqImg_Crop.Width, NextCrop.X + NextCrop.Width); x++)
                                    {
                                        clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                        clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                        clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];
                                    }
                                }
                                else
                                {
                                    for (var x = Math.Min(AcqImg_Crop.X, NextCrop.X); x < Math.Max(AcqImg_Crop.X + AcqImg_Crop.Width, NextCrop.X + NextCrop.Width); x++)
                                    {
                                        clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                        clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                        clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];
                                    }
                                }

                                // After Crops
                                for (var x = Math.Max(AcqImg_Crop.X + AcqImg_Crop.Width, NextCrop.X + NextCrop.Width); x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                            //else
                            //{
                            //    for (var x = 0; x < DispImg_WidthBins; x++)
                            //    {
                            //        clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                            //        clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                            //        clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];
                            //    }
                            //}

                        });

                        DispImg.UnlockBits(dispImg_bitmapData);
                        clone.UnlockBits(clone_bitmapData);
                    }
                }
                else
                {
                    // Mask pixels outside the Next Crop and Current Crop
                    unsafe
                    {
                        BitmapData dispImg_bitmapData = DispImg.LockBits(new Rectangle(0, 0, DispImg_WidthBins, DispImg_HeightBins), ImageLockMode.ReadOnly, DispImg.PixelFormat);
                        BitmapData clone_bitmapData = clone.LockBits(new Rectangle(0, 0, DispImg_WidthBins, DispImg_HeightBins), ImageLockMode.WriteOnly, clone.PixelFormat);

                        var widthInBytes = DispImg_WidthBins * DispImg_BPP;

                        byte* dispImg_ptrFirstPixel = (byte*)dispImg_bitmapData.Scan0;
                        byte* clone_ptrFirstPixel = (byte*)clone_bitmapData.Scan0;

                        Parallel.For(0, DispImg_HeightBins, y =>
                        {
                            var dispImg_ptrCurrLine = dispImg_ptrFirstPixel + y * widthInBytes;
                            var clone_ptrCurrLine = clone_ptrFirstPixel + y * widthInBytes;

                            bool isAcqCropRow = y >= AcqImg_Crop.Y && y <= AcqImg_Crop.Y + AcqImg_Crop.Height;
                            if (!isAcqCropRow)
                            {
                                for (var x = 0; x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                            else
                            {
                                // Before Crop
                                for (var x = 0; x < AcqImg_Crop.X; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }

                                //During Crop
                                for (var x = AcqImg_Crop.X; x < AcqImg_Crop.X + AcqImg_Crop.Width; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = dispImg_ptrCurrLine[DispImg_BPP * x];
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = dispImg_ptrCurrLine[DispImg_BPP * x + 1];
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = dispImg_ptrCurrLine[DispImg_BPP * x + 2];

                                }

                                // After Crop
                                for (var x = AcqImg_Crop.X + AcqImg_Crop.Width; x < DispImg_WidthBins; x++)
                                {
                                    clone_ptrCurrLine[DispImg_BPP * x] = ApplyMask(MaskImg_Color.B, dispImg_ptrCurrLine[DispImg_BPP * x]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 1] = ApplyMask(MaskImg_Color.G, dispImg_ptrCurrLine[DispImg_BPP * x + 1]);
                                    clone_ptrCurrLine[DispImg_BPP * x + 2] = ApplyMask(MaskImg_Color.R, dispImg_ptrCurrLine[DispImg_BPP * x + 2]);
                                }
                            }
                        });

                        DispImg.UnlockBits(dispImg_bitmapData);
                        clone.UnlockBits(clone_bitmapData);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: GetMaskedClone\nMessage: {ex.Message}");
            }

            return clone;
        }

        private static byte ApplyMask(byte maskColor, byte imgColor)
        {
            return (byte)(maskColor + imgColor * (1 - MaskAlpha));
        }

        private void InitFirstFrame(Frame frame)
        {
            // If not first frame, do nothing
            if (!FirstFrame) return;

            // Read in data 
            DispImg_WidthBins = NumPixelsHorz / Binning;
            DispImg_HeightBins = NumPixelsVert / Binning;
            DispImg = new Bitmap(DispImg_WidthBins, DispImg_HeightBins, PixelFormat.Format24bppRgb);
            NextCrop = GetNextCrop();
            //MaskImg = CreateMask(OutsideColor);
            //NextInsideMask = CreateMask(NextInsideColor);

            // Update FirstFrame flag
            FirstFrame = false;
        }

        private void ReadMetadata(Frame frame)
        {
            try
            {
                AcqImg_Crop = new Rectangle(frame.Left, frame.Top, frame.Width, frame.Height);
                AcqImg_BPP = frame.bufframe.rowbytes / frame.Width;
                AcqImg_NumChan = NumChansByPixelType[frame.PixelType];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ReadMetadata\nMessage: {ex.Message}");
            }
        }

        private void UpdateDisplayedImage(Frame frame)
        {
            unsafe
            {
                // LockBits of display image where new frame is located
                BitmapData dispImg_bitmapData = DispImg.LockBits(new Rectangle(0,0,DispImg_WidthBins, DispImg_HeightBins), ImageLockMode.WriteOnly, DispImg.PixelFormat);

                int dispImg_offsetToFirstPixel = (frame.Top * DispImg_WidthBins + frame.Left) * DispImg_BPP;

                int acqImg_widthInBytes = frame.bufframe.rowbytes;
                int dispImg_croppedWidthInBytes = DispImg_WidthBins * DispImg_BPP;

                byte* acqImg_ptrFirstBin = (byte*)frame.bufframe.buf;
                byte* dispImg_ptrFirstNewBin = (byte*)dispImg_bitmapData.Scan0 + dispImg_offsetToFirstPixel;

                if(frame.PixelType == DCAM_PIXELTYPE.MONO8)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            var MSB = acqImg_ptrCurrLine[x];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 0] = MSB;
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = MSB;
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = MSB;
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.MONO12)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through bytes in row grouping by 3 bytes
                        for (int x = 0; x < acqImg_widthInBytes; x+= 3)
                        {
                            var MSB_PixelOne = acqImg_ptrCurrLine[x];
                            var MSB_PixelTwo = acqImg_ptrCurrLine[x + 2];
                            var dispImg_PixelNum = x / 3;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 0] = MSB_PixelOne;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 1] = MSB_PixelOne;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 2] = MSB_PixelOne;
                            dispImg_PixelNum += 1;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 0] = MSB_PixelTwo;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 1] = MSB_PixelTwo;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 2] = MSB_PixelTwo;
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.MONO12P)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through bytes in row grouping by 3 bytes
                        for (int x = 0; x < acqImg_widthInBytes; x += 3)
                        {
                            // Last 4 of byte two followed by first four of byte one
                            byte MSB_PixelOne = ((byte)((byte)((acqImg_ptrCurrLine[x + 1] & 0x0F) << 4) | (acqImg_ptrCurrLine[x] >> 4)));
                            var MSB_PixelTwo = acqImg_ptrCurrLine[x + 2];
                            var dispImg_PixelNum = x / 3;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 0] = MSB_PixelOne;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 1] = MSB_PixelOne;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 2] = MSB_PixelOne;
                            dispImg_PixelNum += 1;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 0] = MSB_PixelTwo;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 1] = MSB_PixelTwo;
                            dispImg_ptrCurrLine[DispImg_BPP * dispImg_PixelNum + 2] = MSB_PixelTwo;
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.MONO16)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        ushort* acqImg_ptrCurrLine = (ushort*)(acqImg_ptrFirstBin + y * acqImg_widthInBytes);
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            var MSB = (byte)(LookupTable[acqImg_ptrCurrLine[x]] >> 8);
                            dispImg_ptrCurrLine[DispImg_BPP * x] = MSB;     // Blue
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = MSB;   // Green
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = MSB;   // Red
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.RGB24)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            dispImg_ptrCurrLine[DispImg_BPP * x + 0] = acqImg_ptrCurrLine[AcqImg_BPP * x];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = acqImg_ptrCurrLine[AcqImg_BPP * x + 1];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = acqImg_ptrCurrLine[AcqImg_BPP * x + 2];
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.RGB48)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            dispImg_ptrCurrLine[DispImg_BPP * x + 0] = acqImg_ptrCurrLine[AcqImg_BPP * x];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = acqImg_ptrCurrLine[AcqImg_BPP * x + 2];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = acqImg_ptrCurrLine[AcqImg_BPP * x + 4];
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.BGR24)
                {// Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            dispImg_ptrCurrLine[DispImg_BPP * x + 0] = acqImg_ptrCurrLine[AcqImg_BPP * x + 2];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = acqImg_ptrCurrLine[AcqImg_BPP * x + 1];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = acqImg_ptrCurrLine[AcqImg_BPP * x];
                        }
                    });
                }
                else if(frame.PixelType == DCAM_PIXELTYPE.BGR48)
                {
                    // Loop in parallel through all of the rows of the new frame
                    Parallel.For(0, AcqImg_Crop.Height, y =>
                    {
                        // Get pointer to rows in new frame and display image
                        byte* acqImg_ptrCurrLine = acqImg_ptrFirstBin + y * acqImg_widthInBytes;
                        byte* dispImg_ptrCurrLine = dispImg_ptrFirstNewBin + y * dispImg_croppedWidthInBytes;

                        // Loop through each pixel in the row
                        for (int x = 0; x < frame.Width; x++)
                        {
                            dispImg_ptrCurrLine[DispImg_BPP * x + 0] = acqImg_ptrCurrLine[AcqImg_BPP * x + 4];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 1] = acqImg_ptrCurrLine[AcqImg_BPP * x + 2];
                            dispImg_ptrCurrLine[DispImg_BPP * x + 2] = acqImg_ptrCurrLine[AcqImg_BPP * x];
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"Pixel Type Not Supported: None");
                }

                DispImg.UnlockBits(dispImg_bitmapData);
            }
        }

        internal void UpdateBinning(int newBinning)
        {
            try
            {
                var s = (double)Binning / newBinning;
                for(int i = 0; i < Regions.Count; i++)
                    Regions[i] = new Rectangle((int)(Regions[i].X * s), (int)(Regions[i].Y * s), (int)(Regions[i].Width * s), (int)(Regions[i].Height * s));

                DispImg_WidthBins = NumPixelsHorz / newBinning;
                DispImg_HeightBins = NumPixelsVert / newBinning;
                DispImg = new Bitmap(DispImg_WidthBins, DispImg_HeightBins, PixelFormat.Format24bppRgb);
                Binning = newBinning;
                NextCrop = GetNextCrop();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: UpdateBinning()\nMessage: {ex.Message}");
            }
        }

        private void UpdateFPS(Frame frame)
        {
            try
            {
                string fpsValue;
                if (PrevFramestamp != 0)
                {
                    var fps = (frame.Framestamp - PrevFramestamp) / (frame.CameraTimestamp - PrevTimestamp);
                    fpsValue = fps.ToString("0.##") + " Hz";
                }
                else
                    fpsValue = "NA";

                Action safeUpdateFPSVal = delegate { FPSVal_Label.Text = fpsValue; };
                try
                {
                    if (!FPSVal_Label.IsDisposed)
                        Invoke(safeUpdateFPSVal);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }

                PrevTimestamp = frame.CameraTimestamp;
                PrevFramestamp = frame.Framestamp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UpdateFPS\nMessage: {ex.Message}");
            }
        }

        private void DrawRegions(ref Bitmap clone)
        {
            try
            {
                // Using graphics, draw rectangles and labels over the image where the regions are located. Use a different pen for the selected region if one exists
                using (var graphics = Graphics.FromImage(clone))
                using (var nonSelectedRegionPen = new Pen(Color.FromArgb(Color.Red.A, (int)(Color.Red.R * Luminance), (int)(Color.Red.G * Luminance), (int)(Color.Red.B * Luminance)), PenWidth))
                using (var selectedRegionPen = new Pen(Color.FromArgb(Color.Yellow.A, (int)(Color.Yellow.R * Luminance), (int)(Color.Yellow.G * Luminance), (int)(Color.Yellow.B * Luminance)), PenWidth))
                using (var cropPen = new Pen(Color.Green, PenWidth))
                using (var nextCropPen = new Pen(Color.FromArgb(255, NextInsideColor), PenWidth))
                using (var format = new StringFormat())
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    // For each region
                    for (int i = 0; i < Regions.Count; i++)
                    {
                        var rect = Regions[i];
                        var regionPen = nonSelectedRegionPen;
                        var fontSize = Math.Max(Math.Min(rect.Height * LabelFontScale,
                                                rect.Width * LabelFontScale), MinFontSize);
                        var labelFont = new Font(Font.Name, fontSize, Font.Style, Font.Unit);
                        //if (i == SelectedRegion) brush = new Pen(Color.FromArgb(brush.Color.A, (int)(brush.Color.R / Luminance), (int)(brush.Color.G / Luminance), (int)(brush.Color.B / Luminance)));
                        if (i == SelectedRegion) regionPen = selectedRegionPen;

                        if(CropMode == CropMode.Auto) graphics.DrawRectangle(nextCropPen, NextCrop);
                        graphics.DrawRectangle(regionPen, rect);
                        graphics.DrawString(i.ToString(), labelFont, Brushes.White, rect, format);
                        graphics.DrawRectangle(cropPen, AcqImg_Crop);
                        // Draw the rectangle and label it with the region index
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: DrawRegions\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Image Processing

        //private Bitmap CloneAndConvert(Frame frame)
        //{
        //    // TODO:
        //    // 
        //    try
        //    {
                

        //        //if (frame.PixelType == DCAM_PIXELTYPE.MONO8)
        //        //    return ConvertMono8(frame);
        //        //else if (frame.PixelType == DCAM_PIXELTYPE.MONO16)
        //        //    return ConvertMono16(frame);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: CloneAndConvert\nMessage: {ex.Message}");
        //    }
        //    return new Bitmap(DisplayWidth, DisplayHeight, PixelFormat.Format24bppRgb);
        //}

        //private Bitmap ConvertMono16(Frame frame)
        //{
        //    var output_bitmap = new Bitmap(DisplayWidth, DisplayHeight, PixelFormat.Format24bppRgb);
        //    try
        //    {
        //        unsafe
        //        {
        //            BitmapData output_bitmapData = output_bitmap.LockBits(new Rectangle(0, 0, DisplayWidth, DisplayHeight), ImageLockMode.WriteOnly, output_bitmap.PixelFormat);

        //            int input_widthInUshorts = frame.Width;
        //            int output_widthInBytes = DisplayWidth * DisplayBPP;

        //            int output_offsetToFirstPixel = (frame.Top * DisplayWidth + frame.Left) * DisplayBPP;

        //            ushort* input_ptrFirstPixel = (ushort*)frame.bufframe.buf;
        //            byte* output_ptrFirstPixel = (byte*)output_bitmapData.Scan0 + output_offsetToFirstPixel;


        //            Parallel.For(0, frame.Height, y =>
        //            {
        //                int input_offset = y * input_widthInUshorts;
        //                int output_offset = y * output_widthInBytes;

        //                ushort* input_currentLine = input_ptrFirstPixel + input_offset;
        //                byte* output_currentLine = output_ptrFirstPixel + output_offset;

        //                for (int x = 0; x < frame.Width; x++)
        //                {
        //                    byte MSB = (byte)(input_currentLine[x] >> 8);
        //                    output_currentLine[3 * x] = MSB;
        //                    output_currentLine[3 * x + 1] = MSB;
        //                    output_currentLine[3 * x + 2] = MSB;
        //                }
        //            });
        //            output_bitmap.UnlockBits(output_bitmapData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: ConvertMono16\nMessage: {ex.Message}");
        //    }
        //    return output_bitmap;
        //}

        //private Bitmap ConvertMono8(Frame frame)
        //{
        //    var output_bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format24bppRgb);
        //    try
        //    {
        //        unsafe
        //        {
        //            BitmapData output_bitmapData = output_bitmap.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.WriteOnly, output_bitmap.PixelFormat);

        //            int input_widthInBytes = frame.bufframe.rowbytes;
        //            int output_widthInBytes = frame.Width * DispImg_BPP;

        //            byte* input_ptrFirstPixel = (byte*)frame.bufframe.buf;
        //            byte* output_ptrFirstPixel = (byte*)output_bitmapData.Scan0;

        //            Parallel.For(0, frame.Height, y =>
        //            {
        //                int input_offset = y * input_widthInBytes;
        //                int output_offset = y * output_widthInBytes;

        //                byte* input_currentLine = input_ptrFirstPixel + input_offset;
        //                byte* output_currentLine = output_ptrFirstPixel + output_offset;

        //                for (int x = 0; x < frame.Width; x++)
        //                {
        //                    byte MSB = input_currentLine[x];
        //                    output_currentLine[3 * x] = MSB;
        //                    output_currentLine[3 * x + 1] = MSB;
        //                    output_currentLine[3 * x + 2] = MSB;
        //                }
        //            });
        //            output_bitmap.UnlockBits(output_bitmapData);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: ConvertMono8\nMessage: {ex.Message}");
        //    }
        //    return output_bitmap;
        //}

        #endregion

        #region Open/Closing
        

        #endregion

        #region Event Handling

        private void Image_PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                Image_PictureBox.Focus();

                // Do nothing if no image is displayed
                if (Image_PictureBox.Image == null)
                    return;

                Display = (Rectangle)Image_PictureBox.GetType().GetProperty("ImageRectangle", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Image_PictureBox, null);


                

                // Update the dragging flag
                SelectedRegion = -1;

                if (!TestIntersection(Display, e.Location))
                    return;

                Dragging = true;

                // Find the location in the image
                DragStartPosition = LocationInFrame(e.Location);

                for (int i = 0; i < Regions.Count; i++)
                {
                    // Set the selected region on first interection
                    if (TestIntersection(Regions[i], DragStartPosition))
                    {
                        SelectedRegion = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Image_PictureBox_MouseDown\nMessage: {ex.Message}");
            }
        }
        private void Image_PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if ((Moving || Drawing) && RegionsChanged != null)
                    RegionsChanged.Invoke(this, EventArgs.Empty);

                Dragging = false;
                Moving = false;
                Drawing = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Image_PictureBox_MouseUp\nMessage: {ex.Message}");
            }
        }
        private void Image_PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // If not dragging do nothing
                if (!Dragging)
                    return;

                var currentLocation = LocationInFrame(e.Location);
                var startLocation = DragStartPosition;

                currentLocation.X = Math.Max(0, Math.Min(currentLocation.X, DispImg_WidthBins - 1));
                currentLocation.Y = Math.Max(0, Math.Min(currentLocation.Y, DispImg_HeightBins - 1));
                // TODO: If a right mouse button or scale selected region
                if (e.Button == MouseButtons.Right && SelectedRegion >= 0)
                {
                    Regions[SelectedRegion] = ScaleRegion(startLocation, currentLocation);
                }
                // Otherwise if left mouse button
                else if (e.Button == MouseButtons.Left)
                {
                    // If selected region does not exist, add the new region to the list and select it.
                    if (SelectedRegion < 0 && !Drawing)
                    {
                        var newRegion = CreateRegion(startLocation, currentLocation);
                        Regions.Add(newRegion);
                        CountVal_Label.Text = Regions.Count.ToString();
                        SelectedRegion = Regions.Count - 1;
                        Drawing = true;
                    }
                    // Else if currently drawing a new ROI
                    else if (Drawing)
                        Regions[SelectedRegion] = CreateRegion(startLocation, currentLocation);
                    // Otherwise move the existing ROI
                    else
                        Regions[SelectedRegion] = MoveRegion(startLocation, currentLocation);
                }

                NextCrop = GetNextCrop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Image_PictureBox_MouseMove\nMessage: {ex.Message}");
            }
        }
        private void Image_PictureBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            try
            {
                // Do nothing if dragging
                if (Dragging)
                    return;

                if (e.KeyCode == Keys.PageUp && ImageScale < MaxImageScale) ImageScale = Math.Min(ImageScale + ScaleIncrement, MaxImageScale);
                else if (e.KeyCode == Keys.PageDown && ImageScale > MinImageScale) ImageScale = Math.Max(ImageScale - ScaleIncrement, MinImageScale);


                // If an ROI is selected
                if (SelectedRegion >= 0)
                {

                    // Use "Delete" key to delete the ROI
                    if (e.KeyCode == Keys.Delete)
                    {
                        Regions.RemoveAt(SelectedRegion);
                        CountVal_Label.Text = Regions.Count.ToString();
                        SelectedRegion = Math.Min(SelectedRegion - 1, 0);
                        NextCrop = GetNextCrop();
                        if (RegionsChanged != null)
                            RegionsChanged.Invoke(this, EventArgs.Empty);
                    }


                    // Use "Q" key to select the next ROI
                    else if (e.KeyCode == Keys.Q)
                    {
                        SelectedRegion = (SelectedRegion + 1) % Regions.Count;
                    }

                    // TODOs: Implement these features
                    // Use CTRL + C to Copy ROI
                    else if (e.Control && e.KeyCode == Keys.C)
                    {

                    }
                    // Use CTRL + Y to Paste ROI
                    else if (e.Control && e.KeyCode == Keys.V)
                    {

                    }
                    // Use CTRL + Y to Redo ROI
                    else if (e.Control && e.KeyCode == Keys.Y)
                    {

                    }
                    // Use CTRL + Z to Undo ROI
                    else if (e.Control && e.KeyCode == Keys.Z)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Image_PictureBox_PreviewKeyDown\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Helper Functions

        private Rectangle GetNextCrop()
        {
            try
            {
                if (Regions.Count > 0)
                {
                    var hPos = (int)Math.Ceiling(Regions.Select(rect => rect.X).Min() / 4.0) * 4;
                    var hSize = (int)Math.Floor((Regions.Select(rect => rect.X + rect.Width).Max() - hPos) / 4.0) * 4;
                    var vPos = (int)Math.Ceiling(Regions.Select(rect => rect.Y).Min() / 4.0) * 4;
                    var vSize = (int)Math.Floor((Regions.Select(rect => rect.Y + rect.Height).Max() - vPos) / 4.0) * 4;
                    return new Rectangle(hPos, vPos, hSize, vSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UpdateCrop\n{ex.Message}");
            }

            return new Rectangle(0, 0, DispImg_WidthBins, DispImg_HeightBins);
        }
        private bool TestIntersection(Rectangle region, Point locInFrame)
        {
            try
            {
                // TODO: If need other polygons or need rotations, change the GetPoints method
                List<Point> regionPoints = GetPoints(region);

                // Check each edge of the rectangle to see if point lies of the edge, all edges must pass
                for (int i = 0; i < regionPoints.Count; i++)
                {
                    var point2 = regionPoints[(i + 1) % regionPoints.Count];
                    var point1 = regionPoints[i % regionPoints.Count];

                    var D = (point2.X - point1.X) * (locInFrame.Y - point1.Y) - (locInFrame.X - point1.X) * (point2.Y - point1.Y);

                    if (D < 0)
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: TestIntersection\nMessage: {ex.Message}");
            }
            return false;
        }
        private List<Point> GetPoints(Rectangle region)
        {
            List<Point> points = new List<Point>();
            try
            {
                points.Add(region.Location);
                points.Add(new Point(region.Location.X + region.Width, region.Location.Y));
                points.Add(new Point(region.Location.X + region.Width, region.Location.Y + region.Height));
                points.Add(new Point(region.Location.X, region.Location.Y + region.Height));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: GetPoints\nMessage: {ex.Message}");
            }
            return points;
        }
        private Point LocationInFrame(Point location)
        {
            try
            {
                // TODO: Clarify naming.
                // Here location = monitor's pixel location in picture box
                // Display = monitor's pixel location in PictureBox.ImageRectangle
                // DisplayWidth/DisplayHeight = Camera's pixel location. 
                var xPos_Display = location.X - Display.X;
                var yPos_Display = location.Y - Display.Y;
                var xPos_Frame = Math.Max(0, Math.Min((int)(xPos_Display * DispImg_WidthBins / (float)Display.Width), DispImg_WidthBins - 1));
                var yPos_Frame = Math.Max(0, Math.Min((int)(yPos_Display * DispImg_HeightBins / (float)Display.Height), DispImg_HeightBins - 1));

                return new Point(xPos_Frame, yPos_Frame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: LocationInFrame\nMessage: {ex.Message}");
            }
            return new Point();
        }
        /// <summary>
        /// Creates a new region by keeping track of how far the <see cref="MouseButtons.Left"/>
        /// has dragged from its starting position. 
        /// </summary>
        /// <param name="startLocation"></param>
        /// <param name="stopLocation"></param>
        /// <returns></returns>
        private Rectangle CreateRegion(Point startLocation, Point stopLocation)
        {
            try
            {
                var dx = Math.Abs(stopLocation.X - startLocation.X);
                var dy = Math.Abs(stopLocation.Y - startLocation.Y);
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    var min = Math.Min(dx, dy);
                    dx = min;
                    dy = min;
                }
                Size size = new Size(dx, dy);
                Point point = new Point(Math.Min(stopLocation.X, startLocation.X), Math.Min(stopLocation.Y, startLocation.Y));
                return new Rectangle(point, size);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateRegion\nMessage: {ex.Message}");
            }
            return new Rectangle();
        }
        /// <summary>
        /// Moves a region by creating a new <see cref="Rectangle"/> that has been translated but not scaled
        /// </summary>
        /// <param name="startLocation"></param>
        /// <param name="stopLocation"></param>
        /// <returns></returns>
        private Rectangle MoveRegion(Point startLocation, Point stopLocation)
        {
            try
            {
                Moving = true;
                var currentRect = Regions[SelectedRegion];
                var newX = Math.Max(0,Math.Min(DispImg_WidthBins - currentRect.Width - 1, currentRect.X + stopLocation.X - startLocation.X));
                var newY = Math.Max(0, Math.Min(DispImg_HeightBins - currentRect.Height - 1, currentRect.Y + stopLocation.Y - startLocation.Y));
                var newPos = new Point(newX, newY);
                DragStartPosition = stopLocation;
                return new Rectangle(newPos, currentRect.Size);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: MoveRegion\nMessage: {ex.Message}");
            }
            return new Rectangle();
        }

        private Rectangle ScaleRegion(Point startLocation, Point stopLocation)
        {
            try
            {
                Moving = true;
                var dx = Math.Abs(stopLocation.X - startLocation.X);
                var dy = Math.Abs(stopLocation.Y - startLocation.Y);
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    var min = Math.Min(dx, dy);
                    dx = min;
                    dy = min;
                }
                Size size = new Size(dx, dy);
                Point point = new Point(Math.Min(stopLocation.X, startLocation.X), Math.Min(stopLocation.Y, startLocation.Y));
                return new Rectangle(point, size);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateRegion\nMessage: {ex.Message}");
            }
            return new Rectangle();
        }

        #endregion
    }
}
