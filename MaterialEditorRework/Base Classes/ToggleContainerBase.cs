using System.Diagnostics;
using BepInEx.Logging;
using TexFac.Universal;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorRework.Views
{
    public abstract class ToggleContainerBase
    {
        private bool _isOpen = true;
        private static Texture2D _headerTextureClose;
        private static Texture2D _headerTextureOpen;
        private static Texture2D _backgroundTexture;
        private static Texture2D _footerTexture;
        private static Texture2D _dividerTexture;
        public int Height;

        public ToggleContainerBase(Vector2 size)
        {
            if ( _headerTextureClose == null || _backgroundTexture == null ||  _headerTextureOpen == null || _footerTexture == null || _dividerTexture == null)
            {
                _headerTextureClose = TextureFactory.SolidColor((int)size.x, (int)size.y, Color.white)
                    .BorderRadius(10, aliasDistance: 0.5f).GetTexture();
                _headerTextureOpen = TextureFactory.SolidColor((int)size.x, (int)size.y, Color.white)
                    .BorderRadius(10, BorderType.TopLeft | BorderType.TopRight, 0.5f).GetTexture();
                _backgroundTexture = TextureFactory.SolidColor(1, 1, Color.white).GetTexture();
                _footerTexture = TextureFactory.SolidColor((int)size.x, 10, Color.white)
                    .BorderRadius(10, BorderType.BottomLeft | BorderType.BottomRight, 0.5f).GetTexture();
                _dividerTexture = TextureFactory.SolidColor(1, 1, new Color(0.95f, 0.96f, 0.96f)).GetTexture();
            }
        }
        public void Draw(Rect header, Rect content)
        {
            Height = (int)header.height;
            if (_isOpen)
                Height += (int)content.height;
            DrawHeaderInt(header);
            if (_isOpen)
                DrawContentInt(content);
        }

        private void DrawHeaderInt(Rect rect)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    _isOpen = !_isOpen;
                }
                GUI.color = new Color(0.9f, 0.9f, 0.9f);
            }


            if (_isOpen)
            {
                GUI.DrawTexture(rect, _headerTextureOpen);
                GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), _dividerTexture);
            }
            else
            {
                GUI.DrawTexture(rect, _headerTextureClose);
            }
            DrawHeader(rect);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.x + rect.width - 36, rect.y + rect.height / 2 - 10, 20, 20), _isOpen ? Icons.ChevronDownIcon : Icons.ChevronRightIcon);
        }

        private void DrawContentInt(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height - 10), _backgroundTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 10, rect.width, 10), _footerTexture);

            DrawContent(rect);
        }

        public abstract void DrawHeader(Rect rect);
        public abstract void DrawContent(Rect rect);
    }
}