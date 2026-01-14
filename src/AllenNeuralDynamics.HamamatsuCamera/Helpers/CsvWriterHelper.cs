using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    /// <summary>
    /// Writes frame metadata to a .csv file
    /// </summary>
    internal class CsvWriterHelper : IDisposable
    {
        private StreamWriter _writer;
        private bool disposedValue;
        internal CsvWriterHelper(CsvWriterProperties properties, List<RegionOfInterest> regions)
        {
            InitializeWriter(properties);
            WriteHeader(regions);
        }

        /// <summary>
        /// Creates the underlying <see cref="StreamWriter"/>
        /// </summary>
        /// <param name="properties"></param>
        private void InitializeWriter(CsvWriterProperties properties)
        {
            if (string.IsNullOrEmpty(properties.FileName))
                throw new InvalidOperationException("A valid file path must be specified.");
            PathHelper.EnsureDirectory(properties.FileName);
            properties.FileName = PathHelper.AppendSuffix(properties.FileName, properties.Suffix);
            if (File.Exists(properties.FileName) && !properties.Overwrite)
                throw new IOException(string.Format("The file '{0}' already exists.", properties.FileName));
            _writer = new StreamWriter(properties.FileName, false, Encoding.ASCII);
        }

        /// <summary>
        /// Writer the header row of the .csv file.
        /// </summary>
        /// <param name="regions"></param>
        private void WriteHeader(List<RegionOfInterest> regions)
        {
            var columns = new List<string>()
                {
                    nameof(FramePacket.Width),
                    nameof(FramePacket.Height),
                    nameof(FramePacket.Left),
                    nameof(FramePacket.Top),
                    nameof(FramePacket.FrameIndex),
                    nameof(FramePacket.ElapsedSeconds),
                    nameof(FramePacket.CameraTimestamp),
                    nameof(FramePacket.ComputerTimestamp)
                };

            if (regions == null || regions.Count == 0)
                columns.Add("Region 0");
            else
            {
                for (var i = 0; i < regions.Count; i++)
                    columns.Add($"Region {i}");
            }
            var header = string.Join(",", columns);
            _writer.WriteLine(header);
        }

        /// <summary>
        /// Write new line of the .csv file
        /// </summary>
        /// <param name="newLine">New line.</param>
        internal void Write(string newLine)
        {
            _writer.WriteLine(newLine);
        }


        /// <summary>
        /// Close and dispose the <see cref="StreamWriter"/>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer?.Close();
                    _writer?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
