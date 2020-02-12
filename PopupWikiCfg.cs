using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forge.Forms.Annotations;
using SuperMemoAssistant.Sys.ComponentModel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net;

namespace SuperMemoAssistant.Plugins.PopupWiki
{
  [Form(Mode = DefaultFields.None)]
  [Title("Watcher Settings",
    IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
    "Cancel",
    IsCancel = true)]
  [DialogAction("save",
    "Save",
    IsDefault = true,
    Validates = true)]
  public class PopupWikiCfg: INotifyPropertyChangedEx
  {
    [Field(Name = "Wikipedia Language")]
    public string WikiLanguage { get; set; } = "en";
    
    [JsonIgnore]
    public bool IsChanged { get; set; }

    public override string ToString()
    {
      return "Watcher";
    }

    public event PropertyChangedEventHandler PropertyChanged;

  }
}
