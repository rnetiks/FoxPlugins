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
    [BepInPlugin(GUID, "The Bird of Hermes", "4.0.0.0")]
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
        private ConfigEntry<bool> _scrubbingSound;

        #region Public API for SceneController

        public TrackManager TrackManager => _trackManager;

        #endregion

        private void Awake()
        {
            _showWaveform = Config.Bind("General", "Show Waveform", new KeyboardShortcut(KeyCode.None));
            _showWindow = Config.Bind("General", "Show Audio Window", new KeyboardShortcut(KeyCode.None));
            _scrubbingSound = Config.Bind("Audio", "Scrubbing Sound", false);
            Logger = base.Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Entry));

            _trackManager = new TrackManager(this);
            _audioWindow = new AudioWindow(_trackManager);
            _audioWindow.SetPlaybackTimeGetter(() => TL?._playbackTime ?? 0f);
            _audioWindow.OnSeek = time =>
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

        private void GetViewWindow(out float startTime, out float visibleDuration)
        {
            float duration = TL._duration;

            float tlVisibleDuration = duration / TL._zoomLevel;
            float tlStartTime = GridScroll * (duration - tlVisibleDuration);

            visibleDuration = tlVisibleDuration / _waveZoom;

            if (_followPlayhead && _waveZoom > 1.01f)
            {
                startTime = TL._playbackTime - visibleDuration / 2f;
            }
            else if (_waveZoom > 1.01f)
            {
                startTime = _waveScrollOffset;
            }
            else
            {
                float tlCenter = tlStartTime + tlVisibleDuration / 2f;
                startTime = tlCenter - visibleDuration / 2f + _waveScrollOffset;
            }

            startTime = Mathf.Clamp(startTime, 0, Mathf.Max(0, duration - visibleDuration));
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

            var selected = _trackManager.SelectedTrack;
            bool hasSelected = selected != null && selected.HasAudio;

            if (hasSelected)
                y -= 100;

            var toolbarRect = new Rect(x, y, width, 20);
            var waveformRect = new Rect(x, y + 22, width, _waveformHeight);

            DrawToolbar(toolbarRect);

            if (hasSelected)
                DrawSelectedWaveform(waveformRect, selected);
        }

        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_audioWindow.IsOpen ? "Close Window" : "Audio Window", GUILayout.Width(90)))
                _audioWindow.IsOpen = !_audioWindow.IsOpen;

            var selected = _trackManager.SelectedTrack;
            if (selected != null && selected.HasAudio)
            {
                GUILayout.Label($"{selected.Name}", GUILayout.Width(100));
                GUILayout.Label($"{selected.EffectiveDuration:F2}s", GUILayout.Width(50));

                GUILayout.Space(10);

                GUILayout.Label($"Zoom: {_waveZoom:F1}x", GUILayout.Width(90));
                if (GUILayout.Button("-", GUILayout.Width(20)))
                    SetZoom(_waveZoom / 1.5f, 0.5f);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                    SetZoom(_waveZoom * 1.5f, 0.5f);
                if (GUILayout.Button("Reset", GUILayout.Width(45)))
                {
                    _waveZoom = 1f;
                    _waveScrollOffset = 0f;
                }

                GUI.color = _followPlayhead ? Color.green : Color.gray;
                if (GUILayout.Button("Follow", GUILayout.Width(50)))
                    _followPlayhead = !_followPlayhead;
                GUI.color = Color.white;

                if (GUILayout.Button("Sync Time"))
                {
                    float maxEnd = 0f;
                    foreach (var track in _trackManager.Tracks)
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
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (new Rect(rect.x, rect.y, 90, rect.height).Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void ForceTimelineGUIUpdate()
        {
            TL.UpdateGrid();
        }

        private void DrawSelectedWaveform(Rect rect, AudioTrack track)
        {
            int width = (int)rect.width;
            int height = (int)rect.height;
            if (width <= 0 || height <= 0) return;

            if (_selectedWaveformTrack != track)
            {
                _selectedWaveformTrack = track;
                _selectedWaveform.Clear();
                if (track.Audio?.MonoSamples != null)
                    _selectedWaveform.SetSamples(track.Audio.MonoSamples, track.Audio.Data.SampleRate);
            }

            GetViewWindow(out float startTime, out float visibleDuration);

            if (_selectedWaveform.NeedsRebuild(width, startTime, visibleDuration))
                _selectedWaveform.Rebuild(width, height, startTime, visibleDuration, track.Offset);

            GUI.DrawTexture(rect, WindowStyles.GetTexture(_bgColor), ScaleMode.StretchToFill, false);

            if (_selectedWaveform.Texture != null)
                GUI.DrawTexture(rect, _selectedWaveform.Texture);

            DrawGridLines(rect, startTime, visibleDuration);
            DrawPlayhead(rect, startTime, visibleDuration);
            HandleSelectedWaveformInput(rect, startTime, visibleDuration, track);
        }

        private void DrawGridLines(Rect rect, float startTime, float visibleDuration)
        {
            float[] intervals = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 30f, 60f };
            float interval = intervals[intervals.Length - 1];
            foreach (float iv in intervals)
            {
                if (visibleDuration / iv <= 15)
                {
                    interval = iv;
                    break;
                }
            }

            float firstLine = Mathf.Ceil(startTime / interval) * interval;
            for (float t = firstLine; t <= startTime + visibleDuration; t += interval)
            {
                float normX = (t - startTime) / visibleDuration;
                if (normX < 0 || normX > 1) continue;

                float x = rect.x + normX * rect.width;
                GUI.DrawTexture(new Rect(x, rect.y, 1, rect.height), WindowStyles.GetTexture(_gridColor),
                    ScaleMode.StretchToFill, true);
            }
        }

        private void DrawPlayhead(Rect rect, float startTime, float visibleDuration)
        {
            float normX = (TL._playbackTime - startTime) / visibleDuration;
            if (normX < 0 || normX > 1) return;

            float x = rect.x + normX * rect.width;
            GUI.DrawTexture(new Rect(x - 1, rect.y, 2, rect.height), WindowStyles.GetTexture(_playheadColor),
                ScaleMode.StretchToFill, false);
        }

        private void HandleSelectedWaveformInput(Rect rect, float startTime, float visibleDuration, AudioTrack track)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            if (e.type == EventType.ScrollWheel)
            {
                float mouseNormX = (e.mousePosition.x - rect.x) / rect.width;
                float timeAtMouse = startTime + mouseNormX * visibleDuration;

                float zoomDelta = -e.delta.y * 0.1f;
                float newZoom = Mathf.Clamp(_waveZoom * (1 + zoomDelta), 0.1f, 5000f);

                SetZoomAtPoint(newZoom, timeAtMouse, mouseNormX);
                e.Use();
            }

            bool canScrub = AllowScrubWhilePlaying || !TL._isPlaying;

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && canScrub)
            {
                float normX = (e.mousePosition.x - rect.x) / rect.width;
                float time = startTime + normX * visibleDuration;
                time = Mathf.Clamp(time, 0, TL._duration);

                bool isDrag = e.type == EventType.MouseDrag;
                if (isDrag && track.Audio != null)
                    track.Audio.Scrub(time, track.Offset, _scrubbingSound.Value);
                else if (track.Audio != null)
                    track.Audio.SeekTo(time, track.Offset);

                TL._playbackTime = time;
                ForceTimelineGUIUpdate();
                e.Use();
            }

            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                float deltaNorm = -e.delta.x / rect.width;
                float deltaTime = deltaNorm * visibleDuration;
                _waveScrollOffset += deltaTime;
                ClampScrollOffset();
                e.Use();
                Input.ResetInputAxes();
            }

            if (rect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void SetZoom(float newZoom, float anchorNormX)
        {
            GetViewWindow(out float startTime, out float visibleDuration);
            float timeAtAnchor = startTime + anchorNormX * visibleDuration;
            SetZoomAtPoint(newZoom, timeAtAnchor, anchorNormX);
        }

        private void SetZoomAtPoint(float newZoom, float timeAtPoint, float normX)
        {
            _waveZoom = Mathf.Clamp(newZoom, 0.1f, 5000f);

            float newVisibleDuration = TL._duration / TL._zoomLevel / _waveZoom;
            _waveScrollOffset = timeAtPoint - normX * newVisibleDuration;

            ClampScrollOffset();
        }

        private void ClampScrollOffset()
        {
            float duration = TL._duration;
            float visibleDuration = duration / TL._zoomLevel / _waveZoom;

            _waveScrollOffset = Mathf.Clamp(_waveScrollOffset, 0, Mathf.Max(0, duration - visibleDuration));
        }
    }
}
