using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Viola.Avalonia.GuiUtils
{
    public class CGuiUtils
    {
        internal static Func<TopLevel, string, string, Task<string>>? ChooseFolderOverride;
        internal static Func<TopLevel, string, string, string, Task<string>>? ChooseExistingFileOverride;
        internal static Func<TopLevel, string, string, string, Task<string>>? SaveFileOverride;

        public static Task<string> ChooseFolderAsync(TopLevel topLevel, string desc, string initialDirectory = "")
        {
            if (ChooseFolderOverride != null)
            {
                return ChooseFolderOverride(topLevel, desc, initialDirectory);
            }
            return ChooseFolderCore(topLevel, desc, initialDirectory);
        }

        public static Task<string> ChooseExistingFileAsync(TopLevel topLevel, string windowTitle, string filter, string initialDirectory = "")
        {
            if (ChooseExistingFileOverride != null)
            {
                return ChooseExistingFileOverride(topLevel, windowTitle, filter, initialDirectory);
            }
            return ChooseExistingFileCore(topLevel, windowTitle, filter, initialDirectory);
        }

        public static Task<string> SaveFileAsync(TopLevel topLevel, string windowTitle, string filter, string initialDirectory = "")
        {
            if (SaveFileOverride != null)
            {
                return SaveFileOverride(topLevel, windowTitle, filter, initialDirectory);
            }
            return SaveFileCore(topLevel, windowTitle, filter, initialDirectory);
        }

        private static async Task<string> ChooseFolderCore(TopLevel topLevel, string desc, string initialDirectory)
        {
            var storage = topLevel.StorageProvider;
            var options = new FolderPickerOpenOptions
            {
                Title = desc,
                AllowMultiple = false,
                SuggestedStartLocation = await ResolveStartLocationAsync(storage, initialDirectory)
            };

            var picked = await storage.OpenFolderPickerAsync(options);
            return picked.Count > 0 ? picked[0].Path.LocalPath : string.Empty;
        }

        private static async Task<string> ChooseExistingFileCore(TopLevel topLevel, string windowTitle, string filter, string initialDirectory)
        {
            var storage = topLevel.StorageProvider;
            var options = new FilePickerOpenOptions
            {
                Title = windowTitle,
                AllowMultiple = false,
                FileTypeFilter = ParseFileTypeFilter(filter),
                SuggestedStartLocation = await ResolveStartLocationAsync(storage, initialDirectory)
            };

            var picked = await storage.OpenFilePickerAsync(options);
            return picked.Count > 0 ? picked[0].Path.LocalPath : string.Empty;
        }

        private static async Task<string> SaveFileCore(TopLevel topLevel, string windowTitle, string filter, string initialDirectory)
        {
            var storage = topLevel.StorageProvider;
            var options = new FilePickerSaveOptions
            {
                Title = windowTitle,
                FileTypeChoices = ParseFileTypeFilter(filter),
                SuggestedStartLocation = await ResolveStartLocationAsync(storage, initialDirectory)
            };

            var file = await storage.SaveFilePickerAsync(options);
            return file is null ? string.Empty : file.Path.LocalPath;
        }

        private static async Task<IStorageFolder?> ResolveStartLocationAsync(IStorageProvider storage, string initialDirectory)
        {
            if (string.IsNullOrEmpty(initialDirectory) || !Directory.Exists(initialDirectory))
            {
                return null;
            }
            return await storage.TryGetFolderFromPathAsync(initialDirectory);
        }

        internal static List<FilePickerFileType> ParseFileTypeFilter(string filter)
        {
            var result = new List<FilePickerFileType>();
            if (string.IsNullOrEmpty(filter)) return result;

            var parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var name = parts[i];
                var patterns = parts[i + 1]
                    .Split(';')
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .ToList();
                result.Add(new FilePickerFileType(name) { Patterns = patterns });
            }
            return result;
        }
    }
}