using System;
using UnityEngine;

namespace PoseLib.KKS
{
    public class YNModal : Modal
    {
        private const int MARGIN = 15;
        private const int BUTTON_HEIGHT = 40;
        private const int BUTTON_SPACING = 10;
        private const int CONTENT_BOTTOM_MARGIN = 120;

        private readonly string _content;
        private readonly Action _onConfirm;
        private readonly Action _onCancel;

        public YNModal(Rect size, string content, string title, Action onConfirm = null, Action onCancel = null) : base(size, title)
        {
            _content = content ?? string.Empty;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        public override void DrawContent()
        {
            var contentWidth = Size.width - (MARGIN * 2);
            var contentHeight = Size.height - CONTENT_BOTTOM_MARGIN;
            var buttonWidth = (contentWidth - BUTTON_SPACING) / 2;
            var buttonY = Size.height - MARGIN - BUTTON_HEIGHT;
            
            var yesButtonX = MARGIN;
            var noButtonX = MARGIN + buttonWidth + BUTTON_SPACING;

            var contentRect = new Rect(MARGIN, MARGIN, contentWidth, contentHeight);
            GUI.Label(contentRect, _content, UIManager._theme.LabelCenterStyle);
            
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

        /// <summary>
        /// Creates a simple Yes/No confirmation modal
        /// </summary>
        /// <param name="size">Modal window size and position</param>
        /// <param name="content">Message to display</param>
        /// <param name="title">Modal title</param>
        /// <param name="onConfirm">Action to execute when Yes is clicked</param>
        /// <param name="onCancel">Action to execute when No is clicked</param>
        /// <returns>Configured YNModal instance</returns>
        public static YNModal Create(Rect size, string content, string title = "Confirm", Action onConfirm = null, Action onCancel = null)
        {
            return new YNModal(size, content, title, onConfirm, onCancel);
        }

        /// <summary>
        /// Creates a centered confirmation modal with default size
        /// </summary>
        /// <param name="content">Message to display</param>
        /// <param name="title">Modal title</param>
        /// <param name="onConfirm">Action to execute when Yes is clicked</param>
        /// <param name="onCancel">Action to execute when No is clicked</param>
        /// <returns>Configured YNModal instance</returns>
        public static YNModal CreateCentered(string content, string title = "Confirm", Action onConfirm = null, Action onCancel = null)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var modalWidth = 400;
            var modalHeight = 200;

            var centeredRect = new Rect(
                (screenWidth - modalWidth) / 2,
                (screenHeight - modalHeight) / 2,
                modalWidth,
                modalHeight
            );

            return new YNModal(centeredRect, content, title, onConfirm, onCancel);
        }
    }
}