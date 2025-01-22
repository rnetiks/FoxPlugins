using System;
using Autumn.Animation;
using PrismaLib.Settings;
using SmartRectV0;
using UnityEngine;

namespace Autumn.Elements
{
    public class ScrollableDropdownSelection : GUIBase
    {
        private GUIStyle activeButtonStyle;
        private Rect boxPosition;
        private GUIStyle boxStyle;
        private Rect cursorLimits;
        private GUIBase guiOwner;
        private int initialSelection;
        private SmartRect rect;
        private Setting<int> refSet;
        private string[] selections;
        private Vector2 scroll = Vector2.zero;
        private Rect viewRect;

        private ScrollableDropdownSelection() : base(nameof(ScrollableDropdownSelection), GUILayers.DropdownBox)
        {

            boxStyle = new GUIStyle();
            boxStyle.normal.background = Style.Button.normal.background;
            activeButtonStyle = new GUIStyle(Style.Button);
            activeButtonStyle.normal = Style.SelectionGrid.onNormal;
            activeButtonStyle.hover = Style.SelectionGrid.onHover;
            activeButtonStyle.active = Style.SelectionGrid.onActive;
        }

        public event EventHandler IndexChanged;

        protected internal override void Draw()
        {
            var pos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
            if (!cursorLimits.Contains(pos) || (guiOwner != null && guiOwner.IsActive == false))
            {
                Disable();
                return;
            }

            GUI.Box(boxPosition, string.Empty, boxStyle);
            scroll = GUI.BeginScrollView(boxPosition, scroll, viewRect, GUIStyle.none, GUIStyle.none);
            rect.Reset();
            bool wasPressed = false;
            for (int i = 0; i < selections.Length; i++)
            {
                if (GUI.Button(rect, selections[i], i == refSet.Value ? activeButtonStyle : Style.Button))
                {
                    refSet.Value = i;
                    IndexChanged?.Invoke(this, EventArgs.Empty);
                    wasPressed = true;
                }

                rect.MoveY();
            }

            GUI.EndScrollView();

            if (wasPressed)
            {
                Disable();
            }
        }

        public static ScrollableDropdownSelection CreateNew(GUIBase baseGUI, Rect position, string[] selections,
            Setting<int> referenceSetting, int showItems)
        {
            if (showItems > selections.Length)
            {
                throw new InvalidOperationException(
                    "Length of shownItems must be less than selections.");
            }

            var element = new ScrollableDropdownSelection
            {
                guiOwner = baseGUI,
                cursorLimits = new Rect(
                    position.x - new AutoScaleFloat(10f),
                    position.y - new AutoScaleFloat(10f),
                    position.width + new AutoScaleFloat(20f),
                    position.height * showItems + Style.VerticalMargin * (showItems + 1) +
                    showItems * Style.Height * 0.8f + new AutoScaleFloat(20f)
                ),
                refSet = referenceSetting,
                boxPosition = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    position.height * showItems + Style.VerticalMargin * (showItems - 1) +
                    (showItems * Style.Height * 0.8f)
                ),
                viewRect = new Rect(0f, 0f, position.width,
                    position.height * selections.Length + (Style.VerticalMargin * (selections.Length - 1)) +
                    (4 * (selections.Length - 1))),
                rect = new SmartRect(0f, 0f, position.width, position.height)
            };

            element.animator = new DropDownAnimation(element, position, showItems + 1);
            element.Enable();
            element.initialSelection = referenceSetting.Value;
            element.selections = selections;
            return element;
        }

        public static ScrollableDropdownSelection CreateNew(Rect position, string[] selections,
            Setting<int> referenceSetting, int shownItems)
        {
            var element = new ScrollableDropdownSelection();

            element.cursorLimits = new Rect(
                position.x - new AutoScaleFloat(10f),
                position.y - new AutoScaleFloat(10f),
                position.width + new AutoScaleFloat(20f),
                position.height * shownItems + Style.VerticalMargin * (shownItems + 1) +
                shownItems * Style.Height * 0.8f + new AutoScaleFloat(20f)
            );

            element.refSet = referenceSetting;
            element.boxPosition = new Rect(
                position.x,
                position.y,
                position.width,
                position.height * shownItems + Style.VerticalMargin * (shownItems - 1) + (shownItems * Style.Height * 0.8f)
            );

            element.viewRect = new Rect(0f, 0f, position.width,
                position.height * selections.Length + (Style.VerticalMargin * 1.5f * (selections.Length + 1)));
            element.rect = new SmartRect(0f, 0f, position.width, position.height);
            element.animator = new DropDownAnimation(element, position, shownItems + 1);
            element.Enable();
            element.initialSelection = referenceSetting.Value;
            element.selections = selections;
            return element;
        }
    }
}