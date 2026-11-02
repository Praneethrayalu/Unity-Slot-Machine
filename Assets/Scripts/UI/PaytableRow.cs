using SlotGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>One line of the pay table: three icons, the multiplier and the prize at the current bet.</summary>
    public sealed class PaytableRow : MonoBehaviour
    {
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text multiplierLabel;
        [SerializeField] private TMP_Text noteLabel;

        private PayTableEntry _entry;

        public void Bind(PayTableEntry entry, bool isWild)
        {
            _entry = entry;
            foreach (Image icon in icons)
            {
                icon.sprite = entry.Symbol.Sprite;
                icon.preserveAspect = true;
            }

            string note = isWild ? "WILD" : string.Empty;
            if (entry.FreeSpins > 0)
            {
                note = (note.Length > 0 ? note + " · " : string.Empty) + $"+{entry.FreeSpins} FREE SPINS";
            }

            noteLabel.text = note;
        }

        public void SetBet(int bet)
        {
            if (_entry != null)
            {
                multiplierLabel.text = $"x{_entry.Multiplier}  =  {(long)_entry.Multiplier * bet:N0}";
            }
        }
    }
}
