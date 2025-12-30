using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;

namespace Sculpt.KKS
{
    public enum FalloffMode
    {
        Linear,
        Smooth,
        Sharper,
        Constant,
        Gaussian
    }

    public enum SculptMode
    {
        Normal,      Camera,      ViewPlane,   WorldY       }

    public enum BrushType
    {
        Standard,    Smooth,      Pinch,       Inflate,     Flatten,     Grab         }

    [BepInDependency("marco.kkapi")]
    [BepInProcess("KoikatsuSunshine")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Entry : BaseUnityPlugin
    {
        public const string GUID = "org.fox.sculpt";
        public const string NAME = "Sculpt";
        public const string VERSION = "0.2.0";

        private Dictionary<Renderer, Dictionary<int, Vector3>> _vertexDeltas = new Dictionary<Renderer, Dictionary<int, Vector3>>();
        private Dictionary<Renderer, Vector3[]> _originalVertices = new Dictionary<Renderer, Vector3[]>();

        private Renderer _selectedRenderer;
        private Mesh _workingMesh;
        private Mesh _bakedMesh;
        private Vector3[] _bakedVertices;
        private int[] _bakedTriangles;

        private ConfigEntry<float> _brushRadius;
        private ConfigEntry<float> _brushStrength;
        private ConfigEntry<bool> _connectedVerticesOnly;
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<KeyboardShortcut> _toggleUIKey;
        private ConfigEntry<FalloffMode> _falloffMode;
        private ConfigEntry<SculptMode> _sculptMode;
        private ConfigEntry<BrushType> _brushType;
        private ConfigEntry<bool> _invertDirection;

        private bool _isEnabled;
        private bool _showUI;
        private bool _isTabletSculpting;
        private Vector2 _lastScreenPosition;
        private Vector2 _lastSculptPosition;
        private float _currentPressure = 1f;
        
        private const float MinMoveDistance = 2f; private const float TabletMaxX = 5000f;
        private const float TabletMaxY = 5000f;

        private Rect _windowRect = new Rect(20, 20, 300, 480);
        private Rect _windowRect2;
        private float _lastWindowCheck;
        private const float WindowCheckInterval = 0.5f;

        private Vector2 _rendererScrollPos;
        private Renderer[] _availableRenderers = Array.Empty<Renderer>();
        private string[] _rendererNames = Array.Empty<string>();

        private bool _hasHit;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal;
        private List<Vector3> _affectedVerticesWorld = new List<Vector3>();
        private List<int> _affectedVertexIndices = new List<int>();
        private Material _previewMaterial;

        private bool _isGrabbing;
        private Vector3 _grabStartHitPoint;
        private Vector3 _grabStartScreenPos;
        private Dictionary<int, Vector3> _grabStartPositions = new Dictionary<int, Vector3>();

        private Dictionary<Renderer, Dictionary<int, List<int>>> _neighborCache = new Dictionary<Renderer, Dictionary<int, List<int>>>();

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;
        }

        private void Awake()
        {
            _brushRadius = Config.Bind("Brush", "Radius", 0.05f, new ConfigDescription("Brush radius in world units", new AcceptableValueRange<float>(0.001f, 1f)));
            _brushStrength = Config.Bind("Brush", "Strength", 0.01f, new ConfigDescription("Base brush strength", new AcceptableValueRange<float>(0.001f, 0.1f)));
            _connectedVerticesOnly = Config.Bind("Brush", "Connected Vertices Only", false, "Only affect vertices connected to the initial hit vertex");
            _falloffMode = Config.Bind("Brush", "Falloff Mode", FalloffMode.Smooth, "How brush strength falls off with distance");
            _sculptMode = Config.Bind("Brush", "Sculpt Mode", SculptMode.Normal, "Direction of vertex displacement");
            _brushType = Config.Bind("Brush", "Brush Type", BrushType.Standard, "Type of sculpting operation");
            _invertDirection = Config.Bind("Brush", "Invert Direction", false, "Invert the sculpt direction (pull instead of push)");
            _toggleKey = Config.Bind("Input", "Toggle Key", new KeyboardShortcut(KeyCode.F10), "Key to toggle sculpting mode and UI");
            _toggleUIKey = Config.Bind("Input", "Toggle UI Key", new KeyboardShortcut(KeyCode.F9), "Key to hide/show UI while sculpt mode is active");

            TabletManager.Subscribe(OnTabletPackets);

            CreatePreviewMaterial();
        }

        private void CreatePreviewMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            _previewMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _previewMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _previewMaterial.SetInt("_ZWrite", 0);
            _previewMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        private void OnDestroy()
        {
            TabletManager.Unsubscribe(OnTabletPackets);
            if (_previewMaterial != null)
                Destroy(_previewMaterial);
            if (_bakedMesh != null)
                Destroy(_bakedMesh);
        }

        private void Update()
        {
            if (_toggleKey.Value.IsDown())
            {
                _isEnabled = !_isEnabled;
                _showUI = _isEnabled;
                Logger.LogInfo($"Sculpt mode: {(_isEnabled ? "ON" : "OFF")}");

                if (_isEnabled)
                    RefreshRendererList();
            }

            if (_toggleUIKey.Value.IsDown() && _isEnabled)
            {
                _showUI = !_showUI;
            }

            if (!_isEnabled)
            {
                _hasHit = false;
                return;
            }

            if (Time.unscaledTime - _lastWindowCheck > WindowCheckInterval)
            {
                UpdateWindowRect();
                _lastWindowCheck = Time.unscaledTime;
            }

            UpdateBrushPreview();
            HandleMouseInput();
        }

        private void RefreshRendererList()
        {
            var chaCtrl = MakerAPI.GetCharacterControl();
            if (chaCtrl == null)
            {
                _availableRenderers = Array.Empty<Renderer>();
                _rendererNames = Array.Empty<string>();
                return;
            }

            _availableRenderers = chaCtrl.transform
                .GetComponentsInChildren<Renderer>(true)
                .Where(r => r is SkinnedMeshRenderer || r is MeshRenderer)
                .Where(r => GetMeshFromRenderer(r) != null)
                .ToArray();

            _rendererNames = _availableRenderers
                .Select(r =>
                {
                    var mesh = GetMeshFromRenderer(r);
                    return $"{r.name} ({mesh?.vertexCount ?? 0} verts)";
                })
                .ToArray();
        }

        private Mesh GetMeshFromRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer smr)
                return smr.sharedMesh;
            if (renderer is MeshRenderer mr)
                return mr.GetComponent<MeshFilter>()?.sharedMesh;
            return null;
        }

        private void OnTabletPackets(Packet[] packets)
        {
            if (!_isEnabled || _selectedRenderer == null)
                return;

            foreach (var packet in packets)
            {
                _currentPressure = packet.pkNormalPressure / (float)TabletManager.MaxPressure;

                var screenPos = TabletToScreen(packet.pkX, packet.pkY);

                bool isPressed = packet.pkNormalPressure > 0;

                if (isPressed)
                {
                    if (!_isTabletSculpting)
                    {
                        _isTabletSculpting = true;
                        _lastSculptPosition = screenPos;
                        
                        if (_brushType.Value == BrushType.Grab)
                        {
                            StartGrab(screenPos);
                        }
                        else
                        {
                            DoSculpt(screenPos, _currentPressure);
                        }
                    }
                    else
                    {
                        if (_brushType.Value == BrushType.Grab && _isGrabbing)
                        {
                            DoGrab(screenPos);
                        }
                        else if (_brushType.Value != BrushType.Grab)
                        {
                            float moveDistance = Vector2.Distance(screenPos, _lastSculptPosition);
                            if (moveDistance >= MinMoveDistance)
                            {
                                DoSculpt(screenPos, _currentPressure);
                                _lastSculptPosition = screenPos;
                            }
                        }
                    }
                    
                    _lastScreenPosition = screenPos;
                }
                else
                {
                    _isTabletSculpting = false;
                    _isGrabbing = false;
                    _grabStartPositions.Clear();
                }
            }
        }

        private void HandleMouseInput()
        {
            if (_selectedRenderer == null)
                return;

            if (_isTabletSculpting)
                return;

            var screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (Input.GetMouseButtonDown(0) && !IsMouseOverWindow())
            {
                if (_brushType.Value == BrushType.Grab)
                {
                    StartGrab(screenPos);
                }
                
                _lastSculptPosition = screenPos;
                if (_brushType.Value != BrushType.Grab)
                {
                    DoSculpt(screenPos, 1f);
                }
            }

            if (Input.GetMouseButton(0) && !IsMouseOverWindow())
            {
                if (_brushType.Value == BrushType.Grab && _isGrabbing)
                {
                    DoGrab(screenPos);
                }
                else if (_brushType.Value != BrushType.Grab)
                {
                    float moveDistance = Vector2.Distance(screenPos, _lastSculptPosition);
                    if (moveDistance >= MinMoveDistance)
                    {
                        DoSculpt(screenPos, 1f);
                        _lastSculptPosition = screenPos;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isGrabbing = false;
                _grabStartPositions.Clear();
            }
        }

        private bool IsMouseOverWindow()
        {
            var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return _showUI && _windowRect.Contains(mousePos);
        }

        private Vector2 TabletToScreen(int tabletX, int tabletY)
        {
            float monitorWidth = Display.main.systemWidth;
            float monitorHeight = Display.main.systemHeight;

            float screenX = (tabletX / TabletMaxX) * monitorWidth;
            float screenY = (tabletY / TabletMaxY) * monitorHeight;

            float windowX = screenX - _windowRect2.x;
            float windowY = screenY - _windowRect2.y;

            float unityY = _windowRect2.height - windowY;

            return new Vector2(windowX, unityY);
        }

        private void UpdateWindowRect()
        {
            var hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero)
                return;

            POINT clientOrigin = new POINT { X = 0, Y = 0 };
            ClientToScreen(hwnd, ref clientOrigin);

            if (GetClientRect(hwnd, out RECT clientRect))
            {
                _windowRect2 = new Rect(
                    clientOrigin.X,
                    clientOrigin.Y,
                    clientRect.Right - clientRect.Left,
                    clientRect.Bottom - clientRect.Top);
            }
        }

        private void UpdateBrushPreview()
        {
            _hasHit = false;
            _affectedVerticesWorld.Clear();
            _affectedVertexIndices.Clear();

            if (_selectedRenderer == null || _workingMesh == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            var ray = camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));

            if (!RaycastMesh(ray, out Vector3 hitPoint, out Vector3 hitNormal, out int hitTriIndex))
                return;

            _hasHit = true;
            _hitPoint = hitPoint;
            _hitNormal = hitNormal;

            UpdateBakedMesh();
            
            var localHitPoint = _selectedRenderer.transform.InverseTransformPoint(hitPoint);
            float radius = _brushRadius.Value;

            for (int i = 0; i < _bakedVertices.Length; i++)
            {
                float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                if (distance < radius)
                {
                    _affectedVertexIndices.Add(i);
                    
                    var worldVert = _selectedRenderer.transform.TransformPoint(_bakedVertices[i]);
                    _affectedVerticesWorld.Add(worldVert);
                }
            }
        }

        private void UpdateBakedMesh()
        {
            if (_selectedRenderer == null || _workingMesh == null)
                return;

            if (_selectedRenderer is SkinnedMeshRenderer smr)
            {
                if (_bakedMesh == null)
                {
                    _bakedMesh = new Mesh();
                    _bakedMesh.MarkDynamic();
                }

                smr.BakeMesh(_bakedMesh);
                _bakedVertices = _bakedMesh.vertices;
                _bakedTriangles = _bakedMesh.triangles;
            }
            else
            {
                _bakedVertices = _workingMesh.vertices;
                _bakedTriangles = _workingMesh.triangles;
            }
        }

        private bool RaycastMesh(Ray worldRay, out Vector3 hitPoint, out Vector3 hitNormal, out int hitTriIndex)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.zero;
            hitTriIndex = -1;

            if (_selectedRenderer == null || _workingMesh == null)
                return false;

            UpdateBakedMesh();

            if (_bakedVertices == null || _bakedTriangles == null)
                return false;

            var transform = _selectedRenderer.transform;
            var localRay = new Ray(
                transform.InverseTransformPoint(worldRay.origin),
                transform.InverseTransformDirection(worldRay.direction).normalized
            );

            float closestDist = float.MaxValue;
            bool hit = false;

            for (int i = 0; i < _bakedTriangles.Length; i += 3)
            {
                var v0 = _bakedVertices[_bakedTriangles[i]];
                var v1 = _bakedVertices[_bakedTriangles[i + 1]];
                var v2 = _bakedVertices[_bakedTriangles[i + 2]];

                if (RayTriangleIntersect(localRay, v0, v1, v2, out float t, out Vector3 bary))
                {
                    if (t > 0 && t < closestDist)
                    {
                        closestDist = t;
                        hitTriIndex = i / 3;

                        var localHit = localRay.origin + localRay.direction * t;
                        hitPoint = transform.TransformPoint(localHit);

                        var localNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                        hitNormal = transform.TransformDirection(localNormal).normalized;

                        hit = true;
                    }
                }
            }

            return hit;
        }

        private bool RayTriangleIntersect(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t, out Vector3 bary)
        {
            t = 0;
            bary = Vector3.zero;

            const float epsilon = 1e-8f;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = Vector3.Cross(ray.direction, edge2);
            var a = Vector3.Dot(edge1, h);

            if (a > -epsilon && a < epsilon)
                return false;

            var f = 1.0f / a;
            var s = ray.origin - v0;
            var u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            var q = Vector3.Cross(s, edge1);
            var v = f * Vector3.Dot(ray.direction, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            t = f * Vector3.Dot(edge2, q);
            bary = new Vector3(1 - u - v, u, v);

            return t > epsilon;
        }

        private void StartGrab(Vector2 screenPos)
        {
            if (_selectedRenderer == null || _workingMesh == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var ray = camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));

            if (!RaycastMesh(ray, out Vector3 hitPoint, out Vector3 hitNormal, out int hitTriIndex))
                return;

            UpdateBakedMesh();
            if (_bakedVertices == null)
                return;

            _isGrabbing = true;
            _grabStartHitPoint = hitPoint;
            _grabStartScreenPos = screenPos;
            _grabStartPositions.Clear();

            var localHitPoint = _selectedRenderer.transform.InverseTransformPoint(hitPoint);
            float radius = _brushRadius.Value;
            var vertices = _workingMesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                if (distance < radius)
                {
                    _grabStartPositions[i] = vertices[i];
                }
            }
        }

        private void DoGrab(Vector2 screenPos)
        {
            if (!_isGrabbing || _selectedRenderer == null || _workingMesh == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            float depth = Vector3.Distance(camera.transform.position, _grabStartHitPoint);
            
            var startWorld = camera.ScreenToWorldPoint(new Vector3(_grabStartScreenPos.x, _grabStartScreenPos.y, depth));
            var currentWorld = camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            var worldDelta = currentWorld - startWorld;

            var localDelta = _selectedRenderer.transform.InverseTransformVector(worldDelta);
            var localHitPoint = _selectedRenderer.transform.InverseTransformPoint(_grabStartHitPoint);

            var vertices = _workingMesh.vertices;
            var deltas = GetOrCreateDeltas(_selectedRenderer);
            float radius = _brushRadius.Value;

            bool modified = false;

            foreach (var kvp in _grabStartPositions)
            {
                int i = kvp.Key;
                var startPos = kvp.Value;
                var originalVert = GetOriginalVertex(_selectedRenderer, i);

                float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                float falloff = CalculateFalloff(distance, radius);

                vertices[i] = startPos + localDelta * falloff;
                deltas[i] = vertices[i] - originalVert;
                modified = true;
            }

            if (modified)
            {
                ApplyMeshChanges(vertices);
            }
        }

        private void DoSculpt(Vector2 screenPos, float pressure)
        {
            if (_selectedRenderer == null || _workingMesh == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var ray = camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));

            if (!RaycastMesh(ray, out Vector3 hitPoint, out Vector3 hitNormal, out int hitTriIndex))
                return;

            UpdateBakedMesh();
            
            if (_bakedVertices == null || _bakedVertices.Length == 0)
                return;

            var localHitPoint = _selectedRenderer.transform.InverseTransformPoint(hitPoint);
            var localHitNormal = _selectedRenderer.transform.InverseTransformDirection(hitNormal).normalized;

            var vertices = _workingMesh.vertices;
            var normals = _workingMesh.normals;
            var deltas = GetOrCreateDeltas(_selectedRenderer);
            float radius = _brushRadius.Value;
            float strength = _brushStrength.Value * pressure;
            
            if (_invertDirection.Value)
                strength = -strength;

            Vector3 sculptDir = GetSculptDirection(camera, hitPoint, localHitNormal);

            bool modified = false;

            var affectedVerts = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                if (distance < radius)
                {
                    affectedVerts.Add(i);
                }
            }

            Vector3 flattenPlanePoint = localHitPoint;
            Vector3 flattenPlaneNormal = sculptDir;

            switch (_brushType.Value)
            {
                case BrushType.Standard:
                    foreach (int i in affectedVerts)
                    {
                        var originalVert = GetOriginalVertex(_selectedRenderer, i);
                        float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                        float falloff = CalculateFalloff(distance, radius);

                        vertices[i] += sculptDir * (strength * falloff);
                        deltas[i] = vertices[i] - originalVert;
                        modified = true;
                    }
                    break;

                case BrushType.Inflate:
                    foreach (int i in affectedVerts)
                    {
                        var originalVert = GetOriginalVertex(_selectedRenderer, i);
                        float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                        float falloff = CalculateFalloff(distance, radius);

                        Vector3 vertNormal = (normals != null && normals.Length > i) ? normals[i] : sculptDir;
                        vertices[i] += vertNormal * (strength * falloff);
                        deltas[i] = vertices[i] - originalVert;
                        modified = true;
                    }
                    break;

                case BrushType.Pinch:
                    foreach (int i in affectedVerts)
                    {
                        var originalVert = GetOriginalVertex(_selectedRenderer, i);
                        float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                        float falloff = CalculateFalloff(distance, radius);

                        Vector3 toCenter = (localHitPoint - vertices[i]).normalized;
                        vertices[i] += toCenter * (strength * falloff);
                        deltas[i] = vertices[i] - originalVert;
                        modified = true;
                    }
                    break;

                case BrushType.Smooth:
                    var neighbors = GetOrCreateNeighborCache(_selectedRenderer);
                    var smoothTargets = new Dictionary<int, Vector3>();

                    foreach (int i in affectedVerts)
                    {
                        if (!neighbors.TryGetValue(i, out var neighborList) || neighborList.Count == 0)
                            continue;

                        Vector3 avg = Vector3.zero;
                        int count = 0;
                        foreach (int n in neighborList)
                        {
                            avg += vertices[n];
                            count++;
                        }
                        if (count > 0)
                        {
                            avg /= count;
                            smoothTargets[i] = avg;
                        }
                    }

                    foreach (int i in affectedVerts)
                    {
                        if (!smoothTargets.TryGetValue(i, out var target))
                            continue;

                        var originalVert = GetOriginalVertex(_selectedRenderer, i);
                        float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                        float falloff = CalculateFalloff(distance, radius);

                        vertices[i] = Vector3.Lerp(vertices[i], target, strength * falloff * 10f);
                        deltas[i] = vertices[i] - originalVert;
                        modified = true;
                    }
                    break;

                case BrushType.Flatten:
                    foreach (int i in affectedVerts)
                    {
                        var originalVert = GetOriginalVertex(_selectedRenderer, i);
                        float distance = Vector3.Distance(_bakedVertices[i], localHitPoint);
                        float falloff = CalculateFalloff(distance, radius);

                        float distToPlane = Vector3.Dot(vertices[i] - flattenPlanePoint, flattenPlaneNormal);
                        Vector3 projected = vertices[i] - flattenPlaneNormal * distToPlane;

                        vertices[i] = Vector3.Lerp(vertices[i], projected, strength * falloff * 10f);
                        deltas[i] = vertices[i] - originalVert;
                        modified = true;
                    }
                    break;
            }

            if (modified)
            {
                ApplyMeshChanges(vertices);
            }
        }

        private Vector3 GetSculptDirection(Camera camera, Vector3 worldHitPoint, Vector3 localNormal)
        {
            switch (_sculptMode.Value)
            {
                case SculptMode.Normal:
                    return localNormal;

                case SculptMode.Camera:
                    var camDir = (camera.transform.position - worldHitPoint).normalized;
                    return _selectedRenderer.transform.InverseTransformDirection(camDir);

                case SculptMode.ViewPlane:
                    var forward = -camera.transform.forward;
                    return _selectedRenderer.transform.InverseTransformDirection(forward);

                case SculptMode.WorldY:
                    return _selectedRenderer.transform.InverseTransformDirection(Vector3.up);

                default:
                    return localNormal;
            }
        }

        private void ApplyMeshChanges(Vector3[] vertices)
        {
            _workingMesh.vertices = vertices;
            _workingMesh.RecalculateNormals();
            _workingMesh.RecalculateBounds();

            if (_selectedRenderer is SkinnedMeshRenderer smr)
            {
                smr.sharedMesh = _workingMesh;
            }
            else if (_selectedRenderer is MeshRenderer mr)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = _workingMesh;

                _bakedVertices = _workingMesh.vertices;
            }
        }

        private Dictionary<int, List<int>> GetOrCreateNeighborCache(Renderer renderer)
        {
            if (_neighborCache.TryGetValue(renderer, out var cache))
                return cache;

            cache = new Dictionary<int, List<int>>();
            var triangles = _workingMesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                AddNeighbor(cache, v0, v1);
                AddNeighbor(cache, v0, v2);
                AddNeighbor(cache, v1, v0);
                AddNeighbor(cache, v1, v2);
                AddNeighbor(cache, v2, v0);
                AddNeighbor(cache, v2, v1);
            }

            _neighborCache[renderer] = cache;
            return cache;
        }

        private void AddNeighbor(Dictionary<int, List<int>> cache, int vertex, int neighbor)
        {
            if (!cache.TryGetValue(vertex, out var list))
            {
                list = new List<int>();
                cache[vertex] = list;
            }
            if (!list.Contains(neighbor))
                list.Add(neighbor);
        }

        private float CalculateFalloff(float distance, float radius)
        {
            float normalized = distance / radius;

            switch (_falloffMode.Value)
            {
                case FalloffMode.Linear:
                    return 1f - normalized;

                case FalloffMode.Smooth:
                    float t = 1f - normalized;
                    return t * t;

                case FalloffMode.Sharper:
                    float s = 1f - normalized;
                    return s * s * s;

                case FalloffMode.Constant:
                    return 1f;

                case FalloffMode.Gaussian:
                    return Mathf.Exp(-3f * normalized * normalized);

                default:
                    return 1f - normalized;
            }
        }

        private Dictionary<int, Vector3> GetOrCreateDeltas(Renderer renderer)
        {
            if (!_vertexDeltas.TryGetValue(renderer, out var deltas))
            {
                deltas = new Dictionary<int, Vector3>();
                _vertexDeltas[renderer] = deltas;
            }
            return deltas;
        }

        private Vector3 GetOriginalVertex(Renderer renderer, int index)
        {
            if (_originalVertices.TryGetValue(renderer, out var verts))
            {
                return verts[index];
            }
            return Vector3.zero;
        }

        public void SelectRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                _selectedRenderer = null;
                _workingMesh = null;
                return;
            }

            Mesh sharedMesh = GetMeshFromRenderer(renderer);

            if (sharedMesh == null)
            {
                Logger.LogError($"Could not get shared mesh for renderer {renderer.name}");
                return;
            }

            if (!_originalVertices.ContainsKey(renderer))
            {
                _originalVertices[renderer] = sharedMesh.vertices.ToArray();
            }

            _workingMesh = Instantiate(sharedMesh);
            _workingMesh.name = sharedMesh.name + "_Sculpt";
            _workingMesh.MarkDynamic();
            
            _neighborCache.Remove(renderer);

            if (_vertexDeltas.TryGetValue(renderer, out var deltas) && deltas.Count > 0)
            {
                var verts = _workingMesh.vertices;
                foreach (var kvp in deltas)
                {
                    if (kvp.Key < verts.Length)
                    {
                        verts[kvp.Key] = _originalVertices[renderer][kvp.Key] + kvp.Value;
                    }
                }
                _workingMesh.vertices = verts;
                _workingMesh.RecalculateNormals();
            }

            if (renderer is SkinnedMeshRenderer smr)
            {
                smr.sharedMesh = _workingMesh;
                
                if (_bakedMesh == null)
                    _bakedMesh = new Mesh();
                _bakedMesh.MarkDynamic();
                smr.BakeMesh(_bakedMesh);
                _bakedVertices = _bakedMesh.vertices;
                _bakedTriangles = _bakedMesh.triangles;
            }
            else if (renderer is MeshRenderer mr)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = _workingMesh;
                    
                _bakedVertices = _workingMesh.vertices;
                _bakedTriangles = _workingMesh.triangles;
            }

            _selectedRenderer = renderer;
            Logger.LogInfo($"Selected renderer: {renderer.name} ({_workingMesh.vertexCount} vertices)");
        }

        public void RestoreMesh(Renderer renderer)
        {
            if (renderer == null)
                return;

            _vertexDeltas.Remove(renderer);

            if (!_originalVertices.TryGetValue(renderer, out var originalVerts))
                return;

            Mesh mesh = GetMeshFromRenderer(renderer);

            if (mesh != null)
            {
                mesh.vertices = originalVerts.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
        }

        private void RestoreAllMeshes()
        {
            foreach (var renderer in _vertexDeltas.Keys.ToList())
            {
                RestoreMesh(renderer);
            }
        }

        public Dictionary<Renderer, Dictionary<int, Vector3>> GetAllDeltas()
        {
            return new Dictionary<Renderer, Dictionary<int, Vector3>>(_vertexDeltas);
        }

        public void LoadDeltas(Renderer renderer, Dictionary<int, Vector3> deltas)
        {
            _vertexDeltas[renderer] = new Dictionary<int, Vector3>(deltas);

            if (_selectedRenderer == renderer)
            {
                SelectRenderer(renderer);
            }
        }

        private void OnGUI()
        {
            if (!_isEnabled || !_showUI)
                return;

            _windowRect = GUILayout.Window(9823745, _windowRect, DrawWindow, "Sculpt");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            var chaCtrl = MakerAPI.GetCharacterControl();
            if (chaCtrl == null)
            {
                GUILayout.Label("No character in maker!");
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            GUILayout.Label($"Character: {chaCtrl.fileParam.fullname}");

            GUILayout.Space(10);

            if (GUILayout.Button("Refresh Renderers"))
            {
                RefreshRendererList();
            }

            GUILayout.Space(5);

            GUILayout.Label("Select Renderer:");
            _rendererScrollPos = GUILayout.BeginScrollView(_rendererScrollPos, GUILayout.Height(120));

            for (int i = 0; i < _availableRenderers.Length; i++)
            {
                bool isSelected = _selectedRenderer == _availableRenderers[i];
                var style = isSelected ? GUI.skin.box : GUI.skin.button;

                if (GUILayout.Button(_rendererNames[i], style))
                {
                    SelectRenderer(_availableRenderers[i]);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.Label("Brush Settings", GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Radius: {_brushRadius.Value:F3}");
            _brushRadius.Value = GUILayout.HorizontalSlider(_brushRadius.Value, 0.001f, 1f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Strength: {_brushStrength.Value:F3}");
            _brushStrength.Value = GUILayout.HorizontalSlider(_brushStrength.Value, 0.001f, 0.1f, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            DrawEnumSelector("Brush:", ref _brushType);

            DrawEnumSelector("Direction:", ref _sculptMode);

            DrawEnumSelector("Falloff:", ref _falloffMode);

            GUILayout.BeginHorizontal();
            _invertDirection.Value = GUILayout.Toggle(_invertDirection.Value, "Invert Direction");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (_selectedRenderer != null)
            {
                GUILayout.Label($"Selected: {_selectedRenderer.name}");
                GUILayout.Label($"Vertices: {_workingMesh?.vertexCount ?? 0}");

                if (_vertexDeltas.TryGetValue(_selectedRenderer, out var deltas))
                {
                    GUILayout.Label($"Modified vertices: {deltas.Count}");
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Restore Mesh"))
                {
                    RestoreMesh(_selectedRenderer);
                    SelectRenderer(_selectedRenderer);
                }
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Restore All Meshes"))
            {
                RestoreAllMeshes();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawEnumSelector<T>(string label, ref ConfigEntry<T> configEntry) where T : Enum
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(65));
            
            var values = Enum.GetValues(typeof(T));
            var currentIndex = Array.IndexOf(values, configEntry.Value);
            
            if (GUILayout.Button("<", GUILayout.Width(25)))
            {
                currentIndex = (currentIndex - 1 + values.Length) % values.Length;
                configEntry.Value = (T)values.GetValue(currentIndex);
            }
            
            GUILayout.Label(configEntry.Value.ToString(), GUI.skin.box, GUILayout.Width(70));
            
            if (GUILayout.Button(">", GUILayout.Width(25)))
            {
                currentIndex = (currentIndex + 1) % values.Length;
                configEntry.Value = (T)values.GetValue(currentIndex);
            }
            
            GUILayout.EndHorizontal();
        }

        private void OnRenderObject()
        {
            if (!_isEnabled || !_hasHit || _previewMaterial == null)
                return;

            DrawBrushPreview();
        }

        private void DrawBrushPreview()
        {
            _previewMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.5f, 0f, 0.8f));

            int segments = 32;
            float radius = _brushRadius.Value;

            var up = _hitNormal;
            var right = Vector3.Cross(up, Vector3.forward).normalized;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(up, Vector3.right).normalized;
            var forward = Vector3.Cross(right, up);

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

                var p1 = _hitPoint + (right * Mathf.Cos(angle1) + forward * Mathf.Sin(angle1)) * radius;
                var p2 = _hitPoint + (right * Mathf.Cos(angle2) + forward * Mathf.Sin(angle2)) * radius;

                GL.Vertex(p1);
                GL.Vertex(p2);
            }

            GL.Vertex(_hitPoint);
            GL.Vertex(_hitPoint + _hitNormal * radius * 0.5f);

            GL.End();

            if (_affectedVerticesWorld.Count > 0 && _bakedVertices != null)
            {
                GL.Begin(GL.QUADS);

                float pointSize = 0.002f;
                var camera = Camera.main;
                if (camera != null)
                {
                    var localHit = _selectedRenderer.transform.InverseTransformPoint(_hitPoint);
                    
                    for (int i = 0; i < _affectedVertexIndices.Count && i < _affectedVerticesWorld.Count; i++)
                    {
                        int vertIndex = _affectedVertexIndices[i];
                        var worldVert = _affectedVerticesWorld[i];
                        
                        float dist = Vector3.Distance(_bakedVertices[vertIndex], localHit);
                        float falloff = CalculateFalloff(dist, _brushRadius.Value);

                        GL.Color(new Color(1f, falloff, 0f, 0.6f));

                        var camRight = camera.transform.right * pointSize;
                        var camUp = camera.transform.up * pointSize;

                        GL.Vertex(worldVert - camRight - camUp);
                        GL.Vertex(worldVert + camRight - camUp);
                        GL.Vertex(worldVert + camRight + camUp);
                        GL.Vertex(worldVert - camRight + camUp);
                    }
                }

                GL.End();
            }

            GL.PopMatrix();
        }
    }
}