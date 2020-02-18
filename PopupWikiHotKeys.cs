using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  internal static class PopupWikiHotKeys
  {
    public const string ExtractPage         = "ExtractPage";
    public const string ExtractSM          = "ExtractSM";
    public const string ExtractSMWithPriority = "ExtractSMWithPriority";
    public const string ExtractSplitPage = "ExtractSplitPage";

    public static void RegisterHotKeys()
    {
      Svc.HotKeyManager

         //
         // Extracts
         .RegisterLocal(ExtractPage,
                        "Create Wiki Page extract",
                        new HotKey(Key.X, KeyModifiers.CtrlShift)
         )
         .RegisterLocal(ExtractSM,
                        "Create SM extract",
                        new HotKey(Key.X, KeyModifiers.Alt)
         )
         .RegisterLocal(ExtractSMWithPriority,
                        "Create SM extract with priority",
                        new HotKey(Key.X, KeyModifiers.AltShift)
         );
    }
  }
}
