using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HamamatsuCamera.Factories
{
    /// <summary>
    /// Asynchronously writes image data from <see cref="Frame"/> to 
    /// split .tif files. A particular instance of this factory will
    /// alternate between writing to a new .tif files for <see cref="FramesPerTiff"/>
    /// frames and not writing to any .tif file for the next <see cref="FramesPerTiff"/>.
    /// Having two instances of this factory appear to prevent frame drops from occuring 
    /// during the initialization of a new .tif file. 
    /// </summary>
    public class BigTiffFactory : IDisposable
    {
        // ---------------- Global Variables ----------------

        private int Index;          // Index used to specify if this instance is writing the even or odd .tif files
        private string FolderName;  // Base name and relative path of the folder containing the .tif files
        private string FolderAbsPath;
        private string FileName;    // Base Filename of the .tif files
        private ushort FramesPerTiff;   // Number of frames to be written to each .tif file

        private bool bGenerate = true;      // Controls the disposal of this class
        private object Observer;            // Observer of frames passed through this factory

        private Queue<Frame> FrameQueue = new Queue<Frame>();  // Stores un-processed frames in a Queue until they are processed
        private FileStream stream;          // Stream for the .tif writer
        private BinaryWriter writer;        // Binary writer for the .tif files

        private string DebugPath;
        private StreamWriter DebugWriter;
        private const string ListSeparator = ",";       // List separator for writing to different columns of a .csv file
        private object Lock = new object();


        // ---------------- Factory Construction && Public Methods ----------------

        /// <summary>
        /// Constructor that sets the observer and stores the user-defined properties
        /// from the <see cref="TiffWriter"/> class.
        /// </summary>
        /// <param name="frameObserver">Observer created in the <see cref="TiffWriter"/> class.</param>
        /// <param name="index">Index of this instance of <see cref="TiffWriterFactory"/>.</param>
        /// <param name="bigTiffWriter">Instance of the <see cref="TiffWriter"/> class.</param>
        internal BigTiffFactory(object frameObserver, TiffWriter tiffWriter, int index)
        {
            try
            {
                FolderName = tiffWriter.FolderName;
                FolderAbsPath = tiffWriter.FolderAbsPath;
                FileName = tiffWriter.FileName;
                Index = index;
                FramesPerTiff = tiffWriter.FramesPerTiff;
                DebugPath = FolderAbsPath + Index.ToString() + ".csv";
                DebugWriter = new StreamWriter(DebugPath);
                SetObserver(frameObserver);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: BigTiffFactory({index})\nMessage = {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the frame observer in this class and throws and exception if there is no observer
        /// </summary>
        /// <param name="observer">Observer created in the <see cref="TiffWriter"/> class.</param>
        internal void SetObserver(object observer)
        {
            this.Observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        /// <summary>
        /// Opens this <see cref="TiffWriterFactory"/> class by starting a new task factory
        /// with the <see cref="DataGenerator(object)"/> 
        /// </summary>
        public void Open()
        {
            Task.Factory.StartNew(new Action<object>(DataGenerator), Observer);
        }

        /// <summary>
        /// Stores un-written <see cref="Frame"/> that enter the <see cref="TiffWriter"/>
        /// class in a list until they are written.
        /// </summary>
        /// <param name="newFrame">Un-written <see cref="Frame"/> that entered the <see cref="TiffWriter"/> class.</param>
        public void Write(Frame newFrame)
        {
            FrameQueue.Enqueue(newFrame);
        }

        /// <summary>
        /// Disposes this <see cref="TiffWriterFactory"/> class by allowing the <see cref="DataGenerator(object)"/>
        /// to gracefully exit.
        /// </summary>
        public void Dispose()
        {
            bGenerate = false;
        }

        /// <summary>
        /// Asynchronously writes the image data from un-written <see cref="Frame"/>.
        /// Alternates between writting <see cref="FramesPerTiff"/> frames and not writting
        /// for <see cref="FramesPerTiff"/> frames. This way one instance of this 
        /// <see cref="TiffWriterFactory"/> is writing at a time while the other re-initializes
        /// for its next .tif file.
        /// </summary>
        /// <param name="observer">The <see cref="Frame"/> observer created within the <see cref="TiffWriter"/> class.</param>
        private void DataGenerator(object observer)
        {
            // Locally set the observer
            IObserver<Frame> processObserver = (IObserver<Frame>)observer;

            // Initialize the .tif file number and ensure the file's directory
            int FileNumber = 0;
            string FileName = EnsureNewFile(Index);

            // Create the .tif writer, we will use a Binary write for speed
            stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
            writer = new BinaryWriter(stream);

            // Initialize local variables for writing to a .tif file
            UInt32 framesInFile = 0;
            UInt64 nextIFDOffset = (UInt64)HeaderLength;
            UInt64 stripOffset = (UInt64)HeaderLength;

            UInt16 width = 0;
            UInt16 height = 0;
            Byte bytesPerPixel = (Byte)2;
            UInt32 bytesPerImage = 0;

            // Loop until factory is disposed by the TiffWriter class, i.e. stopping the Bonsai workflow
            while (bGenerate)
            {
                while(FrameQueue.Count != 0)
                {

                    // Access the oldest un-processed frame in the new frames list
                    Frame frame = FrameQueue.Dequeue();
                    if (frame.isValid())
                    {
                        // If First Frame
                        if (width == 0 || height == 0)
                        {
                            width = (UInt16)frame.Width;
                            height = (UInt16)frame.Height;
                            bytesPerImage = (UInt32)width * (UInt32)height * (UInt32)bytesPerPixel;
                        }

                        // On first frame in the current file, complete initialization and write header and next IFD offset
                        if (framesInFile == 0)
                        {
                            nextIFDOffset += bytesPerImage;

                            InitializeHeader(nextIFDOffset);
                            InitializeFields(width, height, bytesPerImage);
                            writer.Write(Header);
                        }
                        // Otherwise update the next IFD offset and write it to file
                        else
                        {
                            UpdateNextIFDOffset(nextIFDOffset);
                            writer.Write(NextIFDOffset);
                        }
                        unsafe
                        {
                            using (UnmanagedMemoryStream unStream = new UnmanagedMemoryStream((byte*)frame.bufframe.buf, bytesPerImage))
                            {
                                unStream.CopyTo(stream);
                                unStream.Close();
                            }
                        }

                        // Update Fields
                        UpdateFields((uint)frame.Framestamp, stripOffset);

                        // Write the IFD
                        writer.Write(IFD);

                        // Update values for next frame
                        framesInFile++;
                        stripOffset += bytesPerImage + IFDLength;
                        nextIFDOffset += bytesPerImage + IFDLength;

                        // If last frame in file,
                        if (framesInFile >= FramesPerTiff)
                        {
                            // The, Write end of directory and Close file
                            writer.Write(EndOfDir);
                            writer.Close();
                            stream.Close();

                            // Create next file, stream, and writer
                            FileNumber++;
                            FileName = EnsureNewFile(2 * FileNumber + Index);
                            stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            writer = new BinaryWriter(stream);

                            framesInFile = 0;
                            nextIFDOffset = (UInt64)HeaderLength;
                            stripOffset = (UInt64)HeaderLength;
                        }

                        processObserver.OnNext(frame);
                    }
                }

                //// Wait until there is a new frame to process
                //Thread.Sleep(1);
            }

            DebugWriter.Close();
            // When disposed write the end of directory bytes and close the writer
            if (writer != null)
            {
                string lastFile = stream.Name;
                if (framesInFile > 0)
                    writer.Write(EndOfDir);
                writer.Close();
                stream.Close();

                // Delete file if empty
                FileInfo fileInfo = new FileInfo(lastFile);
                if (fileInfo.Exists && framesInFile == 0)
                    File.Delete(lastFile);
            }
        }

        /// <summary>
        /// Ensures the directory for the .tif file exists
        /// </summary>
        /// <param name="FileNumber">Index of .tif file</param>
        /// <returns>Full path of .tif file to be written to.</returns>
        private string EnsureNewFile(int FileNumber)
        {
            // Get base filename
            DirectoryInfo dir_info = new DirectoryInfo(FolderName);
            string baseFolderName = dir_info.Name;
            string baseFileName = string.IsNullOrEmpty(FileName) ? baseFolderName : FileName;
            // Remove any extension
            if (!string.IsNullOrEmpty(Path.GetExtension(baseFileName)))
                baseFileName = Path.GetFileNameWithoutExtension(baseFileName);


            string filePath = Path.Combine(new string[] { FolderAbsPath, baseFileName });


            // Append Filenumber and .tif extension
            filePath = filePath + FileNumber.ToString() + ".tif";

            // Verify that file does not exist
            if (Directory.Exists(filePath))
                throw new IOException(string.Format("The file '{0}' already exists.", Index, filePath));

            return filePath;
        }

        /// <summary>
        /// Converts the next IFD offset to a 8-byte array, updating the 
        /// <see cref="NextIFDOffset"/>.
        /// </summary>
        /// <param name="nextIFDOffset">Next IFD Offset to be convert to byte array</param>
        private void UpdateNextIFDOffset(UInt64 nextIFDOffset)
        {
            // Update UInt64 IFD Offset
            NextIFDOffset[7] = (byte)((nextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            NextIFDOffset[6] = (byte)((nextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            NextIFDOffset[5] = (byte)((nextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            NextIFDOffset[4] = (byte)((nextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            NextIFDOffset[3] = (byte)((nextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            NextIFDOffset[2] = (byte)((nextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            NextIFDOffset[1] = (byte)((nextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            NextIFDOffset[0] = (byte)((nextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);
        }

        /// <summary>
        /// Updates the page and strip offset fields by converting
        /// a <see cref="UInt32"/> page to 4-byte array
        /// and a <see cref="UInt64"/> strip offset to 8-byte array
        /// </summary>
        /// <param name="page"><see cref="UInt32"/> value of page number to be converted to 4-byte array</param>
        /// <param name="stripOffset"><see cref="UInt64"/> value of strip offset to be converted to 8-byte array</param>
        private void UpdateFields(UInt32 page, UInt64 stripOffset)
        {
            // Update UInt16 Page Number
            IFD[(int)FieldValueIndex.PageNumber + 3] = (byte)((page & 0xff000000) / 0x01000000);
            IFD[(int)FieldValueIndex.PageNumber + 2] = (byte)((page & 0x00ff0000) / 0x00010000);
            IFD[(int)FieldValueIndex.PageNumber + 1] = (byte)((page & 0x0000ff00) / 0x00000100);
            IFD[(int)FieldValueIndex.PageNumber + 0] = (byte)((page & 0x000000ff) / 0x00000001);
            // Update UInt64 Strip Offset
            IFD[(int)FieldValueIndex.StripOffset + 7] = (byte)((stripOffset & 0xff00000000000000L) / 0x0100000000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 6] = (byte)((stripOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 5] = (byte)((stripOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            IFD[(int)FieldValueIndex.StripOffset + 4] = (byte)((stripOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            IFD[(int)FieldValueIndex.StripOffset + 3] = (byte)((stripOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            IFD[(int)FieldValueIndex.StripOffset + 2] = (byte)((stripOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            IFD[(int)FieldValueIndex.StripOffset + 1] = (byte)((stripOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            IFD[(int)FieldValueIndex.StripOffset + 0] = (byte)((stripOffset & 0x00000000000000ffL) / 0x0000000000000001L);
        }

        /// <summary>
        /// Initializes the Width, Height, RowsPerStrip, and StripBytes fields
        /// by converting to byte array.
        /// </summary>
        /// <param name="width"><see cref="UInt16"/> value of width to be converted to 2-byte array</param>
        /// <param name="height"><see cref="UInt16"/> value of height to be converted to 2-byte array</param>
        /// <param name="bytesPerImage"><see cref="UInt32"/> value of bytes per image to be converted to 4-byte array</param>
        private void InitializeFields(UInt16 width, UInt16 height, UInt32 bytesPerImage)
        {
            // Initialize UInt16 Width
            IFD[(int)FieldValueIndex.Width + 1] = (byte)((width & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.Width + 0] = (byte)((width & 0x00ff) / 0x0001);
            // Initialize UInt16 Height
            IFD[(int)FieldValueIndex.Height + 1] = (byte)((height & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.Height + 0] = (byte)((height & 0x00ff) / 0x0001);
            // Initialize UInt16 Rows Per Strip
            IFD[(int)FieldValueIndex.RowsPerStrip + 1] = (byte)((height & 0xff00) / 0x0100);
            IFD[(int)FieldValueIndex.RowsPerStrip + 0] = (byte)((height & 0x00ff) / 0x0001);
            // Initialize UInt32 Strip Bytes
            IFD[(int)FieldValueIndex.StripBytes + 3] = (byte)((bytesPerImage & 0xff000000) / 0x01000000);
            IFD[(int)FieldValueIndex.StripBytes + 2] = (byte)((bytesPerImage & 0x00ff0000) / 0x00010000);
            IFD[(int)FieldValueIndex.StripBytes + 1] = (byte)((bytesPerImage & 0x0000ff00) / 0x00000100);
            IFD[(int)FieldValueIndex.StripBytes + 0] = (byte)((bytesPerImage & 0x000000ff) / 0x00000001);
        }

        /// <summary>
        /// Finish initializing the header with the next IFD offset value
        /// converted to 8-bytes of the header byte array
        /// </summary>
        /// <param name="nextIFDOffset"><see cref="UInt64"/> value of strip offset to be converted to 8-byte array</param>
        private void InitializeHeader(UInt64 nextIFDOffset)
        {
            Header[15] = (byte)((nextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            Header[14] = (byte)((nextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            Header[13] = (byte)((nextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            Header[12] = (byte)((nextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            Header[11] = (byte)((nextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            Header[10] = (byte)((nextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            Header[9] = (byte)((nextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            Header[8] = (byte)((nextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);
        }

        private const int HeaderLength = 16;    // Length of byte array containing the .tif file header

        /// <summary>
        /// Semi-Initialized byte array containing the header information for the .tif file
        /// </summary>
        private byte[] Header = new byte[]
        {
            // Declare Byte Order = Little-Endian and that the file is a BigTiff (43)
            0x49, 0x49, 0x2b, 0x00,
            // Byte Size of Offsets, Always 8 for BigTiff, followed by 2 bytes of 0
            0x08, 0x00, 0x00, 0x00,
            // Initialize offset to first IFD, depends on size and dit depth
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private const UInt32 IFDLength = 316;   // Length of byte array containing the IFD for the current frame

        /// <summary>
        /// Semi-Initialized byte array containing the IFD information for each frame
        /// </summary>
        private byte[] IFD = new byte[]
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

        private byte[] NextIFDOffset = new byte[8];     // Contains the next IFD offset as an 8-byte array
        private byte[] EndOfDir = new byte[8];          // Contains the End of Directory as an 8-byte array

        private const byte TagPrefix = 0x01;            // Hex prefix for tags in .tif files

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

        private const byte FieldPrefix = 0x00;          // Hex prefix for fields in .tif files

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
    }
}
