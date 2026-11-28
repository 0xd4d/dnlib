// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;
using FileAttributes = dnlib.DotNet.FileAttributes;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="AssemblyDef"/>: loading, names, finding types, friend assemblies and the target framework
	/// </summary>
	public class AssemblyDefTests {
		const string LibrarySource = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: AssemblyVersion(""1.2.3.4"")]
[assembly: InternalsVisibleTo(""Friend"")]
[assembly: TargetFramework("".NETFramework,Version=v4.6.1,Profile=Client"", FrameworkDisplayName = ""Some name"")]

namespace Ns {
	public class Outer {
		public class Inner { }
	}
}
";
		static readonly Lazy<byte[]> libraryImage = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "Library" }).Image);

		static AssemblyDef LoadLibrary() => AssemblyDef.Load(libraryImage.Value);

		[Fact]
		public void LoadReadsTheAssemblyOfAFile() {
			var fromBytes = AssemblyDef.Load(libraryImage.Value);
			var fromStream = AssemblyDef.Load(new MemoryStream(libraryImage.Value));

			Assert.Equal("Library", fromBytes.Name);
			Assert.Equal(new Version(1, 2, 3, 4), fromBytes.Version);
			Assert.Equal("Library", fromStream.Name);
			Assert.Same(fromBytes, fromBytes.ManifestModule!.Assembly);
			Assert.Same(fromBytes.ManifestModule, Assert.Single(fromBytes.Modules));
		}

		// A netmodule is a module without an assembly
		[Fact]
		public void LoadRejectsAFileWithoutAnAssembly() {
			var netModule = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image;

			Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(netModule));
			using (var module = ModuleDefMD.Load(netModule))
				Assert.Null(module.Assembly);
		}

		[Fact]
		public void FullNames() {
			var assembly = LoadLibrary();
			Assert.Equal("Library, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", assembly.FullName);
			Assert.Equal(assembly.FullName, assembly.GetFullNameWithPublicKeyToken());
			Assert.Equal("Library, Version=1.2.3.4, Culture=neutral, PublicKey=null", assembly.GetFullNameWithPublicKey());
			Assert.Equal(assembly.FullName, assembly.ToString());

			assembly.Culture = "de-DE";
			assembly.PublicKey = new PublicKey(new byte[] { 1, 2, 3 });
			Assert.Equal("Library, Version=1.2.3.4, Culture=de-DE, PublicKey=010203", assembly.GetFullNameWithPublicKey());
			Assert.EndsWith("PublicKeyToken=" + assembly.PublicKeyToken, assembly.FullName);
			Assert.Equal(16, assembly.PublicKeyToken!.ToString().Length);
		}

		[Fact]
		public void TypesAndModulesAreFoundByName() {
			var assembly = LoadLibrary();

			Assert.Equal("Ns.Outer", assembly.Find("Ns.Outer", isReflectionName: true)!.FullName);
			Assert.Equal("Ns.Outer/Inner", assembly.Find("Ns.Outer+Inner", isReflectionName: true)!.FullName);
			Assert.Equal("Ns.Outer/Inner", assembly.Find("Ns.Outer/Inner", isReflectionName: false)!.FullName);
			Assert.Null(assembly.Find("Ns.Missing", isReflectionName: true));

			var module = assembly.ManifestModule!;
			var typeRef = new TypeRefUser(module, "Ns", "Outer", module);
			Assert.Same(assembly.Find("Ns.Outer", isReflectionName: true), assembly.Find(typeRef));
			Assert.Null(assembly.Find((TypeRef?)null));

			Assert.Same(module, assembly.FindModule(module.Name));
			Assert.Same(module, assembly.FindModule(module.Name.String.ToUpperInvariant()));
			Assert.Null(assembly.FindModule("Missing.dll"));
			Assert.Null(assembly.FindModule(null));
		}

		static AssemblyDefUser CreateAssembly(string name) {
			var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
			assembly.Modules.Add(new ModuleDefUser(name + ".dll"));
			return assembly;
		}

		// 'Friend' can use the internal types of 'Library': Library has InternalsVisibleTo("Friend")
		[Fact]
		public void FriendAssembly() {
			var library = LoadLibrary();

			Assert.True(CreateAssembly("Friend").IsFriendAssemblyOf(library));
			Assert.False(CreateAssembly("Stranger").IsFriendAssemblyOf(library));
			Assert.False(library.IsFriendAssemblyOf(CreateAssembly("Friend")));
			Assert.True(library.IsFriendAssemblyOf(library));
			Assert.False(library.IsFriendAssemblyOf(null));
		}

		// A signed assembly is only a friend if the attribute has its public key
		[Fact]
		public void SignedAssemblyIsNotAFriendOfAnUnsignedAssembly() {
			var signedFriend = CreateAssembly("Friend");
			signedFriend.PublicKey = new PublicKey(new byte[] { 1, 2, 3, 4 });

			Assert.False(signedFriend.IsFriendAssemblyOf(LoadLibrary()));
		}

		[Fact]
		public void TargetFrameworkAttributeIsParsed() {
			var library = LoadLibrary();

			Assert.True(library.TryGetOriginalTargetFrameworkAttribute(out var framework, out var version, out var profile));
			Assert.Equal(".NETFramework", framework);
			Assert.Equal(new Version(4, 6, 1, 0), version);
			Assert.Equal("Client", profile);
		}

		[Fact]
		public void AssemblyWithoutATargetFrameworkAttribute() {
			var assembly = AssemblyDef.Load(CSharpCompiler.Compile("public class C { }").Image);

			Assert.False(assembly.TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));
			Assert.False(CreateAssembly("New").TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));
		}

		[Fact]
		public void ProcessorArchitectureAndContentTypeAreBitsOfTheAttributes() {
			var assembly = CreateAssembly("New");
			Assert.True(assembly.IsProcessorArchitectureNone);
			Assert.True(assembly.IsContentTypeDefault);

			assembly.ProcessorArchitecture = AssemblyAttributes.PA_AMD64;
			assembly.ContentType = AssemblyAttributes.ContentType_WindowsRuntime;

			Assert.False(assembly.IsProcessorArchitectureNone);
			Assert.Equal(AssemblyAttributes.PA_AMD64, assembly.ProcessorArchitecture);
			Assert.True(assembly.IsContentTypeWindowsRuntime);
			Assert.Equal(AssemblyAttributes.PA_AMD64 | AssemblyAttributes.ContentType_WindowsRuntime, assembly.Attributes);

			assembly.ProcessorArchitecture = AssemblyAttributes.PA_MSIL;
			Assert.True(assembly.IsProcessorArchitectureMSIL);
			Assert.True(assembly.IsContentTypeWindowsRuntime);
		}

		[Fact]
		public void PublicKeyIsWritten() {
			var assembly = LoadLibrary();
			assembly.PublicKey = new PublicKey(new byte[] { 1, 2, 3, 4 });
			var stream = new MemoryStream();
			assembly.Write(stream);

			var written = AssemblyDef.Load(stream.ToArray());
			Assert.Equal(new byte[] { 1, 2, 3, 4 }, written.PublicKey.Data);
			Assert.Equal(assembly.PublicKeyToken!.ToString(), written.PublicKeyToken!.ToString());
		}

		// An assembly resolver that needs the target framework of the module that has the reference, like
		// AssemblyResolver with UseTargetFrameworkForFrameworkRedirect
		sealed class TargetFrameworkReadingResolver : IAssemblyResolver {
			public int ResolveCalls;

			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) {
				ResolveCalls++;
				sourceModule?.Assembly?.TryGetOriginalTargetFrameworkAttribute(out _, out _, out _);
				return null;
			}
		}

		// Fixed in a606e38c: the attributes of the assembly are read to find the TargetFrameworkAttribute.
		// Reading a generic attribute resolved its type with the assembly resolver, which asked for the target
		// framework again, and so on until the stack overflowed.
		[Fact]
		public void TargetFrameworkIsFoundWithoutResolvingTheTypesOfOtherAttributes() {
			const string source = @"
using System;
using System.Runtime.Versioning;

[assembly: Marker<Uri>]
[assembly: TargetFramework("".NETFramework,Version=v4.8"")]

public class MarkerAttribute<T> : Attribute { }
";
			var resolver = new TargetFrameworkReadingResolver();
			var context = new ModuleContext(resolver);
			context.Resolver = new Resolver(resolver);
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image, context)) {
				Assert.True(module.Assembly!.TryGetOriginalTargetFrameworkAttribute(out var framework, out var version, out _));

				Assert.Equal(".NETFramework", framework);
				Assert.Equal(new Version(4, 8, 0, 0), version);
				Assert.Equal(0, resolver.ResolveCalls);
				Assert.Equal(2, module.Assembly.CustomAttributes.Count(a => a.TypeFullName.Contains("Marker") || a.TypeFullName.Contains("TargetFramework")));
			}
		}

		// An assembly with [assembly: TargetFramework("...")]. It's written and read again: the "original"
		// attribute is the one in the file, not one that was added later.
		static AssemblyDef CreateAssemblyWithTargetFramework(string frameworkName) {
			var assembly = CreateAssembly("WithTargetFramework");
			var module = assembly.ManifestModule!;
			var attributeType = new TypeRefUser(module, "System.Runtime.Versioning", "TargetFrameworkAttribute", module.CorLibTypes.AssemblyRef);
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), attributeType);
			var attribute = new CustomAttribute(constructor);
			attribute.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, new UTF8String(frameworkName)));
			assembly.CustomAttributes.Add(attribute);
			Assert.False(assembly.TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));
			var stream = new MemoryStream();
			module.Write(stream);
			return AssemblyDef.Load(stream.ToArray());
		}

		// The name is "framework,Version=v1.2[.3][,Profile=name]" (what the runtime parses). The version has
		// three numbers, the fourth is always 0.
		[Theory]
		[InlineData(".NETCoreApp,Version=v8.0", ".NETCoreApp", "8.0.0.0", null)]
		[InlineData(".NETFramework,Version=v4.6.1,Profile=Client", ".NETFramework", "4.6.1.0", "Client")]
		[InlineData(".NETStandard,Version=2.0", ".NETStandard", "2.0.0.0", null)]
		[InlineData(" .NETFramework , Version = v4.0 , Profile = ", ".NETFramework", "4.0.0.0", null)]
		[InlineData(".NETFramework,Version=v1.2.3.4", ".NETFramework", "1.2.3.0", null)]
		[InlineData(".NETFramework,Profile=Client,Version=V3.5", ".NETFramework", "3.5.0.0", "Client")]
		public void TargetFrameworkNames(string frameworkName, string expectedFramework, string expectedVersion, string? expectedProfile) {
			var assembly = CreateAssemblyWithTargetFramework(frameworkName);

			Assert.True(assembly.TryGetOriginalTargetFrameworkAttribute(out var framework, out var version, out var profile));

			Assert.Equal(expectedFramework, framework);
			Assert.Equal(new Version(expectedVersion), version);
			Assert.Equal(expectedProfile, profile);
		}

		[Theory]
		[InlineData("")]
		[InlineData(".NETFramework")]
		[InlineData(",Version=v4.0")]
		[InlineData(".NETFramework,Version=vNext")]
		[InlineData(".NETFramework,Version=v4")]
		[InlineData(".NETFramework,Version")]
		[InlineData(".NETFramework,Profile=Client")]
		[InlineData(".NETFramework,Version=v4.0,Profile=Client,Extra=1")]
		public void InvalidTargetFrameworkNames(string frameworkName) =>
			Assert.False(CreateAssemblyWithTargetFramework(frameworkName).TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));

		// The same assembly from a file on disk, and from memory where the file is mapped like the loader does
		[Fact]
		public void LoadFromAFileName() {
			using (var directory = new TempDirectory()) {
				var fileName = directory.GetPath("Library.dll");
				File.WriteAllBytes(fileName, libraryImage.Value);

				var assembly = AssemblyDef.Load(fileName);
				try {
					Assert.Equal("Library", assembly.Name);
					Assert.Equal(fileName, assembly.ManifestModule!.Location);
				}
				finally {
					assembly.ManifestModule!.Dispose();
				}

				var netModuleFileName = directory.GetPath("Module.netmodule");
				File.WriteAllBytes(netModuleFileName, CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image);
				Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(netModuleFileName));
				// The file isn't kept open after the failed load
				File.Delete(netModuleFileName);
				Assert.False(File.Exists(netModuleFileName));
			}
		}

		// An assembly created with the API: a type of another module of the assembly is found through the
		// ExportedType row of the manifest module that names the file of that module
		[Fact]
		public void TypeOfAnotherModuleIsFoundThroughItsExportedType() {
			var assembly = CreateAssembly("Multi");
			var manifestModule = assembly.ManifestModule!;
			var other = new ModuleDefUser("Other.netmodule");
			assembly.Modules.Add(other);
			var type = new TypeDefUser("Ns", "InOther", other.CorLibTypes.Object.TypeDefOrRef);
			other.Types.Add(type);
			var file = new FileDefUser("OTHER.NETMODULE", FileAttributes.ContainsMetadata, new byte[0]);
			var noMetadata = new FileDefUser("data.bin", FileAttributes.ContainsNoMetadata, new byte[0]);
			manifestModule.ExportedTypes.Add(new ExportedTypeUser(manifestModule, 0, "Ns", "InOther", TypeAttributes.Public, file));
			manifestModule.ExportedTypes.Add(new ExportedTypeUser(manifestModule, 0, "Ns", "InData", TypeAttributes.Public, noMetadata));
			manifestModule.ExportedTypes.Add(new ExportedTypeUser(manifestModule, 0, "Ns", "InMissingFile", TypeAttributes.Public,
				new FileDefUser("missing.netmodule", FileAttributes.ContainsMetadata, new byte[0])));

			// The file name is compared without case
			Assert.Same(type, assembly.Find("Ns.InOther", isReflectionName: false));
			Assert.Same(type, assembly.Find("Ns.InOther", isReflectionName: true));
			// A file without metadata or that isn't a module of the assembly has no types
			Assert.Null(assembly.Find("Ns.InData", isReflectionName: false));
			Assert.Null(assembly.Find("Ns.InMissingFile", isReflectionName: false));
		}

		// A module can only be in one assembly. Clear() removes the assembly of every module.
		[Fact]
		public void TheModulesOfAnAssemblyKnowTheirAssembly() {
			var assembly = CreateAssembly("First");
			var second = new ModuleDefUser("Second.netmodule");
			assembly.Modules.Add(second);
			var other = CreateAssembly("Other");

			Assert.Throws<InvalidOperationException>(() => other.Modules.Add(second));
			Assert.Same(assembly, second.Assembly);

			assembly.Modules.Clear();
			Assert.Null(second.Assembly);
			Assert.Empty(assembly.Modules);
		}

		static byte[] CreateKeyPair() {
			using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(2048)) {
				var blob = rsa.ExportCspBlob(includePrivateParameters: true);
				blob[4] = 0x00;
				blob[5] = 0x24;
				return blob;
			}
		}

		// The attribute of the enhanced strong naming (key migration) has the signature key and a counter signature.
		// Only one is kept, and an existing valid one is reused.
		[Fact]
		public void TheSignatureKeyAttributeIsCreatedOrUpdated() {
			var assembly = CreateAssembly("Signed");
			var identityKey = new StrongNameKey(CreateKeyPair());
			var signatureKey = new StrongNameKey(CreateKeyPair());
			var identityPublicKey = new StrongNamePublicKey(identityKey.PublicKey);
			var signaturePublicKey = new StrongNamePublicKey(signatureKey.PublicKey);

			assembly.UpdateOrCreateAssemblySignatureKeyAttribute(identityPublicKey, identityKey, signaturePublicKey);
			var attribute = Assert.Single(assembly.CustomAttributes);
			Assert.Equal("System.Reflection.AssemblySignatureKeyAttribute", attribute.TypeFullName);
			Assert.Equal(signaturePublicKey.ToString(), ((UTF8String)attribute.ConstructorArguments[0].Value!).String);
			var counterSignature = ((UTF8String)attribute.ConstructorArguments[1].Value!).String;
			Assert.False(string.IsNullOrEmpty(counterSignature));

			// Again, with a second attribute that isn't valid (no arguments): the first one is updated, the second is removed
			attribute.NamedArguments.Add(new CANamedArgument(false, assembly.ManifestModule!.CorLibTypes.String, "Extra", new CAArgument(assembly.ManifestModule.CorLibTypes.String, "x")));
			assembly.CustomAttributes.Add(new CustomAttribute(new MemberRefUser(assembly.ManifestModule, ".ctor", MethodSig.CreateInstance(assembly.ManifestModule.CorLibTypes.Void),
				new TypeRefUser(assembly.ManifestModule, "System.Reflection", "AssemblySignatureKeyAttribute"))));
			assembly.UpdateOrCreateAssemblySignatureKeyAttribute(identityPublicKey, identityKey, signaturePublicKey);

			// The thread safe build of dnlib never reuses an attribute
			var updated = Assert.Single(assembly.CustomAttributes);
			if (dnlib.Settings.IsThreadSafe)
				Assert.NotSame(attribute, updated);
			else
				Assert.Same(attribute, updated);
			Assert.Empty(updated.NamedArguments);
			Assert.Equal(counterSignature, ((UTF8String)updated.ConstructorArguments[1].Value!).String);
		}

		// The attribute is only reused if its constructor has two string parameters and it has two arguments
		[Theory]
		[InlineData("no signature")]
		[InlineData("one parameter")]
		[InlineData("first parameter is no string")]
		[InlineData("second parameter is no string")]
		[InlineData("no arguments")]
		public void AnInvalidSignatureKeyAttributeIsReplaced(string what) {
			var assembly = CreateAssembly("Signed");
			var module = assembly.ManifestModule!;
			var types = module.CorLibTypes;
			var attributeType = new TypeRefUser(module, "System.Reflection", "AssemblySignatureKeyAttribute");
			MemberRefUser Ctor(MethodSig? sig) => new MemberRefUser(module, ".ctor", sig, attributeType);
			var invalid = what switch {
				"no signature" => new CustomAttribute(Ctor(null)),
				"one parameter" => new CustomAttribute(Ctor(MethodSig.CreateInstance(types.Void, types.String))),
				"first parameter is no string" => new CustomAttribute(Ctor(MethodSig.CreateInstance(types.Void, types.Int32, types.String))),
				"second parameter is no string" => new CustomAttribute(Ctor(MethodSig.CreateInstance(types.Void, types.String, types.Int32))),
				"no arguments" => new CustomAttribute(Ctor(MethodSig.CreateInstance(types.Void, types.String, types.String))),
				_ => throw new ArgumentException(what),
			};
			assembly.CustomAttributes.Add(invalid);
			var key = new StrongNameKey(CreateKeyPair());
			var publicKey = new StrongNamePublicKey(key.PublicKey);

			assembly.UpdateOrCreateAssemblySignatureKeyAttribute(publicKey, key, publicKey);

			var attribute = Assert.Single(assembly.CustomAttributes);
			Assert.NotSame(invalid, attribute);
			Assert.Equal(2, attribute.ConstructorArguments.Count);
		}

		// Without a module, there's nothing to add the attribute to
		[Fact]
		public void AnAssemblyWithoutAModuleGetsNoSignatureKeyAttribute() {
			var assembly = new AssemblyDefUser("NoModule");
			var key = new StrongNameKey(CreateKeyPair());
			var publicKey = new StrongNamePublicKey(key.PublicKey);

			assembly.UpdateOrCreateAssemblySignatureKeyAttribute(publicKey, key, publicKey);

			Assert.Empty(assembly.CustomAttributes);
		}

		[Fact]
		public void LoadWithAContext() {
			var context = new ModuleContext();
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("Library.dll");
				File.WriteAllBytes(file, libraryImage.Value);

				foreach (var assembly in new[] { AssemblyDef.Load(file, context), AssemblyDef.Load(libraryImage.Value, context), AssemblyDef.Load(new MemoryStream(libraryImage.Value), context) }) {
					Assert.Equal("Library", assembly.Name);
					Assert.Same(context, ((ModuleDefMD)assembly.ManifestModule!).Context);
					assembly.ManifestModule.Dispose();
				}
			}
		}

		// A netmodule has no assembly: loading it from a stream as an assembly fails and closes the module
		[Fact]
		public void LoadingANetModuleFromAStreamFails() {
			var netModule = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image;

			Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(new MemoryStream(netModule)));
		}

		[Fact]
		public void TheAssemblyIsWrittenToAFile() {
			var assembly = CreateAssembly("Written");
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("Written.dll");

				assembly.Write(file);

				using (var module = ModuleDefMD.Load(file))
					Assert.Equal("Written", module.Assembly!.Name);
			}
		}

		[Fact]
		public void TheFullProcessorArchitectureIsSet() {
			var assembly = CreateAssembly("Arch");

			assembly.ProcessorArchitectureFull = AssemblyAttributes.PA_AMD64 | AssemblyAttributes.PA_Specified;
			assembly.Rid = 1;

			Assert.Equal(AssemblyAttributes.PA_AMD64 | AssemblyAttributes.PA_Specified, assembly.ProcessorArchitectureFull);
			Assert.True(assembly.IsProcessorArchitectureX64);
			Assert.Equal(1U, assembly.Rid);
		}
	}
}
