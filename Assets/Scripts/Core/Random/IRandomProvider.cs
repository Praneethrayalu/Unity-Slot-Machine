namespace SlotGame.Core.Random
{
    /// <summary>
    /// Abstraction over a source of random integers.
    /// <para>
    /// Every random decision in the game (reel stop positions) goes through this interface,
    /// which lets production code use a cryptographically secure generator while tests and
    /// replays inject a deterministic, seeded implementation.
    /// </para>
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a uniformly distributed integer in the range [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
        /// </summary>
        int Next(int minInclusive, int maxExclusive);
    }
}
