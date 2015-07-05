// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// Redirects .NET framework assembly references from older to newer versions
	/// </summary>
	public static class FrameworkRedirect {
		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir2;
		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir4;

		struct FrameworkRedirectInfo {
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

		/// <summary>
		/// Redirects a .NET framework assembly from an older version to the correct version
		/// loaded at runtime.
		/// </summary>
		/// <param name="assembly">Current assembly reference that might get updated</param>
		/// <param name="sourceModule">Module using the assembly reference</param>
		public static void ApplyFrameworkRedirect(ref IAssembly assembly, ModuleDef sourceModule) {
			if (sourceModule == null)
				return;
			if (!Utils.LocaleEquals(assembly.Culture, ""))
				return;
			if (!sourceModule.IsClr20 && !sourceModule.IsClr40)
				return;

			FrameworkRedirectInfo redirect;
			if (!(sourceModule.IsClr20 ? frmRedir2 : frmRedir4).TryGetValue(assembly.Name, out redirect))
				return;
			if (PublicKeyBase.TokenCompareTo(assembly.PublicKeyOrToken, redirect.publicKeyToken) != 0)
				return;
			if (Utils.CompareTo(assembly.Version, redirect.redirectVersion) == 0)
				return;

			assembly = new AssemblyNameInfo(assembly);
			assembly.Version = redirect.redirectVersion;
		}
	}
}
