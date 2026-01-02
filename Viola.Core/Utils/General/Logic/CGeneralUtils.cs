using System.IO;
using System.IO.Hashing;
using System.Text;

namespace Viola.Core.Utils.General.Logic;
public class CGeneralUtils
{
    public const string APP_VERSION = "1.4.2";
    public static bool isConsole = true;
    public static event Action<long, long, string>? OnProgress;

    public static List<string> GetAllFilesWithNormalSlash(string folderPath)
    {
        List<string> filePaths = new List<string>();
        foreach (var f in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            filePaths.Add(f.Replace("\\", "/"));
        }
        return filePaths;
    }

    public static uint ComputeCRC32(byte[] data)
    {
        var crc32 = new Crc32();
        crc32.Append(data);
        var hash = crc32.GetCurrentHashAsUInt32();
        return hash;
    }

    //str overload
    public static uint ComputeCRC32(string data)
    {
        var crc32 = new Crc32();
        crc32.Append(Encoding.UTF8.GetBytes(data));
        var hash = crc32.GetCurrentHashAsUInt32();
        return hash;
    }

    public static Stream GetAppropriateStream(string path)
    {
        long length = new FileInfo(path).Length;

        const long twoGBThreshold = (long)2 * 1024 * 1024 * 1024;

        if (length < twoGBThreshold)
        {
            return new MemoryStream(File.ReadAllBytes(path));
        }

        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public static Stream CreateAppropriateStream(string path, long estimatedSize)
    {
        // Use 50MB threshold for in-memory creation to avoid high memory pressure when combined with input stream
        const long memoryThreshold = 50 * 1024 * 1024;

        if (estimatedSize < memoryThreshold)
        {
            return new MemoryStream((int)estimatedSize);
        }

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
    }

    public static void ReportProgress(long current, long total, string prefix = "")
    {
        if (OnProgress != null)
        {
            OnProgress.Invoke(current, total, prefix);
        }
    }
}