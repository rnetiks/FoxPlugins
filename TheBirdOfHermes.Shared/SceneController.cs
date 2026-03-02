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
