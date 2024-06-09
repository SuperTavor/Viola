using Viola.CLINS;
using Viola.Utils;
using CriFsV2Lib;
using Viola.HashCacheNS;
using CriFsV2Lib.Definitions.Structs;
namespace Viola.DumpNS;
class Dump
{
    private string _dirToDump = string.Empty;

    public static string DumpFolder = "dumped";

    private ParsedArguments _options;

    public Dump(ParsedArguments options)
    {
        _options = options;
    }
    public void DumpRomfs()
    {
        _dirToDump = _options.InputPath;
        DumpFolder = _options.OutputPath;
        var folderFiles = GeneralUtils.GetAllFilesWithNormalSlash(_dirToDump);
        var cpkPaths = new List<string>();
        var filesToCopy = new List<string>();
        foreach (var file in folderFiles)
        {
            if (file.Trim().EndsWith(".cpk"))
            {
                cpkPaths.Add(file);
            }
            else filesToCopy.Add(file);
        }
        foreach (var cpk in cpkPaths)
        {
            Console.WriteLine($"Extracting {cpk}");
            using var filestream = new FileStream(cpk, FileMode.Open);
            using var reader = new CriFsLib().CreateCpkReader(filestream, true);
            var files = reader.GetFiles();
            foreach (CpkFile file in files)
            {
                using var extractedFile = reader.ExtractFile(file);
                var filePath = $"{DumpFolder}/{file.Directory}/{file.FileName}";
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllBytes(filePath, extractedFile.Span.ToArray());
            }
        }
        foreach (var file in filesToCopy)
        {
            var destFile = $"{DumpFolder}/{file.Substring(_dirToDump.Length + 1)}";
            Console.WriteLine($"Copying already loose file: {Path.GetFileName(file)}");
            var destDir = Path.GetDirectoryName(destFile);
            Directory.CreateDirectory(destDir!);
            File.Copy(file, destFile, true);
        }
        Console.WriteLine($"Done cleaning. You can find your dumped RomFS in `{DumpFolder}/'");
    }



}