using CriFsV2Lib;
using CriFsV2Lib.Definitions.Structs;

namespace Viola.Core.Utils.Cpk.Logic;

internal static class CAudioCpkRebuilder
{
    public static void Write(
        string sourceCpkPath,
        string outputPath,
        IReadOnlyList<CpkFilePayload> files,
        string tempRoot)
    {
        var extractRoot = Path.Combine(tempRoot, Path.GetFileNameWithoutExtension(outputPath));
        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, true);
        }

        Directory.CreateDirectory(extractRoot);

        try
        {
            var payloads = new List<CpkFilePayload>();
            var remainingReplacements = files.ToDictionary(file => file.RelativePath, file => file.SourcePath, StringComparer.OrdinalIgnoreCase);
            var newFiles = files.Where(file => file.IsNewFile)
                .Select(file => file.RelativePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            using var source = File.OpenRead(sourceCpkPath);
            using var reader = new CriFsLib().CreateCpkReader(source, true);

            foreach (var entry in reader.GetFiles().OrderBy(GetRelativePath, StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = GetRelativePath(entry);
                if (TryGetReplacement(remainingReplacements, newFiles, relativePath, entry.FileName, out var replacementPath, out var replacementKey))
                {
                    payloads.Add(new CpkFilePayload(relativePath, replacementPath, false));
                    remainingReplacements.Remove(replacementKey);
                    newFiles.Remove(replacementKey);
                    continue;
                }

                var tempPath = Path.Combine(extractRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                var localEntry = entry;
                using var extracted = reader.ExtractFileNoDecompression(in localEntry, out _);
                using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
                output.Write(extracted.Span);
                payloads.Add(new CpkFilePayload(relativePath, tempPath, false));
            }

            payloads.AddRange(remainingReplacements
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new CpkFilePayload(item.Key, item.Value, false)));

            CCriCpkWriter.Write(outputPath, payloads);
        }
        finally
        {
            if (Directory.Exists(extractRoot))
            {
                Directory.Delete(extractRoot, true);
            }
        }
    }

    private static string GetRelativePath(CpkFile file)
    {
        return string.IsNullOrWhiteSpace(file.Directory)
            ? file.FileName.Replace('\\', '/')
            : $"{file.Directory.Replace('\\', '/')}/{file.FileName.Replace('\\', '/')}";
    }

    private static bool TryGetReplacement(
        IReadOnlyDictionary<string, string> replacements,
        IReadOnlySet<string> newFiles,
        string relativePath,
        string fileName,
        out string replacementPath,
        out string replacementKey)
    {
        if (replacements.TryGetValue(relativePath, out replacementPath!))
        {
            replacementKey = relativePath;
            return true;
        }

        var matches = replacements
            .Where(item => !newFiles.Contains(item.Key))
            .Where(item => Path.GetFileName(item.Key).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
        {
            replacementKey = matches[0].Key;
            replacementPath = matches[0].Value;
            return true;
        }

        replacementPath = string.Empty;
        replacementKey = string.Empty;
        return false;
    }
}
