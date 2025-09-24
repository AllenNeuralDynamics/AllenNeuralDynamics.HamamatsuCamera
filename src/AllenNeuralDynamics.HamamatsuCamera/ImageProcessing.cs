using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.API;
using AllenNeuralDynamics.HamamatsuCamera.Factories;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCV.Net;
using System.Runtime.InteropServices;
using System.Reflection;


public static class DcamInterop
{
    public static int GetPixelTypeCode(DCAM_PIXELTYPE type)
    {
        // Use reflection to extract private field if absolutely necessary:
        var field = typeof(DCAM_PIXELTYPE).GetField("pixeltype", BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)field.GetValue(type);
    }

    public static IplImage ConvertToIplImage(DCAMBUF_FRAME frame)
    {
        if (frame.buf == IntPtr.Zero)
            throw new InvalidOperationException("Frame buffer is null.");

        // Determine depth and number of channels
        IplDepth depth;
        int channels;
        int bytesPerPixel;

        switch (GetPixelTypeCode(frame.type))
        {
            case 0x00000001: // MONO8
                depth = IplDepth.U8;
                channels = 1;
                bytesPerPixel = 1;
                break;

            case 0x00000002: // MONO16
                depth = IplDepth.U16;
                channels = 1;
                bytesPerPixel = 2;
                break;

            // Add more if needed

            default:
                throw new NotSupportedException($"Unsupported DCAM pixel type: 0x{GetPixelTypeCode(frame.type):X8}");
        }

        var size = new OpenCV.Net.Size(frame.width, frame.height);
        var image = new IplImage(size, depth, channels);

        int bytesToCopyPerRow = frame.width * channels * bytesPerPixel;

        unsafe
        {
            byte* srcPtr = (byte*)frame.buf.ToPointer();
            byte* dstPtr = (byte*)image.ImageData.ToPointer();

            for (int y = 0; y < frame.height; y++)
            {
                Buffer.MemoryCopy(
                    srcPtr + y * frame.rowbytes,
                    dstPtr + y * image.WidthStep,
                    image.WidthStep,
                    bytesToCopyPerRow
                );
            }
        }

        return image;
    }
}

namespace AllenNeuralDynamics.HamamatsuCamera
{

    /// <summary>
    /// Transforms <see cref="Frame"/> data coming from the <see cref="C13440"/>
    /// class into <see cref="ImageData"/> that is written to .csv and displayed
    /// with the <see cref="ImageProcessingVisualizer"/>.
    /// Averages the pixel data contained within each ROI and outputs them alongside the
    /// <see cref="Frame.Metadata"/>.
    /// </summary>
    public class ImageProcessing
    {
        private const int MetaDataOffset = 9;           // Number of columns of MetaData in the output .csv file
        private const string ListSeparator = ",";       // List separator for writing to different columns of a .csv file
        private const string RegionLabel = "Region";   // Base column header for region data, combined with region index

        public ImageProcessingProperties Props;
        private StreamWriter Writer;

        public ImageProcessing(ImageProcessingProperties properties)
        {
            Props = properties;
            Props.InitializeProcessing();
            CreateWriter();
        }

