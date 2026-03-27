using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheBirdOfHermes.Audio;
using TheBirdOfHermes.Audio.Filter;
using TheBirdOfHermes.Undo;
using UnityEngine;

namespace TheBirdOfHermes.UI
{
    public class TrackContextMenu
    {
        public bool IsOpen { get; private set; }

        private Vector2 _position;
        private AudioTrack _track;
        private List<AudioTrack> _tracks;
        private AudioLane _lane;
        private Action _onOpenProperties;
        private UndoManager _undoManager;

        private readonly FilterConfigModal _filterModal = new FilterConfigModal();

        private static readonly int MainWindowId = "TBOHCtxMenu".GetHashCode();
        private static readonly int SubWindowId = "TBOHCtxSub".GetHashCode();

        private static readonly Color MenuBg = new Color(0.16f, 0.16f, 0.20f);
        private static readonly Color MenuBorder = new Color(0.3f, 0.3f, 0.35f);
        private static readonly Color ItemHover = new Color(0.3f, 0.5f, 0.8f, 0.5f);
        private static readonly Color SeparatorColor = new Color(0.32f, 0.32f, 0.38f);
        private static readonly Color SubArrowColor = new Color(0.6f, 0.6f, 0.65f);

        private const float ItemHeight = 22f;
        private const float MenuWidth = 160f;
        private const float SubMenuWidth = 160f;

        private string _openSubGroup;
        private float _openSubGroupY;
        private Rect _mainMenuRect;
        private Rect _subMenuRect;
        private List<SubMenuItem> _subMenuItems;

        private bool _actionTaken;

        private float _cursorLeftTime = -1f;
        private const float CursorLeaveDelay = 0.4f;

        private struct SubMenuItem
        {
            public string Label;
            public Action OnClick;
        }

        private List<string> _groups;
        private Dictionary<string, List<AudioFilterBase>> _grouped;

        private static AudioFilterBase[] CreateFilterPrototypes()
        {
            return new AudioFilterBase[]
            {
                new VocalRemover(),
                new SpectralVocalRemover(),
                new FrequencyMaskVocalRemover(),
                new PitchShifter(),
                new SpeedChanger(),
                new Compressor(),
                new Normalizer(),
                new NoiseGate(),
                new Amplifier(),
                new Equalizer(),
                new HighPassFilter(),
                new LowPassFilter(),
                new ReverbFilter(),
                new EchoEffect(),
                new ChorusEffect(),
                new PhaserEffect(),
                new DistortionEffect(),
                new TremoloEffect(),
                new InvertFilter(),
                new ReverseFilter(),
            };
        }

        private static bool HasCustomDraw(AudioFilterBase filter)
        {
            var method = filter.GetType().GetMethod("OnDraw", BindingFlags.Public | BindingFlags.Instance);
            return method != null && method.DeclaringType != typeof(AudioFilterBase);
        }

        public void Open(AudioTrack clickedTrack, IEnumerable<AudioTrack> selectedTracks, AudioLane lane, Vector2 pos, Action onOpenProperties, UndoManager undoManager)
        {
            _track = clickedTrack;
            _tracks = selectedTracks.ToList();
            if (!_tracks.Contains(clickedTrack))
                _tracks.Add(clickedTrack);
            _lane = lane;
            _position = pos;
            _onOpenProperties = onOpenProperties;
            _undoManager = undoManager;
            _openSubGroup = null;
            _subMenuItems = null;
            _cursorLeftTime = -1f;
            _trackBusy = _tracks.Any(t => t.IsBusy);
            IsOpen = true;

            BuildFilterGroups();
            CalculateMainRect();
        }

        private bool _trackBusy;

        public void Close()
        {
            IsOpen = false;
            _track = null;
            _tracks = null;
            _lane = null;
            _undoManager = null;
            _openSubGroup = null;
            _subMenuItems = null;
        }

