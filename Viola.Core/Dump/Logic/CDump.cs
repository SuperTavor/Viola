using CriFsV2Lib;
using System.Text;
using CriFsV2Lib.Definitions.Structs;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.Settings.Logic;
using Tinifan.Level5.Binary;
using System.Runtime;

namespace Viola.Core.Dump.Logic;

class CDump
{
    private string _dirToDump = string.Empty;
    public static string DumpFolder = "dumped";
    private CLaunchOptions _options;

    private Dictionary<string, int>? _smartDumpSizes;
    private Dictionary<string, List<string>>? _cpkContents;

    public CDump(CLaunchOptions options)
    {
        _options = options;
    }

    public void DumpRomfs()
    {
        _dirToDump = _options.InputPath;
        DumpFolder = _options.OutputPath;
        CLogger.LogInfo("Scanning directory...");
        
        var folderFiles = CGeneralUtils.GetAllFilesWithNormalSlash(_dirToDump);
        var cpkPaths = new List<string>();
        var filesToCopy = new List<string>();

        foreach (var file in folderFiles)
        {
            if (file.Trim().EndsWith(".cpk", StringComparison.OrdinalIgnoreCase)) cpkPaths.Add(file);
            else filesToCopy.Add(file);
        }

        int totalCpks = cpkPaths.Count;
        var settings = CSettings.Load();

        if (settings.SmartDump)
        {
            LoadSmartDumpData();
        }

        CLogger.LogInfo($"Found {totalCpks} CPK(s) to dump.");
        
        try
        {
            for (int i = 0; i < totalCpks; i++)
            {
                ProcessCpk(cpkPaths[i], i, totalCpks);
            }

            CopyLooseFiles(filesToCopy, settings.SmartDump);
            
            CGeneralUtils.ReportProgress(0, 0, "");
            CLogger.LogInfo($"Done. Dumped to `{DumpFolder.Replace("\\", "/")}`");
        }
        finally
        {
            _smartDumpSizes = null;
            _cpkContents = null;

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private void LoadSmartDumpData()
    {
        string cpkListPath = _options.CpkListPath;

        if (string.IsNullOrEmpty(cpkListPath) || !File.Exists(cpkListPath))
        {
            cpkListPath = Path.Combine(_dirToDump, "data", "cpk_list.cfg.bin");
            if (!File.Exists(cpkListPath))
            {
                cpkListPath = Path.Combine(DumpFolder, "data", "cpk_list.cfg.bin");
            }
        }

        if (File.Exists(cpkListPath))
        {
            try
            {
                CLogger.LogInfo($"Loading Smart Dump data from {Path.GetFileName(cpkListPath)}...");
                byte[] fileBytes = File.ReadAllBytes(cpkListPath);
                uint checkHeader = BitConverter.ToUInt32(fileBytes, 0);
                if (checkHeader > 10000000)
                {
                    CCriwareCrypt.DecryptBlock(fileBytes, 0, 0x1717E18E);
                }

                CfgBin cpkList = new CfgBin();
                cpkList.Open(fileBytes);

                if (cpkList.Entries.Count > 0 && cpkList.Entries[0].Children != null)
                {
                    _smartDumpSizes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    _cpkContents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in cpkList.Entries[0].Children)
                    {
                        string dir = (string)item.Variables[0].Value;
                        string name = (string)item.Variables[1].Value;
                        string fullPath = dir + name;
                        int size = (int)item.Variables[4].Value;
                        _smartDumpSizes[fullPath] = size;

                        string cpkDir = (string)item.Variables[2].Value;
                        string cpkName = (string)item.Variables[3].Value;
                        if (!string.IsNullOrEmpty(cpkName))
                        {
                            string cpkPath = Path.Combine(cpkDir, cpkName).Replace("\\", "/");
                            if (cpkPath.StartsWith("/")) cpkPath = cpkPath[1..];
                            
                            if (!_cpkContents.ContainsKey(cpkPath))
                            {
                                _cpkContents[cpkPath] = new List<string>();
                            }
                            _cpkContents[cpkPath].Add(fullPath);
                        }
                    }
                    CLogger.LogInfo($"Smart Dump enabled. Loaded {_smartDumpSizes.Count} file entries.");
                }
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"Failed to load Smart Dump data: {ex.Message}. Proceeding with full dump.");
                _smartDumpSizes = null;
                _cpkContents = null;
            }
        }
        else
        {
            CLogger.LogInfo("Smart Dump enabled but cpk_list.cfg.bin not found. Proceeding with full dump.");
        }
    }

