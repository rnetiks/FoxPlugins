using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Addin;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TiledRenderer
{
    [BepInPlugin("org.fox.tiledrenderer", "Tiled Renderer", "1.0.0")]
    public class Entry : BaseUnityPlugin
    {
        private const int DEFAULT_TILE_COUNT = 2;
        private const int DEFAULT_TILE_WIDTH = 1920;
        private const int DEFAULT_TILE_HEIGHT = 1080;
        private const int MIN_TILE_SIZE = 512;
        private const int MAX_TILE_SIZE = 4096;
        private const int MIN_TILE_COUNT = 1;
        private const int MAX_TILE_COUNT = 20;

        private ConfigEntry<KeyboardShortcut> _openUIKey;

        private bool _uiOpen;
        private float _progress;
        private readonly HorizontalSlider tilesXHorizontalSlider = new HorizontalSlider(DEFAULT_TILE_COUNT, MIN_TILE_COUNT, MAX_TILE_COUNT, "Tiles X", "F0") { AllowUnclamped = true };
        private readonly HorizontalSlider tilesYHorizontalSlider = new HorizontalSlider(DEFAULT_TILE_COUNT, MIN_TILE_COUNT, MAX_TILE_COUNT, "Tiles Y", "F0") { AllowUnclamped = true };
        private readonly HorizontalSlider tileWidthHorizontalSlider = new HorizontalSlider(DEFAULT_TILE_WIDTH, MIN_TILE_SIZE, MAX_TILE_SIZE, "Tile Width", "F0") { AllowUnclamped = true };
        private readonly HorizontalSlider tileHeightHorizontalSlider = new HorizontalSlider(DEFAULT_TILE_HEIGHT, MIN_TILE_SIZE, MAX_TILE_SIZE, "Tile Height", "F0") { AllowUnclamped = true };

        private RenderSettings _renderSettings = new RenderSettings();

        private RenderState _renderState;
        private static bool _cameraResetProjectionMatrixBlocked;

        private void Awake()
        {
            _openUIKey = Config.Bind("General", "Open UI", new KeyboardShortcut(KeyCode.None));
            Harmony.CreateAndPatchAll(GetType());
            ImageEncoder.Initialize();
        }

        private void Update()
        {
            if (_openUIKey.Value.IsDown())
                _uiOpen = !_uiOpen;
        }

        private void OnGUI()
        {
            if (!_uiOpen) return;

            var screenRect = new Rect(100, 100, 300, 400);
            GUILayout.BeginArea(screenRect);
            DrawUI();
            GUILayout.EndArea();

            if (screenRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void DrawUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Tiled Renderer Configuration");

            DrawSliders();
            DrawPresetButtons();
            DrawRenderInfo();
            DrawRenderButton();
            DrawProgress();

            GUILayout.EndVertical();
        }

        private void DrawSliders()
        {
            _renderSettings.TilesX = (int)tilesXHorizontalSlider.Draw();
            GUILayout.Space(10);
            _renderSettings.TilesY = (int)tilesYHorizontalSlider.Draw();
            GUILayout.Space(10);
            _renderSettings.TileWidth = (int)tileWidthHorizontalSlider.Draw();
            GUILayout.Space(10);
            _renderSettings.TileHeight = (int)tileHeightHorizontalSlider.Draw();
            GUILayout.Space(10);
        }

        private void DrawPresetButtons()
        {
            if (GUILayout.Button("Detect Screen resolution"))
            {
                ApplyScreenResolution();
            }

            if (GUILayout.Button("Detect Aspect Ratio"))
            {
                ApplyAspectRatio();
            }
        }

        private void DrawRenderInfo()
        {
            int finalWidth = _renderSettings.TilesX * _renderSettings.TileWidth;
            int finalHeight = _renderSettings.TilesY * _renderSettings.TileHeight;
            GUILayout.Label($"Final Size: {finalWidth}x{finalHeight}");
            GUILayout.Space(10);

            if (finalWidth > 50_000 || finalHeight > 50_000)
                GUI.enabled = false;
            else
                GUI.enabled = true;
        }

        private void DrawRenderButton()
        {
            if (GUILayout.Button("Start Render"))
            {
                StartTileRendering();
            }
        }

        private void DrawProgress()
        {
            if (_progress > 0)
            {
                GUILayout.Label($"Progress: {_progress:P1}");
            }
        }

        private void ApplyScreenResolution()
        {
            tileWidthHorizontalSlider.Value = Screen.width;
            tileHeightHorizontalSlider.Value = Screen.height;

            if (tilesXHorizontalSlider.Value > tilesYHorizontalSlider.Value)
                tilesYHorizontalSlider.Value = tilesXHorizontalSlider.Value;
            else
                tilesXHorizontalSlider.Value = tilesYHorizontalSlider.Value;
        }

        private void ApplyAspectRatio()
        {
            float aspect = Screen.width / (float)Screen.height;
            tileHeightHorizontalSlider.Value = (int)Mathf.Ceil(tileWidthHorizontalSlider.Value / aspect);
        }

        private void StartTileRendering()
        {
            _progress = 0;
            StartCoroutine(RenderTiledImage());
        }

        private IEnumerator RenderTiledImage()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Logger.LogError("Main camera is not available");
                yield break;
            }

            _renderState = new RenderState(camera, _renderSettings);
            _cameraResetProjectionMatrixBlocked = true;

            Logger.LogDebug($"Starting tiled render: {_renderState.FinalWidth}x{_renderState.FinalHeight} " +
                            $"({_renderSettings.TilesX}x{_renderSettings.TilesY} tiles of {_renderSettings.TileWidth}x{_renderSettings.TileHeight})");

            Logger.LogDebug($"Saving tiles to {_renderState.OutputFolder}");

            yield return RenderAllTiles(camera);

            Logger.LogDebug("Tiled render completed");

            Logger.LogDebug($"Saved to: {_renderState.OutputFolder}");
            TryStitchTiles();

            ECLNP();
            _cameraResetProjectionMatrixBlocked = false;
        }

        private IEnumerator RenderAllTiles(Camera camera)
        {
            int tileIndex = 0;

            for (int y = 0; y < _renderSettings.TilesY; y++)
            {
                for (int x = 0; x < _renderSettings.TilesX; x++)
                {
                    yield return RenderSingleTile(camera, x, y, ++tileIndex);
                }

                _progress = (float)(y + 1) / _renderSettings.TilesY;
                Logger.LogDebug($"Tiled render progress: {_progress:P1}");
            }
        }

        private IEnumerator RenderSingleTile(Camera camera, int tileX, int tileY, int tileIndex)
        {
            ConfigureShaderGlobals();

            bool originalCulling = camera.useOcclusionCulling;
            camera.useOcclusionCulling = false;

            Matrix4x4 tileProjection = CalculateTileProjection(camera, tileX, tileY);
            camera.projectionMatrix = tileProjection;

            yield return null;

            Texture2D tileTexture = RenderTileToTexture(camera);

            camera.useOcclusionCulling = originalCulling;
            
            SaveTile(tileTexture, tileX, tileY, tileIndex);

            DestroyImmediate(tileTexture);
            RenderTexture.active = null;
        }

        private void ConfigureShaderGlobals()
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
        }

        private Matrix4x4 CalculateTileProjection(Camera camera, int tileX, int tileY)
        {
            float left = MapTileCoordinate(tileX, _renderSettings.TilesX);
            float right = MapTileCoordinate(tileX + 1, _renderSettings.TilesX);
            float bottom = MapTileCoordinate(tileY, _renderSettings.TilesY);
            float top = MapTileCoordinate(tileY + 1, _renderSettings.TilesY);

            return camera.orthographic
                ? CreateOrthographicTileMatrix(camera, left, right, bottom, top)
                : CreatePerspectiveTileMatrix(camera, left, right, bottom, top);
        }

        private static float MapTileCoordinate(int value, int total)
        {
            return ((float)value / total) * 2f - 1f;
        }

        private Matrix4x4 CreateOrthographicTileMatrix(Camera camera, float left, float right, float bottom, float top)
        {
            float orthoLeft = camera.orthographicSize * camera.aspect * left;
            float orthoRight = camera.orthographicSize * camera.aspect * right;
            float orthoBottom = camera.orthographicSize * bottom;
            float orthoTop = camera.orthographicSize * top;

            return Matrix4x4.Ortho(orthoLeft, orthoRight, orthoBottom, orthoTop,
                camera.nearClipPlane, camera.farClipPlane);
        }

        private Matrix4x4 CreatePerspectiveTileMatrix(Camera camera, float left, float right, float bottom, float top)
        {
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;
            float fov = camera.fieldOfView * Mathf.Deg2Rad;
            float height = 2f * near * Mathf.Tan(fov * 0.5f);
            float width = height * camera.aspect;

            float tileLeft = left * width * 0.5f;
            float tileRight = right * width * 0.5f;
            float tileBottom = bottom * height * 0.5f;
            float tileTop = top * height * 0.5f;

            return MatrixHelper.CreateFrustum(tileLeft, tileRight, tileBottom, tileTop, near, far);
        }

        private Texture2D RenderTileToTexture(Camera camera)
        {
            RenderTexture originalTarget = camera.targetTexture;
            RenderTexture originalActive = RenderTexture.active;
            Rect originalRect = camera.rect;

            camera.targetTexture = _renderState.RenderTexture;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.Render();
            camera.rect = originalRect;

            RenderTexture.active = _renderState.RenderTexture;
            Texture2D texture = new Texture2D(_renderSettings.TileWidth, _renderSettings.TileHeight,
                TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, _renderSettings.TileWidth, _renderSettings.TileHeight), 0, 0);
            texture.Apply();

            camera.targetTexture = originalTarget;
            RenderTexture.active = originalActive;

            return texture;
        }

        private void SaveTile(Texture2D texture, int tileX, int tileY, int tileIndex)
        {
            string fileName = $"{(_renderSettings.TilesY - tileY + 1):D3}_{(tileX + 1):D3}.png";
            string filePath = Path.Combine(_renderState.OutputFolder, fileName);

            byte[] pngData = ImageEncoder.EncodeToPNG(texture);
            File.WriteAllBytes(filePath, pngData);

            Logger.LogDebug($"Saved tile: {fileName} (ID: {tileIndex}, Grid: {tileX + 1}, {tileY + 1})");
        }

        private void TryStitchTiles()
        {
            try
            {
                Process.Start(_renderState.OutputFolder);
                StartVipsStitching();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to stitch tiles: {e}");
            }
        }

        private void StartVipsStitching()
        {
            string[] tileFiles = Directory.GetFiles(_renderState.OutputFolder)
                .Select(Path.GetFileName)
                .ToArray();
            string fileList = string.Join(" ", tileFiles);

            var processInfo = new ProcessStartInfo
            {
                WorkingDirectory = _renderState.OutputFolder,
                Arguments = $"arrayjoin \"{fileList}\" output.png --across {_renderSettings.TilesX}",
                FileName = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) + "/vips",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Process process = Process.Start(processInfo);
            process.OutputDataReceived += (sender, args) => Logger.LogDebug($"VIPS: {args.Data}");
            process.ErrorDataReceived += (sender, args) => Logger.LogError($"VIPS: {args.Data}");
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            foreach (string tileFile in tileFiles)
            {
                File.Delete(_renderState.OutputFolder + "/" + tileFile);
            }
        }

        private void ECLNP()
        {
            _renderState?.Dispose();
            _renderState = null;
        }

        private void OnDestroy()
        {
            ECLNP();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), nameof(Camera.ResetProjectionMatrix))]
        public static bool PreventResetProjectionMatrix(Camera __instance)
        {
            return !_cameraResetProjectionMatrixBlocked;
        }
    }

}