using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Studio;
using UnityEngine;
using BepInEx.Logging;

namespace PoseLib.KKS
{
    public class PoseLibraryManager : IDisposable
    {
        private readonly Dictionary<string, Texture2D> _loadedTextures;
        private readonly PoseFileHandler _fileHandler;
        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, List<PoseFileInfo>> _directoryCache;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private const double CACHE_LIFETIME_SECONDS = 30;

        public PoseLibraryManager(ManualLogSource logger)
        {
            _logger = logger;
            _loadedTextures = new Dictionary<string, Texture2D>();
            _fileHandler = new PoseFileHandler(logger);
            _directoryCache = new Dictionary<string, List<PoseFileInfo>>();
        }

        private List<PoseSearchResult> _lastResults;
        private List<PoseFileInfo> _lastSortedFiles;
        private bool needRevalidate = false;

        public List<PoseSearchResult> SearchPoses(SearchQuery query)
        {
            try
            {
                RefreshCacheIfNeeded();
                var allFiles = GetAllPoseFiles();

                if (!QueryEqualsIgnorePage(query, _lastQuery) || needRevalidate)
                {
                    _logger.LogDebug("Had to revalidate");
                    _lastFilteredFiles = FilterFiles(allFiles, query);
                    _lastSortedFiles = SortFiles(_lastFilteredFiles, query.SortBy);
                    _lastQuery = query;
                    needRevalidate = false;
                }

                var pagedFiles = GetPagedResults(_lastSortedFiles, query.Page, query.ResultsPerPage);
                return ConvertToSearchResults(pagedFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching poses: {ex.Message}");
                return new List<PoseSearchResult>();
            }
        }

        private bool QueryEqualsIgnorePage(SearchQuery query1, SearchQuery query2)
        {
            if (query1 == null || query2 == null)
                return query1 == query2;
    
            bool textEqual = query1.Text == query2.Text;
            bool dirEqual = query1.Directory == query2.Directory;
            bool sortEqual = query1.SortBy == query2.SortBy;
    
            return textEqual && dirEqual && sortEqual;
        }

        private List<PoseFileInfo> _lastFilteredFiles;
        private SearchQuery _lastQuery;

        public int GetTotalPoseCount(SearchQuery query)
        {
            try
            {
                // RefreshCacheIfNeeded();

                // var allFiles = GetAllPoseFiles();
                // var filteredFiles = FilterFiles(allFiles, query);
                var filteredFiles = _lastFilteredFiles;
                return filteredFiles.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting pose count: {ex.Message}");
                return 0;
            }
        }

        public void SavePose(string fileName, OCIChar character, Texture2D screenshot)
        {
            try
            {
                var fullPath = Path.Combine("UserData/studio/pose", $"{fileName}.png");
                
                string directoryName = Path.GetDirectoryName(fullPath);
                UIManager._logger.LogDebug(directoryName);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                _fileHandler.SavePoseFile(fullPath, character, screenshot);

                InvalidateCache();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving pose: {ex.StackTrace}");
                throw;
            }
        }

        public void LoadPoseToCharacters(string filePath, OCIChar[] characters)
        {
            try
            {
                _fileHandler.LoadVanillaPoseFile(filePath, characters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading pose: {ex.Message}");
                throw;
            }
        }

        public void DeletePose(string filePath)
        {
            try
            {
                if (_loadedTextures.ContainsKey(filePath))
                {
                    UnityEngine.Object.Destroy(_loadedTextures[filePath]);
                    _loadedTextures.Remove(filePath);
                }

                File.Delete(filePath);
                InvalidateCache();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting pose: {ex.Message}");
                throw;
            }
        }
        
        private void RefreshCacheIfNeeded()
        {
            var now = DateTime.Now;
            if ((now - _lastCacheUpdate).TotalSeconds > CACHE_LIFETIME_SECONDS)
            {
                RefreshCache();
                _lastCacheUpdate = now;
            }
        }

        private void RefreshCache()
        {
            _directoryCache.Clear();

            var vanillaPosesPath = Path.Combine("UserData/studio", "pose");
            if (Directory.Exists(vanillaPosesPath))
            {
                CacheDirectory(vanillaPosesPath);
            }
        }

        private void CacheDirectory(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var directory = Path.GetDirectoryName(filePath);

                    var poseFileInfo = new PoseFileInfo
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileNameWithoutExtension(filePath).ToLower(),
                        Directory = directory.Replace("/", "\\"),
                        Created = fileInfo.CreationTime,
                        Modified = fileInfo.LastWriteTime,
                        FileSize = fileInfo.Length
                    };

                    if (!_directoryCache.ContainsKey(directory))
                        _directoryCache[directory] = new List<PoseFileInfo>();

                    _directoryCache[directory].Add(poseFileInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error caching directory {directoryPath}: {ex.Message}");
            }
        }

        private void InvalidateCache()
        {
            _lastCacheUpdate = DateTime.MinValue;
            needRevalidate = true;
        }

        private List<PoseFileInfo> GetAllPoseFiles()
        {
            var allFiles = new List<PoseFileInfo>();

            foreach (var directoryFiles in _directoryCache.Values)
            {
                allFiles.AddRange(directoryFiles);
            }

            return allFiles;
        }

        private List<PoseFileInfo> FilterFiles(List<PoseFileInfo> files, SearchQuery query)
        {
            var filtered = files.AsEnumerable();


            query.Directory = query.Directory.Replace("/", "\\");
            if (query.Directory != "ALL")
            {
                filtered = filtered.Where(f => f.Directory.Equals(query.Directory, StringComparison.OrdinalIgnoreCase) ||
                                               f.Directory.StartsWith(query.Directory, StringComparison.OrdinalIgnoreCase));
            }

            if (!query.Text.IsNullOrWhiteSpace())
            {
                var searchTerm = query.Text.ToLower();
                filtered = filtered.Where(f => f.FileName.Contains(searchTerm));
            }

            return filtered.ToList();
        }

        private List<PoseFileInfo> SortFiles(List<PoseFileInfo> files, SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.Name:
                    return files.OrderBy(f => f.FileName).ToList();
                case SortBy.DateCreated:
                    return files.OrderByDescending(f => f.Created).ToList();
                case SortBy.DateModified:
                    return files.OrderByDescending(f => f.Modified).ToList();
                case SortBy.NameDescending:
                    return files.OrderByDescending(f => f.FileName).ToList();
                case SortBy.DateCreatedDescending:
                    return files.OrderBy(f => f.Created).ToList();
                case SortBy.DateModifiedDescending:
                    return files.OrderBy(f => f.Modified).ToList();
                default:
                    return files.OrderBy(f => f.FileName).ToList();
            }
        }

