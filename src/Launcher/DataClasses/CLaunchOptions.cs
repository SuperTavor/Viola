namespace Viola.src.Launcher.DataClasses;
public class CLaunchOptions
{
    public Mode Mode { get; set; }
    public string InputPath = string.Empty;
    public string OutputPath = string.Empty;
    //for the enc command
    public uint Key = 0;
    //for the merge command
    public List<string> StuffToMerge = new();
}