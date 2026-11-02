using System;
using NUnit.Framework;
using SlotGame.Core.Rules;
using static SlotGame.Tests.FakePayTable;

namespace SlotGame.Tests
{
    public sealed class WinEvaluatorTests
    {
        private WinEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new WinEvaluator(new FakePayTable());
        }

        [TestCase(Cherry, 2)]
        [TestCase(Bell, 5)]
        [TestCase(Bar, 10)]
        [TestCase(Seven, 100)]
        public void ThreeOfAKind_PaysBetTimesMultiplier(int symbol, int multiplier)
        {
            WinResult result = _evaluator.Evaluate(new[] { symbol, symbol, symbol }, 10);

            Assert.IsTrue(result.IsWin);
            Assert.AreEqual(symbol, result.SymbolId);
            Assert.AreEqual(multiplier, result.Multiplier);
            Assert.AreEqual(10L * multiplier, result.Payout);
            Assert.IsFalse(result.IncludesWild);
        }

        [TestCase(Cherry, Bell, Bar)]
        [TestCase(Cherry, Cherry, Bar)]
        [TestCase(Bar, Cherry, Cherry)]
        [TestCase(Bell, Bar, Bell)]
        public void MixedSymbols_DoNotWin(int a, int b, int c)
        {
            WinResult result = _evaluator.Evaluate(new[] { a, b, c }, 10);

            Assert.IsFalse(result.IsWin);
            Assert.AreEqual(0, result.Payout);
        }

        [TestCase(Seven, Bar, Bar)]
        [TestCase(Bar, Seven, Bar)]
        [TestCase(Bar, Bar, Seven)]
        [TestCase(Seven, Seven, Bar)]
        public void Wild_SubstitutesForMissingSymbol(int a, int b, int c)
        {
            WinResult result = _evaluator.Evaluate(new[] { a, b, c }, 20);

            Assert.IsTrue(result.IsWin);
            Assert.AreEqual(Bar, result.SymbolId);
            Assert.AreEqual(200, result.Payout);
            Assert.IsTrue(result.IncludesWild);
        }

        [Test]
        public void Wild_CannotJoinTwoDifferentSymbols()
        {
            Assert.IsFalse(_evaluator.Evaluate(new[] { Bar, Seven, Bell }, 10).IsWin);
        }

        [Test]
        public void WildDisabled_RequiresExactMatch()
        {
            var strict = new WinEvaluator(new FakePayTable(wildEnabled: false));

            Assert.IsFalse(strict.Evaluate(new[] { Seven, Bar, Bar }, 10).IsWin);
            Assert.IsTrue(strict.Evaluate(new[] { Seven, Seven, Seven }, 10).IsWin);
        }

        [Test]
        public void Bells_AwardFreeSpins()
        {
            WinResult result = _evaluator.Evaluate(new[] { Bell, Bell, Seven }, 10);

            Assert.AreEqual(3, result.FreeSpinsAwarded);
        }

        [Test]
        public void MatchingUnpaidSymbol_IsNotAWin()
        {
            Assert.IsFalse(_evaluator.Evaluate(new[] { Unpaid, Unpaid, Unpaid }, 10).IsWin);
        }

        [Test]
        public void CanStillWin_DetectsAnticipationCases()
        {
            Assert.IsTrue(_evaluator.CanStillWin(new[] { Bar, Bar }));
            Assert.IsTrue(_evaluator.CanStillWin(new[] { Seven, Cherry }));
            Assert.IsFalse(_evaluator.CanStillWin(new[] { Bar, Cherry }));
        }

        [Test]
        public void InvalidInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _evaluator.Evaluate(null, 10));
            Assert.Throws<ArgumentException>(() => _evaluator.Evaluate(new int[0], 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => _evaluator.Evaluate(new[] { Bar, Bar, Bar }, -1));
        }
    }
}
