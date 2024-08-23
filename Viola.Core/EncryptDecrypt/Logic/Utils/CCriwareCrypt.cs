using System.Text;
namespace Viola.Core.EncryptDecrypt.Logic.Utils;
public class CCriwareCrypt
{
    public byte[]? Key;
    private MemoryStream _ms;
    private byte[] _originalMagic;
    //Key specification is only required when encrypting.
    public CCriwareCrypt(MemoryStream ms, CriwareCryptMode mode, byte[]? originalMagic = null, uint? key = null)
    {
        _ms = ms;
        if (mode == CriwareCryptMode.Decrypt)
        {
            if (originalMagic == null)
            {
                throw new ArgumentException("Please specify the original magic when decrypting using the CriwareCrypt class.");
            }
            else
            {
                _originalMagic = originalMagic;
            }
            GenerateKeyFromFileHeader();
        }
        else if (mode == CriwareCryptMode.Encrypt)
        {
            if (key == null)
            {
                throw new ArgumentException("Please specify the key when encrypting using the CriwareCrypt class.");
            }
            else
            {
                Key = BitConverter.GetBytes(key.Value);
            }
        }
    }
    public void GenerateKeyFromFileHeader()
    {
        byte[] headerBytes = new byte[4];
        _ms.Read(headerBytes, 0, 4);
        //XOR header bytes with the magic to get the key
        Key = new byte[4];
        for (int i = 0; i < _originalMagic.Length; i++)
        {
            Key[i] = (byte)(_originalMagic[i] ^ headerBytes[i]);
        }
    }

    public void DecryptFile()
    {
        byte[] data = _ms.ToArray();
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= Key![i % Key.Length];
        }
        if (!data.Take(4).ToArray().SequenceEqual(_originalMagic))
        {
            throw new FormatException("File does not match the specified format");
        }
        _ms.Position = 0;
        _ms.Write(data, 0, data.Length);
        _ms.SetLength(data.Length);
        _ms.Position = 0;
    }

    public void EncryptFile()
    {
        byte[] data = _ms.ToArray();
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= Key![i % Key.Length];
        }
        _ms.Position = 0;
        _ms.Write(data, 0, data.Length);
        _ms.SetLength(data.Length);
        _ms.Position = 0;
    }
}