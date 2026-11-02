namespace SlotGame.Core.Rules
{
    /// <summary>
    /// Read-only view of the game's pay rules. Implemented by the <c>PayTable</c> ScriptableObject
    /// in the game layer and by lightweight fakes in unit tests, so the win logic never depends
    /// on Unity assets.
    /// </summary>
    public interface IPayTable
    {
        /// <summary>Bet multiplier paid when the whole line shows <paramref name="symbolId"/> (0 = pays nothing).</summary>
        int GetPayoutMultiplier(int symbolId);

        /// <summary>Free spins awarded when the whole line shows <paramref name="symbolId"/> (0 = none).</summary>
        int GetFreeSpinsAwarded(int symbolId);

        /// <summary>True when <paramref name="symbolId"/> substitutes for any other symbol.</summary>
        bool IsWild(int symbolId);
    }
}
