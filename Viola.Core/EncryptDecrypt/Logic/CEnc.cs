using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Core.EncryptDecrypt.Logic
{
    public class CEnc
    {
        private string _outputPath;
        private string _inputPath;

        public CEnc(CLaunchOptions options)
        {
            _inputPath = options.InputPath;
            _outputPath = options.OutputPath;
        }

        public void Encrypt()
        {
            var filename = Path.GetFileName(_inputPath);

            // Calculate Key based on filename
            uint key = CCriwareCrypt.CalculateFilenameKey(filename);

            CLogger.LogInfo($"Started encryption using Key: {key:X8}");

            try
            {
                using (var fsIn = new FileStream(_inputPath, FileMode.Open, FileAccess.Read))
                using (var fsOut = new FileStream(_outputPath, FileMode.Create, FileAccess.Write))
                {
                    CCriwareCrypt.ProcessStream(fsIn, fsOut, key);
                }

                CLogger.LogInfo("Encrypted successfully.");
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"Encryption Error: {ex.Message}");
            }
        }
    }
}