using System.Text;
using Viola.src.EncryptDecrypt.Logic.Utils;
using Viola.src.Launcher.DataClasses;

namespace Viola.src.EncryptDecrypt.Logic
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
            _ms = new MemoryStream(File.ReadAllBytes(_inputPath));
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
                Console.WriteLine($"[FATAL ERROR] Unsupported file extension for decryption: {extension}");
                Environment.Exit(1);
            }
            var cryptor = new CCriwareCrypt(_ms, CriwareCryptMode.Decrypt, ExtensionToHeader[extension]);
            _ms = cryptor.DecryptFile();
            //Write file
            File.WriteAllBytes(_outputPath, _ms.ToArray());
            Console.WriteLine($"Finished decrypting! PLEASE REMEMBER THIS KEY IF YOU WANNA RE-ENCRYPT THE FILE: {BitConverter.ToUInt32(cryptor.Key)}");
        }
    }
}
