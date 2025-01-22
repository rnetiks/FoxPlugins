using System;
using Autumn.Animation;
using PrismaLib.Settings;
using SmartRectV0;
using UnityEngine;

namespace Autumn.Elements
{
    public class DropdownSelection : GUIBase
    {
        private GUIStyle _activeButtonStyle;
        private Rect _boxPosition;
        private GUIStyle _boxStyle;
        private Rect _cursorLimits;
        private GUIBase _guiOwner;
        private SmartRect _rect;
        private Setting<int> _refSet;
        private string[] _selections;

        private DropdownSelection() : base(nameof(DropdownSelection), GUILayers.DropdownBox)
        {
            _boxStyle = new GUIStyle
            {
                normal =
                {
                    background = Style.Button.normal.background
                }
            };
            _activeButtonStyle = new GUIStyle(Style.Button)
            {
                normal = Style.SelectionGrid.onNormal,
                hover = Style.SelectionGrid.onHover,
                active = Style.SelectionGrid.onActive
            };
        }
        
        public event EventHandler IndexChanged;
        protected internal override void Draw()
        {
            var pos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
            if (!_cursorLimits.Contains(pos) || (_guiOwner != null && !_guiOwner.IsActive))
            {
                Disable();
                return;
            }

            GUI.Box(_boxPosition, string.Empty, _boxStyle);
            _rect.Reset();
            var wasPressed = false;
            for (int i = 0; i < _selections.Length; i++)
            {
                
                if (GUI.Button(_rect, _selections[i], i == _refSet.Value ? _activeButtonStyle : Style.Button))
                {
                    _refSet.Value = i;
                    IndexChanged?.Invoke(this, EventArgs.Empty);
                    wasPressed = true;
                }

                _rect.MoveY();
            }

            if (wasPressed)
            {
                Disable();
            }
        }

        public static DropdownSelection CreateNew(GUIBase baseGUI, Rect position, string[] selections,
            Setting<int> referenceSetting)
        {
            var element = new DropdownSelection
            {
                _guiOwner = baseGUI,
                _cursorLimits = new Rect(
                    position.x - new AutoScaleFloat(10f),
                    position.y - new AutoScaleFloat(10f),
                    position.width + new AutoScaleFloat(20f),
                    position.height * selections.Length + Style.VerticalMargin * (selections.Length - 1) +
                    2 * selections.Length + new AutoScaleFloat(20f)
                ),
                _refSet = referenceSetting,
                _boxPosition = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    position.height * selections.Length + Style.VerticalMargin * (selections.Length - 1) +
                    2 * selections.Length
                ),
                _rect = new SmartRect(position)
            };

            element.animator = new DropDownAnimation(element, position, selections.Length);
            element.Enable();
            element._selections = selections;
            return element;
        }

        public static DropdownSelection CreateNew(Rect position, string[] selections, Setting<int> referenceSetting)
        {
            var element = new DropdownSelection
            {
                _cursorLimits = new Rect(
                    position.x - new AutoScaleFloat(10f),
                    position.y - new AutoScaleFloat(10f),
                    position.width + new AutoScaleFloat(20f),
                    position.height * selections.Length + Style.VerticalMargin * (selections.Length + 1) +
                    new AutoScaleFloat(20f)
                ),
                _refSet = referenceSetting,
                _boxPosition = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    position.height * selections.Length + Style.VerticalMargin * (selections.Length - 1)
                ),
                _rect = new SmartRect(position)
            };

            element.animator = new DropDownAnimation(element, position, selections.Length);
            element.Enable();
            element._selections = selections;
            return element;
        }
    }
}