using System;
using System.Collections.Generic;
using System.IO;

namespace StpTool
{
    public class StreamedAnimation
    {
        public enum SabEndiannessSignature
        {
            Little = 0x334C4153,
            Big = 0x33424153,
        }
        public List<ulong> FileNames = new List<ulong>();

        //attempt to read ls and st as one entry, lsst:
        public List<byte[]> LsStFiles = new List<byte[]>();

        //attempt to read ls and st separately as subentries:
        /*public List<byte[]> LsFiles = new List<byte[]>();
        public List<byte[]> StFiles = new List<byte[]>();*/
        public void ReadPackage(BinaryReader reader)
        {
            uint signature = reader.ReadUInt32();
            Console.WriteLine($"signature: {signature}");
            switch (signature)
            {
                case (uint)SabEndiannessSignature.Little:
                    Console.WriteLine("Little Endian");
                    break;
                case (uint)SabEndiannessSignature.Big:
                    throw new NotImplementedException();
            }
            uint pairsCount = reader.ReadUInt32();
            Console.WriteLine($"Count: {pairsCount}");
            List<int> pairOffsets = new List<int>();
            List<int> entrySizes = new List<int>();
            for (int i = 0; i < pairsCount; i++)
            {
                FileNames.Add(reader.ReadUInt64());
                pairOffsets.Add(reader.ReadInt32());
                reader.ReadZeroes(4);
                Console.WriteLine($"Pair #{i}: {FileNames[i]} is at {pairOffsets[i]}");
            }
            for (int i = 0; i < pairsCount; i++)
            {
                if (i < pairsCount - 1)
                    entrySizes.Add(pairOffsets[i + 1] - pairOffsets[i]);
                else
                    entrySizes.Add((int)(reader.BaseStream.Length - pairOffsets[i]));
                Console.WriteLine($"Pair #{i} is size of {entrySizes[i]}");
            }
            for (int i = 0; i < pairsCount; i++)
            {
                reader.BaseStream.Position = pairOffsets[i];

                //attempt to read ls and st as one entry, lsst:

                int lsstFileSize = 0;
                if (i < FileNames.Count - 1)
                    lsstFileSize = pairOffsets[i + 1] - pairOffsets[i];
                else
                    lsstFileSize = (int)(reader.BaseStream.Length - pairOffsets[i]);
                LsStFiles.Add(reader.ReadBytes(lsstFileSize));
                Console.WriteLine($"LsSt File {i} is size of {lsstFileSize}");

                //attempt to read ls and st separately as subentries:

                /*long entryStart = reader.BaseStream.Position;
                int subEntryCount = reader.ReadInt32();
                List<string> subentryExtensions = new List<string>();
                List<int> subEntryOffsets = new List<int>();
                for (int j = 0; j < subEntryCount; j++)
                {
                    subentryExtensions.Add(reader.ReadCString());
                    reader.AlignStream(4);
                    subEntryOffsets.Add(reader.ReadInt32());
                }

                long startOfSubEntries = reader.BaseStream.Position;

                if (!subentryExtensions.Contains("ls"))
                    LsFiles.Add(Array.Empty<byte>());
                else if (!subentryExtensions.Contains("st"))
                    StFiles.Add(Array.Empty<byte>());

                for (int j = 0; j < subEntryCount; j++)
                {
                    long pos = entryStart + subEntryOffsets[j];
                    reader.BaseStream.Position = pos;
                    int subEntrySize = 0;
                    if (j < subEntryCount - 1)
                        subEntrySize = (int)((entryStart + subEntryOffsets[j + 1]) - pos);
                    else
                        if (i < pairsCount - 1)
                            subEntrySize = (int)((pairOffsets[i + 1] - pos));
                        else
                            subEntrySize = (int)(reader.BaseStream.Length - (pos));

                    if (subentryExtensions[j] == "ls")
                        LsFiles.Add(reader.ReadBytes(subEntrySize));
                    else if (subentryExtensions[j] == "st")
                        StFiles.Add(reader.ReadBytes(subEntrySize));
                }*/
            }
        }
        public void ExportFiles(string outputPath)
        {
            foreach (ulong fileName in FileNames)
            {
                int index = FileNames.IndexOf(fileName);

                //attempt to read ls and st as one entry, lsst:
                if (LsStFiles.Count > 0)
                    if (LsStFiles[index].Length > 0)
                        File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".lsst", LsStFiles[index]);

                //attempt to read ls and st separately as subentries:
                /* if (LsFiles.Count > 0)
                     if (LsFiles[index].Length > 0)
                         File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".ls", LsFiles[index]);

                 if (StFiles.Count > 0)
                     if (StFiles[index].Length > 0)
                         File.WriteAllBytes(outputPath + "\\" + fileName.ToString() + ".st", StFiles[index]);*/
            }
        }
        public void ImportFiles(string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                //attempt to read ls and st as one entry, lsst:
                if (Path.GetExtension(files[i]) == ".lsst")
                {
                    FileNames.Add(Convert.ToUInt64(Path.GetFileNameWithoutExtension(files[i])));
                    if (Path.GetExtension(files[i]) == ".lsst")
                        LsStFiles.Add(File.ReadAllBytes(files[i]));
                }

                //attempt to read ls and st separately as subentries:
                /*//TODO .st
                if (Path.GetExtension(files[i]) == ".ls")
                {
                    FileNames.Add(Convert.ToUInt64(Path.GetFileNameWithoutExtension(files[i])));
                    if (Path.GetExtension(files[i]) == ".ls")
                        LsFiles.Add(File.ReadAllBytes(files[i]));
                }*/
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

            writer.Write((uint)SabEndiannessSignature.Little);
            writer.Write((uint)FileNames.Count);

            List<int> paramsStartPositionOffsets = new List<int>();
            List<int> paramsStartPositions = new List<int>();

            foreach (ulong fileName in FileNames)
            {
                writer.Write(fileName);
                paramsStartPositionOffsets.Add((int)writer.BaseStream.Position);
                writer.WriteZeroes(4);//offset
                writer.WriteZeroes(4);//padding
            }

            //attempt to read ls and st as one entry, lsst:
            foreach (byte[] lsstFile in LsStFiles)
            {
                int index = LsStFiles.IndexOf(lsstFile);
                int lsstLength = lsstFile.Length;
                Console.WriteLine($"Lsst File {index} is size of {lsstLength}");
                paramsStartPositions.Add((int)writer.BaseStream.Position);
                writer.Write(lsstFile);
                writer.AlignStream(16); //there's a potential in corruption here! size differences from padding!
            }

            foreach (int offset in paramsStartPositions)
            {
                int index = paramsStartPositions.IndexOf(offset);
                writer.BaseStream.Position = paramsStartPositionOffsets[index];
                writer.Write(offset);
            }
        }
    }
}
