using Viola.Core.EncryptDecrypt.Logic.Utils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.ViolaLogger.Logic;
using Viola.Core.Utils.General.Logic;

namespace Viola.Core.EncryptDecrypt.Logic
{
    public class CDec
    {
        private string _outputPath;
        private string _inputPath;

        public CDec(CLaunchOptions options)
        {
            _inputPath = options.InputPath;
            _outputPath = options.OutputPath;
        }

        public void Decrypt()
        {
            var filename = Path.GetFileName(_inputPath);

            // Calculate Key based on filename
            uint key = CCriwareCrypt.CalculateFilenameKey(filename);

            CLogger.LogInfo($"Decrypting {filename} with Key: {key:X8}...");

            try
            {
                using (var fsIn = new FileStream(_inputPath, FileMode.Open, FileAccess.Read))
                using (var fsOut = new FileStream(_outputPath, FileMode.Create, FileAccess.Write))
                {
                    long lastUpdateTick = 0;
                    CCriwareCrypt.ProcessStream(fsIn, fsOut, key, (curr, tot) =>
                    {
                        long now = DateTime.Now.Ticks;
                        if (now - lastUpdateTick > 500000) // 50ms throttle
                        {
                            CGeneralUtils.ReportProgress(curr, tot, "Decrypting");
                            lastUpdateTick = now;
                        }
                    });
                    CGeneralUtils.ReportProgress(100, 100, "Decrypting");
                }
                
                CLogger.LogInfo($"Finished decrypting to `{_outputPath.Replace("\\", "/")}`");
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"Decryption Error: {ex.Message}");
            }
            CGeneralUtils.ReportProgress(0, 0, "");
        }
    }
}