using Viola.CLI;
using CfgBinEditor.Level5.Binary;
using CfgBinEditor.Level5.Binary.Logic;
using Viola.Utils;
using Viola.HashCache;
using Newtonsoft.Json;
namespace Viola.Pack;
class CPack
{
    private CParsedArguments _options { get; set; }
    private List<string> _modFiles = [];
    private string _root = string.Empty;
    public CPack(CParsedArguments options)
    {
        _options = options;
        _root = _options.AdditionalArgs[0].Replace("\\", "/");
    }

    bool CheckForCpkList()
    {
        return _modFiles.Contains($"{_root}/data/cpk_list.cfg.bin");
    }

    void CollectModPaths()
    {
        _modFiles = GeneralUtils.GetAllFiles(_root);
    }

    public void Pack()
    {
        CollectModPaths();
        if (!CheckForCpkList())
        {
            throw new FileNotFoundException("Cannot find Cpk_List!");
        }
        var cpkListPath = _modFiles[_modFiles.IndexOf($"{_root}/data/cpk_list.cfg.bin")];
        byte[] cpklistBytes = File.ReadAllBytes(cpkListPath);
        CfgBin cpkList = new CfgBin();
        cpkList.Open(cpklistBytes);
        //get all CPK_ITEM entries
        Entry[] cpkItems = cpkList.Entries[0].Children.ToArray();
        //store all the filenames of changed files so we can copy later
        List<string> modifiedFiles = [];
        CHashCache hashCache = new CHashCache();
        //Load hashCache
        hashCache.Load();
        //iterate 
        foreach (var cpkItem in cpkItems)
        {
            var fileNameInCfgBin = (string)cpkItem.Variables[0].Value;
            foreach (var modfile in _modFiles)
            {
                //+1 to account for the /
                var modFileWithoutRoot = modfile.Substring(_root.Length + 1);
                if (fileNameInCfgBin == modFileWithoutRoot)
                {
                    var fnameIndex = hashCache.FindEntryIndex(fileNameInCfgBin);
                    if (fnameIndex == -1)
                    {
                        Console.WriteLine($"{fileNameInCfgBin} missing from HashCache.");
                        Environment.Exit(1);
                    }
                    if (hashCache.HashCacheFile.Entries[fnameIndex].Hash != GeneralUtils.ComputeCRC32(File.ReadAllBytes(modfile)))
                    {
                        Console.WriteLine($"[modified file] {modFileWithoutRoot}");
                        //make the file loose
                        cpkItem.Variables[1].Value = string.Empty;
                        //adjust the size to the modded file's size
                        cpkItem.Variables[2].Value = (int)new FileInfo(modfile).Length;
                        modifiedFiles.Add(modfile);
                        break;
                    }

                }
            }

        }
        var outputModFolder = _root + "_output_mod";
        //create path to the mod output folder, unless it already exists.
        Directory.CreateDirectory(outputModFolder);
        cpkList.Entries[0].Children = cpkItems.ToList();
        var outputCpkListPath = $"{outputModFolder}/data/cpk_list.cfg.bin";
        Directory.CreateDirectory(Path.GetDirectoryName(outputCpkListPath));
        File.WriteAllBytes(outputCpkListPath, cpkList.Save());

        foreach (var file in modifiedFiles)
        {
            var destFilePath = $"{outputModFolder}/{file.Substring(_root.Length + 1)}";
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(file, destFilePath, true);
        }
    }
}