using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using UnityEngine;

namespace Search
{
    [BepInPlugin(GUID, "Search", "2.0.0")]
    public partial class Search : BaseUnityPlugin
    {
        public const string GUID = "org.fox.search";

        private const string SearchFieldControlName = "SearchPaletteField";

        public static Search Instance;
        internal List<ISearchCommand> _filteredResults = new List<ISearchCommand>();

        private bool _needsFocus;
        internal string _searchText = string.Empty;
        internal int _selectedIndex;

        internal Dictionary<object, ISearchCommand> commands;
        internal ShortcutManager shortcuts;

        internal bool showUI;
        private ConfigEntry<KeyboardShortcut> toggleUI;

        private void Awake()
        {
            Instance = this;
            toggleUI = Config.Bind("General", "Toggle UI", new KeyboardShortcut(KeyCode.None));
            commands = new Dictionary<object, ISearchCommand>();
            shortcuts = new ShortcutManager(Config);
            BepinAwake();
        }

        private void Update()
        {
            if (toggleUI.Value.IsDown() && !showUI)
            {
                _searchText = string.Empty;
                _selectedIndex = 0;
                _needsFocus = true;
                showUI = true;
            }

            shortcuts.PollShortcuts(commands);
        }

        private void OnGUI()
        {
            if (!showUI)
                return;

            var evt = Event.current;

            if (shortcuts.IsBinding)
            {
                if (shortcuts.TryCaptureBinding(evt))
                    return;
                DrawPalette();
                return;
            }

            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Escape:
                        ClosePalette();
                        evt.Use();
                        return;

                    case KeyCode.DownArrow:
                        _selectedIndex++;
                        if (_selectedIndex >= _filteredResults.Count)
                            _selectedIndex = 0;
                        evt.Use();
                        break;

                    case KeyCode.UpArrow:
                        _selectedIndex--;
                        if (_selectedIndex < 0)
                            _selectedIndex = _filteredResults.Count > 0 ? _filteredResults.Count - 1 : 0;
                        evt.Use();
                        break;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (_selectedIndex >= 0 && _selectedIndex < _filteredResults.Count)
                        {
                            var cmd = _filteredResults[_selectedIndex];
                            FuzzySearch.RecordUsage(cmd.Name);
                            cmd.Execute();
                            ClosePalette();
                            evt.Use();
                            return;
                        }
                        break;
                }
            }

            if (evt.type == EventType.MouseDown)
            {
                var paletteRect = PaletteRect;
                if (!paletteRect.Contains(evt.mousePosition))
                {
                    ClosePalette();
                    evt.Use();
                    return;
                }
            }

            Input.ResetInputAxes();

            DrawPalette();
        }

        private void ClosePalette()
        {
            showUI = false;
            GUI.FocusControl(null);
            GUI.UnfocusWindow();
        }

        [UsedImplicitly]
        public bool AddCommand(ISearchCommand action)
        {
            if (commands == null)
                return false;

            int hash = action.GetHashCode();
            if (commands.ContainsKey(hash))
                return false;

            commands[hash] = action;
            return true;
        }

        [UsedImplicitly]
        public bool RemoveCommand(ISearchCommand action)
        {
            if (commands == null)
                return false;

            return commands.Remove(action.GetHashCode());
        }
    }
}