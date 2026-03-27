using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        private const int CurrentVersion = 6;

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

            if (data.version > CurrentVersion)
            {
                Entry.Logger.LogWarning($"Scene data version {data.version} is newer than the current version {CurrentVersion}.");
            }
            try
            {
                if (data.version >= 6)
                    LoadV6(data);
                else if (data.version >= 5)
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
        /// Loads v6 format: same as v5 but with audio deduplication via audioRef.
        /// Tracks with audioRef point to another track's audio bytes instead of storing their own copy.
        /// </summary>
        private void LoadV6(PluginData data)
        {
            var manager = Plugin?.TrackManager;
            if (manager == null) return;

            if (data.data.TryGetValue("masterVolume", out var mvObj) && mvObj is float mv)
                manager.MasterVolume = mv;

            if (!data.data.TryGetValue("laneCount", out var lcObj) || !(lcObj is int laneCount))
                return;

            var audioByKey = new Dictionary<string, byte[]>();

            for (int i = 0; i < laneCount; i++)
            {
                string lp = $"lane_{i}_";
                if (!data.data.TryGetValue(lp + "trackCount", out var tcObj) || !(tcObj is int trackCount))
                    continue;

                for (int j = 0; j < trackCount; j++)
                {
                    string tp = $"{lp}track_{j}_";
                    if (data.data.TryGetValue(tp + "audio", out var audioObj) && audioObj is byte[] audioBytes && audioBytes.Length > 0)
                        audioByKey[tp] = audioBytes;
                }
            }

            for (int i = 0; i < laneCount; i++)
            {
                string lp = $"lane_{i}_";
                if (!data.data.TryGetValue(lp + "trackCount", out var tcObj) || !(tcObj is int trackCount))
                    continue;

                AudioLane lane = null;

                for (int j = 0; j < trackCount; j++)
                {
                    string tp = $"{lp}track_{j}_";

                    byte[] audioBytes = null;

                    if (audioByKey.TryGetValue(tp, out var directBytes))
                    {
                        audioBytes = directBytes;
                    }
                    else if (data.data.TryGetValue(tp + "audioRef", out var refObj) && refObj is string refKey)
                    {
                        audioByKey.TryGetValue(refKey, out audioBytes);
                    }

                    if (audioBytes == null || audioBytes.Length == 0)
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

                    Entry.Logger.LogInfo($"Loaded track from scene (v6): {track.Name} ({audioBytes.Length} bytes)");
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

                var audioHashMap = new Dictionary<string, string>();
                long savedBytes = 0;

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

                        string hash = ComputeSHA256(track.RawBytes);
                        if (audioHashMap.TryGetValue(hash, out var sourceKey))
                        {
                            data.data.Add(tp + "audioRef", sourceKey);
                            savedBytes += track.RawBytes.Length;
                        }
                        else
                        {
                            data.data.Add(tp + "audio", track.RawBytes);
                            audioHashMap[hash] = tp;
                        }

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

                string dedupInfo = savedBytes > 0 ? $", deduped {savedBytes / 1024}KB" : "";
                Entry.Logger.LogInfo($"Saved {manager.TrackCount} tracks across {lanesWithTracks.Count} lanes (v{CurrentVersion}{dedupInfo})");
            }
            catch (Exception ex)
            {
                Entry.Logger.LogError($"Failed to save audio to scene: {ex.Message}");
            }
        }
        private static string ComputeSHA256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}