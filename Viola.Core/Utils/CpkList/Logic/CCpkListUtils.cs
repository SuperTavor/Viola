using System.Security.Cryptography;
using System.Text;
using Tinifan.Level5.Binary;
using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Utils.General.Logic;

namespace Viola.Core.Utils.CpkList.Logic;

public sealed class CpkListEntry
{
    public string FullPath { get; init; } = string.Empty;
    public string CpkPath { get; init; } = string.Empty;
    public int Size { get; init; }
}

public static class CCpkListUtils
{
    private const uint LegacyKey = 0x1717E18E;
    private const uint ModernKeyScrambleKey = 0x8A90ABA9;
    private const uint ModernIvScrambleKey = 0x4C801618;
    private const uint T2bFooterMagic = 0x62327401;

    private static readonly byte[] ModernEncryptedKey =
    [
        0x21, 0xCB, 0xC9, 0x72, 0xF9, 0xF2, 0x8B, 0x17,
        0x9D, 0xE2, 0x50, 0x64, 0xD1, 0x8C, 0xA9, 0x4D,
        0x53, 0x8D, 0x90, 0x1E, 0x96, 0xF6, 0x0D, 0x75,
        0x7A, 0xA8, 0xD9, 0x43, 0x42, 0xE2, 0x4F, 0x58
    ];

    private static readonly byte[] ModernEncryptedIv =
    [
        0x6D, 0x4B, 0x8E, 0x3F, 0x2F, 0x49, 0xC9, 0xF0,
        0x9D, 0xE6, 0x44, 0x38, 0xE3, 0x1E, 0xCB, 0xB0
    ];

    public static IEnumerable<byte[]> GetReadableCandidates(byte[] bytes)
    {
        if (TryDecryptModern(bytes, out var modernBytes))
        {
            yield return modernBytes;
        }

        yield return bytes;

        var legacyBytes = (byte[])bytes.Clone();
        CCriwareCrypt.DecryptBlock(legacyBytes, 0, LegacyKey);
        yield return legacyBytes;
    }

