using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using DefaultNamespace;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MaterialEditorRework.CustomElements;
using MaterialEditorRework.CustomElements.ToggleContainers;
using MaterialEditorRework.Views;
using MaterialEditorRework.Views.Property;
using Studio;
using UnityEngine;

namespace MaterialEditorRework
{
	[BepInPlugin(GUID, NAME, VERSION)]
	public class Entry : BaseUnityPlugin
	{
		const string GUID = "org.fox.materialeditorrework";
		const string NAME = "MaterialEditorRework";
		const string VERSION = "1.0.0.0";

		public static ConfigEntry<float> borderSize;
		public static IniFile iniFile = new IniFile(Paths.BepInExConfigPath + "/MaterialEditorRework.ini");

		public static Entry Instance;

		private void Awake()
		{
			borderSize = Config.Bind("General", "Border Size", 2f, new ConfigDescription("A test", new AcceptableValueRange<float>(0, 10), null));
			backgroundStyle = new GUIStyle();
			backgroundStyle.normal.background = null;

			Instance = this;


			StudioSaveLoadApi.ObjectsSelected += StudioSaveLoadApiOnObjectsSelected;
			StudioSaveLoadApi.ObjectDeleted += StudioSaveLoadApiOnObjectDeleted;
		}
		private void StudioSaveLoadApiOnObjectDeleted(object sender, ObjectDeletedEventArgs e)
		{
			CleanWindow();
		}

		private void CleanWindow()
		{
			listHeaderView._dropdown.SetOptions(new string[0]);
			listTreeviewView.CleanItems();
			propertyContentView.Target = null;
		}
		private void StudioSaveLoadApiOnObjectsSelected(object sender, ObjectsSelectedEventArgs e)
		{
			CleanWindow();

			var objectCtrlInfos = KKAPI.Studio.StudioAPI.GetSelectedObjects();
			if (objectCtrlInfos.Any())
			{
				if (objectCtrlInfos.First() is OCIChar charCtrlInfo)
				{

					listHeaderView.UpdateDropdown(charCtrlInfo.charInfo);

					var children = charCtrlInfo.charInfo.objBody.GetComponentsInChildren<Renderer>(true);
					List<TreeViewItemParent> items = new List<TreeViewItemParent>();
					foreach (var renderer in children.Concat(new[] { charCtrlInfo.charInfo.rendFace }).Concat(charCtrlInfo.charInfo.objHead.GetComponentsInChildren<Renderer>()))
					{
						var currentItem = new TreeViewItemParent(new Vector2(303, 101))
						{
							Renderer = renderer
						};

						List<TreeViewItemChild> childrenItems = new List<TreeViewItemChild>();
						foreach (var material in renderer.materials)
						{
							childrenItems.Add(new TreeViewItemChild() { Material = material, Parent = currentItem });
						}
						currentItem.Children = childrenItems.ToArray();

						items.Add(currentItem);
					}
					listTreeviewView.AddItems(items.ToArray());
				}

				if (objectCtrlInfos.First() is OCIItem itemCtrlInfo)
				{
					List<TreeViewItemParent> items = new List<TreeViewItemParent>();
					foreach (var renderer in itemCtrlInfo.arrayRender)
					{
						var currentItem = new TreeViewItemParent(new Vector2(303, 101))
						{
							Renderer = renderer
						};
						items.Add(currentItem);
					}
					listTreeviewView.AddItems(items.ToArray());
				}
			}
		}

		private void OnDestroy()
		{
			StudioSaveLoadApi.ObjectsSelected -= StudioSaveLoadApiOnObjectsSelected;
		}

		private GameObject[] _types;

		private GUIStyle backgroundStyle;

		public ListHeaderView listHeaderView = new ListHeaderView();
		public ListTreeviewView listTreeviewView = new ListTreeviewView();
		public PropertyHeaderView propertyHeaderView = new PropertyHeaderView();
		public PropertyContentView propertyContentView = new PropertyContentView();

		private Rect _windowPosition = new Rect(200, 200, 1050, 600);
		private Rect NoneSelectedRect = new Rect(320, 0, 730, 600);
		private GUIContent NoneSelectedContent = new GUIContent("No Material Selected");
		private void OnGUI()
		{
			_windowPosition = GUI.Window(421, _windowPosition, Func, string.Empty, backgroundStyle);
			if (_windowPosition.Contains(Event.current.mousePosition))
				Input.ResetInputAxes();
		}

		private Rect listtreeviewRect = new Rect(0, 111, 320, 489);
		private Rect listHeaderRect = new Rect(0, 0, 320, 111);

		private void Func(int id)
		{
			listTreeviewView.Draw(listtreeviewRect);
			listHeaderView.Draw(listHeaderRect);
			GUI.DrawTexture(NoneSelectedRect, TextureCache.GetOrCreateSolid(Color.white));
			if (listTreeviewView.AnySelected())
			{
				propertyContentView.Draw(new Rect(320, 98, 730, 502));
				propertyHeaderView.Draw(new Rect(320, 0, _windowPosition.width - 320, 98));
			}
			else
			{

				var calcSize = Styles.DefaultLabelCenteredBlack.CalcSize(NoneSelectedContent);
				var position = new Rect(
					NoneSelectedRect.x - 10 + NoneSelectedRect.width / 2 - calcSize.x / 1.5f,
					NoneSelectedRect.y - 5 + NoneSelectedRect.height / 2 - calcSize.y / 2,
					calcSize.x + 20, calcSize.y + 10
				);
				GUI.Label(position, NoneSelectedContent, Styles.DefaultLabelCenteredBlack);
			}
			GUI.DragWindow();

			Renderer r;
		}
	}
}