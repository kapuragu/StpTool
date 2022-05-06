using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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


            foreach (string arg in args)
            {
                if (arg.ToLower() == "-gz")
                {
                    version = Version.GZ;
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
                        StreamedAnimation sab = ImportSabFiles(files);
                        WriteSabPackage(sab, fileName, version);
                    }
                    else
                    {
                        StreamedPackage stp = ImportStpFiles(files);
                        WriteStpPackage(stp, fileName, version);
                    }

                }
                else if (File.Exists(arg))
                {
                    //Read
                    Console.WriteLine($"Read {arg}");
                    string extension = Path.GetExtension(arg).Substring(1);

                    if (extension == "stp"|| extension == "sab"|| extension == "bnk")
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(arg);
                        string directoryName = Path.GetDirectoryName(arg);
                        string outputDirectory = directoryName + "\\" + fileNameWithoutExtension + "_" + extension;

                        Directory.CreateDirectory(outputDirectory);

                        if (extension == "stp")
                        {
                            StreamedPackage stp = ReadStpPackage(arg);
                            ExportStpFiles(stp, outputDirectory);
                        }
                        else if (extension == "sab")
                        {
                            string direct = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string dictDir = direct + "/" + EmbeddedFilenameStringsFileName;
                            StreamedAnimation sab = ReadSabPackage(arg);
                            ExportSabFiles(sab, outputDirectory, CreateDictionary(dictDir));
                        }
                        else if (extension == "bnk")
                        {
                            EmbeddedDataIndex bnk = ReadSoundBank(arg);
                            DumpBnk(bnk, outputDirectory);
                        }
                    }
                }
            }
        }
        public static StreamedAnimation ReadSabPackage(string path)
        {
            StreamedAnimation sab = new StreamedAnimation();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                sab.ReadPackage(reader);
            }
            return sab;
        }
        public static void ExportSabFiles(StreamedAnimation sab, string outputPath, Dictionary<ulong, string> dictionary)
        {
            sab.ExportFiles(outputPath, dictionary);
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
        public static StreamedAnimation ImportSabFiles(string[] files)
        {
            StreamedAnimation sab = new StreamedAnimation();

            sab.ImportFiles(files);

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
