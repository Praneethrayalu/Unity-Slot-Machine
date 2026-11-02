using System;
using SlotGame.Core.Reels;

namespace SlotGame.Core.Rules
{
    /// <summary>Exact theoretical figures for a reel set + pay table.</summary>
    public readonly struct TheoreticalReturn
    {
        public TheoreticalReturn(double baseGameReturn, double expectedFreeSpinsPerSpin, double hitFrequency, long combinations)
        {
            BaseGameReturn = baseGameReturn;
            ExpectedFreeSpinsPerSpin = expectedFreeSpinsPerSpin;
            HitFrequency = hitFrequency;
            Combinations = combinations;
        }

        /// <summary>Average payout per credit bet from line wins alone.</summary>
        public double BaseGameReturn { get; }

        /// <summary>Average number of free spins awarded by one spin.</summary>
        public double ExpectedFreeSpinsPerSpin { get; }

        /// <summary>Probability that a single spin pays anything.</summary>
        public double HitFrequency { get; }

        /// <summary>Number of stop combinations enumerated.</summary>
        public long Combinations { get; }

        /// <summary>
        /// Total return to player including free spins. Each free spin is worth the same as a paid
        /// spin and can itself award free spins, so the total is the geometric series
        /// R + qR + q^2R + ... = R / (1 - q).
        /// </summary>
        public double TotalReturn => ExpectedFreeSpinsPerSpin >= 1
            ? double.PositiveInfinity
            : BaseGameReturn / (1 - ExpectedFreeSpinsPerSpin);
    }

    /// <summary>
    /// Computes the exact return to player by enumerating every combination of reel stops.
    /// With three 20-stop reels that is only 8,000 combinations, so the figure is exact,
    /// not an estimate, and the simulator can be tested against it.
    /// </summary>
    public static class PayTableMath
    {
        public static TheoreticalReturn Calculate(ReelSet reels, IPayTable payTable)
        {
            if (reels == null)
            {
                throw new ArgumentNullException(nameof(reels));
            }

            var evaluator = new WinEvaluator(payTable);
            int reelCount = reels.ReelCount;
            var stops = new int[reelCount];
            var line = new int[reelCount];

            long combinations = 1;
            for (int r = 0; r < reelCount; r++)
            {
                combinations *= reels.GetStripLength(r);
            }

            double totalPay = 0;
            double totalFreeSpins = 0;
            long hits = 0;

            // Odometer-style enumeration over every stop of every reel.
            for (long n = 0; n < combinations; n++)
            {
                for (int r = 0; r < reelCount; r++)
                {
                    line[r] = reels.GetSymbol(r, stops[r]);
                }

                WinResult win = evaluator.Evaluate(line, 1);
                if (win.IsWin)
                {
                    hits++;
                    totalPay += win.Payout;
                    totalFreeSpins += win.FreeSpinsAwarded;
                }

                for (int r = reelCount - 1; r >= 0; r--)
                {
                    if (++stops[r] < reels.GetStripLength(r))
                    {
                        break;
                    }

                    stops[r] = 0;
                }
            }

            return new TheoreticalReturn(totalPay / combinations, totalFreeSpins / combinations,
                (double)hits / combinations, combinations);
        }
    }
}
