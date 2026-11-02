using System;

namespace SlotGame.Core.Random
{
    /// <summary>
    /// Deterministic random provider (xorshift128+) for unit tests, simulations and replays.
    /// <para>
    /// The same seed always produces the same sequence on every platform, which makes
    /// "spin #4817 with seed 42" reproducible when debugging a reported outcome.
    /// Never use it for real-money style play: the sequence is predictable by design.
    /// </para>
    /// </summary>
    public sealed class SeededRandomProvider : IRandomProvider
    {
        private ulong _state0;
        private ulong _state1;

        public SeededRandomProvider(int seed)
        {
            Seed = seed;

            // SplitMix64 expands the 32-bit seed into the 128 bits of xorshift state,
            // guaranteeing the state is never all zeros.
            ulong x = (ulong)(uint)seed;
            _state0 = SplitMix64(ref x);
            _state1 = SplitMix64(ref x);
        }

        /// <summary>The seed this provider was created with.</summary>
        public int Seed { get; }

        /// <inheritdoc />
        public int Next(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);

            // Same rejection-sampling idea as the crypto provider, on 64-bit values.
            ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong raw;
            do
            {
                raw = NextUInt64();
            }
            while (raw >= limit);

            return (int)(minInclusive + (long)(raw % range));
        }

        private ulong NextUInt64()
        {
            ulong s1 = _state0;
            ulong s0 = _state1;
            _state0 = s0;
            s1 ^= s1 << 23;
            _state1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
            return _state1 + s0;
        }

        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
