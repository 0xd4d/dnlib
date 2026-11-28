// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The bytes of a custom attribute, written in ways that a compiler doesn't use: lengths in more bytes
	/// than needed, strings that aren't UTF-8, type names that don't exist, named arguments with the wrong
	/// kind or type. Every test writes the bytes of an attribute to a file, reads the attribute with dnlib
	/// and lets the runtime that runs the tests create it: if the runtime creates the attribute, dnlib must
	/// read the values that the attribute got.
	/// </summary>
	public class ClrCustomAttributeRuleTests {
		const string Source = @"
using System;
public class A : Attribute {
	public object Value;
	public string Named { get; set; }
	public int Field;
	public object Any { get; set; }
	public A(object value) => Value = value;
	public void NotAConstructor(object value) { }
}
public class B : Attribute {
	public B(object value) { }
}
public class NotAnAttribute {
	public NotAnAttribute(object value) { }
}
public class G<T, U> where U : struct { }
[A(1)]
public class Marker { }
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint markerRid, attributeRid;

		static ClrCustomAttributeRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				markerRid = type.Rid;
				// The row of the attribute: its Parent column is the row of the class (in the high bits) and
				// 3 (in the 5 low bits: the parent is a type)
				var patcher = new MetadataPatcher(baseImage);
				attributeRid = Enumerable.Range(1, (int)patcher.GetRowCount(Table.CustomAttribute)).Select(a => (uint)a)
					.Single(a => patcher.GetCell(Table.CustomAttribute, a, "Parent") == (markerRid << 5 | 3));
			}
		}

		// The bytes as hex numbers. "S:text" is a string: its length (1 byte) and its UTF-8 bytes.
		// "_" in the text is a space.
		static byte[] ParseBytes(string bytes) =>
			bytes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).SelectMany(a => {
				if (!a.StartsWith("S:"))
					return new[] { Convert.ToByte(a, 16) };
				var text = Encoding.UTF8.GetBytes(a.Substring(2).Replace('_', ' '));
				return new[] { (byte)text.Length }.Concat(text);
			}).ToArray();

		// Adds the bytes to the end of the #Blob heap. They are then the value of the attribute.
		static byte[] CreateImage(string attributeBytes) {
			var bytes = ParseBytes(attributeBytes);
			Assert.True(bytes.Length < 0x80);
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(new MetadataPatcher(baseImage).SetCell(Table.CustomAttribute, attributeRid, "Value", (uint)heapLength).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		static string Quote(string text) {
			var sb = new StringBuilder("'");
			foreach (char c in text) {
				if (c >= 0x20 && c < 0x7F)
					sb.Append(c);
				else
					sb.Append($"\\u{(int)c:X4}");
			}
			return sb.Append('\'').ToString();
		}

		// A number as its type and its value: "Int32:5". A char or an enum value is a number too.
		static string DescribeNumber(string typeName, object value) =>
			$"{typeName}:{(value is char c ? (int)c : value is bool b ? (b ? 1 : 0) : Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture))}";

		static string DescribeDnlibValue(object? value) {
			switch (value) {
			case null: return "null";
			case CAArgument argument:
				// An enum value is a number whose type is the enum
				if (argument.Value is not null && argument.Type is ValueTypeSig or ClassSig && !(argument.Value is CAArgument) && !(argument.Value is UTF8String) && !(argument.Value is TypeSig) && !(argument.Value is IList<CAArgument>))
					return DescribeNumber(argument.Type.TypeName, argument.Value);
				return DescribeDnlibValue(argument.Value);
			case UTF8String text: return Quote(text.String);
			case TypeSig type: return $"typeof({type.FullName})";
			case IList<CAArgument> list: return "[" + string.Join(",", list.Select(a => DescribeDnlibValue(a))) + "]";
			default: return DescribeNumber(value.GetType().Name, value);
			}
		}

		static string DescribeRuntimeValue(object? value) {
			switch (value) {
			case null: return "null";
			case string text: return Quote(text);
			case Type type: return $"typeof({type.FullName})";
			case IEnumerable list: return "[" + string.Join(",", list.Cast<object?>().Select(DescribeRuntimeValue)) + "]";
			default: return DescribeNumber(value.GetType().Name, value);
			}
		}

		static readonly string[] memberNames = { "Named", "Field", "Any" };

		// The constructor argument, and then the named arguments that have a value
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var attribute = module.ResolveTypeDef(markerRid)!.CustomAttributes.Single();
				if (attribute.IsRawBlob)
					return "not read";
				var result = DescribeDnlibValue(attribute.ConstructorArguments.Single());
				foreach (var name in memberNames) {
					foreach (var argument in attribute.NamedArguments.Where(a => a.Name == name))
						result += $" {name}={DescribeDnlibValue(argument.Argument)}";
				}
				return result;
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.ManifestModule.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32());
					var attribute = type.GetCustomAttributes(false).Single();
					var attributeType = attribute.GetType();
					var result = DescribeRuntimeValue(attributeType.GetField("Value")!.GetValue(attribute));
					foreach (var name in memberNames) {
						var value = name == "Field" ? attributeType.GetField(name)!.GetValue(attribute) : attributeType.GetProperty(name)!.GetValue(attribute, null);
						if (value is not null && !Equals(value, 0))
							result += $" {name}={DescribeRuntimeValue(value)}";
					}
					return result;
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// "not read": dnlib only has the bytes of the attribute (CustomAttribute.RawData).
		//
		// The bytes: 01 00, the argument of the constructor (an object: the type of the value and the
		// value), the number of named arguments (2 bytes) and the named arguments
		[Theory]
		[InlineData("01 00 08 05 00 00 00 00 00", "Int32:5", "Int32:5")]
		// Numbers, bool, char
		[InlineData("01 00 02 01 00 00", "Boolean:1", "Boolean:1")]
		[InlineData("01 00 02 02 00 00", "Boolean:1", "Boolean:1")]
		[InlineData("01 00 03 41 00 00 00", "Char:65", "Char:65")]
		[InlineData("01 00 03 00 D8 00 00", "Char:55296", "Char:55296")]
		[InlineData("01 00 04 FF 00 00", "SByte:-1", "SByte:-1")]
		[InlineData("01 00 0B 01 02 03 04 05 06 07 08 00 00", "UInt64:578437695752307201", "UInt64:578437695752307201")]
		// Types that can't be the type of a value
		[InlineData("01 00 18 05 00 00 00 00 00 00 00 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 1C 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 01 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 17 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 51 08 05 00 00 00 00 00", "Int32:5", "throws InvalidCastException")]
		// Strings: the length and UTF-8 bytes
		[InlineData("01 00 0E S:abc 00 00", "'abc'", "'abc'")]
		[InlineData("01 00 0E 00 00 00", "''", "''")]
		[InlineData("01 00 0E FF 00 00", "null", "null")]
		[InlineData("01 00 0E 80 03 61 62 63 00 00", "'abc'", "'abc'")]
		[InlineData("01 00 0E C0 00 00 03 61 62 63 00 00", "'abc'", "'abc'")]
		[InlineData("01 00 0E 02 61 FF 00 00", "'a\\uFFFD'", "'a\\uFFFD'")]
		[InlineData("01 00 0E 03 61 00 62 00 00", "'a\\u0000b'", "'a\\u0000b'")]
		[InlineData("01 00 0E 7F 61 62 63 00 00", "not read", "throws CustomAttributeFormatException")]
		// Types: a string with the name
		[InlineData("01 00 50 S:System.Int32 00 00", "typeof(System.Int32)", "typeof(System.Int32)")]
		[InlineData("01 00 50 S:Marker 00 00", "typeof(Marker)", "typeof(Marker)")]
		[InlineData("01 00 50 S:System.Int32[] 00 00", "typeof(System.Int32[])", "typeof(System.Int32[])")]
		[InlineData("01 00 50 S:System.Int32,_mscorlib 00 00", "typeof(System.Int32)", "typeof(System.Int32)")]
		[InlineData("01 00 50 FF 00 00", "null", "null")]
		[InlineData("01 00 50 00 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 50 S:No.Such.Type 00 00", "typeof(No.Such.Type)", "throws TypeLoadException")]
		[InlineData("01 00 50 S:System.Int32,_NoSuchAssembly 00 00", "typeof(System.Int32)", "throws FileNotFoundException")]
		[InlineData("01 00 50 S:[ 00 00", "not read", "throws ArgumentException")]
		// Enums: the name of the enum and its value
		[InlineData("01 00 55 S:System.DayOfWeek 02 00 00 00 00 00", "DayOfWeek:2", "DayOfWeek:2")]
		[InlineData("01 00 55 S:System.DayOfWeek 63 00 00 00 00 00", "DayOfWeek:99", "DayOfWeek:99")]
		[InlineData("01 00 55 S:System.Int32 02 00 00 00 00 00", "Int32:2", "Int32:2")]
		[InlineData("01 00 55 S:No.Such.Enum 02 00 00 00 00 00", "Enum:2", "throws TypeLoadException")]
		// .NET 10 stops the process (segmentation fault) if it creates this attribute: the runtime isn't asked
		[InlineData("01 00 55 FF 02 00 00 00 00 00", "not read", "not asked")]
		// Arrays: the type of the elements, the number of elements and the elements
		[InlineData("01 00 1D 08 02 00 00 00 01 00 00 00 02 00 00 00 00 00", "[Int32:1,Int32:2]", "[Int32:1,Int32:2]")]
		[InlineData("01 00 1D 08 00 00 00 00 00 00", "[]", "[]")]
		[InlineData("01 00 1D 08 FF FF FF FF 00 00", "null", "null")]
		[InlineData("01 00 1D 0E 02 00 00 00 S:a FF 00 00", "['a',null]", "['a',null]")]
		[InlineData("01 00 1D 51 02 00 00 00 08 01 00 00 00 0E S:a 00 00", "[Int32:1,'a']", "[Int32:1,'a']")]
		[InlineData("01 00 1D 08 03 00 00 00 01 00 00 00 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 1D 08 FE FF FF FF 00 00", "not read", "throws OverflowException")]
		[InlineData("01 00 1D 1D 08 00 00 00 00 00 00", "[]", "throws CustomAttributeFormatException")]
		// Named arguments: field (53) or property (54), the type, the name and the value
		[InlineData("01 00 08 05 00 00 00 01 00 54 0E S:Named S:abc", "Int32:5 Named='abc'", "Int32:5 Named='abc'")]
		[InlineData("01 00 08 05 00 00 00 01 00 53 08 S:Field 07 00 00 00", "Int32:5 Field=Int32:7", "Int32:5 Field=Int32:7")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 51 S:Any 0E S:abc", "Int32:5 Any='abc'", "Int32:5 Any='abc'")]
		[InlineData("01 00 08 05 00 00 00 02 00 54 0E S:Named S:abc 53 08 S:Field 07 00 00 00", "Int32:5 Named='abc' Field=Int32:7", "Int32:5 Named='abc' Field=Int32:7")]
		[InlineData("01 00 08 05 00 00 00 02 00 54 0E S:Named S:abc 54 0E S:Named S:def", "Int32:5 Named='abc' Named='def'", "Int32:5 Named='def'")]
		// The wrong kind, name or type
		[InlineData("01 00 08 05 00 00 00 01 00 53 0E S:Named S:abc", "Int32:5 Named='abc'", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 08 S:Field 07 00 00 00", "Int32:5 Field=Int32:7", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 0E S:NoSuch S:abc", "Int32:5", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 0E S:named S:abc", "Int32:5", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 08 S:Named 07 00 00 00", "Int32:5 Named=Int32:7", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 53 0A S:Field 07 00 00 00 00 00 00 00", "Int32:5 Field=Int64:7", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 55 0E S:Named S:abc", "not read", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 01 00 54 0E FF S:abc", "Int32:5", "throws CustomAttributeFormatException")]
		// More or fewer named arguments than the number says
		[InlineData("01 00 08 05 00 00 00 02 00 54 0E S:Named S:abc", "Int32:5 Named='abc'", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 00 00 54 0E S:Named S:abc", "Int32:5", "throws CustomAttributeFormatException")]
		[InlineData("01 00 08 05 00 00 00 FF FF 54 0E S:Named S:abc", "Int32:5", "throws CustomAttributeFormatException")]
		public void AttributeValues(string attributeBytes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(attributeBytes);

			if (expectedRuntime == "not asked") {
				Assert.Equal(expectedDnlib, ReadWithDnlib(image));
				// .NET stops the process (a segmentation fault / an access violation)
				ClrReaderComparison.AssertRuntimeStopsTheProcess(stopsNetFramework: false, () => ReadWithTheRuntime(image));
			}
			else
				ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}

		// The bytes are the value of the attribute and the parameter of A's constructor gets another type: both
		// are added to the end of the #Blob heap
		static byte[] CreateImageWithConstructorParameter(string parameterType, string attributeBytes) {
			uint constructorRid;
			byte[] type;
			using (var module = ModuleDefMD.Load(baseImage)) {
				constructorRid = module.Find("A", isReflectionName: true)!.FindMethod(".ctor")!.Rid;
				var genericParameters = module.Find("G`2", isReflectionName: true)!.GenericParameters;
				switch (parameterType) {
				case "System.__Canon (3E)": type = new byte[] { 0x3E }; break;
				case "generic parameter T of G (3B)": type = new byte[] { 0x3B, (byte)genericParameters[0].Rid }; break;
				case "generic parameter U of G, a value type (3B)": type = new byte[] { 0x3B, (byte)genericParameters[1].Rid }; break;
				case "native Int32 (3D 08)": type = new byte[] { 0x3D, 0x08 }; break;
				case "native Object (3D 1C)": type = new byte[] { 0x3D, 0x1C }; break;
				default: throw new ArgumentException(parameterType);
				}
			}
			var bytes = ParseBytes(attributeBytes);
			// An instance method with 1 parameter that returns void
			var signature = new byte[] { 0x20, 0x01, 0x01 }.Concat(type).ToArray();
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(new MetadataPatcher(baseImage)
				.SetCell(Table.CustomAttribute, attributeRid, "Value", (uint)heapLength)
				.SetCell(Table.Method, constructorRid, "Signature", (uint)(heapLength + 1 + bytes.Length)).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).Concat(new[] { (byte)signature.Length }).Concat(signature).ToArray();
			return block.Write();
		}

		// The elements of an array are arguments too
		static string DescribeRuntimeArgument(CustomAttributeTypedArgument argument) =>
			argument.Value is IEnumerable<CustomAttributeTypedArgument> list ? "[" + string.Join(",", list.Select(DescribeRuntimeArgument)) + "]" : DescribeRuntimeValue(argument.Value);

		// The argument that the runtime reads, the attribute isn't created: its constructor would get an object
		// as a value of another type
		static string ReadDataWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.ManifestModule.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32());
					return DescribeRuntimeArgument(CustomAttributeData.GetCustomAttributes(type).Single().ConstructorArguments.Single());
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// Fixed in this commit: a constructor parameter whose type is one of the types of the runtime's own
		// signatures (3B: a generic parameter, 3D: a native value type, 3E: System.__Canon) wasn't read, dnlib
		// only had the bytes of the attribute. The runtime loads a class for these types and reads the argument
		// like a System.Object argument: the type of the value and the value. The writer writes it the same way.
		[Theory]
		[InlineData("System.__Canon (3E)", "01 00 08 05 00 00 00 00 00", "Int32:5", "Int32:5")]
		[InlineData("System.__Canon (3E)", "01 00 0E S:abc 00 00", "'abc'", "'abc'")]
		[InlineData("System.__Canon (3E)", "01 00 50 S:Marker 00 00", "typeof(Marker)", "typeof(Marker)")]
		[InlineData("System.__Canon (3E)", "01 00 1D 08 02 00 00 00 01 00 00 00 02 00 00 00 00 00", "[Int32:1,Int32:2]", "[Int32:1,Int32:2]")]
		[InlineData("System.__Canon (3E)", "01 00 08 05 00 00 00 01 00 54 0E S:Named S:abc", "Int32:5 Named='abc'", "Int32:5")]
		[InlineData("System.__Canon (3E)", "01 00 1C 00 00", "not read", "throws CustomAttributeFormatException")]
		[InlineData("generic parameter T of G (3B)", "01 00 08 05 00 00 00 00 00", "Int32:5", "Int32:5")]
		[InlineData("generic parameter T of G (3B)", "01 00 0E S:abc 00 00", "'abc'", "'abc'")]
		// The runtime doesn't read an argument whose type is a value type that isn't an enum
		[InlineData("generic parameter U of G, a value type (3B)", "01 00 08 05 00 00 00 00 00", "Int32:5", "throws ArgumentException")]
		[InlineData("native Int32 (3D 08)", "01 00 08 05 00 00 00 00 00", "Int32:5", "Int32:5")]
		[InlineData("native Int32 (3D 08)", "01 00 0E S:abc 00 00", "'abc'", "'abc'")]
		[InlineData("native Object (3D 1C)", "01 00 08 05 00 00 00 00 00", "Int32:5", "Int32:5")]
		public void ConstructorParameterOfARuntimeOwnType(string parameterType, string attributeBytes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithConstructorParameter(parameterType, attributeBytes);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadDataWithTheRuntime(image));

			// The writer writes the same bytes: dnlib reads the same from the file that it wrote
			using (var module = ModuleDefMD.Load(image)) {
				var attribute = module.ResolveTypeDef(markerRid)!.CustomAttributes.Single();
				bool wasRead = !attribute.IsRawBlob;
				var stream = new System.IO.MemoryStream();
				module.Write(stream);
				Assert.Equal(expectedDnlib, ReadWithDnlib(stream.ToArray()));
				Assert.Equal(expectedDnlib != "not read", wasRead);
			}
		}

		// The Type column of the attribute's row is the token of its constructor: the row * 8 + the table
		// (2: Method, 3: MemberRef)
		static byte[] CreateImageWithConstructor(string constructor) {
			var patcher = new MetadataPatcher(baseImage);
			uint normal = patcher.GetCell(Table.CustomAttribute, attributeRid, "Type");
			Assert.Equal(2U, normal & 7);
			uint value;
			using (var module = ModuleDefMD.Load(baseImage)) {
				uint Method(string typeName, string name) => module.Find(typeName, isReflectionName: true)!.FindMethod(name)!.Rid << 3 | 2;
				switch (constructor) {
				case "constructor of A": value = normal; break;
				case "constructor of B": value = Method("B", ".ctor"); break;
				case "constructor of a class that isn't an attribute": value = Method("NotAnAttribute", ".ctor"); break;
				case "method that isn't a constructor": value = Method("A", "NotAConstructor"); break;
				case "reference to the constructor of System.Attribute":
					value = module.GetMemberRefs().First(a => a.FullName.Contains("System.Attribute::.ctor")).Rid << 3 | 3;
					break;
				case "method row 0": value = 2; break;
				case "method row that doesn't exist": value = 1000 << 3 | 2; break;
				case "reference row that doesn't exist": value = 1000 << 3 | 3; break;
				case "table 0": value = (normal & ~7U) | 0; break;
				case "table 1": value = (normal & ~7U) | 1; break;
				case "table 4": value = (normal & ~7U) | 4; break;
				case "table 7": value = (normal & ~7U) | 7; break;
				default: throw new ArgumentException(constructor);
				}
			}
			return patcher.SetCell(Table.CustomAttribute, attributeRid, "Type", value).ToArray();
		}

		// The type of the attribute, and its value if dnlib could read it
		static string ReadTypeWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var attributes = module.ResolveTypeDef(markerRid)!.CustomAttributes;
				return string.Join(" ", attributes.Select(a => (a.AttributeType?.Name.String ?? "no type") + (a.IsRawBlob ? "" : $"({string.Join(",", a.ConstructorArguments.Select(b => DescribeDnlibValue(b)))})")));
			}
		}

		static string ReadTypeWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var type = loaded.Assembly.ManifestModule.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32());
				return string.Join(" ", type.GetCustomAttributes(false).Select(a => a.GetType().Name));
			}
		}

		[Theory]
		[InlineData("constructor of A", "A(Int32:1)", "A")]
		[InlineData("constructor of B", "B(Int32:1)", "B")]
		[InlineData("constructor of a class that isn't an attribute", "NotAnAttribute(Int32:1)", "NotAnAttribute")]
		[InlineData("method that isn't a constructor", "A(Int32:1)", "A")]
		// System.Attribute is abstract: the runtime doesn't create it and returns no attribute
		[InlineData("reference to the constructor of System.Attribute", "Attribute()", "")]
		[InlineData("method row 0", "no type", "throws ArgumentOutOfRangeException")]
		[InlineData("method row that doesn't exist", "no type", "throws BadImageFormatException")]
		[InlineData("reference row that doesn't exist", "no type", "throws BadImageFormatException")]
		[InlineData("table 0", "no type", "throws BadImageFormatException")]
		[InlineData("table 1", "no type", "throws BadImageFormatException")]
		[InlineData("table 4", "no type", "throws BadImageFormatException")]
		[InlineData("table 7", "no type", "throws BadImageFormatException")]
		public void AttributeConstructor(string constructor, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithConstructor(constructor);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadTypeWithDnlib(image), expectedRuntime, () => ReadTypeWithTheRuntime(image));
		}
	}
}
