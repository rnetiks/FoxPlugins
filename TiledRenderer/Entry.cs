using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Prototype.UIElements;
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
        }

        private Slider tilesXSlider = new Slider(1, 1, 20, "Tiles X", $"F0"){AllowUnclamped = true};
        private Slider tilesYSlider = new Slider(1, 1, 20, "Tiles Y", $"F0"){AllowUnclamped = true};
        private Slider tileWidthSlider = new Slider(1920, 512, 4096, "Tile Width", $"F0"){AllowUnclamped = true};
        private Slider tileHeightSlider = new Slider(1080, 512, 4096, "Tile Height", $"F0"){AllowUnclamped = true};
        private void OnGUI()
        {
            if (!_uiOpen) return;

            var screenRect = new Rect(100, 100, 300, 400);
            GUILayout.BeginArea(screenRect);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Tiled Renderer Configuration");
            tilesX = (int)tilesXSlider.Draw();
            GUILayout.Space(10);
            tilesY = (int)tilesYSlider.Draw();
            GUILayout.Space(10);
            tileWidth = (int)tileWidthSlider.Draw();
            GUILayout.Space(10);
            tileHeight = (int)tileHeightSlider.Draw();
            GUILayout.Space(10);
            saveIndividualTiles = GUILayout.Toggle(saveIndividualTiles, saveIndividualTiles ? "Save tiles" : "Dry run");
            GUILayout.Space(10);
            if (GUILayout.Button("Detect Screen resolution"))
            {
                tileWidthSlider.Value = Screen.width;
                tileHeightSlider.Value = Screen.height;
                if (tilesXSlider.Value > tilesYSlider.Value)
                {
                    tilesYSlider.Value = tilesXSlider.Value;
                }
                else
                {
                    tilesXSlider.Value = tilesYSlider.Value;
                }
            }
            if (GUILayout.Button("Detect Aspect Ratio"))
            {
                float aspect = Screen.width / (float)Screen.height;
                tileHeightSlider.Value = (int)Mathf.Ceil(tileWidthSlider.Value / aspect);
            }
            long finalMemory = (long)tileWidth * tileHeight * 12;
            GUILayout.Label($"Final Size: {tilesX * tileWidth}x{tilesY * tileHeight}");
            GUILayout.Label($"Est. Tile Memory: {(finalMemory / 1024f / 1024):F1} MB");
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
        }

        public void StartTileRendering()
        {
            InitImageConversion();
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
                }
                float progress = (float)(y + 1) / tilesY;
                this.progress = progress;
                Logger.LogDebug($"Tiled render progress: {progress:P1}");
            }

            Logger.LogDebug($"Tiled render completed");
            if (saveIndividualTiles)
            {
                Logger.LogDebug($"Saved to: {currentRenderFolder}");
                string join = string.Join(" ", Directory.GetFiles(currentRenderFolder).Select(e => Path.GetFileName(e)).ToArray());
                try
                {
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
                    p.WaitForExit(); // Inefficient, but it works
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
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
            
            yield return null;
            RenderTexture targetTexture = renderCamera.targetTexture;
            RenderTexture active = RenderTexture.active;
            Texture2D tileTexture = new Texture2D(tileWidth, tileHeight, textureFormat, false);
            var rect = renderCamera.rect;
            
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

                // matrix = Matrix4x4.Frustum(tileLeft, tileRight, tileBottom, tileTop, near, far);
                matrix = CreateFrustumMatrix(tileLeft, tileRight, tileBottom, tileTop, near, far);
            }
            return matrix;
        }
        
        private static GUIStyle _style;

        private static GUIStyle Style => _style ?? (_style = new GUIStyle(GUI.skin.box));

        /// <summary>
        /// Fallback Frustum support for older unity versions
        /// </summary>
        public static Matrix4x4 CreateFrustumMatrix(float left, float right, float bottom, float top, float near, float far)
        {
            Matrix4x4 m = new Matrix4x4();

            float x = (2.0f * near) / (right - left);
            float y = (2.0f * near) / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2.0f * far * near) / (far - near);

            
            m[0, 0] = x;
            m[0, 1] = 0;
            m[0, 2] = a;
            m[0, 3] = 0;

            m[1, 0] = 0;
            m[1, 1] = y;
            m[1, 2] = b;
            m[1, 3] = 0;

            m[2, 0] = 0;
            m[2, 1] = 0;
            m[2, 2] = c;
            m[2, 3] = d;

            m[3, 0] = 0;
            m[3, 1] = 0;
            m[3, 2] = -1;
            m[3, 3] = 0;

            return m;
        }
        
        private static MethodInfo PNGEncoder, JPGEncoder;
        private static MethodInfo _modernPNG, _modernJPG, _legacyPNG, _legacyJPG;
        private static bool _initializedTextureAbstractions;
        
        private static void InitImageConversion()
        {
            try
            {
                var imageConversion = Type.GetType("UnityEngine.ImageConversion") ?? 
                                      Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetType("UnityEngine.ImageConversion") != null)?.GetType("UnityEngine.ImageConversion");

                if (imageConversion != null)
                {
                    _modernPNG = imageConversion.GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Texture2D) }, null);
                    _modernJPG = imageConversion.GetMethod("EncodeToJPG", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Texture2D) }, null);
                }

                _legacyPNG = typeof(Texture2D).GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
                _legacyJPG = typeof(Texture2D).GetMethod("EncodeToJPG", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null);
            }
            catch { }
        
            _initializedTextureAbstractions = true;
        }
        
        public static byte[] EncodeToPNG(Texture2D texture)
        {
            if (!_initializedTextureAbstractions) InitImageConversion();
        
            if (_modernPNG != null)
                return (byte[])_modernPNG.Invoke(null, new object[] { texture });
            if (_legacyPNG != null)
                return (byte[])_legacyPNG.Invoke(texture, new object[0]);
        
            throw new NotSupportedException("PNG encoding not available");
        }

        public static byte[] EncodeToJPG(Texture2D texture)
        {
            if (!_initializedTextureAbstractions) InitImageConversion();
        
            if (_modernJPG != null)
                return (byte[])_modernJPG.Invoke(null, new object[] { texture });
            if (_legacyJPG != null)
                return (byte[])_legacyJPG.Invoke(texture, new object[0]);
        
            throw new NotSupportedException("JPG encoding not available");
        }

        private void SaveIndividualTile(Texture2D tileTexture, int tileX, int tileY)
        {
            var maxTiles = tilesX * tilesY;
            string tileFileName = $"{(tilesY - tileY + 1):D3}_{(tileX + 1):D3}.png";
            string tilePath = Path.Combine(currentRenderFolder, tileFileName);
            byte[] pngData = EncodeToPNG(tileTexture);

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