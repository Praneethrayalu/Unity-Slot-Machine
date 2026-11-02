using System.Collections.Generic;
using SlotGame.Core.Rules;

namespace SlotGame.Core.Game
{
    /// <summary>
    /// Everything decided for one spin, produced up-front before any reel animation starts.
    /// The presentation layer only animates towards these stops; it never decides the outcome.
    /// </summary>
    public sealed class SpinResult
    {
        public SpinResult(int spinNumber, int bet, bool isFreeSpin, int[] stopIndices, int[] lineSymbols, WinResult win)
        {
            SpinNumber = spinNumber;
            Bet = bet;
            IsFreeSpin = isFreeSpin;
            StopIndices = stopIndices;
            LineSymbols = lineSymbols;
            Win = win;
        }

        /// <summary>1-based spin counter for this session.</summary>
        public int SpinNumber { get; }

        /// <summary>Bet the line is paid against (free spins reuse the triggering bet).</summary>
        public int Bet { get; }

        /// <summary>True when the spin was free (no credits were taken).</summary>
        public bool IsFreeSpin { get; }

        /// <summary>Stop index on each reel strip.</summary>
        public IReadOnlyList<int> StopIndices { get; }

        /// <summary>Symbol id on the pay line for each reel.</summary>
        public IReadOnlyList<int> LineSymbols { get; }

        public WinResult Win { get; }

        public override string ToString()
        {
            return $"Spin #{SpinNumber} bet={Bet} free={IsFreeSpin} line=[{string.Join(",", LineSymbols)}] {Win}";
        }
    }
}
