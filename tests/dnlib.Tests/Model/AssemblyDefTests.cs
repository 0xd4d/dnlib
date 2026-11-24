// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

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
	}
}
