using System;
using SlotGame.Core.Game;
using SlotGame.Data;
using SlotGame.Gameplay;
using TMPro;
using UnityEngine;
using SlotGame.Utilities;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// The BIG WIN / JACKPOT celebration: title, winning symbols, a counting payout and a coin
    /// fountain. Tapping anywhere (or the X) collects and closes it.
    /// </summary>
    public sealed class WinPopup : PopupPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text detailLabel;
        [SerializeField] private AnimatedCounter amountCounter;
        [SerializeField] private Image[] symbolIcons = Array.Empty<Image>();
        [SerializeField] private Button closeButton;
        [SerializeField] private Button collectArea;
        [SerializeField] private CoinBurst coinBurst;

        [Header("Coins per tier")]
        [SerializeField] private int bigWinCoins = 35;
        [SerializeField] private int megaWinCoins = 55;
        [SerializeField] private int jackpotCoins = 80;

        private long _payout;
        private float _titlePunchTime;

        /// <summary>Raised when the player dismisses the popup.</summary>
        public event Action Dismissed;

        /// <summary>The counter, exposed so the UI can hook coin-tick sounds.</summary>
        public AnimatedCounter AmountCounter => amountCounter;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Dismiss);
            if (collectArea != null)
            {
                collectArea.onClick.AddListener(Dismiss);
            }
        }

        public void Show(SpinResult spin, WinTier tier, SymbolDatabase symbols, float countDuration)
        {
            _payout = spin.Win.Payout;
            titleLabel.text = TitleFor(tier);
            _titlePunchTime = 0f;

            string detail = $"x{spin.Win.Multiplier} on a {spin.Bet:N0} bet";
            if (spin.Win.IncludesWild)
            {
                detail += "  ·  WILD";
            }

            if (spin.Win.FreeSpinsAwarded > 0)
            {
                detail += $"  ·  +{spin.Win.FreeSpinsAwarded} FREE SPINS";
            }

            detailLabel.text = detail;

            for (int i = 0; i < symbolIcons.Length && i < spin.LineSymbols.Count; i++)
            {
                SymbolDefinition symbol = symbols.Get(spin.LineSymbols[i]);
                symbolIcons[i].sprite = symbol != null ? symbol.Sprite : null;
                symbolIcons[i].preserveAspect = true;
            }

            Open();
            amountCounter.SetInstant(0);
            amountCounter.AnimateTo(_payout, countDuration);

            coinBurst.Clear();
            coinBurst.Burst(tier == WinTier.Jackpot ? jackpotCoins : tier == WinTier.Mega ? megaWinCoins : bigWinCoins);
        }

        private static string TitleFor(WinTier tier)
        {
            switch (tier)
            {
                case WinTier.Jackpot: return "JACKPOT!";
                case WinTier.Mega: return "MEGA WIN!";
                default: return "BIG WIN!";
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            // Title "slam": overshoots from 2.2x down to 1x in ~0.4 s, then breathes gently.
            _titlePunchTime += Time.unscaledDeltaTime;
            float punch = _titlePunchTime < 0.4f
                ? Mathf.LerpUnclamped(2.2f, 1f, Easing.OutBack(_titlePunchTime / 0.4f))
                : 1f + 0.06f * Mathf.Sin((_titlePunchTime - 0.4f) * 5f);
            titleLabel.rectTransform.localScale = Vector3.one * punch;
        }

        public override void Close()
        {
            amountCounter.Complete(_payout);
            base.Close();
        }

        private void Dismiss()
        {
            if (!IsOpen)
            {
                return;
            }

            Close();
            Dismissed?.Invoke();
        }
    }
}
