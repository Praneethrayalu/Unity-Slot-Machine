namespace SlotGame.Gameplay
{
    /// <summary>How loudly a win should be celebrated (ordered: compare with &gt;= for "at least").</summary>
    public enum WinTier
    {
        None,

        /// <summary>In-machine "WIN!" banner and a short count-up.</summary>
        Small,

        /// <summary>BIG WIN popup.</summary>
        Big,

        /// <summary>MEGA WIN popup: more coins, longer show.</summary>
        Mega,

        /// <summary>A line of wilds (7-7-7): the top prize.</summary>
        Jackpot,
    }
}
