using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>Generic YES / NO modal (used for the "out of credits, refill?" prompt).</summary>
    public sealed class ConfirmDialog : PopupPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action _onYes;
        private Action _onNo;

        protected override void Awake()
        {
            base.Awake();
            yesButton.onClick.AddListener(() => Resolve(_onYes));
            noButton.onClick.AddListener(() => Resolve(_onNo));
        }

        public void Show(string title, string message, Action onYes, Action onNo = null)
        {
            titleLabel.text = title;
            messageLabel.text = message;
            _onYes = onYes;
            _onNo = onNo;
            Open();
        }

        private void Resolve(Action callback)
        {
            Close();
            Action pending = callback;
            _onYes = null;
            _onNo = null;
            pending?.Invoke();
        }
    }
}
