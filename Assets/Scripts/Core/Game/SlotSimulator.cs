using System;
using System.Collections.Generic;
using SlotGame.Core.Economy;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;

namespace SlotGame.Core.Game
{
    /// <summary>Aggregate statistics from a batch of simulated spins.</summary>
    public sealed class SimulationReport
    {
        public long PaidSpins { get; internal set; }
        public long FreeSpins { get; internal set; }
        public long TotalWagered { get; internal set; }
        public long TotalWon { get; internal set; }
        public long WinningSpins { get; internal set; }
        public long FreeSpinTriggers { get; internal set; }
        public long BiggestWin { get; internal set; }
        public Dictionary<int, long> WinsBySymbol { get; } = new Dictionary<int, long>();

        /// <summary>Return to player: credits won / credits wagered.</summary>
        public double Rtp => TotalWagered == 0 ? 0 : (double)TotalWon / TotalWagered;

        /// <summary>Share of all spins (paid + free) that paid something.</summary>
        public double HitFrequency => PaidSpins + FreeSpins == 0 ? 0 : (double)WinningSpins / (PaidSpins + FreeSpins);

        public override string ToString()
        {
            return $"paid spins={PaidSpins:N0} free spins={FreeSpins:N0} RTP={Rtp:P2} hit rate={HitFrequency:P2} " +
                   $"free-spin triggers={FreeSpinTriggers:N0} biggest win={BiggestWin:N0}";
        }
    }

    /// <summary>
    /// Plays the real <see cref="SlotEngine"/> headlessly to measure the game's maths
    /// (return to player, hit frequency, feature frequency). Used by the editor RTP tool and tests.
    /// </summary>
    public static class SlotSimulator
    {
        public static SimulationReport Run(ReelSet reels, IPayTable payTable, IRandomProvider random, long paidSpins, int bet)
        {
            if (paidSpins <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paidSpins));
            }

            // A wallet big enough that the simulation never runs dry.
            var wallet = new Wallet(long.MaxValue / 4);
            var bets = new BetSelector(new[] { bet });
            var engine = new SlotEngine(reels, payTable, random, wallet, bets);
            var report = new SimulationReport();

            while (report.PaidSpins < paidSpins || engine.FreeSpinsRemaining > 0)
            {
                if (!engine.TryBeginSpin(out SpinResult spin, out _))
                {
                    break;
                }

                if (spin.IsFreeSpin)
                {
                    report.FreeSpins++;
                }
                else
                {
                    report.PaidSpins++;
                    report.TotalWagered += spin.Bet;
                }

                if (spin.Win.IsWin)
                {
                    report.WinningSpins++;
                    report.TotalWon += spin.Win.Payout;
                    report.BiggestWin = Math.Max(report.BiggestWin, spin.Win.Payout);
                    report.WinsBySymbol.TryGetValue(spin.Win.SymbolId, out long count);
                    report.WinsBySymbol[spin.Win.SymbolId] = count + 1;
                    if (spin.Win.FreeSpinsAwarded > 0)
                    {
                        report.FreeSpinTriggers++;
                    }
                }

                engine.SettleSpin(spin);
            }

            return report;
        }
    }
}
