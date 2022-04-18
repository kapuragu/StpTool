using System;
using System.Collections.Generic;
using System.IO;

namespace StpTool
{
    public static class Extensions
    {
        public static string ReadCString(this BinaryReader reader)
        {
            var chars = new List<char>();
            var @char = reader.ReadChar();
            while (@char != '\0')
            {
                chars.Add(@char);
                @char = reader.ReadChar();
            }

            return new string(chars.ToArray());
        }
        public static void WriteCString(this BinaryWriter writer, string iString)
        {
            char[] stringChars = iString.ToCharArray();
            foreach (var chara in stringChars)
                writer.Write(chara);
        }
        public static void ReadZeroes(this BinaryReader reader, int count)
        {
            byte[] zeroes = reader.ReadBytes(count);
            foreach (byte zero in zeroes)
            {
                if (zero != 0)
                {
                    Console.WriteLine($"Padding at {reader.BaseStream.Position} isn't zero!!!");
                    throw new Exception();
                }
            }
        } //WriteZeroes
        public static void WriteZeroes(this BinaryWriter writer, int count)
        {
            byte[] array = new byte[count];

            writer.Write(array);
        } //WriteZeroes
        public static void AlignStream(this BinaryReader reader, byte div)
        {
            long pos = reader.BaseStream.Position;
            if (pos % div != 0)
                reader.BaseStream.Position += div - pos % div;
        }
        public static void AlignStream(this BinaryWriter writer, byte div)
        {
            long pos = writer.BaseStream.Position;
            if (pos % div != 0)
                writer.WriteZeroes((int)(div - pos % div));
        }
    }
}
