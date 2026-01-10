namespace TiledRenderer
{
    internal class RenderSettings
    {
        public int TilesX { get; set; } = 2;
        public int TilesY { get; set; } = 2;
        public int TileWidth { get; set; } = 1920;
        public int TileHeight { get; set; } = 1080;
        public string OutputPath { get; set; } = "TiledRender";
    }
}