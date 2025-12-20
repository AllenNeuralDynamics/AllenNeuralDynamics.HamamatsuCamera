using Bonsai;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace AllenNeuralDynamics.HamamatsuCamera.Nodes
{
    /// <summary>
    /// Converts <see cref="IFrameContainer"/> to <see cref="FrameBundle"/> filtering out any <see cref="Frame"/>
    /// </summary>
    public class FrameBundles : Combinator<IFrameContainer, FrameBundle>
    {
        /// <summary>
        /// Converts <see cref="IFrameContainer"/> to <see cref="FrameBundle"/> filtering out any <see cref="Frame"/>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override IObservable<FrameBundle> Process(IObservable<IFrameContainer> source)
        {
            return source.Select(container => container as FrameBundle)
                         .Where(frame => frame != null);
        }
    }
}
