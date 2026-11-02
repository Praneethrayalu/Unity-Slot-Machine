using System;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Core.Economy;
using SlotGame.Core.Game;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;
using SlotGame.Data;
using SlotGame.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Orchestrates a spin from button press to payout.
    /// <para>
    /// The controller owns the pure C# <see cref="SlotEngine"/> (rules, RNG, wallet) and drives the
    /// <see cref="Reel"/> views. It never plays sounds or touches UI directly: it raises events and
    /// the UI / audio presenters react, which keeps the flow easy to test and re-skin.
    /// </para>
    /// Spin flow: <c>TryBeginSpin</c> (bet taken, outcome decided) → reels start staggered →
    /// minimum spin time → reels stop one by one (the last one waits longer if a win is still
    /// possible) → <c>SettleSpin</c> (win paid) → celebration → next auto/free spin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlotMachineController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SymbolDatabase symbolDatabase;
        [SerializeField] private PayTable payTable;
        [SerializeField] private ReelStripSet reelStrips;
        [SerializeField] private SlotMachineConfig config;

        [Header("Views")]
        [SerializeField] private Reel[] reels = Array.Empty<Reel>();

        [Header("Randomness")]
        [Tooltip("Use a fixed seed instead of the cryptographic RNG (QA / reproducing a sequence). Never ship enabled.")]
        [SerializeField] private bool useDeterministicSeed;

        [SerializeField] private int deterministicSeed = 12345;

        [Header("Designer hooks")]
        [SerializeField] private UnityEvent onSpinStarted = new UnityEvent();

        [Tooltip("Invoked with the payout of every winning spin.")]
        [SerializeField] private UnityEvent<long> onWin = new UnityEvent<long>();

        private SlotEngine _engine;
        private IRandomProvider _random;
        private Coroutine _spinRoutine;
        private bool _quickStopRequested;
        private bool _skipCelebrationRequested;
        private int _autoSpinsRemaining;
        private SlotState _state = SlotState.Idle;

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<SlotState> StateChanged;

        /// <summary>Raised after the bet was taken and the outcome decided, as reels start.</summary>
        public event Action<SpinResult> SpinStarted;

        /// <summary>Raised when a reel settles (reel index, 0-based).</summary>
        public event Action<int> ReelStopped;

        /// <summary>Raised when the last reel starts its suspense extension.</summary>
        public event Action<int> AnticipationStarted;

        /// <summary>Raised once the outcome is paid, with the tier the UI should celebrate at.</summary>
        public event Action<SpinResult, WinTier> SpinCompleted;

        /// <summary>Raised when a spin could not start (e.g. insufficient credits).</summary>
        public event Action<SpinRejection> SpinRejected;

        /// <summary>Raised when auto-spin starts, stops or counts down (active, spins remaining).</summary>
        public event Action<bool, int> AutoSpinChanged;

        /// <summary>Raised when the free spin counter changes.</summary>
        public event Action<int> FreeSpinsChanged;

        public SlotState State => _state;

        public Wallet Wallet => _engine.Wallet;

        public BetSelector Bets => _engine.Bets;

        public SymbolDatabase Symbols => symbolDatabase;

        public PayTable PayTable => payTable;

        public SlotMachineConfig Config => config;

        public ReelSet ReelSet => _engine.Reels;

        public int FreeSpinsRemaining => _engine.FreeSpinsRemaining;

        public bool IsAutoSpinning => _autoSpinsRemaining > 0;

        public int AutoSpinsRemaining => _autoSpinsRemaining;

        public int SpinsPlayed => _engine.SpinsPlayed;

        public IReadOnlyList<Reel> Reels => reels;

        /// <summary>Seed of a reproducible QA session, or null when the cryptographic RNG is in use.</summary>
        public int? SessionSeed { get; private set; }

        /// <summary>True when the spin button should be usable.</summary>
        public bool CanRequestSpin => _state == SlotState.Idle && _engine.CanSpin;

        private void Awake()
        {
            ValidateReferences();

            _random = CreateRandomProvider();

            ReelSet reelSet = reelStrips.CreateReelSet();
            var wallet = new Wallet(config.StartingCredits);
            var bets = new BetSelector(config.BetOptions, config.DefaultBetIndex);
            _engine = new SlotEngine(reelSet, payTable, _random, wallet, bets);
            _engine.FreeSpinsChanged += count => FreeSpinsChanged?.Invoke(count);

            // Start every reel on a random stop so the machine never opens on a fixed screen.
            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].Initialize(reelSet, i, symbolDatabase, _random.Next(0, reelSet.GetStripLength(i)));
            }
        }

        /// <summary>
        /// Production uses the CSPRNG. A seeded provider is used only when explicitly requested,
        /// either from the inspector or with a <c>?seed=123</c> URL parameter on a QA/development build,
        /// so QA can replay an exact sequence of outcomes. Seeded sessions are logged and drop the "SECURE RNG" badge.
        /// </summary>
        private IRandomProvider CreateRandomProvider()
        {
            int seed = deterministicSeed;
            if (useDeterministicSeed || (UrlSeedingAllowed && TryReadSeedFromUrl(Application.absoluteURL, out seed)))
            {
                SessionSeed = seed;
                Debug.Log($"[Slot] Seeded session, seed={seed}. Outcomes are reproducible.");
                return new SeededRandomProvider(seed);
            }

            return new CryptoRandomProvider();
        }

        /// <summary>
        /// Whether a <c>?seed=</c> URL parameter may replace the CSPRNG. Only editor, development and
        /// dedicated QA builds (scripting define <c>SLOT_QA_SEEDS</c>) honour it, so a player can never
        /// choose their own outcome sequence in the shipped release build.
        /// </summary>
        private static bool UrlSeedingAllowed
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || SLOT_QA_SEEDS
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Parses <c>seed=&lt;int&gt;</c> from a URL query string.</summary>
        public static bool TryReadSeedFromUrl(string url, out int seed)
        {
            return AppBootstrap.TryReadIntParameter(url, "seed", out seed);
        }

        private void OnDestroy()
        {
            (_random as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Main input entry point (spin button, lever, space bar).
        /// While reels are spinning it acts as "quick stop"; while celebrating it skips the celebration.
        /// </summary>
        public void RequestSpin()
        {
            switch (_state)
            {
                case SlotState.Spinning:
                    _quickStopRequested = true;
                    return;

                case SlotState.Celebrating:
                    _skipCelebrationRequested = true;
                    return;
            }

            if (_spinRoutine != null)
            {
                return;
            }

            _spinRoutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>Starts auto-spin with the configured count, or stops it if already running.</summary>
        public void ToggleAutoSpin()
        {
            if (IsAutoSpinning)
            {
                StopAutoSpin();
                return;
            }

            _autoSpinsRemaining = config.AutoSpinCount;
            AutoSpinChanged?.Invoke(true, _autoSpinsRemaining);

            if (_state == SlotState.Idle)
            {
                RequestSpin();
            }
        }

        public void StopAutoSpin()
        {
            if (_autoSpinsRemaining == 0)
            {
                return;
            }

            _autoSpinsRemaining = 0;
            AutoSpinChanged?.Invoke(false, 0);
        }

        /// <summary>Skips the current win presentation (e.g. the player tapped the popup).</summary>
        public void SkipCelebration()
        {
            if (_state == SlotState.Celebrating)
            {
                _skipCelebrationRequested = true;
            }
        }

        public bool IncreaseBet()
        {
            return _state == SlotState.Idle && FreeSpinsRemaining == 0 && _engine.Bets.Increase();
        }

        public bool DecreaseBet()
        {
            return _state == SlotState.Idle && FreeSpinsRemaining == 0 && _engine.Bets.Decrease();
        }

        /// <summary>Tops the wallet back up (offered by the UI when credits run out).</summary>
        public void RefillCredits()
        {
            _engine.Wallet.Credit(config.RefillCredits);
        }

        /// <summary>Classifies a win for presentation purposes.</summary>
        public WinTier GetWinTier(WinResult win)
        {
            if (!win.IsWin)
            {
                return WinTier.None;
            }

            if (payTable.IsWild(win.SymbolId) || win.Multiplier >= config.JackpotMultiplier)
            {
                return WinTier.Jackpot;
            }

            return win.Multiplier >= config.BigWinMultiplier ? WinTier.Big : WinTier.Small;
        }

        private IEnumerator SpinRoutine()
        {
            if (!_engine.TryBeginSpin(out SpinResult spin, out SpinRejection rejection))
            {
                StopAutoSpin();
                SpinRejected?.Invoke(rejection);
                _spinRoutine = null;
                yield break;
            }

            _quickStopRequested = false;
            SetState(SlotState.Spinning);
            SpinStarted?.Invoke(spin);
            onSpinStarted.Invoke();

            if (IsAutoSpinning && !spin.IsFreeSpin)
            {
                _autoSpinsRemaining--;
                AutoSpinChanged?.Invoke(true, _autoSpinsRemaining);
            }

            yield return StartReels();
            yield return StopReels(spin);

            // Reels are at rest showing the outcome: only now is the win paid.
            _engine.SettleSpin(spin);
            WinTier tier = GetWinTier(spin.Win);

            // One line per spin: a lightweight audit trail (visible in the browser console on WebGL).
            Debug.Log($"[Slot] {spin} tier={tier} balance={_engine.Wallet.Balance} freeSpinsLeft={_engine.FreeSpinsRemaining}");
            HighlightResult(spin, tier);

            SetState(spin.Win.IsWin ? SlotState.Celebrating : SlotState.Idle);
            SpinCompleted?.Invoke(spin, tier);

            if (spin.Win.IsWin)
            {
                onWin.Invoke(spin.Win.Payout);
                yield return WaitForCelebration(tier);
            }

            SetState(SlotState.Idle);
            _spinRoutine = null;

            yield return ContinueAutomaticPlay();
        }

        private IEnumerator StartReels()
        {
            var stagger = new WaitForSeconds(config.ReelStartStagger);
            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].StartSpin();
                if (i < reels.Length - 1)
                {
                    yield return stagger;
                }
            }
        }

        private IEnumerator StopReels(SpinResult spin)
        {
            yield return WaitOrQuickStop(config.MinimumSpinTime);

            var stoppedSymbols = new List<int>(reels.Length);

            for (int i = 0; i < reels.Length; i++)
            {
                bool isLastReel = i == reels.Length - 1;

                // Near-miss / anticipation: if everything stopped so far can still make a win,
                // hold the final reel a little longer so the player feels the tension.
                if (isLastReel && reels.Length > 1 && !_quickStopRequested &&
                    _engine.Evaluator.CanStillWin(stoppedSymbols))
                {
                    reels[i].SetAnticipation(true);
                    AnticipationStarted?.Invoke(i);
                    yield return WaitOrQuickStop(config.AnticipationTime);
                }

                reels[i].RequestStop(spin.StopIndices[i], _quickStopRequested);
                yield return new WaitUntil(() => !reels[i].IsSpinning);

                ReelStopped?.Invoke(i);
                stoppedSymbols.Add(spin.LineSymbols[i]);

                if (!isLastReel)
                {
                    yield return WaitOrQuickStop(config.ReelStopStagger);
                }
            }
        }

        private IEnumerator WaitOrQuickStop(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (_quickStopRequested)
                {
                    yield return new WaitForSeconds(config.QuickStopStagger);
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator WaitForCelebration(WinTier tier)
        {
            _skipCelebrationRequested = false;
            float duration;
            switch (tier)
            {
                case WinTier.Jackpot:
                    duration = config.JackpotPresentationTime;
                    break;
                case WinTier.Big:
                    duration = config.BigWinPresentationTime;
                    break;
                default:
                    duration = config.SmallWinPresentationTime;
                    break;
            }

            for (float t = 0f; t < duration && !_skipCelebrationRequested; t += Time.deltaTime)
            {
                yield return null;
            }

            _skipCelebrationRequested = false;
        }

        private IEnumerator ContinueAutomaticPlay()
        {
            bool freeSpinPending = _engine.FreeSpinsRemaining > 0;
            if (!freeSpinPending && !IsAutoSpinning)
            {
                yield break;
            }

            yield return new WaitForSeconds(config.AutoSpinDelay);

            if (!freeSpinPending && IsAutoSpinning && !_engine.CanSpin)
            {
                // Out of credits mid auto-spin: stop and let the UI offer a refill.
                StopAutoSpin();
                SpinRejected?.Invoke(SpinRejection.InsufficientCredits);
                yield break;
            }

            if (_state == SlotState.Idle && (_engine.FreeSpinsRemaining > 0 || IsAutoSpinning))
            {
                RequestSpin();
            }
        }

        private void HighlightResult(SpinResult spin, WinTier tier)
        {
            foreach (Reel reel in reels)
            {
                ReelSymbol symbol = reel.PaylineSymbol;
                if (symbol == null)
                {
                    continue;
                }

                if (tier == WinTier.None)
                {
                    symbol.StopHighlight();
                }
                else
                {
                    symbol.PlayWinHighlight();
                }
            }

            if (tier == WinTier.None)
            {
                return;
            }

            // Dim the symbols off the pay line so the winning row pops.
            foreach (Reel reel in reels)
            {
                ReelSymbol payline = reel.PaylineSymbol;
                foreach (ReelSymbol cell in reel.Cells)
                {
                    if (cell != payline)
                    {
                        cell.SetDimmed(true);
                    }
                }
            }
        }

        private void SetState(SlotState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            StateChanged?.Invoke(state);
        }

        private void ValidateReferences()
        {
            if (symbolDatabase == null || payTable == null || reelStrips == null || config == null)
            {
                throw new InvalidOperationException($"{name}: assign SymbolDatabase, PayTable, ReelStripSet and Config.");
            }

            if (reels == null || reels.Length == 0)
            {
                throw new InvalidOperationException($"{name}: no reels assigned.");
            }

            if (reels.Length != reelStrips.Reels.Count)
            {
                throw new InvalidOperationException(
                    $"{name}: {reels.Length} reel views but {reelStrips.Reels.Count} reel strips.");
            }
        }
    }
}
