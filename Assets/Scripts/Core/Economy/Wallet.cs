using System;

namespace SlotGame.Core.Economy
{
    /// <summary>
    /// Holds the player's credits. All balance changes go through this class so the UI can react
    /// to a single <see cref="BalanceChanged"/> event and the balance can never go negative.
    /// </summary>
    public sealed class Wallet
    {
        public Wallet(long startingBalance)
        {
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBalance), "Starting balance cannot be negative.");
            }

            Balance = startingBalance;
        }

        /// <summary>Raised after every change with (previous balance, new balance).</summary>
        public event Action<long, long> BalanceChanged;

        /// <summary>Current credits.</summary>
        public long Balance { get; private set; }

        /// <summary>True when the wallet holds at least <paramref name="amount"/> credits.</summary>
        public bool CanAfford(long amount)
        {
            return amount >= 0 && Balance >= amount;
        }

        /// <summary>
        /// Removes <paramref name="amount"/> credits if the balance allows it.
        /// Returns false (and changes nothing) when funds are insufficient.
        /// </summary>
        public bool TryDebit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Debit amount cannot be negative.");
            }

            if (Balance < amount)
            {
                return false;
            }

            SetBalance(Balance - amount);
            return true;
        }

        /// <summary>Adds <paramref name="amount"/> credits (winnings, refills).</summary>
        public void Credit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount cannot be negative.");
            }

            if (amount == 0)
            {
                return;
            }

            SetBalance(checked(Balance + amount));
        }

        /// <summary>Replaces the balance, e.g. when the player accepts a refill.</summary>
        public void Reset(long balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance), "Balance cannot be negative.");
            }

            SetBalance(balance);
        }

        private void SetBalance(long newBalance)
        {
            long previous = Balance;
            Balance = newBalance;
            if (previous != newBalance)
            {
                BalanceChanged?.Invoke(previous, newBalance);
            }
        }
    }
}
