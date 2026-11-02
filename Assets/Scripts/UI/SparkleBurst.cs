using System.Collections.Generic;
using SlotGame.Core.Game;
using SlotGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Twinkling star sparkles over the reels when a spin wins. Like <see cref="CoinBurst"/>, it
    /// uses pooled UI Images so it layers correctly inside the overlay canvas. Each sparkle grows,
    /// spins and fades on a sine envelope; more sparkles for bigger wins.
    /// </summary>
    public sealed class SparkleBurst : MonoBehaviour
    {
        private struct Sparkle
        {
            public Image Image;
            public float Delay;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Spin;
        }

        [SerializeField] private SlotMachineController controller;
        [SerializeField] private Sprite sparkleSprite;
        [Tooltip("Sparkles appear at random points inside this rect.")]
        [SerializeField] private RectTransform area;
        [SerializeField] private int poolSize = 60;
        [SerializeField] private Vector2 sizeRange = new Vector2(18f, 46f);
        [SerializeField] private Vector2 lifeRange = new Vector2(0.45f, 0.9f);

        [Header("Sparkles per tier")]
        [SerializeField] private int smallWinCount = 14;
        [SerializeField] private int bigWinCount = 30;
        [SerializeField] private int jackpotCount = 60;
        [Tooltip("Sparkles are spread over this many seconds.")]
        [SerializeField] private float emitWindow = 1.4f;

        private static readonly Color[] Palette =
        {
            new Color(1f, 1f, 1f),
            new Color(1f, 0.9f, 0.45f),
            new Color(1f, 0.75f, 0.3f),
        };

        private readonly List<Sparkle> _active = new List<Sparkle>();
        private readonly Stack<Image> _pool = new Stack<Image>();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(area, false);
                var image = go.GetComponent<Image>();
                image.sprite = sparkleSprite;
                image.raycastTarget = false;
                go.SetActive(false);
                _pool.Push(image);
            }
        }

        private void OnEnable()
        {
            controller.SpinCompleted += OnSpinCompleted;
            controller.SpinStarted += OnSpinStarted;
        }

        private void OnDisable()
        {
            controller.SpinCompleted -= OnSpinCompleted;
            controller.SpinStarted -= OnSpinStarted;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            Clear();
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            switch (tier)
            {
                case WinTier.Small: Emit(smallWinCount); break;
                case WinTier.Big: Emit(bigWinCount); break;
                case WinTier.Jackpot: Emit(jackpotCount); break;
            }
        }

        /// <summary>Queues <paramref name="count"/> sparkles spread over the emit window.</summary>
        public void Emit(int count)
        {
            Rect rect = area.rect;
            for (int i = 0; i < count && _pool.Count > 0; i++)
            {
                Image image = _pool.Pop();
                image.gameObject.SetActive(true);
                image.color = Palette[Random.Range(0, Palette.Length)];
                image.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));
                image.rectTransform.localScale = Vector3.zero;

                _active.Add(new Sparkle
                {
                    Image = image,
                    Delay = Random.Range(0f, emitWindow),
                    MaxLife = Random.Range(lifeRange.x, lifeRange.y),
                    Size = Random.Range(sizeRange.x, sizeRange.y),
                    Spin = Random.Range(-180f, 180f),
                });
            }
        }

        public void Clear()
        {
            foreach (Sparkle sparkle in _active)
            {
                Release(sparkle.Image);
            }

            _active.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Sparkle s = _active[i];
                if (s.Delay > 0f)
                {
                    s.Delay -= dt;
                    _active[i] = s;
                    continue;
                }

                s.Life += dt;
                if (s.Life >= s.MaxLife)
                {
                    Release(s.Image);
                    _active.RemoveAt(i);
                    continue;
                }

                // Sine envelope: pops in, peaks mid-life, shrinks away.
                float envelope = Mathf.Sin(s.Life / s.MaxLife * Mathf.PI);
                RectTransform rt = s.Image.rectTransform;
                rt.sizeDelta = new Vector2(s.Size, s.Size);
                rt.localScale = Vector3.one * envelope;
                rt.localRotation = Quaternion.Euler(0f, 0f, s.Spin * s.Life);
                _active[i] = s;
            }
        }

        private void Release(Image image)
        {
            image.gameObject.SetActive(false);
            _pool.Push(image);
        }
    }
}
