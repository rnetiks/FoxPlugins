using UnityEngine;

namespace UIBuilder
{
    /// <summary>
    /// Builder class for creating GUIStyle objects using a fluent interface pattern.
    /// Allows for easy and readable configuration of GUIStyle properties.
    /// </summary>
    public class GUIStyleBuilder
    {
        private GUIStyle style;

        /// <summary>
        /// Initializes a new GUIStyleBuilder with an empty GUIStyle.
        /// </summary>
        public GUIStyleBuilder()
        {
            style = new GUIStyle();
        }

        /// <summary>
        /// Initializes a new GUIStyleBuilder based on an existing GUIStyle.
        /// </summary>
        /// <param name="baseStyle">The base style to copy from</param>
        public GUIStyleBuilder(GUIStyle baseStyle)
        {
            style = new GUIStyle(baseStyle);
        }

        /// <summary>
        /// Initializes a new GUIStyleBuilder based on a built-in GUI style.
        /// </summary>
        /// <param name="builtinStyle">Built-in style name (e.g., "button", "label", "textField")</param>
        public GUIStyleBuilder(string builtinStyle)
        {
            style = new GUIStyle(builtinStyle);
        }

        // Font Properties
        public GUIStyleBuilder WithFont(Font font)
        {
            style.font = font;
            return this;
        }

        public GUIStyleBuilder WithFontSize(int fontSize)
        {
            style.fontSize = fontSize;
            return this;
        }

        public GUIStyleBuilder WithFontStyle(FontStyle fontStyle)
        {
            style.fontStyle = fontStyle;
            return this;
        }

        // Alignment
        public GUIStyleBuilder WithAlignment(TextAnchor alignment)
        {
            style.alignment = alignment;
            return this;
        }

        public GUIStyleBuilder WithImagePosition(ImagePosition imagePosition)
        {
            style.imagePosition = imagePosition;
            return this;
        }

        // Text Properties
        public GUIStyleBuilder WithWordWrap(bool wordWrap = true)
        {
            style.wordWrap = wordWrap;
            return this;
        }

        public GUIStyleBuilder WithClipping(TextClipping clipping)
        {
            style.clipping = clipping;
            return this;
        }

        public GUIStyleBuilder WithRichText(bool richText = true)
        {
            style.richText = richText;
            return this;
        }

        // State-based styling
        public GUIStyleBuilder WithNormalState(Color? textColor = null, Texture2D background = null)
        {
            if (textColor.HasValue)
                style.normal.textColor = textColor.Value;
            if (background != null)
                style.normal.background = background;
            return this;
        }

        public GUIStyleBuilder WithHoverState(Color? textColor = null, Texture2D background = null)
        {
            if (textColor.HasValue)
                style.hover.textColor = textColor.Value;
            if (background != null)
                style.hover.background = background;
            return this;
        }

        public GUIStyleBuilder WithActiveState(Color? textColor = null, Texture2D background = null)
        {
            if (textColor.HasValue)
                style.active.textColor = textColor.Value;
            if (background != null)
                style.active.background = background;
            return this;
        }

        public GUIStyleBuilder WithFocusedState(Color? textColor = null, Texture2D background = null)
        {
            if (textColor.HasValue)
                style.focused.textColor = textColor.Value;
            if (background != null)
                style.focused.background = background;
            return this;
        }

        // Layout Properties
        public GUIStyleBuilder WithPadding(int left, int right, int top, int bottom)
        {
            style.padding = new RectOffset(left, right, top, bottom);
            return this;
        }

        public GUIStyleBuilder WithPadding(int all)
        {
            style.padding = new RectOffset(all, all, all, all);
            return this;
        }

        public GUIStyleBuilder WithMargin(int left, int right, int top, int bottom)
        {
            style.margin = new RectOffset(left, right, top, bottom);
            return this;
        }

        public GUIStyleBuilder WithMargin(int all)
        {
            style.margin = new RectOffset(all, all, all, all);
            return this;
        }

        public GUIStyleBuilder WithBorder(int left, int right, int top, int bottom)
        {
            style.border = new RectOffset(left, right, top, bottom);
            return this;
        }

        public GUIStyleBuilder WithBorder(int all)
        {
            style.border = new RectOffset(all, all, all, all);
            return this;
        }

        public GUIStyleBuilder WithOverflow(int left, int right, int top, int bottom)
        {
            style.overflow = new RectOffset(left, right, top, bottom);
            return this;
        }

        // Size constraints
        public GUIStyleBuilder WithFixedWidth(float width)
        {
            style.fixedWidth = width;
            return this;
        }

        public GUIStyleBuilder WithFixedHeight(float height)
        {
            style.fixedHeight = height;
            return this;
        }

        public GUIStyleBuilder WithContentOffset(Vector2 offset)
        {
            style.contentOffset = offset;
            return this;
        }

        // Convenience methods for common styles
        public GUIStyleBuilder AsButton()
        {
            var buttonStyle = GUI.skin.button;
            style = new GUIStyle(buttonStyle);
            return this;
        }

        public GUIStyleBuilder AsLabel()
        {
            var labelStyle = GUI.skin.label;
            style = new GUIStyle(labelStyle);
            return this;
        }

        public GUIStyleBuilder AsTextField()
        {
            var textFieldStyle = GUI.skin.textField;
            style = new GUIStyle(textFieldStyle);
            return this;
        }

        public GUIStyleBuilder AsBox()
        {
            var boxStyle = GUI.skin.box;
            style = new GUIStyle(boxStyle);
            return this;
        }

        // Preset methods for common styling patterns
        public GUIStyleBuilder AsCenteredLabel()
        {
            return AsLabel()
                .WithAlignment(TextAnchor.MiddleCenter);
        }

        public GUIStyleBuilder AsTitle(int fontSize = 18)
        {
            return AsLabel()
                .WithFontSize(fontSize)
                .WithFontStyle(FontStyle.Bold)
                .WithAlignment(TextAnchor.MiddleCenter);
        }

        public GUIStyleBuilder AsCard(int padding = 10)
        {
            return AsBox()
                .WithPadding(padding);
        }

        /// <summary>
        /// Builds and returns the configured GUIStyle.
        /// </summary>
        /// <returns>The configured GUIStyle instance</returns>
        public GUIStyle Build()
        {
            return new GUIStyle(style);
        }

        /// <summary>
        /// Implicit conversion to GUIStyle for convenience.
        /// </summary>
        /// <param name="builder">The GUIStyleBuilder instance</param>
        public static implicit operator GUIStyle(GUIStyleBuilder builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// Creates a new GUIStyleBuilder instance.
        /// </summary>
        /// <returns>A new GUIStyleBuilder</returns>
        public static GUIStyleBuilder Create()
        {
            return new GUIStyleBuilder();
        }

        /// <summary>
        /// Creates a new GUIStyleBuilder based on an existing style.
        /// </summary>
        /// <param name="baseStyle">The base style to copy from</param>
        /// <returns>A new GUIStyleBuilder</returns>
        public static GUIStyleBuilder CreateFrom(GUIStyle baseStyle)
        {
            return new GUIStyleBuilder(baseStyle);
        }

        /// <summary>
        /// Creates a new GUIStyleBuilder based on a built-in style.
        /// </summary>
        /// <param name="builtinStyle">Built-in style name</param>
        /// <returns>A new GUIStyleBuilder</returns>
        public static GUIStyleBuilder CreateFrom(string builtinStyle)
        {
            return new GUIStyleBuilder(builtinStyle);
        }
    }
}