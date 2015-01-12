// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// Resolves assemblies
	/// </summary>
	public class AssemblyResolver : IAssemblyResolver {
		static readonly ModuleDef nullModule = new ModuleDefUser();

		// DLL files are searched before EXE files
		static readonly IList<string> assemblyExtensions = new string[] { ".dll", ".exe" };

		static readonly GacInfo gac2Info;	// .NET 1.x and 2.x
		static readonly GacInfo gac4Info;	// .NET 4.x

		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir2;
		static readonly Dictionary<string, FrameworkRedirectInfo> frmRedir4;

		ModuleContext defaultModuleContext;
		readonly Dictionary<ModuleDef, IList<string>> moduleSearchPaths = new Dictionary<ModuleDef, IList<string>>();
		readonly Dictionary<string, AssemblyDef> cachedAssemblies = new Dictionary<string, AssemblyDef>(StringComparer.Ordinal);
		readonly ThreadSafe.IList<string> preSearchPaths = ThreadSafeListCreator.Create<string>();
		readonly ThreadSafe.IList<string> postSearchPaths = ThreadSafeListCreator.Create<string>();
		bool findExactMatch;
		bool enableFrameworkRedirect;
		bool enableTypeDefCache;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		sealed class GacInfo {
			public readonly string path;
			public readonly string prefix;
			public readonly IList<string> subDirs;

			public GacInfo(string prefix, string path, IList<string> subDirs) {
				this.prefix = prefix;
				this.path = path;
				this.subDirs = subDirs;
			}
		}

		struct FrameworkRedirectInfo {
			public readonly PublicKeyToken publicKeyToken;
			public readonly Version redirectVersion;

			public FrameworkRedirectInfo(string publicKeyToken, string redirectVersion) {
				this.publicKeyToken = new PublicKeyToken(publicKeyToken);
				this.redirectVersion = new Version(redirectVersion);
			}
		}

		static AssemblyResolver() {
			var windir = Environment.GetEnvironmentVariable("WINDIR");
			if (!string.IsNullOrEmpty(windir)) {
				gac2Info = new GacInfo("", Path.Combine(windir, "assembly"), new string[] {
					"GAC_32", "GAC_64", "GAC_MSIL", "GAC"
				});
				gac4Info = new GacInfo("v4.0_", Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly"), new string[] {
					"GAC_32", "GAC_64", "GAC_MSIL"
				});
			}

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

		static void ApplyFrameworkRedirect(ref IAssembly assembly, ModuleDef sourceModule) {
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

		/// <summary>
		/// Gets/sets the default <see cref="ModuleContext"/>
		/// </summary>
		public ModuleContext DefaultModuleContext {
			get { return defaultModuleContext; }
			set { defaultModuleContext = value; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Resolve"/> should find an assembly that matches exactly.
		/// <c>false</c> if it first tries to match exactly, and if that fails, it picks an
		/// assembly that is closest to the requested assembly.
		/// </summary>
		public bool FindExactMatch {
			get { return findExactMatch; }
			set { findExactMatch = value; }
		}

		/// <summary>
		/// <c>true</c> if resolved .NET framework assemblies can be redirected to the source
		/// module's framework assembly version. Eg. if a resolved .NET 3.5 assembly can be
		/// redirected to a .NET 4.0 assembly if the source module is a .NET 4.0 assembly. This is
		/// ignored if <see cref="FindExactMatch"/> is <c>true</c>.
		/// </summary>
		public bool EnableFrameworkRedirect {
			get { return enableFrameworkRedirect; }
			set { enableFrameworkRedirect = value; }
		}

		/// <summary>
		/// If <c>true</c>, all modules in newly resolved assemblies will have their
		/// <see cref="ModuleDef.EnableTypeDefFindCache"/> property set to <c>true</c>.
		/// </summary>
		public bool EnableTypeDefCache {
			get { return enableTypeDefCache; }
			set { enableTypeDefCache = value; }
		}

		/// <summary>
		/// Gets paths searched before trying the standard locations
		/// </summary>
		public ThreadSafe.IList<string> PreSearchPaths {
			get { return preSearchPaths; }
		}

		/// <summary>
		/// Gets paths searched after trying the standard locations
		/// </summary>
		public ThreadSafe.IList<string> PostSearchPaths {
			get { return postSearchPaths; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolver()
			: this(null, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultModuleContext">Module context for all resolved assemblies</param>
		public AssemblyResolver(ModuleContext defaultModuleContext)
			: this(defaultModuleContext, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultModuleContext">Module context for all resolved assemblies</param>
		/// <param name="addOtherSearchPaths">If <c>true</c>, add other common assembly search
		/// paths, not just the module search paths and the GAC.</param>
		public AssemblyResolver(ModuleContext defaultModuleContext, bool addOtherSearchPaths) {
			this.defaultModuleContext = defaultModuleContext;
			this.enableFrameworkRedirect = true;
			if (addOtherSearchPaths)
				AddOtherSearchPaths(postSearchPaths);
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;

			if (EnableFrameworkRedirect && !FindExactMatch)
				ApplyFrameworkRedirect(ref assembly, sourceModule);

#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			AssemblyDef resolvedAssembly = Resolve2(assembly, sourceModule);
			if (resolvedAssembly == null) {
				string asmName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				string asmNameTrimmed = asmName.Trim();
				if (asmName != asmNameTrimmed) {
					assembly = new AssemblyNameInfo {
						Name = asmNameTrimmed,
						Version = assembly.Version,
						PublicKeyOrToken = assembly.PublicKeyOrToken,
						Culture = assembly.Culture,
					};
					resolvedAssembly = Resolve2(assembly, sourceModule);
				}
			}

			if (resolvedAssembly == null) {
				// Make sure we don't search for this assembly again. This speeds up callers who
				// keep asking for this assembly when trying to resolve many different TypeRefs
				cachedAssemblies[GetAssemblyNameKey(assembly)] = null;
				return null;
			}

			var key1 = GetAssemblyNameKey(resolvedAssembly);
			var key2 = GetAssemblyNameKey(assembly);
			AssemblyDef asm1, asm2;
			cachedAssemblies.TryGetValue(key1, out asm1);
			cachedAssemblies.TryGetValue(key2, out asm2);

			if (asm1 != resolvedAssembly && asm2 != resolvedAssembly) {
				// This assembly was just resolved
				if (enableTypeDefCache) {
					foreach (var module in resolvedAssembly.Modules.GetSafeEnumerable()) {
						if (module != null)
							module.EnableTypeDefFindCache = true;
					}
				}
			}

			bool inserted = false;
			if (!cachedAssemblies.ContainsKey(key1)) {
				cachedAssemblies.Add(key1, resolvedAssembly);
				inserted = true;
			}
			if (!cachedAssemblies.ContainsKey(key2)) {
				cachedAssemblies.Add(key2, resolvedAssembly);
				inserted = true;
			}
			if (inserted || asm1 == resolvedAssembly || asm2 == resolvedAssembly)
				return resolvedAssembly;

			// Dupe assembly. Don't insert it.
			var dupeModule = resolvedAssembly.ManifestModule;
			if (dupeModule != null)
				dupeModule.Dispose();
			return asm1 ?? asm2;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		public bool AddToCache(AssemblyDef asm) {
			if (asm == null)
				return false;
			var asmKey = GetAssemblyNameKey(asm);
			AssemblyDef cachedAsm;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (cachedAssemblies.TryGetValue(asmKey, out cachedAsm) && cachedAsm != null)
				return asm == cachedAsm;
			cachedAssemblies[asmKey] = asm;
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		public bool Remove(AssemblyDef asm) {
			if (asm == null)
				return false;
			var asmKey = GetAssemblyNameKey(asm);
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return cachedAssemblies.Remove(asmKey);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		public void Clear() {
			List<AssemblyDef> asms;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			asms = new List<AssemblyDef>(cachedAssemblies.Values);
			cachedAssemblies.Clear();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
			foreach (var asm in asms) {
				if (asm == null)
					continue;
				foreach (var mod in asm.Modules.GetSafeEnumerable())
					mod.Dispose();
			}
		}

		/// <summary>
		/// Gets the cached assemblies in this resolver.
		/// </summary>
		/// <returns>The cached assemblies.</returns>
		public IEnumerable<AssemblyDef> GetCachedAssemblies() {
			return cachedAssemblies.Values;
		}

		static string GetAssemblyNameKey(IAssembly asmName) {
			// Make sure the name contains PublicKeyToken= and not PublicKey=
			return asmName.FullNameToken.ToUpperInvariant();
		}

		AssemblyDef Resolve2(IAssembly assembly, ModuleDef sourceModule) {
			AssemblyDef resolvedAssembly;

			if (cachedAssemblies.TryGetValue(GetAssemblyNameKey(assembly), out resolvedAssembly))
				return resolvedAssembly;

			var moduleContext = defaultModuleContext;
			if (moduleContext == null && sourceModule != null)
				moduleContext = sourceModule.Context;

			resolvedAssembly = FindExactAssembly(assembly, PreFindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, FindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, PostFindAssemblies(assembly, sourceModule, true), moduleContext);
			if (resolvedAssembly != null)
				return resolvedAssembly;

			if (!findExactMatch) {
				resolvedAssembly = FindClosestAssembly(assembly);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PreFindAssemblies(assembly, sourceModule, false), moduleContext);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, FindAssemblies(assembly, sourceModule, false), moduleContext);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PostFindAssemblies(assembly, sourceModule, false), moduleContext);
			}

			return resolvedAssembly;
		}

		/// <summary>
		/// Finds an assembly that exactly matches the requested assembly
		/// </summary>
		/// <param name="assembly">Assembly name to find</param>
		/// <param name="paths">Search paths or <c>null</c> if none</param>
		/// <param name="moduleContext">Module context</param>
		/// <returns>An <see cref="AssemblyDef"/> instance or <c>null</c> if an exact match
		/// couldn't be found.</returns>
		AssemblyDef FindExactAssembly(IAssembly assembly, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths == null)
				return null;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths.GetSafeEnumerable()) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
					var asm = mod.Assembly;
					if (asm != null && asmComparer.Equals(assembly, asm)) {
						mod = null;
						return asm;
					}
				}
				catch {
				}
				finally {
					if (mod != null)
						mod.Dispose();
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the closest assembly from the already cached assemblies
		/// </summary>
		/// <param name="assembly">Assembly name to find</param>
		/// <returns>The closest <see cref="AssemblyDef"/> or <c>null</c> if none found</returns>
		AssemblyDef FindClosestAssembly(IAssembly assembly) {
			AssemblyDef closest = null;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var asm in cachedAssemblies.Values) {
				if (asm == null)
					continue;
				if (asmComparer.CompareClosest(assembly, closest, asm) == 1)
					closest = asm;
			}
			return closest;
		}

		AssemblyDef FindClosestAssembly(IAssembly assembly, AssemblyDef closest, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths == null)
				return closest;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths.GetSafeEnumerable()) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
					var asm = mod.Assembly;
					if (asm != null && asmComparer.CompareClosest(assembly, closest, asm) == 1) {
						if (!IsCached(closest) && closest != null) {
							var closeMod = closest.ManifestModule;
							if (closeMod != null)
								closeMod.Dispose();
						}
						closest = asm;
						mod = null;
					}
				}
				catch {
				}
				finally {
					if (mod != null)
						mod.Dispose();
				}
			}

			return closest;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="asm"/> is inserted in <see cref="cachedAssemblies"/>
		/// </summary>
		/// <param name="asm">Assembly to check</param>
		bool IsCached(AssemblyDef asm) {
			if (asm == null)
				return false;
			AssemblyDef cachedAsm;
			return cachedAssemblies.TryGetValue(GetAssemblyNameKey(asm), out cachedAsm) &&
					cachedAsm == asm;
		}

		IEnumerable<string> FindAssemblies2(IAssembly assembly, IEnumerable<string> paths) {
			if (paths != null) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var ext in assemblyExtensions) {
					foreach (var path in paths.GetSafeEnumerable()) {
						var fullPath = Path.Combine(path, asmSimpleName + ext);
						if (File.Exists(fullPath))
							yield return fullPath;
					}
				}
			}
		}

		/// <summary>
		/// Called before <see cref="FindAssemblies"/>
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PreFindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, preSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="FindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PostFindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, postSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="PreFindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> FindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssembliesGac(assembly, sourceModule, matchExactly))
				yield return path;
			foreach (var path in FindAssembliesModuleSearchPaths(assembly, sourceModule, matchExactly))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGac(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			if (matchExactly)
				return FindAssembliesGacExactly(assembly, sourceModule);
			return FindAssembliesGacAny(assembly, sourceModule);
		}

		IEnumerable<string> FindAssembliesGacExactly(IAssembly assembly, ModuleDef sourceModule) {
			foreach (var path in FindAssembliesGacExactly(gac2Info, assembly, sourceModule))
				yield return path;
			foreach (var path in FindAssembliesGacExactly(gac4Info, assembly, sourceModule))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGacExactly(GacInfo gacInfo, IAssembly assembly, ModuleDef sourceModule) {
			var pkt = PublicKeyBase.ToPublicKeyToken(assembly.PublicKeyOrToken);
			if (gacInfo != null && pkt != null) {
				string pktString = pkt.ToString();
				string verString = Utils.CreateVersionWithNoUndefinedValues(assembly.Version).ToString();
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.subDirs) {
					var baseDir = Path.Combine(gacInfo.path, subDir);
					baseDir = Path.Combine(baseDir, asmSimpleName);
					baseDir = Path.Combine(baseDir, string.Format("{0}{1}__{2}", gacInfo.prefix, verString, pktString));
					var pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
					if (File.Exists(pathName))
						yield return pathName;
				}
			}
		}

		IEnumerable<string> FindAssembliesGacAny(IAssembly assembly, ModuleDef sourceModule) {
			foreach (var path in FindAssembliesGacAny(gac2Info, assembly, sourceModule))
				yield return path;
			foreach (var path in FindAssembliesGacAny(gac4Info, assembly, sourceModule))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGacAny(GacInfo gacInfo, IAssembly assembly, ModuleDef sourceModule) {
			if (gacInfo != null) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.subDirs) {
					var baseDir = Path.Combine(gacInfo.path, subDir);
					baseDir = Path.Combine(baseDir, asmSimpleName);
					foreach (var dir in GetDirs(baseDir)) {
						var pathName = Path.Combine(dir, asmSimpleName + ".dll");
						if (File.Exists(pathName))
							yield return pathName;
					}
				}
			}
		}

		IEnumerable<string> GetDirs(string baseDir) {
			var dirs = new List<string>();
			try {
				foreach (var di in new DirectoryInfo(baseDir).GetDirectories())
					dirs.Add(di.FullName);
			}
			catch {
			}
			return dirs;
		}

		IEnumerable<string> FindAssembliesModuleSearchPaths(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			string asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
			var searchPaths = GetSearchPaths(sourceModule);
			foreach (var ext in assemblyExtensions) {
				foreach (var path in searchPaths.GetSafeEnumerable()) {
					for (int i = 0; i < 2; i++) {
						string path2;
						if (i == 0)
							path2 = Path.Combine(path, asmSimpleName + ext);
						else
							path2 = Path.Combine(Path.Combine(path, asmSimpleName), asmSimpleName + ext);
						if (File.Exists(path2))
							yield return path2;
					}
				}
			}
		}

		/// <summary>
		/// Gets all search paths to use for this module
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of all search paths to use for this module</returns>
		IEnumerable<string> GetSearchPaths(ModuleDef module) {
			ModuleDef keyModule = module;
			if (keyModule == null)
				keyModule = nullModule;
			IList<string> searchPaths;
			if (moduleSearchPaths.TryGetValue(keyModule, out searchPaths))
				return searchPaths;
			moduleSearchPaths[keyModule] = searchPaths = new List<string>(GetModuleSearchPaths(module));
			return searchPaths;
		}

		/// <summary>
		/// Gets all module search paths. This is usually empty unless its assembly has
		/// a <c>.config</c> file specifying any additional private search paths in a
		/// &lt;probing/&gt; element.
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of search paths</returns>
		protected virtual IEnumerable<string> GetModuleSearchPaths(ModuleDef module) {
			return GetModulePrivateSearchPaths(module);
		}

		/// <summary>
		/// Gets all private assembly search paths as found in the module's <c>.config</c> file.
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of search paths</returns>
		protected IEnumerable<string> GetModulePrivateSearchPaths(ModuleDef module) {
			if (module == null)
				return new string[0];
			var asm = module.Assembly;
			if (asm == null)
				return new string[0];
			module = asm.ManifestModule;
			if (module == null)
				return new string[0];	// Should never happen

			string baseDir = null;
			try {
				var imageName = module.Location;
				if (imageName != string.Empty) {
					baseDir = Directory.GetParent(imageName).FullName;
					var configName = imageName + ".config";
					if (File.Exists(configName))
						return GetPrivatePaths(baseDir, configName);
				}
			}
			catch {
			}
			if (baseDir != null)
				return new List<string> { baseDir };
			return new string[0];
		}

		IEnumerable<string> GetPrivatePaths(string baseDir, string configFileName) {
			var searchPaths = new List<string>();

			try {
				var dirName = Path.GetDirectoryName(Path.GetFullPath(configFileName));
				searchPaths.Add(dirName);

				using (var xmlStream = new FileStream(configFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					var doc = new XmlDocument();
					doc.Load(XmlReader.Create(xmlStream));
					foreach (var tmp in doc.GetElementsByTagName("probing")) {
						var probingElem = tmp as XmlElement;
						if (probingElem == null)
							continue;
						var privatePath = probingElem.GetAttribute("privatePath");
						if (string.IsNullOrEmpty(privatePath))
							continue;
						foreach (var tmp2 in privatePath.Split(';')) {
							var path = tmp2.Trim();
							if (path == "")
								continue;
							var newPath = Path.GetFullPath(Path.Combine(dirName, path.Replace('\\', Path.DirectorySeparatorChar)));
							if (Directory.Exists(newPath) && newPath.StartsWith(baseDir + Path.DirectorySeparatorChar))
								searchPaths.Add(newPath);
						}
					}
				}
			}
			catch (ArgumentException) {
			}
			catch (IOException) {
			}
			catch (XmlException) {
			}

			return searchPaths;
		}

		/// <summary>
		/// Add other common search paths
		/// </summary>
		/// <param name="paths">A list that gets updated with the new paths</param>
		protected static void AddOtherSearchPaths(IList<string> paths) {
			AddOtherAssemblySearchPaths(paths, Environment.GetEnvironmentVariable("ProgramFiles"));
			AddOtherAssemblySearchPaths(paths, Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
		}

		static void AddOtherAssemblySearchPaths(IList<string> paths, string path) {
			if (string.IsNullOrEmpty(path))
				return;
			AddSilverlightDirs(paths, Path.Combine(path, @"Microsoft Silverlight"));
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Reference Assemblies");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v3.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v3.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v4.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v4.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v5.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v5.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft.NET\SDK\CompactFramework\v2.0\WindowsCE");
			AddIfExists(paths, path, @"Microsoft.NET\SDK\CompactFramework\v3.5\WindowsCE");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Client");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETCore\v4.5.1");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETCore\v4.5");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETMicroFramework\v3.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETMicroFramework\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETMicroFramework\v4.1");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETMicroFramework\v4.2");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETMicroFramework\v4.3");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETPortable\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETPortable\v4.5");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\.NETPortable\v4.6");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\v3.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\v3.5");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v3.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v5.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETCore\3.3.1.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v2.0\2.3.0.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETPortable\2.3.5.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\.NETPortable\2.3.5.1");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v2.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\.NETPortable");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v2.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\WindowsPowerShell\v1.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\WindowsPowerShell\3.0");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET 2003\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET 2003\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 8\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 8\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 9.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 9.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 10.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 11.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 12.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 12.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v2.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v2.0\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Zune");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Zune");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v4.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v4.0\References\Xbox360");
			AddIfExists(paths, path, @"Windows CE Tools\wce500\Windows Mobile 5.0 Pocket PC SDK\Designtimereferences");
			AddIfExists(paths, path, @"Windows CE Tools\wce500\Windows Mobile 5.0 Smartphone SDK\Designtimereferences");
			AddIfExists(paths, path, @"Windows Mobile 5.0 SDK R2\Managed Libraries");
			AddIfExists(paths, path, @"Windows Mobile 6 SDK\Managed Libraries");
			AddIfExists(paths, path, @"Windows Mobile 6.5.3 DTK\Managed Libraries");
			AddIfExists(paths, path, @"Microsoft SQL Server\90\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft SQL Server\100\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft SQL Server\110\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 2\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 3\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 4\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET Web Pages\v1.0\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET Web Pages\v2.0\Assemblies");
			AddIfExists(paths, path, @"Microsoft SDKs\F#\3.0\Framework\v4.0");
		}

		static void AddSilverlightDirs(IList<string> paths, string basePath) {
			try {
				var di = new DirectoryInfo(basePath);
				foreach (var dir in di.GetDirectories()) {
					if (Regex.IsMatch(dir.Name, @"^\d+(?:\.\d+){3}$"))
						AddIfExists(paths, basePath, dir.Name);
				}
			}
			catch {
			}
		}

		static void AddIfExists(IList<string> paths, string basePath, string extraPath) {
			var path = Path.Combine(basePath, extraPath);
			if (Directory.Exists(path))
				paths.Add(path);
		}
	}
}
