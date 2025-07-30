using System;
using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    [Serializable]
    public class PoseInfo
    {
        public string FileName { get; set; }
        public string Tags { get; set; }
        public DateTime Created { get; set; }
        public Dictionary<string, ChangeAmount> PoseData { get; set; }
        
        public PoseInfo()
        {
            PoseData = new Dictionary<string, ChangeAmount>();
            Created = DateTime.Now;
        }
    }

    public class PoseSearchResult
    {
        public string FilePath { get; set; }
        public Texture2D PreviewTexture { get; set; }
        public PoseInfo Info { get; set; }
    }

    public class SearchQuery
    {
        public string Text { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int ResultsPerPage { get; set; } = Constants.MAX_POSES_PER_PAGE;
    }
}