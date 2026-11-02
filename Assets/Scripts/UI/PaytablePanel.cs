using System.Collections.Generic;
using SlotGame.Core.Reels;
using SlotGame.Core.Rules;
using SlotGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Shows what each symbol pays, generated from the <see cref="PayTable"/> asset so it can never
    /// drift from the real rules, plus the exact theoretical RTP of the current reel strips.
    /// </summary>
    public sealed class PaytablePanel : PopupPanel
    {
        [SerializeField] private PaytableRow rowPrefab;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private TMP_Text footerLabel;
        [SerializeField] private Button closeButton;

        private readonly List<PaytableRow> _rows = new List<PaytableRow>();
        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>Builds (once) and opens the panel.</summary>
        public void Show(PayTable payTable, ReelSet reels, int currentBet)
        {
            if (!_built)
            {
                Build(payTable, reels);
                _built = true;
            }

            foreach (PaytableRow row in _rows)
            {
                row.SetBet(currentBet);
            }

            Open();
        }

        private void Build(PayTable payTable, ReelSet reels)
        {
            var entries = new List<PayTableEntry>(payTable.Entries);

            // Highest prize first, like a real machine's glass.
            entries.Sort((a, b) => b.Multiplier.CompareTo(a.Multiplier));

            foreach (PayTableEntry entry in entries)
            {
                if (entry.Symbol == null)
                {
                    continue;
                }

                PaytableRow row = Instantiate(rowPrefab, rowContainer);
                row.Bind(entry, payTable.IsWild(entry.Symbol.Id));
                _rows.Add(row);
            }

            TheoreticalReturn maths = PayTableMath.Calculate(reels, payTable);
            string wildNote = payTable.WildEnabled && payTable.WildSymbol != null
                ? $"{payTable.WildSymbol.DisplayName} is WILD: it completes any line.  "
                : string.Empty;
            footerLabel.text =
                $"{wildNote}Win = all 3 reels show the same symbol on the centre line.\n" +
                $"RTP {maths.TotalReturn:P1}  ·  hits {maths.HitFrequency:P1} of spins  ·  exact over {maths.Combinations:N0} reel combinations";
        }
    }
}
