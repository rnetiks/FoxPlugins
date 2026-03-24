using System;
using System.Linq;
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

        private const int CurrentVersion = 5;

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
                if (data.version >= 5)
                    LoadV5(data);
                else if (data.version >= 4)
                    LoadV4(data);
                else
                    Entry.Logger.LogWarning($"Scene data version {data.version} is no longer supported (requires v4+).");
            }
            catch (Exception ex)
            {
                Entry.Logger.LogError($"Failed to load audio from scene: {ex}");
            }
        }

        /// <summary>
        /// Loads v5 format: lane-based structure with fade, normalization, and per-lane volume.
        /// Uses TryGetValue for all fields to be forward-compatible.
        /// </summary>
        private void LoadV5(PluginData data)
        {
            var manager = Plugin?.TrackManager;
            if (manager == null) return;

            if (data.data.TryGetValue("masterVolume", out var mvObj) && mvObj is float mv)
                manager.MasterVolume = mv;

            if (!data.data.TryGetValue("laneCount", out var lcObj) || !(lcObj is int laneCount))
                return;

            for (int i = 0; i < laneCount; i++)
            {
                string lp = $"lane_{i}_";

                if (!data.data.TryGetValue(lp + "trackCount", out var tcObj) || !(tcObj is int trackCount))
                    continue;

                AudioLane lane = null;

                for (int j = 0; j < trackCount; j++)
                {
                    string tp = $"{lp}track_{j}_";

                    if (!data.data.TryGetValue(tp + "audio", out var audioObj) || !(audioObj is byte[] audioBytes) || audioBytes.Length == 0)
                        continue;

                    string fileName = "track";
                    if (data.data.TryGetValue(tp + "name", out var nameObj) && nameObj is string fn)
                        fileName = fn;

                    var track = manager.AddTrackFromBytes(audioBytes, fileName);

                    if (lane == null)
                    {
                        lane = track.Lane;
                    }
                    else if (track.Lane != lane)
                    {
                        manager.MoveTrackToLane(track, lane);
                    }

                    if (data.data.TryGetValue(tp + "displayName", out var dnObj) && dnObj is string dn)
                        track.Name = dn;

                    if (data.data.TryGetValue(tp + "offset", out var offObj) && offObj is float off)
                        track.Offset = off;

                    if (data.data.TryGetValue(tp + "trimStart", out var tsObj) && tsObj is float ts)
                        track.TrimStart = ts;

                    if (data.data.TryGetValue(tp + "trimEnd", out var teObj) && teObj is float te)
                        track.TrimEnd = te;

                    if (data.data.TryGetValue(tp + "color", out var colObj) && colObj is string colStr)
                    {
                        if (ColorUtility.TryParseHtmlString("#" + colStr, out var color))
                            track.TrackColor = color;
                    }

                    if (data.data.TryGetValue(tp + "fadeIn", out var fiObj) && fiObj is float fi)
                        track.FadeInDuration = fi;

                    if (data.data.TryGetValue(tp + "fadeOut", out var foObj) && foObj is float fo)
                        track.FadeOutDuration = fo;

                    if (data.data.TryGetValue(tp + "normMode", out var nmObj) && nmObj is int nm)
                        track.NormalizationMode = (WaveformMode)nm;

                    track.ClampTrim();
                    track.ClampFade();

                    Entry.Logger.LogInfo($"Loaded track from scene (v5): {track.Name} ({audioBytes.Length} bytes)");
                }

                if (lane != null)
                {
                    if (data.data.TryGetValue(lp + "volume", out var lvObj) && lvObj is float lv)
                        lane.Volume = lv;

                    if (data.data.TryGetValue(lp + "muted", out var lmObj) && lmObj is bool lm)
                        lane.IsMuted = lm;
                }
            }
        }

        /// <summary>
        /// Loads v4 format (backward compat): flat tracks, each mapped to its own lane.
        /// </summary>
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
                {
                    if (track.Lane != null)
                        track.Lane.Volume = vol;
                }

                if (data.data.TryGetValue(prefix + "muted", out var mutObj) && mutObj is bool muted)
                {
                    if (track.Lane != null)
                        track.Lane.IsMuted = muted;
                }

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
                foreach (var track in manager.AllTracks)
                {
                    if (track.FileName == selectedName)
                    {
                        manager.SelectTrack(track, false);
                        break;
                    }
                }
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

                var lanesWithTracks = manager.Lanes.Where(l => l.Tracks.Count > 0).ToList();
                data.data.Add("laneCount", lanesWithTracks.Count);

                for (int i = 0; i < lanesWithTracks.Count; i++)
                {
                    var lane = lanesWithTracks[i];
                    string lp = $"lane_{i}_";

                    data.data.Add(lp + "volume", lane.Volume);
                    data.data.Add(lp + "muted", lane.IsMuted);
                    data.data.Add(lp + "trackCount", lane.Tracks.Count);

                    for (int j = 0; j < lane.Tracks.Count; j++)
                    {
                        var track = lane.Tracks[j];
                        string tp = $"{lp}track_{j}_";

                        if (track.RawBytes == null || track.RawBytes.Length == 0)
                            continue;

                        data.data.Add(tp + "audio", track.RawBytes);
                        data.data.Add(tp + "name", track.FileName);
                        data.data.Add(tp + "displayName", track.Name);
                        data.data.Add(tp + "offset", track.Offset);
                        data.data.Add(tp + "trimStart", track.TrimStart);
                        data.data.Add(tp + "trimEnd", track.TrimEnd);
                        data.data.Add(tp + "color", ColorUtility.ToHtmlStringRGBA(track.TrackColor));
                        data.data.Add(tp + "fadeIn", track.FadeInDuration);
                        data.data.Add(tp + "fadeOut", track.FadeOutDuration);
                        data.data.Add(tp + "normMode", (int)track.NormalizationMode);
                    }
                }

                SetExtendedData(data);

                Entry.Logger.LogInfo($"Saved {manager.TrackCount} tracks across {lanesWithTracks.Count} lanes (v{CurrentVersion})");
            }
            catch (Exception ex)
            {
                Entry.Logger.LogError($"Failed to save audio to scene: {ex.Message}");
            }
        }
    }
}