﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ETWController {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    internal sealed partial class Configuration : global::System.Configuration.ApplicationSettingsBase {
        
        private static Configuration defaultInstance = ((Configuration)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Configuration())));
        
        public static Configuration Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4295")]
        public int PortNumber {
            get {
                return ((int)(this["PortNumber"]));
            }
            set {
                this["PortNumber"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("remotehost.somedomain.com")]
        public string Host {
            get {
                return ((string)(this["Host"]));
            }
            set {
                this["Host"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8091")]
        public int WCFPort {
            get {
                return ((int)(this["WCFPort"]));
            }
            set {
                this["WCFPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Here it was slow")]
        public string SlowEventMessage {
            get {
                return ((string)(this["SlowEventMessage"]));
            }
            set {
                this["SlowEventMessage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Insert")]
        public string SlowEventHotkey {
            get {
                return ((string)(this["SlowEventHotkey"]));
            }
            set {
                this["SlowEventHotkey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -start GeneralProfile -start DotNET -start ETW\\HookEvents.w" +
            "prp")]
        public string LocalTraceStart {
            get {
                return ((string)(this["LocalTraceStart"]));
            }
            set {
                this["LocalTraceStart"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -stop $FileName $ScreenshotDir")]
        public string LocalTraceStop {
            get {
                return ((string)(this["LocalTraceStop"]));
            }
            set {
                this["LocalTraceStop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -start GeneralProfile -start DotNET -start ETW\\HookEvents.w" +
            "prp")]
        public string ServerTraceStart {
            get {
                return ((string)(this["ServerTraceStart"]));
            }
            set {
                this["ServerTraceStart"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -stop $FileName $ScreenshotDir")]
        public string ServerTraceStop {
            get {
                return ((string)(this["ServerTraceStop"]));
            }
            set {
                this["ServerTraceStop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool LocalTraceEnabled {
            get {
                return ((bool)(this["LocalTraceEnabled"]));
            }
            set {
                this["LocalTraceEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ServerTraceEnabled {
            get {
                return ((bool)(this["ServerTraceEnabled"]));
            }
            set {
                this["ServerTraceEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Here it was fast again")]
        public string FastEventMessage {
            get {
                return ((string)(this["FastEventMessage"]));
            }
            set {
                this["FastEventMessage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Delete")]
        public string FastEventHotkey {
            get {
                return ((string)(this["FastEventHotkey"]));
            }
            set {
                this["FastEventHotkey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\temp\\ETWControllerScreenshots")]
        public string ScreenshotDirectory {
            get {
                return ((string)(this["ScreenshotDirectory"]));
            }
            set {
                this["ScreenshotDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -cancel")]
        public string LocalTraceCancel {
            get {
                return ((string)(this["LocalTraceCancel"]));
            }
            set {
                this["LocalTraceCancel"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("::.\\Scripts\\xxWPR.cmd -cancel")]
        public string ServerTraceCancel {
            get {
                return ((string)(this["ServerTraceCancel"]));
            }
            set {
                this["ServerTraceCancel"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\temp\\ETW_%TS%_%COMPUTERNAME%.etl")]
        public string TraceFileName {
            get {
                return ((string)(this["TraceFileName"]));
            }
            set {
                this["TraceFileName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000")]
        public int ForcedScreenshotIntervalinMs {
            get {
                return ((int)(this["ForcedScreenshotIntervalinMs"]));
            }
            set {
                this["ForcedScreenshotIntervalinMs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CaptureKeyboard {
            get {
                return ((bool)(this["CaptureKeyboard"]));
            }
            set {
                this["CaptureKeyboard"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CaptureMouseButtonDown {
            get {
                return ((bool)(this["CaptureMouseButtonDown"]));
            }
            set {
                this["CaptureMouseButtonDown"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("85")]
        public int JpgCompressionLevel {
            get {
                return ((int)(this["JpgCompressionLevel"]));
            }
            set {
                this["JpgCompressionLevel"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int KeepNNewestScreenShots {
            get {
                return ((int)(this["KeepNNewestScreenShots"]));
            }
            set {
                this["KeepNNewestScreenShots"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("wpa -i $FileName -profile ETW\\Overview.wpaProfile")]
        public string TraceOpenCmdLine {
            get {
                return ((string)(this["TraceOpenCmdLine"]));
            }
            set {
                this["TraceOpenCmdLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CaptureMouseWheel {
            get {
                return ((bool)(this["CaptureMouseWheel"]));
            }
            set {
                this["CaptureMouseWheel"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CaptureMouseMove {
            get {
                return ((bool)(this["CaptureMouseMove"]));
            }
            set {
                this["CaptureMouseMove"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfPreset xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Preset>
    <Name>WPR Default</Name>
    <TraceStartCommand>::.\Scripts\xxWPR.cmd -start GeneralProfile -start ETW\HookEvents.wprp</TraceStartCommand>
    <TraceStopCommand>::.\Scripts\xxWPR.cmd -stop $FileName $ScreenshotDir</TraceStopCommand>
    <TraceCancelCommand>::.\Scripts\xxWPR.cmd -cancel</TraceCancelCommand>
    <NeedsManualEdit>false</NeedsManualEdit>
  </Preset>
  <Preset>
    <Name>WPR Default + .NET</Name>
    <TraceStartCommand>::.\Scripts\xxWPR.cmd -start GeneralProfile -start DotNET -start ETW\HookEvents.wprp</TraceStartCommand>
    <TraceStopCommand>::.\Scripts\xxWPR.cmd -stop $FileName $ScreenshotDir</TraceStopCommand>
    <TraceCancelCommand>::.\Scripts\xxWPR.cmd -cancel</TraceCancelCommand>
    <NeedsManualEdit>false</NeedsManualEdit>
  </Preset>
</ArrayOfPreset>")]
        public ETWController.UI.Preset[] Presets {
            get {
                return ((ETWController.UI.Preset[])(this["Presets"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool AlwaysShowCommandEditBoxes {
            get {
                return ((bool)(this["AlwaysShowCommandEditBoxes"]));
            }
            set {
                this["AlwaysShowCommandEditBoxes"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>xxwpr|::.\Scripts\xxWPR.cmd</string>
  <string>wpr|::wpr.exe</string>
  <string>xxprofile|::""%perftools%\xxprofile.cmd""</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection CommandNameSubstitutions {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["CommandNameSubstitutions"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool WelcomeScreenShown {
            get {
                return ((bool)(this["WelcomeScreenShown"]));
            }
            set {
                this["WelcomeScreenShown"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ConfigMigrationNeeded {
            get {
                return ((bool)(this["ConfigMigrationNeeded"]));
            }
            set {
                this["ConfigMigrationNeeded"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CaptureScreenShots {
            get {
                return ((bool)(this["CaptureScreenShots"]));
            }
            set {
                this["CaptureScreenShots"] = value;
            }
        }
    }
}
