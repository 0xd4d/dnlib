// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A reference to a type (a row of the TypeRef table) has a name, a namespace and a resolution scope that
	/// says where the type is: in another assembly (AssemblyRef), in this module (Module), in another module
	/// (ModuleRef), nested in another referenced type (TypeRef), or nothing (the type is then looked up in
	/// the ExportedType table). These tests write scopes and names that a compiler doesn't write and read
	/// the reference with dnlib. The runtime that runs the tests is asked for the type of a field that uses
	/// the reference. If it finds a type, dnlib must have a reference with that name in that scope.
	/// </summary>
	public class ClrTypeRefScopeRuleTests {
		// The type of F is the TypeRef that the tests change. Local, Nested and SpecialFolder are there so that
		// the file has their names, and the forwarder so that the file has an exported type.
		const string Source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Text.Encoder))]
namespace Ns {
	public class Local {
		public class Nested { }
	}
}
public class C {
	public System.Version F;
	public System.Environment.SpecialFolder G;
	public System.Text.Decoder H;
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "Column = value", separated by "; ": a column of the TypeRef row of System.Version (ResolutionScope,
		// Name, Namespace). A value is a number, "name of X" / "namespace of X" (of the type or the TypeRef
		// called X), and for the scope: "module", "TypeRef X" or "TypeRef itself".
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			uint row = patcher.FindRow(Table.TypeRef, "Name", "Version");
			// The values are read from the file as it was compiled
			var original = new MetadataPatcher(baseImage);
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				string column = parts[0], value = parts[1];
				uint cell;
				if (char.IsDigit(value[0]))
					cell = uint.Parse(value);
				// The scope is a coded token: the row number and 0 = Module, 1 = ModuleRef, 2 = AssemblyRef,
				// 3 = TypeRef
				else if (value == "module")
					cell = 1 << 2;
				else if (value == "TypeRef itself")
					cell = (row << 2) | 3;
				else if (value.StartsWith("TypeRef "))
					cell = (original.FindRow(Table.TypeRef, "Name", value.Substring(8)) << 2) | 3;
				else if (value == "name of Encoder" || value == "namespace of Encoder")
					cell = original.GetCell(Table.ExportedType, 1, column == "Name" ? "TypeName" : "TypeNamespace");
				else if (value == "name of SpecialFolder" || value == "name of Decoder" || value == "namespace of Decoder")
					cell = original.GetCell(Table.TypeRef, original.FindRow(Table.TypeRef, "Name", value.Substring(value.LastIndexOf(' ') + 1)), column);
				else
					cell = original.GetCell(Table.TypeDef, original.FindRow(Table.TypeDef, "Name", value.Substring(value.LastIndexOf(' ') + 1)), column);
				patcher.SetCell(Table.TypeRef, row, column, cell);
			}
			return patcher.ToArray();
		}

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image, ModuleDef.CreateModuleContext())) {
				var typeRef = (TypeRef)module.Find("C", false)!.FindField("F")!.FieldSig!.Type.ToTypeDefOrRef()!;
				var scope = typeRef.ResolutionScope;
				string scopeText = scope is null ? "nothing" :
					scope is AssemblyRef ? "the core library" :
					scope is ModuleDef ? "this module" :
					scope is TypeRef enclosing ? "TypeRef " + enclosing.Name :
					scope.GetType().Name;
				// The type that the reference is resolved to, if it's a type of this module
				var resolved = typeRef.Resolve();
				string resolvedText = resolved?.Module == module ? ", found in this module" : "";
				return $"{typeRef.Namespace}.{typeRef.Name} in {scopeText}{resolvedText}";
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var type = loaded.Assembly.GetType("C")!.GetField("F")!.FieldType;
				return type.FullName + " in " + (type.Assembly == loaded.Assembly ? "this module" : type.Assembly == typeof(object).Assembly ? "the core library" : type.Assembly.GetName().Name);
			}
		}

		[Theory]
		[InlineData("", "System.Version in the core library", "System.Version in the core library")]
		// A type of this module
		[InlineData("ResolutionScope = module; Name = name of Local; Namespace = namespace of Local", "Ns.Local in this module, found in this module", "Ns.Local in this module")]
		[InlineData("ResolutionScope = module", "System.Version in this module", "throws TypeLoadException")]
		[InlineData("Name = name of Local; Namespace = namespace of Local", "Ns.Local in the core library", "throws TypeLoadException")]
		[InlineData("ResolutionScope = 0; Name = name of Local; Namespace = namespace of Local", "Ns.Local in nothing, found in this module", "Ns.Local in this module")]
		[InlineData("ResolutionScope = 8; Name = name of Local; Namespace = namespace of Local", "Ns.Local in nothing, found in this module", "throws BadImageFormatException")]
		// No scope: the type is an exported type (here a forwarder to System.Text.Encoder)
		// (.NET 10 has System.Void as the type of the field if the type isn't found there)
		[InlineData("ResolutionScope = 0", "System.Version in nothing", "System.Void in the core library")]
		[InlineData("ResolutionScope = 0; Name = name of Encoder; Namespace = namespace of Encoder", "System.Text.Encoder in nothing", "System.Text.Encoder in the core library")]
		[InlineData("ResolutionScope = module; Name = name of Encoder; Namespace = namespace of Encoder", "System.Text.Encoder in this module", "System.Text.Encoder in the core library")]
		// Another type of the other assembly
		[InlineData("Name = name of Decoder; Namespace = namespace of Decoder", "System.Text.Decoder in the core library", "System.Text.Decoder in the core library")]
		[InlineData("Name = name of Decoder", "System.Decoder in the core library", "throws TypeLoadException")]
		[InlineData("Name = 0", "System. in the core library", "throws TypeLoadException")]
		[InlineData("Namespace = 0", ".Version in the core library", "throws TypeLoadException")]
		// A nested type: the scope is the TypeRef of the enclosing type
		[InlineData("ResolutionScope = TypeRef Environment; Name = name of SpecialFolder; Namespace = 0", ".SpecialFolder in TypeRef Environment", "throws TypeLoadException")]
		[InlineData("ResolutionScope = TypeRef Environment; Name = name of SpecialFolder", "System.SpecialFolder in TypeRef Environment", "throws TypeLoadException")]
		[InlineData("ResolutionScope = TypeRef Environment", "System.Version in TypeRef Environment", "throws TypeLoadException")]
		[InlineData("ResolutionScope = TypeRef itself", "System.Version in TypeRef Version", "throws BadImageFormatException")]
		[InlineData("ResolutionScope = 3", "System.Version in nothing", "System.Void in the core library")]
		[InlineData("ResolutionScope = 4003", "System.Version in nothing", "throws BadImageFormatException")]
		// Other scopes: a ModuleRef (the file has none), an AssemblyRef that doesn't exist
		[InlineData("ResolutionScope = 1", "System.Version in nothing", "System.Void in the core library")]
		[InlineData("ResolutionScope = 5", "System.Version in nothing", "throws BadImageFormatException")]
		[InlineData("ResolutionScope = 2", "System.Version in nothing", "System.Void in the core library")]
		[InlineData("ResolutionScope = 4002", "System.Version in nothing", "throws BadImageFormatException")]
		public void TypeOfAReference(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			// Only .NET is asked: the test run on .NET Framework never ended when these tests were added, and which
			// of their cases it doesn't return from isn't known yet
			ClrReaderComparison.AssertReadAskOnlyDotNet(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
