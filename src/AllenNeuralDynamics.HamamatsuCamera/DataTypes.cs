
using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using ZedGraph;

using OpenCV.Net;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    public class Frame
    {
        public IplImage Image;
        public DCAMBUF_FRAME bufframe;
        public Frame()
        {
            bufframe = new DCAMBUF_FRAME(0);
        }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Framestamp { get { return bufframe.framestamp; } }
        public double CameraTimestamp { get; private set; }
        public double ComputerTimestamp { get; private set; }
        public DCAM_PIXELTYPE PixelType { get; private set; }
        public List<Rectangle> Regions { get; set; }
        public Dictionary<int, int> LookupTable { get; set; }
        public List<double> RegionData { get; set; }
        public int DeinterleaveCount { get; set; }
        public bool isValid()
        {
            if (Width <= 0 || Height <= 0 || PixelType == DCAM_PIXELTYPE.NONE)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public void clear()
        {
            bufframe.width = 0;
            bufframe.height = 0;
            bufframe.type = DCAM_PIXELTYPE.NONE;
        }
        public void UpdateFrameInfo(int index, int left, int top)
        {
            bufframe.iFrame = index;
            Width = bufframe.width;
            Height = bufframe.height;
            Left = left;
            Top = top;
            //Framestamp = bufframe.framestamp;
            CameraTimestamp = bufframe.timestamp.sec + bufframe.timestamp.microsec / (1000.0 * 1000.0);
            ComputerTimestamp = HighResolutionScheduler.Now.DateTime.TimeOfDay.TotalSeconds;
            PixelType = bufframe.type;
        }
    }

    public class VisualizerData
    {
        public Bitmap Image { get; set; }
        public int DeinterleaveCount { get; set; }
        public int CurrentSignal { get; set; }
        public double xMin { get; set; }
        public double xMax { get; set; }
        public List<double> yMins { get; set; }
        public List<double> yMaxes { get; set; }
        public double FPS { get; set; }
        public List<PointPair> RegionData { get; set; }
    }

    public class TicProps
    {
        public double BaseTic { get; set; }
        public double Step { get; set; }
        public int SigFigs { get; set; }
    }

    /// <summary>
    /// Data extracted from the <see cref="Frame"/> during image processing.
    /// Generated in the <see cref="ImageProcessingFactory"/> and output from the
    /// <see cref="ImageProcessing"/> node for use in the <see cref="ImageProcessingVisualizer"/>.
    /// </summary>
    public class FrameData
    {
        /// <summary>
        /// Number of Signals each ROI can deinterleave into in the <see cref="ProcessingView"/>.
        /// </summary>
        public byte DeinterleaveCount { get; set; }

        ///// <summary>
        ///// Metadata passed from the unprocessed frame.
        ///// </summary>
        //public Frame.MetaData MetaData { get; set; }

        public double CameraTimestamp { get; set; }

        /// <summary>
        /// Stores the activity data of each Region
        /// </summary>
        public double[] RegionData { get; set; }
        public double[] TransformedRegionData { get; set; }
        public long Framestamp { get; set; }
    }

    public class ImageProcessingProperties
    {
        public bool IncludeProcessing { get; set; } = true;

        /// <summary>
        /// Specifies how many signals each Region activity can deinterleave into
        /// within the <see cref="ProcessingView"/>.
        /// </summary>
        [Description("Specifies the amount of signals to deinteleave into.")]
        public byte DeinterleaveCount { get; set; } = 1;

        /// <summary>
        /// Specifies the file path and name to store the data in a .csv file.
        /// </summary>
        [Description("The name of the file on which to write the elements.")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        public string FileName { get; set; }

        /// <summary>
        /// Specifies the suffix used to generate file names.
        /// </summary>
        [Description("The suffix used to generate file names.")]
        public PathSuffix Suffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output file if it already exists.")]
        public bool Overwrite { get; set; }


        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<int, int> LookupTable { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<int, int> PointsOfInterest { get; set; }

        [Browsable(false)]
        public List<Rectangle> Regions;

        internal void InitializeProcessing()
        {
            if(LookupTable == null)
            {
                LookupTable = new Dictionary<int, int>();
                for (int i = 0; i <= ushort.MaxValue; i++)
                {
                    LookupTable[i] = i;
                }
            }
            if (Regions == null || Regions.Count == 0)
                Regions = new List<Rectangle>() { new Rectangle(0, 0, 2048, 2048) };
        }
    }

    public class TiffProperties
    {
        #region Constants

        private const ulong HeaderLength = 16;    // Length of byte array containing the .tif file header
        private const uint IFDLength = 316;   // Length of byte array containing the IFD for the current frame
        private const byte TagPrefix = 0x01;            // Hex prefix for tags in .tif files
        private const byte FieldPrefix = 0x00;          // Hex prefix for fields in .tif files

        #endregion

        #region Variables

        public uint FileNumber = 0;
        public uint FramesInFile = 0;
        public ulong NextIFDOffset = HeaderLength;
        public ulong StripOffset = HeaderLength;
        public ushort Width = 0;
        public ushort Height = 0;
        public byte BytesPerPixel = 2;
        public uint BytesPerImage = 0;

        #endregion

        #region Properties

        public bool IncludeTIFF { get; set; } = true;

        /// <summary>
        /// Gets or sets the suffix for the containing folder's name.
        /// </summary>
        [Description("The suffix for the containing folder's name.")]
        public PathSuffix Suffix { get; set; } = PathSuffix.Timestamp;

        /// <summary>
        /// Gets or Sets the number of frames per tiff
        /// </summary>
        [Description("Specifies the number of frames per .tif file.")]
        public ushort FramesPerTiff { get; set; } = 1000;

        /// <summary>
        /// Gets or Sets the optional base filename of the output .tifs. If not specified, the base
        /// filename will match the containing folder's base name.
        /// </summary>
        [Description("Optional: Specifies the base filename of the output .tif files. If not specified, the base filename will match the containing folder's base name.")]
        public string BaseFileName { get; set; }

        /// <summary>
        /// Gets or Sets the base name and relative path of the folder containing the output .tif files
        /// </summary>
        [Description("The name of the output file.")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string FolderName { get; set; }

        [Browsable(false)]
        public string FolderAbsPath { get; set; }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Called once by the even indexed TiffWriterFactory in order to
        /// initialize the directory containing all of the .tif files.
        /// </summary>
        public void InitializeTiffDirectory()
        {
            FolderAbsPath = FolderName;

            // Remove file extension if user incorrectly added one
            if (!string.IsNullOrEmpty(Path.GetExtension(FolderAbsPath)))
                FolderAbsPath = Path.GetFileNameWithoutExtension(FolderAbsPath);

            // Append Suffix
            switch (Suffix)
            {
                case PathSuffix.FileCount:
                    var subFolderCount = 0;
                    var directory = Path.GetDirectoryName(FolderAbsPath);
                    if (string.IsNullOrEmpty(directory)) directory = ".";

                    if (Directory.Exists(directory))
                    {
                        var subFolderName = Path.GetFileNameWithoutExtension(FolderAbsPath);
                        subFolderCount = Directory.GetDirectories(directory, subFolderName + "*").Length;
                    }
                    FolderAbsPath += subFolderCount;
                    break;
                case PathSuffix.Timestamp:
                    FolderAbsPath += HighResolutionScheduler.Now.ToString("o").Replace(':', '_');
                    break;

            }

            // Verify folder does not already exist
            if (Directory.Exists(FolderAbsPath))
                throw new IOException($"The Folder {FolderAbsPath} already exists.");

            FolderAbsPath = Directory.CreateDirectory(FolderAbsPath).FullName;
        }

        public void InitializeTiffFile()
        {
            // Initialize NextIFDOffset
            NextIFDOffset = HeaderLength + BytesPerImage;

            // Initialize Header
            Header[15] = (byte)((NextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            Header[14] = (byte)((NextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            Header[13] = (byte)((NextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            Header[12] = (byte)((NextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            Header[11] = (byte)((NextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            Header[10] = (byte)((NextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            Header[9] = (byte)((NextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            Header[8] = (byte)((NextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);

            // Initialize UInt16 Width
            IFD[(int)FieldValueIndex.Width + 1] = (byte)((Width & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.Width + 0] = (byte)((Width & 0x00ff) / 0x0001);
            // Initialize UInt16 Height
            IFD[(int)FieldValueIndex.Height + 1] = (byte)((Height & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.Height + 0] = (byte)((Height & 0x00ff) / 0x0001);
            // Initialize UInt16 Rows Per Strip
            IFD[(int)FieldValueIndex.RowsPerStrip + 1] = (byte)((Height & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.RowsPerStrip + 0] = (byte)((Height & 0x00ff) / 0x0001);
            // Initialize UInt32 Strip Bytes
            IFD[(int)FieldValueIndex.StripBytes + 3] = (byte)((BytesPerImage & 0xff000000) / 0x01000000);
            IFD[(int)FieldValueIndex.StripBytes + 2] = (byte)((BytesPerImage & 0x00ff0000) / 0x00010000);
            IFD[(int)FieldValueIndex.StripBytes + 1] = (byte)((BytesPerImage & 0x0000ff00) / 0x00000100);
            IFD[(int)FieldValueIndex.StripBytes + 0] = (byte)((BytesPerImage & 0x000000ff) / 0x00000001);
        }

        /// <summary>
        /// Converts the next IFD offset to a 8-byte array, updating the
        /// <see cref="NextIFDOffset"/>.
        /// </summary>
        /// <param name="nextIFDOffset">Next IFD Offset to be convert to byte array</param>
        public void UpdateNextIFDOffset()
        {
            // Update UInt64 IFD Offset
            NextIFDOffsetBytes[7] = (byte)((NextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            NextIFDOffsetBytes[6] = (byte)((NextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            NextIFDOffsetBytes[5] = (byte)((NextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            NextIFDOffsetBytes[4] = (byte)((NextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            NextIFDOffsetBytes[3] = (byte)((NextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            NextIFDOffsetBytes[2] = (byte)((NextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            NextIFDOffsetBytes[1] = (byte)((NextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            NextIFDOffsetBytes[0] = (byte)((NextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);
        }

        /// <summary>
        /// Updates the page and strip offset fields by converting
        /// a <see cref="UInt32"/> page to 4-byte array
        /// and a <see cref="UInt64"/> strip offset to 8-byte array
        /// </summary>
        /// <param name="page"><see cref="UInt32"/> value of page number to be converted to 4-byte array</param>
        /// <param name="stripOffset"><see cref="UInt64"/> value of strip offset to be converted to 8-byte array</param>
        public void UpdateFields(UInt32 page)
        {
            // Update UInt16 Page Number
            IFD[(int)FieldValueIndex.PageNumber + 3] = (byte)((page & 0xff000000) / 0x01000000);
            IFD[(int)FieldValueIndex.PageNumber + 2] = (byte)((page & 0x00ff0000) / 0x00010000);
            IFD[(int)FieldValueIndex.PageNumber + 1] = (byte)((page & 0x0000ff00) / 0x00000100);
            IFD[(int)FieldValueIndex.PageNumber + 0] = (byte)((page & 0x000000ff) / 0x00000001);
            // Update UInt64 Strip Offset
            IFD[(int)FieldValueIndex.StripOffset + 7] = (byte)((StripOffset & 0xff00000000000000L) / 0x0100000000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 6] = (byte)((StripOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 5] = (byte)((StripOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 4] = (byte)((StripOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            IFD[(int)FieldValueIndex.StripOffset + 3] = (byte)((StripOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            IFD[(int)FieldValueIndex.StripOffset + 2] = (byte)((StripOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            IFD[(int)FieldValueIndex.StripOffset + 1] = (byte)((StripOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            IFD[(int)FieldValueIndex.StripOffset + 0] = (byte)((StripOffset & 0x00000000000000ffL) / 0x0000000000000001L);
        }

        internal void UpdateValues()
        {
            FramesInFile++;
            StripOffset += BytesPerImage + IFDLength;
            NextIFDOffset += BytesPerImage + IFDLength;
        }

        internal void ResetValues()
        {
            FramesInFile = 0;
            StripOffset = BytesPerImage + IFDLength;
            NextIFDOffset = BytesPerImage + IFDLength;
        }

        #endregion

        #region Pre-Allocated Arrays


        public byte[] NextIFDOffsetBytes = new byte[8];     // Contains the next IFD offset as an 8-byte array
        public byte[] EndOfDir = new byte[8];          // Contains the End of Directory as an 8-byte array

        /// <summary>
        /// Semi-Initialized byte array containing the header information for the .tif file
        /// </summary>
        public byte[] Header = new byte[]
        {
            // Declare Byte Order = Little-Endian and that the file is a BigTiff (43)
            0x49, 0x49, 0x2b, 0x00,
            // Byte Size of Offsets, Always 8 for BigTiff, followed by 2 bytes of 0
            0x08, 0x00, 0x00, 0x00,
            // Initialize offset to first IFD, depends on size and dit depth
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };


        /// <summary>
        /// Semi-Initialized byte array containing the IFD information for each frame
        /// </summary>
        public byte[] IFD = new byte[]
        {
            // IDF (Number of Entries)
            0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 000-007: Number of Entries = 15
            // Width Entry
            (byte)Tags.Width, TagPrefix, (byte)Fields.Short, FieldPrefix,               // Bytes 008-011: Tag and Field Type = Width, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 012-019: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 020-027: Width Value = 0 ****(Uninitialized)****
            // Height Entry
            (byte)Tags.Height, TagPrefix, (byte)Fields.Short, FieldPrefix,              // Bytes 028-031: Tag and Field Type = Height, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 032-039: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 040-047: Height Value = 0 ****(Uninitialized)****
            // Bits Per Sample Entry
            (byte)Tags.BPS, TagPrefix, (byte)Fields.Short, FieldPrefix,                 // Bytes 048-051: Tag and Field Type = Bits Per Sample, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 052-059: Number of values = 1 (Grayscale)
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 060-067: Bits Per Sample Value = 16 (Mono16)
            // Compression Entry
            (byte)Tags.Compression, TagPrefix, (byte)Fields.Short, FieldPrefix,         // Bytes 068-071: Tag and Field Type = Compression, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 072-079: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 080-087: Compression Value = 1 (No Compression)
            // Photometric Interpolation Entry
            (byte)Tags.PhotoInt, TagPrefix, (byte)Fields.Short, FieldPrefix,            // Bytes 088-091: Tag and Field Type = Photometric Interpolation, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 092-099: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 100-107: Photometric Interpolation Value = 1 (Grayscale)
            // Strip Offset Entry
            (byte)Tags.StripOffset, TagPrefix, (byte)Fields.Long8, FieldPrefix,         // Bytes 108-111: Tag and Field Type = Strip Offset, Long8
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 112-119: Number of values = 1
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 120-127: Strip Offset Value = 16 ****(Variable)****
            // Orientation Entry
            (byte)Tags.Orientation, TagPrefix, (byte)Fields.Short, FieldPrefix,         // Bytes 128-131: Tag and Field Type = Orientation, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 132-139: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 140-147: Orientation Value = 1
            // Samples Per Pixel Entry
            (byte)Tags.SamplesPerPixel, TagPrefix, (byte)Fields.Short, FieldPrefix,     // Bytes 148-151: Tag and Field Type = Samples Per Pixel, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 152-159: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 160-167: Samples Per Pixel Value = 1 (Grayscale)
            // Rows Per Strip Entry
            (byte)Tags.RowsPerStrip, TagPrefix, (byte)Fields.Short, FieldPrefix,        // Bytes 168-171: Tag and Field Type = Rows Per Strip, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 172-179: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 180-187: Rows Per Strip Value = 0 ****(Uninitialized)****
            // Strip Bytes Entry
            (byte)Tags.StripBytesCount, TagPrefix, (byte)Fields.Long, FieldPrefix,      // Bytes 188-191: Tag and Field Type = Strip Bytes, long
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 192-199: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 200-207: Strip Bytes Value = 0 ****(Uninitialized)****
            // Minimum Sample Value Entry
            (byte)Tags.MinSampleVal, TagPrefix, (byte)Fields.Short, FieldPrefix,        // Bytes 208-211: Tag and Field Type = Minimum Sample Value, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 212-219: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 220-227: Minimum Sample Value = 0
            // Maxmimum Sample Value Entry
            (byte)Tags.MaxSampleVal, TagPrefix, (byte)Fields.Short, FieldPrefix,        // Bytes 228-231: Tag and Field Type = Maxmimum Sample Value, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 232-239: Number of values = 1
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 240-247: Maxmimum Sample Value = 65535 (Mono16)
            // Planar Configuration Entry
            (byte)Tags.PlanarConfig, TagPrefix, (byte)Fields.Short, FieldPrefix,        // Bytes 248-251: Tag and Field Type = Planar Configuration, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 252-259: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 260-267: Planar Configuration Value = 1
            // Page Number Entry
            (byte)Tags.PageNumber, TagPrefix, (byte)Fields.Short, FieldPrefix,          // Bytes 268-271: Tag and Field Type = Page Number, ushort
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 272-279: Number of values = 2 (Min, Max)
            0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00,                             // Bytes 280-287: Page Number Values = (0, short.Max) ****(Variable)****
            // Sample Format Entry
            (byte)Tags.SampleFormat, TagPrefix, (byte)Fields.Short, FieldPrefix,        // Bytes 288-291: Tag and Field Type = Sample Format, long
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 292-299: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 300-307: Sample Format Value = 1 (Mono16)
        };

        #endregion

        #region Enums

        /// <summary>
        /// Hex for tags used in the IFDs
        /// </summary>
        public enum Tags : byte
        {
            Width = 0x00,
            Height = 0x01,
            BPS = 0x02,
            Compression = 0x03,
            PhotoInt = 0x06,
            StripOffset = 0x11,
            Orientation = 0x12,
            SamplesPerPixel = 0x15,
            RowsPerStrip = 0x16,
            StripBytesCount = 0x17,
            MinSampleVal = 0x18,
            MaxSampleVal = 0x19,
            PlanarConfig = 0x1c,
            PageNumber = 0x29,
            SampleFormat = 0x53
        }


        /// <summary>
        /// Hex for fields used in the IFDs
        /// </summary>
        public enum Fields : byte
        {
            Short = 0x03,
            Long = 0x04,
            Long8 = 0x10
        }

        /// <summary>
        /// Indices for the field values that may be changed during a given frame
        /// </summary>
        public enum FieldValueIndex : int
        {
            Width = 20,
            Height = 40,
            StripOffset = 120,
            RowsPerStrip = 180,
            StripBytes = 200,
            PageNumber = 280
        }

        #endregion
    }

    public struct LUT
    {
        public int camerabpp;          // camera bit per pixel.  This sample code only support MONO.
        public int cameramax;

        public int inmax;
        public int inmin;
    };

    public enum CropMode
    {
        Auto,
        Manual
    }

    public class ManagedSetting
    {
        public int ID;
        public double Value;
    }


}
