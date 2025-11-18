using System.IO;
using System.IO.Hashing;
using System.Text;

namespace Viola.Core.Utils.General.Logic;
public class CGeneralUtils
{
    public const string APP_VERSION = "1.3.0";
    public static bool isConsole = true;
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

    public static Stream GetAppropiateStream(string path)
    {
        long length = new FileInfo(path).Length;
    
        const long twoGBThreshold = (long)2 * 1024 * 1024 * 1024;
    
        if (length < TwoGB)
        {
            return new MemoryStream(File.ReadAllBytes(path));
        }

        return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    }

}
