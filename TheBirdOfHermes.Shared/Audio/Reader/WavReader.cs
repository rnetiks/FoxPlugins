using System;
using System.IO;

namespace TheBirdOfHermes.Audio
{
    public class WavReader : IProgressAudioReader
    {
        public string[] SupportedExtensions => new[] { ".wav" };

        public bool CanRead(byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length < 12)
                return false;
            return headerBytes[0] == 'R' && headerBytes[1] == 'I'
                                         && headerBytes[2] == 'F' && headerBytes[3] == 'F'
                                         && headerBytes[8] == 'W' && headerBytes[9] == 'A'
                                         && headerBytes[10] == 'V' && headerBytes[11] == 'E';
        }

        public AudioData Read(byte[] bytes)
        {
            return ReadWithProgress(bytes, null);
        }
        public AudioData ReadWithProgress(byte[] bytes, Action<float> onProgress)
        {
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                if (new string(br.ReadChars(4)) != "RIFF") throw new Exception("Invalid WAV");
                br.ReadInt32();
                if (new string(br.ReadChars(4)) != "WAVE") throw new Exception("Invalid WAV");

                ushort format = 1;
                int channels = 0, sampleRate = 0, bits = 0;
                long dataPos = -1;
                int dataSize = 0;

                while (ms.Position + 8 <= ms.Length)
                {
                    string id = new string(br.ReadChars(4));
                    int size = br.ReadInt32();
                    long start = ms.Position;

                    if (id == "fmt ")
                    {
                        format = br.ReadUInt16();
                        channels = br.ReadUInt16();
                        sampleRate = br.ReadInt32();
                        br.ReadInt32();
                        br.ReadUInt16();
                        bits = br.ReadUInt16();
                        if (format == 0xFFFE && size >= 40)
                        {
                            br.ReadBytes(8);
                            format = br.ReadUInt16();
                        }
                    }
                    else if (id == "data")
                    {
                        dataPos = ms.Position;
                        dataSize = size;
                    }

                    ms.Position = start + size + (size & 1);
                    onProgress?.Invoke((float)ms.Position / ms.Length);
                }

                if (dataPos < 0) throw new Exception("No data chunk");
                ms.Position = dataPos;
                
                onProgress?.Invoke(1f);

                return new AudioData
                {
                    Samples = Decode(br, dataSize, format, bits),
                    SampleRate = sampleRate,
                    Channels = channels
                };
            }
        }

        private static float[] Decode(BinaryReader br, int size, ushort fmt, int bits)
        {
            byte[] raw = br.ReadBytes(size);
            int count = size / (bits / 8);
            float[] s = new float[count];

            if (fmt == 1 && bits == 16)
            {
                short[] shorts = new short[count];
                Buffer.BlockCopy(raw, 0, shorts, 0, size);
                for (int i = 0; i < count; i++)
                    s[i] = shorts[i] / 32768f;
                return s;
            }

            if (fmt == 3 && bits == 32)
            {
                Buffer.BlockCopy(raw, 0, s, 0, size);
                return s;
            }

            for (int i = 0, offset = 0; i < count; i++)
            {
                if (fmt == 3)
                {
                    s[i] = bits == 64
                        ? (float)BitConverter.ToDouble(raw, offset)
                        : BitConverter.ToSingle(raw, offset);
                }
                else
                    switch (bits)
                    {
                        case 8:  s[i] = (raw[offset] - 128) / 128f; break;
                        case 24:
                            int v = raw[offset] | (raw[offset + 1] << 8) | (raw[offset + 2] << 16);
                            s[i] = ((v & 0x800000) != 0 ? v | unchecked((int)0xFF000000) : v) / 8388608f;
                            break;
                        case 32:
                            s[i] = BitConverter.ToInt32(raw, offset) / 2147483648f;
                            break;
                    }
                offset += bits / 8;
            }
            return s;
        }
    }
}