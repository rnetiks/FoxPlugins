using System;
using System.Collections.Generic;
using System.Reflection;
using alphaShot;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Compositor.KK;
using HarmonyLib;
using Screencap;
using UnityEngine;

namespace DefaultNamespace
{
    [ BepInProcess("CharaStudio") ]
    [ BepInPlugin(GUID, NAME, VERSION) ]
    [ BepInDependency(ScreenshotManager.GUID) ]
    public class Entry : BaseUnityPlugin
    {
        const string GUID = "com.fox.compositor";
        const string NAME = "Compositor";
        const string VERSION = "1.0.0";

        private static ConfigEntry<int> _maxCacheSize;
        private ConfigEntry<KeyboardShortcut> _switchUI;
        private Harmony _harmony;
        public static ManualLogSource Logger;

        private Camera _camera;
        private Studio.CameraControl _cameraControl;
        private CompositorManager _compositorManager;
        private CompositorRenderer _renderer;

        public static List<Type> AvailableNodes = new List<Type>();

        private void OnDestroy()
        {
            if (_camera != null)
                _camera.enabled = true;
            if (_cameraControl != null)
                _cameraControl.enabled = true;
        }

        enum WindowType
        {
            None,
            Compositor,
        }
        
        private WindowType _windowType = WindowType.None;

        private void Awake()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if(!type.IsSubclassOf(typeof(BaseCompositorNode))) continue;
                AvailableNodes.Add(type);
            }
            Logger = base.Logger;
            InitializeConfig();
            InitializeComponents();
            _harmony = Harmony.CreateAndPatchAll(GetType());
        }

        private void InitializeConfig()
        {
            _maxCacheSize = Config.Bind("Performance", "Max Cache Size", 4, "The maximum amount of textures to keep in the local memory cache. Changing this value will clear the cache.");
            _maxCacheSize.SettingChanged += (sender, args) => TextureCache.Clear();
            _switchUI = Config.Bind("General", "Switch UI", new KeyboardShortcut(KeyCode.F4), "Bind to switch between the different UI's.");
        }

        private void InitializeComponents()
        {
            _camera = Camera.main;
            _cameraControl = _camera.transform.GetComponent<Studio.CameraControl>();
            _compositorManager = new CompositorManager();
            _renderer = new CompositorRenderer(_compositorManager);
            _compositorManager.CreateDefaultNodes();
        }

        private void Update()
        {
            if(_switchUI.Value.IsDown())
                _windowType = (WindowType)(((int)_windowType + 1) % Enum.GetValues(typeof(WindowType)).Length);
            if (_windowType == WindowType.None) return;
            _compositorManager.Update();
        }

        private void OnGUI()
        {
            bool isCompositorActive = _windowType == WindowType.Compositor;
            _camera.enabled = !isCompositorActive;
            _cameraControl.enabled = !isCompositorActive;

            if (isCompositorActive)
                _renderer.DrawCompositor();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AlphaShot2), nameof(AlphaShot2.CaptureTex), typeof(int), typeof(int), typeof(int), typeof(AlphaMode))]
        private static void InterceptScreenshot(Texture2D __result)
        {
            if(__result == null) return;
            var tmpTexture = new Texture2D(__result.width, __result.height, __result.format, false);
            Graphics.CopyTexture(__result, tmpTexture);

            TextureCache.AddTexture(tmpTexture, _maxCacheSize.Value);
            Logger.LogDebug($"Texture added to cache {__result.format}({__result.width}x{__result.height})");
        }
    }
}