using CriFsV2Lib;
using System.Text;
using CriFsV2Lib.Definitions.Structs;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.ViolaLogger.Logic;
namespace Viola.Core.Dump.Logic;
class CDump
{
    private string _dirToDump = string.Empty;

    public static string DumpFolder = "dumped";

    private CLaunchOptions _options;

    public CDump(CLaunchOptions options)
    {
        _options = options;
    }
    public void DumpRomfs()
    {
        _dirToDump = _options.InputPath;
        DumpFolder = _options.OutputPath;
        var folderFiles = CGeneralUtils.GetAllFilesWithNormalSlash(_dirToDump);
        var cpkPaths = new List<string>();
        var filesToCopy = new List<string>();
        foreach (var file in folderFiles)
        {
            if (file.Trim().EndsWith(".cpk"))
            {
                cpkPaths.Add(file);
            }
            else filesToCopy.Add(file);
        }
        foreach (var cpk in cpkPaths)
        {
            CLogger.LogInfo($"Extracting {cpk}\n");
            var mem = new MemoryStream(File.ReadAllBytes(cpk));
            byte[] magicBuf = new byte[4];
            mem.Read(magicBuf, 0, 4);
            mem.Position = 0;
            if (Encoding.UTF8.GetString(magicBuf) != "CPK ")
            {

                //Try decrypting it in case it uses the new Criware crypt introduced in MEGATON MUSASHI W: WIRED for PC.
                try
                {
                    var decryptor = new CCriwareCrypt(mem, CriwareCryptMode.Decrypt, Encoding.UTF8.GetBytes("CPK "));
                    decryptor.DecryptFile();
                }
                catch(FormatException)
                {
                    CLogger.AddImportantInfo("Invalid or corrupted CPK: " + cpk + ".\nPlease delete it or replace it with a valid CPK.");
                    CLogger.AddImportantInfo("Operation cancelled due to error.");
                    return;
                }
            }
            using var reader = new CriFsLib().CreateCpkReader(mem, true);
            var files = reader.GetFiles();
            foreach (CpkFile file in files)
            {
                using var extractedFile = reader.ExtractFile(file);
                var filePath = $"{DumpFolder}/{file.Directory}/{file.FileName}";
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllBytes(filePath, extractedFile.Span.ToArray());
            }
            mem.Close();
        }
        foreach (var file in filesToCopy)
        {
            var destFile = $"{DumpFolder}/{file.Substring(_dirToDump.Length)}";
            Console.WriteLine($"Copying already loose file: {Path.GetFileName(file)}");
            var destDir = Path.GetDirectoryName(destFile);
            Directory.CreateDirectory(destDir!);
            File.Copy(file, destFile, true);
        }
        Console.WriteLine($"Done cleaning. You can find your dumped RomFS in `{DumpFolder}/'");

    }



}