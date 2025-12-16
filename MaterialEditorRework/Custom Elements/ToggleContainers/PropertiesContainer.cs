using System;
using System.Linq;
using Addin;
using KKAPI.Utilities;
using MaterialEditorRework.Views;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
    public class PropertiesContainer : ToggleContainerBase
    {
        public PropertiesContainer(Vector2 size, Material material) : base(size)
        { 
        }
        public override void DrawHeader(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x + 16, rect.y + rect.height / 2 - 10, 20, 20), Icons.SettingsIcon);
            GUI.Label(new Rect(rect.x + 48, rect.y + rect.height / 2 - 10, 100, 20), "Properties", Styles.BoldBlack);
        }
        public override void DrawContent(Rect rect)
        {

        }
    }
}