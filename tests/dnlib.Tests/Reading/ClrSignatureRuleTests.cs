// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Field signatures that a compiler doesn't write: array shapes with sizes, tokens of the wrong kind,
	/// element types that aren't types, bytes after the end. Every test writes the bytes of a signature to
	/// a file and reads the type of the field with dnlib and with the runtime that runs the tests. If the
	/// runtime reads a type, dnlib must read the same type. If it doesn't, dnlib reads what is there or has
	/// no type (null).
	/// </summary>
	public class ClrSignatureRuleTests {
		const string Source = @"
public static class Marker {
	public static int Field;
	public static System.Collections.Generic.List<int> List;
	public static System.DateTime Date;
	public static void Method(int a, string b) { }
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint fieldRid, methodRid;
		static readonly string objectToken, listToken, dateTimeToken, markerToken;

		static ClrSignatureRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				fieldRid = type.FindField("Field")!.Rid;
				methodRid = type.FindMethod("Method")!.Rid;
				objectToken = GetCodedToken(module.GetTypeRefs().Single(a => a.FullName == "System.Object"));
				listToken = GetCodedToken(module.GetTypeRefs().Single(a => a.Name == "List`1"));
				dateTimeToken = GetCodedToken(module.GetTypeRefs().Single(a => a.Name == "DateTime"));
				markerToken = GetCodedToken(type);
			}
		}

		// The token of a type in a signature: the row and (in the 2 low bits) the table. It's one byte here.
		static string GetCodedToken(ITypeDefOrRef type) {
			uint token = type.Rid << 2 | (type is TypeDef ? 0U : 1);
			Assert.True(token < 0x80);
			return token.ToString("X2");
		}

		// The bytes as hex numbers. {Object}, {List}, {DateTime} and {Marker} are the tokens of these types.
		static byte[] ParseSignature(string signature) =>
			signature.Replace("{Object}", objectToken).Replace("{List}", listToken).Replace("{DateTime}", dateTimeToken).Replace("{Marker}", markerToken)
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();

		// Adds the signature to the end of the #Blob heap. The field or the method then has that signature.
		static byte[] CreateImage(string signature, Table table = Table.Field) {
			var bytes = ParseSignature(signature);
			Assert.True(bytes.Length < 0x80);
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var patcher = new MetadataPatcher(baseImage);
			if (table == Table.Field)
				patcher.SetCell(Table.Field, fieldRid, "Signature", (uint)heapLength);
			else {
				// The method is the last one. It gets no rows of the Param table (the names of the parameters):
				// System.Reflection doesn't return the parameters if the rows aren't the ones of the signature.
				Assert.Equal(methodRid, patcher.GetRowCount(Table.Method));
				patcher.SetCell(Table.Method, methodRid, "Signature", (uint)heapLength);
				patcher.SetCell(Table.Method, methodRid, "ParamList", patcher.GetRowCount(Table.Param) + 1);
			}
			var block = new MetadataBlock(patcher.ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		// "List`1[[System.Int32, mscorlib, Version=...]]" is "List`1[[System.Int32]]"
		static string WithoutAssemblyNames(string typeName) => Regex.Replace(typeName, @", [\w.]+, Version=[\d.]+, Culture=neutral, PublicKeyToken=\w+", "");

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var field = module.ResolveField(fieldRid)!;
				if (field.FieldSig is null)
					return "no signature";
				var type = field.FieldSig.Type;
				// The reflection name of a function pointer doesn't show its signature
				if (type is FnPtrSig)
					return type.FullName;
				return WithoutAssemblyNames(type?.ReflectionFullName ?? "no type");
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var field = loaded.Assembly.ManifestModule.ResolveField(new MDToken(Table.Field, fieldRid).ToInt32())!;
					var type = field.FieldType;
					// A function pointer has no full name
					return WithoutAssemblyNames(type.FullName ?? type.ToString());
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// The first byte of a field signature is 6
		[Theory]
		[InlineData("06 08", "System.Int32", "System.Int32")]
		// The calling convention byte
		[InlineData("26 08", "System.Int32", "throws BadImageFormatException")]
		[InlineData("86 08", "System.Int32", "throws BadImageFormatException")]
		[InlineData("16 08", "System.Int32", "throws BadImageFormatException")]
		[InlineData("00 08", "no signature", "throws COMException")]
		[InlineData("07 08", "no signature", "throws COMException")]
		[InlineData("06", "no signature", "throws COMException")]
		// Bytes after the type
		[InlineData("06 08 0E", "System.Int32", "System.Int32")]
		// Simple types
		[InlineData("06 01", "System.Void", "throws COMException")]
		[InlineData("06 0E", "System.String", "System.String")]
		[InlineData("06 16", "System.TypedReference", "System.TypedReference")]
		[InlineData("06 18", "System.IntPtr", "System.IntPtr")]
		[InlineData("06 1C", "System.Object", "System.Object")]
		// Bytes that aren't a type
		[InlineData("06 00", "no type", "throws COMException")]
		[InlineData("06 17", "no signature", "throws COMException")]
		[InlineData("06 1A", "no type", "throws COMException")]
		[InlineData("06 21", "no signature", "throws COMException")]
		[InlineData("06 41", "no signature", "throws COMException")]
		[InlineData("06 FF", "no type", "throws COMException")]
		// Pointers, references, arrays
		[InlineData("06 0F 08", "System.Int32*", "System.Int32*")]
		[InlineData("06 0F 01", "System.Void*", "System.Void*")]
		[InlineData("06 0F 0F 08", "System.Int32**", "System.Int32**")]
		[InlineData("06 10 08", "System.Int32&", "throws TypeLoadException")]
		[InlineData("06 1D 08", "System.Int32[]", "System.Int32[]")]
		[InlineData("06 1D 1D 08", "System.Int32[][]", "System.Int32[][]")]
		[InlineData("06 1D 01", "System.Void[]", "throws COMException")]
		[InlineData("06 1D 10 08", "System.Int32&[]", "throws TypeLoadException")]
		[InlineData("06 45 08", "System.Int32", "throws TypeLoadException")]
		// Arrays with a shape: the rank, the number of sizes and the sizes, the number of lower bounds and
		// the lower bounds
		[InlineData("06 14 08 01 00 00", "System.Int32[*]", "System.Int32[*]")]
		[InlineData("06 14 08 02 00 00", "System.Int32[,]", "System.Int32[,]")]
		[InlineData("06 14 08 02 02 03 04 02 00 00", "System.Int32[,]", "System.Int32[,]")]
		[InlineData("06 14 08 02 01 05 01 02", "System.Int32[,]", "System.Int32[,]")]
		[InlineData("06 14 08 01 02 05 06 00", "System.Int32[*]", "System.Int32[*]")]
		[InlineData("06 14 08 01 00 02 01 01", "System.Int32[*]", "System.Int32[*]")]
		[InlineData("06 14 08 00 00 00", "System.Int32[<RANK0>]", "throws BadImageFormatException")]
		[InlineData("06 14 08 20 00 00", "System.Int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]", "System.Int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]")]
		[InlineData("06 14 08 21 00 00", "System.Int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]", "throws TypeLoadException")]
		[InlineData("06 14 08 02 00", "System.Int32[,]", "throws COMException")]
		[InlineData("06 14 08 02", "System.Int32[,]", "throws COMException")]
		// Classes and value types
		[InlineData("06 12 {Object}", "System.Object", "System.Object")]
		[InlineData("06 11 {Object}", "System.Object", "throws TypeLoadException")]
		[InlineData("06 11 {DateTime}", "System.DateTime", "System.DateTime")]
		[InlineData("06 12 {DateTime}", "System.DateTime", "throws TypeLoadException")]
		[InlineData("06 12 {Marker}", "Marker", "Marker")]
		[InlineData("06 12 00", "<<<NULL>>>", "throws COMException")]
		[InlineData("06 12 03", "<<<NULL>>>", "throws COMException")]
		[InlineData("06 12 02", "<<<NULL>>>", "throws COMException")]
		[InlineData("06 12 7D", "<<<NULL>>>", "throws COMException")]
		[InlineData("06 12 80 {Object}", "System.Object", "System.Object")]
		[InlineData("06 12", "<<<NULL>>>", "throws COMException")]
		// Modifiers
		[InlineData("06 20 {Object} 08", "System.Int32", "System.Int32")]
		[InlineData("06 1F {Object} 08", "System.Int32", "System.Int32")]
		[InlineData("06 20 {Object} 1F {DateTime} 08", "System.Int32", "System.Int32")]
		[InlineData("06 1D 20 {Object} 08", "System.Int32[]", "System.Int32[]")]
		[InlineData("06 20 00 08", "System.Int32", "throws COMException")]
		// Generic types: the type, the number of arguments and the arguments
		[InlineData("06 15 12 {List} 01 08", "System.Collections.Generic.List`1[[System.Int32]]", "System.Collections.Generic.List`1[[System.Int32]]")]
		[InlineData("06 15 11 {List} 01 08", "System.Collections.Generic.List`1[[System.Int32]]", "throws TypeLoadException")]
		[InlineData("06 15 12 {List} 01 15 12 {List} 01 0E", "System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.String]]]]", "System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.String]]]]")]
		[InlineData("06 15 12 {List} 00", "System.Collections.Generic.List`1[]", "throws TypeLoadException")]
		[InlineData("06 15 12 {List} 02 08 08", "System.Collections.Generic.List`1[[System.Int32],[System.Int32]]", "throws TypeLoadException")]
		[InlineData("06 15 12 {Object} 01 08", "System.Object[[System.Int32]]", "throws TypeLoadException")]
		[InlineData("06 15 08 01 08", "no type", "throws BadImageFormatException")]
		[InlineData("06 15 12 {List} 01 01", "System.Collections.Generic.List`1[[System.Void]]", "throws COMException")]
		[InlineData("06 15 12 {List} 01 0F 08", "System.Collections.Generic.List`1[[System.Int32*]]", "throws TypeLoadException")]
		[InlineData("06 15 12 {List} 01 10 08", "System.Collections.Generic.List`1[[System.Int32&]]", "throws TypeLoadException")]
		// Generic parameters: the type has none
		[InlineData("06 13 00", "", "throws TypeLoadException")]
		[InlineData("06 1E 00", "", "throws TypeLoadException")]
		// Function pointers: a method signature follows
		[InlineData("06 1B 00 00 01", "System.Void ()", "System.Void()")]
		[InlineData("06 1B 00 01 08 0E", "System.Int32 (System.String)", "System.Int32(System.String)")]
		[InlineData("06 1B 06 08", "<<<NULL>>>", "throws COMException")]
		public void FieldType(string signature, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(signature);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}

		// "return type(parameter type,parameter type)"
		static string ReadMethodWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var sig = module.ResolveMethod(methodRid)!.MethodSig;
				if (sig is null)
					return "no signature";
				// A generic parameter that doesn't exist has no reflection name: "!!0" is its dnlib name
				string Name(TypeSig? type) => type is null ? "no type" : type is GenericSig ? type.FullName : WithoutAssemblyNames(type.ReflectionFullName);
				var parameters = sig.Params.Select(Name);
				if (sig.ParamsAfterSentinel is not null)
					parameters = parameters.Concat(new[] { "..." }).Concat(sig.ParamsAfterSentinel.Select(Name));
				return $"{Name(sig.RetType)}({string.Join(",", parameters)})";
			}
		}

		static string ReadMethodWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var method = (System.Reflection.MethodInfo)loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!;
					string Name(Type type) => WithoutAssemblyNames(type.FullName ?? type.ToString());
					return $"{Name(method.ReturnType)}({string.Join(",", method.GetParameters().Select(a => Name(a.ParameterType)))})";
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// A method signature: the calling convention (0: a static method), the number of parameters, the
		// return type and the types of the parameters
		//
		// "...": dnlib has the parameters after byte 41 in MethodSig.ParamsAfterSentinel, System.Reflection
		// shows them like the other parameters
		[Theory]
		[InlineData("00 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		// The calling convention byte: 20 = has a this parameter, 40 = the this parameter is in the
		// signature, 10 = generic (the number of generic parameters follows), 1-5 and 9 = kinds of native
		// methods and methods with a variable number of arguments
		[InlineData("20 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("60 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("40 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("01 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("02 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("05 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("05 02 01 08 41 0E", "System.Void(System.Int32,...,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("09 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("0B 02 01 08 0E", "System.Void(System.Int32,System.String)", "throws TypeLoadException")]
		[InlineData("0F 02 01 08 0E", "no signature", "throws TypeLoadException")]
		[InlineData("80 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("10 00 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("10 01 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("06 08", "no signature", "throws NullReferenceException")]
		[InlineData("07 02 01 08 0E", "no signature", "throws TypeLoadException")]
		// The number of parameters
		[InlineData("00 00 01", "System.Void()", "System.Void()")]
		[InlineData("00 01 01 08 0E", "System.Void(System.Int32)", "System.Void(System.Int32)")]
		[InlineData("00 03 01 08 0E", "no signature", "throws BadImageFormatException")]
		[InlineData("00 80 02 01 08 0E", "System.Void(System.Int32,System.String)", "System.Void(System.Int32,System.String)")]
		[InlineData("00", "no signature", "throws TypeLoadException")]
		[InlineData("00 02", "no signature", "throws TypeLoadException")]
		// Return types
		[InlineData("00 00 08", "System.Int32()", "System.Int32()")]
		[InlineData("00 00 10 08", "System.Int32&()", "System.Int32&()")]
		[InlineData("00 00 16", "System.TypedReference()", "System.TypedReference()")]
		[InlineData("00 00 10 16", "System.TypedReference&()", "System.TypedReference&()")]
		[InlineData("00 00 10 01", "System.Void&()", "System.Void&()")]
		[InlineData("00 00 20 {Object} 01", "System.Void()", "System.Void()")]
		[InlineData("00 00 45 08", "System.Int32()", "System.Int32()")]
		[InlineData("00 00 41", "no signature", "throws BadImageFormatException")]
		// Parameter types
		[InlineData("00 01 01 10 08", "System.Void(System.Int32&)", "System.Void(System.Int32&)")]
		[InlineData("00 01 01 10 10 08", "System.Void(System.Int32&&)", "throws TypeLoadException")]
		[InlineData("00 01 01 01", "System.Void(System.Void)", "System.Void(System.Void)")]
		[InlineData("00 01 01 16", "System.Void(System.TypedReference)", "System.Void(System.TypedReference)")]
		[InlineData("00 01 01 10 16", "System.Void(System.TypedReference&)", "System.Void(System.TypedReference&)")]
		[InlineData("00 01 01 20 {Object} 10 08", "System.Void(System.Int32&)", "System.Void(System.Int32&)")]
		[InlineData("00 01 01 10 20 {Object} 08", "System.Void(System.Int32&)", "System.Void(System.Int32&)")]
		[InlineData("00 01 01 41 08", "System.Void(...,System.Int32)", "System.Void(System.Int32)")]
		[InlineData("00 01 01 45 08", "System.Void(System.Int32)", "System.Void(System.Int32)")]
		[InlineData("00 01 01 1E 00", "System.Void(!!0)", "throws BadImageFormatException")]
		public void MethodSignature(string signature, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(signature, Table.Method);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadMethodWithDnlib(image), expectedRuntime, () => ReadMethodWithTheRuntime(image));
		}
	}
}
