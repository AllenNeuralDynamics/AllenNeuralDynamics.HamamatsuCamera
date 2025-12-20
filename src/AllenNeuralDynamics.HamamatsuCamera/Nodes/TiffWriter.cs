using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    public class TiffWriter : Sink<IFrameContainer>
    {
        private EventWaitHandle _tiffWriterEvent;
        private Mutex _mutex;

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

        /// <summary>
        /// Generates an <see cref="EventWaitHandle"/> and <see cref="Mutex"/> to notify the <see cref="CameraCapture"/>
        /// of this node's existence. Then, passes these properties to <see cref="TiffWriterShared"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override IObservable<IFrameContainer> Process(IObservable<IFrameContainer> source)
        {

            _tiffWriterEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{nameof(TiffWriter)}_Event");
            _mutex = new Mutex(false, $"{nameof(TiffWriter)}_Shared");
            _mutex.WaitOne();
            TiffWriterShared.Properties = new TiffWriterProperties()
            {
                FolderName = FolderName,
                BaseFileName = BaseFileName,
                Suffix = Suffix,
                FramesPerTiff = FramesPerTiff
            };
            _mutex.ReleaseMutex();
            _tiffWriterEvent.Set();
            return source.Finally(() =>
            {
                _tiffWriterEvent.Dispose();
                _mutex.Dispose();
            });
        }
    }
}
