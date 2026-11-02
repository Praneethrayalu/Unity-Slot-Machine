using System;
using System.Collections.Generic;
using SlotGame.Core.Reels;
using SlotGame.Data;
using SlotGame.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Visual reel: scrolls a small pool of <see cref="ReelSymbol"/> cells and lands exactly on the
    /// stop index the engine already decided.
    /// <para>
    /// Motion model (all distances in symbol heights):
    /// <list type="number">
    /// <item><b>Wind-up</b>: a short backwards kick, like a lever being pulled.</item>
    /// <item><b>Acceleration</b>: ease-in up to top speed; sprites switch to motion-blurred art.</item>
    /// <item><b>Spin</b>: constant speed until a stop is requested.</item>
    /// <item><b>Deceleration</b>: cubic ease-out whose start velocity matches the current speed,
    /// travelling a little past the target.</item>
    /// <item><b>Bounce</b>: ease-out-back settle onto the target, giving the mechanical "clunk".</item>
    /// </list>
    /// </para>
    /// <para>
    /// Cell recycling: the reel position <c>s</c> is a continuous number. A cell represents the
    /// integer "virtual index" <c>k</c> and sits at <c>y = (k - s) * spacing</c>. Only five cells exist;
    /// each one always shows the index congruent to it (mod 5) inside the window around <c>s</c>,
    /// so cells wrap from the bottom back to the top seamlessly.
    /// </para>
    /// </summary>
    public sealed class Reel : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            WindUp,
            Accelerating,
            Spinning,
            Decelerating,
            Bouncing,
        }

        [Header("Layout")]
        [SerializeField] private RectTransform symbolContainer;
        [SerializeField] private ReelSymbol symbolPrefab;

        [Tooltip("Distance between symbol centres, in canvas units.")]
        [SerializeField] private float symbolSpacing = 160f;

        [Tooltip("Pooled cells. 5 covers the visible window plus one hidden cell above and below.")]
        [SerializeField] private int cellCount = 5;

        [Header("Motion")]
        [SerializeField] private ReelMotionProfile motion;

        [Header("Anticipation")]
        [Tooltip("Optional glow frame shown while this reel is building suspense.")]
        [SerializeField] private Image anticipationGlow;

        private readonly List<ReelSymbol> _cells = new List<ReelSymbol>();

        private ReelSet _reelSet;
        private SymbolDatabase _symbols;
        private int _reelIndex;

        private Phase _phase = Phase.Idle;
        private float _phaseTime;
        private float _position;        // continuous reel position in symbols
        private float _velocity;        // symbols per second (for blur/stretch)
        private float _phaseStart;      // position at the start of the current phase

        // Mapping of virtual index -> strip index. Before a stop is planned every index uses
        // _spinOffset. When a stop is planned, indices at or above _seamIndex (not yet visible)
        // switch to _targetOffset so the strip lines up exactly with the chosen stop.
        private int _spinOffset;
        private int _targetOffset;
        private int _seamIndex = int.MaxValue;

        private int _targetVirtualIndex;
        private int _targetStop;
        private float _decelerationDistance;
        private float _decelerationDuration;
        private bool _hasPendingStop;
        private bool _pendingQuick;
        private int _pendingStop;
        private bool _anticipating;

        /// <summary>Raised when the reel has fully settled on its target.</summary>
        public event Action<Reel> Stopped;

        public int ReelIndex => _reelIndex;

        public bool IsSpinning => _phase != Phase.Idle;

        /// <summary>Stop index currently under the pay line (valid when idle).</summary>
        public int CurrentStop { get; private set; }

        /// <summary>The cell currently sitting on the pay line.</summary>
        public ReelSymbol PaylineSymbol
        {
            get
            {
                int centre = Mathf.RoundToInt(_position);
                foreach (ReelSymbol cell in _cells)
                {
                    if (cell.VirtualIndex == centre)
                    {
                        return cell;
                    }
                }

                return null;
            }
        }

        public IReadOnlyList<ReelSymbol> Cells => _cells;

        /// <summary>Binds the reel to its strip and shows <paramref name="initialStop"/> on the pay line.</summary>
        public void Initialize(ReelSet reelSet, int reelIndex, SymbolDatabase symbols, int initialStop)
        {
            _reelSet = reelSet ?? throw new ArgumentNullException(nameof(reelSet));
            _symbols = symbols != null ? symbols : throw new ArgumentNullException(nameof(symbols));
            _reelIndex = reelIndex;

            EnsureCells();

            _position = 0f;
            _velocity = 0f;
            _spinOffset = initialStop;
            _seamIndex = int.MaxValue;
            CurrentStop = Wrap(initialStop);
            _phase = Phase.Idle;

            foreach (ReelSymbol cell in _cells)
            {
                cell.VirtualIndex = int.MinValue;
            }

            SetAnticipation(false);
            RefreshCells(force: true);
        }

        /// <summary>Starts the wind-up + acceleration sequence.</summary>
        public void StartSpin()
        {
            if (_phase != Phase.Idle)
            {
                return;
            }

            ClearHighlights();
            _hasPendingStop = false;
            _seamIndex = int.MaxValue;
            EnterPhase(Phase.WindUp);
        }

        /// <summary>
        /// Plans the landing on <paramref name="stopIndex"/>. If the reel is still winding up or
        /// accelerating, the stop is queued and executed once top speed is reached.
        /// </summary>
        public void RequestStop(int stopIndex, bool quick = false)
        {
            if (_phase == Phase.Idle || _phase == Phase.Decelerating || _phase == Phase.Bouncing)
            {
                return;
            }

            _pendingStop = stopIndex;
            _pendingQuick = quick;
            _hasPendingStop = true;

            if (_phase == Phase.Spinning)
            {
                BeginDeceleration();
            }
        }

        /// <summary>Toggles the suspense glow on this reel.</summary>
        public void SetAnticipation(bool active)
        {
            _anticipating = active;
            if (anticipationGlow != null)
            {
                anticipationGlow.enabled = active;
            }
        }

        public void ClearHighlights()
        {
            foreach (ReelSymbol cell in _cells)
            {
                cell.StopHighlight();
                cell.SetDimmed(false);
            }
        }

        private void Update()
        {
            if (_anticipating && anticipationGlow != null)
            {
                Color c = anticipationGlow.color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 14f);
                anticipationGlow.color = c;
            }

            if (_phase == Phase.Idle)
            {
                return;
            }

            // Clamp the step so a long frame hitch (tab switch, GC) cannot teleport the reel.
            // Landing is computed analytically from phase time, so it stays exact either way.
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _phaseTime += dt;
            float previous = _position;

            switch (_phase)
            {
                case Phase.WindUp:
                {
                    float t = _phaseTime / motion.WindUpDuration;
                    _position = _phaseStart - motion.WindUpDistance * Easing.OutQuad(t);
                    if (t >= 1f)
                    {
                        EnterPhase(Phase.Accelerating);
                    }

                    break;
                }

                case Phase.Accelerating:
                {
                    float t = _phaseTime / motion.AccelerationDuration;
                    float speed = motion.MaxSpeed * Easing.InQuad(t);
                    _position += speed * dt;
                    if (t >= 1f)
                    {
                        EnterPhase(Phase.Spinning);
                        if (_hasPendingStop)
                        {
                            BeginDeceleration();
                        }
                    }

                    break;
                }

                case Phase.Spinning:
                    _position += motion.MaxSpeed * dt;
                    break;

                case Phase.Decelerating:
                {
                    float t = _phaseTime / _decelerationDuration;
                    _position = _phaseStart + _decelerationDistance * Easing.OutCubic(t);
                    if (t >= 1f)
                    {
                        _position = _targetVirtualIndex + motion.BounceOvershoot;
                        EnterPhase(Phase.Bouncing);
                    }

                    break;
                }

                case Phase.Bouncing:
                {
                    float t = _phaseTime / motion.BounceDuration;

                    // From target+overshoot back to the target; OutBack dips slightly past it once
                    // more, which reads as a heavy mechanical reel settling.
                    _position = _targetVirtualIndex + motion.BounceOvershoot * (1f - Easing.OutBack(t));
                    if (t >= 1f)
                    {
                        CompleteStop();
                        return;
                    }

                    break;
                }
            }

            _velocity = dt > 0f ? Mathf.Abs(_position - previous) / dt : 0f;
            RefreshCells(force: false);
        }

        private void BeginDeceleration()
        {
            _hasPendingStop = false;

            int centre = Mathf.FloorToInt(_position + 0.5f);

            // The landing index is far enough ahead that it (and its neighbours) are still hidden
            // above the window, so re-mapping them to the target strip position is invisible.
            _targetVirtualIndex = centre + motion.StopDistance;
            _targetStop = Wrap(_pendingStop);
            _targetOffset = _targetStop - _targetVirtualIndex;
            _seamIndex = centre + 3;

            // Travel past the target by the bounce overshoot, then settle back.
            _decelerationDistance = _targetVirtualIndex + motion.BounceOvershoot - _position;

            // For p(t) = d * OutCubic(t / T) the start velocity is 3d / T, so choosing
            // T = 3d / v makes the hand-off from constant speed perfectly smooth.
            float speed = motion.MaxSpeed * (_pendingQuick ? motion.QuickStopSpeedUp : 1f);
            _decelerationDuration = 3f * _decelerationDistance / speed;

            EnterPhase(Phase.Decelerating);
        }

        private void CompleteStop()
        {
            _position = _targetVirtualIndex;
            _velocity = 0f;
            CurrentStop = _targetStop;

            // Re-base so the position stays small (no float precision drift over thousands of
            // spins). Shifting by a multiple of the cell count keeps every cell on the same
            // virtual-index residue, so nothing visibly changes.
            int shift = _targetVirtualIndex - Mod(_targetVirtualIndex, cellCount);
            int rebasedTarget = _targetVirtualIndex - shift;
            _position = rebasedTarget;
            _spinOffset = _targetStop - rebasedTarget;
            _seamIndex = int.MaxValue;
            foreach (ReelSymbol cell in _cells)
            {
                if (cell.VirtualIndex != int.MinValue)
                {
                    cell.VirtualIndex -= shift;
                }
            }

            _phase = Phase.Idle;
            SetAnticipation(false);
            RefreshCells(force: false);
            Stopped?.Invoke(this);
        }

        private void EnterPhase(Phase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
            _phaseStart = _position;
        }

        private void EnsureCells()
        {
            if (_cells.Count > 0)
            {
                return;
            }

            for (int i = 0; i < cellCount; i++)
            {
                ReelSymbol cell = Instantiate(symbolPrefab, symbolContainer);
                cell.name = $"Symbol {i}";
                _cells.Add(cell);
            }
        }

        private void RefreshCells(bool force)
        {
            int half = cellCount / 2;
            int first = Mathf.FloorToInt(_position + 0.5f) - half;

            bool blurred = _velocity > motion.BlurSpeedThreshold;
            float stretch = 1f + motion.MotionStretch * Mathf.Clamp01(_velocity / motion.MaxSpeed);

            for (int i = 0; i < _cells.Count; i++)
            {
                ReelSymbol cell = _cells[i];

                // The unique virtual index in [first, first + cellCount) congruent to i (mod cellCount).
                int k = first + Mod(i - first, cellCount);

                if (force || cell.VirtualIndex != k)
                {
                    cell.VirtualIndex = k;
                    cell.Show(_symbols.Get(SymbolIdAt(k)));
                }

                // Higher virtual index = further up the window; increasing position scrolls down.
                cell.RectTransform.anchoredPosition = new Vector2(0f, (k - _position) * symbolSpacing);
                cell.SetMotion(blurred, stretch);
            }
        }

        private int SymbolIdAt(int virtualIndex)
        {
            int offset = virtualIndex >= _seamIndex ? _targetOffset : _spinOffset;
            return _reelSet.GetSymbol(_reelIndex, offset + virtualIndex);
        }

        private int Wrap(int stop)
        {
            int length = _reelSet.GetStripLength(_reelIndex);
            return Mod(stop, length);
        }

        private static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }
    }
}
