using System.Collections.Generic;
using System.Linq;
using KKAPI.Studio;
using MaterialEditorRework.CustomElements;
using Studio;
using UnityEngine;

namespace MaterialEditorRework.Views
{
	public class ListHeaderView : BaseElementView
	{
		private const string HEADER_TITLE = "Material Editor";
		private GUIStyle _headerTitleStyle;
		private GUIStyle _textStyle;
		public Dropdown _dropdown = new Dropdown();

		public override void Initialize()
		{
			_headerTitleStyle = new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold,
				fontSize = 18,
				normal =
				{
					textColor = Color.black
				}
			};
			_textStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				normal =
				{
					textColor = new Color(0.61f, 0.64f, 0.69f)
				},
				alignment = TextAnchor.MiddleLeft
			};

			IsInitialized = true;

			_dropdown.OnSelectionChanged += DropdownOnOnSelectionChanged;
		}
		private void DropdownOnOnSelectionChanged(int index)
		{

		}

		public void CleanTypeDropdown()
		{
			_dropdown.SetOptions(new string[0]);
		}

		public void UpdateDropdown(ChaControl chaCtrl)
		{
			List<string> options = new List<string>();
			options.Add("Body");

			var clothes = chaCtrl.objClothes;
			for (int index = 0; index < clothes.Length; index++)
			{
				if (clothes[index] != null && clothes[index].GetComponentInChildren<ChaClothesComponent>() != null)
				{
					options.Add($"Clothes {ClothesIndexToString(index)}");
				}
			}

			var hair = chaCtrl._objHair;
			for (var i = 0; i < hair.Length; i++)
			{
				if (hair[i] != null && hair[i].GetComponent<ChaCustomHairComponent>() != null)
				{
					options.Add(hair[i].name);
				}
			}

			var accessories = chaCtrl._objAccessory;
			for (var i = 0; i < accessories.Length; i++)
			{
				if (accessories[i] != null)
				{
					string optionName = $"Accessory {chaCtrl.infoAccessory[i].Name}";
					options.Add(optionName);
				}
			}
			_dropdown = new Dropdown(options.ToArray());
			_dropdown.SetMaxContainerSize(new Vector2(287, 200));
		}

		private string ClothesIndexToString(int index)
		{
			switch (index)
			{
				case 0:
					return "Top";
				case 1:
					return "Bottom";
				case 2:
					return "Bra";
				case 3:
					return "Underwear";
				case 4:
					return "Gloves";
				case 5:
					return "Pantyhose";
				case 6:
					return "Legwear";
				case 7:
					return "Indoor Shoes";
				case 8:
					return "Outdoor Shoes";
				default:
					return "";
			}
		}

		private string HairIndexToString(int index)
		{
			switch (index)
			{
				case 0:
					return "Back";
				case 1:
					return "Front";
				case 2:
					return "Side";
				case 3:
					return "Extension";
				default:
					return "";
			}
		}

		Textbox _textbox = new Textbox();
		private string text = string.Empty;
		public override void Draw(Rect rect)
		{
			if (!IsInitialized) Initialize();
			float borderSizeValue = Entry.borderSize.Value;
			GUI.DrawTexture(rect, TextureCache.GetOrCreateSolid(Color.white));
			GUI.DrawTexture(new Rect(0, rect.height - borderSizeValue, rect.width, borderSizeValue), TextureCache.GetOrCreateSolid(new Color(0.9f, 0.91f, 0.92f)));
			GUI.DrawTexture(new Rect(rect.width - borderSizeValue, 0, borderSizeValue, rect.height), TextureCache.GetOrCreateSolid(new Color(0.9f, 0.91f, 0.92f)));
			GUI.Label(new Rect(16, 16, 126, 100), HEADER_TITLE, _headerTitleStyle);
			if (_dropdown.IsOpen)
				GUI.enabled = false;
			var tmpText = _textbox.Draw(new Rect(16, 56, 287, 38), text, "Filter renderer and materials...");
			if (tmpText != text)
			{
				text = tmpText;
				Entry.Instance.listTreeviewView.SetFilter(text);
			}
			GUI.enabled = true;
			if (_dropdown.Items.Length > 0)
				_dropdown.Draw(new Rect(150f, 16, 156, 26));


			// Search box
		}
	}
}