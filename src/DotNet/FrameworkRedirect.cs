// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace dnlib.DotNet {
	/// <summary>
	/// Redirects .NET framework assembly references from older to newer versions
	/// </summary>
	public static class FrameworkRedirect {
		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir2;
		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir4;

		readonly struct FrameworkRedirectInfo {
			public readonly PublicKeyToken publicKeyToken;
			public readonly Version redirectVersion;

			public FrameworkRedirectInfo(string publicKeyToken, string redirectVersion) {
				this.publicKeyToken = new PublicKeyToken(publicKeyToken);
				this.redirectVersion = new Version(redirectVersion);
			}
		}

		static FrameworkRedirect() {
			frmRedir2 = new Dictionary<string, FrameworkRedirectInfo>(StringComparer.OrdinalIgnoreCase);
			frmRedir4 = new Dictionary<string, FrameworkRedirectInfo>(StringComparer.OrdinalIgnoreCase);
			InitFrameworkRedirectV2();
			InitFrameworkRedirectV4();
		}

		static void InitFrameworkRedirectV2() {
			frmRedir2["Accessibility"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["cscompmgd"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["CustomMarshalers"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["IEExecRemote"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["IEHost"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["IIEHost"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["ISymWrapper"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["Microsoft.JScript"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.VisualBasic"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.VisualBasic.Compatibility"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.VisualBasic.Compatibility.Data"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.VisualBasic.Vsa"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.VisualC"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.Vsa"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft.Vsa.Vb.CodeDOMProcessor"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["Microsoft_VsaVb"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "8.0.0.0");
			frmRedir2["mscorcfg"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["mscorlib"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Configuration"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Configuration.Install"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Data"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Data.OracleClient"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Data.SqlXml"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Deployment"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Design"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.DirectoryServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.DirectoryServices.Protocols"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Drawing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Drawing.Design"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.EnterpriseServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Management"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Messaging"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Runtime.Remoting"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Runtime.Serialization.Formatters.Soap"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Security"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.ServiceProcess"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Transactions"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Web"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Web.Mobile"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Web.RegularExpressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Web.Services"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["System.Windows.Forms"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["System.Xml"] = new FrameworkRedirectInfo("b77a5c561934e089", "2.0.0.0");
			frmRedir2["vjscor"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["VJSharpCodeProvider"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjsJBC"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjslib"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjslibcw"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["Vjssupuilib"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjsvwaux"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjswfc"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["VJSWfcBrowserStubLib"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjswfccw"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir2["vjswfchtml"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
		}

		static void InitFrameworkRedirectV4() {
			frmRedir4["Accessibility"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["CustomMarshalers"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["ISymWrapper"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.JScript"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["Microsoft.VisualBasic"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["Microsoft.VisualBasic.Compatibility"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["Microsoft.VisualBasic.Compatibility.Data"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["Microsoft.VisualC"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["mscorlib"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Configuration"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Configuration.Install"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Data"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.OracleClient"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.SqlXml"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Deployment"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Design"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.DirectoryServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.DirectoryServices.Protocols"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Drawing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Drawing.Design"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.EnterpriseServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Management"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Messaging"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Remoting"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Runtime.Serialization.Formatters.Soap"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Security"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceProcess"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Transactions"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Web"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Web.Mobile"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Web.RegularExpressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Web.Services"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Windows.Forms"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Xml"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["AspNetMMCExt"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["sysglobl"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Build.Engine"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Build.Framework"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["PresentationCFFRasterizer"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationCore"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.Aero"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.Classic"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.Luna"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.Royale"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationUI"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["ReachFramework"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Printing"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Speech"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["UIAutomationClient"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["UIAutomationClientsideProviders"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["UIAutomationProvider"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["UIAutomationTypes"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["WindowsBase"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["WindowsFormsIntegration"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["SMDiagnostics"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IdentityModel"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IdentityModel.Selectors"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IO.Log"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Serialization"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.ServiceModel"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.ServiceModel.Install"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.ServiceModel.WasHosting"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Workflow.Activities"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Workflow.ComponentModel"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Workflow.Runtime"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["Microsoft.Transactions.Bridge"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Transactions.Bridge.Dtc"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.AddIn"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.AddIn.Contract"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ComponentModel.Composition"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Core"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.DataSetExtensions"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.Linq"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Xml.Linq"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.DirectoryServices.AccountManagement"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Management.Instrumentation"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Net"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.Web"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.Extensions"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.Extensions.Design"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Windows.Presentation"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.WorkflowServices"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ComponentModel.DataAnnotations"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Data.Entity"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.Entity.Design"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.Services"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.Services.Client"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Data.Services.Design"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Web.Abstractions"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.DynamicData"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.DynamicData.Design"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.Entity"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Web.Entity.Design"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Web.Routing"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["Microsoft.Build"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.CSharp"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Dynamic"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Numerics"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Xaml"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["Microsoft.Workflow.Compiler"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["Microsoft.Activities.Build"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["Microsoft.Build.Conversion.v4.0"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Build.Tasks.v4.0"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Build.Utilities.v4.0"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["Microsoft.Internal.Tasks.Dataflow"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["Microsoft.VisualBasic.Activities.Compiler"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "10.0.0.0");
			frmRedir4["Microsoft.VisualC.STLCLR"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "2.0.0.0");
			frmRedir4["Microsoft.Windows.ApplicationServer.Applications"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationBuildTasks"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.Aero2"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework.AeroLite"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["PresentationFramework-SystemCore"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["PresentationFramework-SystemData"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["PresentationFramework-SystemDrawing"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["PresentationFramework-SystemXml"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["PresentationFramework-SystemXmlLinq"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Activities"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Activities.Core.Presentation"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Activities.DurableInstancing"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Activities.Presentation"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ComponentModel.Composition.Registration"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Device"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IdentityModel.Services"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IO.Compression"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.IO.Compression.FileSystem"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Net.Http"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Net.Http.WebRequest"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Context"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Runtime.Caching"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.DurableInstancing"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Runtime.WindowsRuntime"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Runtime.WindowsRuntime.UI.Xaml"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.ServiceModel.Activation"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.Activities"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.Channels"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.Discovery"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.Internals"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.Routing"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.ServiceModel.ServiceMoniker40"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Web.ApplicationServices"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.DataVisualization"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Web.DataVisualization.Design"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Windows.Controls.Ribbon"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Windows.Forms.DataVisualization"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Windows.Forms.DataVisualization.Design"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Windows.Input.Manipulations"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
			frmRedir4["System.Xaml.Hosting"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["XamlBuildTask"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["XsdBuildTask"] = new FrameworkRedirectInfo("31bf3856ad364e35", "4.0.0.0");
			frmRedir4["System.Collections"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Collections.Concurrent"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ComponentModel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ComponentModel.Annotations"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ComponentModel.EventBasedAsync"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Diagnostics.Contracts"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Diagnostics.Debug"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Diagnostics.Tools"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Diagnostics.Tracing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Dynamic.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Globalization"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.IO"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Linq"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Linq.Expressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Linq.Parallel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Linq.Queryable"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Net.NetworkInformation"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Net.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Net.Requests"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ObjectModel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Emit"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Emit.ILGeneration"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Emit.Lightweight"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Reflection.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Resources.ResourceManager"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.InteropServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.InteropServices.WindowsRuntime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Numerics"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Serialization.Json"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Serialization.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Runtime.Serialization.Xml"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Security.Principal"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.Duplex"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.Http"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.NetTcp"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.ServiceModel.Security"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Text.Encoding"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Text.Encoding.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Text.RegularExpressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Threading"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Threading.Timer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Threading.Tasks"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Threading.Tasks.Parallel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Xml.ReaderWriter"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Xml.XDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Xml.XmlSerializer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Net.Http.Rtc"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Windows"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
			frmRedir4["System.Xml.Serialization"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.0.0");
		}

		// .NET Framework 4.5.1 and 4.5.2: only the differences from the previous table. Created when first used.
		static class V451 {
			internal static readonly Dictionary<string, FrameworkRedirectInfo> Table = Create();

			static Dictionary<string, FrameworkRedirectInfo> Create() {
				var d = new Dictionary<string, FrameworkRedirectInfo>(frmRedir4, StringComparer.OrdinalIgnoreCase);
				d["System.Diagnostics.Tracing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Runtime.InteropServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				return d;
			}
		}

		// .NET Framework 4.6, 4.6.1, 4.6.2 and 4.7: only the differences from the previous table. Created when first used.
		static class V46 {
			internal static readonly Dictionary<string, FrameworkRedirectInfo> Table = Create();

			static Dictionary<string, FrameworkRedirectInfo> Create() {
				var d = new Dictionary<string, FrameworkRedirectInfo>(V451.Table, StringComparer.OrdinalIgnoreCase);
				d["System.Collections"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Collections.Concurrent"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.ComponentModel.Annotations"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.ComponentModel.EventBasedAsync"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Diagnostics.Debug"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Diagnostics.Tracing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.20.0");
				d["System.Dynamic.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Globalization"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.IO"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Linq.Expressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Net.NetworkInformation"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Net.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Net.Requests"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Net.WebHeaderCollection"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
				d["System.ObjectModel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Reflection"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.20.0");
				d["System.Runtime.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Runtime.Handles"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.0.0");
				d["System.Runtime.InteropServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.20.0");
				d["System.Runtime.Serialization.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Runtime.Serialization.Xml"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.ServiceModel.Http"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Text.Encoding"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Text.Encoding.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Text.RegularExpressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Threading"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Threading.Tasks"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Xml.ReaderWriter"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Xml.XDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				d["System.Xml.XmlSerializer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.10.0");
				return d;
			}
		}

		// .NET Framework 4.7.1: only the differences from the previous table. Created when first used.
		static class V471 {
			internal static readonly Dictionary<string, FrameworkRedirectInfo> Table = Create();

			static Dictionary<string, FrameworkRedirectInfo> Create() {
				var d = new Dictionary<string, FrameworkRedirectInfo>(V46.Table, StringComparer.OrdinalIgnoreCase);
				d["Microsoft.Win32.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["netstandard"] = new FrameworkRedirectInfo("cc7b13ffcd2ddd51", "2.0.0.0");
				d["System.AppContext"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Collections"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Collections.Concurrent"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Collections.NonGeneric"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Collections.Specialized"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.ComponentModel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.ComponentModel.EventBasedAsync"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.ComponentModel.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.ComponentModel.TypeConverter"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Console"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Data.Common"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Diagnostics.Contracts"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Diagnostics.Debug"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Diagnostics.FileVersionInfo"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Diagnostics.Process"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Diagnostics.StackTrace"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.4.0");
				d["System.Diagnostics.TextWriterTraceListener"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Diagnostics.Tools"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Diagnostics.TraceSource"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Diagnostics.Tracing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Drawing.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Dynamic.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Globalization"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Globalization.Calendars"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Globalization.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.IO"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.IO.Compression.ZipFile"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.0.3.0");
				d["System.IO.FileSystem"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.IO.FileSystem.DriveInfo"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.IO.FileSystem.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.IO.FileSystem.Watcher"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.IO.IsolatedStorage"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.IO.MemoryMappedFiles"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.IO.Pipes"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.IO.UnmanagedMemoryStream"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Linq"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Linq.Expressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Linq.Parallel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Linq.Queryable"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Net.NameResolution"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Net.NetworkInformation"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Net.Ping"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Net.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Net.Requests"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Net.Security"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Net.Sockets"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Net.WebHeaderCollection"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Net.WebSockets"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Net.WebSockets.Client"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.ObjectModel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Reflection"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Reflection.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Reflection.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Resources.Reader"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Resources.ResourceManager"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Resources.Writer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Runtime"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Runtime.CompilerServices.VisualC"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Runtime.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Runtime.Handles"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Runtime.InteropServices"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Runtime.InteropServices.RuntimeInformation"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Runtime.Numerics"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Runtime.Serialization.Formatters"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Runtime.Serialization.Json"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Runtime.Serialization.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.3.0");
				d["System.Runtime.Serialization.Xml"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.3.0");
				d["System.Security.Claims"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Security.Cryptography.Algorithms"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.2.0");
				d["System.Security.Cryptography.Csp"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Security.Cryptography.Encoding"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Security.Cryptography.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Security.Cryptography.X509Certificates"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.2.0");
				d["System.Security.Principal"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Security.SecureString"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Text.Encoding"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Text.Encoding.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Text.RegularExpressions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.1.0");
				d["System.Threading"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Threading.Overlapped"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Threading.Tasks"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Threading.Tasks.Parallel"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.Threading.Thread"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.2.0");
				d["System.Threading.ThreadPool"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.12.0");
				d["System.Threading.Timer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.1.0");
				d["System.ValueTuple"] = new FrameworkRedirectInfo("cc7b13ffcd2ddd51", "4.0.2.0");
				d["System.Xml.ReaderWriter"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.1.0");
				d["System.Xml.XDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Xml.XmlDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Xml.XmlSerializer"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.11.0");
				d["System.Xml.XPath"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				d["System.Xml.XPath.XDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.0.3.0");
				return d;
			}
		}

		// .NET Framework 4.7.2, 4.8 and 4.8.1: only the differences from the previous table. Created when first used.
		static class V472 {
			internal static readonly Dictionary<string, FrameworkRedirectInfo> Table = Create();

			static Dictionary<string, FrameworkRedirectInfo> Create() {
				var d = new Dictionary<string, FrameworkRedirectInfo>(V471.Table, StringComparer.OrdinalIgnoreCase);
				d["System.Data.Common"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.0.0");
				d["System.Diagnostics.StackTrace"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.0.0");
				d["System.Diagnostics.Tracing"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.0.0");
				d["System.Globalization.Extensions"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.0.0");
				d["System.IO.Compression"] = new FrameworkRedirectInfo("b77a5c561934e089", "4.2.0.0");
				d["System.Net.Http"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.0.0");
				d["System.Net.Sockets"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.0.0");
				d["System.Runtime.Serialization.Primitives"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.2.0.0");
				d["System.Security.Cryptography.Algorithms"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.3.0.0");
				d["System.Security.SecureString"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.0.0");
				d["System.Threading.Overlapped"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.0.0");
				d["System.Xml.XPath.XDocument"] = new FrameworkRedirectInfo("b03f5f7f11d50a3a", "4.1.0.0");
				return d;
			}
		}

		static Dictionary<string, FrameworkRedirectInfo> GetTable(FrameworkRedirectVersion version) {
			switch (version) {
			case FrameworkRedirectVersion.Net20:
			case FrameworkRedirectVersion.Net30:
			case FrameworkRedirectVersion.Net35:
				return frmRedir2;
			case FrameworkRedirectVersion.Net40:
			case FrameworkRedirectVersion.Net45:
				return frmRedir4;
			case FrameworkRedirectVersion.Net451:
			case FrameworkRedirectVersion.Net452:
				return V451.Table;
			case FrameworkRedirectVersion.Net46:
			case FrameworkRedirectVersion.Net461:
			case FrameworkRedirectVersion.Net462:
			case FrameworkRedirectVersion.Net47:
				return V46.Table;
			case FrameworkRedirectVersion.Net471:
				return V471.Table;
			case FrameworkRedirectVersion.Net472:
			case FrameworkRedirectVersion.Net48:
			case FrameworkRedirectVersion.Net481:
				return V472.Table;
			default:
				throw new ArgumentOutOfRangeException(nameof(version));
			}
		}

		/// <summary>
		/// Redirects a .NET Framework assembly from an older version to the version in .NET Framework <paramref name="version"/>
		/// </summary>
		/// <param name="assembly">Current assembly reference that might get updated</param>
		/// <param name="version">The .NET Framework version, eg. the installed version or the version of the reference assemblies</param>
		public static void ApplyFrameworkRedirect(ref IAssembly assembly, FrameworkRedirectVersion version) {
			if (TryApplyFrameworkRedirect(assembly, GetTable(version), out var redirectedAssembly))
				assembly = redirectedAssembly;
		}

		/// <summary>
		/// Tries to redirect a .NET Framework assembly from an older version to the version in .NET Framework <paramref name="version"/>
		/// </summary>
		/// <param name="assembly">Assembly reference</param>
		/// <param name="version">The .NET Framework version, eg. the installed version or the version of the reference assemblies</param>
		/// <param name="redirectedAssembly">Updated with the redirected assembly if successful</param>
		/// <returns></returns>
		public static bool TryApplyFrameworkRedirect(IAssembly assembly, FrameworkRedirectVersion version, [NotNullWhen(true)] out IAssembly? redirectedAssembly) =>
			TryApplyFrameworkRedirect(assembly, GetTable(version), out redirectedAssembly);

		/// <summary>
		/// Gets the .NET Framework version <paramref name="module"/> targets: the version in its assembly's
		/// <c>System.Runtime.Versioning.TargetFrameworkAttribute</c> (rounded down to a known version) if it's a
		/// .NET Framework assembly, else <see cref="FrameworkRedirectVersion.Net40"/> if it's a CLR 4.0 module or
		/// <see cref="FrameworkRedirectVersion.Net20"/> if it's a CLR 2.0 module.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="version">Updated with the .NET Framework version if successful</param>
		/// <returns></returns>
		public static bool TryGetFrameworkRedirectVersion(ModuleDef? module, out FrameworkRedirectVersion version) {
			version = FrameworkRedirectVersion.Net20;
			if (module is null)
				return false;
			if (module.Assembly is { } asm && asm.TryGetOriginalTargetFrameworkAttribute(out var framework, out var tfmVersion, out _)) {
				if (framework != ".NETFramework")
					return false;
				version = GetFrameworkRedirectVersion(tfmVersion);
				return true;
			}
			if (module.IsClr40) {
				version = FrameworkRedirectVersion.Net40;
				return true;
			}
			return module.IsClr20;
		}

		// The newest known version that isn't newer than 'version'
		static FrameworkRedirectVersion GetFrameworkRedirectVersion(Version version) {
			int v = version.Major * 100 + version.Minor * 10 + Math.Max(0, Math.Min(9, version.Build));
			if (v >= 481) return FrameworkRedirectVersion.Net481;
			if (v >= 480) return FrameworkRedirectVersion.Net48;
			if (v >= 472) return FrameworkRedirectVersion.Net472;
			if (v >= 471) return FrameworkRedirectVersion.Net471;
			if (v >= 470) return FrameworkRedirectVersion.Net47;
			if (v >= 462) return FrameworkRedirectVersion.Net462;
			if (v >= 461) return FrameworkRedirectVersion.Net461;
			if (v >= 460) return FrameworkRedirectVersion.Net46;
			if (v >= 452) return FrameworkRedirectVersion.Net452;
			if (v >= 451) return FrameworkRedirectVersion.Net451;
			if (v >= 450) return FrameworkRedirectVersion.Net45;
			if (v >= 400) return FrameworkRedirectVersion.Net40;
			if (v >= 350) return FrameworkRedirectVersion.Net35;
			if (v >= 300) return FrameworkRedirectVersion.Net30;
			return FrameworkRedirectVersion.Net20;
		}

		/// <summary>
		/// Redirects a .NET Framework assembly from an older version to the correct version
		/// loaded at runtime. The .NET Framework 4.0/4.5 versions are used if <paramref name="sourceModule"/>
		/// is a CLR 4.0 module, and the .NET Framework 2.0-3.5 versions if it's a CLR 2.0 module. Use the method
		/// with a <see cref="FrameworkRedirectVersion"/> argument to redirect to a later version's assemblies.
		/// </summary>
		/// <param name="assembly">Current assembly reference that might get updated</param>
		/// <param name="sourceModule">Module using the assembly reference</param>
		public static void ApplyFrameworkRedirect(ref IAssembly assembly, ModuleDef? sourceModule) {
			if (TryApplyFrameworkRedirectCore(assembly, sourceModule, out var redirectedAssembly))
				assembly = redirectedAssembly;
		}

		/// <summary>
		/// Tries to redirect a .NET Framework assembly from an older version to the correct version
		/// loaded at runtime. The .NET Framework 4.0/4.5 versions are used if <paramref name="sourceModule"/>
		/// is a CLR 4.0 module, and the .NET Framework 2.0-3.5 versions if it's a CLR 2.0 module. Use the method
		/// with a <see cref="FrameworkRedirectVersion"/> argument to redirect to a later version's assemblies.
		/// </summary>
		/// <param name="assembly">Assembly reference</param>
		/// <param name="sourceModule">Module using the assembly reference</param>
		/// <param name="redirectedAssembly">Updated with the redirected assembly if successful</param>
		/// <returns></returns>
		public static bool TryApplyFrameworkRedirect(IAssembly assembly, ModuleDef? sourceModule, [NotNullWhen(true)] out IAssembly? redirectedAssembly) =>
			TryApplyFrameworkRedirectCore(assembly, sourceModule, out redirectedAssembly);

		static bool TryApplyFrameworkRedirectCore(IAssembly assembly, ModuleDef? sourceModule, [NotNullWhen(true)] out IAssembly? redirectedAssembly) {
			if (sourceModule is not null) {
				if (sourceModule.IsClr40)
					return TryApplyFrameworkRedirect(assembly, frmRedir4, out redirectedAssembly);
				if (sourceModule.IsClr20)
					return TryApplyFrameworkRedirect(assembly, frmRedir2, out redirectedAssembly);
			}

			redirectedAssembly = null;
			return false;
		}

		/// <summary>
		/// Redirects a .NET Framework 2.0-3.5 assembly from an older version to the correct version
		/// loaded at runtime.
		/// </summary>
		/// <param name="assembly">Current assembly reference that might get updated</param>
		[Obsolete("Use " + nameof(ApplyFrameworkRedirect) + "(ref IAssembly, " + nameof(FrameworkRedirectVersion) + ") with " + nameof(FrameworkRedirectVersion) + "." + nameof(FrameworkRedirectVersion.Net20) + " or the real .NET Framework version")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void ApplyFrameworkRedirectV2(ref IAssembly assembly) {
			if (TryApplyFrameworkRedirect(assembly, frmRedir2, out var redirectedAssembly))
				assembly = redirectedAssembly;
		}

		/// <summary>
		/// Redirects a .NET Framework 4.0+ assembly from an older version to the correct version
		/// loaded at runtime.
		/// </summary>
		/// <param name="assembly">Current assembly reference that might get updated</param>
		[Obsolete("Use " + nameof(ApplyFrameworkRedirect) + "(ref IAssembly, " + nameof(FrameworkRedirectVersion) + ") with " + nameof(FrameworkRedirectVersion) + "." + nameof(FrameworkRedirectVersion.Net40) + " or the real .NET Framework version")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void ApplyFrameworkRedirectV4(ref IAssembly assembly) {
			if (TryApplyFrameworkRedirect(assembly, frmRedir4, out var redirectedAssembly))
				assembly = redirectedAssembly;
		}

		/// <summary>
		/// Tries to redirect a .NET Framework 2.0-3.5 assembly from an older version to the correct version
		/// loaded at runtime.
		/// </summary>
		/// <param name="assembly">Assembly reference</param>
		/// <param name="redirectedAssembly">Updated with the redirected assembly if successful</param>
		/// <returns></returns>
		[Obsolete("Use " + nameof(TryApplyFrameworkRedirect) + "(IAssembly, " + nameof(FrameworkRedirectVersion) + ", out IAssembly) with " + nameof(FrameworkRedirectVersion) + "." + nameof(FrameworkRedirectVersion.Net20) + " or the real .NET Framework version")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool TryApplyFrameworkRedirectV2(IAssembly assembly, [NotNullWhen(true)] out IAssembly? redirectedAssembly) =>
			TryApplyFrameworkRedirect(assembly, frmRedir2, out redirectedAssembly);

		/// <summary>
		/// Tries to redirect a .NET Framework 4.0+ assembly from an older version to the correct version
		/// loaded at runtime.
		/// </summary>
		/// <param name="assembly">Assembly reference</param>
		/// <param name="redirectedAssembly">Updated with the redirected assembly if successful</param>
		/// <returns></returns>
		[Obsolete("Use " + nameof(TryApplyFrameworkRedirect) + "(IAssembly, " + nameof(FrameworkRedirectVersion) + ", out IAssembly) with " + nameof(FrameworkRedirectVersion) + "." + nameof(FrameworkRedirectVersion.Net40) + " or the real .NET Framework version")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool TryApplyFrameworkRedirectV4(IAssembly assembly, [NotNullWhen(true)] out IAssembly? redirectedAssembly) =>
			TryApplyFrameworkRedirect(assembly, frmRedir4, out redirectedAssembly);

		static bool TryApplyFrameworkRedirect(IAssembly assembly, Dictionary<string, FrameworkRedirectInfo> frmRedir, [NotNullWhen(true)] out IAssembly? redirectedAssembly) {
			redirectedAssembly = null;
			if (!Utils.LocaleEquals(assembly.Culture, ""))
				return false;
			if (!frmRedir.TryGetValue(assembly.Name, out var redirect))
				return false;
			if (PublicKeyBase.TokenCompareTo(assembly.PublicKeyOrToken, redirect.publicKeyToken) != 0)
				return false;

			if (Utils.CompareTo(assembly.Version, redirect.redirectVersion) == 0)
				redirectedAssembly = assembly;
			else {
				redirectedAssembly = new AssemblyNameInfo(assembly);
				redirectedAssembly.Version = redirect.redirectVersion;
			}
			return true;
		}
	}
}
