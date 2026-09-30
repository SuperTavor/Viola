using Tinifan.Level5.Binary;
using Tinifan.Level5.Binary.Logic;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Utils.Cpk.Logic;
using Viola.Core.Utils.CpkList.Logic;
using Viola.Core.Pack.DataClasses;

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

        byte[] originalFileBytes = File.ReadAllBytes(cpkListInputPath);
        byte[] fileBytes = originalFileBytes;
        bool wasEncrypted = false;
        LogIgnoredJunkFiles();
        var localFiles = CGeneralUtils.GetAllFilesWithNormalSlash(_dirToPack);
        var userCustomPacks = FindCustomPacks(localFiles);
        string outputModFolder = _options.OutputPath;
        string destRoot = (_options.PackPlatform == DataClasses.Platform.NintendoSwitch)
             ? Path.Combine(outputModFolder, "romfs")
             : outputModFolder;
        string outputConfigPath = (_options.PackPlatform == DataClasses.Platform.NintendoSwitch)
            ? Path.Combine(outputModFolder, "romfs", "data", "cpk_list.cfg.bin")
            : Path.Combine(outputModFolder, "data", "cpk_list.cfg.bin");

        outputConfigPath = outputConfigPath.Replace("\\", "/");
        Directory.CreateDirectory(Path.GetDirectoryName(outputConfigPath)!);
        var autoPackedAudio = BuildAutoPackedAudio(originalFileBytes, cpkListInputPath, localFiles, userCustomPacks);

        if (CCpkListUtils.IsModernCpkList(originalFileBytes))
        {
            CLogger.LogInfo("Decrypting modern T2B config...");
            if (!CCpkListUtils.TryPackModernCpkList(
                    originalFileBytes,
                    localFiles,
                    _dirToPack,
                    out var modernSavedBytes,
                    CLogger.LogInfo,
                    (current, total) => CGeneralUtils.ReportProgress(current, total, "Updating Config"),
                    autoPackedAudio.CustomPackNames,
                    autoPackedAudio.RedirectedPackNames,
                    autoPackedAudio.CpkEntries))
            {
                CLogger.AddImportantInfo("Failed to update modern T2B cpk_list.cfg.bin.");
                return;
            }

            CLogger.LogInfo("Re-encrypting modern T2B config...");
            File.WriteAllBytes(outputConfigPath, modernSavedBytes);
            goto CopyFiles;
        }

        if (!CCpkListUtils.TryGetCfgBinPayload(fileBytes, out fileBytes, out wasEncrypted))
        {
            CLogger.AddImportantInfo("Invalid CfgBin structure: No entries found.");
            return;
        }

        if (wasEncrypted)
        {
            CLogger.LogInfo("Decrypting config...");
        }

        CfgBin cpkList = new CfgBin();
        cpkList.Open(fileBytes);

        if (cpkList.Entries.Count == 0 || cpkList.Entries[0].Children == null)
        {
            CLogger.AddImportantInfo("Invalid CfgBin structure: No entries found.");
            return;
        }

        List<Entry> cpkItems = cpkList.Entries[0].Children;
        
        Entry? templateEntry = cpkItems.Count > 0 ? cpkItems[cpkItems.Count - 1] : null;

        //Infer cpklist structure from templateEntry variableCount
        CpkListStructure cpkListMode;
        switch(templateEntry.Variables.Count)
        {
            case CGeneralUtils.OLD_CPKLIST_CNT:
                cpkListMode = CpkListStructure.Old;
                break;
            case CGeneralUtils.NEW_CPKLIST_CNT:
                cpkListMode = CpkListStructure.New;
                break;
            default:
                cpkListMode = CpkListStructure.None;
                break;
        }
        if (cpkListMode == CpkListStructure.None)
        {
            CLogger.AddImportantInfo("Invalid CfgBin structure: Unknown entry structure.");
            return;
        }

        Dictionary<string, int> existingFileMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < cpkItems.Count; i++)
        {
            var item = cpkItems[i];
            string fullPath = "";
            if(cpkListMode == CpkListStructure.New)
            {
                string dir = (string)item.Variables[0].Value;
                string name = (string)item.Variables[1].Value;
                fullPath = dir + name; // already contains / from game data
            }
            else
            {
                fullPath = (string)item.Variables[0].Value;
            }

            existingFileMap[fullPath] = i;
        }
        


        int processedCount = 0;
        int totalFiles = localFiles.Count;
        var customPacks = new HashSet<string>(userCustomPacks, StringComparer.OrdinalIgnoreCase);
        customPacks.UnionWith(autoPackedAudio.CustomPackNames);

        foreach (var file in localFiles)
        {
            processedCount++;
            CGeneralUtils.ReportProgress(processedCount, totalFiles, "Updating Config");

            if (file.EndsWith("cpk_list.cfg.bin", StringComparison.OrdinalIgnoreCase)) continue;

            string relativePath = Path.GetRelativePath(_dirToPack, file).Replace("\\", "/");
            if (IsCustomPack(relativePath)) continue;
            int size = (int)new FileInfo(file).Length;

            if (existingFileMap.TryGetValue(relativePath, out int entryIndex))
            {
                CLogger.LogInfo($"[Update] {relativePath}");
                
                var entry = cpkItems[entryIndex];
                string cpkName = "";
                if(cpkListMode == CpkListStructure.New)
                {
                    cpkName = Convert.ToString(entry.Variables[3].Value) ?? string.Empty;
                }
                else
                {
                    cpkName = Path.GetFileName((string)entry.Variables[1].Value) ?? string.Empty;
                }
                if (autoPackedAudio.CpkEntries.TryGetValue(relativePath, out var audioCpkPath))
                {
                    if (cpkListMode == CpkListStructure.New)
                    {
                        SetCfgBinCpkPath(entry, audioCpkPath);
                    }
                    else
                    {
                        entry.Variables[1].Value = audioCpkPath;
                    }
                }
                else if (autoPackedAudio.RedirectedPackNames.TryGetValue(cpkName, out var redirectedCpkName))
                {
                    if(cpkListMode == CpkListStructure.New)
                    {
                        entry.Variables[2].Value = "data/packs/";
                        entry.Variables[3].Value = redirectedCpkName;
                    }

                }
                else if (customPacks.Contains(cpkName))
                {
                    if(cpkListMode == CpkListStructure.New)
                    {
                        entry.Variables[2].Value = "data/packs_custom/";
                        entry.Variables[3].Value = cpkName;
                    }
                    else
                    {
                        entry.Variables[1].Value = "data/packs_custom/" + cpkName;
                    }
                }
                else
                {
                    // Update for Loose File Mode
                    if (cpkListMode == CpkListStructure.New)
                    {
                        entry.Variables[2].Value = ""; // Clear CPK Dir
                        entry.Variables[3].Value = ""; // Clear CPK Name
                    }
                    else
                    {
                        entry.Variables[1].Value = "";
                    }
                }
                // Update Size
                if (cpkListMode == CpkListStructure.New)
                    entry.Variables[4].Value = size;
                else entry.Variables[2].Value = size;
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

                if(cpkListMode == CpkListStructure.Old)
                {
                    newEntry.Variables[0].Value = dirName + fileName;
                    newEntry.Variables[1].Value = autoPackedAudio.CpkEntries.GetValueOrDefault(relativePath, "");
                    newEntry.Variables[2].Value = size;
                }
                else if(cpkListMode == CpkListStructure.New)
                {
                    newEntry.Variables[0].Value = dirName; //fileDir
                    newEntry.Variables[1].Value = fileName; //fileName
                    newEntry.Variables[2].Value = ""; //cpkDir
                    newEntry.Variables[3].Value = ""; //cpkName
                    newEntry.Variables[4].Value = size; //fileSize
                    if (autoPackedAudio.CpkEntries.TryGetValue(relativePath, out var audioCpkPath))
                    {
                        SetCfgBinCpkPath(newEntry, audioCpkPath);
                    }
                }
   

                cpkItems.Add(newEntry);
            }
        }
        
        if(cpkListMode == CpkListStructure.New)
        {
            var sortedCpkItems = cpkItems
    .OrderBy(item => CCpkListUtils.GetCpkListEntryIndex(
        (Convert.ToString(item.Variables[0].Value) ?? string.Empty) +
        (Convert.ToString(item.Variables[1].Value) ?? string.Empty)))
    .ToList();
            cpkItems.Clear();
            cpkItems.AddRange(sortedCpkItems);
        }

        cpkList.Entries[0].Variables[0].Value = cpkItems.Count;

        byte[] savedBytes = cpkList.Save();

        if (wasEncrypted)
        {
            CLogger.LogInfo("Re-encrypting config...");
            CCriwareCrypt.DecryptBlock(savedBytes, 0, 0x1717E18E);
        }

        File.WriteAllBytes(outputConfigPath, savedBytes);

    CopyFiles:
        WriteAutoPackedAudio(destRoot, autoPackedAudio);
        CLogger.LogInfo("Copying files...");

        var filesToCopy = localFiles
            .Where(f => !f.EndsWith("data/cpk_list.cfg.bin", StringComparison.OrdinalIgnoreCase))
            .Where(f => !autoPackedAudio.SourceFiles.Contains(f))
            .ToList();

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

    private static bool IsCustomPack(string relativePath)
    {
        return relativePath.StartsWith("data/packs_custom/", StringComparison.OrdinalIgnoreCase) &&
               relativePath.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase);
    }

    private HashSet<string> FindCustomPacks(IReadOnlyList<string> localFiles)
    {
        var packs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var localFile in localFiles)
        {
            string relativePath = Path.GetRelativePath(_dirToPack, localFile).Replace("\\", "/");
            if (IsCustomPack(relativePath))
            {
                packs.Add(Path.GetFileName(relativePath));
            }
        }

        return packs;
    }

    private AutoPackedAudio BuildAutoPackedAudio(
        byte[] cpkListBytes,
        string cpkListInputPath,
        IReadOnlyList<string> localFiles,
        IReadOnlySet<string> userCustomPacks)
    {
        var result = new AutoPackedAudio();
        if (!CCpkListUtils.TryReadEntries(cpkListBytes, out var entries))
        {
            return result;
        }

        var cpkByPath = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CpkPath))
            .ToDictionary(entry => NormalizeRelativePath(entry.FullPath), entry => entry.CpkPath, StringComparer.OrdinalIgnoreCase);

        foreach (var localFile in localFiles)
        {
            var relativePath = NormalizeRelativePath(Path.GetRelativePath(_dirToPack, localFile));
            if (!IsAutoPackedAudioFile(relativePath) || !cpkByPath.TryGetValue(relativePath, out var cpkPath))
            {
                continue;
            }

            var cpkName = Path.GetFileName(cpkPath.Replace("\\", "/"));
            if (string.IsNullOrWhiteSpace(cpkName) || userCustomPacks.Contains(cpkName))
            {
                continue;
            }

            if (!result.FilesByCpk.TryGetValue(cpkName, out var files))
            {
                files = new List<CpkFilePayload>();
                result.FilesByCpk.Add(cpkName, files);
            }

            files.Add(new CpkFilePayload(GetCpkInternalPath(relativePath), localFile, false));
            result.SourceFiles.Add(localFile);
            result.CustomPackNames.Add(cpkName);
            result.OriginalCpkPaths.TryAdd(cpkName, ResolveOriginalCpkPath(cpkPath, cpkListInputPath));
        }

        var unassignedAudio = localFiles
            .Select(localFile => (LocalFile: localFile, RelativePath: NormalizeRelativePath(Path.GetRelativePath(_dirToPack, localFile))))
            .Where(file => IsAutoPackedAudioFile(file.RelativePath) && !cpkByPath.ContainsKey(file.RelativePath))
            .ToList();

        if (unassignedAudio.Count > 0)
        {
            var targetPack = FindSmallestOriginalCpk(entries, cpkListInputPath, userCustomPacks);
            if (targetPack is null)
            {
                CLogger.AddImportantInfo("No original CPK could be found; new audio will use a compact custom CPK.");
            }

            string cpkName = targetPack?.Name ?? GetAvailableAudioPackName(userCustomPacks);
            string? originalCpkPath = targetPack?.Path;
            result.OriginalCpkPaths.TryAdd(cpkName, originalCpkPath);
            result.CustomPackNames.Add(cpkName);

            if (!result.FilesByCpk.TryGetValue(cpkName, out var files))
            {
                files = new List<CpkFilePayload>();
                result.FilesByCpk.Add(cpkName, files);
            }

            foreach (var audio in unassignedAudio)
            {
                files.Add(new CpkFilePayload(GetCpkInternalPath(audio.RelativePath), audio.LocalFile, true));
                result.SourceFiles.Add(audio.LocalFile);
                result.CpkEntries[audio.RelativePath] = $"data/packs_custom/{cpkName}";
            }
        }

        return result;
    }

    private (string Name, string Path)? FindSmallestOriginalCpk(
        IReadOnlyList<CpkListEntry> entries,
        string cpkListInputPath,
        IReadOnlySet<string> userCustomPacks)
    {
        (string Name, string Path, long Size)? smallest = null;

        foreach (var cpkPath in entries
                     .Select(entry => NormalizeRelativePath(entry.CpkPath))
                     .Where(path => path.StartsWith("data/packs/", StringComparison.OrdinalIgnoreCase) &&
                                    path.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var cpkName = Path.GetFileName(cpkPath);
            if (userCustomPacks.Contains(cpkName))
            {
                continue;
            }

            var originalPath = ResolveOriginalCpkPath(cpkPath, cpkListInputPath);
            if (originalPath is null)
            {
                continue;
            }

            var candidate = (Name: cpkName, Path: originalPath, Size: new FileInfo(originalPath).Length);
            if (smallest is null || candidate.Size < smallest.Value.Size ||
                candidate.Size == smallest.Value.Size && StringComparer.OrdinalIgnoreCase.Compare(candidate.Name, smallest.Value.Name) < 0)
            {
                smallest = candidate;
            }
        }

        return smallest is null ? null : (smallest.Value.Name, smallest.Value.Path);
    }

    private static string GetAvailableAudioPackName(IReadOnlySet<string> userCustomPacks)
    {
        const string baseName = "audio_custom";
        var name = $"{baseName}.cpk";
        var suffix = 2;
        while (userCustomPacks.Contains(name))
        {
            name = $"{baseName}_{suffix++}.cpk";
        }

        return name;
    }

    private static string GetCpkInternalPath(string relativePath)
    {
        return relativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
            ? relativePath[5..]
            : relativePath;
    }

    private static void SetCfgBinCpkPath(Entry entry, string cpkPath)
    {
        var normalizedPath = cpkPath.Replace('\\', '/');
        var cpkDirectory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? string.Empty;
        entry.Variables[2].Value = string.IsNullOrEmpty(cpkDirectory) ? string.Empty : $"{cpkDirectory}/";
        entry.Variables[3].Value = Path.GetFileName(normalizedPath);
    }

    private static void WriteAutoPackedAudio(string destRoot, AutoPackedAudio audio)
    {
        foreach (var (cpkName, files) in audio.FilesByCpk)
        {
            var outputPath = Path.Combine(destRoot, "data", "packs_custom", cpkName);
            CLogger.LogInfo($"[Pack] data/packs_custom/{cpkName} ({files.Count} audio file(s), source {cpkName})");
            var originalCpkPath = audio.OriginalCpkPaths.GetValueOrDefault(cpkName);
            WriteEncryptedAutoPack(outputPath, files, originalCpkPath);
        }
    }

    private static void WriteEncryptedAutoPack(string outputPath, IReadOnlyList<CpkFilePayload> files, string? originalCpkPath)
    {
        var tempPath = outputPath + ".tmp";
        var decryptedOriginalPath = tempPath + ".original";
        var encryptedTempPath = tempPath + ".encrypted";

        if (!string.IsNullOrWhiteSpace(originalCpkPath) && File.Exists(originalCpkPath))
        {
            CLogger.LogInfo($"[Pack] Using original CPK template: {Path.GetFileName(originalCpkPath)}");
            DecryptCpkIfNeeded(originalCpkPath, decryptedOriginalPath);
            CAudioCpkRebuilder.Write(decryptedOriginalPath, tempPath, files, Path.GetDirectoryName(tempPath)!);
        }
        else
        {
            CLogger.AddImportantInfo($"Original CPK not found for {Path.GetFileName(outputPath)}. Falling back to compact audio CPK.");
            CCriCpkWriter.Write(tempPath, files);
        }

        try
        {
            var key = CCriwareCrypt.CalculateFilenameKey(Path.GetFileName(outputPath));
            using (var input = File.OpenRead(tempPath))
            using (var output = new FileStream(encryptedTempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CCriwareCrypt.ProcessStream(input, output, key);
            }

            if (File.Exists(outputPath))
            {
                CLogger.LogInfo($"[Pack] Overwriting existing CPK: {Path.GetFileName(outputPath)}");
                File.SetAttributes(outputPath, FileAttributes.Normal);
            }

            File.Move(encryptedTempPath, outputPath, true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (File.Exists(decryptedOriginalPath))
            {
                File.Delete(decryptedOriginalPath);
            }

            if (File.Exists(encryptedTempPath))
            {
                File.Delete(encryptedTempPath);
            }
        }
    }

    private static void DecryptCpkIfNeeded(string sourcePath, string targetPath)
    {
        using var source = File.OpenRead(sourcePath);
        Span<byte> magic = stackalloc byte[4];
        source.ReadExactly(magic);
        source.Position = 0;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
        if (magic.SequenceEqual("CPK "u8))
        {
            source.CopyTo(target);
            return;
        }

        var key = CCriwareCrypt.CalculateFilenameKey(Path.GetFileName(sourcePath));
        CCriwareCrypt.ProcessStream(source, target, key);
    }

    private string? ResolveOriginalCpkPath(string cpkPath, string cpkListInputPath)
    {
        var normalizedCpkPath = NormalizeRelativePath(cpkPath);
        var candidates = new List<string>();

        candidates.Add(Path.Combine(_dirToPack, normalizedCpkPath));

        var cpkListDir = Path.GetDirectoryName(cpkListInputPath);
        if (!string.IsNullOrWhiteSpace(cpkListDir))
        {
            candidates.Add(Path.Combine(cpkListDir, normalizedCpkPath));
            if (Path.GetFileName(cpkListDir).Equals("data", StringComparison.OrdinalIgnoreCase))
            {
                var gameRoot = Path.GetDirectoryName(cpkListDir);
                if (!string.IsNullOrWhiteSpace(gameRoot))
                {
                    candidates.Add(Path.Combine(gameRoot, normalizedCpkPath));
                }
            }

            if (normalizedCpkPath.StartsWith("data/", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(Path.Combine(cpkListDir, normalizedCpkPath[5..]));
            }
        }

        var cpkName = Path.GetFileName(normalizedCpkPath);
        candidates.AddRange(GetKnownGameCpkCandidates(cpkName));

        foreach (var candidate in candidates.Select(path => path.Replace("\\", "/")).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetKnownGameCpkCandidates(string cpkName)
    {
        var relative = Path.Combine("steamapps", "common", "INAZUMA ELEVEN Victory Road", "data", "packs", cpkName);
        var roots = new List<string>();

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            roots.Add(Path.Combine(programFilesX86, "Steam"));
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            roots.Add(Path.Combine(programFiles, "Steam"));
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            roots.Add(Path.Combine(home, "Library", "Application Support", "Steam"));
            roots.Add(Path.Combine(home, ".steam", "steam"));
            roots.Add(Path.Combine(home, ".local", "share", "Steam"));
        }

        if (Directory.Exists("/Volumes"))
        {
            foreach (var volume in Directory.EnumerateDirectories("/Volumes"))
            {
                roots.Add(Path.Combine(volume, "Applications", "Steam"));
            }
        }

        foreach (var root in roots)
        {
            yield return Path.Combine(root, relative);
        }
    }

    private static bool IsAutoPackedAudioFile(string relativePath)
    {
        return relativePath.StartsWith("data/common/sound_asset/", StringComparison.OrdinalIgnoreCase) &&
               (relativePath.EndsWith(".acb", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".awb", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace("\\", "/");
    }

    private void LogIgnoredJunkFiles()
    {
        var ignored = Directory.EnumerateFiles(_dirToPack, "*", SearchOption.AllDirectories)
            .Where(CGeneralUtils.IsJunkFile)
            .Select(file => Path.GetRelativePath(_dirToPack, file).Replace("\\", "/"))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ignored.Count == 0)
        {
            return;
        }

        CLogger.LogInfo($"Ignoring {ignored.Count} junk file(s).");
        foreach (var file in ignored.Take(5))
        {
            CLogger.LogInfo($"[Ignore] {file}");
        }

        if (ignored.Count > 5)
        {
            CLogger.LogInfo($"[Ignore] ... {ignored.Count - 5} more");
        }
    }

    private sealed class AutoPackedAudio
    {
        public Dictionary<string, List<CpkFilePayload>> FilesByCpk { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string?> OriginalCpkPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> RedirectedPackNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CpkEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> SourceFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> CustomPackNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
