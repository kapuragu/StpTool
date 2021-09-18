using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StpTool
{
    internal static class Program
    {
        // Based on BobDoleOwndU's AutoPftxsTool
        // https://github.com/BobDoleOwndU/AutoPftxsTool/
        private static void Main(string[] args)
        {

            Version version = Version.TPP;


            foreach (string stpPath in args)
            {
                if (stpPath.ToLower() == "-gz")
                {
                    version = Version.GZ;
                    continue;
                }
                if (File.GetAttributes(stpPath).HasFlag(FileAttributes.Directory))
                {
                    //Write
                    Console.WriteLine($"Write {stpPath}");
                    string extension = ".stp";
                    string fileName = stpPath.Substring(0, stpPath.Length - extension.Length) + extension;

                    StreamedPackage stp = ImportFiles(Directory.GetFiles(stpPath, "*", SearchOption.TopDirectoryOnly));
                    WritePackage(stp, fileName, version);
                }
                else if (File.Exists(stpPath))
                {
                    //Read
                    Console.WriteLine($"Read {stpPath}");
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(stpPath);
                    string extension = Path.GetExtension(stpPath).Substring(1);
                    string directoryName = Path.GetDirectoryName(stpPath);
                    string outputDirectory = directoryName + "\\" + fileNameWithoutExtension + "_" + extension;

                    Directory.CreateDirectory(outputDirectory);

                    StreamedPackage stp = ReadPackage(stpPath);
                    ExportFiles(stp, outputDirectory);
                }
            }
        }
        public static StreamedPackage ReadPackage(string path)
        {
            StreamedPackage stp = new StreamedPackage();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                stp.ReadPackage(reader);
            }
            return stp;
        }
        public static void ExportFiles(StreamedPackage stp, string outputPath)
        {
            stp.ExportFiles(outputPath);
        }
        public static StreamedPackage ImportFiles(string[] files)
        {
            StreamedPackage stp = new StreamedPackage();

            stp.ImportFiles(files);

            return stp;
        }
        public static void WritePackage(StreamedPackage stp, string outputPath, Version version)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
            {
                stp.WritePackage(writer, version);
            }
        }
    }
}
