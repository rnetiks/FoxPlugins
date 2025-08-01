using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace PoseLib.KKS
{
    [BepInPlugin(Constants.GUID, Constants.NAME, Constants.VERSION)]
    public class Entry : BaseUnityPlugin
    {
        private ConfigEntry<KeyboardShortcut> _openUIKey;
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