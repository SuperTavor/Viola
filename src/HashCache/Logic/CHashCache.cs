using System.Reflection;
using Viola.src.HashCache.DataClasses;
using Viola.src.Utils.General.Logic;

namespace Viola.src.HashCache.Logic
{
    class CHashCache
    {

        private HashCacheStructure _hashCacheContent = new HashCacheStructure();
        private string _dumpedRomfsPath;
        private string _hashCachePath;
        public CHashCache(string dumpedRomfsPath, HashCacheMode mode, string currentHashCachePath = "")
        {
            if (currentHashCachePath != "")
            {
                _hashCachePath = currentHashCachePath;
            }
            else
            {
                _hashCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HashCache.bin");
            }
            _dumpedRomfsPath = dumpedRomfsPath;
            if (mode == HashCacheMode.Load)
            {
                if (File.Exists(_hashCachePath))
                {
                    Load(_hashCachePath);
                }
                else
                {
                    Console.WriteLine("No HashCache.bin file found. Please download one or create one.");
                    Environment.Exit(1);
                }
            }
        }


        public uint GetRomfsFileHash(string key)
        {
            try
            {
                return _hashCacheContent.Hashes[key];
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine("Can't find key while looking in HashCache: " + key);
                Environment.Exit(1);
            }
            //to satisfy compiler
            return 0;
        }
        private byte[] CreateBinary()
        {
            Console.WriteLine("Starting hash cache creation...");
            var files = CGeneralUtils.GetAllFilesWithNormalSlash(_dumpedRomfsPath);

            foreach (var file in files)
            {
                _hashCacheContent.Hashes[Path.GetRelativePath(_dumpedRomfsPath, file).Replace("\\", "/")] = CGeneralUtils.ComputeCRC32(File.ReadAllBytes(file));
                Console.WriteLine($"Hashed {file}");
            }

            return CHashCacheSerializer.Serialize(_hashCacheContent);
        }
        private void Load(string currentHashCachePath)
        {
            var bin = File.ReadAllBytes(currentHashCachePath);
            _hashCacheContent = CHashCacheSerializer.Deserialize(bin);
        }
        public void Save()
        {
            var bin = CreateBinary();
            File.WriteAllBytes(_hashCachePath, bin);
        }
    }

}
