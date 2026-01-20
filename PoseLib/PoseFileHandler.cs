using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    public class PoseFileHandler
    {
        private readonly ManualLogSource _logger;
        private const string VANILLA_POSE_HEADER = "【pose】";

        public PoseFileHandler(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void SavePoseFile(string path, OCIChar data, Texture2D screenshot)
        {
            if (path.IsNullOrWhiteSpace())
                path = Path.Combine("UserData/studio/pose", $"{DateTime.Now:yyyyMMddHHmmss}.png");
            PauseCtrl.FileInfo poseInfo = new PauseCtrl.FileInfo(data);
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        byte[] png = screenshot.EncodeToPNG();
                        writer.Write(png);
                        writer.Write(VANILLA_POSE_HEADER);
                        writer.Write(101);
                        writer.Write(data.oiCharInfo.sex);
                        writer.Write(Path.GetFileNameWithoutExtension(path));
                        poseInfo.Save(writer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public bool LoadVanillaPoseFile(string path, params OCIChar[] characters)
        {
            try
            {
                var extension = Path.GetExtension(path).ToLower();
                if (extension == ".png")
                    return LoadVanillaPngPose(path, characters);
                if (extension == ".dat")
                    return LoadVanillaDatPose(path, characters);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load vanilla pose file {path}: {ex.Message}");
                return false;
            }
        }
        
        private bool LoadVanillaPngPose(string path, OCIChar[] characters)
        {
            var poseInfo = new PauseCtrl.FileInfo();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new BinaryReader(fs))
                {
                    PngFile.SkipPng(reader);
                    if (reader.ReadString() != VANILLA_POSE_HEADER)
                        return false;

                    int version = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadString();
                    poseInfo.Load(reader, version);

                    foreach (var character in characters)
                        poseInfo.Apply(character);

                    return true;
                }
            }
        }

        private bool LoadVanillaDatPose(string path, OCIChar[] characters)
        {
            var poseInfo = new PauseCtrl.FileInfo();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new BinaryReader(fs))
                {
                    if (reader.ReadString() != VANILLA_POSE_HEADER)
                        return false;

                    int version = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadString();
                    poseInfo.Load(reader, version);

                    foreach (var character in characters)
                        poseInfo.Apply(character);

                    return true;
                }
            }
        }
    }
}