    public static bool TryReadEntries(byte[] bytes, out List<CpkListEntry> entries)
    {
        entries = new List<CpkListEntry>();
        foreach (var candidate in GetReadableCandidates(bytes))
        {
            if (TryReadT2bEntries(candidate, out entries) || TryReadCfgBinEntries(candidate, out entries))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetCfgBinPayload(byte[] bytes, out byte[] payload, out bool wasLegacyEncrypted)
    {
        wasLegacyEncrypted = false;

        foreach (var candidate in GetReadableCandidates(bytes))
        {
            if (TryOpenCfgBin(candidate))
            {
                payload = candidate;
                wasLegacyEncrypted = !ReferenceEquals(candidate, bytes);
                return true;
            }
        }

        payload = Array.Empty<byte>();
        return false;
    }

    public static bool IsModernCpkList(byte[] bytes)
    {
        return TryDecryptModern(bytes, out var decrypted) && IsT2b(decrypted);
    }

    public static bool TryPackModernCpkList(
        byte[] bytes,
        IReadOnlyList<string> localFiles,
        string rootPath,
        out byte[] encryptedBytes,
        Action<string>? log = null,
        Action<int, int>? progress = null,
        IReadOnlySet<string>? additionalCustomPacks = null,
        IReadOnlyDictionary<string, string>? redirectedPacks = null,
        IReadOnlyDictionary<string, string>? additionalCpkEntries = null)
    {
        encryptedBytes = Array.Empty<byte>();
        if (!TryDecryptModern(bytes, out var decrypted) || !TryReadT2bFile(decrypted, out var file))
        {
            return false;
        }

        var existingFileMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < file.Entries.Count; i++)
        {
            if (!IsCpkListEntry(file.Entries[i]))
            {
                continue;
            }

            string fullPath = ReadT2bString(file.StringData, file.Entries[i].Values[0]) +
                              ReadT2bString(file.StringData, file.Entries[i].Values[1]);
            existingFileMap[fullPath] = i;
        }

        var templateIndex = file.Entries.FindLastIndex(IsCpkListEntry);
        if (templateIndex < 0)
        {
            return false;
        }

        var customPacks = FindCustomPacks(localFiles, rootPath);
        if (additionalCustomPacks is not null)
        {
            customPacks.UnionWith(additionalCustomPacks);
        }
        int processedCount = 0;
        int totalFiles = localFiles.Count;

        foreach (var localFile in localFiles)
        {
            processedCount++;
            progress?.Invoke(processedCount, totalFiles);

            if (localFile.EndsWith("cpk_list.cfg.bin", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(rootPath, localFile).Replace("\\", "/");
            if (IsCustomPack(relativePath))
            {
                continue;
            }

            int size = (int)new FileInfo(localFile).Length;

            if (existingFileMap.TryGetValue(relativePath, out int entryIndex))
            {
                log?.Invoke($"[Update] {relativePath}");
                var entry = file.Entries[entryIndex];
                entry.PendingDir = ReadT2bString(file.StringData, entry.Values[0]);
                entry.PendingName = ReadT2bString(file.StringData, entry.Values[1]);
                if (additionalCpkEntries is not null && additionalCpkEntries.TryGetValue(relativePath, out var customCpkPath))
                {
                    SetPendingCpkPath(entry, customCpkPath);
                }
                else
                {
                    var cpkName = ReadT2bString(file.StringData, entry.Values[3]);
                    if (redirectedPacks is not null && redirectedPacks.TryGetValue(cpkName, out var redirectedCpkName))
                    {
                        entry.PendingCpkDir = "data/packs/";
                        entry.PendingCpkName = redirectedCpkName;
                    }
                    else if (customPacks.Contains(cpkName))
                    {
                        entry.PendingCpkDir = "data/packs_custom/";
                        entry.PendingCpkName = cpkName;
                    }
                    else
                    {
                        entry.PendingCpkDir = string.Empty;
                        entry.PendingCpkName = string.Empty;
                    }
                }
                entry.Values[4] = size;
            }
            else
            {
                log?.Invoke($"[Add] {relativePath}");

                string fileName = Path.GetFileName(relativePath);
                string dirName = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? string.Empty;
                if (!string.IsNullOrEmpty(dirName) && !dirName.EndsWith("/"))
                {
                    dirName += "/";
                }

                var newEntry = file.Entries[templateIndex].Clone();
                newEntry.PendingDir = dirName;
                newEntry.PendingName = fileName;
                if (additionalCpkEntries is not null && additionalCpkEntries.TryGetValue(relativePath, out var customCpkPath))
                {
                    SetPendingCpkPath(newEntry, customCpkPath);
                }
                else
                {
                    newEntry.PendingCpkDir = string.Empty;
                    newEntry.PendingCpkName = string.Empty;
                }
                newEntry.Values[4] = size;

                file.Entries.Add(newEntry);
                existingFileMap[relativePath] = file.Entries.Count - 1;
            }
        }

        SortT2bCpkListEntriesByIndex(file);
        UpdateT2bCountEntry(file);
        var packedBytes = WriteT2bFile(file);
        encryptedBytes = EncryptModern(packedBytes);
        return encryptedBytes.Length > 0;
    }

    private static bool TryDecryptModern(byte[] bytes, out byte[] decrypted)
    {
        decrypted = Array.Empty<byte>();
        if (bytes.Length == 0 || bytes.Length % 16 != 0)
        {
            return false;
        }

        try
        {
            var key = (byte[])ModernEncryptedKey.Clone();
            var iv = (byte[])ModernEncryptedIv.Clone();
            CCriwareCrypt.DecryptBlock(key, 0, ModernKeyScrambleKey);
            CCriwareCrypt.DecryptBlock(iv, 0, ModernIvScrambleKey);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return decrypted.Length > 0;
        }
        catch (CryptographicException)
        {
            decrypted = Array.Empty<byte>();
            return false;
        }
    }

    private static byte[] EncryptModern(byte[] bytes)
    {
        var key = (byte[])ModernEncryptedKey.Clone();
        var iv = (byte[])ModernEncryptedIv.Clone();
        CCriwareCrypt.DecryptBlock(key, 0, ModernKeyScrambleKey);
        CCriwareCrypt.DecryptBlock(iv, 0, ModernIvScrambleKey);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    private static bool TryReadCfgBinEntries(byte[] bytes, out List<CpkListEntry> entries)
    {
        entries = new List<CpkListEntry>();

        try
        {
            CfgBin cpkList = new CfgBin();
            cpkList.Open(bytes);
            if (cpkList.Entries.Count == 0 || cpkList.Entries[0].Children == null)
            {
                return false;
            }

            foreach (var item in cpkList.Entries[0].Children)
            {
                if (item.Variables.Count == CGeneralUtils.OLD_CPKLIST_CNT)
                {
                    entries.Add(new CpkListEntry
                    {
                        FullPath = Convert.ToString(item.Variables[0].Value) ?? string.Empty,
                        CpkPath = Convert.ToString(item.Variables[1].Value) ?? string.Empty,
                        Size = Convert.ToInt32(item.Variables[2].Value)
                    });
                }
                else if (item.Variables.Count == CGeneralUtils.NEW_CPKLIST_CNT)
                {
                    string dir = Convert.ToString(item.Variables[0].Value) ?? string.Empty;
                    string name = Convert.ToString(item.Variables[1].Value) ?? string.Empty;
                    string cpkDir = Convert.ToString(item.Variables[2].Value) ?? string.Empty;
                    string cpkName = Convert.ToString(item.Variables[3].Value) ?? string.Empty;
                    int size = Convert.ToInt32(item.Variables[4].Value);

                    entries.Add(new CpkListEntry
                    {
                        FullPath = dir + name,
                        CpkPath = BuildCpkPath(cpkDir, cpkName),
                        Size = size
                    });
                }
                else
                {
                    entries.Clear();
                    return false;
                }
            }

            return entries.Count > 0;
        }
        catch
        {
            entries.Clear();
            return false;
        }
    }

    private static bool TryOpenCfgBin(byte[] bytes)
    {
        try
        {
            CfgBin cpkList = new CfgBin();
            cpkList.Open(bytes);
            return cpkList.Entries.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadT2bEntries(byte[] bytes, out List<CpkListEntry> entries)
    {
        entries = new List<CpkListEntry>();
        if (!TryReadT2bFile(bytes, out var file))
        {
            return false;
        }

        try
        {
            foreach (var entry in file.Entries)
            {
                if (!IsCpkListEntry(entry))
                {
                    continue;
                }

                string dir = ReadT2bString(file.StringData, entry.Values[0]);
                string name = ReadT2bString(file.StringData, entry.Values[1]);
                string cpkDir = ReadT2bString(file.StringData, entry.Values[2]);
                string cpkName = ReadT2bString(file.StringData, entry.Values[3]);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
                {
                    continue;
                }

                entries.Add(new CpkListEntry
                {
                    FullPath = dir + name,
                    CpkPath = BuildCpkPath(cpkDir, cpkName),
                    Size = entry.Values[4]
                });
            }

            return entries.Count > 0;
        }
        catch
        {
            entries.Clear();
            return false;
        }
    }

    private static bool TryReadT2bFile(byte[] bytes, out T2bFile file)
    {
        file = new T2bFile();
        if (!IsT2b(bytes))
        {
            return false;
        }

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            uint entryCount = reader.ReadUInt32();
            uint stringOffset = reader.ReadUInt32();
            uint stringSize = reader.ReadUInt32();
            uint stringCount = reader.ReadUInt32();

            if (stringOffset > bytes.Length || stringSize > bytes.Length - stringOffset)
            {
                return false;
            }

            var entries = new List<T2bRawEntry>(checked((int)entryCount));
            for (uint index = 0; index < entryCount; index++)
            {
                if (stream.Position >= stringOffset)
                {
                    return false;
                }

                uint crc32 = reader.ReadUInt32();
                int valueCount = reader.ReadByte();
                var types = ReadT2bTypes(reader, valueCount);
                var values = new int[valueCount];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = reader.ReadInt32();
                }

                entries.Add(new T2bRawEntry(crc32, types, values));
            }

            long tailOffset = AlignValue(stringOffset + stringSize, 16);
            if (tailOffset > bytes.Length)
            {
                return false;
            }

            file = new T2bFile
            {
                Entries = entries,
                StringData = bytes[(int)stringOffset..(int)(stringOffset + stringSize)],
                StringCount = stringCount,
                Tail = bytes[(int)tailOffset..]
            };

            return true;
        }
        catch
        {
            file = new T2bFile();
            return false;
        }
    }

    private static byte[] WriteT2bFile(T2bFile file)
    {
        var strings = new MemoryStream();
        var stringOffsets = new Dictionary<string, int>(StringComparer.Ordinal);

        int AddString(string value)
        {
            if (stringOffsets.TryGetValue(value, out int offset))
            {
                return offset;
            }

            offset = checked((int)strings.Position);
            var encoded = Encoding.UTF8.GetBytes(value);
            strings.Write(encoded, 0, encoded.Length);
            strings.WriteByte(0);
            stringOffsets[value] = offset;
            return offset;
        }

        foreach (var entry in file.Entries)
        {
            for (int i = 0; i < entry.Types.Length; i++)
            {
                if (entry.Types[i] != 0)
                {
                    continue;
                }

                string value = entry.GetPendingString(i) ?? ReadT2bString(file.StringData, entry.Values[i]);
                entry.Values[i] = AddString(value);
            }
        }

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);

        writer.Write((uint)file.Entries.Count);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write((uint)stringOffsets.Count);

        foreach (var entry in file.Entries)
        {
            writer.Write(entry.Crc32);
            writer.Write((byte)entry.Types.Length);
            WriteT2bTypes(writer, entry.Types);
            Align(output, 4);
            foreach (int value in entry.Values)
            {
                writer.Write(value);
            }
        }

        uint stringOffset = checked((uint)output.Position);
        var stringData = strings.ToArray();
        writer.Write(stringData);
        Align(output, 16);
        writer.Write(file.Tail);

        output.Position = 4;
        writer.Write(stringOffset);
        writer.Write((uint)stringData.Length);
        writer.Write((uint)stringOffsets.Count);

        return output.ToArray();
    }

    private static bool IsCpkListEntry(T2bRawEntry entry)
    {
        return entry.Values.Length >= 5 &&
               entry.Types.Length >= 5 &&
               entry.Types[0] == 0 &&
               entry.Types[1] == 0 &&
               entry.Types[2] == 0 &&
               entry.Types[3] == 0 &&
               entry.Types[4] == 1;
    }

    private static void UpdateT2bCountEntry(T2bFile file)
    {
        if (file.Entries.Count == 0)
        {
            return;
        }

        var firstEntry = file.Entries[0];
        if (firstEntry.Values.Length == 1 && firstEntry.Types.Length == 1 && firstEntry.Types[0] == 1)
        {
            firstEntry.Values[0] = file.Entries.Count(entry => IsCpkListEntry(entry));
        }
    }

    internal static uint GetCpkListEntryIndex(string fullPath)
    {
        return CGeneralUtils.ComputeCRC32(fullPath);
    }

    private static void SortT2bCpkListEntriesByIndex(T2bFile file)
    {
        var fileEntryIndexes = new List<int>();
        var sortedFileEntries = new List<T2bRawEntry>();

        for (int index = 0; index < file.Entries.Count; index++)
        {
            var entry = file.Entries[index];
            if (!IsCpkListEntry(entry))
            {
                continue;
            }

            fileEntryIndexes.Add(index);
            sortedFileEntries.Add(entry);
        }

        sortedFileEntries = sortedFileEntries
            .OrderBy(entry => GetCpkListEntryIndex(GetT2bFullPath(file, entry)))
            .ToList();

        for (int index = 0; index < fileEntryIndexes.Count; index++)
        {
            file.Entries[fileEntryIndexes[index]] = sortedFileEntries[index];
        }
    }

    private static string GetT2bFullPath(T2bFile file, T2bRawEntry entry)
    {
        string dir = entry.PendingDir ?? ReadT2bString(file.StringData, entry.Values[0]);
        string name = entry.PendingName ?? ReadT2bString(file.StringData, entry.Values[1]);
        return dir + name;
    }

    private static bool IsT2b(byte[] bytes)
    {
        return bytes.Length >= 0x10 && BitConverter.ToUInt32(bytes, bytes.Length - 0x10) == T2bFooterMagic;
    }

    private static byte[] ReadT2bTypes(BinaryReader reader, int count)
    {
        var types = new byte[count];
        for (int index = 0; index < count; index += 4)
        {
            byte chunk = reader.ReadByte();
            for (int nibble = 0; nibble < 4 && index + nibble < count; nibble++)
            {
                types[index + nibble] = (byte)((chunk >> (nibble * 2)) & 0x3);
            }
        }

        Align(reader.BaseStream, 4);
        return types;
    }

    private static void WriteT2bTypes(BinaryWriter writer, byte[] types)
    {
        for (int index = 0; index < types.Length; index += 4)
        {
            byte chunk = 0;
            for (int nibble = 0; nibble < 4 && index + nibble < types.Length; nibble++)
            {
                chunk |= (byte)((types[index + nibble] & 0x3) << (nibble * 2));
            }

            writer.Write(chunk);
        }
    }

    private static string ReadT2bString(byte[] stringData, int offset)
    {
        if (offset < 0 || offset >= stringData.Length)
        {
            return string.Empty;
        }

        int endOffset = offset;
        while (endOffset < stringData.Length && stringData[endOffset] != 0)
        {
            endOffset++;
        }

        return Encoding.UTF8.GetString(stringData[offset..endOffset]);
    }

    private static void Align(Stream stream, int alignment)
    {
        long aligned = AlignValue(stream.Position, alignment);
        if (!stream.CanWrite)
        {
            stream.Position = aligned;
            return;
        }

        while (stream.Position < aligned)
        {
            stream.WriteByte(0);
        }
    }

    private static long AlignValue(long value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private static string BuildCpkPath(string cpkDir, string cpkName)
    {
        if (string.IsNullOrEmpty(cpkName))
        {
            return string.Empty;
        }

        string cpkPath = Path.Combine(cpkDir, cpkName).Replace("\\", "/");
        return cpkPath.StartsWith("/") ? cpkPath[1..] : cpkPath;
    }

    private static void SetPendingCpkPath(T2bRawEntry entry, string cpkPath)
    {
        var normalizedPath = cpkPath.Replace('\\', '/');
        entry.PendingCpkDir = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? string.Empty;
        if (!string.IsNullOrEmpty(entry.PendingCpkDir) && !entry.PendingCpkDir.EndsWith('/'))
        {
            entry.PendingCpkDir += "/";
        }
        entry.PendingCpkName = Path.GetFileName(normalizedPath);
    }

    private static bool IsCustomPack(string relativePath)
    {
        return relativePath.StartsWith("data/packs_custom/", StringComparison.OrdinalIgnoreCase) &&
               relativePath.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> FindCustomPacks(IReadOnlyList<string> localFiles, string rootPath)
    {
        var packs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var localFile in localFiles)
        {
            string relativePath = Path.GetRelativePath(rootPath, localFile).Replace("\\", "/");
            if (IsCustomPack(relativePath))
            {
                packs.Add(Path.GetFileName(relativePath));
            }
        }

        return packs;
    }

    private sealed class T2bFile
    {
        public List<T2bRawEntry> Entries { get; init; } = new();
        public byte[] StringData { get; init; } = Array.Empty<byte>();
        public uint StringCount { get; init; }
        public byte[] Tail { get; init; } = Array.Empty<byte>();
    }

    private sealed class T2bRawEntry
    {
        public T2bRawEntry(uint crc32, byte[] types, int[] values)
        {
            Crc32 = crc32;
            Types = types;
            Values = values;
        }

        public uint Crc32 { get; }
        public byte[] Types { get; }
        public int[] Values { get; }
        public string? PendingDir { get; set; }
        public string? PendingName { get; set; }
        public string? PendingCpkDir { get; set; }
        public string? PendingCpkName { get; set; }

        public T2bRawEntry Clone()
        {
            return new T2bRawEntry(Crc32, (byte[])Types.Clone(), (int[])Values.Clone());
        }

        public string? GetPendingString(int index)
        {
            return index switch
            {
                0 => PendingDir,
                1 => PendingName,
                2 => PendingCpkDir,
                3 => PendingCpkName,
                _ => null
            };
        }
    }
}
