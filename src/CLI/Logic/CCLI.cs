using System.Diagnostics.CodeAnalysis;

namespace Viola.CLI;


class CCLI
{
    private string[] _args;
    public CCLI(string[] args)
    {
        _args = args;
    }

    public CParsedArguments ParseArguments()
    {
        if (_args.Length >= 2)
        {
            return new CParsedArguments(_args[0], _args.Skip(1).ToArray());
        }
        else ShowUsageAndExit();
        //show usage and exit does not return, but i need to end the function to satisfy the compiler.
        throw new Exception("if this is called something is seriously wrong dude, what the fuck have you done");
    }
    public static eModes ValidiateMode(string mode)
    {
        switch (mode.Trim().ToLower())
        {
            case "pack":
                return eModes.Pack;
            case "dump":
                return eModes.Dump;
            case "merge":
                return eModes.Merge;
            default:
                throw new ArgumentException("Invalid mode!");
        }
    }

    [DoesNotReturn]
    public static void ShowUsageAndExit()
    {
        Console.WriteLine("Viola version 1.0.0\n\nUsage:\n\nViola.exe <mode> <additional_arguments>\n\nModes:\n* pack - packs a dumped mod folder. Example: `Viola.exe pack MyModFolder'\n* merge - Merges multiple dumped mod folders. Example: `Viola.exe merge MyFirstMod MySecondMod`\n* dump - Dumps a specified RomFS into a Cpk-less directory structure. You can specify `--cache` before your folder name to create a HashCache. Example without creating hashcache: 'Viola.exe dump MyRomfs MyDumpedRomFs'. Example with creating a hashcache: `Viola.exe dump --cache MyRomfs MyDumpedRomfs'\n\n");
        Environment.Exit(0);
    }
}