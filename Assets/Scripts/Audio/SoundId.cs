namespace SlotGame.Audio
{
    /// <summary>Every sound effect the game can request. Mapped to clips by <see cref="SoundLibrary"/>.</summary>
    public enum SoundId
    {
        ButtonClick = 0,
        LeverPull = 1,
        ReelSpinLoop = 2,
        ReelStop = 3,
        Anticipation = 4,
        WinSmall = 5,
        WinBig = 6,
        Jackpot = 7,
        CoinTick = 8,
        FreeSpins = 9,
        NoCredits = 10,
        BetChange = 11,
    }
}
