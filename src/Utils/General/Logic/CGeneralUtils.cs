using System.IO.Hashing;
using System.Text;
namespace Viola.src.Utils.General.Logic;
class CGeneralUtils
{
    public const string APP_VERSION = "1.2.0";
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

}