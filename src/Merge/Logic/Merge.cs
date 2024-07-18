using Viola.CLINS;
using Viola.Utils;

namespace Viola.MergeNS;


class Merge
{
    private List<string> _folders = new();
    private string _mergeDestination = string.Empty;
    public Merge(ParsedArguments options)
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
            var files = GeneralUtils.GetAllFilesWithNormalSlash(folder);
            foreach (var file in files)
            {
                var destFile = $"{_mergeDestination}/" + file.Substring(folder.Length + 1);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
            }
            Console.WriteLine($"Added {folder} to merge pool.");
        }
        Console.WriteLine($"\nFinished merging all your mods into {_mergeDestination}/.\nTo pack them, simply run 'Viola.exe -m pack -i \"{_mergeDestination}\" -o \"{_mergeDestination}_packed\"'");
    }
}