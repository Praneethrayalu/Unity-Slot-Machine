using System;
using System.Collections.Generic;

namespace SlotGame.Core.Rules
{
    /// <summary>
    /// Decides whether a pay line wins and how much it pays.
    /// <para>
    /// Rule (from the brief): the player wins when every reel shows the same symbol on the pay line.
    /// Optional extension: a wild symbol substitutes for any other symbol, so 7-BAR-BAR counts as
    /// BAR-BAR-BAR when 7 is wild. A line made only of wilds pays the wild's own (top) prize.
    /// </para>
    /// The evaluator is stateless and allocation-free, so it is safe to call millions of times in
    /// RTP simulations and unit tests.
    /// </summary>
    public sealed class WinEvaluator
    {
        private readonly IPayTable _payTable;

        public WinEvaluator(IPayTable payTable)
        {
            _payTable = payTable ?? throw new ArgumentNullException(nameof(payTable));
        }

        /// <summary>
        /// Evaluates the symbols on the pay line (one per reel, left to right) for the given bet.
        /// </summary>
        public WinResult Evaluate(IReadOnlyList<int> lineSymbols, int bet)
        {
            if (lineSymbols == null)
            {
                throw new ArgumentNullException(nameof(lineSymbols));
            }

            if (lineSymbols.Count == 0)
            {
                throw new ArgumentException("A pay line needs at least one symbol.", nameof(lineSymbols));
            }

            if (bet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bet), "Bet cannot be negative.");
            }

            if (!TryResolveLineSymbol(lineSymbols, out int symbolId, out bool includesWild))
            {
                return WinResult.None;
            }

            int multiplier = Math.Max(0, _payTable.GetPayoutMultiplier(symbolId));
            int freeSpins = Math.Max(0, _payTable.GetFreeSpinsAwarded(symbolId));

            // A matching line of a symbol the pay table does not reward is not a win.
            if (multiplier == 0 && freeSpins == 0)
            {
                return WinResult.None;
            }

            return new WinResult(true, symbolId, multiplier, (long)bet * multiplier, freeSpins, includesWild);
        }

        /// <summary>
        /// True when the reels that have already stopped can still form a winning line.
        /// Used by the presentation layer to build anticipation on the last reel ("near miss" feel).
        /// </summary>
        public bool CanStillWin(IReadOnlyList<int> stoppedSymbols)
        {
            if (stoppedSymbols == null || stoppedSymbols.Count == 0)
            {
                return true;
            }

            return TryResolveLineSymbol(stoppedSymbols, out _, out _);
        }

        /// <summary>
        /// Finds the single symbol every position agrees on, treating wilds as jokers.
        /// Returns false when two different non-wild symbols are present.
        /// </summary>
        private bool TryResolveLineSymbol(IReadOnlyList<int> symbols, out int symbolId, out bool includesWild)
        {
            int resolved = WinResult.NoSymbol;
            int firstWild = WinResult.NoSymbol;
            includesWild = false;

            for (int i = 0; i < symbols.Count; i++)
            {
                int current = symbols[i];

                if (_payTable.IsWild(current))
                {
                    includesWild = true;
                    if (firstWild == WinResult.NoSymbol)
                    {
                        firstWild = current;
                    }

                    continue;
                }

                if (resolved == WinResult.NoSymbol)
                {
                    resolved = current;
                }
                else if (resolved != current)
                {
                    symbolId = WinResult.NoSymbol;
                    includesWild = false;
                    return false;
                }
            }

            if (resolved == WinResult.NoSymbol)
            {
                // Every position is a wild: the line counts as the wild symbol itself (jackpot).
                symbolId = firstWild;
                includesWild = false;
                return true;
            }

            symbolId = resolved;
            return true;
        }
    }
}
