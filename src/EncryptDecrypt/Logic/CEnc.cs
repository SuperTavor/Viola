using Viola.src.EncryptDecrypt.Logic.Utils;
using Viola.src.Launcher.DataClasses;

namespace Viola.src.EncryptDecrypt.Logic
{
    public class CEnc
    {
        private MemoryStream _ms;
        private uint _key;
        private string _outputPath;
        public CEnc(CLaunchOptions options)
        {
            _outputPath=options.OutputPath;
            _ms = new MemoryStream(File.ReadAllBytes(options.InputPath));
            _key = options.Key;
        }

        public void Encrypt()
        {
            var cryptor = new CCriwareCrypt(_ms, CriwareCryptMode.Encrypt, null, _key);
            _ms = cryptor.EncryptFile();
            //Write file
            File.WriteAllBytes(_outputPath, _ms.ToArray());
            Console.WriteLine("Finished encrypting.");
        }
    }
}
