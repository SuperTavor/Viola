using Tinifan.Level5.Binary;
using Tinifan.Level5.Binary.Logic;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.EncryptDecrypt.Logic.Utils;

namespace Viola.Core.Pack.Logic;

class CPack
{
    private readonly CLaunchOptions _options;
    private readonly string _dirToPack;

    public CPack(CLaunchOptions options)
    {
        _options = options;
        _dirToPack = Path.TrimEndingDirectorySeparator(_options.InputPath!.Replace("\\", "/"));
    }

    public void PackMod()
    {
        if (_options.ClearOutputBeforePack && Directory.Exists(_options.OutputPath))
        {
            CLogger.LogInfo("Clearing output folder...");
            try
            {
                var dirInfo = new DirectoryInfo(_options.OutputPath);
                foreach (var file in dirInfo.GetFiles()) file.Delete();
                foreach (var dir in dirInfo.GetDirectories()) dir.Delete(true);
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"Failed to clear output folder: {ex.Message}");
            }
        }

        string cpkListInputPath = string.IsNullOrEmpty(_options.CpkListPath)
            ? Path.Combine(_dirToPack, "data", "cpk_list.cfg.bin").Replace("\\", "/")
            : _options.CpkListPath;

        if (!File.Exists(cpkListInputPath))
        {
            CLogger.AddImportantInfo($"Can't find master config at: {cpkListInputPath}");
            return;
        }

        CLogger.LogInfo("Processing cpk_list.cfg.bin...");

        byte[] fileBytes = File.ReadAllBytes(cpkListInputPath);
        bool wasEncrypted = false;

        uint checkHeader = BitConverter.ToUInt32(fileBytes, 0);
        if (checkHeader > 10000000)
        {
            wasEncrypted = true;
            CLogger.LogInfo("Decrypting config...");
            CCriwareCrypt.DecryptBlock(fileBytes, 0, 0x1717E18E);
        }

        CfgBin cpkList = new CfgBin();
        cpkList.Open(fileBytes);

        if (cpkList.Entries.Count == 0 || cpkList.Entries[0].Children == null)
        {
            CLogger.AddImportantInfo("Invalid CfgBin structure: No entries found.");
            return;
        }

        List<Entry> cpkItems = cpkList.Entries[0].Children;

        Dictionary<string, int> existingFileMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < cpkItems.Count; i++)
        {
            var item = cpkItems[i];
            string dir = (string)item.Variables[0].Value;
            string name = (string)item.Variables[1].Value;
            string fullPath = dir + name; // already contains / from game data

            existingFileMap[fullPath] = i;
        }

        var localFiles = CGeneralUtils.GetAllFilesWithNormalSlash(_dirToPack);
        
        Entry? templateEntry = cpkItems.Count > 0 ? cpkItems[cpkItems.Count - 1] : null;

        int processedCount = 0;
        int totalFiles = localFiles.Count;

        foreach (var file in localFiles)
        {
            processedCount++;
            CGeneralUtils.ReportProgress(processedCount, totalFiles, "Updating Config");

            if (file.EndsWith("cpk_list.cfg.bin", StringComparison.OrdinalIgnoreCase)) continue;

            string relativePath = Path.GetRelativePath(_dirToPack, file).Replace("\\", "/");
            int size = (int)new FileInfo(file).Length;

            if (existingFileMap.TryGetValue(relativePath, out int entryIndex))
            {
                CLogger.LogInfo($"[Update] {relativePath}");
                
                var entry = cpkItems[entryIndex];
                
                // Update for Loose File Mode
                entry.Variables[2].Value = ""; // Clear CPK Dir
                entry.Variables[3].Value = ""; // Clear CPK Name
                entry.Variables[4].Value = size; // Update Size
            }
            else
            {
                CLogger.LogInfo($"[Add] {relativePath}");

                if (templateEntry == null)
                {
                    CLogger.AddImportantInfo("Cannot add new files: CfgBin entry list is empty, no template available.");
                    continue; 
                }

                string fileName = Path.GetFileName(relativePath);
                string dirName = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";
                if (!string.IsNullOrEmpty(dirName) && !dirName.EndsWith("/")) dirName += "/";

                Entry newEntry = templateEntry.Clone();

                newEntry.Variables[0].Value = dirName;
                newEntry.Variables[1].Value = fileName;
                newEntry.Variables[2].Value = ""; 
                newEntry.Variables[3].Value = ""; 
                newEntry.Variables[4].Value = size;

                cpkItems.Add(newEntry);
            }
        }

        cpkList.Entries[0].Variables[0].Value = cpkItems.Count;

        string outputModFolder = _options.OutputPath;
        string outputConfigPath = (_options.PackPlatform == DataClasses.Platform.SWITCH)
            ? Path.Combine(outputModFolder, "romfs", "data", "cpk_list.cfg.bin")
            : Path.Combine(outputModFolder, "data", "cpk_list.cfg.bin");

        outputConfigPath = outputConfigPath.Replace("\\", "/");
        Directory.CreateDirectory(Path.GetDirectoryName(outputConfigPath)!);

        byte[] savedBytes = cpkList.Save();

        if (wasEncrypted)
        {
            CLogger.LogInfo("Re-encrypting config...");
            CCriwareCrypt.DecryptBlock(savedBytes, 0, 0x1717E18E);
        }

        File.WriteAllBytes(outputConfigPath, savedBytes);

        CLogger.LogInfo("Copying files...");

        string destRoot = (_options.PackPlatform == DataClasses.Platform.SWITCH)
             ? Path.Combine(outputModFolder, "romfs")
             : outputModFolder;

        var filesToCopy = localFiles.Where(f => !f.EndsWith("data/cpk_list.cfg.bin", StringComparison.OrdinalIgnoreCase)).ToList();

        var distinctDirectories = filesToCopy
            .Select(f => Path.GetDirectoryName(Path.Combine(destRoot, Path.GetRelativePath(_dirToPack, f))))
            .Distinct();

        foreach (var dir in distinctDirectories)
        {
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        long totalFilesToCopy = filesToCopy.Count;
        long copiedCount = 0;
        object lockObj = new object();

        Parallel.ForEach(filesToCopy, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
        {
            string relative = Path.GetRelativePath(_dirToPack, file);
            string destPath = Path.Combine(destRoot, relative);
            
            File.Copy(file, destPath, true);

            lock (lockObj)
            {
                copiedCount++;
                CGeneralUtils.ReportProgress(copiedCount, totalFilesToCopy, "Copying Files");
            }
        });

        CGeneralUtils.ReportProgress(0, 0, "");
        CLogger.LogInfo($"Done packing to `{outputModFolder.Replace("\\", "/")}`");
    }
}