using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace KK
{
    /// <summary>
    /// Click Is Overrated - Manages keyboard shortcuts for object selection in Studio
    /// </summary>
    internal class CIO : MonoBehaviour
    {
        #region Constants
        private const int WINDOW_ID = 340987;
        private const float WINDOW_WIDTH_RATIO = 0.2f;
        private const float WINDOW_HEIGHT_RATIO = 0.4f;
        private const float WINDOW_X_RATIO = 0.1f;
        private const float WINDOW_Y_RATIO = 0.1f;
        private const float DELETE_BUTTON_WIDTH = 20f;
        #endregion

        #region Fields
        // Keyboard shortcut bindings
        public static Dictionary<KeyboardShortcut, IEnumerable<ObjectCtrlInfo>> binds;

        // UI state
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private bool _isGuiEnabled;

        // Key detection for shortcut creation
        private readonly List<KeyCode> _currentlyHeldKeys = new List<KeyCode>();
        private readonly List<KeyCode> _previouslyHeldKeys = new List<KeyCode>();
        private bool _isSelectingKey;
        private IEnumerable<ObjectCtrlInfo> _tempObjectsForBinding;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeBindings();
            InitializeWindow();
        }

        private void Update()
        {
            HandleGuiToggle();

            if (_isSelectingKey)
            {
                ProcessKeySelection();
            }
            else
            {
                ProcessShortcutActivation();
                HandleInputBlocking();
            }
        }

        private void OnGUI()
        {
            if (_isSelectingKey)
            {
                DrawKeySelectionOverlay();
            }

            if (_isGuiEnabled)
            {
                _windowRect = GUILayout.Window(WINDOW_ID, _windowRect, DrawWindow, "Click Is Overrated");
            }
        }
        #endregion

        #region Initialization
        private void InitializeBindings()
        {
            binds = new Dictionary<KeyboardShortcut, IEnumerable<ObjectCtrlInfo>>();
        }

        private void InitializeWindow()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            _windowRect = new Rect(
                screenWidth * WINDOW_X_RATIO,
                screenHeight * WINDOW_Y_RATIO,
                screenWidth * WINDOW_WIDTH_RATIO,
                screenHeight * WINDOW_HEIGHT_RATIO
            );

            _scrollPosition = Vector2.zero;
        }
        #endregion

        #region GUI Drawing
        private void DrawKeySelectionOverlay()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), "");
        }

        private void DrawWindow(int id)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            DrawBindingsList();
            GUILayout.EndScrollView();

            DrawAddBindButton();
            GUI.DragWindow();
        }

        private void DrawBindingsList()
        {
            // Create a copy to avoid modification during iteration
            var bindingsList = binds.ToList();

            foreach (var binding in bindingsList)
            {
                try
                {
                    DrawBindingEntry(binding);
                }
                catch (Exception)
                {
                    // Silently handle exceptions for invalid bindings
                }
            }
        }

        private void DrawBindingEntry(KeyValuePair<KeyboardShortcut, IEnumerable<ObjectCtrlInfo>> binding)
        {
            string displayText = FormatBindingText(binding.Key, binding.Value);

            GUILayout.BeginHorizontal();
            GUILayout.Button(displayText);

            if (GUILayout.Button("X", GUILayout.Width(DELETE_BUTTON_WIDTH)))
            {
                binds.Remove(binding.Key);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                return;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawAddBindButton()
        {
            var selectedObjects = StudioAPI.GetSelectedObjects().ToArray();

            if (GUILayout.Button("Add Bind") && selectedObjects.Any())
            {
                _tempObjectsForBinding = selectedObjects;
                _isSelectingKey = true;
            }
        }
        #endregion

        #region Input Handling
        private void HandleGuiToggle()
        {
            if (Entry.enableGUI.Value.IsDown())
            {
                _isGuiEnabled = !_isGuiEnabled;
            }
        }

        private void ProcessKeySelection()
        {
            UpdateHeldKeys();

            if (HasAnyKeyBeenReleased())
            {
                CreateShortcutFromHeldKeys(_previouslyHeldKeys);
                EndKeySelection();
            }
        }

        private void UpdateHeldKeys()
        {
            _previouslyHeldKeys.Clear();
            _previouslyHeldKeys.AddRange(_currentlyHeldKeys);

            _currentlyHeldKeys.Clear();

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityInput.Current.GetKey(key))
                {
                    _currentlyHeldKeys.Add(key);
                }
            }
        }

        private bool HasAnyKeyBeenReleased()
        {
            return _previouslyHeldKeys.Any(key => !_currentlyHeldKeys.Contains(key));
        }

        private void EndKeySelection()
        {
            _isSelectingKey = false;
            _currentlyHeldKeys.Clear();
        }

        private void ProcessShortcutActivation()
        {
            var selectedObjects = StudioAPI.GetSelectedObjects();

            foreach (var binding in binds)
            {
                if (IsShortcutPressed(binding.Key))
                {
                    ToggleObjectSelection(binding.Value, selectedObjects);
                    return;
                }
            }
        }

        private void HandleInputBlocking()
        {
            if (_isGuiEnabled && _windowRect.Contains(Event.current.mousePosition))
            {
                Input.ResetInputAxes();
            }
        }
        #endregion

        #region Shortcut Detection
        public bool IsShortcutPressed(KeyboardShortcut shortcut)
        {
            // Special handling for left control or joystick inputs
            if (!UnityInput.Current.GetKey(KeyCode.LeftControl) && 
                !shortcut.MainKey.ToString().ToLower().Contains("joystick"))
            {
                return shortcut.IsDown();
            }

            KeyCode mainKey = shortcut.MainKey;
            if (mainKey == KeyCode.None)
            {
                return false;
            }

            var allKeys = SanitizeKeyArray(mainKey, shortcut.Modifiers);

            return UnityInput.Current.GetKeyDown(mainKey) && 
                   AreAllModifiersPressed(allKeys, mainKey);
        }

        private bool AreAllModifiersPressed(KeyCode[] allKeys, KeyCode mainKey)
        {
            return allKeys.All(key => key == mainKey || UnityInput.Current.GetKey(key));
        }

        private static KeyCode[] SanitizeKeyArray(KeyCode mainKey, IEnumerable<KeyCode> modifiers)
        {
            var allKeys = new[] { mainKey }.Concat(modifiers).ToArray();

            if (allKeys.Length == 0 || allKeys[0] == KeyCode.None)
            {
                return new KeyCode[1];
            }

            return new[] { allKeys[0] }
                .Concat(allKeys.Skip(1)
                    .Distinct()
                    .Where(key => key != allKeys[0])
                    .OrderBy(key => (int)key))
                .ToArray();
        }
        #endregion

        #region Object Selection
        private void ToggleObjectSelection(IEnumerable<ObjectCtrlInfo> boundObjects, IEnumerable<ObjectCtrlInfo> currentSelection)
        {
            if (AreObjectsSame(boundObjects, currentSelection))
            {
                ClearSelection();
            }
            else
            {
                SelectObjects(boundObjects);
            }
        }

        private bool AreObjectsSame(IEnumerable<ObjectCtrlInfo> objects1, IEnumerable<ObjectCtrlInfo> objects2)
        {
            string names1 = string.Join("", objects1.Select(GetObjectName));
            string names2 = string.Join("", objects2.Select(GetObjectName));
            return names1 == names2;
        }

        private string GetObjectName(ObjectCtrlInfo obj)
        {
            return obj?.guideObject?.transformTarget?.name ?? "";
        }

        private void ClearSelection()
        {
            var treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
            treeNodeCtrl.selectNode = null;
        }

        private void SelectObjects(IEnumerable<ObjectCtrlInfo> objects)
        {
            var treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
            var guideObjectManager = Singleton<GuideObjectManager>.Instance;

            treeNodeCtrl.selectNode = null;
            guideObjectManager.selectObject = null;

            bool isFirstObject = true;

            foreach (var obj in objects)
            {
                treeNodeCtrl.AddSelectNode(obj.treeNodeObject, true);
                guideObjectManager.AddObject(obj.guideObject, isFirstObject);

                if (isFirstObject)
                {
                    isFirstObject = false;
                }
            }
        }
        #endregion

        #region Shortcut Creation
        private void CreateShortcutFromHeldKeys(List<KeyCode> keys)
        {
            if (!keys.Any())
            {
                return;
            }

            var (mainKey, modifiers) = ExtractMainKeyAndModifiers(keys);

            if (mainKey == KeyCode.None)
            {
                return;
            }

            var shortcut = new KeyboardShortcut(mainKey, modifiers.ToArray());

            if (!binds.ContainsKey(shortcut))
            {
                binds.Add(shortcut, _tempObjectsForBinding);
            }
        }

        private (KeyCode mainKey, List<KeyCode> modifiers) ExtractMainKeyAndModifiers(List<KeyCode> keys)
        {
            var modifiers = new List<KeyCode>();
            KeyCode mainKey = KeyCode.None;

            foreach (var key in keys)
            {
                if (IsModifierKey(key))
                {
                    modifiers.Add(key);
                }
                else
                {
                    mainKey = key;
                }
            }

            // If no main key was found but we have modifiers, use the last modifier as main key
            if (mainKey == KeyCode.None && modifiers.Any())
            {
                mainKey = modifiers.Last();
                modifiers.RemoveAt(modifiers.Count - 1);
            }

            return (mainKey, modifiers);
        }

        private bool IsModifierKey(KeyCode key)
        {
            return key == KeyCode.LeftControl ||
                   key == KeyCode.RightControl ||
                   key == KeyCode.LeftShift ||
                   key == KeyCode.RightShift ||
                   key == KeyCode.LeftAlt ||
                   key == KeyCode.RightAlt;
        }
        #endregion

        #region Formatting
        private string FormatBindingText(KeyboardShortcut shortcut, IEnumerable<ObjectCtrlInfo> objects)
        {
            string keyText = FormatShortcutText(shortcut);
            string objectsText = FormatObjectsText(objects);
            return $"{keyText}: {objectsText}";
        }

        private string FormatShortcutText(KeyboardShortcut shortcut)
        {
            var modifiers = shortcut.Modifiers.ToArray();

            if (modifiers.Length == 0)
            {
                return shortcut.MainKey.ToString();
            }

            string modifiersText = string.Join(" + ", modifiers.Select(m => m.ToString()));
            return $"{modifiersText} + {shortcut.MainKey}";
        }

        private string FormatObjectsText(IEnumerable<ObjectCtrlInfo> objects)
        {
            var objectsArray = objects.ToArray();

            if (objectsArray.Length <= 1)
            {
                var singleObject = objectsArray.FirstOrDefault();
                return GetObjectName(singleObject) ?? "null";
            }

            return $"[{objectsArray.Length} objects]";
        }
        #endregion

        #region Properties
        [Obsolete("Use _isSelectingKey field instead")]
        public bool DoSelectKey
        {
            get => _isSelectingKey;
            set => _isSelectingKey = value;
        }
        #endregion
    }
}