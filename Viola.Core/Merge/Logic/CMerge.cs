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
        CLogger.LogInfo("Preparing pack");
        var packOptions = new CLaunchOptions
        {
            Mode = Mode.Pack,
            InputPath = tempMergePool,
            OutputPath = _mergePackDestiniation,
            PackPlatform = _plat,
            CpkListPath = _clPath
        };
        return packOptions;
    }
    public void MergeMods()
    {
        string tempFolderName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(tempFolderName);

        var foldersToProcess = _folders.Reverse<string>().ToList();
        var folderFilesMap = new Dictionary<string, List<string>>();
        long totalFiles = 0;

        foreach (var folder in foldersToProcess)
        {
            var files = CGeneralUtils.GetAllFilesWithNormalSlash(folder);
            folderFilesMap[folder] = files;
            totalFiles += files.Count;
        }

        long processedFiles = 0;
        long lastUpdateTick = 0;

        if (totalFiles > 0) CGeneralUtils.ReportProgress(0, totalFiles, "Merging Files");

        foreach (var folder in foldersToProcess)
        {
            var files = folderFilesMap[folder];
            foreach (var file in files)
            {
                var destFile = string.Concat($"{tempFolderName}/", file.AsSpan(folder.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);

                processedFiles++;
                long now = DateTime.Now.Ticks;
                if (now - lastUpdateTick > 500000)
                {
                    CGeneralUtils.ReportProgress(processedFiles, totalFiles, "Merging Files");
                    lastUpdateTick = now;
                }
            }
            CLogger.LogInfo($"Added {folder.Replace("\\","/")} to temporary merge pool.\n");
        }

        if (totalFiles > 0) CGeneralUtils.ReportProgress(totalFiles, totalFiles, "Merging Files");

        var packOptions = ConstructMergePacker(tempFolderName);
        var launcher = new CLauncher(packOptions);

        launcher.Launch();

        CGeneralUtils.ReportProgress(0, 0, "");
        CLogger.AddImportantInfo("Finished merging into your output folder");
    }
}