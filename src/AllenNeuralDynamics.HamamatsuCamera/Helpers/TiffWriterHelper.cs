using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    internal unsafe class TiffWriterHelper : IDisposable
    {
        private readonly byte[] _nextIFDOffsetBytes = new byte[8];     // Contains the next IFD offset as an 8-byte array
        private readonly byte[] _endOfDir = new byte[8];          // Contains the End of Directory as an 8-byte array
        private readonly string _baseFileName;
        private readonly string _folderName;
        private readonly PathSuffix _suffix;
        private readonly ushort _framesPerTiff;
        private readonly ushort _width;
        private readonly ushort _height;
        private readonly ushort _bitsPerSample;
        private readonly uint _bytesPerFrame;

        private string _directoryPath;
        private FileStream _fileStream;
        private SafeFileHandle _handle;

        private const int _headerLength = 16;    // Length of byte array containing the .tif file header
        private const int _ifdOffsetLength = 8;     // Length of byte array containing the IFD Offset
        private const int _endOfDirLength = 8;      // Length of byte array containing the End of Directory bytes
        private const uint _ifdLength = 308;   // Length of byte array containing the IFD for the current frame
        private const byte _tagPrefix = 0x01;            // Hex prefix for tags in .tif files
        private const byte _fieldPrefix = 0x00;          // Hex prefix for fields in .tif files
        private uint _fileNumber = 0;
        private uint _framesInFile = 0;
        private ulong _nextIFDOffset = _headerLength;
        private ulong _stripOffset = _headerLength;
        private bool disposedValue;

        /// <summary>
        /// Store settings and initialize the writer.
        /// </summary>
        /// <param name="properties">Properties stored in the <see cref="TiffWriter"/> node.</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="bytesPerPixel">Bytes per pixel</param>
        /// <param name="bytesPerFrame">Bytes per frame</param>
        internal TiffWriterHelper(TiffWriterProperties properties, int width, int height, int bytesPerPixel, int bytesPerFrame)
        {
            _baseFileName = properties.BaseFileName;
            _folderName = properties.FolderName;
            _suffix = properties.Suffix;
            _framesPerTiff = properties.FramesPerTiff;
            _width = (ushort)width;
            _height = (ushort)height;
            _bitsPerSample = (ushort)(bytesPerPixel * 8);
            _bytesPerFrame = (uint)bytesPerFrame;

            TryInitializeWriter();
        }

        /// <summary>
        /// Tries to initialize the writer
        /// </summary>
        private void TryInitializeWriter()
        {
            try
            {
                InitializeWriter();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Initializes the parent directory, adds the first empty .tif file, and initializes
        /// pre-allocated arrays.
        /// </summary>
        private void InitializeWriter()
        {
            InitializeDirectory();
            EnsureNewFile();
            InitializeArrays();
        }

        /// <summary>
        /// Remove file extension, appends suffix, and verify parent directory does not already exist.
        /// </summary>
        private void InitializeDirectory()
        {
            _directoryPath = _folderName;

            // Remove file extension if user incorrectly added one
            if (!string.IsNullOrEmpty(Path.GetExtension(_directoryPath)))
                _directoryPath = Path.GetFileNameWithoutExtension(_directoryPath);

            // Append Suffix
            switch (_suffix)
            {
                case PathSuffix.FileCount:
                    var subFolderCount = 0;
                    var directory = Path.GetDirectoryName(_directoryPath);
                    if (string.IsNullOrEmpty(directory)) directory = ".";

                    if (Directory.Exists(directory))
                    {
                        var subFolderName = Path.GetFileNameWithoutExtension(_directoryPath);
                        subFolderCount = Directory.GetDirectories(directory, subFolderName + "*").Length;
                    }
                    _directoryPath += subFolderCount;
                    break;
                case PathSuffix.Timestamp:
                    _directoryPath += HighResolutionScheduler.Now.ToString("o").Replace(':', '_');
                    break;
            }

            // Verify folder does not already exist
            if (Directory.Exists(_directoryPath))
                throw new IOException($"The Folder {_directoryPath} already exists.");

            _directoryPath = Directory.CreateDirectory(_directoryPath).FullName;
        }

        /// <summary>
        /// Initializes the header and IFD arrays. Additionally initializes the framesInFile and offsets
        /// </summary>
        private void InitializeArrays()
        {
            _framesInFile = 0;
            _stripOffset = _headerLength;
            _nextIFDOffset = _headerLength + _bytesPerFrame;

            // Initialize Header
            _header[15] = (byte)((_nextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            _header[14] = (byte)((_nextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            _header[13] = (byte)((_nextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            _header[12] = (byte)((_nextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            _header[11] = (byte)((_nextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            _header[10] = (byte)((_nextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            _header[9] = (byte)((_nextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            _header[8] = (byte)((_nextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);

            // Initialize UInt16 Width
            _ifd[(int)FieldValueIndex.Width + 1] = (byte)((_width & 0xff00) / 0x0100);
            _ifd[(int)FieldValueIndex.Width + 0] = (byte)((_width & 0x00ff) / 0x0001);
            // Initialize UInt16 Height
            _ifd[(int)FieldValueIndex.Height + 1] = (byte)((_height & 0xff00) / 0x0100);
            _ifd[(int)FieldValueIndex.Height + 0] = (byte)((_height & 0x00ff) / 0x0001);
            // Initialize UInt16 Bits Per Sample
            _ifd[(int)FieldValueIndex.BitsPerSample + 1] = (byte)((_bitsPerSample & 0xff00) / 0x0100);
            _ifd[(int)FieldValueIndex.BitsPerSample + 0] = (byte)((_bitsPerSample & 0x00ff) / 0x0001);
            // Initialize UInt16 Rows Per Strip
            _ifd[(int)FieldValueIndex.RowsPerStrip + 1] = (byte)((_height & 0xff00) / 0x0100);
            _ifd[(int)FieldValueIndex.RowsPerStrip + 0] = (byte)((_height & 0x00ff) / 0x0001);
            // Initialize UInt32 Strip Bytes
            _ifd[(int)FieldValueIndex.StripBytes + 3] = (byte)((_bytesPerFrame & 0xff000000) / 0x01000000);
            _ifd[(int)FieldValueIndex.StripBytes + 2] = (byte)((_bytesPerFrame & 0x00ff0000) / 0x00010000);
            _ifd[(int)FieldValueIndex.StripBytes + 1] = (byte)((_bytesPerFrame & 0x0000ff00) / 0x00000100);
            _ifd[(int)FieldValueIndex.StripBytes + 0] = (byte)((_bytesPerFrame & 0x000000ff) / 0x00000001);
            // Initialize UInt16 Maximum Sample Value
            if (_bitsPerSample == 8)
            {
                _ifd[(int)FieldValueIndex.MaximumSampleValue + 1] = (byte)((byte.MaxValue & 0xff00) / 0x0100);
                _ifd[(int)FieldValueIndex.MaximumSampleValue + 0] = (byte)((byte.MaxValue & 0x00ff) / 0x0001);
            }
            else
            {
                _ifd[(int)FieldValueIndex.MaximumSampleValue + 1] = (byte)((ushort.MaxValue & 0xff00) / 0x0100);
                _ifd[(int)FieldValueIndex.MaximumSampleValue + 0] = (byte)((ushort.MaxValue & 0x00ff) / 0x0001);
            }


        }

        /// <summary>
        /// Add a new .tif file to the directory, initializing the underlying <see cref="FileStream"/>
        /// and getting the <see cref="SafeFileHandle"/>
        /// </summary>
        private void EnsureNewFile()
        {
            // Get base filename
            DirectoryInfo dir_info = new DirectoryInfo(_folderName);
            string baseFolderName = dir_info.Name;
            string baseFileName = string.IsNullOrEmpty(_baseFileName) ? baseFolderName : _baseFileName;
            // Remove any extension
            if (!string.IsNullOrEmpty(Path.GetExtension(baseFileName)))
                baseFileName = Path.GetFileNameWithoutExtension(baseFileName);


            string filePath = Path.Combine(_directoryPath, baseFileName);


            // Append Filenumber and .tif extension
            filePath = filePath + _fileNumber.ToString() + ".tif";

            // Verify that file does not exist
            if (Directory.Exists(filePath))
                throw new IOException(string.Format("The file '{0}' already exists.", filePath));

            _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: false);
            _handle = _fileStream.SafeFileHandle;
        }

        /// <summary>
        /// Write a new <see cref="FramePacket"/> to .tif.
        /// Writes the header or next IFD offset.
        /// Writes the image data
        /// Writes the IFD
        /// Updates the _framesInFile and offsets
        /// Optionally, finish the current .tif file and move onto the next.
        /// </summary>
        /// <param name="packet">New frame packet</param>
        internal unsafe void Write(FramePacket packet)
        {
            if (_framesInFile == 0)
                WriteHeader();
            else
                WriteNextIFDOffset();

            WriteImageData(packet);
            WriteIFD((uint)packet.FrameIndex);

            _framesInFile++;
            _stripOffset += _bytesPerFrame + _ifdLength + _ifdOffsetLength;
            _nextIFDOffset += _bytesPerFrame + _ifdLength + _ifdOffsetLength;

            if (_framesInFile >= _framesPerTiff)
            {
                WriteEndOfDir();
                InitializeArrays();
                _fileStream.Close();
                _fileNumber++;
                EnsureNewFile();
            }
        }

        /// <summary>
        /// Write the header to the .tif file.
        /// </summary>
        private void WriteHeader()
        {
            fixed(byte* p = _header)
            {
                if(!WriteFile(_handle, p, _headerLength, out var written, IntPtr.Zero))
                    throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());

                if (written != _headerLength)
                    throw new IOException($"Partial header write: {written} of {_headerLength}");
            }
        }

        /// <summary>
        /// Update the pre-allocated byte array containing the next IFD offset and write it to the .tif file.
        /// </summary>
        private void WriteNextIFDOffset()
        {
            _nextIFDOffsetBytes[7] = (byte)((_nextIFDOffset & 0xff00000000000000L) / 0x0100000000000000L);
            _nextIFDOffsetBytes[6] = (byte)((_nextIFDOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            _nextIFDOffsetBytes[5] = (byte)((_nextIFDOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            _nextIFDOffsetBytes[4] = (byte)((_nextIFDOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            _nextIFDOffsetBytes[3] = (byte)((_nextIFDOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            _nextIFDOffsetBytes[2] = (byte)((_nextIFDOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            _nextIFDOffsetBytes[1] = (byte)((_nextIFDOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            _nextIFDOffsetBytes[0] = (byte)((_nextIFDOffset & 0x00000000000000ffL) / 0x0000000000000001L);

            fixed (byte* p = _nextIFDOffsetBytes)
            {
                if (!WriteFile(_handle, p, _ifdOffsetLength, out var written, IntPtr.Zero))
                    throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());

                if (written != _ifdOffsetLength)
                    throw new IOException($"Partial IFD Offset write: {written} of {_ifdOffsetLength}");
            }
        }

        /// <summary>
        /// Update the pre-allocated byte array containing the IFD and write it to the .tif file.
        /// </summary>
        private void WriteIFD(UInt32 page)
        {
            // Update UInt16 Page Number
            _ifd[(int)FieldValueIndex.PageNumber + 3] = (byte)((page & 0xff000000) / 0x01000000);
            _ifd[(int)FieldValueIndex.PageNumber + 2] = (byte)((page & 0x00ff0000) / 0x00010000);
            _ifd[(int)FieldValueIndex.PageNumber + 1] = (byte)((page & 0x0000ff00) / 0x00000100);
            _ifd[(int)FieldValueIndex.PageNumber + 0] = (byte)((page & 0x000000ff) / 0x00000001);
            // Update UInt64 Strip Offset
            _ifd[(int)FieldValueIndex.StripOffset + 7] = (byte)((_stripOffset & 0xff00000000000000L) / 0x0100000000000000L);
            _ifd[(int)FieldValueIndex.StripOffset + 6] = (byte)((_stripOffset & 0x00ff000000000000L) / 0x0001000000000000L);
            _ifd[(int)FieldValueIndex.StripOffset + 5] = (byte)((_stripOffset & 0x0000ff0000000000L) / 0x0000010000000000L);
            _ifd[(int)FieldValueIndex.StripOffset + 4] = (byte)((_stripOffset & 0x000000ff00000000L) / 0x0000000100000000L);
            _ifd[(int)FieldValueIndex.StripOffset + 3] = (byte)((_stripOffset & 0x00000000ff000000L) / 0x0000000001000000L);
            _ifd[(int)FieldValueIndex.StripOffset + 2] = (byte)((_stripOffset & 0x0000000000ff0000L) / 0x0000000000010000L);
            _ifd[(int)FieldValueIndex.StripOffset + 1] = (byte)((_stripOffset & 0x000000000000ff00L) / 0x0000000000000100L);
            _ifd[(int)FieldValueIndex.StripOffset + 0] = (byte)((_stripOffset & 0x00000000000000ffL) / 0x0000000000000001L);

            fixed (byte* p = _ifd)
            {
                if (!WriteFile(_handle, p, (int)_ifdLength, out var written, IntPtr.Zero))
                    throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());

                if (written != _ifdLength)
                    throw new IOException($"Partial IFD Offset write: {written} of {_ifdOffsetLength}");
            }
        }

        /// <summary>
        /// Write the image data from the <see cref="FramePacket"/> to the .tif file.
        /// </summary>
        /// <param name="packet">New <see cref="FramePacket"/></param>
        private void WriteImageData(FramePacket packet)
        {
            if (!WriteFile(_handle, packet.DataPtr.ToPointer(), (int)_bytesPerFrame, out var written, IntPtr.Zero))
                throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());

            if (written != _bytesPerFrame)
                throw new IOException($"Partial frame write: {written} of {_bytesPerFrame}");
        }

        /// <summary>
        /// Write the pre-allocated byte array representing the end of the .tif file.
        /// </summary>
        private void WriteEndOfDir()
        {
            fixed (byte* p = _endOfDir)
            {
                if (!WriteFile(_handle, p, _endOfDirLength, out var written, IntPtr.Zero))
                    throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());

                if (written != _endOfDirLength)
                    throw new IOException($"Partial header write: {written} of {_headerLength}");
            }
        }

        // -------------------- Native interop --------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(
            SafeFileHandle hFile,
            void* lpBuffer,
            int nNumberOfBytesToWrite,
            out int lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        public static void WriteFrame(SafeFileHandle file, IntPtr framePtr, int bytesPerFrame)
        {
            if (!WriteFile(file, framePtr.ToPointer(), bytesPerFrame, out int written, IntPtr.Zero))
            {
                throw new IOException("WriteFile failed: " + Marshal.GetLastWin32Error());
            }
            if (written != bytesPerFrame)
            {
                throw new IOException($"WriteFile wrote only {written} of {bytesPerFrame} bytes.");
            }
        }

        /// <summary>
        /// If a .tif file is unfinished, write the end of directory bytes.
        /// Then close and dispose the underlying <see cref="FileStream"/>.
        /// If the last file written to only contains a header, then delete it.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_framesInFile > 0)
                        WriteEndOfDir();
                    var lastFileName = _fileStream?.Name;
                    _fileStream?.Close();
                    _fileStream?.Dispose();
                    var fileInfo = new FileInfo(lastFileName);
                    if (fileInfo.Exists && fileInfo.Length <= _headerLength)
                        File.Delete(lastFileName);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Semi-Initialized byte array containing the header information for the .tif file
        /// </summary>
        private readonly byte[] _header = new byte[]
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
        private readonly byte[] _ifd = new byte[]
        {
            // IDF (Number of Entries)
            0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 000-007: Number of Entries = 15
            // Width Entry
            (byte)Tags.Width, _tagPrefix, (byte)Fields.Short, _fieldPrefix,               // Bytes 008-011: Tag and Field Type = Width, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 012-019: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 020-027: Width Value = 0 ****(Uninitialized)****
            // Height Entry
            (byte)Tags.Height, _tagPrefix, (byte)Fields.Short, _fieldPrefix,              // Bytes 028-031: Tag and Field Type = Height, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 032-039: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 040-047: Height Value = 0 ****(Uninitialized)****
            // Bits Per Sample Entry
            (byte)Tags.BPS, _tagPrefix, (byte)Fields.Short, _fieldPrefix,                 // Bytes 048-051: Tag and Field Type = Bits Per Sample, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 052-059: Number of values = 1 (Grayscale)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 060-067: Bits Per Sample Value = 0 ****(Uninitialized)****
            // Compression Entry
            (byte)Tags.Compression, _tagPrefix, (byte)Fields.Short, _fieldPrefix,         // Bytes 068-071: Tag and Field Type = Compression, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 072-079: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 080-087: Compression Value = 1 (No Compression)
            // Photometric Interpolation Entry
            (byte)Tags.PhotoInt, _tagPrefix, (byte)Fields.Short, _fieldPrefix,            // Bytes 088-091: Tag and Field Type = Photometric Interpolation, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 092-099: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 100-107: Photometric Interpolation Value = 1 (Grayscale)
            // Strip Offset Entry
            (byte)Tags.StripOffset, _tagPrefix, (byte)Fields.Long8, _fieldPrefix,         // Bytes 108-111: Tag and Field Type = Strip Offset, Long8
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 112-119: Number of values = 1
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 120-127: Strip Offset Value = 16 ****(Variable)****
            // Orientation Entry
            (byte)Tags.Orientation, _tagPrefix, (byte)Fields.Short, _fieldPrefix,         // Bytes 128-131: Tag and Field Type = Orientation, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 132-139: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 140-147: Orientation Value = 1
            // Samples Per Pixel Entry
            (byte)Tags.SamplesPerPixel, _tagPrefix, (byte)Fields.Short, _fieldPrefix,     // Bytes 148-151: Tag and Field Type = Samples Per Pixel, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 152-159: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 160-167: Samples Per Pixel Value = 1 (Grayscale)
            // Rows Per Strip Entry
            (byte)Tags.RowsPerStrip, _tagPrefix, (byte)Fields.Short, _fieldPrefix,        // Bytes 168-171: Tag and Field Type = Rows Per Strip, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 172-179: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 180-187: Rows Per Strip Value = 0 ****(Uninitialized)****
            // Strip Bytes Entry
            (byte)Tags.StripBytesCount, _tagPrefix, (byte)Fields.Long, _fieldPrefix,      // Bytes 188-191: Tag and Field Type = Strip Bytes, long
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 192-199: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 200-207: Strip Bytes Value = 0 ****(Uninitialized)****
            // Minimum Sample Value Entry
            (byte)Tags.MinSampleVal, _tagPrefix, (byte)Fields.Short, _fieldPrefix,        // Bytes 208-211: Tag and Field Type = Minimum Sample Value, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 212-219: Number of values = 1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 220-227: Minimum Sample Value = 0
            // Maxmimum Sample Value Entry
            (byte)Tags.MaxSampleVal, _tagPrefix, (byte)Fields.Short, _fieldPrefix,        // Bytes 228-231: Tag and Field Type = Maxmimum Sample Value, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 232-239: Number of values = 1
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 240-247: Maxmimum Sample Value = 65535 (Mono16)
            // Planar Configuration Entry
            (byte)Tags.PlanarConfig, _tagPrefix, (byte)Fields.Short, _fieldPrefix,        // Bytes 248-251: Tag and Field Type = Planar Configuration, ushort
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 252-259: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 260-267: Planar Configuration Value = 1
            // Page Number Entry
            (byte)Tags.PageNumber, _tagPrefix, (byte)Fields.Short, _fieldPrefix,          // Bytes 268-271: Tag and Field Type = Page Number, ushort
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 272-279: Number of values = 2 (Min, Max)
            0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00,                             // Bytes 280-287: Page Number Values = (0, short.Max) ****(Variable)****
            // Sample Format Entry
            (byte)Tags.SampleFormat, _tagPrefix, (byte)Fields.Short, _fieldPrefix,        // Bytes 288-291: Tag and Field Type = Sample Format, long
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 292-299: Number of values = 1
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,                             // Bytes 300-307: Sample Format Value = 1 (Mono16)
        };

        /// <summary>
        /// Hex for tags used in the IFDs
        /// </summary>
        private enum Tags : byte
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
        private enum Fields : byte
        {
            Short = 0x03,
            Long = 0x04,
            Long8 = 0x10
        }

        /// <summary>
        /// Indices for the field values that may be changed during a given frame
        /// </summary>
        private enum FieldValueIndex
        {
            Width = 20,
            Height = 40,
            BitsPerSample = 60,
            StripOffset = 120,
            RowsPerStrip = 180,
            StripBytes = 200,
            MaximumSampleValue = 240,
            PageNumber = 280
        }
    }
}
