using System;
using System.Security.Cryptography;

namespace SlotGame.Core.Random
{
    /// <summary>
    /// Production random provider backed by <see cref="RandomNumberGenerator"/> (a CSPRNG).
    /// <para>
    /// Outcomes are unpredictable (no seed can be observed or guessed) and unbiased: values are
    /// produced with rejection sampling instead of a plain modulo, so every stop on a reel strip
    /// has exactly the same probability regardless of the strip length.
    /// </para>
    /// </summary>
    public sealed class CryptoRandomProvider : IRandomProvider, IDisposable
    {
        private readonly RandomNumberGenerator _generator;
        private readonly byte[] _buffer = new byte[4];
        private bool _disposed;

        public CryptoRandomProvider()
        {
            _generator = RandomNumberGenerator.Create();
        }

        /// <inheritdoc />
        public int Next(int minInclusive, int maxExclusive)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CryptoRandomProvider));
            }

            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
            }

            uint range = (uint)(maxExclusive - minInclusive);

            // Rejection sampling: discard raw values from the incomplete "tail" bucket at the top of
            // the 32-bit space. Without this, `raw % range` would favour small numbers whenever
            // 2^32 is not an exact multiple of `range` (classic modulo bias).
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            uint raw;
            do
            {
                _generator.GetBytes(_buffer);
                raw = BitConverter.ToUInt32(_buffer, 0);
            }
            while (raw >= limit);

            return (int)(minInclusive + (raw % range));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _generator.Dispose();
            _disposed = true;
        }
    }
}
