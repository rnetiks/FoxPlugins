using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Studio;
using TexFac.Universal;
using UnityEngine;
using BepInEx.Logging;

namespace PoseLib.KKS
{
    public class PoseLibraryManager : IDisposable
    {
        private readonly Dictionary<string, Texture2D> _loadedTextures;
        private readonly TextureManager _textureManager;
        private readonly PoseFileHandler _fileHandler;
        private readonly ManualLogSource _logger;

        public PoseLibraryManager(ManualLogSource logger)
        {
            _logger = logger;
            _loadedTextures = new Dictionary<string, Texture2D>();
            _textureManager = new TextureManager();
            _fileHandler = new PoseFileHandler(logger);
            EnsureDirectoriesExist();
        }

        public List<PoseSearchResult> SearchPoses(SearchQuery query)
        {
            var results = new List<PoseSearchResult>();

            try
            {
                var customPoses = SearchCustomPoses(query);
                results.AddRange(customPoses);

                var vanillaPoses = SearchVanillaPoses(query);
                results.AddRange(vanillaPoses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching poses: {ex.Message}");
            }

            return results;
        }

        public void SavePose(string fileName, string tags, Dictionary<string, ChangeAmount> poseData, BaseTextureElement screenshot)
        {
            try
            {
                var fullPath = Path.Combine(Constants.POSES_DIRECTORY, $"{fileName}.png");
                _fileHandler.SavePoseFile(fullPath, poseData, screenshot);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving pose: {ex.Message}");
                throw;
            }
        }

        public void LoadPoseToCharacters(string filePath, OCIChar[] characters)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var isVanillaPose = directory?.Contains("UserData") == true;

                if (isVanillaPose)
                {
                    _fileHandler.LoadVanillaPoseFile(filePath, characters);
                }
                else
                {
                    var poseData = _fileHandler.LoadPoseFile(filePath);
                    foreach (var character in characters)
                    {
                        ApplyFkDataToCharacter(character, poseData);
                    }
                }
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting pose: {ex.Message}");
                throw;
            }
        }

        public Dictionary<string, ChangeAmount> ExtractFkDataFromCharacter(OCIChar character)
        {
            var bones = character.fkCtrl.listBones;
            var dictionary = new Dictionary<string, ChangeAmount>();

            foreach (var bone in bones)
            {
                dictionary.Add(bone.transform.name, bone.changeAmount);
            }

            return dictionary;
        }

        public Dictionary<string, ChangeAmount> ExtractIkDataFromCharacter(OCIChar character)
        {
            var bones = character.ikCtrl.listIKInfo;
            var dictionary = new Dictionary<string, ChangeAmount>();

            foreach (var bone in bones)
            {
                dictionary.Add(bone.baseObject.name, bone.guideObject.changeAmount);
            }

            return dictionary;
        }

        private List<PoseSearchResult> SearchCustomPoses(SearchQuery query)
        {
            var results = new List<PoseSearchResult>();
            var files = Directory.GetFiles(Constants.POSES_DIRECTORY, "*.png");
            var filteredFiles = FilterFilesByQuery(files, query.Text);
            var pagedFiles = GetPagedResults(filteredFiles, query.Page, query.ResultsPerPage);

            foreach (var filePath in pagedFiles)
            {
                var texture = LoadPoseTexture(filePath);
                if (texture != null)
                {
                    results.Add(new PoseSearchResult
                    {
                        FilePath = filePath,
                        PreviewTexture = texture,
                        Info = new PoseInfo { FileName = Path.GetFileNameWithoutExtension(filePath) }
                    });
                }
            }

            return results;
        }

        private List<PoseSearchResult> SearchVanillaPoses(SearchQuery query)
        {
            var results = new List<PoseSearchResult>();
            var vanillaPosesPath = Path.Combine("UserData/studio", "pose");

            if (!Directory.Exists(vanillaPosesPath))
                return results;

            var files = Directory.GetFiles(vanillaPosesPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".dat"))
                .ToArray();

            var filteredFiles = FilterFilesByQuery(files, query.Text);
            var pagedFiles = GetPagedResults(filteredFiles, query.Page, query.ResultsPerPage);

            foreach (var filePath in pagedFiles)
            {
                var texture = LoadPoseTextureWithFallback(filePath);
                results.Add(new PoseSearchResult
                {
                    FilePath = filePath,
                    PreviewTexture = texture,
                    Info = new PoseInfo { FileName = Path.GetFileNameWithoutExtension(filePath) }
                });
            }

            return results;
        }

        private static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(Constants.POSES_DIRECTORY);
            Directory.CreateDirectory(Constants.TEXTURES_DIRECTORY);
        }

        private IEnumerable<string> FilterFilesByQuery(string[] files, string query)
        {
            if (query.IsEmptyOrWhitespace())
                return files;

            return files.Where(file =>
                Path.GetFileNameWithoutExtension(file.ToLower())
                    .Contains(query.ToLower()));
        }

        private static IEnumerable<string> GetPagedResults(IEnumerable<string> files, int page, int resultsPerPage)
        {
            var offset = (page - 1) * resultsPerPage;
            return files.Skip(offset).Take(resultsPerPage);
        }

        private Texture2D LoadPoseTexture(string filePath)
        {
            if (_loadedTextures.TryGetValue(filePath, out var cachedTexture))
                return cachedTexture;

            try
            {
                var texture = new Texture2D(1, 1);
                texture.LoadImage(File.ReadAllBytes(filePath));
                texture.Apply();
                _loadedTextures[filePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load texture from {filePath}: {ex.Message}");
                return null;
            }
        }

        private Texture2D LoadPoseTextureWithFallback(string filePath)
        {
            if (_loadedTextures.TryGetValue(filePath, out var cachedTexture))
                return cachedTexture;

            Texture2D texture = null;
            var extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".png")
            {
                try
                {
                    texture = new Texture2D(1, 1);
                    texture.LoadImage(File.ReadAllBytes(filePath));
                    texture.Apply();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to load PNG texture from {filePath}: {ex.Message}");
                }
            }

            if (texture == null)
            {
                texture = _textureManager.CreatePlaceholderTexture(Path.GetFileName(filePath), extension);
            }

            _loadedTextures[filePath] = texture;
            return texture;
        }

        private static void ApplyFkDataToCharacter(OCIChar character, Dictionary<string, ChangeAmount> poseData)
        {
            var bones = character.fkCtrl.listBones;
            foreach (var bone in bones)
            {
                if (!poseData.TryGetValue(bone.transform.name, out var amount))
                    continue;

                bone.changeAmount.rot = amount.rot;
                bone.changeAmount.scale = amount.scale;
                bone.changeAmount.pos = amount.pos;
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
            _textureManager?.Dispose();
        }
    }
}