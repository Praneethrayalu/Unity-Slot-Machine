using System;
using System.Collections.Generic;
using SlotGame.Core.Rules;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>One row of the pay table: three of <see cref="Symbol"/> pays bet x <see cref="Multiplier"/>.</summary>
    [Serializable]
    public sealed class PayTableEntry
    {
        [SerializeField] private SymbolDefinition symbol;

        [Tooltip("Bet multiplier paid for a full line of this symbol.")]
        [Min(0)] [SerializeField] private int multiplier;

        [Tooltip("Free spins awarded for a full line of this symbol (0 = none).")]
        [Min(0)] [SerializeField] private int freeSpins;

        public PayTableEntry(SymbolDefinition symbol, int multiplier, int freeSpins)
        {
            this.symbol = symbol;
            this.multiplier = multiplier;
            this.freeSpins = freeSpins;
        }

        public SymbolDefinition Symbol => symbol;

        public int Multiplier => multiplier;

        public int FreeSpins => freeSpins;
    }

    /// <summary>
    /// Designer-editable pay rules. Implements <see cref="IPayTable"/> so the pure C# win logic can
    /// read it without knowing it is a Unity asset.
    /// </summary>
    [CreateAssetMenu(fileName = "PayTable", menuName = "Slot Game/Pay Table", order = 2)]
    public sealed class PayTable : ScriptableObject, IPayTable
    {
        [SerializeField] private List<PayTableEntry> entries = new List<PayTableEntry>();

        [Header("Wild")]
        [Tooltip("When enabled, the wild symbol substitutes for any other symbol on the pay line.")]
        [SerializeField] private bool wildEnabled = true;

        [SerializeField] private SymbolDefinition wildSymbol;

        private Dictionary<int, PayTableEntry> _lookup;

        public IReadOnlyList<PayTableEntry> Entries => entries;

        public bool WildEnabled => wildEnabled;

        public SymbolDefinition WildSymbol => wildSymbol;

        public int GetPayoutMultiplier(int symbolId)
        {
            return TryGetEntry(symbolId, out PayTableEntry entry) ? entry.Multiplier : 0;
        }

        public int GetFreeSpinsAwarded(int symbolId)
        {
            return TryGetEntry(symbolId, out PayTableEntry entry) ? entry.FreeSpins : 0;
        }

        public bool IsWild(int symbolId)
        {
            return wildEnabled && wildSymbol != null && wildSymbol.Id == symbolId;
        }

        public bool TryGetEntry(int symbolId, out PayTableEntry entry)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<int, PayTableEntry>();
                foreach (PayTableEntry row in entries)
                {
                    if (row?.Symbol != null)
                    {
                        _lookup[row.Symbol.Id] = row;
                    }
                }
            }

            return _lookup.TryGetValue(symbolId, out entry);
        }

        private void OnValidate()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        public void EditorInitialize(IEnumerable<PayTableEntry> newEntries, SymbolDefinition wild, bool enableWild)
        {
            entries = new List<PayTableEntry>(newEntries);
            wildSymbol = wild;
            wildEnabled = enableWild;
            _lookup = null;
        }
#endif
    }
}
