using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;

namespace Search.KKS
{
    [BepInPlugin(GUID, "Search", "1.0.0")]
    public partial class Search : BaseUnityPlugin
    {
        const string GUID = "org.fox.search";
        private List<SearchCommand> callbacks;
        private ConfigEntry<KeyboardShortcut> toggleUI;
        private bool showUI;
        public static Search Instance;

        private void Awake()
        {
            Instance = this;
            toggleUI = Config.Bind("General", "Toggle UI", new KeyboardShortcut(KeyCode.None));
            callbacks = new List<SearchCommand>();
        }


        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private string _searchText;

        private void Update()
        {
            var height = (float)(Screen.height * 0.3);
            var width = (float)(Screen.width * 0.3);
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
                return;

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
            _searchText = GUILayout.TextField(_searchText);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (var command in callbacks.Where(
                         command => command.command.ToLower().Contains(_searchText.ToLower())))
            {
                GUILayout.BeginHorizontal();

                var text = !IsNullOrWhiteSpace(command.description)
                    ? $"{command.command}: {command.description}"
                    : $"{command.command}";

                if (GUILayout.Button(text, GUILayout.ExpandWidth(true)))
                {
                    command.callback();
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
        public bool AddCommand(SearchCommand action)
        {
            Logger.LogError($"Add Command");
            if (callbacks == null || action.callback == null || IsNullOrWhiteSpace(action.command))
            {
                return false;
            }

            if (callbacks.Any(e => e.command == action.command))
                return false;

            callbacks.Add(action);
            return true;
        }

        [UsedImplicitly]
        public bool RemoveCommand(SearchCommand action)
        {
            if (callbacks == null || action.callback == null)
                return false;
            return callbacks.Any(e => e.command == action.command) && callbacks.Remove(action);
        }
    }
}