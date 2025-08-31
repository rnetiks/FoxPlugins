using UnityEngine;

namespace PoseLib.KKS
{
    public static class Constants
    {
        public const string GUID = "org.fox.poselib";
        public const string NAME = "PoseLibrary";
        public const string VERSION = "1.3.0";
        
        public const string TEXTURES_DIRECTORY = "Fox-Textures";
        public const string BACKGROUND_IMAGE_PATH = "./wb.png";
        
        public const int WINDOW_ID = 31;
        public const int UI_OFFSET_X = 5;
        public const int UI_OFFSET_Y = 20;
        public const int PREVIEW_SIZE = 256;
        public const int MIN_PREVIEW_COLUMNS = 3;
        public const int MAX_PREVIEW_COLUMNS = 8;
        public const int DEFAULT_PREVIEW_COLUMNS = 5;
        public const int MAX_POSES_PER_PAGE = 8;

        public const float SEARCH_COOLDOWN_DURATION = 2f;

        public static readonly Color32 BORDER_COLOR = new Color32(0, 119, 255, 255);
        public static readonly Color32 BACKGROUND_COLOR = new Color32(220, 220, 220, 255);
        public const float BACKGROUND_OPACITY = 0.7f;
    }
}