using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.Pack.DataClasses;
namespace Viola.CLI.CLI.Logic;

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
        //Choosing an external cpklist is optional for packing but required for merging (so it's consistent)
        if(options.Mode == Mode.Pack || options.Mode == Mode.Merge)
        {
            var cpklist = reader.ReadOption("cl|cpklist");
            if (cpklist == null)
            {
                //for merging it's required so we show usage but packing we let it slide
                if (options.Mode == Mode.Merge) ShowUsageAndExit();
                //and for packing..
                options.CpkListPath = "";
                CLogger.LogInfo("<Warning> No cpk_list provided, trying to use default location (data/cpk_list.cfg.bin)");
            }
            else options.CpkListPath = cpklist;
            //Get compilation target
            var platform = reader.ReadOption("p|platform");
            if (platform == null) ShowUsageAndExit();
            if (Enum.TryParse(platform.Trim().ToUpper(), out Platform plat))
            {
                options.PackPlatform = plat;
            }
            else ShowUsageAndExit();
        }
        //Get encryption key
        if (options.Mode == Mode.Encrypt)
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
        Console.WriteLine($@"
Viola version {CGeneralUtils.APP_VERSION} (CLI)

IF YOU ARE STRUGGLING, PLEASE VISIT THE QUICKSTART GUIDE! (https://github.com/SuperTavor/Viola?tab=readme-ov-file#quickstart)

Available options/flags:
-m / --mode: Specify the mode of operation. Available modes:
    * pack   - Pack a dumped mod folder.
    * merge  - Merge multiple dumped mod folders.
    * dump   - Dump a specified RomFS into a Cpk-less directory structure.
    * encrypt - Encrypt any previously decrypted file using LEVEL5's XOR encryption scheme.
    * decrypt - Decrypt any file using LEVEL5's XOR encryption scheme.

-i / --input: Specify an input file.
-o / --output: Specify an output file.

Examples:
    To pack any CPK-less directory with only your modded files:
        viola -m pack -p <Target platform (Switch/PC)> --cl <Path to your cpk_list (optional but recommended, if not specified cpk_list from the romfs will be used) -i <FolderToPack> -o <OutputFolder>
    
    To dump a RomFS with CPKs into a clean structure:
        viola -m dump -i <FolderToDump> -o <OutputFolder>

    To merge any number of packed mods:
        viola -m merge -p <Target platform (Switch/PC)> --cl <Path to your vanilla cpk_list (required)> <myMostImportantMod> <lessImportantMod> <leastImportantMod> -o <OutputFolder>

    To encrypt any file using LEVEL5's encryption scheme:
        viola -m encrypt -k <Key> -i <FileToEncrypt> -o <OutputPath>

    To decrypt any file using LEVEL5's encryption scheme:
        viola -m decrypt -i <PathToDecrypt> -o <OutputPath>

Please report any bugs on GitHub: https://github.com/SuperTavor/Viola/
");

        Environment.Exit(0);
    }
}