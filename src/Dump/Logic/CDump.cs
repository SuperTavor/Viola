using Viola.CLI;
using Viola.Utils;
using CriFsV2Lib;
using Viola.HashCache;
using CriFsV2Lib.Definitions.Structs;
namespace Viola.Dump;
class CDump
{
    private string _folderPath { get; set; }
    private bool _cache { get; set; }

    public static string DumpFolder = "dumped";

    private CParsedArguments _options;

    public CDump(CParsedArguments options)
    {
        _options = options;
    }
    public void Dump()
    {

        int specBase = 0;
        if (_options.AdditionalArgs[0] == "--cache")
        {
            _cache = true;
            specBase++;
            Console.WriteLine("CACHING ENABLED!!! Big slowdowns ahead\n\n\n");
        }

        _folderPath = _options.AdditionalArgs[specBase];
        DumpFolder = _options.AdditionalArgs[specBase + 1];
        var folderFiles = GeneralUtils.GetAllFiles(_folderPath);
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
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, extractedFile.Span.ToArray());
            }
        }
        foreach (var file in filesToCopy)
        {
            var destFile = $"{DumpFolder}/{file.Substring(_folderPath.Length + 1)}";
            Console.WriteLine($"Copying already loose file: {Path.GetFileName(file)}");
            var destDir = Path.GetDirectoryName(destFile);
            Directory.CreateDirectory(destDir);
            File.Copy(file, destFile, true);
        }
        Console.WriteLine($"Done cleaning. You can find your dumped RomFS in `{DumpFolder}/'");
        if (_cache)
        {
            CHashCache cache = new CHashCache();
            cache.CreateJson();
            File.WriteAllText(CHashCache.HASHCACHE_FILE, cache.HashCacheJson);
            Console.WriteLine("Finished creating cache.");
        }

    }



}