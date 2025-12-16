using System;
using System.Collections.Generic;
using System.Linq;
using MaterialEditorRework.CustomElements;
using TexFac.Universal;
using UnityEngine;

namespace MaterialEditorRework.Views.Property
{
	public class PropertyHeaderView : BaseElementView
	{
		private static Texture2D _buttonTexture;

		private Dropdown _dropdown;
		private object _target;
		public override void Draw(Rect rect)
		{
			if (_buttonTexture == null)
			{
				_buttonTexture = TextureFactory.SolidColor(162, 40, new Color(0.95f, 0.96f, 0.96f)).BorderRadius(10, aliasDistance: 0.5f);
			}

			GUI.DrawTexture(rect, TextureCache.GetOrCreateSolid(Color.white));
			var borderSizeValue = Entry.borderSize.Value;
			GUI.DrawTexture(new Rect(rect.x, rect.height - borderSizeValue, rect.width, borderSizeValue), TextureCache.GetOrCreateSolid(new Color(0.9f, 0.91f, 0.92f)));
			if (_target is Renderer renderer)
			{


				GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 18, 16, 16), Icons.BoxIcon);
				GUI.Label(new Rect(rect.x + 36, rect.y + 16, rect.width - 32, 20), renderer.name, Styles.DefaultLabelBlack);
				GUI.Label(new Rect(rect.x + 16, rect.y + 44, rect.width - 32, 20), "Renderer Settings", Styles.DefaultLabelBlack);

			}
			else if (_target is Material material)
			{
				GUI.DrawTexture(new Rect(rect.x + 16, rect.y + 18, 16, 16), Icons.LayerIcon);
				GUI.Label(new Rect(rect.x + 36, rect.y + 16, rect.width - 32, 20), material.name.Replace("(Instance)", "").Trim(), Styles.DefaultLabelBlack);
				GUI.Label(new Rect(rect.x + 16, rect.y + 44, rect.width - 32, 20), "Material Settings", Styles.DefaultLabelBlack);
				if (_dropdown == null)
				{
					_dropdown = new Dropdown();

					List<Shader> shaders = new List<Shader>();
					shaders.AddRange(Resources.FindObjectsOfTypeAll<Shader>());

					// availableShaders = shaders.Distinct(x => x.name).ToArray();
					availableShaders = shaders.OrderBy(x => x.name).ToArray();
					_dropdown.SetOptions(availableShaders.Select(x => x.name).ToArray(), Array.IndexOf(availableShaders, material.shader));
					_dropdown.SetMaxContainerSize(new Vector2(150, 200));

					_dropdown.OnSelectionChanged += DropdownOnOnSelectionChanged;
				}

				_dropdown.Draw(new Rect(rect.x + 16, rect.y + rect.height - 30, 250, 20));
			}



			/*GUI.DrawTexture(new Rect(rect.x + rect.width - 162 - 10, rect.y + rect.height / 2 - 20, 162, 40), _buttonTexture);
			GUI.DrawTexture(new Rect(rect.x + rect.width - 324 - 20, rect.y + rect.height / 2 - 20, 162, 40), _buttonTexture);*/
		}
		private void DropdownOnOnSelectionChanged(int obj)
		{
			((Material)_target).shader = availableShaders[obj];
		}

		private Shader[] availableShaders;

		public void SetItem(Renderer renderer)
		{
			_target = renderer;
		}

		public void SetItem(Material material)
		{
			_target = material;
			_dropdown = null;
		}
	}
}