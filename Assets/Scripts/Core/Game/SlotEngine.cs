using System;
using SlotGame.Core.Economy;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;

namespace SlotGame.Core.Game
{
    /// <summary>
    /// Pure C# game rules: takes the bet, rolls the reels, evaluates the line and pays out.
    /// <para>
    /// A spin happens in two steps so the presentation can stay honest and dramatic at once:
    /// <list type="number">
    /// <item><see cref="TryBeginSpin"/> takes the bet and decides the full outcome immediately.</item>
    /// <item><see cref="SettleSpin"/> pays the win and awards free spins once the reels have stopped,
    /// so the balance never "spoils" the result before the player sees it.</item>
    /// </list>
    /// Having no Unity dependency means the exact same code runs in the game, in edit-mode tests
    /// and in the million-spin RTP simulator.
    /// </para>
    /// </summary>
    public sealed class SlotEngine
    {
        private readonly ReelSet _reels;
        private readonly WinEvaluator _evaluator;
        private readonly IRandomProvider _random;

        private SpinResult _pendingSpin;
        private int _freeSpinBet;

        public SlotEngine(ReelSet reels, IPayTable payTable, IRandomProvider random, Wallet wallet, BetSelector betSelector)
        {
            _reels = reels ?? throw new ArgumentNullException(nameof(reels));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Bets = betSelector ?? throw new ArgumentNullException(nameof(betSelector));
            _evaluator = new WinEvaluator(payTable ?? throw new ArgumentNullException(nameof(payTable)));
        }

        /// <summary>Raised when the number of remaining free spins changes.</summary>
        public event Action<int> FreeSpinsChanged;

        public Wallet Wallet { get; }

        public BetSelector Bets { get; }

        public ReelSet Reels => _reels;

        public WinEvaluator Evaluator => _evaluator;

        public int FreeSpinsRemaining { get; private set; }

        public bool IsSpinPending => _pendingSpin != null;

        public int SpinsPlayed { get; private set; }

        /// <summary>The bet the next spin will be paid against.</summary>
        public int NextSpinBet => FreeSpinsRemaining > 0 ? _freeSpinBet : Bets.CurrentBet;

        /// <summary>True when the next spin can start (free spin available or bet affordable).</summary>
        public bool CanSpin => !IsSpinPending && (FreeSpinsRemaining > 0 || Wallet.CanAfford(Bets.CurrentBet));

        /// <summary>
        /// Takes the bet (or consumes a free spin) and decides the outcome.
        /// Nothing is paid until <see cref="SettleSpin"/> is called.
        /// </summary>
        public bool TryBeginSpin(out SpinResult result, out SpinRejection rejection)
        {
            result = null;

            if (IsSpinPending)
            {
                rejection = SpinRejection.SpinInProgress;
                return false;
            }

            bool isFreeSpin = FreeSpinsRemaining > 0;
            int bet;

            if (isFreeSpin)
            {
                bet = _freeSpinBet;
                SetFreeSpins(FreeSpinsRemaining - 1);
            }
            else
            {
                bet = Bets.CurrentBet;
                if (!Wallet.TryDebit(bet))
                {
                    rejection = SpinRejection.InsufficientCredits;
                    return false;
                }
            }

            // The whole outcome is decided here, by the RNG, before any animation runs.
            int[] stops = _reels.RollStops(_random);
            int[] line = _reels.GetLineSymbols(stops);
            WinResult win = _evaluator.Evaluate(line, bet);

            SpinsPlayed++;
            _pendingSpin = new SpinResult(SpinsPlayed, bet, isFreeSpin, stops, line, win);
            result = _pendingSpin;
            rejection = SpinRejection.None;
            return true;
        }

        /// <summary>Pays the pending spin's win and awards any free spins.</summary>
        public void SettleSpin(SpinResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!ReferenceEquals(result, _pendingSpin))
            {
                throw new InvalidOperationException("Only the pending spin can be settled, and only once.");
            }

            _pendingSpin = null;

            if (!result.Win.IsWin)
            {
                return;
            }

            Wallet.Credit(result.Win.Payout);

            if (result.Win.FreeSpinsAwarded > 0)
            {
                // Free spins are played at the bet that triggered them (retriggers keep that bet).
                if (FreeSpinsRemaining == 0)
                {
                    _freeSpinBet = result.Bet;
                }

                SetFreeSpins(FreeSpinsRemaining + result.Win.FreeSpinsAwarded);
            }
        }

        private void SetFreeSpins(int value)
        {
            if (FreeSpinsRemaining == value)
            {
                return;
            }

            FreeSpinsRemaining = value;
            FreeSpinsChanged?.Invoke(value);
        }
    }
}