        /// <summary>
        /// Creates the .csv writer and writes the header row to file.
        /// </summary>
        private void CreateWriter()
        {
            try
            {
                // Verify a valid file path exists
                var fileName = Props.FileName;
                //DebugPath = Props.FileName.Split('.')[0] + "_Debug.csv";
                if (string.IsNullOrEmpty(fileName))
                    throw new InvalidOperationException("A valid file path must be specified.");

                // Ensure the directory exists and append the user-defined suffix
                PathHelper.EnsureDirectory(fileName);
                fileName = PathHelper.AppendSuffix(fileName, Props.Suffix);
                //PathHelper.EnsureDirectory(DebugPath);
                //DebugPath = PathHelper.AppendSuffix(DebugPath, PathSuffix.FileCount);

                // Verify that if the file already exists, the user gave permission to overwrite it.
                if (File.Exists(fileName) && !Props.Overwrite)
                    throw new IOException(string.Format("The file '{0}' already exists.", fileName));

                // Create .csv writer
                Writer = new StreamWriter(fileName, false, Encoding.ASCII);
                //DebugWriter = new StreamWriter(DebugPath);

                var columns = new List<string>()
            {
                nameof(Frame.Width),
                nameof(Frame.Height),
                nameof(Frame.Left),
                nameof(Frame.Top),
                nameof(Frame.Framestamp),
                nameof(Frame.ComputerTimestamp),
                nameof(Frame.CameraTimestamp)
            };

                for (int i = 0; i < Props.Regions.Count; i++)
                    columns.Add(RegionLabel + i);
                //for (int i = 0; i < Props.Regions.Count; i++)
                //    columns.Add(RegionLabel + i + " With LUT");

                var header = string.Join(ListSeparator, columns);
                Writer.WriteLine(header);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateWriter\nMessage: {ex.Message}");
            }
        }
        Stopwatch sw = new Stopwatch();
        public void Process(ref Frame frame)
        {
            try
            {
                try
                {
                    frame.Image = DcamInterop.ConvertToIplImage(frame.bufframe);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: IplImage conversion failed: {ex.Message}");
                    return;
                }

                var values = new List<string>()
            {
                frame.Width.ToString(CultureInfo.InvariantCulture),
                frame.Height.ToString(CultureInfo.InvariantCulture),
                frame.Left.ToString(CultureInfo.InvariantCulture),
                frame.Top.ToString(CultureInfo.InvariantCulture),
                frame.Framestamp.ToString(CultureInfo.InvariantCulture),
                frame.ComputerTimestamp.ToString(CultureInfo.InvariantCulture),
                frame.CameraTimestamp.ToString(CultureInfo.InvariantCulture)
            };
                double[] activity = null;
                if (frame.PixelType == DCAM_PIXELTYPE.MONO8)
                    activity = ProcessMono8(frame);
                else if (frame.PixelType == DCAM_PIXELTYPE.MONO16)
                    activity = ProcessMono16(frame);
                else
                    return;
                if (activity != null)
                {
                    for (int i = 0; i < activity.Length; i++)
                    {
                        values.Add(activity[i].ToString(CultureInfo.InvariantCulture));
                    }
                }

                var newLine = string.Join(ListSeparator, values);
                Writer.WriteLine(newLine);
                frame.Regions = Props.Regions;
                frame.LookupTable = Props.LookupTable;
                frame.RegionData = activity.ToList();
                frame.DeinterleaveCount = Math.Max((int)Props.DeinterleaveCount, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Process\nMessage: {ex.Message}");
            }
        }

        private double[] ProcessMono16(Frame frame)
        {

            double[] output = new double[Props.Regions.Count];
            try
            {
                unsafe
                {
                    ushort* img_ptrFirstPixel = (ushort*)frame.bufframe.buf;

                    for (int i = 0; i < Props.Regions.Count; i++)
                    {
                        var region = Props.Regions[i];
                        int reg_widthInPixels = region.Width;
                        int reg_heightInPixels = region.Height;
                        int reg_offsetInPixels = Math.Max(0, (region.Y - frame.Top)) * frame.Width + Math.Max(0, (region.X - frame.Left));
                        ushort* reg_ptrFirstPixel = img_ptrFirstPixel + reg_offsetInPixels;

                        double[] rowSums = new double[reg_heightInPixels];
                        double numRows = reg_heightInPixels;

                        // Parallel process each row of pixels contained in the region
                        Parallel.For(0, reg_heightInPixels, y =>
                        {
                            // Offset to first pixel of row in image
                            int row_offsetInPixels = frame.Width * y;

                            // Pointer to first pixel of the row in the image that is contained in the region
                            ushort* row_ptrFirstPixel = reg_ptrFirstPixel + row_offsetInPixels;

                            double rowSum = 0;
                            ushort* row_ptrLastPixel = row_ptrFirstPixel + reg_widthInPixels;
                            while (row_ptrFirstPixel < row_ptrLastPixel)
                                rowSum = rowSum + *(row_ptrFirstPixel++);
                            rowSums[y] = rowSum;
                        });
                        output[i] = Props.LookupTable[(int)(rowSums.Average() / (double)reg_widthInPixels)];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ProcessMono16\nMessage: {ex.Message}");
            }

            return output;
        }

        private double[] ProcessMono8(Frame frame)
        {
            double[] output = new double[Props.Regions.Count];
            //double[][] output = new double[2][];
            try
            {
                unsafe
                {
                    byte* img_ptrFirstPixel = (byte*)frame.bufframe.buf;
                    int img_widthInBytes = frame.bufframe.rowbytes;
                    //output[0] = new double[Props.Regions.Count];
                    //output[1] = new double[Props.Regions.Count];
                    for (int i = 0; i < Props.Regions.Count; i++)
                    {
                        var region = Props.Regions[i];
                        int reg_widthInPixels = region.Width;
                        int reg_heightInPixels = region.Height;
                        int reg_offsetInBytes = Math.Max(0, (region.Y - frame.Top)) * frame.Width + Math.Max(0, (region.X - frame.Left));
                        byte* reg_ptrFirstPixel = img_ptrFirstPixel + reg_offsetInBytes;

                        double[] rowSums = new double[reg_heightInPixels];
                        //double[] transformedRowSums = new double[reg_heightInPixels];
                        double numRows = reg_heightInPixels;

                        // Parallel process each row of pixels contained in the region
                        Parallel.For(0, reg_heightInPixels, y =>
                        {
                            // Offset to first pixel of row in image
                            int row_offsetInBytes = img_widthInBytes * y;

                            // Pointer to first pixel of the row in the image that is contained in the region
                            byte* row_ptrFirstPixel = reg_ptrFirstPixel + row_offsetInBytes;

                            double rowSum = 0;
                            byte* row_ptrLastPixel = row_ptrFirstPixel + reg_widthInPixels;
                            while (row_ptrFirstPixel < row_ptrLastPixel)
                                rowSum = rowSum + *(row_ptrFirstPixel++);
                            rowSums[y] = rowSum;
                        });

                        output[i] = rowSums.Average() / (double)reg_widthInPixels;
                        //output[0][i] = rowSums.Average() / (double)reg_widthInPixels;
                        //output[1][i] = transformedRowSums.Average() / (double)reg_widthInPixels;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ProcessMono8\nMessage: {ex.Message}");
            }

            return output;
        }

        internal void Close()
        {
            Writer.Close();
        }
    }
}

