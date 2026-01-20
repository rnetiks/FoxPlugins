using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    [BepInPlugin(Constants.GUID, Constants.NAME, Constants.VERSION)]
    public class Entry : BaseUnityPlugin
    {
        private ConfigEntry<KeyboardShortcut> _openUIKey;
        public static ConfigEntry<string> _defaultName;

        public static ConfigEntry<int> _windowWidth;
        public static ConfigEntry<int> _windowHeight;
        

        private PoseLibraryManager _poseManager;
        private UIManager _uiManager;

        
        private void Awake()
        {
            InitializeConfiguration();
            InitializeManagers();
        }

        private void InitializeConfiguration()
        {
            _openUIKey = Config.Bind("General", "Open Window",
                new KeyboardShortcut(KeyCode.N, KeyCode.RightControl));
            _defaultName = Config.Bind("General", "Default Name", "PoseLib_${Date} ${Time}");
            _windowWidth = Config.Bind("UI", "width", 900, 
                new ConfigDescription("Sets the window width", new AcceptableValueRange<int>(900, 4096)));
            _windowHeight = Config.Bind("UI", "height", 400, new ConfigDescription("Sets the window height", new AcceptableValueRange<int>(400, 2048)));
        }
        
        private void InitializeManagers()
        {
            _poseManager = new PoseLibraryManager(Logger);
            _uiManager = new UIManager(_poseManager, Logger);
        }

        private void Update()
        {
            HandleInput();
            _uiManager.Update();
        }

        private void HandleInput()
        {
            if (_openUIKey.Value.IsDown())
                _uiManager.ToggleUI();
        }

        private void OnGUI()
        {
            _uiManager.OnGUI();
        }

        private void OnDestroy()
        {
            _uiManager?.Dispose();
            _poseManager?.Dispose();
        }
    }
}