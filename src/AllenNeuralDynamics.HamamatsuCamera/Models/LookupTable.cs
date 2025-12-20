namespace AllenNeuralDynamics.HamamatsuCamera.Models
{
    /// <summary>
    /// Contains an array of <see cref="byte"/> and an array of <see cref="ushort"/> representing
    /// the Mono8 and Mono16 lookup tables.
    /// </summary>
    internal sealed class LookupTable

    {
        /// <summary>
        /// Lookup table for Mono8 images.
        /// </summary>
        public byte[] Mono8 { get; set; }

        /// <summary>
        /// Lookup table for Mono16 images.
        /// </summary>
        public ushort[] Mono16 { get; set; }

        public LookupTable()
        {
            Mono8 = new byte[byte.MaxValue + 1];
            Mono16 = new ushort[ushort.MaxValue + 1];
            for (var i = 0; i <= byte.MaxValue; i++)
                Mono8[i] = (byte)i;
            for (var i = 0; i <= ushort.MaxValue; i++)
                Mono16[i] = (ushort)i;
        }
    }
}
