namespace SlotGame.Core.Rules
{
    /// <summary>
    /// Immutable outcome of evaluating one pay line.
    /// </summary>
    public readonly struct WinResult
    {
        /// <summary>Symbol id used to report "no winning symbol".</summary>
        public const int NoSymbol = -1;

        /// <summary>A losing result.</summary>
        public static readonly WinResult None = new WinResult(false, NoSymbol, 0, 0, 0, false);

        public WinResult(bool isWin, int symbolId, int multiplier, long payout, int freeSpinsAwarded, bool includesWild)
        {
            IsWin = isWin;
            SymbolId = symbolId;
            Multiplier = multiplier;
            Payout = payout;
            FreeSpinsAwarded = freeSpinsAwarded;
            IncludesWild = includesWild;
        }

        /// <summary>True when the line is a winning combination.</summary>
        public bool IsWin { get; }

        /// <summary>The symbol the line counts as (the wild's own id when the line is all wilds).</summary>
        public int SymbolId { get; }

        /// <summary>Bet multiplier from the pay table.</summary>
        public int Multiplier { get; }

        /// <summary>Credits paid: bet x multiplier.</summary>
        public long Payout { get; }

        /// <summary>Free spins awarded by this line.</summary>
        public int FreeSpinsAwarded { get; }

        /// <summary>True when at least one wild substituted to complete the line.</summary>
        public bool IncludesWild { get; }

        public override string ToString()
        {
            return IsWin
                ? $"WIN symbol={SymbolId} x{Multiplier} payout={Payout} freeSpins={FreeSpinsAwarded} wild={IncludesWild}"
                : "NO WIN";
        }
    }
}
