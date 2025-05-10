using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Search.KKS
{
	[BepInPlugin(GUID, "Search", "1.0.0")]
	public partial class Search : BaseUnityPlugin
	{
		const string GUID = "org.fox.search";
		//Todo Not thread safe
		private Dictionary<object, ISearchCommand> commands;
		private ConfigEntry<KeyboardShortcut> toggleUI;

		private bool showUI;

		public static Search Instance;

		private void Awake()
		{
			Instance = this;
			toggleUI = Config.Bind("General", "Toggle UI", new KeyboardShortcut(KeyCode.None));
			commands = new Dictionary<object, ISearchCommand>();
		}

		private Rect _windowRect;
		private Vector2 _scrollPosition;
		private string _searchText;
		private static int _tab;

		private bool _initializedBepinex;

		private void Update()
		{
			var height = (float)(Screen.height * 0.3);
			var width = (float)(Screen.width * 0.3);

			if (Event.current?.mousePosition == null)
			{
				return;
			}

			var mousePos = Event.current.mousePosition;
			if (showUI && !_windowRect.Contains(mousePos))
			{
				GUI.FocusControl(null);
				GUI.UnfocusWindow();
				showUI = false;
				return;
			}

			if (toggleUI.Value.IsDown() && !showUI)
			{
				if (_initializedBepinex != true)
				{
					_initializedBepinex = true;
					BepinAwake();
				}
				_searchText = string.Empty;
				_windowRect = new Rect(mousePos.x - width / 2, mousePos.y - height / 2, width, height);
				showUI = true;
			}
		}

		private void OnGUI()
		{
			if (!showUI)
			{
				return;
			}

			if (_windowRect.Contains(Event.current.mousePosition))
			{
				Input.ResetInputAxes();
			}

			_windowRect = GUILayout.Window(54098, _windowRect, WindowFunc, "Search");
		}

		bool IsNullOrWhiteSpace(string value)
		{
			return string.IsNullOrEmpty(value) || value.All(char.IsWhiteSpace);
		}

		private void WindowFunc(int id)
		{
			_tab = GUILayout.Toolbar(_tab, new[]
			{
				"Commands",
				"BepInEx",
			});

			_searchText = GUILayout.TextField(_searchText);
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

			var collection = commands.Values;

			foreach (var command in collection.Where(
						 command => command.Name.ToLower().Contains(_searchText.ToLower()))
						 .OrderBy(d => d.Name))
			{
				if (_tab == 0 && !(command is SearchCommand))
				{
					continue;
				}

				if (command is BepInExCommand hotkeyInfo)
				{
					if (hotkeyInfo.FramesSinceHit > Application.targetFrameRate)
					{
						continue;
					}
				}

				GUILayout.BeginHorizontal();

				var text = !IsNullOrWhiteSpace(command.Description)
					? $"{command.Name}: {command.Description}"
					: $"{command.Name}";

				if (GUILayout.Button(text, GUILayout.ExpandWidth(true)))
				{
					command.Execute();
					showUI = false;
					GUILayout.EndHorizontal();
					GUILayout.EndScrollView();
					return;
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
			GUI.DragWindow();
		}

		[UsedImplicitly]
		public bool AddCommand(ISearchCommand action)
		{
			if (commands == null)
			{
				return false;
			}

			var hash = action.GetHashCode();

			if (commands.ContainsKey(hash))
			{
				return false;
			}

			commands[hash] = action;
			return true;
		}

		[UsedImplicitly]
		public bool RemoveCommand(ISearchCommand action)
		{
			if (commands == null)
			{
				return false;
			}

			var hash = action.GetHashCode();
			return commands.Remove(hash);
		}
	}
}