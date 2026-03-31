using Bonsai;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace AllenNeuralDynamics.HamamatsuCamera.Nodes
{
    /// <summary>
    /// Converts <see cref="IFrameContainer"/> to <see cref="Frame"/> filtering out any <see cref="FrameBundle"/>
    /// </summary>
    public class Frames : Combinator<IFrameContainer, Frame>
    {
        /// <summary>
        /// Converts <see cref="IFrameContainer"/> to <see cref="Frame"/> filtering out any <see cref="FrameBundle"/>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override IObservable<Frame> Process(IObservable<IFrameContainer> source)
        {
            return source.Select(container => container as Frame)
                         .Where(frame => frame != null);
        }
    }
}
