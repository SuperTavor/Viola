using Newtonsoft.Json;
using Viola.Core.Pack.DataClasses;

namespace Viola.Core.Settings.Logic;

public class CSettings
{
    public Platform? DefaultPackPlatform { get; set; } = null;
    public string DefaultDumpInputPath { get; set; } = "";
    public string DefaultDumpOutputPath { get; set; } = "";
    public string DefaultPackInputPath { get; set; } = "";
    public string DefaultPackOutputPath { get; set; } = "";
    public string DefaultVanillaCpkListPath { get; set; } = "";
    public bool ClearOutputBeforePack { get; set; } = false;
    public bool SmartDump { get; set; } = false;

    private static string SettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static CSettings Load()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonConvert.DeserializeObject<CSettings>(json);
                return settings ?? new CSettings();
            }
            catch
            {
                return new CSettings();
            }
        }
        return new CSettings();
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(SettingsPath, json);
    }
}
