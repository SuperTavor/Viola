
using System.Text;

namespace Viola.HashCacheNS
{
    public class HashCacheSerializer
    {
        public static byte[] Serialize(HashCacheStructure structure)
        {
            using var memoryStream = new MemoryStream();
            using var writer =new BinaryWriter(memoryStream);
            writer.Write(structure.Hashes.Count);
            foreach(var kvp in structure.Hashes)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
            return memoryStream.ToArray();
        }

        public static HashCacheStructure Deserialize(byte[] binaryRep)
        {
            HashCacheStructure structure = new HashCacheStructure();
            using var memoryStream = new MemoryStream(binaryRep);
            using var reader = new BinaryReader(memoryStream);
            var entryCount = reader.ReadInt32();
            for (int i = 0; i < entryCount; i++)
            {
                structure.Hashes[reader.ReadString()] = reader.ReadUInt32();
            }
            return structure;
        }
    }
}
