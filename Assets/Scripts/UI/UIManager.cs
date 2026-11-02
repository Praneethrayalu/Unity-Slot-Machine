using SlotGame.Audio;
using SlotGame.Core.Game;
using SlotGame.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Binds the HUD to the <see cref="SlotMachineController"/>: credit / bet / win displays,
    /// the control buttons, the message strip on the machine and the modal popups.
    /// Purely reactive: it listens to controller and wallet events and forwards player input.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SlotMachineController controller;
        [SerializeField] private AudioManager audioManager;

        [Header("Displays")]
        [SerializeField] private AnimatedCounter creditsCounter;
        [SerializeField] private AnimatedCounter winCounter;
        [SerializeField] private TMP_Text betLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject freeSpinsBanner;
        [SerializeField] private TMP_Text freeSpinsLabel;

        [Tooltip("Small notice shown when the session uses a fixed QA seed instead of the secure RNG.")]
        [SerializeField] private TMP_Text sessionInfoLabel;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private TMP_Text spinButtonLabel;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button maxBetButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private TMP_Text autoSpinLabel;
        [SerializeField] private Button paytableButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private Animator spinButtonAnimator;

        [Header("Popups")]
        [SerializeField] private IntroScreen introScreen;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private PaytablePanel paytablePanel;
        [SerializeField] private ConfirmDialog confirmDialog;

        [Header("Timing")]
        [Tooltip("Seconds the credit meter takes to count up a small win.")]
        [SerializeField] private float smallWinCountTime = 0.9f;

        [SerializeField] private float bigWinCountTime = 2.6f;

        private static readonly int ReadyParam = Animator.StringToHash("Ready");

        private void Awake()
        {
            spinButton.onClick.AddListener(OnSpinPressed);
            betUpButton.onClick.AddListener(() => ChangeBet(true));
            betDownButton.onClick.AddListener(() => ChangeBet(false));
            maxBetButton.onClick.AddListener(OnMaxBetPressed);
            autoSpinButton.onClick.AddListener(OnAutoSpinPressed);
            introScreen.Dismissed += OnIntroDismissed;
            paytableButton.onClick.AddListener(OnPaytablePressed);
            soundButton.onClick.AddListener(OnSoundPressed);
            winPopup.Dismissed += OnWinPopupDismissed;
            winPopup.AmountCounter.Ticked += PlayCoinTick;
        }

        private void OnEnable()
        {
            controller.StateChanged += OnStateChanged;
            controller.SpinStarted += OnSpinStarted;
            controller.SpinCompleted += OnSpinCompleted;
            controller.SpinRejected += OnSpinRejected;
            controller.AutoSpinChanged += OnAutoSpinChanged;
            controller.FreeSpinsChanged += OnFreeSpinsChanged;
            controller.AnticipationStarted += OnAnticipationStarted;
            audioManager.MuteChanged += OnMuteChanged;
            creditsCounter.Ticked += PlayCoinTick;
        }

        private void Start()
        {
            // Wallet and bet selector are created in the controller's Awake, so bind here.
            controller.Wallet.BalanceChanged += OnBalanceChanged;
            controller.Bets.BetChanged += OnBetChanged;

            creditsCounter.SetInstant(controller.Wallet.Balance);
            winCounter.SetInstant(0);
            OnBetChanged(controller.Bets.CurrentBet);
            OnFreeSpinsChanged(controller.FreeSpinsRemaining);
            OnAutoSpinChanged(false, 0);
            OnMuteChanged(audioManager.IsMuted);
            SetMessage("PRESS SPIN OR PULL THE LEVER");
            // A seeded (?seed=) session is a QA/demo replay: the seed is logged to the console
            // instead of shown on screen, and the "SECURE RNG" badge is hidden because it would be untrue.
            sessionInfoLabel.text = controller.SessionSeed.HasValue ? string.Empty : "SECURE RNG";
            RefreshControls();
        }

        private void OnDisable()
        {
            controller.StateChanged -= OnStateChanged;
            controller.SpinStarted -= OnSpinStarted;
            controller.SpinCompleted -= OnSpinCompleted;
            controller.SpinRejected -= OnSpinRejected;
            controller.AutoSpinChanged -= OnAutoSpinChanged;
            controller.FreeSpinsChanged -= OnFreeSpinsChanged;
            controller.AnticipationStarted -= OnAnticipationStarted;
            audioManager.MuteChanged -= OnMuteChanged;
            creditsCounter.Ticked -= PlayCoinTick;

            if (controller.Wallet != null)
            {
                controller.Wallet.BalanceChanged -= OnBalanceChanged;
                controller.Bets.BetChanged -= OnBetChanged;
            }
        }

        private void Update()
        {
            // Keyboard shortcut: space spins / quick-stops / skips, like the button.
            if (introScreen.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    introScreen.Dismiss();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !AnyModalOpen())
            {
                OnSpinPressed();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && paytablePanel.IsOpen)
            {
                paytablePanel.Close();
            }
        }

        // ---------- Player input ----------

        private void OnSpinPressed()
        {
            audioManager.Play(SoundId.ButtonClick);
            if (winPopup.IsOpen)
            {
                OnWinPopupDismissed();
                winPopup.Close();
                return;
            }

            controller.RequestSpin();
        }

        private void ChangeBet(bool increase)
        {
            bool changed = increase ? controller.IncreaseBet() : controller.DecreaseBet();
            audioManager.Play(changed ? SoundId.BetChange : SoundId.ButtonClick, increase ? 1.1f : 0.9f);
        }

        private void OnMaxBetPressed()
        {
            bool changed = controller.MaxBet();
            audioManager.Play(changed ? SoundId.BetChange : SoundId.ButtonClick, 1.25f);
        }

        private void OnIntroDismissed()
        {
            // First user gesture: browsers allow audio from here on.
            audioManager.Play(SoundId.LeverPull);
        }

        private void OnAutoSpinPressed()
        {
            audioManager.Play(SoundId.ButtonClick);
            controller.ToggleAutoSpin();
        }

        private void OnPaytablePressed()
        {
            audioManager.Play(SoundId.ButtonClick);
            paytablePanel.Show(controller.PayTable, controller.ReelSet, controller.Bets.CurrentBet);
        }

        private void OnSoundPressed()
        {
            audioManager.ToggleMute();
            audioManager.Play(SoundId.ButtonClick);
        }

        private void OnWinPopupDismissed()
        {
            // Collecting the popup also finishes the credit count-up and skips the wait.
            creditsCounter.Complete(controller.Wallet.Balance);
            controller.SkipCelebration();
        }

        // ---------- Controller events ----------

        private void OnStateChanged(SlotState state)
        {
            RefreshControls();
        }

        private void OnSpinStarted(SpinResult spin)
        {
            // Free spins and auto-spin continue on their own: collect any open celebration first.
            if (winPopup.IsOpen)
            {
                winPopup.Close();
            }

            winCounter.SetInstant(0);
            spinButtonLabel.text = "STOP";

            if (spin.IsFreeSpin)
            {
                SetMessage($"FREE SPIN  ·  {controller.FreeSpinsRemaining} LEFT");
            }
            else if (controller.IsAutoSpinning)
            {
                SetMessage($"AUTO SPIN  ·  {controller.AutoSpinsRemaining} LEFT");
            }
            else
            {
                SetMessage("GOOD LUCK!");
            }
        }

        private void OnAnticipationStarted(int reelIndex)
        {
            SetMessage("SO CLOSE...");
        }

        private void OnSpinCompleted(SpinResult spin, WinTier tier)
        {
            if (!spin.Win.IsWin)
            {
                SetMessage(controller.FreeSpinsRemaining > 0 ? "FREE SPINS CONTINUE" : "TRY AGAIN!");
                return;
            }

            string symbolName = controller.Symbols.Get(spin.Win.SymbolId)?.DisplayName ?? "?";
            string message = $"3 x {symbolName}  ·  WIN {spin.Win.Payout:N0}";
            if (spin.Win.FreeSpinsAwarded > 0)
            {
                message += $"  ·  +{spin.Win.FreeSpinsAwarded} FREE SPINS";
            }

            SetMessage(message);

            float countTime = tier >= WinTier.Big ? bigWinCountTime : smallWinCountTime;
            winCounter.AnimateTo(spin.Win.Payout, countTime);

            if (tier >= WinTier.Big)
            {
                winPopup.Show(spin, tier, controller.Symbols, bigWinCountTime);
            }
        }

        private void OnSpinRejected(SpinRejection rejection)
        {
            if (rejection != SpinRejection.InsufficientCredits)
            {
                return;
            }

            // If a smaller bet is still affordable, step down instead of interrupting play.
            if (controller.Bets.FitToBalance(controller.Wallet.Balance))
            {
                SetMessage($"BET LOWERED TO {controller.Bets.CurrentBet:N0}");
                audioManager.Play(SoundId.NoCredits);
                RefreshControls();
                return;
            }

            audioManager.Play(SoundId.NoCredits);
            confirmDialog.Show(
                "OUT OF CREDITS",
                $"Top up with {controller.Config.RefillCredits:N0} free credits?",
                () =>
                {
                    controller.RefillCredits();
                    SetMessage("CREDITS REFILLED. GOOD LUCK!");
                },
                () => SetMessage("COME BACK SOON!"));
        }

        private void OnAutoSpinChanged(bool active, int remaining)
        {
            autoSpinLabel.text = active ? $"STOP\n{remaining}" : "AUTO";
            RefreshControls();
        }

        private void OnFreeSpinsChanged(int remaining)
        {
            freeSpinsBanner.SetActive(remaining > 0);
            freeSpinsLabel.text = $"FREE SPINS: {remaining}";
            RefreshControls();
        }

        private void OnBalanceChanged(long previous, long current)
        {
            if (current < previous)
            {
                // Bets leave the meter instantly; winnings count up for the "coins pouring in" feel.
                creditsCounter.SetInstant(current);
            }
            else
            {
                creditsCounter.AnimateTo(current, CountUpDurationFor(current - previous));
            }

            RefreshControls();
        }

        private void OnBetChanged(int bet)
        {
            betLabel.text = bet.ToString("N0");
            RefreshControls();
        }

        private void OnMuteChanged(bool muted)
        {
            soundLabel.text = muted ? "MUTED" : "SOUND";
        }

        // ---------- Helpers ----------

        private float CountUpDurationFor(long amount)
        {
            long bet = controller.Bets.CurrentBet;
            return amount >= bet * controller.Config.BigWinMultiplier ? bigWinCountTime : smallWinCountTime;
        }

        private void PlayCoinTick()
        {
            audioManager.Play(SoundId.CoinTick, 1f, 0.6f);
        }

        private void SetMessage(string text)
        {
            messageLabel.text = text;
        }

        private bool AnyModalOpen()
        {
            return introScreen.IsOpen || paytablePanel.IsOpen || confirmDialog.IsOpen;
        }

        private void RefreshControls()
        {
            bool idle = controller.State == SlotState.Idle;
            bool betLocked = !idle || controller.FreeSpinsRemaining > 0 || controller.IsAutoSpinning;

            betUpButton.interactable = !betLocked && controller.Bets.CanIncrease;
            betDownButton.interactable = !betLocked && controller.Bets.CanDecrease;
            int maxAffordable = controller.Bets.HighestAffordableIndex(controller.Wallet.Balance);
            maxBetButton.interactable = !betLocked && maxAffordable >= 0 && controller.Bets.Index < maxAffordable;
            paytableButton.interactable = idle && !controller.IsAutoSpinning;

            if (idle)
            {
                spinButtonLabel.text = controller.FreeSpinsRemaining > 0 ? "FREE\nSPIN" : "SPIN";
            }
            else if (controller.State == SlotState.Celebrating)
            {
                spinButtonLabel.text = "SKIP";
            }

            if (spinButtonAnimator != null)
            {
                // Idle "breathing" pulse invites the next spin.
                spinButtonAnimator.SetBool(ReadyParam, idle && !controller.IsAutoSpinning);
            }
        }
    }
}
