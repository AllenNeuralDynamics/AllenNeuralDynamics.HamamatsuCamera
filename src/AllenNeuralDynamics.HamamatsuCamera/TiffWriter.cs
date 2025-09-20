using Bonsai;

using AllenNeuralDynamics.HamamatsuCamera.Factories;

using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bonsai.IO;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    public class TiffWriter
    {
        private UnmanagedMemoryStream UnmanagedMemStream;
        public TiffProperties Props;
        private FileStream StreamEven;
        private BinaryWriter WriterEven;
        private FileStream StreamOdd;
        private BinaryWriter WriterOdd;
        private FileStream CurrStream;
        private BinaryWriter CurrWriter;

        //private string DebugPath;
        //private StreamWriter DebugWriter;
        //private const string ListSeparator = ",";       // List separator for writing to different columns of a .csv file
        private bool FirstFrame;
        public TiffWriter(TiffProperties properties)
        {
            FirstFrame = true;
            Props = properties;
            Props.InitializeTiffDirectory();
            //DebugPath = Props.FolderAbsPath + "_Debug.csv";
            //DebugWriter = new StreamWriter(DebugPath);
            EnsureNewFile(0);
            EnsureNewFile(1);
            CurrStream = StreamEven;
            CurrWriter = WriterEven;
        }


        /// <summary>
        /// Ensures the directory for the .tif file exists
        /// </summary>
        /// <param name="FileNumber">Index of .tif file</param>
        /// <returns>Full path of .tif file to be written to.</returns>
        public void EnsureNewFile(uint FileNumber)
        {
            try
            {
                // Get base filename
                DirectoryInfo dir_info = new DirectoryInfo(Props.FolderName);
                string baseFolderName = dir_info.Name;
                string baseFileName = string.IsNullOrEmpty(Props.BaseFileName) ? baseFolderName : Props.BaseFileName;
                // Remove any extension
                if (!string.IsNullOrEmpty(Path.GetExtension(baseFileName)))
                    baseFileName = Path.GetFileNameWithoutExtension(baseFileName);


                string filePath = Path.Combine(new string[] { Props.FolderAbsPath, baseFileName });


                // Append Filenumber and .tif extension
                filePath = filePath + FileNumber.ToString() + ".tif";

                // Verify that file does not exist
                if (Directory.Exists(filePath))
                    throw new IOException(string.Format("The file '{0}' already exists.", filePath));

                if (FileNumber % 2 == 0)
                {
                    StreamEven = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                    WriterEven = new BinaryWriter(StreamEven);
                }
                else
                {
                    StreamOdd = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                    WriterOdd = new BinaryWriter(StreamOdd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: EnsureNewFile\nMessage: {ex.Message}");
            }
        }
        //Stopwatch sw = new Stopwatch();
        public void WriteFrame(ref Frame frame)
        {
            unsafe
            {
                if (FirstFrame)
                {
                    UnmanagedMemStream = new UnmanagedMemoryStream((byte*)frame.bufframe.buf, Props.BytesPerImage);
                }

                // Update the next IFD offset and write it to file
                if (Props.FramesInFile == 0)
                {

                    //Console.WriteLine("Write Header");
                    CurrWriter.Write(Props.Header);
                }
                else
                {
                    Props.UpdateNextIFDOffset();
                    CurrWriter.Write(Props.NextIFDOffsetBytes);
                }


                // Write Image data from unmanaged memory stream to file stream
                UnmanagedMemStream.CopyTo(CurrStream);

                //var dt1 = sw.ElapsedMilliseconds;
                // Update Fields
                Props.UpdateFields((uint)frame.Framestamp);

                //var dt2 = sw.ElapsedMilliseconds;
                // Write the IFD
                CurrWriter.Write(Props.IFD);
                //var dt3 = sw.ElapsedMilliseconds;

                // Update values for next frame
                Props.UpdateValues();
                //var dt4 = sw.ElapsedMilliseconds;


                //if (DebugWriter != null)
                //    WriteDebugInfo(frame, dt0, dt1, dt2, dt3, dt4);
                // If last frame in file,
                if (Props.FramesInFile >= Props.FramesPerTiff)
                {
                    // The, Write end of directory and Close file
                    CurrWriter.Write(Props.EndOfDir);

                    // Update current writer and current stream
                    Props.FileNumber++;
                    Console.WriteLine(Props.FileNumber);
                    CurrWriter = Props.FileNumber % 2 == 0 ? WriterEven : WriterOdd;
                    CurrStream = Props.FileNumber % 2 == 0 ? StreamEven : StreamOdd;
                    Props.ResetValues();
                    Props.InitializeTiffFile();

                    // Close previous writer/stream and open next writer/stream asynchronously
                    Task.Run(() => OpenNextTiffWriter(Props.FileNumber + 1));
                }
            }

        }

        private void OpenNextTiffWriter(uint NextFileNumber)
        {
            if (NextFileNumber % 2 == 0)
            {
                WriterEven.Close();
                StreamEven.Close();
                EnsureNewFile(NextFileNumber);
            }
            else
            {
                WriterOdd.Close();
                StreamOdd.Close();
                EnsureNewFile(NextFileNumber);
            }
        }

        //private void WriteDebugInfo(Frame frame, long dt0, long dt1, long dt2, long dt3, long dt4)
        //{
        //    var debugValues = new List<string>
        //    {
        //        frame.Framestamp.ToString(CultureInfo.InvariantCulture),
        //        frame.ComputerTimestamp.ToString(CultureInfo.InvariantCulture),
        //        frame.CameraTimestamp.ToString(CultureInfo.InvariantCulture),
        //        dt0.ToString(CultureInfo.InvariantCulture),
        //        dt1.ToString(CultureInfo.InvariantCulture),
        //        dt2.ToString(CultureInfo.InvariantCulture),
        //        dt3.ToString(CultureInfo.InvariantCulture),
        //        dt4.ToString(CultureInfo.InvariantCulture)
        //    };
        //    var debugNewLine = string.Join(ListSeparator, debugValues);
        //    DebugWriter.WriteLine(debugNewLine);
        //}

        public void Close()
        {

            if (Props.FramesInFile > 0)
            {
                if (Props.FileNumber % 2 == 0)
                    WriterEven.Write(Props.EndOfDir);
                else
                    WriterOdd.Write(Props.EndOfDir);
            }

            string lastEvenFile = StreamEven.Name;
            string lastOddFile = StreamOdd.Name;
            WriterEven.Close();
            WriterOdd.Close();
            StreamEven.Close();
            StreamOdd.Close();
            UnmanagedMemStream.Close();

            // Delete file if empty
            FileInfo fileInfo = new FileInfo(lastEvenFile);
            if (fileInfo.Exists && fileInfo.Length <= Props.Header.Length)
                File.Delete(lastEvenFile);
            fileInfo = new FileInfo(lastOddFile);
            if (fileInfo.Exists && fileInfo.Length <= Props.Header.Length)
                File.Delete(lastOddFile);
        }

        public void Initialize(double width, double height, double bytesPerImage)
        {
            Props.Width = (ushort)width;
            Props.Height = (ushort)height;
            Props.BytesPerImage = (uint)bytesPerImage;
            Props.ResetValues();
            Props.InitializeTiffFile();
        }
    }
}
