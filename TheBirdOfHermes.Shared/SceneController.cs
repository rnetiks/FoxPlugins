using System;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace TheBirdOfHermes
{
    public class SceneController : SceneCustomFunctionController
    {
        public static Entry Plugin { get; set; }

        private const int CurrentVersion = 4;

        /// Handles scene loading operations, clearing or initializing audio tracks and settings based on the scene state.
        /// If the operation is a scene clear or load, existing tracks are cleared from the TrackManager. For loading,
        /// the method attempts to restore audio track data and configurations from the scene's extended data.
        /// Ensures compatibility with both modern (version 4 and above) and legacy data formats, prioritizing error
        /// logging in case of failure during the loading process.
        /// <param name="operation">Specifies the type of scene operation being performed, such as clearing or loading the scene.</param>
        /// <param name="loadedItems">Provides a read-only dictionary of loaded scene items mapped by their unique identifiers.
        /// This is used to track objects present in the scene at the time of loading.</param>
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
            {
                Plugin?.TrackManager?.ClearAll();
            }

            if (operation == SceneOperationKind.Clear)
                return;

            var data = GetExtendedData();
            if (data?.data == null)
                return;

            try
            {
                if (data.version >= 4)
                    LoadV4(data);
                else
                    LoadLegacy(data);
            }
            catch (Exception ex)
            {
                Entry.Logger.LogError($"Failed to load audio from scene: {ex.Message}");
            }
        }

        /// Loads audio tracks and settings from plugin data formatted for version 4 and above.
        /// This method processes modern data schema, retrieving track-specific details such as
        /// audio data, volume, trimming information, and metadata. The tracks are then added
        /// to the plugin's track manager and initialized with their respective properties.
        /// Tracks marked as selected in the data are highlighted, and a log entry is created
        /// for each loaded track.
        /// <param name="data">The plugin data containing track information and settings. Assumes that the data schema
        /// adheres to version 4 formatting or newer, with structured keys for track properties.</param>
        private void LoadV4(PluginData data)
        {
            var manager = Plugin?.TrackManager;
            if (manager == null) return;

            if (data.data.TryGetValue("masterVolume", out var mvObj) && mvObj is float mv)
                manager.MasterVolume = mv;

            if (!data.data.TryGetValue("trackCount", out var countObj) || !(countObj is int count))
                return;

            string selectedName = null;

            for (int i = 0; i < count; i++)
            {
                string prefix = $"track_{i}_";

                if (!data.data.TryGetValue(prefix + "audio", out var audioObj) || !(audioObj is byte[] audioBytes) || audioBytes.Length == 0)
                    continue;

                string fileName = "track";
                if (data.data.TryGetValue(prefix + "name", out var nameObj) && nameObj is string fn)
                    fileName = fn;

                var track = manager.AddTrackFromBytes(audioBytes, fileName);

                if (data.data.TryGetValue(prefix + "offset", out var offObj) && offObj is float off)
                    track.Offset = off;

                if (data.data.TryGetValue(prefix + "trimStart", out var tsObj) && tsObj is float ts)
                    track.TrimStart = ts;

                if (data.data.TryGetValue(prefix + "trimEnd", out var teObj) && teObj is float te)
                    track.TrimEnd = te;

                if (data.data.TryGetValue(prefix + "volume", out var volObj) && volObj is float vol)
                    track.Volume = vol;

                if (data.data.TryGetValue(prefix + "muted", out var mutObj) && mutObj is bool muted)
                    track.IsMuted = muted;

                if (data.data.TryGetValue(prefix + "selected", out var selObj) && selObj is bool sel && sel)
                    selectedName = fileName;

                if (data.data.TryGetValue(prefix + "color", out var colObj) && colObj is string colStr)
                {
                    if (ColorUtility.TryParseHtmlString("#" + colStr, out var color))
                        track.TrackColor = color;
                }

                if (data.data.TryGetValue(prefix + "displayName", out var dnObj) && dnObj is string dn)
                    track.Name = dn;

                track.ClampTrim();

                Entry.Logger.LogInfo($"Loaded track from scene (v4): {track.Name} ({audioBytes.Length} bytes)");
            }

            if (selectedName != null)
            {
                foreach (var track in manager.Tracks)
                {
                    if (track.FileName == selectedName)
                    {
                        manager.SelectTrack(track);
                        break;
                    }
                }
            }
        }

        /// Loads legacy audio data from the provided plugin data based on the data's version.
        /// This method supports scenarios where the scene's extended data format varies between versions,
        /// ensuring compatibility with older scene save formats.
        /// For version 3 and above, it retrieves audio data and its associated file name from updated keys.
        /// For version 2, it uses legacy keys for audio bytes and file names.
        /// For version 1 and earlier, it processes data stored under even older naming conventions.
        /// If valid audio data is found, it is loaded into the plugin's track manager, and a log entry
        /// is created to indicate the loaded audio details.
        /// <param name="data">The plugin data containing audio and associated metadata. The method adapts to different schemas based on the data's version.</param>
        private void LoadLegacy(PluginData data)
        {
            byte[] audioBytes = null;
            string fileName = "loaded";

            if (data.version >= 3)
            {
                if (data.data.TryGetValue("audio", out var audioObj) && audioObj is byte[] ab && ab.Length > 0)
                {
                    audioBytes = ab;
                    if (data.data.TryGetValue("name", out var nameObj) && nameObj is string fn)
                        fileName = fn;
                }
            }
            else if (data.version >= 2)
            {
                if (data.data.TryGetValue("audioBytes", out var audioBytesObj) && audioBytesObj is byte[] ab && ab.Length > 0)
                {
                    audioBytes = ab;
                    if (data.data.TryGetValue("fileName", out var fileNameObj) && fileNameObj is string fn)
                        fileName = fn;
                }
            }
            else
            {
                if (data.data.TryGetValue("wavBytes", out var wavBytesObj) && wavBytesObj is byte[] wb && wb.Length > 0)
                {
                    audioBytes = wb;
                    if (data.data.TryGetValue("fileName", out var fileNameObj) && fileNameObj is string fn)
                        fileName = fn;
                }
            }

            if (audioBytes != null)
            {
                Plugin?.TrackManager?.AddTrackFromBytes(audioBytes, fileName);
                Entry.Logger.LogInfo($"Loaded audio from scene (v{data.version}, legacy): {fileName} ({audioBytes.Length} bytes)");
            }
        }

        /// Handles the save operation when the scene is being saved.
        /// This method is invoked to store audio track data and associated settings into
        /// the scene's extended data. It saves the master volume, the number of tracks,
        /// as well as details for each individual audio track, including audio bytes,
        /// file name, display name, offsets, trim values, volume, mute status, selection
        /// status, and color.
        /// If there is no `TrackManager` instance or no audio tracks are present, the
        /// method exits without performing any save operation.
        /// Any exceptions encountered during the save process are caught and logged as
        /// errors.
        protected override void OnSceneSave()
        {
            var manager = Plugin?.TrackManager;
            if (manager == null || !manager.HasAudio)
                return;

            try
            {
                var data = new PluginData();
                data.version = CurrentVersion;
                data.data.Add("masterVolume", manager.MasterVolume);
                data.data.Add("trackCount", manager.TrackCount);

                for (int i = 0; i < manager.TrackCount; i++)
                {
                    var track = manager.Tracks[i];
                    string prefix = $"track_{i}_";

                    if (track.RawBytes == null || track.RawBytes.Length == 0)
                        continue;

                    data.data.Add(prefix + "audio", track.RawBytes);
                    data.data.Add(prefix + "name", track.FileName);
                    data.data.Add(prefix + "displayName", track.Name);
                    data.data.Add(prefix + "offset", track.Offset);
                    data.data.Add(prefix + "trimStart", track.TrimStart);
                    data.data.Add(prefix + "trimEnd", track.TrimEnd);
                    data.data.Add(prefix + "volume", track.Volume);
                    data.data.Add(prefix + "muted", track.IsMuted);
                    data.data.Add(prefix + "selected", track.IsSelected);
                    data.data.Add(prefix + "color", ColorUtility.ToHtmlStringRGBA(track.TrackColor));
                }

                SetExtendedData(data);

                Entry.Logger.LogInfo($"Saved {manager.TrackCount} tracks to scene (v{CurrentVersion})");
            }
            catch (Exception ex)
            {
                Entry.Logger.LogError($"Failed to save audio to scene: {ex.Message}");
            }
        }
    }
}
