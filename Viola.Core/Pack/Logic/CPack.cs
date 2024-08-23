using Tinifan.Level5.Binary;
using Tinifan.Level5.Binary.Logic;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Core.Pack.Logic;
class CPack
{
    private CLaunchOptions _options { get; set; }
    private string _dirToPack = string.Empty;
    public CPack(CLaunchOptions options)
    {
        _options = options;
        _dirToPack = _options.InputPath!.Replace("\\", "/");
    }

    bool CheckForCpkList(List<string> modFiles)
    {
        return modFiles.Contains($"{_dirToPack}/data/cpk_list.cfg.bin");
    }

    List<string> CollectModPaths()
    {
        return CGeneralUtils.GetAllFilesWithNormalSlash(_dirToPack);
    }

    public void PackMod()
    {
        var recFilesInPackDir = CollectModPaths();
        string cpkListPath;
        if (_options.CpkListPath == string.Empty) cpkListPath = $"{_dirToPack}/data/cpk_list.cfg.bin";
        else cpkListPath = _options.CpkListPath;
        if (!File.Exists(cpkListPath))
        {
            CLogger.AddImportantInfo($"Can't find your selected cpk_list file. If you chose to use the default cpk_list file, make sure it's in <your_mod_directory>/data/cpk_list.cfg.bin");
            return;
        }
        byte[] cpklistBytes = File.ReadAllBytes(cpkListPath);
        CfgBin cpkList = new CfgBin();
        cpkList.Open(cpklistBytes);
        //get all CPK_ITEM entries
        List<Entry> cpkItems = cpkList.Entries[0].Children;
        //Get all files already registered in the cpklist.
        HashSet<string> alreadyRegisteredFiles = new HashSet<string>();
        foreach (var cpkItem in cpkItems)
        {
            //Add fileName field from each entry.
            alreadyRegisteredFiles.Add(_dirToPack + "/" + (string)cpkItem.Variables[0].Value);
        }
        //store all the filenames of changed files so we can copy later
        HashSet<string> modifiedOrAddedFiles = [];
        for (int i = 0; i < cpkItems.Count; i++)
        {
            var fileNameInCfgBin = (string)cpkItems[i].Variables[0].Value;
            //This should create a full path of the file specified in the enbtry
            var modfile = $"{_dirToPack}/{fileNameInCfgBin}";

            if (File.Exists(modfile))
            {
                CLogger.LogInfo($"[modified file] {fileNameInCfgBin}\n");
                //make the file loose
                cpkItems[i].Variables[1].Value = string.Empty;
                //adjust the size to the modded file's size
                cpkItems[i].Variables[2].Value = (int)new FileInfo(modfile).Length;
                modifiedOrAddedFiles.Add(modfile);
            }

        }
        foreach (var file in recFilesInPackDir)
        {
            //Get all the new files
            if (!alreadyRegisteredFiles.Contains(file) && file != $"{_dirToPack}/data/cpk_list.cfg.bin" && File.Exists(file))
            {
                var relativePath = Path.GetRelativePath(_dirToPack, file).Replace("\\", "/");
                CLogger.LogInfo($"[new file] {relativePath}\n");
                modifiedOrAddedFiles.Add(file);
                //dupe the last entry and fill in our values
                var newEntry = cpkItems.Last().Clone();
                newEntry.Variables[0].Value = Path.GetRelativePath(_dirToPack, file).Replace("\\", "/");
                newEntry.Variables[1].Value = string.Empty;
                newEntry.Variables[2].Value = (int)new FileInfo(file).Length;
                cpkItems.Add(newEntry);
            }
        }
        var outputModFolder = _options.OutputPath;
        //create path to the mod output folder, unless it already exists.
        Directory.CreateDirectory(outputModFolder);
        //Set updated entry count if files were added
        cpkList.Entries[0].Variables[0].Value = cpkItems.Count;
        //replace the basegame cpklist with the stuff from our cpklist
        cpkList.Entries[0].Children = cpkItems;
        string outputCpkListPath = "";
        if(_options.PackPlatform == DataClasses.Platform.SWITCH)
            outputCpkListPath = $"{outputModFolder}/romfs/data/cpk_list.cfg.bin";
        else outputCpkListPath = $"{outputModFolder}/data/cpk_list.cfg.bin";
        Directory.CreateDirectory(Path.GetDirectoryName(outputCpkListPath)!);
        File.WriteAllBytes(outputCpkListPath, cpkList.Save());

        foreach (var file in modifiedOrAddedFiles)
        {
            string destFilePath = "";
            if(_options.PackPlatform == DataClasses.Platform.PC)
                destFilePath = $"{outputModFolder}/{Path.GetRelativePath(_dirToPack, file)}";
            else destFilePath = $"{outputModFolder}/romfs/{Path.GetRelativePath(_dirToPack, file)}";
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);
            File.Copy(file, destFilePath, true);
        }
    }
}