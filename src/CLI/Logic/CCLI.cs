using Viola.src.Launcher.DataClasses;
using Viola.src.Utils.General.Logic;

namespace Viola.src.CLI.Logic;

class CCLI
{
    private readonly string[] _args;
    public CCLI(string[] args)
    {
        _args = args;
    }

    public CLaunchOptions ParseArguments()
    {
        ArgsReader reader = new(_args);
        CLaunchOptions options = new CLaunchOptions();
        var mode = reader.ReadOption("m|mode");
        if (mode == null) ShowUsageAndExit();
        options.Mode = ValidiateMode(mode!);
        if (options.Mode != Mode.Merge)
        {
            var inputPath = reader.ReadOption("i|input");
            if (inputPath == null) ShowUsageAndExit();
            options.InputPath = inputPath!;
        }
        var outputPath = reader.ReadOption("o|output");
        if (outputPath == null) ShowUsageAndExit();
        options.OutputPath = outputPath!;
        if(options.Mode == Mode.Encrypt)
        {
            var key = reader.ReadOption("k|key");
            if (key == null) ShowUsageAndExit();
            else
            {
                try
                {
                    options.Key = uint.Parse(key);
                }
                catch
                {
                    Console.WriteLine("Please enter a valid uint as the key.");
                    Environment.Exit(1);
                }
            }
        }
        options.StuffToMerge = reader.ReadArguments().ToList();
        try
        {
            reader.VerifyComplete();
        }
        catch (ArgsReaderException ex)
        {
            Console.WriteLine("Args reader: " + ex.Message);
            Environment.Exit(1);
        }
        return options;
    }
    public static Mode ValidiateMode(string mode)
    {
        switch (mode.Trim().ToLower())
        {
            case "pack":
                return Mode.Pack;
            case "dump":
                return Mode.Dump;
            case "merge":
                return Mode.Merge;
            case "cache":
                return Mode.Cache;
            case "decrypt":
                return Mode.Decrypt;
            case "encrypt":
                return Mode.Encrypt;
            default:
                ShowUsageAndExit();
                //to satisfy compiler
                return new Mode();
        }
    }

    public static void ShowUsageAndExit()
    {
        Console.WriteLine($"Viola version {CGeneralUtils.APP_VERSION}\n\nAvailable options/flags:\n-m/--mode = Accepts a 'mode' argument. Available modes:\n\t* pack - packs a dumped mod folder.\n\t* merge - Merges multiple dumped mod folders.\n\t* dump - Dumps a specified RomFS into a Cpk-less directory structure.\n\t* cache - creates a HashCache from a dumped directory structure.\n\n-i/--input - Specify an input file.\n\n-o/--output - Specify an output file.\n\nExamples:" +
            $"\n\tto pack any cpk-less directory: viola -m pack -i <FolderToPack> -o <OutputFolder>\n\tto dump a romfs with CPKs into a clean structure: viola -m dump -i <FolderToDump> -o <OutputFolder>\n\tto merge any amount of packed mods: viola -m merge <myMostImportantMod> <lessImportantMod> <leastImportantMod> -o <OutputFolder>\n\tto create a HashCache from any cpk-less directory: viola -m cache -i <FolderToCacheFrom> -o <ResultHashCachePath>\n\tto encrypt any file using LEVEL5's new encryption scheme: viola -m encrypt -i <FileToEncrypt> -o <OutputPath> -key <Key>\n\tto decrypt any file using LEVEL5's new encryption scheme: viola -m decrypt -i <PathToDecrypt> -i <OutputPath>\n\nPlease report any bugs on GitHub (https://github.com/SuperTavor/Viola/)!");
        Environment.Exit(0);
    }
}