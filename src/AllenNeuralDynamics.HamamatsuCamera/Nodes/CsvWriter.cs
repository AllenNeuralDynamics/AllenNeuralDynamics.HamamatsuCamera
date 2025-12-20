using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    /// <summary>
    /// Bonsai node for configuring the <see cref="CsvWriterHelper"/> in the <see cref="CameraCapture"/> class.
    /// </summary>
    public class CsvWriter : Sink<IFrameContainer>
    {
        private EventWaitHandle _csvWriterEvent;
        private Mutex _mutex;

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
        public PathSuffix Suffix { get; set; } = PathSuffix.Timestamp;

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output file if it already exists.")]
        public bool Overwrite { get; set; }

        /// <summary>
        /// Generates an <see cref="EventWaitHandle"/> and <see cref="Mutex"/> to notify the <see cref="CameraCapture"/>
        /// of this node's existence. Then, passes these properties to <see cref="CsvWriterShared"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override IObservable<IFrameContainer> Process(IObservable<IFrameContainer> source)
        {
            _csvWriterEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{nameof(CsvWriter)}_Event");
            _mutex = new Mutex(false, $"{nameof(CsvWriter)}_Shared");
            _mutex.WaitOne();
            CsvWriterShared.Properties = new CsvWriterProperties()
            {
                FileName = FileName,
                Suffix = Suffix,
                Overwrite = Overwrite
            };
            _mutex.ReleaseMutex();
            _csvWriterEvent.Set();
            return source.Finally(() =>
            {
                _csvWriterEvent.Dispose();
                _mutex.Dispose();
            });
        }
    }
}