        private void BuildFilterGroups()
        {
            var prototypes = CreateFilterPrototypes();
            _groups = new List<string>();
            _grouped = new Dictionary<string, List<AudioFilterBase>>();

            foreach (var filter in prototypes)
            {
                if (!_grouped.ContainsKey(filter.Group))
                {
                    _groups.Add(filter.Group);
                    _grouped[filter.Group] = new List<AudioFilterBase>();
                }
                _grouped[filter.Group].Add(filter);
            }
        }

        private void CalculateMainRect()
        {
            int rowCount = _groups.Count + 1;
            float totalHeight = rowCount * ItemHeight + 9f + 8f;
            _mainMenuRect = new Rect(_position.x, _position.y, MenuWidth, totalHeight);
            ClampToScreen(ref _mainMenuRect);
        }

        public void Draw()
        {
            _filterModal.Draw();

            if (!IsOpen || _track == null) return;

            var prevDepth = GUI.depth;
            GUI.depth = -2000;

            _actionTaken = false;

            if (_openSubGroup != null && _subMenuItems != null)
                GUI.Window(SubWindowId, _subMenuRect, DrawSubMenuWindow, GUIContent.none, GUIStyle.none);

            GUI.Window(MainWindowId, _mainMenuRect, DrawMainMenuWindow, GUIContent.none, GUIStyle.none);

            GUI.BringWindowToFront(MainWindowId);
            if (_openSubGroup != null && _subMenuItems != null)
                GUI.BringWindowToFront(SubWindowId);

            GUI.depth = prevDepth;

            if (_actionTaken) return;

            {
                var screenMouse = Event.current.mousePosition;
                bool inMain = _mainMenuRect.Contains(screenMouse);
                bool inSub = _openSubGroup != null && _subMenuRect.Contains(screenMouse);

                if (inMain || inSub)
                {
                    _cursorLeftTime = -1f;
                }
                else
                {
                    if (_cursorLeftTime < 0f)
                        _cursorLeftTime = Time.realtimeSinceStartup;
                    else if (Time.realtimeSinceStartup - _cursorLeftTime >= CursorLeaveDelay)
                    {
                        Close();
                        return;
                    }
                }
            }

            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                bool inMain = _mainMenuRect.Contains(e.mousePosition);
                bool inSub = _openSubGroup != null && _subMenuRect.Contains(e.mousePosition);
                if (!inMain && !inSub)
                {
                    Close();
                    e.Use();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
            }
        }

        private void DrawMainMenuWindow(int id)
        {
            var fullRect = new Rect(0, 0, MenuWidth, _mainMenuRect.height);
            GUI.DrawTexture(fullRect, WindowStyles.GetTexture(MenuBg));
            DrawBorder(fullRect);

            float y = 4;

            for (int i = 0; i < _groups.Count; i++)
            {
                string group = _groups[i];
                var itemRect = new Rect(0, y, MenuWidth, ItemHeight);
                bool hovered = itemRect.Contains(Event.current.mousePosition);
                bool isOpenSub = _openSubGroup == group;

                if (_trackBusy)
                {
                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.35f);
                    GUI.Label(new Rect(10, y + 1, MenuWidth - 30, ItemHeight),
                        group, WindowStyles.MenuItemLabel);
                    GUI.color = prevColor;
                }
                else
                {
                    if (hovered || isOpenSub)
                        GUI.DrawTexture(itemRect, WindowStyles.GetTexture(ItemHover));

                    GUI.Label(new Rect(10, y + 1, MenuWidth - 30, ItemHeight),
                        group, WindowStyles.MenuItemLabel);

                    if (hovered && _openSubGroup != group)
                    {
                        _openSubGroup = group;
                        _openSubGroupY = _mainMenuRect.y + y;
                        BuildSubMenu(group);
                    }
                }

                y += ItemHeight;
            }

            GUI.DrawTexture(new Rect(8, y + 4, MenuWidth - 16, 1),
                WindowStyles.GetTexture(SeparatorColor));
            y += 9f;

