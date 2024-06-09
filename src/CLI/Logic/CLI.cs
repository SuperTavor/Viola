using System.Diagnostics.CodeAnalysis;
using Viola.Utils;
namespace Viola.CLINS;

class CLI
{
    private readonly string[] _args;
    public CLI(string[] args)
    {
        _args = args;
    }

    public ParsedArguments ParseArguments()
    {
        ArgsReader reader = new(_args);
        ParsedArguments parsedArguments = new ParsedArguments();
        var mode = reader.ReadOption("m|mode");
        if (mode == null) ShowUsageAndExit();
        parsedArguments.Mode = ValidiateMode(mode!);
        if(parsedArguments.Mode != eMode.Merge)
        {
            var inputPath = reader.ReadOption("i|input");
            if (inputPath == null) ShowUsageAndExit();
            parsedArguments.InputPath = inputPath!;
        }
        var outputPath = reader.ReadOption("o|output");
        if (outputPath == null) ShowUsageAndExit();
        parsedArguments.OutputPath = outputPath!;
        parsedArguments.StuffToMerge = reader.ReadArguments().ToList();
        try
        {
            reader.VerifyComplete();
        } catch(ArgsReaderException ex)
        {
            Console.WriteLine("Args reader: " + ex.Message);
            Environment.Exit(1);
        }
        return parsedArguments;
    }
    public static eMode ValidiateMode(string mode)
    {
        switch (mode.Trim().ToLower())
        {
            case "pack":
                return eMode.Pack;
            case "dump":
                return eMode.Dump;
            case "merge":
                return eMode.Merge;
            case "cache":
                return eMode.Cache;
            default:
                ShowUsageAndExit();
                //to satisfy compiler
                return new eMode();
        }
    }

    public static void ShowUsageAndExit()
    {
        Console.WriteLine($"Viola version {GeneralUtils.APP_VERSION}\n\nAvailable options/flags:\n-m/--mode = Accepts a 'mode' argument. Available modes:\n\t* pack - packs a dumped mod folder.\n\t* merge - Merges multiple dumped mod folders.\n\t* dump - Dumps a specified RomFS into a Cpk-less directory structure.\n\t* cache - creates a HashCache from a dumped directory structure.\n\n-i/--input - Specify an input file.\n\n-o/--output - Specify an output file.\n\nExamples:" +
            $"\n\tto pack any cpk-less directory: viola -m pack -i <FolderToPack> -o <OutputFolder>\n\tto dump a romfs with CPKs into a clean structure: viola -m dump -i <FolderToDump> -o <OutputFolder>\n\tto merge any amount of packed mods: viola -m merge <myMostImportantMod> <lessImportantMod> <leastImportantMod> -o <OutputFolder>\n\tto create a HashCache from any cpk-less directory: viola -m cache -i <FolderToCacheFrom> -o <ResultHashCachePath>\n\nPlease report any bugs on GitHub (https://github.com/SuperTavor/Viola/)!");
        Environment.Exit(0);
    }
}