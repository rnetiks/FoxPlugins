using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Diagnostics;
using StrayTech;

namespace DBToggler.Core
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("CharaStudio")]
    public class Init : BaseUnityPlugin
    {
        private const string GUID = "fox.dbtoggler";
        private const string NAME = "Dynamic Bone Toggler";
        private const string VERSION = "1.0.0.0";

        internal static ConfigEntry<KeyboardShortcut> EnableDynamicBonesKey;
        internal static ConfigEntry<KeyboardShortcut> DisableDynamicBonesKey;
        internal static ConfigEntry<KeyboardShortcut> ExperimanlBonesKey;
        internal static ManualLogSource _logger;

        private static GameObject bepinex;
        private static Harmony harmony;

        private void Awake()
        {
            EnableDynamicBonesKey = Config.Bind("Bones", "Enable", new KeyboardShortcut(KeyCode.G), "Partial support");
            DisableDynamicBonesKey =
                Config.Bind("Bones", "Disable", new KeyboardShortcut(KeyCode.H), "Partial support");
            ExperimanlBonesKey = Config.Bind("Experimental", "Toggle", KeyboardShortcut.Empty,
                "Experimental toggle that keeps the current position of the bones rather than resetting it");
            _logger = Logger;
            bepinex = gameObject;
            harmony = Harmony.CreateAndPatchAll(GetType());
            // bepinex.GetOrAddComponent<ToggleDynamicBones>();
        }


        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        private static bool _ignoreNewline;
        private static bool _ignoreLogFile;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DiskLogListener), nameof(DiskLogListener.LogEvent))]
        private static bool LogEventPatch(LogEventArgs __instance)
        {
            if (_ignoreLogFile)
            {
                return false;
            }

            return true;
        }

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

        public static bool EnableDynamicBones = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.LateUpdate))]
        [HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.Update))]
        public static bool DynamicBonePatch(DynamicBone __instance)
        {
            return EnableDynamicBones;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudioScene), nameof(StudioScene.Start))]
        private static void StudioEntry()
        {
            bepinex.GetOrAddComponent<ToggleDynamicBones>();
        }
    }

    public class ProgressBar
    {
        private readonly int _width;
        private string _title;
        private readonly char _filledChar;
        private readonly char _emptyChar;
        private readonly bool _showPercentage;
        private readonly bool _trackTime;
        private readonly Stopwatch _stopwatch;

        private float _currentProgress;

        internal ProgressBar(int width, string title, char filledChar, char emptyChar,
            bool showPercentage, bool trackTime)
        {
            _width = width;
            _title = title;
            _filledChar = filledChar;
            _emptyChar = emptyChar;
            _showPercentage = showPercentage;
            _trackTime = trackTime;
            _stopwatch = trackTime ? Stopwatch.StartNew() : null;
            _currentProgress = 0f;
        }

        public void SetTitle(string title)
        {
            _title = title ?? "";
        }

        /// <summary>
        /// Updates the current progress (0.0 to 1.0)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            _currentProgress = Math.Max(0f, Math.Min(1f, progress));
        }

        /// <summary>
        /// Gets the current progress as a percentage (0-100)
        /// </summary>
        public float ProgressPercentage => _currentProgress * 100f;

        /// <summary>
        /// Gets the elapsed time since progress tracking started
        /// </summary>
        public TimeSpan ElapsedTime => _trackTime ? _stopwatch.Elapsed : TimeSpan.Zero;

        /// <summary>
        /// Estimates the remaining time based on current progress and elapsed time
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                if (!_trackTime || _currentProgress <= 0f)
                    return null;

                var elapsedTicks = _stopwatch.ElapsedTicks;
                var estimatedTotalTicks = elapsedTicks / _currentProgress;
                var remainingTicks = estimatedTotalTicks - elapsedTicks;

                return remainingTicks > 0 ? TimeSpan.FromTicks((long)remainingTicks) : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Renders the progress bar as a string
        /// </summary>
        public string Render()
        {
            var result = "";

            // Add title if specified
            if (!string.IsNullOrEmpty(_title))
            {
                result += _title + ": ";
            }

            // Create the progress bar
            int filledWidth = (int)(_currentProgress * _width);
            int emptyWidth = _width - filledWidth;

            result += "[" + new string(_filledChar, filledWidth) + new string(_emptyChar, emptyWidth) + "]";

            // Add percentage if enabled
            if (_showPercentage)
            {
                result += $" {_currentProgress * 100:F2}%";
            }

            // Add time information if tracking is enabled
            if (_trackTime)
            {
                result += $" | Elapsed: {FormatTimeSpan(ElapsedTime)}";

                var eta = EstimatedTimeRemaining;
                if (eta.HasValue)
                {
                    result += $" | ETA: {FormatTimeSpan(eta.Value)}";
                }
            }

            return result;
        }

        /// <summary>
        /// Completes the progress and stops time tracking
        /// </summary>
        public void Complete()
        {
            _currentProgress = 1f;
            _stopwatch?.Stop();
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// Creates a new ProgressBarBuilder for fluent configuration
        /// </summary>
        public static ProgressBarBuilder Create() => new ProgressBarBuilder();
    }

    /// <summary>
    /// Builder class for creating customized ProgressBar instances
    /// </summary>
    public class ProgressBarBuilder
    {
        private int _width = 50;
        private string _title = "";
        private char _filledChar = '=';
        private char _emptyChar = ' ';
        private bool _showPercentage = true;
        private bool _trackTime;

        /// <summary>
        /// Sets the width of the progress bar (default: 50)
        /// </summary>
        public ProgressBarBuilder WithWidth(int width)
        {
            _width = Math.Max(1, width);
            return this;
        }

        /// <summary>
        /// Sets the title/label for the progress bar
        /// </summary>
        public ProgressBarBuilder WithTitle(string title)
        {
            _title = title ?? "";
            return this;
        }

        /// <summary>
        /// Sets custom characters for filled and empty portions
        /// </summary>
        public ProgressBarBuilder WithSymbols(char filled, char empty)
        {
            _filledChar = filled;
            _emptyChar = empty;
            return this;
        }

        /// <summary>
        /// Sets the character for the filled portion (default: '=')
        /// </summary>
        public ProgressBarBuilder WithFilledChar(char filled)
        {
            _filledChar = filled;
            return this;
        }

        /// <summary>
        /// Sets the character for the empty portion (default: ' ')
        /// </summary>
        public ProgressBarBuilder WithEmptyChar(char empty)
        {
            _emptyChar = empty;
            return this;
        }

        /// <summary>
        /// Enables or disables percentage display (default: enabled)
        /// </summary>
        public ProgressBarBuilder ShowPercentage(bool show = true)
        {
            _showPercentage = show;
            return this;
        }

        /// <summary>
        /// Enables time tracking for elapsed time and ETA calculations
        /// </summary>
        public ProgressBarBuilder WithTimeTracking(bool enabled = true)
        {
            _trackTime = enabled;
            return this;
        }

        /// <summary>
        /// Builds and returns a configured ProgressBar instance
        /// </summary>
        public ProgressBar Build()
        {
            return new ProgressBar(_width, _title, _filledChar, _emptyChar, _showPercentage, _trackTime);
        }
    }
}