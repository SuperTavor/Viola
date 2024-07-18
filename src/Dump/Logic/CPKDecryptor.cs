using System.Text;
namespace Viola.DumpNS;
public class CPKDecryptor
{
    private byte[]? key;
    private MemoryStream _ms;
    public CPKDecryptor(MemoryStream ms)
    {
        _ms = ms;
        GenerateKeyFromFileHeader();
    }
    public void GenerateKeyFromFileHeader()
    {
        byte[] headerBytes = new byte[4];
        _ms.Read(headerBytes, 0, 4);
        byte[] cpkMagic = Encoding.UTF8.GetBytes("CPK ");
        //XOR header bytes with the CPK magic to get the key
        key = new byte[4];
        for (int i = 0; i < cpkMagic.Length; i++)
        {
            key[i] = (byte)(cpkMagic[i] ^ headerBytes[i]);
        }
    }

    public MemoryStream DecryptFile()
    {
        byte[] data = _ms.ToArray();
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= key![i % key.Length];
        }
        if(Encoding.UTF8.GetString(data.Take(4).ToArray()) != "CPK ")
        {
            throw new FormatException("File is not a valid CPK!");
        }
        else return new MemoryStream(data);
    }
}