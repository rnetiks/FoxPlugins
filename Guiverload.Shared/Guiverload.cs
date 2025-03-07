using System;
using System.Collections.Generic;
using System.Linq;
using Autumn;
using SmartRectV0;
using Studio;
using UnityEngine;

namespace Guiverload.KKS
{
    partial class Guiverload : MonoBehaviour
    {
        private GUIStyle _windowStyle;
        private Texture2D _windowTexture;
        private Texture2D _containerTexture;
        private Texture2D _closeButtonTexture;
        private List<string> characters;

        private const float Margin = 15f;

        internal void RecalculateElements()
        {
            _windowTexture = new Texture2D(350, 650);
            ;
            _windowTexture = _windowTexture.Fill(37, 37, 37, (byte)(255 * Entry.opacity.Value))
                .SetBorder(10, TextureFactory.Border.All, 1);
            var windowTextureHeight = _windowTexture.height;
            var windowTextureWidth = _windowTexture.width;
            _windowRect = new SmartRect(Screen.width - windowTextureWidth - Margin, 70, windowTextureWidth,
                windowTextureHeight);
            _windowStyle = new GUIStyle()
            {
                normal =
                {
                    background = _windowTexture
                }
            };
 
            _containerTexture = new Texture2D((int)(_windowRect.Width - 70), (int)(_windowRect.Height - 50));
            _containerTexture.Fill(130, 130, 130, 100);
            _containerTexture.SetBorder(10, TextureFactory.Border.BottomRight | TextureFactory.Border.TopLeft, 1);
            int windowRectWidth = (int)(_windowRect.Width * 0.1f);
            SideButtonBackground = new Texture2D(windowRectWidth, windowRectWidth, TextureFormat.RGBA32, false)
                .Fill(50, 50, 50, 255).SetBorder(10, TextureFactory.Border.All, 1);
        }

        private void Awake()
        {
            RecalculateElements();
            characters = new List<string>();
            _testRect = new SmartRect(Screen.width - 300, 400, 300, 400);
        }

        private static void GetSelectedCharacterBounds()
        {
            var selectedCharacters = KKAPI.Studio.StudioAPI.GetSelectedCharacters();
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f, minZ = 0f, maxZ = 0f;
            foreach (var selectedCharacter in selectedCharacters)
            {
                var smr = selectedCharacter.charReference.GetComponentsInChildren<SkinnedMeshRenderer>();
                var mr = selectedCharacter.charReference.GetComponentsInChildren<MeshRenderer>();
                foreach (var skinnedMeshRenderer in smr)
                {
                    var component = skinnedMeshRenderer.sharedMesh;

                    if (component == null) continue;
                    foreach (var meshVertex2 in component.vertices)
                    {
                        var meshVertex = skinnedMeshRenderer.transform.TransformPoint(meshVertex2); // Local to worldspace
                        if(meshVertex.y > maxY)
                            maxY = meshVertex.y;
                        if(meshVertex.y < minY)
                            minY = meshVertex.y;
                            
                        if(meshVertex.x > maxX)
                            maxX = meshVertex.x;
                        if(meshVertex.x < minX)
                            minX = meshVertex.x;
                            
                        if(meshVertex.z > maxZ)
                            maxZ = meshVertex.z;
                        if(meshVertex.z < minZ)
                            minZ = meshVertex.z;
                    }
                }
                Entry._logSource.LogError($"\nX: {minX} {maxX}\nY: {minY} {maxY}\nZ: {minZ} {maxZ}");
            }
        }

        private SmartRect _windowRect;

        private SmartRect _testRect;

        private void OnGUI()
        {
            _windowRect = new SmartRect(GUI.Window(9921, _windowRect, WindowFunction, string.Empty, _windowStyle));
        }

        private void FixedUpdate()
        {
            _testRect.UpdateAnimationIndependent(Beziers.LinearTemplate);
            mPos = Event.current.mousePosition;
            characters.Clear();
            int i = 0;
            TreeNodeObject n;
            do
            {
                n = Singleton<Studio.Studio>.Instance.treeNodeCtrl.GetNode(i++);
                if (n != null)
                    characters.Add(n.textName);
            } while (n != null);
        }

        private int selectedIndex;
        Vector2 mPos = Vector2.zero;

        private Studio.CameraControl _control;

        private void WindowFunction(int id)
        {
            if (_control == null)
                _control = FindObjectOfType<Studio.CameraControl>();
            _control.enabled = !_windowRect.ToRect().Contains(mPos); // Interesting, that's something I gotta add
            DrawHeader();
            DrawButton();
            DrawCharacterList();

            GUI.DragWindow();
        }

        private void DrawCharacterList()
        {
            var position = new Rect(_windowRect.Width - _containerTexture.width,
                _windowRect.Height - _containerTexture.height,
                _containerTexture.width, _containerTexture.height);
            GUI.DrawTexture(position, _containerTexture);

            SmartRect rect = new SmartRect(position) { Height = 30 };
            int i = 0;
            foreach (var character in characters)
            {
                if (GUI.Button(rect.ToRect(), character) && !Input.GetKey(KeyCode.LeftShift)) SelectCharacter(i);
                rect.NextRow();
                i++;
            }
        }

        private void SelectCharacter(int i)
        {
            var instanceTreeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
            instanceTreeNodeCtrl.selectNode = instanceTreeNodeCtrl.GetNode(i);
            selectedIndex = i;
        }

        private Texture2D SideButtonBackground;

        private readonly GUIStyle _style = new GUIStyle()
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.white
            }
        };

        private Func<bool> _animator;
        private bool _state;

        private void DrawButton()
        {
            SmartRect rect = new SmartRect(10, 50, 50, 50);

            GUI.Button(rect.ToRect(), "All");
            GUI.Button(rect.NextRow().ToRect(), "Characters");
            if (GUI.Button(rect.NextRow().ToRect(), "Lights"))
            {
            }
        }

        private void DrawHeader()
        {
            const float margin = 5f;
            var labelRect = new Rect(10, 15, 200, 200);
            GUI.Label(labelRect, "Workspace", _style);
        }
    }
}