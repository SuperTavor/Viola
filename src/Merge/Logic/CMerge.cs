using Viola.CLI;
using Viola.Utils;

namespace Viola.Merge;


class CMerge
{
    private string[] _folders;
    public CMerge(CParsedArguments options)
    {
        _folders = options.AdditionalArgs;
    }

    public void Merge()
    {
        Console.Write("Folder for the mods to be merged: ");
        var cacheFolder = Console.ReadLine().Trim();
        Directory.CreateDirectory(cacheFolder);
        //reversed to start from the least 'important' mod
        foreach (var folder in _folders.Reverse())
        {
            var files = GeneralUtils.GetAllFiles(folder);
            foreach (var file in files)
            {
                var destFile = $"{cacheFolder}/" + file.Substring(folder.Length + 1);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }
            Console.WriteLine($"Added {folder} to merge pool.");
        }
        Console.WriteLine($"\nFinished merging all your mods into {cacheFolder}/.\nTo pack them, simply run 'Viola.exe pack {cacheFolder}'");
    }
}