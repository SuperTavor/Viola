using System.Buffers.Binary;
using System.Text;

namespace Viola.Core.Utils.Cpk.Logic;

internal sealed record CpkFilePayload(string RelativePath, string SourcePath, bool IsNewFile);

internal static class CCriCpkWriter
{
    private const int Alignment = 0x800;
    private const ulong NoOffset = 0xFFFFFFFFFFFFFFFF;

    public static void Write(string outputPath, IReadOnlyList<CpkFilePayload> files)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        DeleteExistingOutput(outputPath);

        var orderedFiles = files
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var contentOffset = (long)Alignment;
        var offsets = new List<long>(orderedFiles.Count);
        var cursor = contentOffset;

        foreach (var file in orderedFiles)
        {
            cursor = Align(cursor, Alignment);
            offsets.Add(cursor);
            cursor += new FileInfo(file.SourcePath).Length;
        }
        var tocOffset = Align(cursor, Alignment);

        var cpkTable = UtfTable.Create("CpkHeader")
            .AddPerRow("UpdateDateTime", UtfType.Int64)
            .AddZero("FileSize", UtfType.Int64)
            .AddPerRow("ContentOffset", UtfType.Int64)
            .AddPerRow("ContentSize", UtfType.Int64)
            .AddPerRow("TocOffset", UtfType.Int64)
            .AddPerRow("TocSize", UtfType.Int64)
            .AddZero("TocCrc", UtfType.Int32)
            .AddPerRow("EtocOffset", UtfType.Int64)
            .AddPerRow("EtocSize", UtfType.Int64)
            .AddZero("ItocOffset", UtfType.Int64)
            .AddZero("ItocSize", UtfType.Int64)
            .AddZero("ItocCrc", UtfType.Int32)
            .AddZero("GtocOffset", UtfType.Int64)
            .AddZero("GtocSize", UtfType.Int64)
            .AddZero("GtocCrc", UtfType.Int32)
            .AddPerRow("EnabledPackedSize", UtfType.Int64)
            .AddPerRow("EnabledDataSize", UtfType.Int64)
            .AddZero("TotalDataSize", UtfType.Int64)
            .AddZero("Tocs", UtfType.Int32)
            .AddPerRow("Files", UtfType.Int32)
            .AddPerRow("Groups", UtfType.Int32)
            .AddPerRow("Attrs", UtfType.Int32)
            .AddZero("TotalFiles", UtfType.Int32)
            .AddZero("Directories", UtfType.Int32)
            .AddZero("Updates", UtfType.Int32)
            .AddPerRow("Version", UtfType.Int16)
            .AddPerRow("Revision", UtfType.Int16)
            .AddPerRow("Align", UtfType.Int16)
            .AddPerRow("Sorted", UtfType.Int16)
            .AddZero("EID", UtfType.Int16)
            .AddPerRow("CpkMode", UtfType.Int32)
            .AddPerRow("Tvers", UtfType.String)
            .AddZero("Comment", UtfType.String)
            .AddPerRow("Codec", UtfType.Int32)
            .AddPerRow("DpkItoc", UtfType.Int32);

        var tocTable = UtfTable.Create("CpkTocInfo");
        var commonDir = TryGetCommonDirectory(orderedFiles);
        if (commonDir != null)
        {
            tocTable.AddConst("DirName", UtfType.String, commonDir);
        }
        else
        {
            tocTable.AddPerRow("DirName", UtfType.String);
        }

        tocTable
            .AddPerRow("FileName", UtfType.String)
            .AddPerRow("FileSize", UtfType.Int32)
            .AddPerRow("ExtractSize", UtfType.Int32)
            .AddPerRow("FileOffset", UtfType.Int64)
            .AddPerRow("ID", UtfType.Int32)
            .AddConst("UserString", UtfType.String, "<NULL>");

        for (var i = 0; i < orderedFiles.Count; i++)
        {
            var file = orderedFiles[i];
            var relative = file.RelativePath.Replace("\\", "/");
            var dirName = Path.GetDirectoryName(relative)?.Replace("\\", "/") ?? string.Empty;
            var fileName = Path.GetFileName(relative);
            var size = (ulong)new FileInfo(file.SourcePath).Length;

            if (commonDir != null)
            {
                tocTable.AddRow(fileName, (uint)size, (uint)size, (ulong)(offsets[i] - contentOffset), (uint)i);
            }
            else
            {
                tocTable.AddRow(dirName, fileName, (uint)size, (uint)size, (ulong)(offsets[i] - contentOffset), (uint)i);
            }
        }

        var etocTable = UtfTable.Create("CpkEtocInfo")
            .AddInitialString("<NULL>")
            .AddPerRow("UpdateDateTime", UtfType.Int64)
            .AddPerRow("LocalDir", UtfType.String);

