using System;
using NUnit.Framework;
using SlotGame.Core.Economy;

namespace SlotGame.Tests
{
    public sealed class EconomyTests
    {
        [Test]
        public void Wallet_DebitAndCredit_UpdateBalanceAndRaiseEvents()
        {
            var wallet = new Wallet(100);
            long lastOld = -1, lastNew = -1;
            wallet.BalanceChanged += (o, n) => { lastOld = o; lastNew = n; };

            Assert.IsTrue(wallet.TryDebit(30));
            Assert.AreEqual(70, wallet.Balance);
            Assert.AreEqual(100L, lastOld);
            Assert.AreEqual(70L, lastNew);

            wallet.Credit(50);
            Assert.AreEqual(120, wallet.Balance);
            Assert.AreEqual(70L, lastOld);
            Assert.AreEqual(120L, lastNew);
        }

        [Test]
        public void Wallet_CannotGoNegative()
        {
            var wallet = new Wallet(20);

            Assert.IsFalse(wallet.TryDebit(21));
            Assert.AreEqual(20, wallet.Balance);
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TryDebit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Credit(-5));
        }

        [Test]
        public void BetSelector_StepsWithinBounds()
        {
            var bets = new BetSelector(new[] { 10, 20, 50, 100 });

            Assert.AreEqual(10, bets.CurrentBet);
            Assert.IsFalse(bets.Decrease());
            Assert.IsTrue(bets.Increase());
            Assert.IsTrue(bets.Increase());
            Assert.IsTrue(bets.Increase());
            Assert.AreEqual(100, bets.CurrentBet);
            Assert.IsFalse(bets.Increase());
        }

        [Test]
        public void BetSelector_FitToBalance_LowersToAffordableBet()
        {
            var bets = new BetSelector(new[] { 10, 20, 50, 100 }, startIndex: 3);

            Assert.IsTrue(bets.FitToBalance(35));
            Assert.AreEqual(20, bets.CurrentBet);
            Assert.IsFalse(bets.FitToBalance(5));
        }

        [Test]
        public void BetSelector_SelectHighestAffordable_PicksLargestCoveredBet()
        {
            var bets = new BetSelector(new[] { 10, 20, 50, 100 });

            Assert.IsTrue(bets.SelectHighestAffordable(75));
            Assert.AreEqual(50, bets.CurrentBet);

            Assert.IsFalse(bets.SelectHighestAffordable(75), "Already at max affordable: no change.");
            Assert.IsTrue(bets.SelectHighestAffordable(1000));
            Assert.AreEqual(100, bets.CurrentBet);

            Assert.IsFalse(bets.SelectHighestAffordable(5), "Nothing affordable: selection is kept.");
            Assert.AreEqual(100, bets.CurrentBet);
            Assert.AreEqual(-1, bets.HighestAffordableIndex(5));
        }

        [Test]
        public void BetSelector_RejectsInvalidOptions()
        {
            Assert.Throws<ArgumentException>(() => new BetSelector(new int[0]));
            Assert.Throws<ArgumentException>(() => new BetSelector(new[] { 10, 10 }));
            Assert.Throws<ArgumentException>(() => new BetSelector(new[] { 0, 10 }));
        }
    }
}
