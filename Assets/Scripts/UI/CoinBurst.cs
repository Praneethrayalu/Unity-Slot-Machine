using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Lightweight UI "particle" fountain of coins for win celebrations.
    /// <para>
    /// Implemented with pooled UI Images instead of a ParticleSystem so it renders inside the
    /// Screen Space - Overlay canvas, sorts correctly with the popup and costs no extra camera.
    /// </para>
    /// </summary>
    public sealed class CoinBurst : MonoBehaviour
    {
        private struct Coin
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Spin;
            public float Life;
            public float MaxLife;
        }

        [SerializeField] private Sprite coinSprite;
        [SerializeField] private int poolSize = 80;
        [SerializeField] private Vector2 coinSize = new Vector2(64f, 64f);
        [SerializeField] private float gravity = -2600f;
        [SerializeField] private Vector2 speedRange = new Vector2(900f, 1800f);
        [SerializeField] private float spreadDegrees = 70f;
        [SerializeField] private Vector2 lifeRange = new Vector2(1.2f, 2f);

        private readonly List<Coin> _active = new List<Coin>();
        private readonly Stack<Image> _pool = new Stack<Image>();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                _pool.Push(CreateCoin(i));
            }
        }

        /// <summary>Launches <paramref name="count"/> coins upwards from this object's position.</summary>
        public void Burst(int count)
        {
            for (int i = 0; i < count && _pool.Count > 0; i++)
            {
                Image image = _pool.Pop();
                image.gameObject.SetActive(true);

                float angle = (90f + Random.Range(-spreadDegrees, spreadDegrees)) * Mathf.Deg2Rad;
                float speed = Random.Range(speedRange.x, speedRange.y);
                var coin = new Coin
                {
                    Rect = image.rectTransform,
                    Image = image,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Spin = Random.Range(-540f, 540f),
                    Life = 0f,
                    MaxLife = Random.Range(lifeRange.x, lifeRange.y),
                };

                coin.Rect.anchoredPosition = Vector2.zero;
                coin.Rect.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
                coin.Image.color = Color.white;
                _active.Add(coin);
            }
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Recycle(i);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Coin coin = _active[i];
                coin.Life += dt;
                coin.Velocity.y += gravity * dt;
                coin.Rect.anchoredPosition += coin.Velocity * dt;
                coin.Rect.Rotate(0f, 0f, coin.Spin * dt);

                // Fake 3D flip: squash the coin horizontally on a sine to look like it tumbles.
                float flip = Mathf.Abs(Mathf.Cos(coin.Life * 9f + i));
                Vector3 scale = coin.Rect.localScale;
                coin.Rect.localScale = new Vector3(Mathf.Max(0.15f, flip) * Mathf.Abs(scale.y), scale.y, 1f);

                float fade = 1f - Mathf.Clamp01((coin.Life - coin.MaxLife * 0.7f) / (coin.MaxLife * 0.3f));
                coin.Image.color = new Color(1f, 1f, 1f, fade);

                _active[i] = coin;
                if (coin.Life >= coin.MaxLife)
                {
                    Recycle(i);
                }
            }
        }

        private void Recycle(int index)
        {
            Coin coin = _active[index];
            coin.Image.gameObject.SetActive(false);
            _pool.Push(coin.Image);
            _active.RemoveAt(index);
        }

        private Image CreateCoin(int index)
        {
            var go = new GameObject($"Coin {index}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = coinSprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = coinSize;
            go.SetActive(false);
            return image;
        }
    }
}
