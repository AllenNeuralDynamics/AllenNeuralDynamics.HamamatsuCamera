using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    public sealed class UnmanagedFramePool : IDisposable
    {
        private readonly ConcurrentStack<IntPtr> _pool = new ConcurrentStack<IntPtr>();
        private readonly int _frameSize;
        private readonly int _count;
        private readonly bool _aligned;
        private bool _disposed;

        public int FrameSize => _frameSize;
        public int Count => _count;

        /// <summary>
        /// Initializes the frame pool with the specified characteristics
        /// </summary>
        /// <param name="frameSizeBytes">Size of a frame in bytes</param>
        /// <param name="count">Number of frames allocated</param>
        /// <param name="aligned">64-byte aligned allocation.</param>
        public UnmanagedFramePool(int frameSizeBytes, int count, bool aligned = false)
        {
            _frameSize = frameSizeBytes;
            _count = count;
            _aligned = aligned;

            for (int i = 0; i < count; i++)
            {
                IntPtr ptr;

                if (!_aligned)
                {
                    // Normal allocation
                    ptr = Marshal.AllocHGlobal(frameSizeBytes);
                }
                else
                {
                    // Optional: 64-byte aligned allocation for SIMD / DMA
                    ptr = AllocateAligned(frameSizeBytes, 64);
                }

                _pool.Push(ptr);
            }
        }

        /// <summary>
        /// Rents a pointer.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public bool TryRent(out IntPtr ptr)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnmanagedFramePool));

            return _pool.TryPop(out ptr);
        }

        /// <summary>
        /// Returns a ptr
        /// </summary>
        /// <param name="ptr">ptr to return</param>
        public void Return(IntPtr ptr)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnmanagedFramePool));

            _pool.Push(ptr);
        }

        /// <summary>
        /// Pop and free pointers until pool is empty
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_disposed)
                    return;

                _disposed = true;

                while (_pool.TryPop(out IntPtr ptr))
                {
                    if (!_aligned)
                        Marshal.FreeHGlobal(ptr);
                    else
                        FreeAligned(ptr);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /* -------------------------------------------------------------
            Aligned Allocation Helpers (optional but useful for speed)
        --------------------------------------------------------------*/

        private static IntPtr AllocateAligned(int size, int alignment)
        {
            // Allocate size + alignment + sizeof(void*)
            IntPtr raw = Marshal.AllocHGlobal(size + alignment + IntPtr.Size);

            long rawAddr = raw.ToInt64();

            // Align AFTER space reserved for storing the original pointer
            long aligned = (rawAddr + IntPtr.Size + (alignment - 1)) & ~(alignment - 1);

            // Store original pointer immediately before aligned
            Marshal.WriteIntPtr((IntPtr)(aligned - IntPtr.Size), raw);

            return (IntPtr)aligned;
        }

        private static void FreeAligned(IntPtr alignedPtr)
        {
            // Read original pointer
            IntPtr raw = Marshal.ReadIntPtr(alignedPtr - IntPtr.Size);
            Marshal.FreeHGlobal(raw);
        }

    }
}