        foreach (var file in orderedFiles)
        {
            var relative = file.RelativePath.Replace("\\", "/");
            var dirName = Path.GetDirectoryName(relative)?.Replace("\\", "/") ?? string.Empty;
            etocTable.AddRow(570276038271829760UL, dirName);
        }
        etocTable.AddRow(0UL, string.Empty);

        var tocPacket = WritePacket("TOC ", tocTable.Build());
        var etocPacket = WritePacket("ETOC", etocTable.Build());
        var etocOffset = Align(tocOffset + tocPacket.Length, Alignment);
        var finalSize = etocOffset + etocPacket.Length;
        var contentSize = tocOffset - contentOffset;
        var enabledSize = (ulong)orderedFiles.Sum(file => new FileInfo(file.SourcePath).Length);

        cpkTable.AddRow(
            1UL,
            (ulong)contentOffset,
            (ulong)contentSize,
            (ulong)tocOffset,
            (ulong)tocPacket.Length,
            (ulong)etocOffset,
            (ulong)etocPacket.Length,
            enabledSize,
            enabledSize,
            (uint)orderedFiles.Count,
            0U,
            0U,
            (ushort)7,
            (ushort)2,
            (ushort)Alignment,
            (ushort)1,
            1U,
            "N/A, DLL3.11.05",
            0U,
            0U);

        var cpkPacket = WritePacket("CPK ", cpkTable.Build());

        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        output.Write(cpkPacket);
        PadTo(output, Alignment);
        output.Position = contentOffset;

        for (var i = 0; i < orderedFiles.Count; i++)
        {
            output.Position = offsets[i];
            using var input = File.OpenRead(orderedFiles[i].SourcePath);
            input.CopyTo(output);
        }

