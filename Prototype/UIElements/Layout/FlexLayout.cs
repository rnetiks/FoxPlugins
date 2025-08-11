using System.Collections.Generic;
using UnityEngine;

namespace Prototype.UIElements.Layout
{
    /// <summary>
    /// Represents a flexible layout system that arranges child elements according to
    /// configurable layout rules such as direction, justification, alignment, and spacing.
    /// Used to create dynamic and adaptive UI layouts in immediate mode GUI.
    /// </summary>
    public class FlexLayout
    {
        public enum FlexDirection
        {
            Row,
            Column
        }

        public enum JustifyContent
        {
            Start,
            Center,
            End,
            SpaceBetween,
            SpaceAround
        }

        public enum AlignItems
        {
            Start,
            Center,
            End,
            Stretch
        }

        public class FlexOptions
        {
            public float flexGrow = 0f;
            public float flexShrink = 1f;
            public float flexBasis = -1f;
            public float minWidth = 0f;
            public float minHeight = 0f;
            public float maxWidth = float.MaxValue;
            public float maxHeight = float.MaxValue;
            public Vector2 margin = Vector2.zero;
            public Vector2 padding = Vector2.zero;
        }

        public class FlexChild
        {
            public IMGUIElement element;
            public FlexOptions options;
            public Rect calculatedRect;
        }

        private readonly List<FlexChild> _children = new List<FlexChild>();
        public FlexDirection Direction { get; set; } = FlexDirection.Row;
        public JustifyContent JustifyType { get; set; } = JustifyContent.Start;
        public AlignItems AlignType { get; set; } = AlignItems.Start;
        public Vector2 Gap { get; set; } = Vector2.zero;

        public void AddChild(IMGUIElement element, FlexOptions options = null)
        {
            _children.Add(new FlexChild
            {
                element = element,
                options = options ?? new FlexOptions()
            });
        }

        public void OnGUI(Rect container)
        {
            CalculateLayout(container);

            foreach (var child in _children)
            {
                child.element.OnGUI(child.calculatedRect);
            }
        }

        private void CalculateLayout(Rect container)
        {
            if (_children.Count == 0) return;

            bool isRow = Direction == FlexDirection.Row;
            float availableSpace = isRow ? container.width : container.height;
            float crossAxisSize = isRow ? container.height : container.width;

            float totalGap = (isRow ? Gap.x : Gap.y) * (_children.Count - 1);
            availableSpace -= totalGap;

            float totalFixedSize = 0f;
            float totalFlexGrow = 0f;

            foreach (var child in _children)
            {
                if (child.options.flexBasis >= 0)
                {
                    totalFixedSize += child.options.flexBasis;
                }
                else
                {
                    var contentSize = child.element.GetPreferredSize();
                    float size = isRow ? contentSize.x : contentSize.y;
                    totalFixedSize += size;
                }

                totalFlexGrow += child.options.flexGrow;
            }

            float remainingSpace = availableSpace - totalFixedSize;
            float flexUnit = totalFlexGrow > 0 ? remainingSpace / totalFlexGrow : 0f;

            float currentPosition = 0f;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];

                float childSize = child.options.flexBasis >= 0 ? child.options.flexBasis : (isRow ? child.element.GetPreferredSize().x : child.element.GetPreferredSize().y);

                childSize += child.options.flexGrow * flexUnit;

                if (isRow)
                {
                    child.calculatedRect = new Rect(
                        container.x + currentPosition,
                        container.y,
                        childSize,
                        crossAxisSize
                    );
                }
                else
                {
                    child.calculatedRect = new Rect(
                        container.x,
                        container.y + currentPosition,
                        crossAxisSize,
                        childSize
                    );
                }

                currentPosition += childSize + (isRow ? Gap.x : Gap.y);
            }
        }
    }
}