        private List<PoseFileInfo> GetPagedResults(List<PoseFileInfo> files, int page, int resultsPerPage)
        {
            var offset = (page - 1) * resultsPerPage;
            return files.Skip(offset).Take(resultsPerPage).ToList();
        }

        private List<PoseSearchResult> ConvertToSearchResults(List<PoseFileInfo> files)
        {
            var results = new List<PoseSearchResult>();

            foreach (var file in files)
            {
                var texture = LoadPoseTexture(file.FilePath);
                if (texture == null)
                {
                    texture = CreatePlaceholderTexture(file.FileName, Path.GetExtension(file.FilePath));
                }

                results.Add(new PoseSearchResult
                {
                    FilePath = file.FilePath,
                    PreviewTexture = texture,
                    FileCreated = file.Created,
                    FileModified = file.Modified,
                    Info = new PoseInfo
                    {
                        FileName = file.FileName,
                        Directory = file.Directory,
                        Created = file.Created,
                        Modified = file.Modified
                    }
                });
            }

            return results;
        }

        private Texture2D LoadPoseTexture(string filePath)
        {
            if (_loadedTextures.TryGetValue(filePath, out var cachedTexture))
                return cachedTexture;

            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".png")
                {
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(File.ReadAllBytes(filePath));
                    texture.Apply();
                    _loadedTextures[filePath] = texture;
                    return texture;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load texture from {filePath}: {ex.Message}");
            }

            return null;
        }

        private Texture2D placeholderTexture = null;

        private Texture2D CreatePlaceholderTexture(string fileName, string fileExtension)
        {
            try
            {
                if (placeholderTexture != null)
                {
                    return placeholderTexture;
                }
                var texture = new Texture2D(1, 1);
                

                texture.SetPixel(1, 1, new Color(0.3f, 0.3f, 0.3f, 1f) );
                texture.Apply();

                placeholderTexture = texture;
                return texture;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create placeholder texture: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            foreach (var texture in _loadedTextures.Values)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            _loadedTextures.Clear();
            _directoryCache.Clear();
        }
    }
}