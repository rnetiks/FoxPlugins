using System;
using Addin;
using KKAPI.Utilities;
using MaterialEditorRework.Views;
using TexFac.Universal;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialEditorRework.CustomElements.ToggleContainers
{
    public class RenderSettingsContainer : ToggleContainerBase
    {
        private Dropdown dropdownShadows;
        private CheckboxM checkboxShadowsEnabled;
        private CheckboxM checkboxUpdateOffscreen;
        public RenderSettingsContainer(Vector2 size) : base(size)
        {
            checkboxShadowsEnabled = new CheckboxM
            {
                State = ((Renderer)Entry.Instance.propertyContentView.Target).receiveShadows
            };
            checkboxShadowsEnabled.OnValueChanged += (state) => ((Renderer)(Entry.Instance.propertyContentView.Target)).receiveShadows = state;
            dropdownShadows = new Dropdown(new [] { "Off", "On", "Double Sided", "Shadows Only" }, (int)((Renderer)Entry.Instance.propertyContentView.Target).shadowCastingMode);
            dropdownShadows.OnSelectionChanged += DropdownShadowsOnOnSelectionChanged;
        }
        private void DropdownShadowsOnOnSelectionChanged(int idx)
        {
            ((Renderer)Entry.Instance.propertyContentView.Target).shadowCastingMode = (ShadowCastingMode)idx;
        }

        public override void DrawHeader(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x + 16, rect.y + rect.height / 2 - 10, 20, 20), Icons.SettingsIcon);
            GUI.Label(new Rect(rect.x + 48, rect.y + rect.height / 2 - 10, 100, 20), "Render Settings", Styles.BoldBlack);
        }
        public override void DrawContent(Rect rect)
        {
            checkboxShadowsEnabled.Draw(new Rect(rect.x + 16, rect.y + 60, 14, 14));
            GUI.Label(new Rect(rect.x + 36, rect.y + 57, rect.width, 20), "Shadows Enabled", Styles.DefaultLabelBlack);

            if (Entry.Instance.propertyContentView.Target is SkinnedMeshRenderer)
            {
                if (checkboxUpdateOffscreen == null)
                {
                    checkboxUpdateOffscreen = new CheckboxM
                    {
                        State = ((SkinnedMeshRenderer)(Entry.Instance.propertyContentView.Target)).updateWhenOffscreen
                    };
                    checkboxUpdateOffscreen.OnValueChanged += (state) => ((SkinnedMeshRenderer)(Entry.Instance.propertyContentView.Target)).updateWhenOffscreen = state;
                }
                checkboxUpdateOffscreen.Draw(new Rect(rect.x + rect.width / 2 + 16, rect.y + 60, 14, 14));
                GUI.Label(new Rect(rect.x +  rect.width / 2 + 36, rect.y + 57, rect.width, 20), "Update Offscreen", Styles.DefaultLabelBlack);
            }

            GUI.Label(new Rect(rect.x + 16, rect.y + 4, rect.width, 20), "Shadow Casting", Styles.DefaultLabelBlack);
            dropdownShadows.Draw(new Rect(rect.x + 16, rect.y + 30, (rect.width - 32) / 2, 20));

        }
    }
}