using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TiledRenderer
{
    [BepInPlugin("org.fox.tiledrenderer", "Tiled Renderer", "1.0.0")]
    [BepInProcess("CharaStudio")]
    public class Entry : BaseUnityPlugin
    {
        ConfigEntry<KeyboardShortcut> _openUIKey;
        ConfigEntry<KeyboardShortcut> _render;
        public static Camera renderCamera;
        public static int tilesX = 2;
        public static int tilesY = 2;
        public static int tileWidth = 1920;
        public static int tileHeight = 1080;
        public TextureFormat textureFormat = TextureFormat.RGB24;
        public string outputPath = "TiledRender";
        private bool _uiOpen;
        public bool saveIndividualTiles = true;
        public bool renderAsync = true;
        public float delayBetweenTiles = 0.1f;
        private float progress = 0;

        private Matrix4x4 originalProjectionMatrix;
        private RenderTexture tileRenderTexture;
        private string currentRenderFolder;
        private int currentTileId;
        private int finalImageWidth;
        private int finalImageHeight;
        private string combinedImagePath;

        private void Awake()
        {
            _openUIKey = Config.Bind("General", "Open UI", new KeyboardShortcut(KeyCode.None));
            _render = Config.Bind("General", "Render", new KeyboardShortcut(KeyCode.None));
        }

        private void OnGUI()
        {
            if (!_uiOpen) return;

            var screenRect = new Rect(100, 100, 300, 400);
            GUILayout.BeginArea(screenRect);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Tiled Renderer Configuration");
            GUILayout.Label($"Tiles X: {tilesX}");
            tilesX = (int)GUILayout.HorizontalSlider(tilesX, 1, 20);
            GUILayout.Label($"Tiles Y: {tilesY}");
            tilesY = (int)GUILayout.HorizontalSlider(tilesY, 1, 20);
            GUILayout.Label($"Tile Width: {tileWidth}");
            tileWidth = (int)GUILayout.HorizontalSlider(tileWidth, 512, 4096);
            GUILayout.Label($"Tile Height: {tileHeight}");
            tileHeight = (int)GUILayout.HorizontalSlider(tileHeight, 512, 4096);
            GUILayout.Space(10);
            saveIndividualTiles = GUILayout.Toggle(saveIndividualTiles, "Save Individual Tiles");
            GUILayout.Space(10);
            long finalMemory = (long)(tilesX * tileWidth) * (tilesY * tileHeight) * 3;
            GUILayout.Label($"Final Size: {tilesX * tileWidth}x{tilesY * tileHeight}");
            GUILayout.Label($"Est. File Size: {finalMemory / 1024 / 1024} MB");
            GUILayout.Space(10);
            if (GUILayout.Button("Start Render"))
            {
                renderCamera = Camera.main;
                progress = 0;
                StartTileRendering();
            }

            if (progress > 0)
            {
                GUILayout.Label("Progress: " + progress.ToString("P1"));
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            if (screenRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();
        }

        private void Update()
        {
            if (_openUIKey.Value.IsDown())
                _uiOpen = !_uiOpen;
            if (_render.Value.IsDown())
                StartTileRendering();
        }

        public void StartTileRendering()
        {
            StartCoroutine(RenderTiledImage());
        }

        private IEnumerator RenderTiledImage()
        {
            if (renderCamera == null)
            {
                Logger.LogError("Render camera is not assigned");
                yield break;
            }
            string timestamp = DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            currentRenderFolder = Path.Combine(outputPath, $"TiledRender_{timestamp}");
            if (!Directory.Exists(currentRenderFolder) && saveIndividualTiles)
                Directory.CreateDirectory(currentRenderFolder);
            currentTileId = 0;
            originalProjectionMatrix = renderCamera.projectionMatrix;
            tileRenderTexture = new RenderTexture(tileWidth, tileHeight, 24);
            tileRenderTexture.format = RenderTextureFormat.ARGB32;
            tileRenderTexture.Create();
            finalImageWidth = tilesX * tileWidth;
            finalImageHeight = tilesY * tileHeight;

            Logger.LogDebug($"Starting tiled render: {finalImageWidth}x{finalImageHeight} ({tilesX}x{tilesY} tiles of {tileWidth}x{tileHeight})");
            Logger.LogDebug($"Saving tiles to {currentRenderFolder}");
            for (var y = 0; y < tilesY; y++)
            {
                for (var x = 0; x < tilesX; x++)
                {
                    yield return StartCoroutine(RenderTile(x, y));
                    if (renderAsync && delayBetweenTiles > 0)
                    {
                        yield return new WaitForSeconds(delayBetweenTiles);
                    }
                }
                float progress = (float)(y + 1) / tilesY;
                this.progress = progress;
                Logger.LogDebug($"Tiled render progress: {progress:P1}");
            }

            Logger.LogDebug($"Tiled render completed");
            if (saveIndividualTiles)
            {
                Logger.LogDebug($"Saved to: {currentRenderFolder}");
                string join = string.Join(" ", Directory.GetFiles(currentRenderFolder).Select(e => Path.GetFileName(e)));
                Logger.LogDebug(join);
                Logger.LogDebug($"> vips arrayjoin \"{join}\" output.png --across {tilesX}");
                Process.Start(currentRenderFolder);
                var p = Process.Start(new ProcessStartInfo()
                {
                    WorkingDirectory = currentRenderFolder,
                    Arguments = $"arrayjoin \"{join}\" output.png --across {tilesX}",
                    FileName = "vips",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.OutputDataReceived += (sender, args) => Logger.LogDebug(args.Data);
                p.ErrorDataReceived += (sender, args) => Logger.LogError(args.Data);
                p.WaitForExit();
            }
            Cleanup();
        }
        
        private IEnumerator RenderTile(int tileX, int tileY)
        {
            Shader.SetGlobalTexture("_AlphaMask", Texture2D.whiteTexture);
            Shader.SetGlobalInt("_alpha_a", 1);
            Shader.SetGlobalInt("_alpha_b", 1);
            Shader.SetGlobalInt("_LineWidthS", 1);
            currentTileId++;
            float left = (float)tileX / tilesX;
            float right = (float)(tileX + 1) / tilesX;
            float bottom = (float)tileY / tilesY;
            float top = (float)(tileY + 1) / tilesY;
            left = left * 2f - 1f;
            right = right * 2f - 1f;
            bottom = bottom * 2f - 1f;
            top = top * 2f - 1f;
            Matrix4x4 tileProjection = CreateTileProjectionMatrix(left, right, bottom, top);
            renderCamera.projectionMatrix = tileProjection;

            if (renderAsync)
                yield return new WaitForEndOfFrame();

            RenderTexture targetTexture = renderCamera.targetTexture;
            RenderTexture active = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(tileWidth, tileHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            Texture2D tileTexture = new Texture2D(tileWidth, tileHeight, textureFormat, false);
            var rect = renderCamera.rect;

            yield return new WaitForEndOfFrame();
            renderCamera.targetTexture = tileRenderTexture;
            renderCamera.rect = new Rect(0, 0, 1, 1);
            renderCamera.Render();
            renderCamera.rect = rect;

            RenderTexture.active = tileRenderTexture;
            tileTexture.ReadPixels(new Rect(0, 0, tileWidth, tileHeight), 0, 0);
            tileTexture.Apply();
            renderCamera.targetTexture = targetTexture;
            RenderTexture.active = active;

            if (saveIndividualTiles)
            {
                SaveIndividualTile(tileTexture, tileX, tileY);
            }
            DestroyImmediate(tileTexture);
            RenderTexture.active = null;
        }
        
        private Matrix4x4 CreateTileProjectionMatrix(float left, float right, float bottom, float top)
        {
            Matrix4x4 matrix = originalProjectionMatrix;
            if (renderCamera.orthographic)
            {
                float orthoLeft = renderCamera.orthographicSize * renderCamera.aspect * left;
                float orthoRight = renderCamera.orthographicSize * renderCamera.aspect * right;
                float orthoBottom = renderCamera.orthographicSize * bottom;
                float orthoTop = renderCamera.orthographicSize * top;
                matrix = Matrix4x4.Ortho(orthoLeft, orthoRight, orthoBottom, orthoTop, renderCamera.nearClipPlane, renderCamera.farClipPlane);
            }
            else
            {
                float near = renderCamera.nearClipPlane;
                float far = renderCamera.farClipPlane;

                float fov = renderCamera.fieldOfView * Mathf.Deg2Rad;
                float height = 2f * near * Mathf.Tan(fov * 0.5f);
                float width = height * renderCamera.aspect;

                float tileLeft = left * width * 0.5f;
                float tileRight = right * width * 0.5f;
                float tileBottom = bottom * height * 0.5f;
                float tileTop = top * height * 0.5f;

                matrix = Matrix4x4.Frustum(tileLeft, tileRight, tileBottom, tileTop, near, far);
            }
            return matrix;
        }
        
        private void SaveIndividualTile(Texture2D tileTexture, int tileX, int tileY)
        {
            var maxTiles = tilesX * tilesY;
            string tileFileName = $"{(tilesY - tileY + 1):D3}_{(tileX + 1):D3}.png";
            string tilePath = Path.Combine(currentRenderFolder, tileFileName);
            byte[] pngData = tileTexture.EncodeToPNG();
            File.WriteAllBytes(tilePath, pngData);
            Logger.LogDebug($"Saved tile: {tileFileName} (ID: {currentTileId}, Grid: {tileX + 1}, {tileY + 1})");
        }
        
        private void Cleanup()
        {
            if (originalProjectionMatrix != default)
                renderCamera.projectionMatrix = originalProjectionMatrix;
            renderCamera.targetTexture = null;
            if (tileRenderTexture != null)
            {
                tileRenderTexture.Release();
                DestroyImmediate(tileRenderTexture);
                tileRenderTexture = null;
            }

            currentTileId = 0;
            currentRenderFolder = null;
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
    }
}