using System.Runtime.InteropServices;

namespace Viola.Core.ViolaLogger.Logic
{
   
    public enum ImportantInfoType
    {
        Normal, 
        Fatal
    }
    public struct ImportantInfo
    {
        public bool IsFatal;
        public string Message;

        public ImportantInfo(string message, bool isFatal)
        {
            this.Message = message;
            this.IsFatal = isFatal;
        }
    }
    public static class CLogger
    {
        //Function to execute when you would show a messagebox. Null in CLI mode
        public static event Action<string>? GuiMsgBoxEvent = null;

        //Viola console rich text box buffer. Null in CLI mode
        public static event Action<string>? GuiLogInfoEvent = null;
        private static List<string>? _importantInfos;

        public static void AddImportantInfo(string info)
        {
            if (_importantInfos==null)
            {
                _importantInfos = new();
            }
            _importantInfos.Add(info);
        }
        public static void LogInfo(string msg)
        {
            //Meaning CLI mode is enabled
            if (GuiLogInfoEvent == null)
            {
                Console.WriteLine(msg);
            }
            else
            {
                GuiLogInfoEvent(msg+"\n");
            }
        }
        public static void InvokeImportantInfos()
        {
            if (_importantInfos == null) return;
            foreach(var info in _importantInfos)
            {
                //Meaning CLI mode is enabled
                if (GuiMsgBoxEvent == null)
                {
                    Console.Write(info);
                }
                else
                {
                    GuiMsgBoxEvent(info);
                }
            }

            _importantInfos = null;
        }

    }
}
