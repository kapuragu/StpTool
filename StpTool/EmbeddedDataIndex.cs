using System;
using System.Collections.Generic;
using System.IO;

namespace StpTool
{
    public class EmbeddedDataIndex
    {
        public List<uint> FileNames = new List<uint>();
        public List<byte[]> WemFiles = new List<byte[]>();
        public void ReadSoundBank(BinaryReader reader)
        {
            //find bkhd:
            uint bankHeaderSignature = reader.ReadUInt32();
            Console.WriteLine($"signature: {bankHeaderSignature}");

            if (bankHeaderSignature!=0x44484B42) //BKHD
                throw new ArgumentOutOfRangeException();

            int bankHeaderSize = reader.ReadInt32();
            reader.BaseStream.Position += bankHeaderSize;

            //find didx:
            uint dataIndexSignature = reader.ReadUInt32();

            if (dataIndexSignature != 0x58444944) //DIDX
            {
                Console.WriteLine($"signature: {dataIndexSignature} no DIDX found!!!");
                return;
            }

            uint offsetsArraySizeInBytes = reader.ReadUInt32();
            uint fileCount = offsetsArraySizeInBytes / (0x4 * 3);
            List<int> wemStartOffsets = new List<int>();
            List<int> wemSizes = new List<int>();
            for (int i = 0; i < fileCount; i++)
            {
                FileNames.Add(reader.ReadUInt32());
                wemStartOffsets.Add(reader.ReadInt32());
                wemSizes.Add(reader.ReadInt32());
                Console.WriteLine($"Riff File #{i}: {FileNames[i]} Offset to start: {wemStartOffsets[i]} Size: {wemSizes[i]}");
            }
            reader.AlignStream(16);
            long startOfArray = reader.BaseStream.Position;
            for (int i = 0; i < fileCount; i++)
            {
                reader.BaseStream.Position = startOfArray + wemStartOffsets[i];
                WemFiles.Add(reader.ReadBytes(wemSizes[i]));
            }
        }
        public void DumpFiles(string outputPath)
        {
            foreach (uint fileName in FileNames)
            {
                int index = FileNames.IndexOf(fileName);

                if (WemFiles[index].Length > 0)
                    File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".wem", WemFiles[index]);
            }
        }
    }
}
