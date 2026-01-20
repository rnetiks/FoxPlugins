using System;
using UnityEngine;

namespace PoseLib.KKS
{
    public class YNModal : ModalBase
    {
        private const int MARGIN = 15;
        private const int BUTTON_HEIGHT = 40;
        private const int BUTTON_SPACING = 10;
        private const int CONTENT_BOTTOM_MARGIN = 120;

        private readonly string _content;
        private readonly Action _onConfirm;
        private readonly Action _onCancel;

        public YNModal(Rect size, string content, string title, Action onConfirm = null, Action onCancel = null) : base(title)
        {
            _content = content ?? string.Empty;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        protected override void DrawContent(Rect size)
        {
            var contentWidth = size.width - (MARGIN * 2);
            var contentHeight = size.height - CONTENT_BOTTOM_MARGIN;
            var buttonWidth = (contentWidth - BUTTON_SPACING) / 2;
            var buttonY = size.height - MARGIN - BUTTON_HEIGHT;
            
            var yesButtonX = MARGIN;
            var noButtonX = MARGIN + buttonWidth + BUTTON_SPACING;

            var contentRect = new Rect(MARGIN, MARGIN, contentWidth, contentHeight);
            GUI.Label(contentRect, _content, UIManager._theme.LabelMiddleCenterStyle);
            
            var yesButtonRect = new Rect(yesButtonX, buttonY, buttonWidth, BUTTON_HEIGHT);
            if (GUI.Button(yesButtonRect, "Yes", UIManager._theme.SaveButtonStyle))
            {
                _onConfirm?.Invoke();
            }
            
            var noButtonRect = new Rect(noButtonX, buttonY, buttonWidth, BUTTON_HEIGHT);
            if (GUI.Button(noButtonRect, "No", UIManager._theme.CancelButtonStyle))
            {
                _onCancel?.Invoke();
            }
        }
    }
}