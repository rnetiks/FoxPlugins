using System;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using UnityEngine;

namespace DebugBoxes
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin("com.fox.debugboxes", "Debug Boxes", "0.1.0")]
    public class Entry : BaseUnityPlugin
    {
        private Renderer[] _selectedRenderers = new Renderer[0];
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private Material _lineMaterial;

        private void Awake()
        {
            _toggleKey = Config.Bind("General", "Toggle Boxes", KeyboardShortcut.Empty);
        }

        private void OnDestroy()
        {
            _selectedRenderers = new Renderer[0];
            _toggleKey = null;
            Destroy(_lineMaterial);
            _lineMaterial = null;
        }

        private void Update()
        {
            var objectCtrlInfos = KKAPI.Studio.StudioAPI.GetSelectedObjects();
            if (objectCtrlInfos.Any())
            {
                if (_toggleKey.Value.IsDown())
                {
                    if (!_selectedRenderers.Any())
                    {
                        _selectedRenderers = objectCtrlInfos
                            .SelectMany(x => x.guideObject.transformTarget.GetComponentsInChildren<Renderer>())
                            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer)
                            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
                            .ToArray();
                        Logger.LogInfo($"Added {_selectedRenderers.Length} renderers to debug box list");
                    }
                    else
                        _selectedRenderers = new Renderer[0];
                }
            }
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                _lineMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.SetInt("_ZWrite", 0);
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        private void OnRenderObject()
        {
            if (Camera.current != Camera.main) return;
            if (!_selectedRenderers.Any()) return;

            EnsureLineMaterial();
            _lineMaterial.SetPass(0);

            Bounds _accumulatedBounds = new Bounds();
            foreach (var renderer in _selectedRenderers.Where(e => e.enabled && e.gameObject.activeInHierarchy))
            {
                if (renderer == null) continue;

                var bounds = renderer.bounds;

                _accumulatedBounds.Encapsulate(bounds);
                var min = bounds.min;
                var max = bounds.max;

                var corners = new Vector3[]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, max.z),
                    new Vector3(min.x, max.y, max.z),
                };

                GL.Begin(GL.LINES);
                GL.Color(Color.green);

                DrawLine(corners[0], corners[1]);
                DrawLine(corners[1], corners[2]);
                DrawLine(corners[2], corners[3]);
                DrawLine(corners[3], corners[0]);
                DrawLine(corners[4], corners[5]);
                DrawLine(corners[5], corners[6]);
                DrawLine(corners[6], corners[7]);
                DrawLine(corners[7], corners[4]);
                DrawLine(corners[0], corners[4]);
                DrawLine(corners[1], corners[5]);
                DrawLine(corners[2], corners[6]);
                DrawLine(corners[3], corners[7]);

                /*DrawBone(corners[0], corners[0] + new Vector3(0, -1, 0), Color.red);
                DrawBone(corners[0] + new Vector3(1, 0, 0), corners[0] + new Vector3(1, -1, 0), Color.green, 2);
                DrawBone(corners[0] + new Vector3(2, 0, 0), corners[0] + new Vector3(2, -1, 0), Color.white, 1);*/
                GL.End();
            }

            _accumulatedBounds.Expand(0.05f);

            var _accumulatedPoints = new Vector3[]
            {
                new Vector3(_accumulatedBounds.min.x, _accumulatedBounds.min.y, _accumulatedBounds.min.z),
                new Vector3(_accumulatedBounds.max.x, _accumulatedBounds.min.y, _accumulatedBounds.min.z),
                new Vector3(_accumulatedBounds.max.x, _accumulatedBounds.max.y, _accumulatedBounds.min.z),
                new Vector3(_accumulatedBounds.min.x, _accumulatedBounds.max.y, _accumulatedBounds.min.z),
                new Vector3(_accumulatedBounds.min.x, _accumulatedBounds.min.y, _accumulatedBounds.max.z),
                new Vector3(_accumulatedBounds.max.x, _accumulatedBounds.min.y, _accumulatedBounds.max.z),
                new Vector3(_accumulatedBounds.max.x, _accumulatedBounds.max.y, _accumulatedBounds.max.z),
                new Vector3(_accumulatedBounds.min.x, _accumulatedBounds.max.y, _accumulatedBounds.max.z),
            };
            GL.Begin(GL.LINES);
            GL.Color(Color.red);
            DrawLine(_accumulatedPoints[0], _accumulatedPoints[1]);
            DrawLine(_accumulatedPoints[1], _accumulatedPoints[2]);
            DrawLine(_accumulatedPoints[2], _accumulatedPoints[3]);
            DrawLine(_accumulatedPoints[3], _accumulatedPoints[0]);
            DrawLine(_accumulatedPoints[4], _accumulatedPoints[5]);
            DrawLine(_accumulatedPoints[5], _accumulatedPoints[6]);
            DrawLine(_accumulatedPoints[6], _accumulatedPoints[7]);
            DrawLine(_accumulatedPoints[7], _accumulatedPoints[4]);
            DrawLine(_accumulatedPoints[0], _accumulatedPoints[4]);
            DrawLine(_accumulatedPoints[1], _accumulatedPoints[5]);
            DrawLine(_accumulatedPoints[2], _accumulatedPoints[6]);
            DrawLine(_accumulatedPoints[3], _accumulatedPoints[7]);
            GL.End();
        }
        private void DrawLine(Vector3 start, Vector3 end)
        {
            GL.Vertex(start);
            GL.Vertex(end);
        }

    private void DrawBone(Vector3 start, Vector3 end, Color color, int segments = 4)
        {
            var length = Vector3.Distance(start, end);
            if (length < 0.0001f) return;

            var radius = length * 0.1f;
            var forward = (end - start).normalized;
            var widestPoint = Vector3.Lerp(start, end, 0.2f);

            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.9f)
                up = Vector3.right;
            var right = Vector3.Cross(forward, up).normalized;
            up = Vector3.Cross(right, forward).normalized;

            var ringVertices = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (2f * Mathf.PI * i) / segments;
                var offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                ringVertices[i] = widestPoint + offset;
            }

            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < segments; i++)
            {
                GL.Vertex(start);
                GL.Vertex(ringVertices[i]);
            }

            for (int i = 0; i < segments; i++)
            {
                GL.Vertex(ringVertices[i]);
                GL.Vertex(ringVertices[(i + 1) % segments]);
            }

            for (int i = 0; i < segments; i++)
            {
                GL.Vertex(ringVertices[i]);
                GL.Vertex(end);
            }

            GL.End();
        }
    }
}