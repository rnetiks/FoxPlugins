using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    public partial class Entry
    {
        public void SaveFile(string path, Dictionary<string, ChangeAmount> data)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var bytes = screenshot.GetBytes();

                fs.Write(bytes, 0, bytes.Length);

                BinaryWriter header = new BinaryWriter(fs, Encoding.ASCII);
                header.Write(Encoding.ASCII.GetBytes("f3p"));
                header.Write(true);

                GZipStream compression = new GZipStream(fs, CompressionMode.Compress);
                BinaryWriter binaryWriter = new BinaryWriter(compression);

                binaryWriter.Write(data.Count);

                foreach (var bone in data)
                {
                    binaryWriter.Write(bone.Key);
                    binaryWriter.Write(bone.Value.pos.x);
                    binaryWriter.Write(bone.Value.pos.y);
                    binaryWriter.Write(bone.Value.pos.z);
                    binaryWriter.Write(bone.Value.rot.x);
                    binaryWriter.Write(bone.Value.rot.y);
                    binaryWriter.Write(bone.Value.rot.z);
                    binaryWriter.Write(bone.Value.scale.x);
                    binaryWriter.Write(bone.Value.scale.y);
                    binaryWriter.Write(bone.Value.scale.z);
                }
                
                compression.Dispose();
            }
        }


        public Dictionary<string, ChangeAmount> LoadFile(string path)
        {
            Dictionary<string, ChangeAmount> data = new Dictionary<string, ChangeAmount>();
            
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
                byte[] buffer = new byte[8];
                fs.Read(buffer, 0, 8);
                if (!buffer.SequenceEqual(signature))
                {
                    Logger.LogError("File does not start with a PNG signature");
                    throw new Exception();
                }
                
                byte[] iendSignature = Encoding.ASCII.GetBytes("IEND");
                bool foundEnd = false;
                
                while (!foundEnd && fs.Position < fs.Length)
                {
                    buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    int chunkLength = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
                    buffer = new byte[4];
                    fs.Read(buffer, 0, 4);

                    if (buffer.SequenceEqual(iendSignature))
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
                    Logger.LogError("Could not find the end of the file");
                    throw new Exception();
                }

                BinaryReader headerReader = new BinaryReader(fs, Encoding.ASCII);

                byte[] header = headerReader.ReadBytes(3);

                string headerStr = Encoding.ASCII.GetString(header);

                if (headerStr != "f3p")
                {
                    Logger.LogError("Invalid file header");
                    throw new Exception();
                }
                
                bool isCompressed = headerReader.ReadBoolean();
                
                
                
                Stream readStream =
                    isCompressed ? (Stream)new GZipStream(fs, CompressionMode.Decompress) : fs;
                
                using (BinaryReader binaryReader = new BinaryReader(readStream))
                {
                    int count = binaryReader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string key = binaryReader.ReadString();
                        ChangeAmount value = new ChangeAmount
                        {
                            pos = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(),
                                binaryReader.ReadSingle()),

                            rot = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(),
                                binaryReader.ReadSingle()),

                            scale = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(),
                                binaryReader.ReadSingle())
                        };

                        data.Add(key, value);
                    }
                }
                readStream.Dispose();
            }
            
            return data;
        }


        private void SetFkData(OCIChar ociChar, Dictionary<string, ChangeAmount> targets)
        {
            var bones = ociChar.fkCtrl.listBones;
            foreach (var bone in bones)
            {
                if (!targets.TryGetValue(bone.transform.name, out var amount)) continue;
                bone.changeAmount.rot = amount.rot;
                bone.changeAmount.scale = amount.scale;
                bone.changeAmount.pos = amount.pos;
            }
        }


        private Dictionary<string, ChangeAmount> GetFkData(OCIChar character)
        {
            var bones = character.fkCtrl.listBones;
            Dictionary<string, ChangeAmount> dictionary = new Dictionary<string, ChangeAmount>();
            foreach (var bone in bones) dictionary.Add(bone.transform.name, bone.changeAmount);
            return dictionary;
        }

        public Dictionary<string, ChangeAmount> GetIKData(OCIChar character)
        {
            var bones = character.ikCtrl.listIKInfo;
            Dictionary<string, ChangeAmount> fallbackData = new Dictionary<string, ChangeAmount>();
            foreach (var bone in bones) fallbackData.Add(bone.baseObject.name, bone.guideObject.changeAmount);
            return fallbackData;
        }
    }
}