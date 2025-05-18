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
			BepinAwake();
		}

		private Rect _windowRect;
		private Vector2 _scrollPosition;
		private string _searchText;
		private static int _tab;

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