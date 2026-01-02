using System.Text;

namespace Viola.Core.EncryptDecrypt.Logic.Utils;

public static class CCriwareCrypt
{
    private static uint[]? _crc32Table;

    // Standard CRC32 Polynomial: 0xEDB88320
    private static void InitializeTable()
    {
        if (_crc32Table != null) return;
        _crc32Table = new uint[256];
        uint polynomial = 0xEDB88320;
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (uint j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1) crc = (crc >> 1) ^ polynomial;
                else crc >>= 1;
            }
            _crc32Table[i] = crc;
        }
    }

    /// <summary>
    /// Calculates the decryption key based on the filename.
    /// </summary>
    public static uint CalculateFilenameKey(string filename)
    {
        InitializeTable();
        
        // Game uses Standard Seed 0xFFFFFFFF
        uint crc = 0xFFFFFFFF; 
        
        byte[] bytes = Encoding.UTF8.GetBytes(filename);
        
        foreach (byte b in bytes)
        {
            byte index = (byte)((crc ^ b) & 0xFF);
            crc = (crc >> 8) ^ _crc32Table![index];
        }
        
        return ~crc; 
    }

    /// <summary>
    /// Core decryption logic for a byte array.
    /// </summary>
    public static void DecryptBlock(byte[] buffer, long fileOffset, uint key)
    {
        InitializeTable();

        byte[] KEY = BitConverter.GetBytes(key);
        
        // Initialize Rolling CRC State using the Offset (Salt)
        uint currentCrc = UpdateCrcState((uint)(fileOffset & -4), KEY);

        for (int i = 0; i < buffer.Length; i++)
        {
            long globalPos = i + fileOffset;
            
            if ((globalPos & 3) == 0) 
            {
                currentCrc = UpdateCrcState((uint)globalPos, KEY);
            }

            // Bit Scrambler Logic
            int baseShift = (int)(globalPos & 3) * 2; 

            uint r8 = (currentCrc >> (baseShift + 8)) & 3;
            uint rdx = (currentCrc >> baseShift) & 0xFF; 
            uint mask = (rdx << 2) & 0xFF; 
            r8 |= mask;

            rdx = (currentCrc >> (baseShift + 16)) & 3;
            mask = (r8 << 2) & 0xFF; 
            r8 = mask | rdx;

            rdx = (currentCrc >> (baseShift + 24)) & 3;
            mask = (r8 << 2) & 0xFF; 
            r8 = mask | rdx;

            buffer[i] ^= (byte)r8;
        }
    }

    /// <summary>
    /// Helper to stream data from Input to Output while decrypting.
    /// </summary>
    public static void ProcessStream(Stream input, Stream output, uint key, Action<long, long>? onProgress = null)
    {
        byte[] buffer = new byte[1024 * 1024]; // 1MB Chunk
        int bytesRead;
        long totalRead = 0;
        long totalLength = input.Length;

        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (bytesRead < buffer.Length)
            {
                byte[] partial = new byte[bytesRead];
                Array.Copy(buffer, partial, bytesRead);
                DecryptBlock(partial, totalRead, key);
                output.Write(partial, 0, bytesRead);
            }
            else
            {
                DecryptBlock(buffer, totalRead, key);
                output.Write(buffer, 0, bytesRead);
            }
            
            totalRead += bytesRead;
            onProgress?.Invoke(totalRead, totalLength);
        }
    }

    private static uint UpdateCrcState(uint seed, byte[] keys)
    {
        uint crc = ~seed;
        for(int k=0; k<4; k++)
        {
            byte index = (byte)(crc & 0xFF);
            index ^= keys[k];
            crc = (crc >> 8) ^ _crc32Table![index];
        }
        return ~crc;
    }
}