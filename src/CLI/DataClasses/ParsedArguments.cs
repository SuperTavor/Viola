namespace Viola.CLINS;
class ParsedArguments
{
    public eMode Mode { get; set; }
    public string? InputPath = null;
    public string OutputPath = string.Empty;
    public List<string> StuffToMerge = new();
    public string Alias =string.Empty;
}