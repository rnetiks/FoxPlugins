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

        private PoseLibraryManager _poseManager;
        private UIManager _uiManager;

        
        private void Awake()
        {
            InitializeConfiguration();
            InitializeManagers();

            OCIChar _char;
        }

        private void InitializeConfiguration()
        {
            _openUIKey = Config.Bind("General", "Open Window",
                new KeyboardShortcut(KeyCode.N, KeyCode.RightControl));
            _defaultName = Config.Bind("General", "Default Name", "PoseLib_${Date} ${Time}");
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