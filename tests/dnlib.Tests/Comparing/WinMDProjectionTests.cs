// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// The CLR shows some types of Windows Runtime metadata (.winmd files) as .NET types: a reference to
	/// <c>Windows.Foundation.Uri</c> in a Windows Runtime assembly is <c>System.Uri</c>, and the <c>Close</c>
	/// method of <c>Windows.Foundation.IClosable</c> is <c>IDisposable.Dispose</c> ("projection").
	/// <see cref="WinMDHelpers"/> converts a type or method to what the CLR shows, and
	/// <see cref="SigComparer"/> compares the converted types unless
	/// <see cref="SigComparerOptions.DontProjectWinMDRefs"/> is set.
	/// </summary>
	public class WinMDProjectionTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");

		public WinMDProjectionTests() => new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);

		// A reference to an assembly that is Windows Runtime metadata
		static AssemblyRefUser WindowsRuntimeAssembly() =>
			new AssemblyRefUser("Windows", new Version(255, 255, 255, 255)) { ContentType = AssemblyAttributes.ContentType_WindowsRuntime };

		TypeRefUser WindowsRuntimeType(string @namespace, string name) => new TypeRefUser(module, @namespace, name, WindowsRuntimeAssembly());

		[Theory]
		[InlineData("Windows.Foundation", "Uri", "System.Uri", "System.Runtime", false)]
		[InlineData("Windows.Foundation", "IClosable", "System.IDisposable", "System.Runtime", false)]
		[InlineData("Windows.Foundation", "DateTime", "System.DateTimeOffset", "System.Runtime", true)]
		[InlineData("Windows.Foundation", "Point", "Windows.Foundation.Point", "System.Runtime.WindowsRuntime", true)]
		[InlineData("Windows.Foundation.Collections", "IVector`1", "System.Collections.Generic.IList`1", "System.Runtime", false)]
		// The other assemblies of projected types
		[InlineData("Windows.Foundation.Numerics", "Vector2", "System.Numerics.Vector2", "System.Numerics.Vectors", true)]
		[InlineData("Windows.UI.Xaml.Input", "ICommand", "System.Windows.Input.ICommand", "System.ObjectModel", false)]
		[InlineData("Windows.Foundation", "EventRegistrationToken", "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "System.Runtime.InteropServices.WindowsRuntime", true)]
		[InlineData("Windows.UI.Xaml", "CornerRadius", "Windows.UI.Xaml.CornerRadius", "System.Runtime.WindowsRuntime.UI.Xaml", true)]
		public void TypeRefIsProjected(string @namespace, string name, string expectedFullName, string expectedAssembly, bool expectedIsValueType) {
			var projected = WinMDHelpers.ToCLR(module, WindowsRuntimeType(@namespace, name), out bool isValueType);

			Assert.NotNull(projected);
			Assert.Equal(expectedFullName, projected.FullName);
			Assert.Equal(expectedAssembly, projected.DefinitionAssembly!.Name);
			Assert.False(projected.DefinitionAssembly.IsContentTypeWindowsRuntime);
			Assert.Equal(expectedIsValueType, isValueType);
			Assert.Same(module, projected.Module);
		}

		[Fact]
		public void OtherTypeRefsAreNotProjected() {
			var notWindowsRuntime = new TypeRefUser(module, "Windows.Foundation", "Uri", new AssemblyRefUser("Windows", new Version(1, 0, 0, 0)));
			var nested = new TypeRefUser(module, "Windows.Foundation", "Uri", WindowsRuntimeType("Windows.Foundation", "Outer"));

			Assert.Null(WinMDHelpers.ToCLR(module, notWindowsRuntime));
			Assert.Null(WinMDHelpers.ToCLR(module, WindowsRuntimeType("Windows.Foundation", "NotAProjectedType")));
			Assert.Null(WinMDHelpers.ToCLR(module, nested));
			Assert.Null(WinMDHelpers.ToCLR(module, (TypeRef?)null));
		}

		// A module of a Windows Runtime assembly with the type Windows.Foundation.IClosable and its method Close
		static ModuleDefUser CreateWindowsRuntimeModule(out TypeDefUser closable, out MethodDefUser close) {
			var winmd = new ModuleDefUser("Windows.winmd");
			var assembly = new AssemblyDefUser("Windows", new Version(255, 255, 255, 255)) { ContentType = AssemblyAttributes.ContentType_WindowsRuntime };
			assembly.Modules.Add(winmd);
			closable = new TypeDefUser("Windows.Foundation", "IClosable") {
				Attributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime,
			};
			winmd.Types.Add(closable);
			close = new MethodDefUser("Close", MethodSig.CreateInstance(winmd.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);
			closable.Methods.Add(close);
			return winmd;
		}

		[Fact]
		public void TypeDefOfAWindowsRuntimeAssemblyIsProjected() {
			var winmd = CreateWindowsRuntimeModule(out var closable, out _);

			var projected = WinMDHelpers.ToCLR(winmd, closable, out bool isValueType);

			Assert.Equal("System.IDisposable", projected!.FullName);
			Assert.False(isValueType);

			// Not without the WindowsRuntime flag of the type, and not in a normal assembly
			closable.Attributes &= ~TypeAttributes.WindowsRuntime;
			Assert.Null(WinMDHelpers.ToCLR(winmd, closable));
			closable.Attributes |= TypeAttributes.WindowsRuntime;
			winmd.Assembly!.ContentType = AssemblyAttributes.ContentType_Default;
			Assert.Null(WinMDHelpers.ToCLR(winmd, closable));
			Assert.Null(WinMDHelpers.ToCLR(winmd, (TypeDef?)null));
		}

		[Fact]
		public void ExportedTypeIsProjected() {
			var exported = new ExportedTypeUser(module, 0, "Windows.Foundation", "Uri", TypeAttributes.Public, WindowsRuntimeAssembly());
			var notProjected = new ExportedTypeUser(module, 0, "Windows.Foundation", "Other", TypeAttributes.Public, WindowsRuntimeAssembly());

			var projected = WinMDHelpers.ToCLR(module, exported);

			Assert.Equal("System.Uri", projected!.FullName);
			Assert.Equal("System.Runtime", Assert.IsAssignableFrom<AssemblyRef>(projected.Implementation).Name);
			Assert.Null(WinMDHelpers.ToCLR(module, notProjected));
			Assert.Null(WinMDHelpers.ToCLR(module, (ExportedType?)null));
		}

		// A signature becomes a class or a value type signature, whatever the .NET type is
		[Fact]
		public void TypeSigIsProjected() {
			var uri = WinMDHelpers.ToCLR(module, new ClassSig(WindowsRuntimeType("Windows.Foundation", "Uri")));
			var dateTime = WinMDHelpers.ToCLR(module, new ClassSig(WindowsRuntimeType("Windows.Foundation", "DateTime")));

			Assert.Equal("System.Uri", Assert.IsType<ClassSig>(uri).FullName);
			Assert.Equal("System.DateTimeOffset", Assert.IsType<ValueTypeSig>(dateTime).FullName);
			Assert.Null(WinMDHelpers.ToCLR(module, module.CorLibTypes.Int32));
			Assert.Null(WinMDHelpers.ToCLR(module, new SZArraySig(new ClassSig(WindowsRuntimeType("Windows.Foundation", "Uri")))));
			Assert.Null(WinMDHelpers.ToCLR(module, new ClassSig(WindowsRuntimeType("Windows.Foundation", "Other"))));
			Assert.Null(WinMDHelpers.ToCLR(module, (TypeSig?)null));
		}

		[Fact]
		public void CloseMethodOfIClosableIsDispose() {
			var signature = MethodSig.CreateInstance(module.CorLibTypes.Void);
			var closeRef = new MemberRefUser(module, "Close", signature, WindowsRuntimeType("Windows.Foundation", "IClosable"));
			var otherMethod = new MemberRefUser(module, "Open", signature, WindowsRuntimeType("Windows.Foundation", "IClosable"));
			var closeOfAnotherType = new MemberRefUser(module, "Close", signature, WindowsRuntimeType("Windows.Foundation", "Uri"));

			var projected = WinMDHelpers.ToCLR(module, closeRef);

			Assert.Equal("System.Void System.IDisposable::Dispose()", projected!.FullName);
			Assert.Null(WinMDHelpers.ToCLR(module, otherMethod));
			Assert.Null(WinMDHelpers.ToCLR(module, closeOfAnotherType));
			Assert.Null(WinMDHelpers.ToCLR(module, (MemberRef?)null));

			var winmd = CreateWindowsRuntimeModule(out _, out var close);
			Assert.Equal("System.Void System.IDisposable::Dispose()", WinMDHelpers.ToCLR(winmd, close)!.FullName);
			close.Name = "Open";
			Assert.Null(WinMDHelpers.ToCLR(winmd, close));
		}

		// The comparer sees the projected type: Windows.Foundation.Uri is System.Uri of System.Runtime
		[Fact]
		public void SigComparerComparesTheProjectedTypes() {
			var windowsUri = WindowsRuntimeType("Windows.Foundation", "Uri");
			var projected = WinMDHelpers.ToCLR(module, windowsUri)!;
			var systemUri = new TypeRefUser(module, "System", "Uri", (AssemblyRef)projected.ResolutionScope!);

			Assert.True(new SigComparer().Equals(windowsUri, systemUri));
			Assert.Equal(new SigComparer().GetHashCode(windowsUri), new SigComparer().GetHashCode(systemUri));
			Assert.True(new SigComparer().Equals(new ClassSig(windowsUri), new ClassSig(systemUri)));

			Assert.False(new SigComparer(SigComparerOptions.DontProjectWinMDRefs).Equals(windowsUri, systemUri));
			Assert.True(new SigComparer(SigComparerOptions.DontProjectWinMDRefs).Equals(windowsUri, WindowsRuntimeType("Windows.Foundation", "Uri")));
		}

		[Fact]
		public void SigComparerComparesCloseWithDispose() {
			var signature = MethodSig.CreateInstance(module.CorLibTypes.Void);
			var closeRef = new MemberRefUser(module, "Close", signature, WindowsRuntimeType("Windows.Foundation", "IClosable"));
			var disposable = WinMDHelpers.ToCLR(module, WindowsRuntimeType("Windows.Foundation", "IClosable"));
			var disposeRef = new MemberRefUser(module, "Dispose", signature, disposable);
			const SigComparerOptions options = SigComparerOptions.CompareMethodFieldDeclaringType;

			Assert.True(new SigComparer(options).Equals(closeRef, disposeRef));
			Assert.Equal(new SigComparer(options).GetHashCode(closeRef), new SigComparer(options).GetHashCode(disposeRef));
			Assert.False(new SigComparer(options | SigComparerOptions.DontProjectWinMDRefs).Equals(closeRef, disposeRef));
		}

		// A module that references System.Runtime 4.0.10.0 and 4.0.20.0, a Windows Runtime assembly that's also
		// called System.Runtime, and System.Runtime with another public key
		static byte[] CreateImageWithReferencesToTheContractAssembly() {
			var created = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(created);
			var contractKey = new PublicKeyToken("b03f5f7f11d50a3a");
			var references = new[] {
				new AssemblyRefUser("System.Runtime", new Version(4, 0, 10, 0), contractKey),
				new AssemblyRefUser("System.Runtime", new Version(4, 0, 20, 0), contractKey),
				new AssemblyRefUser("System.Runtime", new Version(4, 9, 0, 0), contractKey) { ContentType = AssemblyAttributes.ContentType_WindowsRuntime },
				new AssemblyRefUser("System.Runtime", new Version(4, 8, 0, 0), new PublicKeyToken("0123456789abcdef")),
				new AssemblyRefUser("System.Runtime", new Version(6, 0, 0, 0), contractKey),
			};
			var type = new TypeDefUser("Ns", "Type", created.CorLibTypes.Object.TypeDefOrRef);
			created.Types.Add(type);
			for (int i = 0; i < references.Length; i++)
				type.Fields.Add(new FieldDefUser("field" + i, new FieldSig(new ClassSig(new TypeRefUser(created, "Ns", "Type" + i, references[i])))));
			var stream = new System.IO.MemoryStream();
			created.Write(stream);
			return stream.ToArray();
		}

		// The reference to the .NET assembly of a projected type gets the version that the module already uses
		// for that assembly: the highest one, but not a version above 5 (that's not a version of the contract
		// assemblies), a Windows Runtime assembly or an assembly with another public key
		[Fact]
		public void ProjectedAssemblyHasTheVersionOfTheModulesReferenceToIt() {
			using (var read = ModuleDefMD.Load(CreateImageWithReferencesToTheContractAssembly())) {
				var uri = new TypeRefUser(read, "Windows.Foundation", "Uri", WindowsRuntimeAssembly());

				var projected = WinMDHelpers.ToCLR(read, uri)!;

				Assert.Equal("System.Runtime", projected.DefinitionAssembly!.Name);
				Assert.Equal(new Version(4, 0, 20, 0), projected.DefinitionAssembly.Version);
			}
		}

		// If the module's core library is mscorlib, its version is used (.NET Framework 2.0 - 4.x)
		[Fact]
		public void ProjectedAssemblyHasTheVersionOfMscorlib() {
			var oldModule = new ModuleDefUser("Old.dll", null, new AssemblyRefUser("mscorlib", new Version(2, 0, 0, 0), new PublicKeyToken("b77a5c561934e089")));
			var uri = new TypeRefUser(oldModule, "Windows.Foundation", "Uri", WindowsRuntimeAssembly());

			Assert.Equal(new Version(2, 0, 0, 0), WinMDHelpers.ToCLR(oldModule, uri)!.DefinitionAssembly!.Version);
			// No module: the default version
			Assert.Equal(new Version(4, 0, 0, 0), WinMDHelpers.ToCLR(null, uri)!.DefinitionAssembly!.Version);
		}

		// The definition of a projected type in a Windows Runtime module is compared as the projected type: with a
		// reference, an exported type, a definition and a System.Type
		[Fact]
		public void SigComparerComparesTheProjectedDefinitions() {
			var winmd = CreateWindowsRuntimeModule(out var closable, out var close);
			var uri = new TypeDefUser("Windows.Foundation", "Uri") { Attributes = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime };
			winmd.Types.Add(uri);
			var projectedUri = WinMDHelpers.ToCLR(module, WindowsRuntimeType("Windows.Foundation", "Uri"))!;
			var systemUri = new TypeRefUser(module, "System", "Uri", (AssemblyRef)projectedUri.ResolutionScope!);
			var exportedUri = new ExportedTypeUser(module, 0, "System", "Uri", TypeAttributes.Public, (AssemblyRef)projectedUri.ResolutionScope!);
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(uri, systemUri));
			Assert.True(comparer.Equals(uri, exportedUri));
			Assert.True(comparer.Equals((IType)uri, systemUri));
			Assert.Equal(comparer.GetHashCode(systemUri), comparer.GetHashCode(uri));
			// Two projected definitions are equal, also if they're different objects
			var otherWinmd = CreateWindowsRuntimeModule(out _, out _);
			var otherUri = new TypeDefUser("Windows.Foundation", "Uri") { Attributes = uri.Attributes };
			otherWinmd.Types.Add(otherUri);
			Assert.True(comparer.Equals(uri, otherUri));
			Assert.False(new SigComparer(SigComparerOptions.DontProjectWinMDRefs).Equals(uri, systemUri));

			// IClosable.Close() is IDisposable.Dispose()
			const SigComparerOptions options = SigComparerOptions.CompareMethodFieldDeclaringType;
			var disposable = WinMDHelpers.ToCLR(module, WindowsRuntimeType("Windows.Foundation", "IClosable"))!;
			var disposeRef = new MemberRefUser(module, "Dispose", MethodSig.CreateInstance(module.CorLibTypes.Void), disposable);
			Assert.True(new SigComparer(options).Equals(close, disposeRef));
			Assert.True(new SigComparer(options).Equals((IMethod)close, disposeRef));
			Assert.Equal(new SigComparer(options).GetHashCode(disposeRef), new SigComparer(options).GetHashCode(close));
			var otherClose = CreateWindowsRuntimeModule(out _, out var otherCloseMethod);
			Assert.True(new SigComparer(options).Equals(close, otherCloseMethod));
		}

		// The projected definitions are compared with reflection's types and methods too
		[Fact]
		public void SigComparerComparesTheProjectedDefinitionsWithReflection() {
			var winmd = CreateWindowsRuntimeModule(out var closable, out var close);
			var uri = new TypeDefUser("Windows.Foundation", "Uri") { Attributes = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime };
			winmd.Types.Add(uri);
			const SigComparerOptions options = SigComparerOptions.CompareMethodFieldDeclaringType | SigComparerOptions.DontCompareTypeScope;

			Assert.True(new SigComparer(options).Equals(uri, typeof(Uri)));
			Assert.True(new SigComparer(options).Equals(close, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!));
		}

		// Only Close of IClosable is projected: a member of a generic instance (its class is a TypeSpec) is projected
		// only if the generic type is projected to IDisposable, which no generic type is
		[Fact]
		public void MembersOfGenericInstancesAreNotProjected() {
			var signature = MethodSig.CreateInstance(module.CorLibTypes.Void);
			var vectorOfInt = new TypeSpecUser(new GenericInstSig(new ClassSig(WindowsRuntimeType("Windows.Foundation.Collections", "IVector`1")), module.CorLibTypes.Int32));
			var notGeneric = new TypeSpecUser(new SZArraySig(module.CorLibTypes.Int32));
			var otherGeneric = new TypeSpecUser(new GenericInstSig(new ClassSig(WindowsRuntimeType("Windows.Foundation", "NotProjected`1")), module.CorLibTypes.Int32));

			Assert.Null(WinMDHelpers.ToCLR(module, new MemberRefUser(module, "Close", signature, vectorOfInt)));
			Assert.Null(WinMDHelpers.ToCLR(module, new MemberRefUser(module, "Close", signature, notGeneric)));
			Assert.Null(WinMDHelpers.ToCLR(module, new MemberRefUser(module, "Close", signature, otherGeneric)));
			Assert.Null(WinMDHelpers.ToCLR(module, (MemberRef?)null));
			Assert.Null(WinMDHelpers.ToCLR(module, (MethodDef?)null));
			Assert.Null(WinMDHelpers.ToCLR(module, (ExportedType?)null));
			Assert.Null(WinMDHelpers.ToCLR(module, (TypeDef?)null));
		}
	}
}
