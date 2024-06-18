using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace StpTool
{
    public enum Version
    {
        GZ = 0,
        TPP = 1,
    }
    internal static class Program
    {
        private const string EmbeddedFilenameStringsFileName = "embedded_filename_strings.txt";
        // Based on BobDoleOwndU's AutoPftxsTool
        // https://github.com/BobDoleOwndU/AutoPftxsTool/
        private static void Main(string[] args)
        {
            Version version = Version.TPP;
            Version outversion = Version.TPP;

            foreach (string arg in args)
            {
                if (arg.ToLower() == "-gz")
                {
                    version = Version.GZ;
                    continue;
                }
                if (arg.ToLower() == "-outgz")
                {
                    outversion = Version.GZ;
                    continue;
                }
                if (File.GetAttributes(arg).HasFlag(FileAttributes.Directory))
                {
                    var isStp = true;
                    string[] files = Directory.GetFiles(arg, "*", SearchOption.TopDirectoryOnly);
                    //Write
                    Console.WriteLine($"Write {arg}");

                    string extension = ".stp";
                    if (arg.EndsWith("sab"))
                        isStp = false;
                    else if(arg.EndsWith("stp"))
                        isStp = true;

                    if (!isStp)
                        extension = ".sab";
                    string fileName = arg.Substring(0, arg.Length - extension.Length) + extension;

                    if (!isStp)
                    {
                        StreamedAnimation sab = ImportSabFiles(files, version);
                        WriteSabPackage(sab, fileName, outversion);
                    }
                    else
                    {
                        StreamedPackage stp = ImportStpFiles(files);
                        WriteStpPackage(stp, fileName, outversion);
                    }

                }
                else if (File.Exists(arg))
                {
                    //Read
                    Console.WriteLine($"Read {arg}");
                    string extension = Path.GetExtension(arg).Substring(1);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(arg);
                    string directoryName = Path.GetDirectoryName(arg);
                    if (directoryName == string.Empty)
                        directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string outputDirectory = directoryName + "\\" + fileNameWithoutExtension + "_" + extension;

                    string direct = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string dictDir = direct + "\\" + EmbeddedFilenameStringsFileName;

                    switch (extension)
                    {
                        case "stp":
                        case "sab":
                        case "bnk":
                            Directory.CreateDirectory(outputDirectory);
                            break;
                    };

                    switch (extension)
                    {
                        case "stp":
                            StreamedPackage stp = ReadStpPackage(arg);
                            ExportStpFiles(stp, outputDirectory);
                            break;
                        case "sab":
                            StreamedAnimation sab = ReadSabPackage(arg, version);
                            ExportSabFiles(sab, outputDirectory, CreateDictionary(dictDir), outversion);
                            break;
                        case "bnk":
                            EmbeddedDataIndex bnk = ReadSoundBank(arg);
                            DumpBnk(bnk, outputDirectory);
                            break;
                        case "ls":
                        case "ls2":
                            LsTrack ls = ReadBinary(arg,version);
                            WriteXml(ls, Path.GetFileNameWithoutExtension(arg) + "." + extension + ".xml");
                            break;
                        case "xml":
                            LsTrack xmlLs = ReadXml(arg);
                            WriteLsBinary(xmlLs, directoryName + "\\" + fileNameWithoutExtension, outversion, fileNameWithoutExtension, false);
                            break;
                    };
                }
            }
        }
        public static void WriteLsBinary(LsTrack xmlLs, string outputPath, Version version, string fileName, bool isSab)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
            {
                xmlLs.WriteBinary(writer, version, fileName, isSab);
            }
        }
        public static LsTrack ReadXml(string path)
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };

            LsTrack ls = new LsTrack();
            using (var reader = XmlReader.Create(path, xmlReaderSettings))
            {
                ls.ReadXml(reader);
            }
            return ls;
        }
        public static void WriteXml(LsTrack ls, string path)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };
            using (var writer = XmlWriter.Create(path, xmlWriterSettings))
            {
                ls.WriteXml(writer);
            }
        }
        public static LsTrack ReadBinary(string path, Version version)
        {
            LsTrack ls = new LsTrack();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                ls.ReadBinary(reader, version, false);
            }
            return ls;
        }
        public static StreamedAnimation ReadSabPackage(string path, Version version)
        {
            StreamedAnimation sab = new StreamedAnimation();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                sab.ReadPackage(reader, version);
            }
            return sab;
        }
        public static void ExportSabFiles(StreamedAnimation sab, string outputPath, Dictionary<ulong, string> dictionary, Version version)
        {
            sab.ExportFiles(outputPath, dictionary, version);
        }
        public static StreamedPackage ReadStpPackage(string path)
        {
            StreamedPackage stp = new StreamedPackage();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                stp.ReadPackage(reader);
            }
            return stp;
        }
        public static void ExportStpFiles(StreamedPackage stp, string outputPath)
        {
            stp.ExportFiles(outputPath);
        }
        public static StreamedPackage ImportStpFiles(string[] files)
        {
            StreamedPackage stp = new StreamedPackage();

            stp.ImportFiles(files);

            return stp;
        }
        public static void WriteStpPackage(StreamedPackage stp, string outputPath, Version version)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
            {
                stp.WritePackage(writer, version);
            }
        }
        public static StreamedAnimation ImportSabFiles(string[] files, Version version)
        {
            StreamedAnimation sab = new StreamedAnimation();

            sab.ImportFiles(files, version);

            return sab;
        }
        public static void WriteSabPackage(StreamedAnimation sab, string outputPath, Version version)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
            {
                sab.WritePackage(writer, version);
            }
        }
        public static EmbeddedDataIndex ReadSoundBank(string path)
        {
            EmbeddedDataIndex bnk = new EmbeddedDataIndex();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                bnk.ReadSoundBank(reader);
            }
            return bnk;
        }
        public static void DumpBnk(EmbeddedDataIndex bnk, string outputPath)
        {
            bnk.DumpFiles(outputPath);
        }

        public static Dictionary<ulong, string> CreateDictionary(string dictDir)
        {
            Dictionary<ulong, string> embeddedFilenameMarkerDictionary = new Dictionary<ulong, string>();

            if (File.Exists(dictDir))
            {
                string[] embeddedFilenameMarkers = File.ReadAllLines(dictDir).Distinct().ToArray();
                foreach (string marker in embeddedFilenameMarkers)
                {
                    embeddedFilenameMarkerDictionary.Add(Extensions.StrCode64(marker), marker);
                }
            };

            return embeddedFilenameMarkerDictionary;
        }
    }
}
