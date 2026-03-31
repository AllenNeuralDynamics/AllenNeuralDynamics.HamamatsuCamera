using Bonsai;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    public class Processing : Transform<IFrameContainer, IFrameContainer>
    {
        private EventWaitHandle _processingEvent;
        private Mutex _mutex;

        /// <summary>
        /// Number of channels to deinterleave into.
        /// </summary>
        [Description("Number of channels to deinterleave into.")]
        public byte DeinterleaveCount { get; set; } = 1;

        /// <summary>
        /// Generates an <see cref="EventWaitHandle"/> and <see cref="Mutex"/> to notify the <see cref="CameraCapture"/>
        /// of this node's existence. Then, passes these properties to <see cref="ProcessingShared"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override IObservable<IFrameContainer> Process(IObservable<IFrameContainer> source)
        {
            _processingEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{nameof(Processing)}_Event");
            _mutex = new Mutex(false, $"{nameof(Processing)}_Shared");
            _mutex.WaitOne();
            ProcessingShared.DeinterleaveCount = DeinterleaveCount;
            _mutex.ReleaseMutex();
            _processingEvent.Set();
            return source.Finally(() =>
            {
                _processingEvent.Dispose();
                _mutex.Dispose();
            });
        }
    }
}
