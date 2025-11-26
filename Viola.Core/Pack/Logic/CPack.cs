using System.Text;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.EncryptDecrypt.Logic.Utils;

namespace Viola.Core.Pack.Logic;

class CPack
{
    private const int ENTRY_SIZE = 28;
    private const int HEADER_SIZE = 8;
    private const uint LOOSE_FILE_FLAGS = 0xFFFFFFFF;
    private const uint LOOSE_FILE_CPK_ID = 0xFFFFFFFF;
    
    // Fixed footer magic bytes (do not zero; keep for validity in editors and game)
    private static readonly byte[] ENTRY_FOOTER_MAGIC = [0xD3, 0x46, 0xF3, 0x36, 0x05, 0x00, 0x01, 0xFF];

    private readonly CLaunchOptions _options;
    private readonly string _dirToPack;

    public CPack(CLaunchOptions options)
    {
        _options = options;
        // Normalize path separators and remove trailing slash using standard .NET API
        _dirToPack = Path.TrimEndingDirectorySeparator(_options.InputPath!.Replace("\\", "/"));
    }

    public void PackMod()
    {
        string cpkListInputPath = string.IsNullOrEmpty(_options.CpkListPath)
            ? $"{_dirToPack}/data/cpk_list.cfg.bin"
            : _options.CpkListPath;

        if (!File.Exists(cpkListInputPath))
        {
            CLogger.AddImportantInfo($"Can't find master config at: {cpkListInputPath}\n");
            return;
        }

        CLogger.LogInfo("Processing cpk_list.cfg.bin...\n");

        byte[] fileBytes = File.ReadAllBytes(cpkListInputPath);

        // 1. Decrypt if needed
        bool wasEncrypted = DecryptIfNeeded(fileBytes);

        // 2. Parse Header
        uint entryCount = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(0, 4));
        uint stringPoolOffset = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(4, 4));

        CLogger.LogInfo($"Entries: {entryCount}, Pool Offset: {stringPoolOffset:X}. Parsing...\n");

        // 3. Prepare Table Data (Handle overlap/truncation)
        List<byte> tableDataList = FixTableOverlap(fileBytes, (int)entryCount, (int)stringPoolOffset);

        // 4. Prepare String Pool (Copy original bytes perfectly first)
        List<byte> masterStringPool = CopyOriginalStringPool(fileBytes, (int)stringPoolOffset);

        // Setup optimization structures
        Dictionary<uint, string> stringCache = new Dictionary<uint, string>();
        
        // Local function for optimized string reading
        string GetStringFast(uint offset)
        {
            if (offset >= (uint)masterStringPool.Count) return "";
            if (stringCache.TryGetValue(offset, out string? cached)) return cached;

            var poolSpan = CollectionsMarshal.AsSpan(masterStringPool);
            int length = 0;
            // Find null terminator
            while ((int)offset + length < poolSpan.Length && poolSpan[(int)offset + length] != 0) length++;

            string res = Encoding.UTF8.GetString(poolSpan.Slice((int)offset, length));
            stringCache[offset] = res;
            return res;
        }

        uint AddString(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            uint offset = (uint)masterStringPool.Count;
            masterStringPool.AddRange(Encoding.UTF8.GetBytes(s));
            masterStringPool.Add(0); 
            return offset;
        }

        uint emptyStringOffset = (masterStringPool.Count > 0 && masterStringPool[0] == 0) ? 0 : AddString("");

        // 5. Identify Files to Pack
        var modFileMap = GetModFilesMap();
        HashSet<string> processedFiles = new HashSet<string>();

        // PASS 1: UPDATE EXISTING ENTRIES
        Span<byte> tableSpan = CollectionsMarshal.AsSpan(tableDataList);

        for (int i = 0; i < (int)entryCount; i++)
        {
            int baseAddr = i * ENTRY_SIZE;
            if (baseAddr + ENTRY_SIZE > tableDataList.Count) break; // Safety check

            uint dirOffset = BinaryPrimitives.ReadUInt32LittleEndian(tableSpan.Slice(baseAddr, 4));
            uint nameOffset = BinaryPrimitives.ReadUInt32LittleEndian(tableSpan.Slice(baseAddr + 4, 4));
            
            string fullInternalPath = GetStringFast(dirOffset) + GetStringFast(nameOffset);

            if (modFileMap.TryGetValue(fullInternalPath, out string? localPath))
            {
                CLogger.LogInfo($"[Update] {fullInternalPath}\n");
                int size = (int)new FileInfo(localPath).Length;
                
                // Update Size
                BinaryPrimitives.WriteInt32LittleEndian(tableSpan.Slice(baseAddr + 16, 4), size);

                // Force Loose/Raw mode
                BinaryPrimitives.WriteUInt32LittleEndian(tableSpan.Slice(baseAddr + 8, 4), LOOSE_FILE_FLAGS);  // Flags
                BinaryPrimitives.WriteUInt32LittleEndian(tableSpan.Slice(baseAddr + 12, 4), LOOSE_FILE_CPK_ID); // CPK ID

                // Ensure valid footer if within bounds
                if (baseAddr + 20 < tableDataList.Count)
                { 
                    new ReadOnlySpan<byte>(ENTRY_FOOTER_MAGIC).CopyTo(tableSpan.Slice(baseAddr + 20, 8));
                }

                processedFiles.Add(localPath);
            }
        }

        // PASS 2: ADD NEW ENTRIES
        foreach (var kvp in modFileMap)
        {
            if (processedFiles.Contains(kvp.Value)) continue;

            CLogger.LogInfo($"[Add] {kvp.Key}\n");
            
            string fileName = Path.GetFileName(kvp.Key);
            string dirName = Path.GetDirectoryName(kvp.Key)?.Replace("\\", "/") ?? "";
            if (!string.IsNullOrEmpty(dirName) && !dirName.EndsWith("/")) dirName += "/";

            uint dirOff = AddString(dirName);
            uint nameOff = AddString(fileName);
            int size = (int)new FileInfo(kvp.Value).Length;

            byte[] entryBuffer = CreateEntryBuffer(dirOff, nameOff, size);
            tableDataList.AddRange(entryBuffer);
            
            entryCount++;
        }

        // 6. Write Output
        WriteOutput(entryCount, tableDataList, masterStringPool, wasEncrypted);

        // 7. Copy Files
        CopyFilesParallel(modFileMap);
    }

    private bool DecryptIfNeeded(byte[] fileBytes)
    {
        uint checkHeader = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes);
        if (checkHeader > 10000000)
        {
            CLogger.LogInfo("Decrypting config...\n");
            CCriwareCrypt.DecryptBlock(fileBytes, 0, 0x1717E18E);
            return true;
        }
        return false;
    }

    private List<byte> FixTableOverlap(byte[] fileBytes, int entryCount, int stringPoolOffset)
    {
        int expectedTableSize = entryCount * ENTRY_SIZE;
        int actualTableSpace = stringPoolOffset - HEADER_SIZE;
        
        List<byte> tableDataList = new List<byte>(expectedTableSize + 1024);

        if (actualTableSpace < expectedTableSize)
        {
            CLogger.LogInfo($"[Fix] Detected overlap! Table expects {expectedTableSize} bytes but only {actualTableSpace} available. Preserving original data and aligning pool.\n");
            
            // Copy only valid table data up to the pool start
            tableDataList.AddRange(new ReadOnlySpan<byte>(fileBytes, HEADER_SIZE, actualTableSpace));
            
            // Pad missing bytes (unless exact overlap of footer where pool starts immediately)
            int missingBytes = expectedTableSize - actualTableSpace;
            if (missingBytes > 0 && missingBytes != 8)
            {
                tableDataList.AddRange(new byte[missingBytes]);
            }
        }
        else
        {
            // Normal read
            tableDataList.AddRange(new ReadOnlySpan<byte>(fileBytes, HEADER_SIZE, expectedTableSize));
        }
        return tableDataList;
    }

    private List<byte> CopyOriginalStringPool(byte[] fileBytes, int stringPoolOffset)
    {
        // Use actual available pool space from the file to avoid offset shifts
        // Note: We calculate start based on the *file's* pointer, not our reconstructed table
        int originalPoolStart = stringPoolOffset; // Use file's offset directly
        
        // Re-calculate based on fix logic if needed, but code used:
        // int originalPoolStart = HEADER_SIZE + actualTableSpace; 
        // Since actualTableSpace = stringPoolOffset - HEADER_SIZE, this simplifies to stringPoolOffset.
        
        int originalPoolSize = fileBytes.Length - originalPoolStart;
        byte[] originalStringPool = new byte[originalPoolSize];
        Array.Copy(fileBytes, originalPoolStart, originalStringPool, 0, originalPoolSize);
        
        return new List<byte>(originalStringPool);
    }

    private Dictionary<string, string> GetModFilesMap()
    {
        var modFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allFiles = CGeneralUtils.GetAllFilesWithNormalSlash(_dirToPack);

        foreach (var f in allFiles)
        {
            string normalized = f.Replace("\\", "/");
            if (normalized.EndsWith("data/cpk_list.cfg.bin")) continue;
            
            string internalPath = Path.GetRelativePath(_dirToPack, f).Replace("\\", "/");
            modFileMap[internalPath] = normalized;
        }
        return modFileMap;
    }

    private byte[] CreateEntryBuffer(uint dirOff, uint nameOff, int size)
    {
        byte[] buffer = new byte[ENTRY_SIZE];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), dirOff);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), nameOff);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), LOOSE_FILE_FLAGS);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), LOOSE_FILE_CPK_ID);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(16, 4), size);
        
        new ReadOnlySpan<byte>(ENTRY_FOOTER_MAGIC).CopyTo(span.Slice(20, 8));
        
        return buffer;
    }

    private void WriteOutput(uint entryCount, List<byte> tableData, List<byte> stringPool, bool encrypt)
    {
        string outputModFolder = _options.OutputPath;
        string outputConfigPath = (_options.PackPlatform == DataClasses.Platform.SWITCH)
            ? $"{outputModFolder}/romfs/data/cpk_list.cfg.bin"
            : $"{outputModFolder}/data/cpk_list.cfg.bin";

        Directory.CreateDirectory(Path.GetDirectoryName(outputConfigPath)!);

        using (var fs = new FileStream(outputConfigPath, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(entryCount);
            
            // Pool offset: HEADER + Full Table Size
            uint newStringPoolOffset = (uint)(HEADER_SIZE + tableData.Count);
            bw.Write(newStringPoolOffset);
            
            bw.Write(CollectionsMarshal.AsSpan(tableData));
            bw.Write(CollectionsMarshal.AsSpan(stringPool));
        }

        if (encrypt)
        {
            CLogger.LogInfo("Re-encrypting config...\n");
            byte[] finalBytes = File.ReadAllBytes(outputConfigPath);
            CCriwareCrypt.DecryptBlock(finalBytes, 0, 0x1717E18E);
            File.WriteAllBytes(outputConfigPath, finalBytes);
        }
        
        CLogger.LogInfo($"Done packing to `{outputModFolder}`\n");
    }

    private void CopyFilesParallel(Dictionary<string, string> modFileMap)
    {
        CLogger.LogInfo("Copying files...\n");
        var filesToCopy = modFileMap.Values.Where(f => !f.EndsWith("data/cpk_list.cfg.bin")).ToList();
        
        Parallel.ForEach(filesToCopy, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
        {
            string destPath = (_options.PackPlatform == DataClasses.Platform.SWITCH)
                ? Path.Combine(_options.OutputPath, "romfs", Path.GetRelativePath(_dirToPack, file))
                : Path.Combine(_options.OutputPath, Path.GetRelativePath(_dirToPack, file));

            string destDir = Path.GetDirectoryName(destPath)!;
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            File.Copy(file, destPath, true);
        });
    }
}