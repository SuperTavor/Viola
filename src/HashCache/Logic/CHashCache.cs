using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Viola.Dump;
using Viola.Utils;

namespace Viola.HashCache
{
    class CHashCache
    {

        public SCacheFormat HashCacheFile = new();
        public const string HASHCACHE_FILE = "HashCache.json";

        public string HashCacheJson = string.Empty;
        public void CreateJson()
        {
            Console.WriteLine("Starting hash cache creation...");
            var files = GeneralUtils.GetAllFiles(CDump.DumpFolder);
            HashCacheFile.EntryCount = (uint)files.Count;
            foreach (var file in files)
            {
                SCacheEntry entry = new SCacheEntry
                {
                    Path = file.Substring(CDump.DumpFolder.Length + 1),
                    Hash = GeneralUtils.ComputeCRC32(File.ReadAllBytes(file))
                };
                HashCacheFile.Entries.Add(entry);
                Console.WriteLine($"Hashed {file}");
            }

            HashCacheJson = JsonConvert.SerializeObject(HashCacheFile);
        }

        public void Load()
        {
            string json = File.ReadAllText(HASHCACHE_FILE);
            SCacheFormat hashCache = JsonConvert.DeserializeObject<SCacheFormat>(json);
            this.HashCacheFile = hashCache;
        }

        public int FindEntryIndex(string path)
        {
            for (int i = 0; i < HashCacheFile.Entries.Count; i++)
            {
                if (HashCacheFile.Entries[i].Path == path)
                {
                    return i;
                }
            }
            // If hasn't returned
            return -1;
        }
    }

}
