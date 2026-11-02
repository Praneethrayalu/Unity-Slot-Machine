using System.Collections.Generic;
using SlotGame.Core.Rules;

namespace SlotGame.Tests
{
    /// <summary>In-memory pay table so rule tests do not depend on Unity assets.</summary>
    internal sealed class FakePayTable : IPayTable
    {
        public const int Seven = 1;
        public const int Cherry = 2;
        public const int Bell = 3;
        public const int Bar = 4;
        public const int Unpaid = 9;

        private readonly Dictionary<int, int> _multipliers = new Dictionary<int, int>
        {
            { Seven, 100 }, { Bar, 10 }, { Bell, 5 }, { Cherry, 2 },
        };

        private readonly Dictionary<int, int> _freeSpins = new Dictionary<int, int> { { Bell, 3 } };

        public FakePayTable(bool wildEnabled = true)
        {
            WildEnabled = wildEnabled;
        }

        public bool WildEnabled { get; }

        public int GetPayoutMultiplier(int symbolId) => _multipliers.TryGetValue(symbolId, out int m) ? m : 0;

        public int GetFreeSpinsAwarded(int symbolId) => _freeSpins.TryGetValue(symbolId, out int f) ? f : 0;

        public bool IsWild(int symbolId) => WildEnabled && symbolId == Seven;
    }
}
