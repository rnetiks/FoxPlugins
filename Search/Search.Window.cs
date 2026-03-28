using System;
using System.Collections.Generic;
using Addin;
using UnityEngine;

namespace Search
{
    public partial class Search
    {
        private const float PaletteWidth = 600f;
        private const float PaletteTopMargin = 80f;
        private const float SearchFieldHeight = 36f;
        private const float ResultRowHeight = 32f;
        private const int MaxVisibleResults = 15;
        private const float HeaderHeight = 45f;

        private static bool _stylesInitialized;
        private static GUIStyle _backdropStyle;
        private static GUIStyle _searchFieldStyle;
        private static GUIStyle _resultNormalStyle;
        private static GUIStyle _resultSelectedStyle;
        private static GUIStyle _nameStyle;
        private static GUIStyle _descriptionStyle;
        private static GUIStyle _categoryStyle;
        private static GUIStyle _noResultsStyle;
        private static GUIStyle _shortcutBadgeStyle;
        private static GUIStyle _bindButtonStyle;
        private static GUIStyle _bindingOverlayStyle;
        private static GUIStyle _removeBtnStyle;
        private static Texture2D _bgTex;
        private static Texture2D _fieldBgTex;
        private static Texture2D _selectedBgTex;
        private static Texture2D _hoverBgTex;
        private static Texture2D _separatorTex;
        private static Texture2D _categoryBgTex;
        private static Texture2D _shortcutBadgeBgTex;
        private static Texture2D _bindBtnBgTex;
        private static Texture2D _removeBtnBgTex;

