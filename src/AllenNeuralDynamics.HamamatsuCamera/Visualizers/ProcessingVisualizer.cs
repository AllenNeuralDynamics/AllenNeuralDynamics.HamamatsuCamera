using Bonsai;
using Bonsai.Design;
using AllenNeuralDynamics.HamamatsuCamera;
using AllenNeuralDynamics.HamamatsuCamera.Factories;
using AllenNeuralDynamics.HamamatsuCamera.Visualizers;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

[assembly: TypeVisualizer(typeof(ProcessingVisualizer), Target = typeof(C13440))]
namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    /// <summary>
    /// Allows <see cref="Bonsai"/> to recognize that the <see cref="ImageProcessing"/>
    /// node has a visualizer. Loads the <see cref="ProcessingView"/> into the visualizer
    /// service and updates it when new data is output from the <see cref="ImageProcessing"/> class.
    /// </summary>
    public class ProcessingVisualizer : DialogTypeVisualizer
    {
        // Custom UserControl to be displayed in the visualizer service
        internal Component1 view;

        /// <summary>
        /// Loads a <see cref="ProcessingView"/> into the visualizer.
        /// Called by <see cref="Bonsai"/> when opening a visualizer through
        /// the UI.
        /// </summary>
        /// <param name="provider"></param>
        public override void Load(IServiceProvider provider)
        {
            // Create the view to be displayed
            view = new Component1();

            // Load the view into the visualizer service
            var visualizerService = (IDialogTypeVisualizerService)provider.GetService(typeof(IDialogTypeVisualizerService));
            if (visualizerService != null)
            {
                visualizerService.AddControl(view);
                view.Dock = System.Windows.Forms.DockStyle.Fill;
            }
        }

        /// <summary>
        /// Updates the <see cref="ProcessingView"/> when a new
        /// processed frame is sent from the <see cref="ImageProcessing"/> class.
        /// </summary>
        /// <param name="frame">Processed frame from the <see cref="ImageProcessing"/> class.</param>
        private void Show(VisualizerData frame)
        {
            view.UpdateFrame(frame);
        }

        /// <summary>
        /// Entrance point for data from the <see cref="ImageProcessing"/> class.
        /// </summary>
        /// <param name="value">Processed <see cref="ImageData"/> from the <see cref="ImageProcessing"/> class.</param>
        public override void Show(object value)
        {
            // Verify data type and show it
            if (value is VisualizerData frame)
            {
                Show(frame);
            }
        }

        /// <summary>
        /// Disposes the loaded <see cref="ProcessingView"/> instance.
        /// </summary>
        public override void Unload()
        {
            view.Dispose();
        }
        /// <summary>
        /// Entrance point for data from the <see cref="C13440"/> class.
        /// Creates and observable sequence of <see cref="ConvertedFrame"/>
        /// as <see cref="object"/>. Asynchronously converts the <see cref="Frame"/>
        /// to <see cref="ConvertedFrame"/> using a writable factory: <see cref="ImageVisualizerFactory"/>.
        /// Sends the <see cref="ConvertedFrame"/> to the loaded <see cref="ImageView"/> using the
        /// overrided <see cref="Show(object)"/> method. Throttles the passing of data to the 
        /// <see cref="ImageView"/> to 40 Hz.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public override IObservable<object> Visualize(IObservable<IObservable<object>> source, IServiceProvider provider)
        {
            var visualizerControl = provider.GetService(typeof(IDialogTypeVisualizerService)) as Control;
            if (visualizerControl != null)
            {
                return Observable.Create<VisualizerData>(observer =>
                {
                    var transport = CreateTransport(observer);
                    var sourceDisposable = new SingleAssignmentDisposable();
                    sourceDisposable.Disposable = source.SelectMany(xs => xs.Select(input => (Frame)input))
                                                        .Subscribe(transport.Write);

                    return new CompositeDisposable(sourceDisposable, transport);
                })
                .ObserveOn(visualizerControl)
                //.Do(output => UpdateLog(output.FrameCount, output.Timestamp))
                .Select(visualizerData => (object)visualizerData)
                .Do(Show, SequenceCompleted);
            }

            return source;
        }

        /// <summary>
        /// Creates the <see cref="ImageVisualizerFactory"/> for converting 
        /// <see cref="Frame"/> to <see cref="ConvertedFrame"/>.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        private VisualizerFactory CreateTransport(IObserver<VisualizerData> observer)
        {
            var transport = new VisualizerFactory(observer);
            transport.Open();
            return transport;
        }
    }
}