    private void ProcessCpk(string cpkPath, int index, int totalCpks)
    {
        if (ShouldSkipCpk(cpkPath, index, totalCpks)) return;

        string tempFile = Path.Combine(DumpFolder, Path.GetFileName(cpkPath) + ".tmp");
        bool cleanupTemp = false;
        Stream? streamToRead = null;

        try
        {
            streamToRead = PrepareCpkStream(cpkPath, index, totalCpks, tempFile, out cleanupTemp);
            
            if (streamToRead != null)
            {
                ExtractFilesFromStream(streamToRead);
            }
        }
        catch (Exception ex)
        {
            CLogger.AddImportantInfo($"Error extracting {Path.GetFileName(cpkPath)}: {ex.Message}");
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

    private bool ShouldSkipCpk(string cpkPath, int index, int totalCpks)
    {
        if (_cpkContents != null && _smartDumpSizes != null)
        {
            string relativeCpk = Path.GetRelativePath(_dirToDump, cpkPath).Replace("\\", "/");
            List<string>? expectedFiles = null;

            if (_cpkContents.TryGetValue(relativeCpk, out expectedFiles))
            {
                // Direct match found
            }
            else
            {
                // Try fuzzy match for folder structure mismatches
                string? matchedKey = null;
                int matchCount = 0;

                foreach (var key in _cpkContents.Keys)
                {
                    if (key.EndsWith(relativeCpk, StringComparison.OrdinalIgnoreCase) || 
                        relativeCpk.EndsWith(key, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedKey = key;
                        matchCount++;
                    }
                }

                if (matchCount == 1 && matchedKey != null)
                {
                    expectedFiles = _cpkContents[matchedKey];
                }
            }

            if (expectedFiles != null)
            {
                bool allExist = true;
                int checkedCount = 0;
                int totalToCheck = expectedFiles.Count;
                string cpkName = Path.GetFileName(cpkPath);

                CGeneralUtils.ReportProgress(0, totalToCheck, $"Verifying {cpkName}");

                foreach (var file in expectedFiles)
                {
                    checkedCount++;
                    if (checkedCount % 50 == 0)
                    {
                        CGeneralUtils.ReportProgress(checkedCount, totalToCheck, $"Verifying {cpkName}");
                    }

                    string checkPath = Path.Combine(DumpFolder, file.StartsWith("/") ? file[1..] : file);
                    if (!File.Exists(checkPath))
                    {
                        allExist = false;
                        break;
                    }
                    if (new FileInfo(checkPath).Length != _smartDumpSizes[file])
                    {
                        allExist = false;
                        break;
                    }
                }

                if (allExist)
                {
                    CLogger.LogInfo($"Skipping {cpkName} ({index + 1}/{totalCpks}) (Smart Dump - All files exist)");
                    return true;
                }
            }
        }
        return false;
    }

    private Stream? PrepareCpkStream(string cpkPath, int index, int totalCpks, string tempFile, out bool cleanupTemp)
    {
        cleanupTemp = false;
        var fs = CGeneralUtils.GetAppropriateStream(cpkPath);
        long streamLength = fs.Length;

        byte[] magic = new byte[4];
        fs.Read(magic, 0, 4);
        fs.Position = 0;

        if (Encoding.UTF8.GetString(magic) == "CPK ")
        {
            return fs;
        }

        // Decryption needed
        string filename = Path.GetFileName(cpkPath);
        uint key = CCriwareCrypt.CalculateFilenameKey(filename);
        CLogger.LogInfo($"Processing {filename}... ({index + 1}/{totalCpks})\nDecrypting stream with Key: {key:X8}...");

        Stream targetStream = CGeneralUtils.CreateAppropriateStream(tempFile, streamLength);
        if (targetStream is FileStream)
        {
            cleanupTemp = true;
        }

        CCriwareCrypt.ProcessStream(fs, targetStream, key, (curr, tot) =>
        {
            CGeneralUtils.ReportProgress(curr, tot, "Decrypting");
        });

        // Verify
        targetStream.Position = 0;
        byte[] check = new byte[4];
        targetStream.Read(check, 0, 4);
        string headerStr = Encoding.UTF8.GetString(check);
        bool isValidHeader = headerStr == "CPK " || (check[0] == 0x82);

        if (!isValidHeader)
        {
            CLogger.LogInfo($"Header '{headerStr}' invalid. Retrying with lowercase filename...");
            key = CCriwareCrypt.CalculateFilenameKey(filename.ToLower());

            fs.Position = 0;
            targetStream.Position = 0;
            targetStream.SetLength(0);

            CCriwareCrypt.ProcessStream(fs, targetStream, key, (curr, tot) =>
            {
                CGeneralUtils.ReportProgress(curr, tot, "Decrypting (Retry)");
            });

            targetStream.Position = 0;
            targetStream.Read(check, 0, 4);
            headerStr = Encoding.UTF8.GetString(check);

            if (headerStr != "CPK " && !(check[0] == 0x82))
            {
                fs.Dispose();
                targetStream.Dispose();
                throw new Exception($"Decryption failed. Header is: {headerStr}");
            }
        }

        targetStream.Position = 0;
        fs.Dispose();
        return targetStream;
    }

    private void ExtractFilesFromStream(Stream stream)
    {
        using var reader = new CriFsLib().CreateCpkReader(stream, true);
        var files = reader.GetFiles().ToArray();
        
        long totalEstimatedBytes = 0;
        foreach (var f in files) totalEstimatedBytes += f.FileSize;
        if (totalEstimatedBytes == 0) totalEstimatedBytes = 1; // Avoid divide by zero

        double currentProgressBase = 0;
        long lastUpdateTick = 0;

        HashSet<string> createdDirs = [];

        CGeneralUtils.ReportProgress(0, 10000, "Extracting");

        foreach (CpkFile file in files)
        {
            long fileSize = file.FileSize;
            double fileWeight = (double)fileSize / totalEstimatedBytes;

            if (_smartDumpSizes != null)
            {
                string relativePath = string.IsNullOrEmpty(file.Directory)
                    ? file.FileName
                    : $"{file.Directory}/{file.FileName}";

                if (_smartDumpSizes.TryGetValue(relativePath, out int expectedSize))
                {
                    var checkPath = Path.Combine(DumpFolder, relativePath);
                    if (File.Exists(checkPath))
                    {
                        long currentSize = new FileInfo(checkPath).Length;
                        if (currentSize == expectedSize)
                        {
                            // Skip file but advance progress
                            currentProgressBase += fileWeight;
                            // Report progress occasionally even when skipping to keep UI responsive
                            long now = DateTime.Now.Ticks;
                            if (now - lastUpdateTick > 500000)
                            {
                                CGeneralUtils.ReportProgress((long)(currentProgressBase * 10000), 10000, "Extracting");
                                lastUpdateTick = now;
                            }
                            continue;
                        }
                    }
                }
            }

            using var extractedFile = reader.ExtractFile(file);
            var filePath = $"{DumpFolder}/{file.Directory}/{file.FileName}";
            
            string dirPath = Path.GetDirectoryName(filePath)!;
            if (createdDirs.Add(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            
            // Optimization: Larger buffer for writing (64KB)
            using var outFs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
            
            var span = extractedFile.Span;
            long currentFileBytes = 0;
            int chunkSize = 5 * 1024 * 1024; // 5MB chunks
            
            if (span.Length > 0)
            {
                for (int j = 0; j < span.Length; j += chunkSize)
                {
                    int size = Math.Min(chunkSize, span.Length - j);
                    outFs.Write(span.Slice(j, size));
                    currentFileBytes += size;

                    long now = DateTime.Now.Ticks;
                    if (now - lastUpdateTick > 500000) // 50ms
                    {
                        double fileRatio = (fileSize > 0) ? ((double)currentFileBytes / fileSize) : 1.0;
                        if (fileRatio > 1.0) fileRatio = 1.0;

                        double globalProgress = currentProgressBase + (fileWeight * fileRatio);
                        CGeneralUtils.ReportProgress((long)(globalProgress * 10000), 10000, "Extracting");
                        lastUpdateTick = now;
                    }
                }
            }
            
            currentProgressBase += fileWeight;
            
            long endNow = DateTime.Now.Ticks;
            if (endNow - lastUpdateTick > 500000)
            {
                 CGeneralUtils.ReportProgress((long)(currentProgressBase * 10000), 10000, "Extracting");
                 lastUpdateTick = endNow;
            }
        }
        CGeneralUtils.ReportProgress(10000, 10000, "Extracting");
    }

    private void CopyLooseFiles(List<string> filesToCopy, bool smartDump)
    {
        byte[] buffer = new byte[5 * 1024 * 1024]; // 5MB buffer
        foreach (var file in filesToCopy)
        {
            var destFile = $"{DumpFolder}/{file.Substring(_dirToDump.Length)}";

            if (smartDump && File.Exists(destFile))
            {
                long srcLen = new FileInfo(file).Length;
                long dstLen = new FileInfo(destFile).Length;
                if (srcLen == dstLen)
                {
                    CLogger.LogInfo($"Skipping {Path.GetFileName(file)} (Smart Dump)");
                    continue;
                }
            }

            string fileName = Path.GetFileName(file);
            CLogger.LogInfo($"Copying already loose file: {fileName}");
            var destDir = Path.GetDirectoryName(destFile);
            Directory.CreateDirectory(destDir!);
            
            long fileLength = new FileInfo(file).Length;
            long currentFileBytes = 0;
            long lastUpdateTick = 0;

            CGeneralUtils.ReportProgress(0, fileLength, $"Copying {fileName}");

            using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write);

            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                destStream.Write(buffer, 0, bytesRead);
                currentFileBytes += bytesRead;

                long now = DateTime.Now.Ticks;
                if (now - lastUpdateTick > 500000) // 50ms
                {
                    CGeneralUtils.ReportProgress(currentFileBytes, fileLength, $"Copying {fileName}");
                    lastUpdateTick = now;
                }
            }
            CGeneralUtils.ReportProgress(fileLength, fileLength, $"Copying {fileName}");
        }
    }
}