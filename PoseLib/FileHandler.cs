using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ActionGame.Chara;
using Autumn;
using Studio;
using UnityEngine;

namespace PoseLib.KKS
{
    public partial class Entry
    {
        public byte GetTargets(OCIChar character)
        {
            int targets = 0;
            if (character.oiCharInfo.enableFK)
                targets |= 0b01;
            if (character.oiCharInfo.enableIK)
                targets |= 0b10;

            return (byte)targets;
        }

        public void SaveFile(string path, Dictionary<string, ChangeAmount> data)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var bytes = screenshot.GetBytes();
                fs.Write(bytes, 0, bytes.Length);
                using (BinaryWriter header = new BinaryWriter(fs, Encoding.ASCII, true))
                {
                    header.Write(Encoding.ASCII.GetBytes("f3p"));
                    header.Write(true);
                }

                using (GZipStream compression = new GZipStream(fs, CompressionLevel.Optimal, true))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(compression))
                    {
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
                    }
                }
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
                    throw new InvalidDataException("File does not start with a PNG signature");
                
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
                    throw new InvalidDataException("Could not find the end of the file");
                }
                
                using (BinaryReader headerReader = new BinaryReader(fs, Encoding.ASCII, true))
                {
                    byte[] header = headerReader.ReadBytes(3);
                    string headerStr = Encoding.ASCII.GetString(header);
                    if (headerStr != "f3p")
                    {
                        throw new InvalidDataException("Invalid file header");
                    }

                    bool isCompressed = headerReader.ReadBoolean();

                    Stream readStream = isCompressed ? (Stream)new GZipStream(fs, CompressionMode.Decompress, true) : fs;

                    using (BinaryReader binaryReader = new BinaryReader(readStream))
                    {
                        int count = binaryReader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            string key = binaryReader.ReadString();
                            ChangeAmount value = new ChangeAmount
                            {
                                pos = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle()),
                                rot = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle()),
                                scale = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle())
                            };
                            data.Add(key, value);
                        }
                    }
                }
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
            return bones.ToDictionary(bone => bone.transform.name, bone => bone.changeAmount);
        }

        public Dictionary<string, ChangeAmount> GetIKData(OCIChar character)
        {
            var bones = character.ikCtrl.listIKInfo;
            return bones.ToDictionary(bone => bone.baseObject.name, bone => bone.guideObject.changeAmount);
        }
    }
}