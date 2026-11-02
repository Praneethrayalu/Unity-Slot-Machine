using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlotGame.Core.Economy;
using SlotGame.Core.Game;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;
using SlotGame.Utilities;
using static SlotGame.Tests.FakePayTable;

namespace SlotGame.Tests
{
    public sealed class SlotEngineTests
    {
        /// <summary>Returns a scripted sequence of numbers so a test can choose exact reel stops.</summary>
        private sealed class ScriptedRandom : IRandomProvider
        {
            private readonly Queue<int> _values;

            public ScriptedRandom(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive) => _values.Dequeue();
        }

        // Three short strips: stop 0 = BAR, 1 = BELL, 2 = CHERRY, 3 = SEVEN on every reel.
        private static ReelSet Strips() => new ReelSet(new IReadOnlyList<int>[]
        {
            new[] { Bar, Bell, Cherry, Seven },
            new[] { Bar, Bell, Cherry, Seven },
            new[] { Bar, Bell, Cherry, Seven },
        });

        private static SlotEngine Engine(IRandomProvider random, long balance = 1000)
        {
            return new SlotEngine(Strips(), new FakePayTable(), random, new Wallet(balance), new BetSelector(new[] { 10, 20 }));
        }

        [Test]
        public void Spin_TakesBetUpFront_AndPaysOnlyWhenSettled()
        {
            SlotEngine engine = Engine(new ScriptedRandom(0, 0, 0)); // BAR BAR BAR

            Assert.IsTrue(engine.TryBeginSpin(out SpinResult spin, out _));
            Assert.AreEqual(990, engine.Wallet.Balance, "bet is taken immediately");
            Assert.AreEqual(100, spin.Win.Payout);

            engine.SettleSpin(spin);
            Assert.AreEqual(1090, engine.Wallet.Balance, "win is paid at settlement");
        }

        [Test]
        public void Spin_IsRejected_WhenCreditsAreInsufficient()
        {
            SlotEngine engine = Engine(new ScriptedRandom(), balance: 5);

            Assert.IsFalse(engine.TryBeginSpin(out _, out SpinRejection rejection));
            Assert.AreEqual(SpinRejection.InsufficientCredits, rejection);
            Assert.AreEqual(5, engine.Wallet.Balance);
        }

        [Test]
        public void Spin_IsRejected_WhilePreviousSpinIsUnsettled()
        {
            SlotEngine engine = Engine(new ScriptedRandom(2, 1, 0, 0, 0, 0));
            engine.TryBeginSpin(out _, out _);

            Assert.IsFalse(engine.TryBeginSpin(out _, out SpinRejection rejection));
            Assert.AreEqual(SpinRejection.SpinInProgress, rejection);
        }

        [Test]
        public void ThreeBells_AwardFreeSpins_ThatCostNothing()
        {
            // Spin 1: BELL BELL BELL (3 free spins). Spins 2-4: losing free spins.
            SlotEngine engine = Engine(new ScriptedRandom(1, 1, 1, 0, 1, 2, 0, 1, 2, 0, 1, 2));

            engine.TryBeginSpin(out SpinResult trigger, out _);
            engine.SettleSpin(trigger);
            Assert.AreEqual(3, engine.FreeSpinsRemaining);
            long balanceAfterTrigger = engine.Wallet.Balance;

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(engine.TryBeginSpin(out SpinResult free, out _));
                Assert.IsTrue(free.IsFreeSpin);
                engine.SettleSpin(free);
            }

            Assert.AreEqual(0, engine.FreeSpinsRemaining);
            Assert.AreEqual(balanceAfterTrigger, engine.Wallet.Balance, "free spins never debit the wallet");
        }

        [Test]
        public void SettlingTwice_Throws()
        {
            SlotEngine engine = Engine(new ScriptedRandom(0, 1, 2));
            engine.TryBeginSpin(out SpinResult spin, out _);
            engine.SettleSpin(spin);

            Assert.Throws<InvalidOperationException>(() => engine.SettleSpin(spin));
        }

        [Test]
        public void Simulation_MatchesExactRtp()
        {
            // Production-like strips: 20 stops, two sevens (one on the last reel).
            int[] reel = { Cherry, Bell, Cherry, Bar, Cherry, Bell, Seven, Cherry, Bell, Bar, Cherry, Bell, Cherry, Bar, Bell, Cherry, Seven, Bell, Cherry, Bar };
            int[] last = { Cherry, Bell, Cherry, Bar, Cherry, Bell, Seven, Cherry, Bell, Bar, Cherry, Bell, Cherry, Bar, Bell, Cherry, Cherry, Bell, Cherry, Bar };
            var reels = new ReelSet(new IReadOnlyList<int>[] { reel, reel, last });
            var payTable = new FakePayTable();

            TheoreticalReturn exact = PayTableMath.Calculate(reels, payTable);
            SimulationReport simulated = SlotSimulator.Run(reels, payTable, new SeededRandomProvider(99), 300_000, 1);

            Assert.AreEqual(8000, exact.Combinations);
            Assert.That(exact.TotalReturn, Is.InRange(0.90, 0.99), "the game should be fair but not pay more than it takes");
            Assert.AreEqual(exact.TotalReturn, simulated.Rtp, 0.03, "simulation should converge on the exact figure");
            Assert.AreEqual(exact.HitFrequency, simulated.HitFrequency, 0.01);
        }

        [TestCase("https://x.y/index.html?seed=42", true, 42)]
        [TestCase("https://x.y/index.html?a=1&seed=-7#top", true, -7)]
        [TestCase("https://x.y/index.html", false, 0)]
        [TestCase("https://x.y/index.html?seed=abc", false, 0)]
        public void UrlParameters_AreParsed(string url, bool found, int value)
        {
            Assert.AreEqual(found, AppBootstrap.TryReadIntParameter(url, "seed", out int parsed));
            Assert.AreEqual(value, parsed);
        }
    }
}
