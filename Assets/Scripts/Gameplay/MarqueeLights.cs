using SlotGame.Core.Game;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// The chasing bulb marquee around the cabinet. Bulbs are plain UI Images (ordered around the
    /// cabinet outline); this component only decides which are lit each frame, so the whole show
    /// costs one cheap loop and no Animator.
    /// <list type="bullet">
    /// <item>Idle: a slow "attract mode" chase.</item>
    /// <item>Spinning: a fast chase.</item>
    /// <item>Win: odd/even bulbs flash alternately.</item>
    /// <item>Jackpot: a rainbow sweep, then back to the attract chase.</item>
    /// </list>
    /// </summary>
    public sealed class MarqueeLights : MonoBehaviour
    {
        private enum Mode
        {
            Attract,
            Spinning,
            Win,
            Jackpot,
        }

        [SerializeField] private SlotMachineController controller;
        [SerializeField] private Image[] bulbs = new Image[0];

        [Header("Colours")]
        [SerializeField] private Color litColor = new Color(1f, 0.93f, 0.62f, 1f);
        [SerializeField] private Color unlitColor = new Color(0.62f, 0.36f, 0.12f, 0.55f);

        [Header("Timing")]
        [Tooltip("Bulbs advanced per second in attract mode.")]
        [SerializeField] private float attractSpeed = 7f;
        [Tooltip("Bulbs advanced per second while the reels spin.")]
        [SerializeField] private float spinSpeed = 22f;
        [Tooltip("Every Nth bulb is lit in a chase.")]
        [SerializeField] private int chaseSpacing = 4;
        [SerializeField] private float winDuration = 2.4f;
        [SerializeField] private float jackpotDuration = 5f;

        private Mode _mode = Mode.Attract;
        private float _modeUntil;
        private Vector3[] _baseScales;

        private void Awake()
        {
            _baseScales = new Vector3[bulbs.Length];
            for (int i = 0; i < bulbs.Length; i++)
            {
                _baseScales[i] = bulbs[i].rectTransform.localScale;
            }
        }

        private void OnEnable()
        {
            controller.SpinStarted += OnSpinStarted;
            controller.SpinCompleted += OnSpinCompleted;
        }

        private void OnDisable()
        {
            controller.SpinStarted -= OnSpinStarted;
            controller.SpinCompleted -= OnSpinCompleted;
        }

        private void OnSpinStarted(SpinResult spin)
        {
            _mode = Mode.Spinning;
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            switch (tier)
            {
                case WinTier.None:
                    _mode = Mode.Attract;
                    break;
                case WinTier.Jackpot:
                    _mode = Mode.Jackpot;
                    _modeUntil = Time.time + jackpotDuration;
                    break;
                default:
                    _mode = Mode.Win;
                    _modeUntil = Time.time + (tier == WinTier.Big ? winDuration * 1.5f : winDuration);
                    break;
            }
        }

        private void Update()
        {
            if ((_mode == Mode.Win || _mode == Mode.Jackpot) && Time.time >= _modeUntil)
            {
                _mode = Mode.Attract;
            }

            float t = Time.time;
            int count = bulbs.Length;
            for (int i = 0; i < count; i++)
            {
                bool lit;
                Color on = litColor;
                switch (_mode)
                {
                    case Mode.Spinning:
                        lit = Chase(i, t * spinSpeed);
                        break;
                    case Mode.Win:
                        // Alternate halves of the marquee 6 times a second.
                        lit = ((i + Mathf.FloorToInt(t * 6f)) & 1) == 0;
                        break;
                    case Mode.Jackpot:
                        // Every bulb on, hue sweeping around the cabinet.
                        lit = true;
                        on = Color.HSVToRGB(Mathf.Repeat(t * 0.9f - i / (float)count, 1f), 0.55f, 1f);
                        break;
                    default:
                        lit = Chase(i, t * attractSpeed);
                        break;
                }

                Image bulb = bulbs[i];
                bulb.color = lit ? on : unlitColor;
                bulb.rectTransform.localScale = lit ? _baseScales[i] * 1.12f : _baseScales[i];
            }
        }

        /// <summary>True for every <see cref="chaseSpacing"/>th bulb, offset by the moving head.</summary>
        private bool Chase(int index, float head)
        {
            int spacing = Mathf.Max(2, chaseSpacing);
            return ((index - Mathf.FloorToInt(head)) % spacing + spacing) % spacing == 0;
        }
    }
}
