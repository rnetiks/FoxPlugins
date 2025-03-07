using System.Collections.Generic;
using System.Linq;
using Autumn;
using BepInEx;
using Illusion.Extensions;
using JetBrains.Annotations;
using KKAPI.Utilities;
using SmartRectV0;
using Studio;
using UnityEngine;
using UnityEngine.Rendering;

namespace Guiverload.KKS
{
    public class TestWindow : MonoBehaviour
    {
        SmartRect rect;
        private SmartRect rect2;
        private bool Active;
        private Texture2D tex;
        private Texture2D tex2;
        private int MaxWindowWidth;

        // private Texture2D FUCKYOU;

        public MaterialEditorWindow MaterialEditorWindow
        {
            get { return _materialEditorWindow; }
        }

        private void Awake()
        {
            MaxWindowWidth = (int)(Screen.width * 0.2d);
            // rect = new SmartRect(Screen.width - MaxWindowWidth, 0, MaxWindowWidth, Screen.height);
            rect2 = new SmartRect(64, 64, 128, 128);
            rect = new SmartRect(Screen.width + 30, 0, MaxWindowWidth, Screen.height);
            tex = new Texture2D(MaxWindowWidth, Screen.height).Fill(40, 40, 40, 200);
            tex2 = new Texture2D(128, 128).Fill(40,0,0,200);
            allShaders = Resources.FindObjectsOfTypeAll<Shader>(); // Expensive invocation, call once and forget
            // FUCKYOU = new Texture2D(15, 15).Gradient(Color.cyan, Color.magenta, 90);
        }

        private void Update()
        {
            rect.UpdateAnimationIndependent(Beziers.EaseInOutTemplate);
            if (Entry._testMenuKey.Value.IsDown())
            {
                rect.SetAnimateTo(
                    Active
                        ? new Rect(Screen.width + 30, 0, MaxWindowWidth, Screen.height)
                        : new Rect(Screen.width - MaxWindowWidth, 0, MaxWindowWidth, Screen.height),
                    0.2f
                );
                Active = !Active;
            }
            
            if(((Rect)rect).Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private OCIChar _currentlySelected;
        List<Component> _objs = new List<Component>();
        private Vector2 _scrollPosition = new Vector2(0, 0);

        private int _page = 1;

        private string text = "123";

        // private float dr;
        private void OnGUI()
        {
            // dr += 0.001f;
            // dr %= 1f;
            GUI.DrawTexture(rect, tex);
            SmartRect itemRect = new SmartRect(rect) { Height = 20 };
            SmartRect sideButtonsRect = new SmartRect(rect) { Width = 30, Height = 30 };
            sideButtonsRect.MoveToEndY(new Rect(0, 0, 0, Screen.height),
                (sideButtonsRect.Height + SmartRect.DefaultOffsetY) * 5);
            for (int i = 0; i < 5; i++)
            {
                if (GUI.Button(sideButtonsRect.MoveX(-sideButtonsRect.Width), i.ToString()))
                    _page = i;
                sideButtonsRect.NextRow();
            }

            switch (_page)
            {
                case 0:
                    GeneralWindow(itemRect);
                    break;
                case 1:
                    MaterialEditorWindow.MaterialWindow(itemRect);
                    break;
                case 2:

                    break;
            }
        }

        private string selectedMaterial;
        Vector2 _materialScrollPosition = Vector2.zero;
        private Rect _viewRect;
        public Shader[] allShaders;
        private readonly MaterialEditorWindow _materialEditorWindow;

        public TestWindow()
        {
            _materialEditorWindow = new MaterialEditorWindow(this);
        }

        private void GeneralWindow(SmartRect re2)
        {
            var selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters();
            var currentlySelected = selectedCharacters.FirstOrDefault();
            if (currentlySelected != null)
            {
                re2.MoveY(10);
                if (_currentlySelected != currentlySelected)
                {
                    Helper.GetBoundsAll(currentlySelected);
                    _objs.Clear();
                    currentlySelected.charReference.transform.GetComponents(_objs);
                }

                _currentlySelected = currentlySelected;

                DrawBounds(re2);

                re2.NextRow();
                DrawComponents(re2);
                re2.NextRow();
            }
            else
            {
                re2 = new SmartRect(rect);
                GUI.Label(re2, $"No character selected");
            }
        }

        private void DrawBounds(SmartRect re2)
        {
            GUI.Label(re2.MoveX(10), "[Bounds]");
            re2.NextRow().MoveX(10);
            re2.BeginHorizontal(3);
            GUI.Label(re2.Col(0), $"X:{Helper._maxX}");
            GUI.Label(re2.Col(1), $"Y:{Helper._maxY}");
            GUI.Label(re2.Col(2), $"Z:{Helper._maxZ}");
            re2.EndHorizontal().NextRow().MoveX(10).BeginHorizontal(3);
            GUI.Label(re2.Col(0), $"X:{Helper._minX}");
            GUI.Label(re2.Col(1), $"Y:{Helper._minY}");
            GUI.Label(re2.Col(2), $"Z:{Helper._minZ}");
            re2.EndHorizontal();
        }

        private void DrawComponents(SmartRect re2)
        {
            GUI.Label(re2.NextRow().MoveX(10), "[Components]");
            re2.NextRow();
            _viewRect = new Rect(0, 0, re2.Width - 20, _objs.Count * (re2.Height + 5));
            _scrollPosition = GUI.BeginScrollView(
                new Rect(re2.X, re2.Y, re2.Width, 300),
                _scrollPosition,
                _viewRect
            );

            SmartRect rect = new SmartRect(0, 0, re2.Width, 20);
            var visibleItems = FastUI.GetVisibleFields(_scrollPosition, 25, (int)_viewRect.height, _objs.Count);
            for (int i = visibleItems.Key; i < visibleItems.Value; i++)
            {
                GUI.Label(rect.Row(i),
                    $"[{_objs[i].ToString().Trim().Replace("\n", "").Replace(" ", "").Replace("\t", "")}]");
            }

            GUI.EndScrollView();
        }
    }
}