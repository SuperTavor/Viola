using Viola.src.Launcher.DataClasses;
using Viola.src.Utils.General.Logic;

namespace Viola.src.Merge.Logic;


class CMerge
{
    private List<string> _folders = new();
    private string _mergeDestination = string.Empty;
    public CMerge(CLaunchOptions options)
    {
        _mergeDestination = options.OutputPath;
        _folders = options.StuffToMerge;
    }

    public void MergeMods()
    {
        Directory.CreateDirectory(_mergeDestination);
        //reversed to start from the least 'important' mod
        foreach (var folder in _folders.Reverse<string>())
        {
            var files = CGeneralUtils.GetAllFilesWithNormalSlash(folder);
            foreach (var file in files)
            {
                var destFile = $"{_mergeDestination}/" + file.Substring(folder.Length + 1);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
            }
            Console.WriteLine($"Added {folder} to merge pool.");
        }
        Console.WriteLine($"\nFinished merging all your mods into {_mergeDestination}/.\nTo pack them, simply pack that directory through the GUI or run 'Viola.exe -m pack -i \"{_mergeDestination}\" -o \"{_mergeDestination}_packed\"'");
    }
}