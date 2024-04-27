using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StpTool
{
    enum LipAnim : uint
    {
        A = 0,
        AH = 1,
        AY = 2,
        B = 3,
        C = 4,
        CH = 5,
        D = 6,
        E = 7,
        EE = 8,
        ER = 9,
        F = 10,
        G = 11,
        H = 12,
        I = 13,
        J = 14,
        L = 15,
        M = 16,
        N = 17,
        NG = 18,
        OH = 19,
        OO = 20,
        OU = 21,
        OW = 22,
        OY = 23,
        P = 24,
        R = 25,
        S = 26,
        SH = 27,
        T = 28,
        TH = 29,
        TT = 30,
        U = 31,
        V = 32,
        W = 33,
        Y = 34,
        Z = 35,
        _i = 36,
        _tH = 37,
    };
    public class LsTrackKey
    {
        public ushort Time = new ushort();
        public ushort Intensity = new ushort();
        List<LipAnim> LipAnims = new List<LipAnim>();
        List<float> Strengths = new List<float>();
        public void ReadBinary(BinaryReader reader, Version version)
        {
            Time = reader.ReadUInt16();
            Intensity = reader.ReadUInt16();
            byte lipAnimCount = reader.ReadByte();
            byte strengthCount = reader.ReadByte();
            reader.BaseStream.Position += 2;
            Console.WriteLine($"		Time: {Time}, Intensity: {Intensity}");
            for (int i = 0; i < lipAnimCount; i++)
            {
                LipAnims.Add((LipAnim)reader.ReadUInt32());
                Console.WriteLine($"			Lip anim #{i}: {Enum.GetName(typeof(LipAnim), LipAnims[i])}");
            };
            for (int i = 0; i < strengthCount; i++)
            {
                Strengths.Add(reader.ReadSingle());
                Console.WriteLine($"			Strength #{i}: {Strengths[i]}");
            };
        }
        public void WriteBinary(BinaryWriter writer, Version version)
        {
            if (version == Version.TPP && Strengths.Count == 0)
                Strengths.Add(1);
            else if (version == Version.GZ)
                Strengths = new List<float>();

            writer.Write(Time);
            writer.Write(Intensity);
            writer.Write((byte)LipAnims.Count);
            writer.Write((byte)Strengths.Count);
            writer.Write((short)0);
            foreach (LipAnim lipAnim in LipAnims)
                writer.Write((int)lipAnim);
            foreach (float strength in Strengths)
                writer.Write(strength);
        }
    }
}
