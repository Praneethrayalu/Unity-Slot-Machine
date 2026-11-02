namespace SlotGame.Gameplay
{
    /// <summary>High-level state of the machine, used by the UI to enable/disable controls.</summary>
    public enum SlotState
    {
        /// <summary>Waiting for the player.</summary>
        Idle,

        /// <summary>Reels are moving.</summary>
        Spinning,

        /// <summary>A win is being presented (counter, popup, highlights).</summary>
        Celebrating,
    }
}
