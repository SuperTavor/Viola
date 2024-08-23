using System.ComponentModel.DataAnnotations;
using System.Text;
using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Core.EncryptDecrypt.Logic
{
    public class CDec
    {
        private MemoryStream _ms;
        private string _outputPath;
        private string _inputPath;
        private Dictionary<string, byte[]> ExtensionToHeader = new()
        {
            {".cpk",Encoding.UTF8.GetBytes("CPK ")},
            {".usm",Encoding.UTF8.GetBytes("CRID")},
            {".acb",Encoding.UTF8.GetBytes("@UTF")},
            {".awb",Encoding.UTF8.GetBytes("AFS2")},
            {".acf",Encoding.UTF8.GetBytes("@UTF")}
        };
        public CDec(CLaunchOptions options)
        {
            _inputPath = options.InputPath;
            _outputPath = options.OutputPath;
            _ms = new(File.ReadAllBytes(_inputPath));
        }

        public void Decrypt()
        {
            var extension = Path.GetExtension(_inputPath);
            byte[] header;
            try
            {
                header = ExtensionToHeader[extension];
            }
            catch
            {
                CLogger.AddImportantInfo($"Unsupported file extension for decryption: {extension}");
                return;
            }
            var cryptor = new CCriwareCrypt(_ms ,CriwareCryptMode.Decrypt, ExtensionToHeader[extension]);
            cryptor.DecryptFile();
            //Write file
            File.WriteAllBytes(_outputPath,_ms.ToArray());
            //Close the input ms
            _ms.Close();
            CLogger.LogInfo($"**Finished decrypting! PLEASE REMEMBER THIS KEY IF YOU WANT TO RE-ENCRYPT THE FILE: {BitConverter.ToUInt32(cryptor.Key)}**\n");
        }
    }
}
