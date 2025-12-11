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
		private Checkbox checkboxShadowsEnabled;
		private Checkbox checkboxUpdateOffscreen;
		public RenderSettingsContainer(Vector2 size) : base(size)
		{
			checkboxShadowsEnabled = new Checkbox();
			checkboxShadowsEnabled.State = ((Renderer)(Entry.Instance.propertyContentView.Target)).receiveShadows;
			checkboxShadowsEnabled.OnValueChanged += (state) => ((Renderer)(Entry.Instance.propertyContentView.Target)).receiveShadows = state;
			dropdownShadows = new Dropdown(new [] { "On", "Off", "Double Sided", "Shadows Only" });
			switch (((Renderer)(Entry.Instance.propertyContentView.Target)).shadowCastingMode)
			{

				case ShadowCastingMode.On:
					dropdownShadows.SelectIndex(0);
					break;
				case ShadowCastingMode.Off:
					dropdownShadows.SelectIndex(1);
					break;
				case ShadowCastingMode.TwoSided:
					dropdownShadows.SelectIndex(2);
					break;
				case ShadowCastingMode.ShadowsOnly:
					dropdownShadows.SelectIndex(3);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			dropdownShadows.OnSelectionChanged += DropdownShadowsOnOnSelectionChanged;
		}
		private void DropdownShadowsOnOnSelectionChanged(int obj)
		{
			switch (obj)
			{
				case 0:
					((Renderer)(Entry.Instance.propertyContentView.Target)).shadowCastingMode = ShadowCastingMode.On;
					break;
				case 1:
					((Renderer)(Entry.Instance.propertyContentView.Target)).shadowCastingMode = ShadowCastingMode.Off;
					break;
				case 2:
					((Renderer)(Entry.Instance.propertyContentView.Target)).shadowCastingMode = ShadowCastingMode.TwoSided;
					break;
				case 3:
					((Renderer)(Entry.Instance.propertyContentView.Target)).shadowCastingMode = ShadowCastingMode.ShadowsOnly;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
			}
		}

		public override void DrawHeader(Rect rect)
		{
			if (rect.Contains(Event.current.mousePosition))
				GUI.color = new Color(0.73f, 0.73f, 0.73f);

			if (_isOpen)
			{
				GUI.DrawTexture(rect, _headerTextureOpen);
				GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), TextureCache.GetOrCreateSolid(new Color(0.95f, 0.96f, 0.96f)));
			}
			else
				GUI.DrawTexture(rect, _headerTextureClose);
			GUI.DrawTexture(new Rect(rect.x + 16, rect.y + rect.height / 2 - 10, 20, 20), Icons.SettingsIcon);
			GUI.Label(new Rect(rect.x + 48, rect.y + rect.height / 2 - 10, 100, 20), "Render Settings", Styles.BoldBlack);

			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
				_isOpen = !_isOpen;
			GUI.color = Color.white;
		}
		public override void DrawContent(Rect rect)
		{
			GUI.DrawTexture(rect, TextureFactory.SolidColor(1, 1, Color.white));
			GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, 10), _footerTexture);


			checkboxShadowsEnabled.Draw(new Rect(rect.x + 16, rect.y + 60, 14, 14));
			GUI.Label(new Rect(rect.x + 36, rect.y + 57, rect.width, 20), "Shadows Enabled", Styles.DefaultLabelBlack);

			if (Entry.Instance.propertyContentView.Target is SkinnedMeshRenderer)
			{
				if (checkboxUpdateOffscreen == null)
				{
					checkboxUpdateOffscreen = new Checkbox();
					checkboxUpdateOffscreen.State = ((SkinnedMeshRenderer)(Entry.Instance.propertyContentView.Target)).updateWhenOffscreen;
					checkboxUpdateOffscreen.OnValueChanged += (state) => ((SkinnedMeshRenderer)(Entry.Instance.propertyContentView.Target)).updateWhenOffscreen = state;
				}
				checkboxUpdateOffscreen.Draw(new Rect(rect.x + rect.width / 2 + 16, rect.y + 60, 14, 14));
				GUI.Label(new Rect(rect.x +  rect.width / 2 + 36, rect.y + 57, rect.width, 20), "Update Offscreen", Styles.DefaultLabelBlack);
			}

			GUI.Label(new Rect(rect.x + 16, rect.y + 4, rect.width, 20), "Shadow Casting", Styles.DefaultLabelBlack);
			dropdownShadows.Draw(new Rect(rect.x + 16, rect.y + 30, (rect.width - 32) / 2, 20));

		}

		public override int GetHeight()
		{
			throw new System.NotImplementedException();
		}
	}
}