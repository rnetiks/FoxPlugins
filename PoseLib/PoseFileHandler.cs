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
        private const string POSE_HEADER = "f3p";
        private static readonly byte[] PNG_SIGNATURE = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] IEND_SIGNATURE = Encoding.ASCII.GetBytes("IEND");
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


        public Dictionary<string, ChangeAmount> LoadPoseFile(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ValidatePngSignature(fs);
                    SkipToPoseData(fs);

                    using (var headerReader = new BinaryReader(fs, Encoding.ASCII))
                    {
                        ValidatePoseHeader(headerReader);
                        bool isCompressed = headerReader.ReadBoolean();

                        Stream readStream = isCompressed
                            ? (Stream)new GZipStream(fs, CompressionMode.Decompress)
                            : fs;

                        using (var dataReader = new BinaryReader(readStream))
                        {
                            return ReadPoseData(dataReader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load pose file {path}: {ex.Message}");
                throw;
            }
        }

        public PoseMetadata LoadPoseMetadata(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                return new PoseMetadata
                {
                    FileName = Path.GetFileNameWithoutExtension(path),
                    FilePath = path,
                    Created = fileInfo.CreationTime,
                    Modified = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length,
                    Tags = string.Empty, 
                    Version = "1.0"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load pose metadata from {path}: {ex.Message}");
                return new PoseMetadata
                {
                    FileName = Path.GetFileNameWithoutExtension(path),
                    FilePath = path,
                    Created = DateTime.Now,
                    Modified = DateTime.Now
                };
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

        private void WriteTransformData(BinaryWriter writer, ChangeAmount data)
        {
            writer.Write(data.pos.x);
            writer.Write(data.pos.y);
            writer.Write(data.pos.z);

            writer.Write(data.rot.x);
            writer.Write(data.rot.y);
            writer.Write(data.rot.z);

            writer.Write(data.scale.x);
            writer.Write(data.scale.y);
            writer.Write(data.scale.z);
        }

        private void ValidatePngSignature(FileStream fs)
        {
            var buffer = new byte[8];
            fs.Read(buffer, 0, 8);
            if (!buffer.SequenceEqual(PNG_SIGNATURE))
            {
                throw new InvalidDataException("File does not start with a PNG signature");
            }
        }

        private void SkipToPoseData(FileStream fs)
        {
            bool foundEnd = false;

            while (!foundEnd && fs.Position < fs.Length)
            {
                var buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                int chunkLength = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];

                buffer = new byte[4];
                fs.Read(buffer, 0, 4);

                if (buffer.SequenceEqual(IEND_SIGNATURE))
                {
                    fs.Seek(4, SeekOrigin.Current);
                    foundEnd = true;
                }
                else
                {
                    fs.Seek(chunkLength + 4, SeekOrigin.Current);
                }
            }

            if (!foundEnd)
            {
                throw new InvalidDataException("Could not find the end of PNG data");
            }
        }

        private void ValidatePoseHeader(BinaryReader reader)
        {
            var header = reader.ReadBytes(3);
            var headerStr = Encoding.ASCII.GetString(header);

            if (headerStr != POSE_HEADER)
            {
                throw new InvalidDataException($"Invalid pose header: expected '{POSE_HEADER}', got '{headerStr}'");
            }
        }

        private Dictionary<string, ChangeAmount> ReadPoseData(BinaryReader reader)
        {
            var data = new Dictionary<string, ChangeAmount>();
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                var value = new ChangeAmount
                {
                    pos = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()),
                    rot = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()),
                    scale = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle())
                };

                data.Add(key, value);
            }

            return data;
        }
    }

    public class PoseMetadata
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Tags { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public long FileSize { get; set; }
        public string Version { get; set; } = "1.0";
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}