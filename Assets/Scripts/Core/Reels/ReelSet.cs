using System;
using System.Collections.Generic;
using SlotGame.Core.Random;

namespace SlotGame.Core.Reels
{
    /// <summary>
    /// The mathematical reels: one strip of symbol ids per reel.
    /// <para>
    /// Like a physical machine, the outcome is a uniformly random stop position on each strip;
    /// symbol frequency on a strip (not a hidden weight table) is what defines the odds.
    /// This keeps outcomes fair and auditable: the probability of any symbol is simply
    /// (copies on the strip / strip length).
    /// </para>
    /// </summary>
    public sealed class ReelSet
    {
        private readonly int[][] _strips;

        public ReelSet(IReadOnlyList<IReadOnlyList<int>> strips)
        {
            if (strips == null || strips.Count == 0)
            {
                throw new ArgumentException("At least one reel strip is required.", nameof(strips));
            }

            _strips = new int[strips.Count][];
            for (int reel = 0; reel < strips.Count; reel++)
            {
                IReadOnlyList<int> strip = strips[reel];
                if (strip == null || strip.Count == 0)
                {
                    throw new ArgumentException($"Reel {reel} has an empty strip.", nameof(strips));
                }

                _strips[reel] = new int[strip.Count];
                for (int i = 0; i < strip.Count; i++)
                {
                    _strips[reel][i] = strip[i];
                }
            }
        }

        public int ReelCount => _strips.Length;

        public int GetStripLength(int reel)
        {
            return _strips[reel].Length;
        }

        /// <summary>Symbol at <paramref name="stopIndex"/> on a reel; wraps around in both directions.</summary>
        public int GetSymbol(int reel, int stopIndex)
        {
            int[] strip = _strips[reel];
            int wrapped = ((stopIndex % strip.Length) + strip.Length) % strip.Length;
            return strip[wrapped];
        }

        /// <summary>Picks an independent, uniformly random stop index for every reel.</summary>
        public int[] RollStops(IRandomProvider random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var stops = new int[_strips.Length];
            for (int reel = 0; reel < _strips.Length; reel++)
            {
                stops[reel] = random.Next(0, _strips[reel].Length);
            }

            return stops;
        }

        /// <summary>Symbols on the pay line for the given stop indices.</summary>
        public int[] GetLineSymbols(IReadOnlyList<int> stops)
        {
            var symbols = new int[_strips.Length];
            for (int reel = 0; reel < _strips.Length; reel++)
            {
                symbols[reel] = GetSymbol(reel, stops[reel]);
            }

            return symbols;
        }
    }
}
