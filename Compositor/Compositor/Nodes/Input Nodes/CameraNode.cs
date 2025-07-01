using System;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Compositor.KK
{
    /// <summary>
    /// Represents a compositing node in the system that performs operations related to rendering from a camera.
    /// </summary>
    public class CameraNode : BaseCompositorNode
    {
        public override string Title => "Camera";
        public static string Group => "Input";

        private Camera _camera;
        private int selectedCameraIndex;
        private int selectedLayerIndex;

        private Dropdown _cameraDropdown;
        private Dropdown _cameraLayerDropdown;
        private Camera[] _availableCameras;
        private string[] _layerNames;

        private Texture2D _texture2D;
        private RenderTexture _renderTexture;

        // Configurable render resolution
        private int renderWidth = 1920;
        private int renderHeight = 1080;

        protected override void Initialize()
        {
            Size = new Vector2(400, 300);
            InitializeRenderTextures();
        }

        private void InitializeRenderTextures()
        {
            // Clean up existing textures
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.DestroyImmediate(_renderTexture);
            }

            if (_texture2D != null)
            {
                Object.DestroyImmediate(_texture2D);
            }

            _renderTexture = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
            _texture2D = new Texture2D(renderWidth, renderHeight, TextureFormat.RGBA32, false);
        }

        protected override void InitializePorts()
        {
            _outputs.Add(new NodeOutput("Image", SocketType.RGBA, new Vector2(Size.x, Size.y * 0.6f)));
            _outputs.Add(new NodeOutput("Depth", SocketType.A, new Vector2(Size.x, Size.y * 0.7f)));

            // Initialize camera dropdown
            _cameraDropdown = new Dropdown();
            _cameraDropdown.OnSelectionChanged += OnCameraSelectionChanged;

            // Initialize layer dropdown
            _cameraLayerDropdown = new Dropdown();
            _cameraLayerDropdown.OnSelectionChanged += OnLayerSelectionChanged;

            // Setup layer names
            _layerNames = new string[32];
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                _layerNames[i] = string.IsNullOrEmpty(layerName) ? $"Layer {i}" : $"{i}: {layerName}";
            }

            _cameraLayerDropdown.UpdateList(_layerNames);
            UpdateCameraList();

            if (_availableCameras != null && _availableCameras.Length > 0)
            {
                selectedCameraIndex = Mathf.Clamp(selectedCameraIndex, 0, _availableCameras.Length - 1);
                _camera = _availableCameras[selectedCameraIndex];
            }

            selectedLayerIndex = Mathf.Clamp(selectedLayerIndex, 0, 31);
        }

        private void OnCameraSelectionChanged(int index)
        {
            if (_availableCameras != null && index >= 0 && index < _availableCameras.Length)
            {
                selectedCameraIndex = index;
                _camera = _availableCameras[index];
            }
            UpdateRender();

            Entry.Logger.LogDebug($"New Camera: {_camera?.name}");
        }

        private void OnLayerSelectionChanged(int index)
        {
            selectedLayerIndex = Mathf.Clamp(index, 0, 31);
            layerMask = GetSelectedLayerMask();
            UpdateRender();
        }

        public override void DrawContent(Rect contentRect)
        {
            if (_cameraDropdown != null)
                _cameraDropdown.Draw();

            if (_cameraLayerDropdown != null)
                _cameraLayerDropdown.Draw();

            if (_texture2D != null && !_cameraDropdown.IsExpanded && !_cameraLayerDropdown.IsExpanded)
            {
                GUI.DrawTexture(new Rect(contentRect.x, contentRect.y + 30, contentRect.width, contentRect.height), _texture2D);
            }
        }

        private int layerMask = ~0;

        private void UpdateRender()
        {
            if (_camera == null || _renderTexture == null || _texture2D == null)
                return;

            // Store original camera settings
            var originalCullingMask = _camera.cullingMask;
            var originalTarget = _camera.targetTexture;
            var originalClearFlags = _camera.clearFlags;
            var originalBackgroundColor = _camera.backgroundColor;

            try
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0, 0, 0, 0);
                // Apply selected layer mask if needed
                if (selectedLayerIndex >= 0)
                {
                    _camera.cullingMask = layerMask;
                }

                _camera.targetTexture = _renderTexture;
                RenderTexture.active = _renderTexture;
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                _camera.Render();
                
                _texture2D.ReadPixels(new Rect(0, 0, renderWidth, renderHeight), 0, 0);
                _texture2D.Apply();
                RenderTexture.active = null;
            }
            finally
            {
                // Always restore camera settings
                _camera.cullingMask = originalCullingMask;
                _camera.targetTexture = originalTarget;
                _camera.clearFlags = originalClearFlags;        // ADD THIS
                _camera.backgroundColor = originalBackgroundColor; // ADD THIS
            }
        }


        public override void Process()
        {
            if (_camera == null) return;
            UpdateRender();
            // if(_outputs[1].Connections.Count > 0)
            //     Generate(Entry._bundle.LoadAsset<Shader>("assets/depth.shader"), "");

            if (_texture2D != null)
            {
                byte[] imageData = _texture2D.GetRawTextureData();
                if (_outputs.Count > 0)
                {
                    Entry.Logger.LogDebug($"CameraNode: Send data with {imageData.Length} values");
                    _outputs[0].SetValue(imageData);
                }
            }
        }
        private byte[] Generate(Shader shader, string tag)
        {
            if (_camera == null || _renderTexture == null || _texture2D == null)
                return new byte[0];

            var originalCullingMask = _camera.cullingMask;
            var originalTarget = _camera.targetTexture;
            var originalClearFlags = _camera.clearFlags;
            var originalBackgroundColor = _camera.backgroundColor;

            try
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0, 0, 0, 0);
                if (selectedLayerIndex >= 0)
                {
                    _camera.cullingMask = layerMask;
                }

                _camera.targetTexture = _renderTexture;
                RenderTexture.active = _renderTexture;
                GL.Clear(true, true, new Color(0, 0, 0, 0));

                _camera.RenderWithShader(shader, "");
                _texture2D.ReadPixels(new Rect(0, 0, renderWidth, renderHeight), 0, 0);
                _texture2D.Apply();
                RenderTexture.active = null;
                return _texture2D.GetRawTextureData();
            }
            finally
            {
                _camera.cullingMask = originalCullingMask;
                _camera.targetTexture = originalTarget;
                _camera.clearFlags = originalClearFlags;
                _camera.backgroundColor = originalBackgroundColor;
            }
        }

        private void UpdateCameraList()
        {
            _availableCameras = Object.FindObjectsOfType<Camera>();

            if (_availableCameras.Length == 0)
            {
                selectedCameraIndex = 0;
                _camera = null;
                if (_cameraDropdown != null)
                    _cameraDropdown.UpdateList(new string[] { "No Cameras Available" });
            }
            else
            {
                selectedCameraIndex = Mathf.Clamp(selectedCameraIndex, 0, _availableCameras.Length - 1);
                _camera = _availableCameras[selectedCameraIndex];

                if (_cameraDropdown != null)
                    _cameraDropdown.UpdateList(_availableCameras.Select(c => c.name).ToArray());
            }
        }

        private LayerMask GetSelectedLayerMask() => 1 << selectedLayerIndex;

        private bool IsOnSelectedLayer(GameObject obj) => obj.layer == selectedLayerIndex; // Fixed bug

        // Public method to change render resolution
        public void SetRenderResolution(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                renderWidth = width;
                renderHeight = height;
                InitializeRenderTextures();
            }
        }

        protected virtual void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.DestroyImmediate(_renderTexture);
            }

            if (_texture2D != null)
            {
                Object.DestroyImmediate(_texture2D);
            }
        }

        public void RefreshCameraList()
        {
            UpdateCameraList();
        }
    }
}