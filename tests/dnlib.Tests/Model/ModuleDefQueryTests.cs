// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The methods of <see cref="ModuleDef"/> that list or find the references of a module (assemblies, modules,
	/// types, members), and a few of its other small helpers.
	/// </summary>
	public class ModuleDefQueryTests {
		// A module that references two versions of the assembly Library, the module Other.netmodule and Native.dll:
		//   class Test : [Library 1.0.0.0]Ns.Base {
		//       [Library 2.0.0.0]Ns.Newer field1;
		//       [.module Other.netmodule]Ns.InModule field2;
		//       static void Method() { call void [.module Native.dll]::NativeFunction() }
		//   }
		static byte[] CreateImage() {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);
			var oldLibrary = new AssemblyRefUser("Library", new Version(1, 0, 0, 0));
			var newLibrary = new AssemblyRefUser("Library", new Version(2, 0, 0, 0));
			var type = new TypeDefUser("Ns", "Test", new TypeRefUser(module, "Ns", "Base", oldLibrary));
			module.Types.Add(type);
			type.Fields.Add(new FieldDefUser("field1", new FieldSig(new ClassSig(new TypeRefUser(module, "Ns", "Newer", newLibrary)))));
			type.Fields.Add(new FieldDefUser("field2", new FieldSig(new ClassSig(new TypeRefUser(module, "Ns", "InModule", new ModuleRefUser(module, "Other.netmodule"))))));
			var method = new MethodDefUser("Method", MethodSig.CreateStatic(module.CorLibTypes.Void), MethodAttributes.Static);
			type.Methods.Add(method);
			method.Body = new dnlib.DotNet.Emit.CilBody();
			var nativeFunction = new MemberRefUser(module, "NativeFunction", MethodSig.CreateStatic(module.CorLibTypes.Void), new ModuleRefUser(module, "Native.dll"));
			method.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Call.ToInstruction(nativeFunction));
			method.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());

			var stream = new MemoryStream();
			module.Write(stream);
			return stream.ToArray();
		}

		static readonly Lazy<byte[]> image = new Lazy<byte[]>(CreateImage);

		[Fact]
		public void AllReferencesOfEveryKindAreReturned() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var tables = module.TablesStream;

				Assert.Equal(new[] { "Library 1.0.0.0", "Library 2.0.0.0" },
					module.GetAssemblyRefs().Select(a => a.Name + " " + a.Version).OrderBy(a => a, StringComparer.Ordinal));
				Assert.Equal(new[] { "Native.dll", "Other.netmodule" }, module.GetModuleRefs().Select(a => a.Name.String).OrderBy(a => a, StringComparer.Ordinal));
				Assert.Equal(new[] { "Ns.Base", "Ns.InModule", "Ns.Newer" }, module.GetTypeRefs().Select(a => a.FullName).OrderBy(a => a, StringComparer.Ordinal));
				Assert.Equal(new[] { "NativeFunction" }, module.GetMemberRefs().Select(a => a.Name.String));
				// Every row of the tables
				Assert.Equal((int)tables.AssemblyRefTable.Rows, module.GetAssemblyRefs().Count());
				Assert.Equal((int)tables.ModuleRefTable.Rows, module.GetModuleRefs().Count());
				Assert.Equal((int)tables.TypeRefTable.Rows, module.GetTypeRefs().Count());
				Assert.Equal((int)tables.MemberRefTable.Rows, module.GetMemberRefs().Count());
			}
		}

		[Fact]
		public void AssemblyReferenceWithTheHighestVersionIsFoundByName() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				Assert.Equal(new Version(2, 0, 0, 0), module.GetAssemblyRef("Library")!.Version);
				Assert.Null(module.GetAssemblyRef("library"));
				Assert.Null(module.GetAssemblyRef("Missing"));
				Assert.Null(module.GetAssemblyRef(null));
			}
		}

		// Fixed in this commit (issue 349): a version number that is 65535 has no value. The CLR then takes an
		// assembly with any number there, so such a reference isn't the one with the highest version: the
		// number and the numbers after it are compared as 0.
		[Theory]
		[InlineData("65535.65535.65535.65535", "2.0.0.0")]
		[InlineData("65535.0.0.0", "2.0.0.0")]
		[InlineData("2.65535.65535.65535", "2.0.0.0")]
		[InlineData("2.0.65535.9", "2.0.0.0")]
		[InlineData("3.65535.65535.65535", "3.65535.65535.65535")]
		[InlineData("2.1.65535.65535", "2.1.65535.65535")]
		[InlineData("2.0.0.65535", "2.0.0.0")]
		[InlineData("2.0.1.65535", "2.0.1.65535")]
		public void VersionNumberWithoutValueIsNotTheHighest(string versionOfTheFirst, string expectedVersion) {
			var patcher = new MetadataPatcher(image.Value);
			uint rid;
			using (var module = ModuleDefMD.Load(image.Value))
				rid = module.GetAssemblyRefs().Single(a => a.Name == "Library" && a.Version.Major == 1).Rid;
			var numbers = versionOfTheFirst.Split('.').Select(uint.Parse).ToArray();
			patcher.SetCell(Table.AssemblyRef, rid, "MajorVersion", numbers[0]);
			patcher.SetCell(Table.AssemblyRef, rid, "MinorVersion", numbers[1]);
			patcher.SetCell(Table.AssemblyRef, rid, "BuildNumber", numbers[2]);
			patcher.SetCell(Table.AssemblyRef, rid, "RevisionNumber", numbers[3]);

			using (var module = ModuleDefMD.Load(patcher.ToArray()))
				Assert.Equal(expectedVersion, module.GetAssemblyRef("Library")!.Version.ToString());
		}

		// A module that isn't read from a file has no tables to list
		[Fact]
		public void NewModuleReturnsNoReferences() {
			var module = new ModuleDefUser("Test.dll");

			Assert.Empty(module.GetAssemblyRefs());
			Assert.Empty(module.GetModuleRefs());
			Assert.Empty(module.GetTypeRefs());
			Assert.Empty(module.GetMemberRefs());
			Assert.Null(module.GetAssemblyRef("mscorlib"));
		}

		[Fact]
		public void TypeOfTheModuleIsFoundFromAnyFormOfAReferenceToIt() {
			using (var module = ModuleDefMD.Load(image.Value))
			using (var otherModule = ModuleDefMD.Load(image.Value)) {
				var type = module.Find("Ns.Test", isReflectionName: true)!;
				var typeRef = new TypeRefUser(module, "Ns", "Test", module);

				Assert.Same(type, module.Find((ITypeDefOrRef)type));
				Assert.Same(type, module.Find((ITypeDefOrRef)typeRef));
				Assert.Same(type, module.Find(new TypeSpecUser(new ClassSig(type))));
				Assert.Same(type, module.Find(new TypeSpecUser(new ClassSig(typeRef))));

				// A type of another module, a type that's made of the type, and no type
				Assert.Null(module.Find((ITypeDefOrRef)otherModule.Find("Ns.Test", isReflectionName: true)!));
				Assert.Null(module.Find(new TypeSpecUser(new ClassSig(otherModule.Find("Ns.Test", isReflectionName: true)))));
				Assert.Null(module.Find(new TypeSpecUser(new SZArraySig(new ClassSig(type)))));
				Assert.Null(module.Find(new TypeSpecUser()));
				Assert.Null(module.Find((ITypeDefOrRef?)null));
				Assert.Null(module.Find((ITypeDefOrRef)new TypeRefUser(module, "Ns", "Missing", module)));
			}
		}

		// The metadata version string of a Windows Runtime file (.winmd) has two parts
		[Theory]
		[InlineData("WindowsRuntime 1.4;CLR v4.0.30319", "WindowsRuntime 1.4", "v4.0.30319")]
		[InlineData("WindowsRuntime 1.4;clr  v4.0.30319", "WindowsRuntime 1.4", "v4.0.30319")]
		[InlineData("WindowsRuntime 1.4;v4.0.30319", "WindowsRuntime 1.4", "v4.0.30319")]
		[InlineData("WindowsRuntime 1.4", "WindowsRuntime 1.4", null)]
		[InlineData("v4.0.30319", null, null)]
		[InlineData("windowsruntime 1.4;CLR v4.0.30319", null, null)]
		public void WinMDVersionStrings(string runtimeVersion, string? expectedWinMDVersion, string? expectedRuntimeVersion) {
			var module = new ModuleDefUser("Test.winmd") { RuntimeVersion = runtimeVersion };

			Assert.Equal(expectedWinMDVersion, module.WinMDVersion);
			Assert.Equal(expectedRuntimeVersion, module.RuntimeVersionWinMD);
		}

		[Fact]
		public void CreatedModuleContextHasAResolverThatUsesIt() {
			var context = ModuleDef.CreateModuleContext();

			var assemblyResolver = Assert.IsType<AssemblyResolver>(context.AssemblyResolver);
			Assert.IsType<Resolver>(context.Resolver);
			Assert.Same(context, assemblyResolver.DefaultModuleContext);
			// Each call creates a new one
			Assert.NotSame(context, ModuleDef.CreateModuleContext());
		}
	}
}
