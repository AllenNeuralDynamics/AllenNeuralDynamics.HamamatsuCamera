using Bonsai;
using Bonsai.Design;
using AllenNeuralDynamics.HamamatsuCamera;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using AllenNeuralDynamics.HamamatsuCamera.Visualizers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;

[assembly: TypeVisualizer(typeof(ProcessingVisualizer), Target = typeof(Processing))]
namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    /// <summary>
    /// Targets <see cref="Processing"/> to provide a visualizer for it at runtime.
    /// </summary>
    public class ProcessingVisualizer : DialogTypeVisualizer
    {
        internal ProcessingView view;

        /// <summary>
        /// Loads the <see cref="ProcessingView"/> to the <see cref="IDialogTypeVisualizerService"/>.
        /// </summary>
        /// <param name="provider"></param>
        public override void Load(IServiceProvider provider)
        {
            view = new ProcessingView();
            var visualizerService = (IDialogTypeVisualizerService)provider.GetService(typeof(IDialogTypeVisualizerService));
            if (visualizerService != null)
            {
                visualizerService.AddControl(view);
                view.Dock = DockStyle.Fill;
            }
        }

        public override void Show(object value)
        {
            // Do nothing, there are dedicated pipelines for updating the ProcessingView
            // that minimize casting
        }

        public override void Unload()
        {
            view.Dispose();
        }

        /// <summary>
        /// Splits the incoming <see cref="IObservable{IFrameContainer}"/> stream into
        /// <see cref="IObservable{Frame}"/> and <see cref="IObservable{FrameBundle}"/>,
        /// where one will always be empty. Samples the incoming images at 30Hz while
        /// buffering 33ms of metadata. Then updates the <see cref="ProcessingView"/>
        /// with the latest image and the buffered metadata.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public override IObservable<object> Visualize(IObservable<IObservable<object>> source, IServiceProvider provider)
        {
            if (provider.GetService(typeof(IDialogTypeVisualizerService)) is Control visualizerControl)
                return source.SelectMany(xs =>
                {
                    var frames = xs.OfType<Frame>();
                    var frameBundles = xs.OfType<FrameBundle>();

                    var imageStream =
                        frames
                            .Sample(TimeSpan.FromMilliseconds(33))   // 30 FPS to UI
                            .ObserveOn(visualizerControl)
                            .Do(f => view.TryUpdateImage(f.Image));
                    var imageBundleStream =
                        frameBundles
                            .Sample(TimeSpan.FromMilliseconds(33))   // 30 FPS to UI
                            .ObserveOn(visualizerControl)
                            .Do(f => view.TryUpdateImageBundle(f.Images));

                    var regionDataStream =
                        frames
                            .Select(f => new VisualizerData(f))
                            .Buffer(TimeSpan.FromMilliseconds(33)) // match your image sampling
                            .Where(batch => batch.Any())
                            .ObserveOn(visualizerControl)
                            .Do(batch => view.TryUpdateRegionDataBatch(batch));
                    var regionDataBundleStream =
                        frameBundles
                            .Select(f => f.Frames)
                            .Buffer(TimeSpan.FromMilliseconds(33)) // match your image sampling
                            .Where(batch => batch.Any())
                            .ObserveOn(visualizerControl)
                            .Do(batch => view.TryUpdateRegionDataBundleBatch(batch));

                    return Observable.Merge<object>(imageStream, regionDataStream, imageBundleStream, regionDataBundleStream);
                }).Finally(() => Unload());

            return source.Finally(() => Unload());
        }
    }
}
