using System;
using System.Collections.Generic;

namespace SlotGame.Core.Economy
{
    /// <summary>
    /// Steps through a fixed, ascending list of allowed bet sizes.
    /// </summary>
    public sealed class BetSelector
    {
        private readonly int[] _options;

        public BetSelector(IReadOnlyList<int> options, int startIndex = 0)
        {
            if (options == null || options.Count == 0)
            {
                throw new ArgumentException("At least one bet option is required.", nameof(options));
            }

            _options = new int[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] <= 0)
                {
                    throw new ArgumentException("Bet options must be positive.", nameof(options));
                }

                if (i > 0 && options[i] <= options[i - 1])
                {
                    throw new ArgumentException("Bet options must be strictly ascending.", nameof(options));
                }

                _options[i] = options[i];
            }

            Index = Clamp(startIndex);
        }

        /// <summary>Raised with the new bet whenever the selection changes.</summary>
        public event Action<int> BetChanged;

        public int Index { get; private set; }

        public int CurrentBet => _options[Index];

        public IReadOnlyList<int> Options => _options;

        public bool CanIncrease => Index < _options.Length - 1;

        public bool CanDecrease => Index > 0;

        public bool Increase()
        {
            return Select(Index + 1);
        }

        public bool Decrease()
        {
            return Select(Index - 1);
        }

        /// <summary>
        /// Lowers the bet to the largest option the player can still afford.
        /// Returns false when not even the smallest bet is affordable.
        /// </summary>
        public bool FitToBalance(long balance)
        {
            for (int i = Index; i >= 0; i--)
            {
                if (_options[i] <= balance)
                {
                    Select(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// MAX BET: selects the largest option the player can afford.
        /// Returns false when nothing changed (already at that bet, or no option is affordable).
        /// </summary>
        public bool SelectHighestAffordable(long balance)
        {
            for (int i = _options.Length - 1; i >= 0; i--)
            {
                if (_options[i] <= balance)
                {
                    return Select(i);
                }
            }

            return false;
        }

        /// <summary>Index of the largest affordable option, or -1 when none is affordable.</summary>
        public int HighestAffordableIndex(long balance)
        {
            for (int i = _options.Length - 1; i >= 0; i--)
            {
                if (_options[i] <= balance)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Select(int index)
        {
            if (index < 0 || index >= _options.Length || index == Index)
            {
                return false;
            }

            Index = index;
            BetChanged?.Invoke(CurrentBet);
            return true;
        }

        private int Clamp(int index)
        {
            return Math.Max(0, Math.Min(_options.Length - 1, index));
        }
    }
}
