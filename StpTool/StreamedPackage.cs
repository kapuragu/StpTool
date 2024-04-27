using System;
using System.Collections.Generic;
using System.IO;

namespace StpTool
{
    public class StreamedPackage
    {
        public enum StpEndiannessSignature
        {
            Little = 0x4C505453,
            Big = 0x42505453,
        }
        public List<uint> FileNames = new List<uint>();
        public List<byte[]> WemFiles = new List<byte[]>();
        public List<byte[]> Ls2Files = new List<byte[]>();
        public void ReadPackage(BinaryReader reader)
        {
            uint signature = reader.ReadUInt32();
            Console.WriteLine($"signature: {signature}");
            switch (signature)
            {
                case (uint)StpEndiannessSignature.Little:
                    Console.WriteLine("Little Endian");
                    break;
                case (uint)StpEndiannessSignature.Big:
                    throw new NotImplementedException();
            }
            ushort count = reader.ReadUInt16();
            Console.WriteLine($"Count: {count}");
            byte version = reader.ReadByte();
            Console.WriteLine($"Version: {version}");
            reader.ReadByte();
            switch (version)
            {
                case (byte)Version.GZ:
                    break;
                case (byte)Version.TPP:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            List<int> wemStartOffsets = new List<int>();
            List<int> ls2StartOffsets = new List<int>();
            for (int i = 0; i < count; i++)
            {
                FileNames.Add(reader.ReadUInt32());
                wemStartOffsets.Add(reader.ReadInt32());
                Console.WriteLine($"Riff File {i}: {FileNames[i]} Offset to start: {wemStartOffsets[i]}");
                if (version==(byte)Version.TPP)
                {
                    ls2StartOffsets.Add(reader.ReadInt32());
                    Console.WriteLine($"Lipsync File {i}: {FileNames[i]} Offset to start :{ls2StartOffsets[i]}");
                }
            }
            reader.AlignStream(0x10);

            switch (version)
            {
                case (byte)Version.GZ:
                    foreach (uint fileName in FileNames)
                    {
                        int wemFileSize = 0;
                        int index = FileNames.IndexOf(fileName);
                        if (index < FileNames.Count-1)
                            wemFileSize = wemStartOffsets[index + 1] - wemStartOffsets[index];
                        else
                            wemFileSize = (int)(reader.BaseStream.Length - wemStartOffsets[index]);
                        WemFiles.Add(reader.ReadBytes(wemFileSize));
                        Console.WriteLine($"Riff File {index} is size of {wemFileSize}");
                    }
                    break;
                case (byte)Version.TPP:
                    foreach (uint fileName in FileNames)
                    {
                        int wemFileSize = 0;
                        int ls2FileSize = 0;
                        int index = FileNames.IndexOf(fileName);
                        ls2FileSize = wemStartOffsets[index] - ls2StartOffsets[index];
                        if (index < FileNames.Count-1)
                            wemFileSize = ls2StartOffsets[index + 1] - wemStartOffsets[index];
                        else
                            wemFileSize = (int)(reader.BaseStream.Length - wemStartOffsets[index]);
                        Ls2Files.Add(reader.ReadBytes(ls2FileSize));
                        WemFiles.Add(reader.ReadBytes(wemFileSize));
                        Console.WriteLine($"Riff File {FileNames[index]} is size of {wemFileSize}");
                        Console.WriteLine($"Lip File {FileNames[index]} is size of {ls2FileSize}");
                    }
                    break;
            }
        }
        public void ExportFiles(string outputPath)
        {
            foreach (uint fileName in FileNames)
            {
                int index = FileNames.IndexOf(fileName);

                if (WemFiles[index].Length > 0)
                    File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".wem", WemFiles[index]);

                if (Ls2Files.Count > 0)
                    if (Ls2Files[index].Length > 0)
                        File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".ls2", Ls2Files[index]);
            }
        }
        public void ImportFiles(string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetExtension(files[i]) == ".wem")
                {
                    FileNames.Add(Convert.ToUInt32(Path.GetFileNameWithoutExtension(files[i])));

                    WemFiles.Add(File.ReadAllBytes(files[i]));

                    string ls2Path = Path.ChangeExtension(files[i], ".ls2");
                    byte[] ls2 = Array.Empty<byte>();
                    if (File.Exists(ls2Path))
                        ls2 = File.ReadAllBytes(ls2Path);
                    Ls2Files.Add(ls2);
                }
            }
        }
        public void WritePackage(BinaryWriter writer, Version version)
        {
            switch (version)
            {
                case Version.GZ:
                    break;
                case Version.TPP:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.Write((uint)StpEndiannessSignature.Little);
            writer.Write((ushort)FileNames.Count);
            writer.Write((byte)version);
            writer.Write((byte)0);

            List<int> wemStartOffsetPositions = new List<int>();
            List<int> ls2StartOffsetPositions = new List<int>();
            List<int> wemStartOffsets = new List<int>();
            List<int> ls2StartOffsets = new List<int>();

            foreach (uint fileName in FileNames)
            {
                writer.Write(fileName);
                wemStartOffsetPositions.Add((int)writer.BaseStream.Position);
                writer.Write(0);//offset
                if (version==Version.TPP)
                {
                    ls2StartOffsetPositions.Add((int)writer.BaseStream.Position);
                    writer.Write(0);//offset
                }
            }

            writer.AlignStream(0x10);

            switch (version)
            {
                case Version.GZ:
                    foreach (byte[] wemFile in WemFiles)
                    {
                        int index = WemFiles.IndexOf(wemFile);
                        int length = wemFile.Length;
                        wemStartOffsets.Add((int)writer.BaseStream.Position);
                        writer.Write(wemFile);
                        Console.WriteLine($"Riff File {index} is size of {length}");
                    }
                    break;
                case Version.TPP:
                    foreach (byte[] wemFile in WemFiles)
                    {
                        int index = WemFiles.IndexOf(wemFile);
                        byte[] ls2File = Ls2Files[index];
                        int wemLength = wemFile.Length;
                        Console.WriteLine($"Riff File {index} is size of {wemLength}");
                        int ls2Length = ls2File.Length;
                        Console.WriteLine($"Lip File {index} is size of {ls2Length}");
                        ls2StartOffsets.Add((int)writer.BaseStream.Position);
                        if (ls2Length > 0)
                            writer.Write(ls2File);
                        writer.AlignStream(16);
                        wemStartOffsets.Add((int)writer.BaseStream.Position);
                        writer.Write(wemFile);
                        writer.AlignStream(16);
                    }
                    break;
            }
            
            foreach (int offset in wemStartOffsets)
            {
                int index = wemStartOffsets.IndexOf(offset);
                writer.BaseStream.Position = wemStartOffsetPositions[index];
                writer.Write(offset);
                if (version==Version.TPP)
                {
                    writer.BaseStream.Position = ls2StartOffsetPositions[index];
                    writer.Write(ls2StartOffsets[index]);
                }
            }
        }
    }
}