        output.Position = tocOffset;
        output.Write(tocPacket);
        output.Position = etocOffset;
        output.Write(etocPacket);
    }

    private static byte[] WritePacket(string magic, byte[] utf)
    {
        var packet = new byte[0x10 + utf.Length];
        Encoding.ASCII.GetBytes(magic, packet);
        packet[4] = 0xFF;
        BinaryPrimitives.WriteUInt64LittleEndian(packet.AsSpan(8), (ulong)utf.Length);
        utf.CopyTo(packet.AsSpan(0x10));
        return packet;
    }

    private static long Align(long value, int alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }

    private static string? TryGetCommonDirectory(IReadOnlyList<CpkFilePayload> files)
    {
        string? commonDir = null;
        foreach (var file in files)
        {
            var relative = file.RelativePath.Replace("\\", "/");
            var dirName = Path.GetDirectoryName(relative)?.Replace("\\", "/") ?? string.Empty;
            if (commonDir == null)
            {
                commonDir = dirName;
                continue;
            }

            if (!commonDir.Equals(dirName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return commonDir;
    }

    private static void DeleteExistingOutput(string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            return;
        }

        File.SetAttributes(outputPath, FileAttributes.Normal);
        File.Delete(outputPath);
    }

    private static void PadTo(Stream stream, int alignment)
    {
        var aligned = Align(stream.Position, alignment);
        while (stream.Position < aligned)
        {
            stream.WriteByte(0);
        }
    }

    private enum UtfStorage : byte
    {
        Zero = 0x10,
        Const = 0x30,
        PerRow = 0x50
    }

    private enum UtfType : byte
    {
        Int16 = 0x02,
        UInt16 = 0x03,
        Int32 = 0x04,
        UInt32 = 0x05,
        Int64 = 0x06,
        UInt64 = 0x07,
        String = 0x0A
    }

    private sealed class UtfColumn
    {
        public string Name { get; init; } = string.Empty;
        public UtfStorage Storage { get; init; }
        public UtfType Type { get; init; }
        public object? ConstValue { get; init; }
    }

    private sealed class UtfTable
    {
        private readonly string _name;
        private readonly List<UtfColumn> _columns = new();
        private readonly List<object?[]> _rows = new();
        private readonly List<string> _initialStrings = new();

        private UtfTable(string name)
        {
            _name = name;
        }

        public static UtfTable Create(string name) => new(name);

        public UtfTable AddInitialString(string value)
        {
            _initialStrings.Add(value);
            return this;
        }

        public UtfTable AddConst(string name, UtfType type, object value)
        {
            _columns.Add(new UtfColumn { Name = name, Type = type, Storage = UtfStorage.Const, ConstValue = value });
            return this;
        }

        public UtfTable AddZero(string name, UtfType type)
        {
            _columns.Add(new UtfColumn { Name = name, Type = type, Storage = UtfStorage.Zero });
            return this;
        }

        public UtfTable AddPerRow(string name, UtfType type)
        {
            _columns.Add(new UtfColumn { Name = name, Type = type, Storage = UtfStorage.PerRow });
            return this;
        }

        public void AddRow(params object?[] values)
        {
            _rows.Add(values);
        }

        public byte[] Build()
        {
            var rowLength = _columns.Where(column => column.Storage == UtfStorage.PerRow).Sum(column => TypeSize(column.Type));
            var columnBytes = new MemoryStream();
            var rowBytes = new MemoryStream();
            var strings = new StringTable();
            foreach (var value in _initialStrings)
            {
                strings.Add(value);
            }
            strings.Add(_name);

            foreach (var column in _columns)
            {
                columnBytes.WriteByte((byte)((byte)column.Storage | (byte)column.Type));
                WriteUInt32(columnBytes, strings.Add(column.Name));
                if (column.Storage == UtfStorage.Const)
                {
                    WriteValue(columnBytes, column.Type, column.ConstValue, strings);
                }
            }

            foreach (var row in _rows.DefaultIfEmpty(Array.Empty<object?>()))
            {
                var rowIndex = 0;
                foreach (var column in _columns)
                {
                    if (column.Storage != UtfStorage.PerRow)
                    {
                        continue;
                    }

                    WriteValue(rowBytes, column.Type, row[rowIndex++], strings);
                }
            }

            var rowsOffset = 0x20 + columnBytes.Length;
            var stringsOffset = rowsOffset + rowBytes.Length;
            var stringBytes = strings.Build();
            var unpaddedSize = stringsOffset + stringBytes.Length;
            var paddedSize = Align(unpaddedSize, 0x08);
            if (paddedSize != unpaddedSize)
            {
                Array.Resize(ref stringBytes, (int)(paddedSize - stringsOffset));
            }
            var tableSize = stringsOffset + stringBytes.Length - 8;
            var output = new MemoryStream();

            output.Write(Encoding.ASCII.GetBytes("@UTF"));
            WriteUInt32(output, (uint)tableSize);
            WriteUInt32(output, (uint)(rowsOffset - 8));
            WriteUInt32(output, (uint)(stringsOffset - 8));
            WriteUInt32(output, (uint)(stringsOffset + stringBytes.Length - 8));
            WriteUInt32(output, strings.OffsetOf(_name));
            WriteUInt16(output, (ushort)_columns.Count);
            WriteUInt16(output, (ushort)rowLength);
            WriteUInt32(output, (uint)Math.Max(1, _rows.Count));
            columnBytes.WriteTo(output);
            rowBytes.WriteTo(output);
            output.Write(stringBytes);

            return output.ToArray();
        }

        private static int TypeSize(UtfType type)
        {
            return type switch
            {
                UtfType.Int16 => 2,
                UtfType.UInt16 => 2,
                UtfType.Int32 => 4,
                UtfType.UInt32 => 4,
                UtfType.Int64 => 8,
                UtfType.UInt64 => 8,
                UtfType.String => 4,
                _ => throw new NotSupportedException($"Unsupported UTF type {type}")
            };
        }

        private static void WriteValue(Stream stream, UtfType type, object? value, StringTable strings)
        {
            switch (type)
            {
                case UtfType.Int16:
                case UtfType.UInt16:
                    WriteUInt16(stream, Convert.ToUInt16(value));
                    break;
                case UtfType.Int32:
                case UtfType.UInt32:
                    WriteUInt32(stream, Convert.ToUInt32(value));
                    break;
                case UtfType.Int64:
                case UtfType.UInt64:
                    WriteUInt64(stream, Convert.ToUInt64(value));
                    break;
                case UtfType.String:
                    WriteUInt32(stream, strings.Add(Convert.ToString(value) ?? string.Empty));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported UTF type {type}");
            }
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            stream.Write(buffer);
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            stream.Write(buffer);
        }

        private static void WriteUInt64(Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
            stream.Write(buffer);
        }
    }

    private sealed class StringTable
    {
        private readonly MemoryStream _stream = new();
        private readonly Dictionary<string, uint> _offsets = new(StringComparer.Ordinal);

        public uint Add(string value)
        {
            if (_offsets.TryGetValue(value, out var offset))
            {
                return offset;
            }

            offset = (uint)_stream.Length;
            var bytes = Encoding.UTF8.GetBytes(value);
            _stream.Write(bytes);
            _stream.WriteByte(0);
            _offsets.Add(value, offset);
            return offset;
        }

        public uint OffsetOf(string value) => _offsets[value];

        public byte[] Build() => _stream.ToArray();
    }
}
