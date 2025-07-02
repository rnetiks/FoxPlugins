using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using alphaShot;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Compositor.KK;
using HarmonyLib;
using Screencap;
using UnityEngine;
using UnityEngine.Rendering;

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
        public static ConfigEntry<KeyboardShortcut> _search;
        public static ConfigEntry<bool> _openAfterExport;
        public static ConfigEntry<int> _segments;
        private Harmony _harmony;
        public static ManualLogSource Logger;

        private Camera _camera;
        private Studio.CameraControl _cameraControl;
        private CompositorManager _compositorManager;
        public static CompositorRenderer _renderer;

        public static List<Type> AvailableNodes = new List<Type>();

        private void OnDestroy()
        {
            _bundle.Unload(false);
            if (_camera != null)
                _camera.enabled = true;
            if (_cameraControl != null)
                _cameraControl.enabled = true;
            _harmony?.UnpatchSelf();
        }

        enum WindowType
        {
            None,
            Compositor,
        }

        public static AssetBundle _bundle;
        private WindowType _windowType = WindowType.None;


        /// <summary>
        /// Creates a gradient texture based on the provided colors, positions, and dimensions.
        /// </summary>
        /// <param name="colors">An array of colors to be used in the gradient.</param>
        /// <param name="positions">An array of positions corresponding to the colors, defining the gradient distribution.</param>
        /// <param name="width">The width of the resulting gradient texture.</param>
        /// <param name="height">The height of the resulting gradient texture.</param>
        
        private void Awake()
        {
            Logger = base.Logger;

            byte[] readAllBytes = File.ReadAllBytes(Path.Combine("BepInEx/plugins/", "compositor1.unity3d"));
            _bundle = AssetBundle.LoadFromMemory(readAllBytes);
            
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(BaseCompositorNode))) continue;
                AvailableNodes.Add(type);
            }
            InitializeConfig();
            InitializeComponents();
            _harmony = Harmony.CreateAndPatchAll(GetType());
        }

        public static void WriteLog(string message)
        {
            _ignoreNewline = true;
            Logger.LogDebug(message);
            _ignoreNewline = false;
        }
        
        private static bool _ignoreNewline;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LogEventArgs), nameof(LogEventArgs.ToStringLine))]
        public static bool LogEventPatch(LogEventArgs __instance, ref string __result)
        {
            if (_ignoreNewline)
            {
                __result = __instance.ToString();
                return false;
            }

            return true;
        }

        private void InitializeConfig()
        {
            _maxCacheSize = Config.Bind("Performance", "Max Cache Size", 4, "The maximum amount of textures to keep in the local memory cache. Changing this value will clear the cache.");
            _maxCacheSize.SettingChanged += (sender, args) => TextureCache.Clear();
            _openAfterExport = Config.Bind("General", "Open Image after Render", false);
            _switchUI = Config.Bind("General", "Switch UI", new KeyboardShortcut(KeyCode.F4), "Bind to switch between the different UI's.");
            _search = Config.Bind("General", "Open Search", new KeyboardShortcut(KeyCode.A, KeyCode.LeftShift));
            _segments = Config.Bind("General", "Line Segments", 1, new ConfigDescription("Sets the amount of segments for lines", new AcceptableValueRange<int>(1, 30)));
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
            if (_switchUI.Value.IsDown())
                _windowType = (WindowType)(((int)_windowType + 1) % Enum.GetValues(typeof(WindowType)).Length);
            if (_windowType == WindowType.None) return;
            _compositorManager.Update();
        }

        private void OnGUI()
        {
            bool isCompositorActive = _windowType == WindowType.Compositor;
            // _camera.enabled = !isCompositorActive;
            _cameraControl.enabled = !isCompositorActive;

            if (isCompositorActive)
            {
                _renderer.DrawCompositor();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AlphaShot2), nameof(AlphaShot2.CaptureTex), typeof(int), typeof(int), typeof(int), typeof(AlphaMode))]
        private static void InterceptScreenshot(Texture2D __result)
        {
            if (__result == null) return;
            var tmpTexture = new Texture2D(__result.width, __result.height, __result.format, false);
            Graphics.CopyTexture(__result, tmpTexture);

            TextureCache.AddTexture(tmpTexture, _maxCacheSize.Value);
        }
    }
}