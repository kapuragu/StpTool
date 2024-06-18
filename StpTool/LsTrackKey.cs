using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

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
        public ushort Duration = new ushort();
        List<LipAnim> LipAnims = new List<LipAnim>();
        List<float> Multipliers = new List<float>();
        public void ReadBinary(BinaryReader reader, Version version)
        {
            Time = reader.ReadUInt16();
            Duration = reader.ReadUInt16();
            byte lipAnimCount = reader.ReadByte();
            byte strengthCount = reader.ReadByte();
            reader.BaseStream.Position += 2;
            Console.WriteLine($"		Time: {Time}, Intensity: {Duration}");
            for (int i = 0; i < lipAnimCount; i++)
            {
                LipAnims.Add((LipAnim)reader.ReadUInt32());
                Console.WriteLine($"			Lip anim #{i}: {Enum.GetName(typeof(LipAnim), LipAnims[i])}");
            };
            for (int i = 0; i < strengthCount; i++)
            {
                Multipliers.Add(reader.ReadSingle());
                Console.WriteLine($"			Strength #{i}: {Multipliers[i]}");
            };
        }
        public void WriteBinary(BinaryWriter writer, Version version)
        {
            if (version == Version.TPP && Multipliers.Count == 0)
                Multipliers.Add(1);
            else if (version == Version.GZ)
                Multipliers = new List<float>();

            writer.Write(Time);
            writer.Write(Duration);
            writer.Write((byte)LipAnims.Count);
            writer.Write((byte)Multipliers.Count);
            writer.Write((short)0);
            foreach (LipAnim lipAnim in LipAnims)
                writer.Write((int)lipAnim);
            foreach (float strength in Multipliers)
                writer.Write(strength);
        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("key");
            writer.WriteAttributeString("time", Time.ToString());
            writer.WriteAttributeString("duration", Duration.ToString());
            foreach (LipAnim lipAnim in LipAnims)
            {
                writer.WriteStartElement("pose");
                writer.WriteAttributeString("pose", lipAnim.ToString());
                writer.WriteEndElement();
            }
            foreach (float multiplier in Multipliers)
            {
                writer.WriteStartElement("multiplier");
                writer.WriteAttributeString("multiplier", multiplier.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public void ReadXml(XmlReader reader)
        {
            ushort.TryParse(reader.GetAttribute("time"), out Time);
            //Console.WriteLine($"time={Time}");
            ushort.TryParse(reader.GetAttribute("duration"), out Duration);
            //Console.WriteLine($"duration={Duration}");
            reader.ReadStartElement("key");
            var loop = true;
            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "pose":
                                if (Enum.TryParse(reader.GetAttribute("pose"), out LipAnim pose))
                                {
                                    LipAnims.Add(pose);
                                    //Console.WriteLine($"pose={pose}");
                                }
                                reader.ReadStartElement("pose");
                                break;
                            case "multiplier":
                                if (float.TryParse(reader.GetAttribute("multiplier"), out float multiplier))
                                {
                                    Multipliers.Add(multiplier);
                                    //Console.WriteLine($"multiplier={multiplier}");
                                }
                                reader.ReadStartElement("multiplier");
                                break;
                        }
                        continue;
                    case XmlNodeType.EndElement:
                        if (reader.Name=="key")
                        {
                            //Console.WriteLine("KEY END");
                            loop = false;
                            reader.ReadEndElement();
                            return;
                        }
                        continue;
                }
            }
        }
    }
}
