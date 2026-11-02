namespace SlotGame.Core.Game
{
    /// <summary>Why <see cref="SlotEngine.TryBeginSpin"/> refused to start a spin.</summary>
    public enum SpinRejection
    {
        None = 0,

        /// <summary>The previous spin has not been settled yet.</summary>
        SpinInProgress,

        /// <summary>The wallet cannot cover the current bet.</summary>
        InsufficientCredits,
    }
}
