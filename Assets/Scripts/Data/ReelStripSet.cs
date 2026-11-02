using System;
using System.Collections.Generic;
using SlotGame.Core.Reels;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>The ordered symbols printed on one physical reel strip.</summary>
    [Serializable]
    public sealed class ReelStrip
    {
        [SerializeField] private List<SymbolDefinition> stops = new List<SymbolDefinition>();

        public ReelStrip(IEnumerable<SymbolDefinition> symbols)
        {
            stops = new List<SymbolDefinition>(symbols);
        }

        public IReadOnlyList<SymbolDefinition> Stops => stops;
    }

    /// <summary>
    /// The reel strips that define the game's odds. Symbol frequency on each strip is the
    /// probability model: e.g. 2 sevens on a 20-stop strip = 10% per reel.
    /// </summary>
    [CreateAssetMenu(fileName = "ReelStripSet", menuName = "Slot Game/Reel Strip Set", order = 3)]
    public sealed class ReelStripSet : ScriptableObject
    {
        [SerializeField] private List<ReelStrip> reels = new List<ReelStrip>();

        public IReadOnlyList<ReelStrip> Reels => reels;

        /// <summary>Converts the asset into the engine's id-based <see cref="ReelSet"/>.</summary>
        public ReelSet CreateReelSet()
        {
            var strips = new List<IReadOnlyList<int>>(reels.Count);
            foreach (ReelStrip reel in reels)
            {
                var ids = new List<int>(reel.Stops.Count);
                foreach (SymbolDefinition symbol in reel.Stops)
                {
                    if (symbol == null)
                    {
                        throw new InvalidOperationException($"{name}: a reel strip contains an empty slot.");
                    }

                    ids.Add(symbol.Id);
                }

                strips.Add(ids);
            }

            return new ReelSet(strips);
        }

#if UNITY_EDITOR
        public void EditorSetReels(IEnumerable<ReelStrip> newReels)
        {
            reels = new List<ReelStrip>(newReels);
        }
#endif
    }
}
