// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// <c>module.CorLibTypes.AssemblyRef</c> is the reference to the assembly with <c>System.Object</c>,
	/// <c>System.Int32</c> etc: the module's "core library". A file doesn't say which of its references that
	/// is, so <see cref="ModuleDefMD"/> looks at their names. A file without a reference to a core library
	/// gets a reference to the mscorlib of the runtime it was compiled for.
	/// </summary>
	public class CoreLibraryReferenceTests {
		static readonly PublicKeyToken frameworkToken = new PublicKeyToken("b77a5c561934e089");
		static readonly PublicKeyToken contractsToken = new PublicKeyToken("b03f5f7f11d50a3a");

		// Creates a module with a class for every reference (the class's base type is in that assembly),
		// writes it and loads it
		static ModuleDefMD CreateModule(string runtimeVersion, params AssemblyRefUser[] references) {
			var module = new ModuleDefUser("Test.dll") { RuntimeVersion = runtimeVersion };
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);
			foreach (var reference in references)
				module.Types.Add(new TypeDefUser("Ns", "ClassOf" + reference.Name + reference.Version.Major, new TypeRefUser(module, "System", "Object", reference)));
			var stream = new MemoryStream();
			module.Write(stream);
			var written = ModuleDefMD.Load(stream.ToArray());
			Assert.Equal((uint)references.Length, written.TablesStream.AssemblyRefTable.Rows);
			return written;
		}

		static AssemblyRefUser Reference(string name, string version, PublicKeyToken token) => new AssemblyRefUser(name, new Version(version), token);

		static string GetCoreLibrary(ModuleDefMD module) {
			var reference = module.CorLibTypes.AssemblyRef;
			return $"{reference.Name} {reference.Version}";
		}

		[Fact]
		public void NewestMscorlibIsTheCoreLibrary() {
			using (var module = CreateModule(MDHeaderRuntimeVersion.MS_CLR_40,
					Reference("mscorlib", "2.0.0.0", frameworkToken), Reference("mscorlib", "4.0.0.0", frameworkToken), Reference("System", "4.0.0.0", frameworkToken)))
				Assert.Equal("mscorlib 4.0.0.0", GetCoreLibrary(module));
		}

		// A .NET Standard library that also references the facade of .NET: System.Runtime comes first
		[Fact]
		public void SystemRuntimeIsPreferredToNetstandardAndMscorlib() {
			using (var module = CreateModule(MDHeaderRuntimeVersion.MS_CLR_40,
					Reference("netstandard", "2.0.0.0", new PublicKeyToken("cc7b13ffcd2ddd51")), Reference("System.Runtime", "4.2.0.0", contractsToken),
					Reference("mscorlib", "9.9.9.9", new PublicKeyToken("0102030405060708"))))
				Assert.Equal("System.Runtime 4.2.0.0", GetCoreLibrary(module));
		}

		[Fact]
		public void SystemPrivateCoreLibIsPreferredToSystemRuntime() {
			using (var module = CreateModule(MDHeaderRuntimeVersion.MS_CLR_40,
					Reference("System.Runtime", "10.0.0.0", contractsToken), Reference("System.Private.CoreLib", "10.0.0.0", new PublicKeyToken("7cec85d7bea7798e"))))
				Assert.Equal("System.Private.CoreLib 10.0.0.0", GetCoreLibrary(module));
		}

		[Fact]
		public void HighestVersionOfTheSameCoreLibraryIsUsed() {
			using (var module = CreateModule(MDHeaderRuntimeVersion.MS_CLR_40,
					Reference("System.Runtime", "4.0.0.0", contractsToken), Reference("System.Runtime", "8.0.0.0", contractsToken), Reference("System.Runtime", "6.0.0.0", contractsToken)))
				Assert.Equal("System.Runtime 8.0.0.0", GetCoreLibrary(module));
		}

		// Without any reference to a core library, it's the mscorlib of the runtime version in the file
		[Theory]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_10, "mscorlib 1.0.3300.0")]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_11, "mscorlib 1.0.5000.0")]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_20, "mscorlib 2.0.0.0")]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_40, "mscorlib 4.0.0.0")]
		[InlineData("an unknown version", "mscorlib 4.0.0.0")]
		public void ModuleWithoutACoreLibraryReferenceGetsTheMscorlibOfItsRuntime(string runtimeVersion, string expected) {
			using (var module = CreateModule(runtimeVersion, Reference("SomeLibrary", "1.0.0.0", frameworkToken))) {
				Assert.Equal(expected, GetCoreLibrary(module));
				// The reference is new: it's not a row of the file
				Assert.Equal(1U, module.TablesStream.AssemblyRefTable.Rows);
				Assert.Equal("System.Int32", module.CorLibTypes.Int32.FullName);
				Assert.Equal("mscorlib", module.CorLibTypes.Int32.DefinitionAssembly!.Name);
			}
		}

		// The core library itself has no reference to a core library
		[Fact]
		public void ModuleThatDefinesSystemObjectIsItsOwnCoreLibrary() {
			var module = new ModuleDefUser("MyCoreLibrary.dll");
			new AssemblyDefUser("MyCoreLibrary", new Version(3, 2, 1, 0)).Modules.Add(module);
			module.Types.Add(new TypeDefUser("System", "Object", null));
			var stream = new MemoryStream();
			module.Write(stream);

			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				Assert.Equal("MyCoreLibrary 3.2.1.0", GetCoreLibrary(written));
				Assert.True(written.IsCoreLibraryModule);
			}
		}
	}
}
