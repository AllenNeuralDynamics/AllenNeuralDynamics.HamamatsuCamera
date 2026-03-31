using System;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    /// <summary>
    /// Static helper class for writing timestamped messages to <see cref="Console"/>
    /// or for writing/suppressing <see cref="Exception"/>.
    /// </summary>
    public static class ConsoleLogger
    {
        /// <summary>
        /// Writes an <see cref="Exception"/> to <see cref="Console"/> including the
        /// stack trace and message.
        /// </summary>
        /// <param name="ex"><see cref="Exception"/> to be written to <see cref="Console"/>.</param>
        public static void LogError(Exception ex)
        {
            Console.WriteLine($"Error: {ex.StackTrace}\nMessage: {ex.Message}");
        }

        /// <summary>
        /// Intentionally does nothing, suppressing an <see cref="Exception"/>.
        /// </summary>
        internal static void SuppressError()
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Timestamps and writes as message to <see cref="Console"/>. Heavily used in debugging.
        /// </summary>
        /// <param name="msg">Message to write to <see cref="Console"/>.</param>
        internal static void LogMessage(string msg)
        {
            var now = DateTime.Now.TimeOfDay;
            Console.WriteLine($"{now.Hours:D2}:{now.Minutes:D2}:{now.Seconds:D2}.{now.Milliseconds:D3}: {msg}");
        }
    }
}
