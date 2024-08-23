using Viola.Core.Pack.DataClasses;

namespace Viola.Core.Launcher.DataClasses;
public class CLaunchOptions
{
    public Mode Mode { get; set; }
    public Platform PackPlatform { get; set; }
    public string InputPath { get; set; }
    public string OutputPath { get; set; }
    public string HashCachePath { get; set; }
    public string CpkListPath { get; set; }

    //for the enc command
    public uint Key = 0;
    //for the merge command
    public List<string> StuffToMerge = new();
}