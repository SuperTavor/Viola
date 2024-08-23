using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Core.EncryptDecrypt.Logic
{
    public class CEnc
    {
        private MemoryStream _ms;
        private uint _key;
        private string _outputPath;
        public CEnc(CLaunchOptions options)
        {
            _outputPath=options.OutputPath;
            _ms = new(File.ReadAllBytes(options.InputPath));
            _key = options.Key;
        }

        public void Encrypt()
        {
            CLogger.LogInfo("Started encryption\n");
            var cryptor = new CCriwareCrypt(_ms,CriwareCryptMode.Encrypt, null, _key);
            cryptor.EncryptFile();
            //Write file
            File.WriteAllBytes(_outputPath, _ms.ToArray());
            _ms.Close();
            CLogger.LogInfo("Encrypted succussfully.");
        }
    }
}
