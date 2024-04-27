using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StpTool
{
    public class LsTrack
    {
        public List<LsTrackKey> keys = new List<LsTrackKey>();
        public void ReadBinary(BinaryReader reader, Version version, bool isSab)
        {
            if (!isSab)
            {
                if (version == Version.TPP)
                {
                    ulong messageIdHash = reader.ReadUInt64();

                    Console.WriteLine($"{messageIdHash}");
                }
            }

            //header
            uint keyframeCount = reader.ReadUInt32();
            ushort[] keyframeOffsets = new ushort[keyframeCount];

            for (int i = 0; i < keyframeCount; i++)
                keyframeOffsets[i] = reader.ReadUInt16();

            if (version == Version.TPP)
                reader.AlignStream(4); //THIS ONES WIERD. ls/ls2 inconsistency

            Console.WriteLine($"    Keyframes: {keyframeCount}");
            //keys array header
            ushort defaultTime = reader.ReadUInt16();
            ushort defaultIntensity = reader.ReadUInt16();
            Console.WriteLine($"    Default time: {defaultTime}, Default intensity: {defaultIntensity}");
            byte paramsCount = reader.ReadByte();
            reader.BaseStream.Position += 3;
            int[] parameters = new int[paramsCount];

            for (int i = 0; i < paramsCount; i++)
                parameters[i] = reader.ReadInt32();

            int keyCount = parameters[1];
            uint realKeyCount = keyframeCount - 1;
            Console.WriteLine($"    Fake key count: {keyCount}, Real key count: {realKeyCount}");
            //keys
            for (int i = 0; i < realKeyCount; i++)
            {
                //Console.WriteLine($"    Key #{i}:");
                LsTrackKey key = new LsTrackKey();
                key.ReadBinary(reader, version);
                keys.Add(key);
            };
        }
        public void WriteBinary(BinaryWriter writer, Version version, string fileName, bool isSab)
        {
            if (version == Version.TPP || !isSab)
                if (ulong.TryParse(fileName, out ulong fileNameHash))
                    writer.Write(fileNameHash);
                else
                    writer.Write(Extensions.StrCode64(fileName));
            short offsetStart = (short)writer.BaseStream.Position;
            Console.WriteLine($"!!!offsetStart {offsetStart}!!!");
            int badKeyCount = keys.Count + 1;
            writer.Write(badKeyCount);
            long[] offsetsToKeyframeOffsets = new long[badKeyCount];
            short[] keyframeOffsets = new short[badKeyCount];
            for (int i = 0; i < badKeyCount; i++)
            {
                offsetsToKeyframeOffsets[i] = writer.BaseStream.Position;
                writer.Write((ushort)0);
            };
            if (version == Version.TPP)
            {
                writer.AlignStream(4);
            };

            keyframeOffsets[keyframeOffsets.GetLowerBound(0)] = (short)(writer.BaseStream.Position);
            writer.BaseStream.Position = offsetsToKeyframeOffsets[keyframeOffsets.GetLowerBound(0)];
            writer.Write((short)(keyframeOffsets[keyframeOffsets.GetLowerBound(0)] - offsetStart));
            writer.BaseStream.Position = keyframeOffsets[keyframeOffsets.GetLowerBound(0)];

            writer.Write((ushort)100); //default time
            writer.Write((ushort)0); //default intensity

            writer.Write((byte)3);
            writer.BaseStream.Position += 3;
            writer.Write(0);
            writer.Write(keys.Count);
            writer.Write(1);
            for (int i = 0; i < keys.Count; i++)
            {
                keyframeOffsets[i + 1] = (short)writer.BaseStream.Position;
                writer.BaseStream.Position = offsetsToKeyframeOffsets[i + 1];
                writer.Write((short)(keyframeOffsets[i + 1] - offsetStart));

                writer.BaseStream.Position = keyframeOffsets[i + 1];
                keys[i].WriteBinary(writer, version);
            };
            writer.AlignStream(16);
        }
    }
}
