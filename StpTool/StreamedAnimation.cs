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
        public List<StreamedAnimationData> Files = new List<StreamedAnimationData>();

        //attempt to read ls and st as one entry, lsst:
        /*public List<byte[]> LsStFiles = new List<byte[]>();*/

        //attempt to read ls and st separately as subentries:
        public void ReadPackage(BinaryReader reader, Version version, Dictionary<ulong, string> dictionary)
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
            int pairsCount = reader.ReadInt32();
            Console.WriteLine($"Count: {pairsCount}");
            List<int> pairOffsets = new List<int>();
            List<int> entrySizes = new List<int>();
            for (int i = 0; i < pairsCount; i++)
            {
                Files.Add(new StreamedAnimationData() { FileName = reader.ReadUInt64() });
                pairOffsets.Add(reader.ReadInt32());
                reader.ReadZeroes(4);
                //Console.WriteLine($"Pair #{i}: {FileNames[i]} is at {pairOffsets[i]}");
            }
            for (int i = 0; i < pairsCount; i++)
            {
                if (i < pairsCount - 1)
                    entrySizes.Add(pairOffsets[i + 1] - pairOffsets[i]);
                else
                    entrySizes.Add((int)(reader.BaseStream.Length - pairOffsets[i]));
                //Console.WriteLine($"Pair #{i} is size of {entrySizes[i]}");
            }
            for (int i = 0; i < pairsCount; i++)
            {
                reader.BaseStream.Position = pairOffsets[i];

                //attempt to read ls and st as one entry, lsst:

                /*int lsstFileSize = 0;
                if (i < FileNames.Count - 1)
                    lsstFileSize = pairOffsets[i + 1] - pairOffsets[i];
                else
                    lsstFileSize = (int)(reader.BaseStream.Length - pairOffsets[i]);
                LsStFiles.Add(reader.ReadBytes(lsstFileSize));
                Console.WriteLine($"LsSt File {i} is size of {lsstFileSize}");*/

                //attempt to read ls and st separately as subentries:

                Console.WriteLine($"Pair #{i}: {Files[i].FileName} is at {pairOffsets[i]}");
                int subEntryCount = reader.ReadInt32();
                Console.WriteLine($"Pair #{i}: {Files[i].FileName} has {subEntryCount} subentries");
                uint offsetToReturnTo = (uint)reader.BaseStream.Position;
                for (int j = 0; j < subEntryCount; j++)
                {
                    reader.BaseStream.Position = offsetToReturnTo;
                    string subentryExtension = reader.ReadCString();
                    reader.AlignStream(4);
                    int subEntryOffset = reader.ReadInt32();
                    offsetToReturnTo = (uint)reader.BaseStream.Position;
                    reader.BaseStream.Position = pairOffsets[i] + subEntryOffset;
                    Console.WriteLine($"Pair #{i}: {Files[i].FileName} data is at {subEntryOffset}");
                    switch (subentryExtension)
                    {
                        case "ls":
                            LsTrack lsSab = new LsTrack();
                            lsSab.ReadBinary(reader, version, true, dictionary);
                            Files[i].Ls=lsSab;
                            Console.WriteLine($"Pair #{i}: {Files[i].FileName} ls has {lsSab.keys.Count} keys");
                            break;
                        case "st":
                            reader.BaseStream.Position += 0x12;
                            string subtitleId = reader.ReadCString();
                            Files[i].St=subtitleId;
                            Console.WriteLine($"Pair #{i}: {Files[i].FileName} st is {subtitleId}");
                            break;
                        default:
                            Console.WriteLine($"Extension {subentryExtension} unsupported!!");
                            return;
                    }
                    //inverted endianness in gz
                    ulong fileNameHashFooter = reader.ReadUInt64();
                }
            }
        }
        public void ExportFiles(string outputPath, Dictionary<ulong, string> dictionary, Version version)
        {
            foreach (StreamedAnimationData file in Files)
            {
                string strFileName = file.FileName.ToString();

                if (dictionary.ContainsKey(file.FileName))
                    dictionary.TryGetValue(file.FileName, out strFileName);

                //attempt to read ls and st as one entry, lsst:
                /*if (LsStFiles.Count > 0)
                    if (LsStFiles[index].Length > 0)
                        File.WriteAllBytes(outputPath + "\\" + strFileName + ".lsst", LsStFiles[index]);*/

                //attempt to read ls and st separately as subentries:
                if (file.Ls != null)
                    using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath + "\\" + strFileName + ".ls", FileMode.Create)))
                    {
                        file.Ls.WriteBinary(writer, version, strFileName, false);
                    };

                if (file.St != null)
                    using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath + "\\" + strFileName + ".st", FileMode.Create)))
                    {
                        WriteSt(writer, file.St);
                    };
            }
        }
        public void ImportFiles(string[] files, Version version)
        {
            for (int i = 0; i < files.Length; i++)
            {
                //attempt to read ls and st as one entry, lsst:
                /*if (Path.GetExtension(files[i]) == ".lsst")
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    ulong fileNameHash;

                    if (ulong.TryParse(fileName, out fileNameHash))
                        fileNameHash = Convert.ToUInt64(fileName);
                    else
                        fileNameHash = Extensions.StrCode64(fileName);

                    FileNames.Add(fileNameHash);
                    if (Path.GetExtension(files[i]) == ".lsst")
                        LsStFiles.Add(File.ReadAllBytes(files[i]));
                }*/

                //attempt to read ls and st separately as subentries:
                if (Path.GetExtension(files[i]) == ".ls" || Path.GetExtension(files[i]) == ".st")
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);

                    if (ulong.TryParse(fileName, out ulong fileNameHash))
                        fileNameHash = Convert.ToUInt64(fileName);
                    else
                        fileNameHash = Extensions.StrCode64(fileName);

                    if (Files.Find(j => j.FileName == fileNameHash)==null)
                        Files.Add(new StreamedAnimationData() { FileName=fileNameHash });

                    using (BinaryReader reader = new BinaryReader(new FileStream(files[i], FileMode.Open)))
                    {
                        switch (Path.GetExtension(files[i]))
                        {
                            case ".ls":
                                LsTrack ls = new LsTrack();
                                ls.ReadBinary(reader, version, false);
                                Files.Find(j => j.FileName == fileNameHash).Ls = ls;
                                break;
                            case ".st":
                                reader.BaseStream.Position += 0x12;
                                string st = reader.ReadCString();
                                Files.Find(j => j.FileName == fileNameHash).St = st;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        public void WritePackage(BinaryWriter writer, Version version)
        {
            writer.Write((uint)SabEndiannessSignature.Little);
            writer.Write((uint)Files.Count);

            List<int> paramsStartPositionOffsets = new List<int>();

            foreach (StreamedAnimationData file in Files)
            {
                writer.Write(file.FileName);
                paramsStartPositionOffsets.Add((int)writer.BaseStream.Position);
                writer.WriteZeroes(4);//offset
                writer.WriteZeroes(4);//padding
            }

            //attempt to read ls and st as one entry, lsst:
            /*foreach (byte[] lsstFile in LsStFiles)
            {
                int index = LsStFiles.IndexOf(lsstFile);
                int lsstLength = lsstFile.Length;
                Console.WriteLine($"Lsst File {index} is size of {lsstLength}");
                paramsStartPositions.Add((int)writer.BaseStream.Position);
                writer.Write(lsstFile);
                writer.AlignStream(16); //there's a potential in corruption here! size differences from padding!
            }*/

            foreach (StreamedAnimationData file in Files)
            {
                uint startOfEntry = (uint)writer.BaseStream.Position;
                int index = Files.IndexOf(file);
                writer.BaseStream.Position = paramsStartPositionOffsets[index];
                writer.Write(startOfEntry);
                writer.BaseStream.Position = startOfEntry;

                int fileCount = 0;
                if (file.Ls != null)
                    fileCount += 1;
                if (file.St != null)
                    fileCount += 1;

                writer.Write(fileCount);

                uint offsetToLs = 0;
                uint offsetToSt = 0;
                uint offsetToLsOffset = 0;
                uint offsetToStOffset = 0;

                if (file.Ls != null)
                {
                    writer.WriteCString("ls");
                    writer.AlignStream(4);
                    offsetToLsOffset = (uint)writer.BaseStream.Position;
                    writer.WriteZeroes(4);
                }
                if (file.St != null)
                {
                    writer.WriteCString("st");
                    writer.AlignStream(4);
                    offsetToStOffset = (uint)writer.BaseStream.Position;
                    writer.WriteZeroes(4);
                }

                if (file.Ls != null)
                {
                    Console.WriteLine($"Ls File {index} is {file.FileName}");
                    if (offsetToLsOffset>0)
                        offsetToLs = (uint)(writer.BaseStream.Position - startOfEntry);
                    file.Ls.WriteBinary(writer, version, file.FileName.ToString(), false);
                    if (version == Version.TPP)
                    {
                        if (writer.BaseStream.Position % 8 != 0)
                            writer.WriteZeroes(4);
                    }
                    writer.Write(file.FileName);
                    writer.AlignStream(16);
                }
                if (file.St != null)
                {
                    Console.WriteLine($"St File {index} is {file.FileName}");
                    if (offsetToStOffset > 0)
                        offsetToSt = (uint)(writer.BaseStream.Position - startOfEntry);
                    WriteSt(writer, file.St);
                    if (version == Version.TPP)
                    {
                        if (writer.BaseStream.Position % 8 != 0)
                            writer.WriteZeroes(4);
                    }
                    writer.Write(file.FileName);
                    writer.AlignStream(16);
                }
                if (offsetToLs > 0)
                {
                    writer.BaseStream.Position = offsetToLsOffset;
                    writer.Write(offsetToLs);
                }
                if (offsetToSt > 0)
                {
                    writer.BaseStream.Position = offsetToStOffset;
                    writer.Write(offsetToSt);
                }
            }
        }
        public void WriteSt(BinaryWriter writer, string st)
        {
            writer.Write(1);
            writer.Write((short)6);
            writer.Write(100);
            writer.WriteZeroes(2);
            writer.Write((short)8);
            writer.Write(1);
            writer.WriteCString(st);
        }
    }
}
