﻿#pragma checksum "..\..\..\..\UI\PopupWikiWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "75B69EFB710F62BFF333CC052400352D17B0838AF9427A7E0DD1E00419C83788"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace SuperMemoAssistant.Plugins.PopupWiki.UI {
    
    
    /// <summary>
    /// PopupWikiWindow
    /// </summary>
    public partial class PopupWikiWindow : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 38 "..\..\..\..\UI\PopupWikiWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Forms.Integration.WindowsFormsHost wfh;
        
        #line default
        #line hidden
        
        
        #line 41 "..\..\..\..\UI\PopupWikiWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Forms.WebBrowser wf_Browser;
        
        #line default
        #line hidden
        
        
        #line 58 "..\..\..\..\UI\PopupWikiWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnSMExtract;
        
        #line default
        #line hidden
        
        
        #line 65 "..\..\..\..\UI\PopupWikiWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnSMPriorityExtract;
        
        #line default
        #line hidden
        
        
        #line 73 "..\..\..\..\UI\PopupWikiWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnImport;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/SuperMemoAssistant.Plugins.PopupWiki;V0.1.0;component/ui/popupwikiwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\UI\PopupWikiWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.wfh = ((System.Windows.Forms.Integration.WindowsFormsHost)(target));
            return;
            case 2:
            this.wf_Browser = ((System.Windows.Forms.WebBrowser)(target));
            
            #line 43 "..\..\..\..\UI\PopupWikiWindow.xaml"
            this.wf_Browser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.wf_Browser_DocumentCompleted);
            
            #line default
            #line hidden
            return;
            case 3:
            this.BtnSMExtract = ((System.Windows.Controls.Button)(target));
            
            #line 62 "..\..\..\..\UI\PopupWikiWindow.xaml"
            this.BtnSMExtract.Click += new System.Windows.RoutedEventHandler(this.BtnSMExtract_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.BtnSMPriorityExtract = ((System.Windows.Controls.Button)(target));
            
            #line 68 "..\..\..\..\UI\PopupWikiWindow.xaml"
            this.BtnSMPriorityExtract.Click += new System.Windows.RoutedEventHandler(this.BtnSMPriorityExtract_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.BtnImport = ((System.Windows.Controls.Button)(target));
            
            #line 77 "..\..\..\..\UI\PopupWikiWindow.xaml"
            this.BtnImport.Click += new System.Windows.RoutedEventHandler(this.BtnImport_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}
