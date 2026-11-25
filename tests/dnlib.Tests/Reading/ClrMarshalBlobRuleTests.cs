// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;
using MarshalType = dnlib.DotNet.MarshalType;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// How a field is marshalled is a blob: the native type, and then values that depend on the native type
	/// (the size of an array, the type of its elements, the names of a custom marshaler). Most of the values
	/// are optional. These tests write blobs that end early, have numbers in more bytes than needed or have
	/// bytes after their end, and read them with dnlib and with the runtime that runs the tests (its
	/// MarshalAsAttribute). If the runtime reads the blob, dnlib must read the same values.
	/// </summary>
	public class ClrMarshalBlobRuleTests {
		const string Source = @"
using System.Runtime.InteropServices;
public class Marker {
	[MarshalAs(UnmanagedType.I4)] public int Field;
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);

		// The bytes as hex numbers. "S:text" is a string: its length (1 byte) and its UTF-8 bytes.
		static byte[] ParseBytes(string bytes) =>
			bytes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).SelectMany(a => {
				if (!a.StartsWith("S:"))
					return new[] { Convert.ToByte(a, 16) };
				var text = Encoding.UTF8.GetBytes(a.Substring(2));
				return new[] { (byte)text.Length }.Concat(text);
			}).ToArray();

		// Adds the bytes to the end of the #Blob heap. They are then the marshalling info of the field.
		static byte[] CreateImage(string blob) {
			var bytes = ParseBytes(blob);
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(new MetadataPatcher(baseImage).SetCell(Table.FieldMarshal, 1, "NativeType", (uint)heapLength).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		// The native type as a hex number, and then the values that aren't 0 or empty
		static string Describe(int nativeType, params (string name, object? value)[] values) {
			var parts = values.Where(a => a.value is not null && !Equals(a.value, 0) && !Equals(a.value, "")).Select(a => $"{a.name}={a.value}");
			return string.Join(" ", new[] { nativeType.ToString("X2") }.Concat(parts));
		}

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var marshalType = module.Find("Marker", isReflectionName: true)!.FindField("Field")!.MarshalType;
				int nativeType = marshalType is null ? 0 : (int)marshalType.NativeType;
				switch (marshalType) {
				case null:
					return "none";
				case RawMarshalType:
					// dnlib couldn't read the blob: it has the bytes (RawMarshalType.Data)
					return "only the bytes";
				case FixedSysStringMarshalType type:
					return Describe(nativeType, ("size", type.IsSizeValid ? type.Size : 0));
				case FixedArrayMarshalType type:
					return Describe(nativeType, ("size", type.IsSizeValid ? type.Size : 0), ("element", type.IsElementTypeValid ? ((int)type.ElementType).ToString("X2") : null));
				case ArrayMarshalType type:
					return Describe(nativeType, ("element", type.IsElementTypeValid ? ((int)type.ElementType).ToString("X2") : null),
						("parameter", type.IsParamNumberValid ? type.ParamNumber : 0), ("size", type.IsSizeValid ? type.Size : 0));
				case SafeArrayMarshalType type:
					return Describe(nativeType, ("variant", type.IsVariantTypeValid ? (int)type.VariantType : 0), ("type", type.UserDefinedSubType?.FullName));
				case CustomMarshalType type:
					return Describe(nativeType, ("marshaler", type.CustomMarshaler?.FullName), ("cookie", type.Cookie?.String));
				case InterfaceMarshalType type:
					return Describe(nativeType, ("iid", type.IsIidParamIndexValid ? type.IidParamIndex : 0));
				default:
					return Describe(nativeType);
				}
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var field = loaded.Assembly.GetType("Marker", throwOnError: true)!.GetField("Field")!;
					var attribute = (MarshalAsAttribute?)field.GetCustomAttributes(typeof(MarshalAsAttribute), false).FirstOrDefault();
					if (attribute is null)
						return "none";
					int nativeType = (int)attribute.Value;
					switch (attribute.Value) {
					case UnmanagedType.ByValTStr:
						return Describe(nativeType, ("size", attribute.SizeConst));
					case UnmanagedType.ByValArray:
						return Describe(nativeType, ("size", attribute.SizeConst), ("element", attribute.ArraySubType == 0 ? null : ((int)attribute.ArraySubType).ToString("X2")));
					case UnmanagedType.LPArray:
						return Describe(nativeType, ("element", attribute.ArraySubType == 0 ? null : ((int)attribute.ArraySubType).ToString("X2")),
							("parameter", (int)attribute.SizeParamIndex), ("size", attribute.SizeConst));
					case UnmanagedType.SafeArray:
						return Describe(nativeType, ("variant", (int)attribute.SafeArraySubType), ("type", attribute.SafeArrayUserDefinedSubType?.FullName));
					case UnmanagedType.CustomMarshaler:
						return Describe(nativeType, ("marshaler", attribute.MarshalTypeRef?.FullName ?? attribute.MarshalType), ("cookie", attribute.MarshalCookie));
					case UnmanagedType.IUnknown:
					case UnmanagedType.IDispatch:
					case UnmanagedType.Interface:
						return Describe(nativeType, ("iid", attribute.IidParameterIndex));
					default:
						return Describe(nativeType);
					}
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// "A|B": the values of a safe array and of an interface are only in .NET's MarshalAsAttribute on
		// Windows (B). On other systems the attribute only has the native type (A); dnlib always reads them.
		[Theory]
		// Simple native types
		[InlineData("07", "07", "07")]
		[InlineData("07 08", "07", "07")]
		[InlineData("14", "14", "14")]
		[InlineData("00", "00", "00")]
		[InlineData("", "only the bytes", "none")]
		[InlineData("50", "50", "50")]
		[InlineData("80 07", "80", "80")]
		[InlineData("7F", "7F", "7F")]
		// A string with a fixed size (17): the size
		[InlineData("17 10", "17 size=16", "17 size=16")]
		[InlineData("17", "17", "throws BadImageFormatException")]
		[InlineData("17 80 10", "17 size=16", "17 size=16")]
		[InlineData("17 C0 00 00 10", "17 size=16", "17 size=16")]
		[InlineData("17 10 05", "17 size=16", "17 size=16")]
		[InlineData("17 FF", "only the bytes", "throws BadImageFormatException")]
		// An array with a fixed size (1E): the size and the type of the elements
		[InlineData("1E 04 07", "1E size=4 element=07", "1E size=4 element=07")]
		[InlineData("1E 04", "1E size=4", "1E size=4")]
		[InlineData("1E", "1E", "throws BadImageFormatException")]
		[InlineData("1E 04 50", "1E size=4 element=50", "1E size=4 element=50")]
		[InlineData("1E 04 07 09", "1E size=4 element=07", "1E size=4 element=07")]
		[InlineData("1E 00 07", "1E element=07", "1E element=07")]
		// An array (2A): the type of the elements, the number of the parameter with the size, the size, flags
		[InlineData("2A 07 02 0A 01", "2A element=07 parameter=2 size=10", "2A element=07 parameter=2 size=10")]
		[InlineData("2A 07 02 0A 00", "2A element=07 parameter=2 size=10", "2A element=07 parameter=2 size=10")]
		[InlineData("2A 07 02 0A", "2A element=07 parameter=2 size=10", "2A element=07 parameter=2 size=10")]
		[InlineData("2A 07 02", "2A element=07 parameter=2", "2A element=07 parameter=2")]
		[InlineData("2A 07", "2A element=07", "2A element=07")]
		[InlineData("2A", "2A", "2A")]
		[InlineData("2A 50", "2A element=50", "2A element=50")]
		[InlineData("2A 50 02 0A 01", "2A element=50 parameter=2 size=10", "2A element=50 parameter=2 size=10")]
		[InlineData("2A 07 02 0A 01 09", "2A element=07 parameter=2 size=10", "2A element=07 parameter=2 size=10")]
		[InlineData("2A 07 80 02 80 0A 01", "2A element=07 parameter=2 size=10", "2A element=07 parameter=2 size=10")]
		[InlineData("2A 07 02 0A FF", "only the bytes", "throws BadImageFormatException")]
		// A safe array (1D): the type of the elements and the name of a type
		[InlineData("1D 03", "1D variant=3", "1D|1D variant=3")]
		[InlineData("1D", "1D", "1D")]
		[InlineData("1D 03 S:Marker", "1D variant=3 type=Marker", "1D|1D variant=3 type=Marker")]
		[InlineData("1D 03 S:No.Such.Type", "1D variant=3 type=No.Such.Type", "1D|1D variant=3")]
		[InlineData("1D 03 00", "1D variant=3", "1D|1D variant=3")]
		[InlineData("1D 00", "1D", "1D")]
		[InlineData("1D 03 7F", "only the bytes", "1D|throws BadImageFormatException")]
		// A custom marshaler (2C): a GUID, the name of a native type, the name of the marshaler, a cookie
		[InlineData("2C 00 00 S:Marker S:cookie", "2C marshaler=Marker cookie=cookie", "2C marshaler=Marker cookie=cookie")]
		[InlineData("2C S:guid S:native S:Marker S:cookie", "2C marshaler=Marker cookie=cookie", "2C marshaler=Marker cookie=cookie")]
		[InlineData("2C 00 00 S:Marker 00", "2C marshaler=Marker", "2C marshaler=Marker")]
		[InlineData("2C 00 00 S:Marker", "only the bytes", "throws BadImageFormatException")]
		[InlineData("2C 00 00", "only the bytes", "throws BadImageFormatException")]
		[InlineData("2C 00", "only the bytes", "throws BadImageFormatException")]
		[InlineData("2C", "only the bytes", "throws BadImageFormatException")]
		[InlineData("2C 00 00 S:No.Such.Type S:cookie", "2C marshaler=No.Such.Type cookie=cookie", "2C marshaler=No.Such.Type cookie=cookie")]
		[InlineData("2C 00 00 S:Marker S:cookie 09", "2C marshaler=Marker cookie=cookie", "2C marshaler=Marker cookie=cookie")]
		[InlineData("2C 00 00 S:Marker 7F", "only the bytes", "throws BadImageFormatException")]
		// Interfaces (19, 1A, 1C): the number of a parameter
		[InlineData("19", "19", "19")]
		[InlineData("19 03", "19 iid=3", "19|19 iid=3")]
		[InlineData("1A 03", "1A iid=3", "1A|1A iid=3")]
		[InlineData("1C 03", "1C iid=3", "1C|1C iid=3")]
		[InlineData("19 80 03", "19 iid=3", "19|19 iid=3")]
		public void MarshallingInfoOfAField(string blob, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(blob);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
