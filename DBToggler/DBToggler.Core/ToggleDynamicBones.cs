using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autumn;
using DBToggler.Core;
using KKAPI.Studio;
using Studio;
using UnityEngine;
using Random = UnityEngine.Random;
using SmartRectV0;

namespace DBToggler
{
    public class ToggleDynamicBones : MonoBehaviour
    {
        private int sHeight, sWidth;
        private Rect windowPosition;
        public GUIStyle windowStyle;
        private Camera cam;

        private bool enabled = false;

        private void OnGUI()
        {
            if (!enabled)
                return;
            plugins ??= GetPlugins();
            windowPosition = GUI.Window(9918, windowPosition, WindowFunc, $"KPU ({plugins.Length} plugins loaded)",
                windowStyle);
        }


        Vector2 scroll = Vector2.zero;
        private string[] plugins = null;

        private string searchText = string.Empty;

        private void WindowFunc(int id)
        {
            searchText = GUI.TextField(new Rect(0, 0, 100, 20), searchText);
            scroll = GUI.BeginScrollView(new Rect(0, 20, sWidth / 2f, sHeight / 2f), scroll,
                new Rect(0, 0, sWidth / 2f, 25 * plugins.Length + 5));
            int ir = 0;
            for (int i = 0; i < plugins.Length; i++)
            {
                var current = plugins[i].Substring(plugins[i].LastIndexOf('\\') + 1);
                if (current.Contains(searchText))
                {
                    KPUItem(0, ir++ * 25, current);
                }
            }

            GUI.EndScrollView();
            GUI.DragWindow();
        }

        private void KPUItem(int x, int y, string t)
        {
            var windowWidth = sWidth / 2f;
            SmartRect smartRect = new SmartRect(10 + x, y, windowWidth - 150, 20);
            GUI.Label(smartRect.ToRect(),
                t);
            smartRect.NextColumn();
            smartRect.Width = 100;

            float f = Random.value;
            GUI.Button(smartRect.ToRect(), f > 0.5 ? "Update" : "Latest",
                f > 0.5f ? roundButtonStyle : roundButtonStyleDisabled);
        }

        private string[] GetPlugins()
        {
            return Directory.GetFiles(Path.GetDirectoryName(Application.dataPath) + "\\BepInEx\\plugins", "*.dll",
                SearchOption.AllDirectories);
        }


        private void TestMethod()
        {
            var camera = Camera.main;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            float rayOffset = 0.1f;

            Ray offsetRay = new Ray(ray.origin + ray.direction * rayOffset, ray.direction);
            var cast = Physics.Raycast(ray.origin, ray.direction.normalized, out var hit, 99999f, ~0);
            if (cast)
            {
                var mesh2 = hit.transform.GetComponent<SkinnedMeshRenderer>();
                List<Vector3> finalPoints = new List<Vector3>();
                Mesh m = new Mesh();
                mesh2.BakeMesh(m);

                Vector3[] vertices = m.vertices;
                Vector3 hitPoint = hit.point;

                Vector3[] worldVertices = new Vector3[vertices.Length];
                for (int v = 0; v < vertices.Length; v++)
                {
                    worldVertices[v] = hit.transform.TransformDirection(vertices[v]);
                }

                foreach (var sharedMeshVertex in worldVertices)
                {
                    var worldToScreenPoint = camera.WorldToScreenPoint(sharedMeshVertex);
                    if (Vector3.Distance(hitPoint, sharedMeshVertex) < 0.1)
                    {
                        GUI.Label(new Rect(worldToScreenPoint.x, Screen.height - worldToScreenPoint.y, 100, 100), "*",
                            labelStyle);
                    }
                }

                Init._logger.LogError($"{hit.transform.name} {hit.point} {finalPoints.Count} {m.vertices[0]}");
            }
        }

        public GUIStyle labelStyle { get; set; }

