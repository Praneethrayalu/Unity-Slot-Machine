using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Editor
{
    /// <summary>Small helpers for building uGUI hierarchies from code.</summary>
    internal static class UiFactory
    {
        private const int UiLayer = 5;

        public static GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Rect(this GameObject go)
        {
            return (RectTransform)go.transform;
        }

        /// <summary>Centre-anchored placement in the parent's local units.</summary>
        public static RectTransform Place(this RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static RectTransform Stretch(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image AddImage(GameObject go, Sprite sprite, Color color, bool raycastTarget = false,
            Image.Type type = Image.Type.Simple, float pixelsPerUnitMultiplier = 1f)
        {
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = type;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color,
            float x, float y, float width, float height, Image.Type type = Image.Type.Simple, float ppuMultiplier = 1f)
        {
            GameObject go = Create(name, parent);
            go.Rect().Place(x, y, width, height);
            return AddImage(go, sprite, color, false, type, ppuMultiplier);
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font,
            Material material, float size, Color color, float x, float y, float width, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = Create(name, parent);
            go.Rect().Place(x, y, width, height);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSharedMaterial = material;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.enableAutoSizing = false;

            // Also write the serialized fields directly. In batch mode TMP's default-font
            // initialisation could override the property setters, so every label silently fell
            // back to LiberationSans; the builder now verifies this before saving the scene.
            EditorBindings.Set(label, "m_fontAsset", font);
            EditorBindings.Set(label, "m_sharedMaterial", material);
            return label;
        }

        /// <summary>Button whose four states come from the sliced sprite sheets.</summary>
        public static Button SpriteSwapButton(GameObject go, Sprite normal, Sprite highlighted, Sprite pressed, Sprite disabled)
        {
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = AddImage(go, normal, Color.white, true);
            }

            image.sprite = normal;
            image.raycastTarget = true;
            image.preserveAspect = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = pressed,
                selectedSprite = normal,
                disabledSprite = disabled != null ? disabled : normal,
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>Button that tints its graphic for hover / press / disabled feedback.</summary>
        public static Button TintButton(GameObject go, Graphic target)
        {
            target.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 0.82f, 1f),
                pressedColor = new Color(0.78f, 0.72f, 0.6f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }
    }
}
