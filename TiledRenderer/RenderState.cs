using System;
using System.IO;
using UnityEngine;

namespace TiledRenderer
{
    internal class RenderState : IDisposable
    {
        public string OutputFolder { get; }
        public RenderTexture RenderTexture { get; }
        public int FinalWidth { get; }
        public int FinalHeight { get; }

        private readonly Camera _camera;
        private readonly Matrix4x4 _originalProjectionMatrix;

        public RenderState(Camera camera, RenderSettings settings)
        {
            _camera = camera;
            _originalProjectionMatrix = camera.projectionMatrix;

            string timestamp = DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            OutputFolder = Path.Combine(settings.OutputPath, $"TiledRender_{timestamp}");

            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            RenderTexture = new RenderTexture(settings.TileWidth, settings.TileHeight, 24)
            {
                format = RenderTextureFormat.ARGB32
            };
            RenderTexture.Create();

            FinalWidth = settings.TilesX * settings.TileWidth;
            FinalHeight = settings.TilesY * settings.TileHeight;
        }

        public void Dispose()
        {
            if (_camera != null)
            {
                _camera.projectionMatrix = _originalProjectionMatrix;
                _camera.targetTexture = null;
            }

            if (RenderTexture != null)
            {
                RenderTexture.Release();
                UnityEngine.Object.DestroyImmediate(RenderTexture);
            }
        }
    }
}