        private void Awake()
        {
            Texture2D tex2 = new Texture2D(800, 160);
            TextureFactory.Fill(tex2, Color.green);
            roundButtonStyle = new GUIStyle()
            {
                normal =
                {
                    background = TextureFactory.SetBorder(tex2, 80, TextureFactory.Border.All)
                },
                alignment = TextAnchor.MiddleCenter
            };

            Texture2D tex3 = new Texture2D(800, 160);
            roundButtonStyleDisabled = new GUIStyle()
            {
                normal =
                {
                    background = TextureFactory.SetBorder(tex3, 80, TextureFactory.Border.All)
                },
                alignment = TextAnchor.MiddleCenter
            };

            cam = Camera.main;
            sHeight = Screen.height;
            sWidth = Screen.width;
            windowPosition = new Rect(100, 100, sWidth / 2f, sHeight / 2f);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.gray);
            tex.Apply();
            windowStyle = new GUIStyle()
            {
                active =
                {
                    background = tex
                },
                focused =
                {
                    background = tex
                },
                normal =
                {
                    background = tex
                },
                alignment = TextAnchor.UpperCenter
            };
            // labelStyle = new GUIStyle()
            // {
            //     normal =
            //     {
            //         textColor = Color.black,
            //     }
            // };
        }

        public GUIStyle roundButtonStyleDisabled { get; set; }

        public GUIStyle roundButtonStyle { get; set; }

        private void Update()
        {
#if KKS
            if (Init.SelectAllKey.Value.IsUp())
            {
                var nodes = TreeView.GetAllNodes();
                var treeNodeCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
                if (treeNodeCtrl.selectNodes.Length < nodes.Length)
                {
                    SelectNodes(nodes);
                    return;
                }

                foreach (var treeNodeObject in treeNodeCtrl.selectNodes)
                {
                    treeNodeCtrl.DeselectNode(treeNodeObject);
                }
                return;
            }
#endif

            var selectedCharacters = StudioAPI.GetSelectedCharacters().ToArray();
            if (selectedCharacters.Length > 0)
            {
                if (Init.EnableDynamicBonesKey.Value.IsUp())
                {
                    foreach (var selectedCharacter in selectedCharacters)
                    {
                        var bones = selectedCharacter.charReference.GetComponentsInChildren<DynamicBone>();
                        SetBones(bones, true);
                    }
                }
                else if (Init.DisableDynamicBonesKey.Value.IsUp())
                {
                    foreach (var selectedCharacter in selectedCharacters)
                    {
                        var bones = selectedCharacter.charReference.GetComponentsInChildren<DynamicBone>();
                        SetBones(bones, false);
                    }
                }

                return;
            }

            if (Init.EnableDynamicBonesKey.Value.IsUp())
            {
                var bones = FindObjectsOfType<DynamicBone>();
                SetBones(bones, true);
            }
            else if (Init.DisableDynamicBonesKey.Value.IsUp())
            {
                var bones = FindObjectsOfType<DynamicBone>();
                SetBones(bones, false);
            }
        }

        /// <summary>
        /// Sets the enabled state of the specified DynamicBone instances.
        /// </summary>
        /// <param name="bones">Array of DynamicBone instances to be modified.</param>
        /// <param name="state">Boolean value indicating the desired enabled state of the bones.</param>
        private static void SetBones(DynamicBone[] bones, bool state)
        {
            Init._logger.LogWarning($"{(state ? "Enabling" : "Disabling")} dynamic bones on {bones.Length} bones");
            foreach (var bone in bones)
            {
                bone.enabled = state;
            }
        }

#if KKS
        /// <summary>
        /// Selects the given array of TreeNodeObject nodes in the TreeView.
        /// </summary>
        /// <param name="objects">Array of TreeNodeObject nodes to be selected.</param>
        private static void SelectNodes(TreeNodeObject[] objects)
        {
            if (!StudioAPI.StudioLoaded) return;

            var ctrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl;
            ctrl.selectNodes = objects;
        }
#endif
    }

    public class TreeView
    {
        /// <summary>
        /// Retrieves all TreeNodeObject nodes from the TreeView.
        /// </summary>
        /// <returns>
        /// An array of TreeNodeObject nodes. Returns an empty array if Studio is not loaded.
        /// </returns>
        public static TreeNodeObject[] GetAllNodes()
        {
            return StudioAPI.StudioLoaded
                ? Singleton<Studio.Studio>.Instance.treeNodeCtrl.m_TreeNodeObject.ToArray()
                : [];
        }
    }
}