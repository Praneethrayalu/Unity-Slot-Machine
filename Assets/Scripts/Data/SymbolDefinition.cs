using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Designer-facing description of one reel symbol: its stable id, art and display name.
    /// </summary>
    [CreateAssetMenu(fileName = "Symbol", menuName = "Slot Game/Symbol Definition", order = 0)]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [Tooltip("Stable numeric id used by the game maths. Must be unique inside the SymbolDatabase.")]
        [SerializeField] private int id;

        [SerializeField] private string displayName = "Symbol";

        [Tooltip("Sharp sprite shown when the reel is slow or stopped.")]
        [SerializeField] private Sprite sprite;

        [Tooltip("Vertically motion-blurred version shown while the reel spins at full speed.")]
        [SerializeField] private Sprite blurredSprite;

        [Tooltip("Colour used for this symbol's win glow and paytable accents.")]
        [SerializeField] private Color accentColor = Color.white;

        public int Id => id;

        public string DisplayName => displayName;

        public Sprite Sprite => sprite;

        /// <summary>Falls back to the sharp sprite when no blurred variant was provided.</summary>
        public Sprite BlurredSprite => blurredSprite != null ? blurredSprite : sprite;

        public Color AccentColor => accentColor;

#if UNITY_EDITOR
        /// <summary>Editor-only initialiser used by the project builder.</summary>
        public void EditorInitialize(int newId, string newName, Sprite newSprite, Sprite newBlurred, Color accent)
        {
            id = newId;
            displayName = newName;
            sprite = newSprite;
            blurredSprite = newBlurred;
            accentColor = accent;
        }
#endif
    }
}
