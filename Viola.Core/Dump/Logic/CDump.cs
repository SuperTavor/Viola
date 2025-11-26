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
            if (file.Trim().EndsWith(".cpk")) cpkPaths.Add(file);
            else filesToCopy.Add(file);
        }

        foreach (var cpk in cpkPaths)
        {
            CLogger.LogInfo($"Processing {Path.GetFileName(cpk)}...");

            Stream? streamToRead = null;
            string tempFile = Path.Combine(DumpFolder, Path.GetFileName(cpk) + ".tmp");
            bool cleanupTemp = false;

            try
            {
                var fs = CGeneralUtils.GetAppropriateStream(cpk);

                byte[] magic = new byte[4];
                fs.Read(magic, 0, 4);
                fs.Position = 0;

                if (Encoding.UTF8.GetString(magic) != "CPK ")
                {
                    string filename = Path.GetFileName(cpk);

                    // Calculate Key
                    uint key = CCriwareCrypt.CalculateFilenameKey(filename);
                    CLogger.LogInfo($"Decrypting stream with Key: {key:X8}...");

                    // Prepare Temp File
                    Directory.CreateDirectory(DumpFolder);
                    var tempFs = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite);

                    // Decrypt Stream
                    CCriwareCrypt.ProcessStream(fs, tempFs, key);

                    // Verify Result
                    tempFs.Position = 0;
                    byte[] check = new byte[4];
                    tempFs.Read(check, 0, 4);

                    string headerStr = Encoding.UTF8.GetString(check);
                    bool isValidHeader = headerStr == "CPK " || (check[0] == 0x82);

                    if (!isValidHeader)
                    {
                        CLogger.LogInfo($"Header '{headerStr}' invalid. Retrying with lowercase filename...");
                        key = CCriwareCrypt.CalculateFilenameKey(filename.ToLower());

                        fs.Position = 0;
                        tempFs.Position = 0;
                        tempFs.SetLength(0);

                        CCriwareCrypt.ProcessStream(fs, tempFs, key);

                        tempFs.Position = 0;
                        tempFs.Read(check, 0, 4);
                        headerStr = Encoding.UTF8.GetString(check);

                        if (headerStr != "CPK " && !(check[0] == 0x82))
                        {
                            throw new Exception($"Decryption failed. Header is: {headerStr}");
                        }
                    }

                    // Setup for Extraction
                    tempFs.Position = 0;
                    streamToRead = tempFs;
                    cleanupTemp = true;
                    fs.Dispose();
                }
                else
                {
                    streamToRead = fs;
                }

                // Extraction
                if (streamToRead != null)
                {
                    using var reader = new CriFsLib().CreateCpkReader(streamToRead, true);
                    foreach (CpkFile file in reader.GetFiles())
                    {
                        using var extractedFile = reader.ExtractFile(file);
                        var filePath = $"{DumpFolder}/{file.Directory}/{file.FileName}";
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                        File.WriteAllBytes(filePath, extractedFile.Span.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"Error extracting {Path.GetFileName(cpk)}: {ex.Message}");
            }
            finally
            {
                streamToRead?.Dispose();
                if (cleanupTemp && File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        foreach (var file in filesToCopy)
        {
            var destFile = $"{DumpFolder}/{file.Substring(_dirToDump.Length)}";
            CLogger.LogInfo($"Copying already loose file: {Path.GetFileName(file)}");
            var destDir = Path.GetDirectoryName(destFile);
            Directory.CreateDirectory(destDir!);
            File.Copy(file, destFile, true);
        }
        CLogger.LogInfo($"Done. Dumped to `{DumpFolder}/`");
    }
}