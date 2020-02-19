using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forge.Forms.Annotations;
using SuperMemoAssistant.Sys.ComponentModel;
using Newtonsoft.Json;
using System.ComponentModel;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;

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

    [Field(Name = "Load minimalist HTML for speed?")]
    public bool MinimalistHtml { get; set; } = false;
    
    [Field(Name = "Default SM Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double SMExtractPriority { get; set; } = 15;

    [Field(Name = "Default Page Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double PageExtractPriority { get; set; } = 15;

    [Field(Name = "Default Image Stretch Type")]
    [SelectFrom(typeof(ImageStretchMode),
                SelectionType = SelectionType.RadioButtonsInline)]
    public ImageStretchMode ImageStretchType { get; set; } = ImageStretchMode.Proportional;

    [Field(Name = "Add HTML component to extracts containing only images?")]
    public bool ImageExtractAddHtml { get; set; } = false;


    [JsonIgnore]
    public bool IsChanged { get; set; }

    public override string ToString()
    {
      return "PopupWiki";
    }

    public event PropertyChangedEventHandler PropertyChanged;

  }
}
