using Viola.Core.Launcher.DataClasses;
using Viola.Core.Launcher.Logic;
using Viola.Core.Pack.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Core.Merge.Logic;


class CMerge
{
    private List<string> _folders = new();
    private string _mergePackDestiniation = string.Empty;
    private Platform _plat;
    private string _clPath;
    public CMerge(CLaunchOptions options)
    {
        _mergePackDestiniation = options.OutputPath;
        _folders = options.StuffToMerge;
        _plat = options.PackPlatform;
        _clPath = options.CpkListPath;
    }

    private CLaunchOptions ConstructMergePacker(string tempMergePool)
    {
        CLogger.LogInfo("Preparing pack\n");
        var packOptions = new CLaunchOptions();
        packOptions.Mode = Mode.Pack;
        packOptions.InputPath = tempMergePool;
        packOptions.OutputPath = _mergePackDestiniation;
        packOptions.PackPlatform = _plat;
        packOptions.CpkListPath = _clPath;
        return packOptions;
    }
    public void MergeMods()
    {
        string tempFolderName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        //Create temp folder
        Directory.CreateDirectory(tempFolderName);
        //reversed to start from the least 'important' mod
        foreach (var folder in _folders.Reverse<string>())
        {
            var files = CGeneralUtils.GetAllFilesWithNormalSlash(folder);
            foreach (var file in files)
            {
                var destFile = $"{tempFolderName}/" + file.Substring(folder.Length + 1);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
            }
            CLogger.LogInfo($"Added {folder.Replace("\\","/")} to temporary merge pool.\n");
        }
        var packOptions = ConstructMergePacker(tempFolderName);
        var launcher = new CLauncher(packOptions);

        launcher.Launch();

        CLogger.AddImportantInfo("Finished merging into your output folder");
    }
}