        private ScrollView _scrollView;
        internal Rect PaletteRect
        {
            get
            {
                float x = (Screen.width - PaletteWidth) / 2f;
                int resultCount = Mathf.Min(_filteredResults.Count, MaxVisibleResults);
                float totalHeight = SearchFieldHeight + 8f + resultCount * ResultRowHeight + 8f;
                return new Rect(x, PaletteTopMargin, PaletteWidth, totalHeight);
            }
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _bgTex = MakeTex(new Color(0.12f, 0.12f, 0.14f, 0.97f));
            _fieldBgTex = MakeTex(new Color(0.18f, 0.18f, 0.22f, 1f));
            _selectedBgTex = MakeTex(new Color(0.22f, 0.40f, 0.70f, 0.85f));
            _hoverBgTex = MakeTex(new Color(0.20f, 0.20f, 0.25f, 1f));
            _separatorTex = MakeTex(new Color(0.25f, 0.25f, 0.30f, 1f));
            _categoryBgTex = MakeTex(new Color(0.25f, 0.25f, 0.32f, 0.9f));
            _shortcutBadgeBgTex = MakeTex(new Color(0.30f, 0.28f, 0.20f, 0.9f));
            _bindBtnBgTex = MakeTex(new Color(0.22f, 0.22f, 0.28f, 0.9f));
            _removeBtnBgTex = MakeTex(new Color(0.45f, 0.20f, 0.20f, 0.9f));

            _backdropStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _bgTex },
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _searchFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 0, 0),
                fixedHeight = SearchFieldHeight,
                normal = { background = _fieldBgTex, textColor = new Color(0.9f, 0.9f, 0.9f) },
                focused = { background = _fieldBgTex, textColor = Color.white },
                hover = { background = _fieldBgTex, textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            _resultNormalStyle = new GUIStyle
            {
                padding = new RectOffset(12, 12, 0, 0),
                fixedHeight = ResultRowHeight,
                normal = { background = null }
            };

            _resultSelectedStyle = new GUIStyle(_resultNormalStyle)
            {
                normal = { background = _selectedBgTex }
            };

            _nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.93f, 0.93f, 0.95f) },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.55f, 0.55f, 0.60f) },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _categoryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                normal = { background = _categoryBgTex, textColor = new Color(0.65f, 0.70f, 0.85f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _noResultsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.45f, 0.45f, 0.50f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 8, 8)
            };

            _shortcutBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { background = _shortcutBadgeBgTex, textColor = new Color(0.85f, 0.80f, 0.55f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(2, 2, 0, 0)
            };

            _bindButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { background = _bindBtnBgTex, textColor = new Color(0.55f, 0.55f, 0.65f) },
                hover = { background = _hoverBgTex, textColor = new Color(0.80f, 0.80f, 0.90f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(2, 0, 0, 0)
            };

            _bindingOverlayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.80f, 0.55f) },
                alignment = TextAnchor.MiddleCenter
            };

            _removeBtnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { background = _removeBtnBgTex, textColor = new Color(0.90f, 0.60f, 0.60f) },
                hover = { background = _removeBtnBgTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 1, 1),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _stylesInitialized = true;
        }

        private void DrawPalette()
        {
            InitStyles();
            BuildFilteredResults();

            var rect = PaletteRect;

            GUI.color = new Color(0, 0, 0, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Box(rect, GUIContent.none, _backdropStyle);

            GUILayout.BeginArea(rect);

            GUILayout.BeginVertical();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);

            GUI.SetNextControlName(SearchFieldControlName);
            _searchText = GUILayout.TextField(_searchText, _searchFieldStyle,
                GUILayout.Width(PaletteWidth - 8));

            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_searchText))
            {
                var placeholderRect = GUILayoutUtility.GetLastRect();
                var prev = GUI.color;
                GUI.color = new Color(0.45f, 0.45f, 0.50f, 1f);
                var placeholderStyle = new GUIStyle(_searchFieldStyle)
                {
                    normal = { textColor = new Color(0.45f, 0.45f, 0.50f), background = null },
                    hover = { textColor = new Color(0.45f, 0.45f, 0.50f), background = null },
                    focused = { textColor = new Color(0.45f, 0.45f, 0.50f), background = null }
                };
                GUI.Label(placeholderRect, "  Type to search commands...", placeholderStyle);
                GUI.color = prev;
            }

            if (_needsFocus)
            {
                GUI.FocusControl(SearchFieldControlName);
                _needsFocus = false;
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var sepRect = GUILayoutUtility.GetRect(PaletteWidth - 16, 1);
            GUI.DrawTexture(sepRect, _separatorTex);
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            GUILayout.EndVertical();
            GUILayout.EndArea();

            if (_filteredResults.Count == 0 && !string.IsNullOrEmpty(_searchText))
            {
                var noResultsRect = new Rect(rect.x, rect.y + HeaderHeight, rect.width, ResultRowHeight);
                GUI.Label(noResultsRect, "No matching commands", _noResultsStyle);
            }
            else if (_filteredResults.Count > 0)
            {
                if (_scrollView == null)
                {
                    _scrollView = new ScrollView(3f)
                    {
                        AutoHide = true,
                        ScrollThumbColor = new Color(0.40f, 0.40f, 0.50f, 0.8f),
                        ScrollThumbHoverColor = new Color(0.50f, 0.50f, 0.60f, 0.9f),
                        ScrollThumbActiveColor = new Color(0.60f, 0.60f, 0.70f, 1f),
                        ScrollbarBackgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.3f),
                        ThumbMinSize = 16f,
                        ScrollSensitivity = 25f
                    };
                }

                int visibleCount = Mathf.Min(_filteredResults.Count, MaxVisibleResults);
                float scrollAreaHeight = visibleCount * ResultRowHeight;
                float contentHeight = _filteredResults.Count * ResultRowHeight;
                var scrollAreaRect = new Rect(rect.x, rect.y + HeaderHeight, rect.width, scrollAreaHeight);

                var selectedItemRect = new Rect(0, _selectedIndex * ResultRowHeight, rect.width, ResultRowHeight);
                _scrollView.ScrollToRect(selectedItemRect, scrollAreaHeight);

                var contentRect = _scrollView.BeginScrollView(scrollAreaRect, contentHeight);

                for (int i = 0; i < _filteredResults.Count; i++)
                {
                    var rowRect = new Rect(contentRect.x, contentRect.y + i * ResultRowHeight,
                        contentRect.width, ResultRowHeight);

                    DrawResultRow(_filteredResults[i], i == _selectedIndex, i, rowRect);
                }

                _scrollView.EndScrollView();
            }

            if (shortcuts.IsBinding)
            {
                GUI.color = new Color(0, 0, 0, 0.6f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                string preview = shortcuts.GetBindingPreview();
                string bindingText;
                if (!string.IsNullOrEmpty(preview))
                    bindingText = "Binding: " + preview + "\n\nRelease to confirm";
                else if (shortcuts.BindingCommand != null)
                    bindingText = "Press a key combo for:\n" + shortcuts.BindingCommand.Name + "\n\n(Escape to cancel)";
                else
                    bindingText = "Press a key combo...\n\n(Escape to cancel)";
                GUI.Label(rect, bindingText, _bindingOverlayStyle);
            }
        }

        private void DrawResultRow(ISearchCommand command, bool isSelected, int index, Rect rowRect)
        {

            bool isHovered = rowRect.Contains(Event.current.mousePosition);
            if (isHovered && Event.current.type == EventType.Repaint)
            {
                _selectedIndex = index;
                if (!isSelected)
                    GUI.DrawTexture(rowRect, _hoverBgTex);
            }

            if (isSelected && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(rowRect, _selectedBgTex);

            float rightEdge = rowRect.xMax - 8f;

            if (isSelected || isHovered)
            {
                var bindContent = new GUIContent("+Key");
                var bindSize = _bindButtonStyle.CalcSize(bindContent);
                var bindRect = new Rect(rightEdge - bindSize.x, rowRect.y + (rowRect.height - bindSize.y) / 2f,
                    bindSize.x, bindSize.y);
                rightEdge = bindRect.x - 4f;

                if (GUI.Button(bindRect, bindContent, _bindButtonStyle))
                {
                    shortcuts.StartBinding(command);
                    Event.current.Use();
                    return;
                }
            }

            var boundShortcuts = shortcuts.GetShortcuts(command);
            for (int si = boundShortcuts.Count - 1; si >= 0; si--)
            {
                string label = ShortcutManager.FormatShortcut(boundShortcuts[si]);
                var badgeContent = new GUIContent(label);
                var badgeSize = _shortcutBadgeStyle.CalcSize(badgeContent);
                var badgeRect = new Rect(rightEdge - badgeSize.x,
                    rowRect.y + (rowRect.height - badgeSize.y) / 2f,
                    badgeSize.x, badgeSize.y);

                GUI.Label(badgeRect, badgeContent, _shortcutBadgeStyle);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 &&
                    badgeRect.Contains(Event.current.mousePosition))
                {
                    shortcuts.RemoveShortcut(command, si);
                    Event.current.Use();
                    return;
                }

                rightEdge = badgeRect.x - 2f;
            }

            string category = command.Category;
            if (!string.IsNullOrEmpty(category))
            {
                var catContent = new GUIContent(category);
                var catSize = _categoryStyle.CalcSize(catContent);
                var catRect = new Rect(rightEdge - catSize.x,
                    rowRect.y + (rowRect.height - catSize.y) / 2f,
                    catSize.x, catSize.y);
                GUI.Label(catRect, catContent, _categoryStyle);
                rightEdge = catRect.x - 6f;
            }

            var clickableRect = new Rect(rowRect.x, rowRect.y, rightEdge - rowRect.x, rowRect.height);
            if (isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                clickableRect.Contains(Event.current.mousePosition))
            {
                FuzzySearch.RecordUsage(command.Name);
                command.Execute();
                ClosePalette();
                Event.current.Use();
                return;
            }

            var nameContent = new GUIContent(command.Name);
            var nameSize = _nameStyle.CalcSize(nameContent);
            var nameRect = new Rect(rowRect.x + 12, rowRect.y, nameSize.x, rowRect.height);
            GUI.Label(nameRect, nameContent, _nameStyle);

            if (!string.IsNullOrEmpty(command.Description))
            {
                float descX = nameRect.xMax;
                float descWidth = rightEdge - descX - 4f;
                if (descWidth > 30)
                {
                    var descRect = new Rect(descX, rowRect.y, descWidth, rowRect.height);
                    GUI.Label(descRect, command.Description, _descriptionStyle);
                }
            }
        }

        private void BuildFilteredResults()
        {
            var scored = new List<KeyValuePair<int, ISearchCommand>>();
            var snapshot = new List<ISearchCommand>(commands.Values);

            foreach (var cmd in snapshot)
            {
                int score = FuzzySearch.Score(_searchText, cmd);
                if (score >= 0)
                    scored.Add(new KeyValuePair<int, ISearchCommand>(score, cmd));
            }

            scored.Sort((a, b) =>
            {
                int cmp = b.Key.CompareTo(a.Key);
                return cmp != 0 ? cmp : string.Compare(a.Value.Name, b.Value.Name, StringComparison.OrdinalIgnoreCase);
            });

            _filteredResults.Clear();
            foreach (var pair in scored)
                _filteredResults.Add(pair.Value);

            if (_selectedIndex >= _filteredResults.Count)
                _selectedIndex = _filteredResults.Count > 0 ? _filteredResults.Count - 1 : 0;
        }
    }
}