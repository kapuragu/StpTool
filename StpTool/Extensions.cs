using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StpTool
{
    public static class Extensions
    {
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
