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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.13.0.0")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -start GeneralProfile -start DotNET -start ETW\\HookEvents.wpr" +
            "p")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -stop $FileName $ScreenshotDir")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -start GeneralProfile -start DotNET -start ETW\\HookEvents.wpr" +
            "p")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -stop $FileName $ScreenshotDir")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -cancel")]
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
        [global::System.Configuration.DefaultSettingValueAttribute(".\\Scripts\\xxWPR.cmd -cancel")]
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
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <string>xxwpr|.\Scripts\xxWPR.cmd</string>
  <string>wpr|wpr.exe</string>
  <string>xxprofile|""%perftools%\xxprofile.cmd""</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection CommandNameSubstitutions {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["CommandNameSubstitutions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfPreset xmlns:xsd=\"http://www.w3." +
            "org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n  <P" +
            "reset>\r\n    <Name>Full (Network+CSwitch+File+Frequency)</Name>\r\n    <TraceStartC" +
            "ommand>xxwpr -start \"ETW\\MultiProfile.wprp!Network\"  -start \"ETW\\MultiProfile.wp" +
            "rp!CSwitch\" -start \"ETW\\MultiProfile.wprp!File\" -start \"ETW\\MultiProfile.wprp!Fr" +
            "equency\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>x" +
            "xwpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>" +
            "xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>" +
            "\r\n  </Preset>\r\n  <Preset>\r\n    <Name>Default (CPU Samples/Disk/.NET Exceptions/F" +
            "ocus)</Name>\r\n    <TraceStartCommand>xxwpr -start \"ETW\\MultiProfile.wprp\" -start" +
            " ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $Fil" +
            "eName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</" +
            "TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n" +
            "  <Preset>\r\n    <Name>Frequency (CPU Samples/Disk/.NET Exceptions/Focus/Frequenc" +
            "y)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!Frequenc" +
            "y\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -" +
            "stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr " +
            "-cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </" +
            "Preset>\r\n  <Preset>\r\n    <Name>File (CPU Samples/Disk/.NET Exceptions/Focus/File" +
            " IO)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!File\" " +
            "-start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -sto" +
            "p $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -ca" +
            "ncel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Pre" +
            "set>\r\n  <Preset>\r\n    <Name>Handle (CPU Samples/Disk/.NET Exceptions/Focus/Handl" +
            "e)</Name>\r\n    <TraceStartCommand>xxwpr -setprofint 200000 -start \".\\ETW\\MultiPr" +
            "ofile.wprp!Handle\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceSto" +
            "pCommand>xxwpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCanc" +
            "elCommand>xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsM" +
            "anualEdit>\r\n  </Preset>\r\n  <Preset>\r\n    <Name>Network (CPU Samples/Disk/.NET Ex" +
            "ceptions/Focus/TCP/Socket)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\Mu" +
            "ltiProfile.wprp!Network\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <Tr" +
            "aceStopCommand>xxwpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <Tra" +
            "ceCancelCommand>xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</" +
            "NeedsManualEdit>\r\n  </Preset>\r\n  <Preset>\r\n    <Name>Network Light (CPU Samples/" +
            "Disk/.NET Exceptions/Focus/TCP/Socket)</Name>\r\n    <TraceStartCommand>xxwpr -sta" +
            "rt \".\\ETW\\MultiProfile.wprp!Network\" -start ETW\\HookEvents.wprp</TraceStartComma" +
            "nd>\r\n    <TraceStopCommand>xxwpr -stop $FileName $ScreenshotDir</TraceStopComman" +
            "d>\r\n    <TraceCancelCommand>xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManual" +
            "Edit>false</NeedsManualEdit>\r\n  </Preset>\r\n  <Preset>\r\n    <Name>VirtualAlloc (L" +
            "ong Term)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!V" +
            "irtualAlloc\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopComma" +
            "nd>xxwpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelComm" +
            "and>xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualE" +
            "dit>\r\n  </Preset>\r\n  <Preset>\r\n    <Name>UserGDILeaks (Long Term)</Name>\r\n    <T" +
            "raceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!UserGDILeaks\" -start ETW\\" +
            "HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $FileName" +
            " $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</Trace" +
            "CancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n  <Pr" +
            "eset>\r\n    <Name>PMCSample (PMC Sampling for PMC Rollover + Default)</Name>\r\n   " +
            " <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!PMCSample\" -start ETW\\" +
            "HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $FileName" +
            " $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</Trace" +
            "CancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n  <Pr" +
            "eset>\r\n    <Name>PMCBranch (PMC Cycles per Instruction and Branch data - Countin" +
            "g)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!PMCBranc" +
            "h\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -" +
            "stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr " +
            "-cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </" +
            "Preset>\r\n  <Preset>\r\n    <Name>PMCLLC (PMC Cycles per Instruction and LLC data -" +
            " Counting)</Name>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!" +
            "PMCLLC\" -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xx" +
            "wpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>x" +
            "xwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r" +
            "\n  </Preset>\r\n  <Preset>\r\n    <Name>LBR (LBR - Last Branch Record Sampling)</Nam" +
            "e>\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!LBR\" -start ETW" +
            "\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $FileNam" +
            "e $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</Trac" +
            "eCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n  <P" +
            "reset>\r\n    <Name>SysCall (CPU Samples/Disk/.NET Exceptions/Focus/SysCall)</Name" +
            ">\r\n    <TraceStartCommand>xxwpr -start \".\\ETW\\MultiProfile.wprp!SysCall\" -start " +
            "ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $File" +
            "Name $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</T" +
            "raceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n " +
            " <Preset>\r\n    <Name>WPR Default</Name>\r\n    <TraceStartCommand>xxwpr -start Gen" +
            "eralProfile -start ETW\\HookEvents.wprp</TraceStartCommand>\r\n    <TraceStopComman" +
            "d>xxwpr -stop $FileName $ScreenshotDir</TraceStopCommand>\r\n    <TraceCancelComma" +
            "nd>xxwpr -cancel</TraceCancelCommand>\r\n    <NeedsManualEdit>false</NeedsManualEd" +
            "it>\r\n  </Preset>\r\n  <Preset>\r\n    <Name>WPR Default + .NET</Name>\r\n    <TraceSta" +
            "rtCommand>xxwpr -start GeneralProfile -start DotNET -start ETW\\HookEvents.wprp</" +
            "TraceStartCommand>\r\n    <TraceStopCommand>xxwpr -stop $FileName $ScreenshotDir</" +
            "TraceStopCommand>\r\n    <TraceCancelCommand>xxwpr -cancel</TraceCancelCommand>\r\n " +
            "   <NeedsManualEdit>false</NeedsManualEdit>\r\n  </Preset>\r\n</ArrayOfPreset>")]
        public ETWController.UI.Preset[] Presets {
            get {
                return ((ETWController.UI.Preset[])(this["Presets"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CompressOnSave {
            get {
                return ((bool)(this["CompressOnSave"]));
            }
            set {
                this["CompressOnSave"] = value;
            }
        }
    }
}
