using System.IO.Hashing;
using System.Text;
namespace Viola.Utils;
class GeneralUtils
{
    public static List<string> GetAllFiles(string folderPath)
    {
        List<string> filePaths = new List<string>();
        foreach (string filePath in Directory.GetFiles(folderPath))
        {

            filePaths.Add(filePath.Replace("\\", "/"));
        }
        foreach (string subdirectory in Directory.GetDirectories(folderPath))
        {
            filePaths.AddRange(GetAllFiles(subdirectory));
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

    public static uint ComputeCRC32(string data)
    {
        var crc32 = new Crc32();
        crc32.Append(Encoding.UTF8.GetBytes(data));
        var hash = crc32.GetCurrentHashAsUInt32();
        return hash;
    }
}