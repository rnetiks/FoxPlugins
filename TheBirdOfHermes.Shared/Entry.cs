using System;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using TheBirdOfHermes.UI;
using TheBirdOfHermes.Waveform;
using UnityEngine;
using UnityEngine.UI;

namespace TheBirdOfHermes
{
    [BepInPlugin(GUID, "The Bird of Hermes", "5.1.0.0")]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(Timeline.Timeline.GUID, Timeline.Timeline.Version)]
#if KK || KKS
    [BepInProcess("CharaStudio")]
#elif HS2 || AI
    [BepInProcess("StudioNEOV2")]
#endif
    public class Entry : BaseUnityPlugin
    {
        public const string GUID = "org.fox.thebirdofhermes";
        public static new BepInEx.Logging.ManualLogSource Logger { get; private set; }

        private const bool AllowScrubWhilePlaying = false;

        private Harmony _harmony;
        private TrackManager _trackManager;
        private AudioWindow _audioWindow;

        private WaveformRenderer _selectedWaveform = new WaveformRenderer();
        private AudioTrack _selectedWaveformTrack;
        private float _waveZoom = 1f;
        private float _waveScrollOffset = 0f;
        private bool _followPlayhead = true;

        private Color _bgColor = new Color(0.15f, 0.15f, 0.18f);
        private Color _playheadColor = Color.white;
        private Color _gridColor = new Color(1f, 1f, 1f, 0.15f);

        private int _waveformHeight = 80;

        private bool _uiEnabled = true;

        private ConfigEntry<KeyboardShortcut> _showWaveform;
        private ConfigEntry<KeyboardShortcut> _showWindow;

        #region Public API for SceneController

        public TrackManager TrackManager => _trackManager;

        #endregion

        private void Awake()
        {
            _showWaveform = Config.Bind("General", "Show Waveform", new KeyboardShortcut(KeyCode.None));
            _showWindow = Config.Bind("General", "Show Audio Window", new KeyboardShortcut(KeyCode.None));
            Logger = base.Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Entry));

            _trackManager = new TrackManager(this);
            _audioWindow = new AudioWindow(_trackManager);
            _audioWindow.SetPlaybackTimeGetter(() => TL?._playbackTime ?? 0f);
            _audioWindow.OnSeekEvent += time =>
            {
                if (TL == null) return;
                TL._playbackTime = time;
                _trackManager.SeekAll(time);
                ForceTimelineGUIUpdate();
            };


            SceneController.Plugin = this;
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
        }

        private void OnDestroy()
        {
            _trackManager?.ClearAll();
            _selectedWaveform?.Clear();
            _harmony?.UnpatchSelf();
        }

        private Timeline.Timeline TL => Singleton<Timeline.Timeline>.Instance;

        private float GridScroll
        {
            get
            {
                if (TL._horizontalScrollView == null) return 0f;
                return TL._horizontalScrollView.horizontalNormalizedPosition;
            }
        }

        private void Update()
        {
            if (_showWaveform.Value.IsDown())
                _uiEnabled = !_uiEnabled;

            if (_showWindow.Value.IsDown())
                _audioWindow.IsOpen = !_audioWindow.IsOpen;

            if (TL != null && _trackManager.HasAudio)
                _trackManager.SyncAllPlayback(TL._playbackTime, TL._isPlaying);
        }

        private void OnGUI()
        {
            if (TL == null || !_uiEnabled) return;

            GUI.depth = 1000;

            _audioWindow.Draw();

            var windowRT = TL._timelineWindow;
            if (windowRT == null || !windowRT.gameObject.activeInHierarchy) return;

            var ui = (Canvas)AccessTools.Field(typeof(Timeline.Timeline), "_ui")?.GetValue(TL);
            if (ui == null) return;

            var scaler = ui.GetComponentInParent<CanvasScaler>();

            var scalerX = Screen.width / scaler.referenceResolution.x;
            var scalerY = Screen.height / scaler.referenceResolution.y;

            float x = windowRT.position.x;
            float y = Screen.height - windowRT.position.y - (windowRT.rect.height * scalerY) - scalerY * 20;
            float width = windowRT.rect.width * scalerX;

            var toolbarRect = new Rect(x, y, width, 20);

            DrawToolbar(toolbarRect);
        }
        /// <summary>
        /// Draws the toolbar interface on the specified rectangle area.
        /// </summary>
        /// <param name="rect">The rectangular area where the toolbar interface will be drawn.</param>
        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_audioWindow.IsOpen ? "Close Window" : "Audio Window", GUILayout.Width(90)))
                _audioWindow.IsOpen = !_audioWindow.IsOpen;

            GUILayout.Space(230);
            if (TrackManager.HasAudio)
                if (GUILayout.Button("Sync Time"))
                {
                    float maxEnd = 0f;
                    foreach (var track in _trackManager.AllTracks)
                    {
                        if (track.TimelineEnd > maxEnd)
                            maxEnd = track.TimelineEnd;
                    }
                    if (maxEnd > 0)
                    {
                        TL._duration = maxEnd;
                        ForceTimelineGUIUpdate();
                    }
                }


            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (new Rect(rect.x, rect.y, 90, rect.height).Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        /// <summary>
        /// Triggers a graphical update for the timeline interface, ensuring that any changes to the timeline's state are reflected visually.
        /// </summary>
        private void ForceTimelineGUIUpdate()
        {
            TL.UpdateGrid();
        }
    }
}