            {
                var itemRect = new Rect(0, y, MenuWidth, ItemHeight);
                bool propsHovered = itemRect.Contains(Event.current.mousePosition);

                if (propsHovered)
                {
                    _openSubGroup = null;
                    _subMenuItems = null;
                    GUI.DrawTexture(itemRect, WindowStyles.GetTexture(ItemHover));
                }

                GUI.Label(new Rect(10, y + 1, MenuWidth - 14, ItemHeight),
                    "Properties", WindowStyles.MenuItemLabel);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && propsHovered)
                {
                    _onOpenProperties?.Invoke();
                    Close();
                    Event.current.Use();
                    _actionTaken = true;
                }
            }

            if (_openSubGroup != null && Event.current.type == EventType.Repaint)
            {
                var screenMouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                bool inMain = _mainMenuRect.Contains(screenMouse);
                bool inSub = _subMenuRect.Contains(screenMouse);
                if (!inMain && !inSub)
                {
                    _openSubGroup = null;
                    _subMenuItems = null;
                }
            }
        }

        private void BuildSubMenu(string group)
        {
            var tracks = _tracks;
            var undoMgr = _undoManager;
            var filters = _grouped[group];
            _subMenuItems = new List<SubMenuItem>();

            foreach (var filter in filters)
            {
                bool hasModal = HasCustomDraw(filter);
                string label = hasModal ? filter.Name + "..." : filter.Name;
                var f = filter;

                _subMenuItems.Add(new SubMenuItem
                {
                    Label = label,
                    OnClick = () =>
                    {
                        if (HasCustomDraw(f))
                        {
                            _filterModal.Open(f, tracks, undoMgr);
                        }
                        else
                        {
                            var cmd = new ApplyFilterCommand(f.Name, tracks, f);
                            foreach (var t in tracks)
                                t.ApplyFilter(f);
                            undoMgr?.Push(cmd);
                        }
                    }
                });
            }

            float subHeight = _subMenuItems.Count * ItemHeight + 8f;

            float subX = _mainMenuRect.xMax;
            if (subX + SubMenuWidth > Screen.width)
                subX = _mainMenuRect.x - SubMenuWidth;

            _subMenuRect = new Rect(subX, _openSubGroupY, SubMenuWidth, subHeight);
            ClampToScreen(ref _subMenuRect);
        }

        private void DrawSubMenuWindow(int id)
        {
            var fullRect = new Rect(0, 0, SubMenuWidth, _subMenuRect.height);
            GUI.DrawTexture(fullRect, WindowStyles.GetTexture(MenuBg));
            DrawBorder(fullRect);

            float y = 4;
            foreach (var item in _subMenuItems)
            {
                var itemRect = new Rect(0, y, SubMenuWidth, ItemHeight);
                if (itemRect.Contains(Event.current.mousePosition))
                    GUI.DrawTexture(itemRect, WindowStyles.GetTexture(ItemHover));

                GUI.Label(new Rect(10, y + 1, SubMenuWidth - 14, ItemHeight),
                    item.Label, WindowStyles.MenuItemLabel);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    itemRect.Contains(Event.current.mousePosition))
                {
                    item.OnClick?.Invoke();
                    Close();
                    Event.current.Use();
                    _actionTaken = true;
                    return;
                }

                y += ItemHeight;
            }
        }

        private static void ClampToScreen(ref Rect rect)
        {
            if (rect.xMax > Screen.width) rect.x = Screen.width - rect.width;
            if (rect.yMax > Screen.height) rect.y = Screen.height - rect.height;
            if (rect.x < 0) rect.x = 0;
            if (rect.y < 0) rect.y = 0;
        }

        private static void DrawBorder(Rect rect)
        {
            var tex = WindowStyles.GetTexture(MenuBorder);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), tex);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), tex);
        }
    }
}