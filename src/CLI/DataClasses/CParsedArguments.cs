namespace Viola.CLI;
class CParsedArguments
{
    public eModes Mode { get; set; }
    public string[] AdditionalArgs { get; set; }

    public CParsedArguments(string mode, string[] paths)
    {
        AdditionalArgs = paths;
        Mode = CCLI.ValidiateMode(mode);
    }

}