// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Security attributes (<c>[SecurityPermission(SecurityAction.Demand, ...)]</c>) are rows of the
	/// DeclSecurity table: an action, the type, method or assembly that has the attributes, and a blob with
	/// the attributes. These tests write values that a compiler doesn't write and read them with dnlib. Only
	/// .NET Framework reads this table (its reflection returns the security attributes with the custom
	/// attributes), .NET never does: if .NET Framework runs the tests, it's asked for the same attributes.
	/// </summary>
	public class ClrDeclSecurityRuleTests {
		const string Source = @"
using System.Security.Permissions;
#pragma warning disable SYSLIB0003, CS0618 // obsolete
public class Marker {
	[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
	public static void Method() { }
	public static void Other() { }
}
[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.Execution)]
public class Second { }
";
		// The compiler adds a security attribute to an assembly with unsafe code
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source, compilerOptions: new CSharpCompilerOptions { AllowUnsafe = false });

		const string PermissionSetXml = "<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>";

		// The name of SecurityPermissionAttribute with its assembly, like the compiler wrote it
		static readonly string attributeTypeName = GetAttributeTypeName();
		static string GetAttributeTypeName() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var blob = module.Find("Second", isReflectionName: true)!.DeclSecurities[0].GetBlob()!;
				// '.', the number of attributes (1), the length of the name (2 bytes if it's 128 or more), the name
				int nameOffset = blob[2] < 0x80 ? 3 : 4;
				int nameLength = blob[2] < 0x80 ? blob[2] : ((blob[2] & 0x7F) << 8) | blob[3];
				return Encoding.UTF8.GetString(blob, nameOffset, nameLength);
			}
		}

		static byte[] CompressedNumber(int value) =>
			value < 0x80 ? new[] { (byte)value } : new[] { (byte)(0x80 | (value >> 8)), (byte)value };

		static byte[] WithLength(IEnumerable<byte> bytes) {
			var array = bytes.ToArray();
			return CompressedNumber(array.Length).Concat(array).ToArray();
		}

		// Creates a blob. The words of the text:
		//	a hex number	that byte
		//	S:text			a string: its length and its UTF-8 bytes
		//	TYPE			the string with the name of SecurityPermissionAttribute and its assembly
		//	FLAGS			the string with the name of the enum SecurityPermissionFlag and its assembly
		//	TYPE:name		the string with the name of another type of the same namespace and assembly
		//	[ ... ]			the length of the bytes between the brackets, and those bytes
		//	XML				the XML text of a permission set (UTF-16)
		//	UTF16:text		UTF-16 text
		static byte[] ParseBlob(string text) {
			var stack = new Stack<List<byte>>();
			var bytes = new List<byte>();
			foreach (var word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
				if (word == "[") {
					stack.Push(bytes);
					bytes = new List<byte>();
				}
				else if (word == "]") {
					var inner = bytes;
					bytes = stack.Pop();
					bytes.AddRange(WithLength(inner));
				}
				else if (word == "TYPE")
					bytes.AddRange(WithLength(Encoding.UTF8.GetBytes(attributeTypeName)));
				else if (word == "FLAGS")
					bytes.AddRange(WithLength(Encoding.UTF8.GetBytes(attributeTypeName.Replace("SecurityPermissionAttribute", "SecurityPermissionFlag"))));
				else if (word.StartsWith("TYPE:"))
					bytes.AddRange(WithLength(Encoding.UTF8.GetBytes(attributeTypeName.Replace("SecurityPermissionAttribute", word.Substring(5)))));
				else if (word.StartsWith("S:"))
					bytes.AddRange(WithLength(Encoding.UTF8.GetBytes(word.Substring(2))));
				else if (word == "XML")
					bytes.AddRange(Encoding.Unicode.GetBytes(PermissionSetXml));
				else if (word.StartsWith("UTF16:"))
					bytes.AddRange(Encoding.Unicode.GetBytes(word.Substring(6)));
				else
					bytes.Add(Convert.ToByte(word, 16));
			}
			return bytes.ToArray();
		}

		// The row of the DeclSecurity table with the attributes of the method Marker.Method or of the type Second
		static uint FindRow(MetadataPatcher patcher, bool ofMethod) {
			uint parent = ofMethod ? (patcher.FindRow(Table.Method, "Name", "Method") << 2) | 1 : patcher.FindRow(Table.TypeDef, "Name", "Second") << 2;
			for (uint rid = 1; rid <= patcher.GetRowCount(Table.DeclSecurity); rid++) {
				if (patcher.GetCell(Table.DeclSecurity, rid, "Parent") == parent)
					return rid;
			}
			throw new InvalidOperationException();
		}

		// "Column = value", separated by "; ": a column of the row with the attribute of Marker.Method. A value
		// is a number, "of Second" (the value that the row of the type Second has), or for Parent (a coded token:
		// the row number and 0 = TypeDef, 1 = MethodDef, 2 = Assembly, 3 = not used) "type Marker", "method Other"
		// or "the assembly". "Method.Flags = value" changes the flags of Marker.Method. The table is sorted again
		// after the changes.
		static byte[] CreateImageWithChangedRow(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			uint row = FindRow(patcher, ofMethod: true);
			uint rowOfSecond = FindRow(patcher, ofMethod: false);
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				if (parts[0] == "Method.Flags") {
					patcher.SetCell(Table.Method, patcher.FindRow(Table.Method, "Name", "Method"), "Flags", uint.Parse(parts[1]));
					continue;
				}
				patcher.SetCell(Table.DeclSecurity, row, parts[0], parts[1] == "of Second" ? patcher.GetCell(Table.DeclSecurity, rowOfSecond, parts[0]) :
					parts[1] == "type Marker" ? patcher.FindRow(Table.TypeDef, "Name", "Marker") << 2 :
					parts[1] == "method Other" ? (patcher.FindRow(Table.Method, "Name", "Other") << 2) | 1 :
					parts[1] == "the assembly" ? (1 << 2) | 2 :
					uint.Parse(parts[1]));
			}
			patcher.SortRows(Table.DeclSecurity, "Parent");
			return patcher.ToArray();
		}

		// Adds the blob to the end of the #Blob heap. It's then the blob of the row of Marker.Method.
		static byte[] CreateImageWithBlob(string blob) {
			var bytes = ParseBlob(blob);
			var patcher = new MetadataPatcher(baseImage);
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			patcher.SetCell(Table.DeclSecurity, FindRow(patcher, ofMethod: true), "PermissionSet", (uint)heapLength);
			var block = new MetadataBlock(patcher.ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(WithLength(bytes)).ToArray();
			return block.Write();
		}

		// Characters that aren't ASCII letters are shown as their number
		static string ToText(object? value) => value switch {
			null => "null",
			bool boolean => boolean ? "true" : "false",
			_ => string.Concat(Convert.ToString(value, CultureInfo.InvariantCulture)!.Select(a => a >= 0x20 && a <= 0x7E ? a.ToString() : "{" + ((int)a).ToString("X4") + "}")),
		};

		// "who: attributes" of the assembly, the types and the methods that have a security attribute. An
		// attribute is its action (a number), the name of its type and its fields and properties.
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var marker = module.Find("Marker", isReflectionName: true)!;
				return Join(
					("assembly", Describe(module.Assembly!)),
					("Marker", Describe(marker)),
					("Method", Describe(marker.FindMethod("Method")!)),
					("Other", Describe(marker.FindMethod("Other")!)),
					("Second", Describe(module.Find("Second", isReflectionName: true)!)));
			}
		}

		// The attributes of the method Marker.Method
		static string ReadMethodWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image))
				return Join(("Method", Describe(module.Find("Marker", isReflectionName: true)!.FindMethod("Method")!)));
		}

		static string Describe(IHasDeclSecurity owner) =>
			string.Join(", ", owner.DeclSecurities.SelectMany(declSecurity => {
				int action = (int)declSecurity.Action;
				if (declSecurity.SecurityAttributes.Count == 0)
					return new[] { action + " without attributes" };
				return declSecurity.SecurityAttributes.Select(attribute => {
					var arguments = attribute.NamedArguments.Select(a => (a.IsField ? "field " : "") + a.Name + "=" + ToText(a.Value));
					return $"{action} {attribute.AttributeType?.Name ?? "null"}({string.Join(" ", arguments)})";
				});
			}));

		static string Join(params (string who, string attributes)[] all) {
			var withAttributes = all.Where(a => a.attributes.Length != 0).Select(a => a.who + ": " + a.attributes).ToArray();
			return withAttributes.Length == 0 ? "nothing" : string.Join("; ", withAttributes);
		}

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var marker = loaded.Assembly.GetType("Marker", throwOnError: true)!;
				return Join(
					("assembly", Describe(loaded.Assembly)),
					("Marker", Describe(marker)),
					("Method", Describe(marker.GetMethod("Method")!)),
					("Other", Describe(marker.GetMethod("Other")!)),
					("Second", Describe(loaded.Assembly.GetType("Second", throwOnError: true)!)));
			}
		}

		static string ReadMethodWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image))
				return Join(("Method", Describe(loaded.Assembly.GetType("Marker", throwOnError: true)!.GetMethod("Method")!)));
		}

		// The values of an attribute's properties that aren't false, 0 or null
#pragma warning disable SYSLIB0003 // SecurityAttribute is obsolete
		static string Describe(ICustomAttributeProvider owner) {
			try {
				return string.Join(", ", owner.GetCustomAttributes(false).OfType<System.Security.Permissions.SecurityAttribute>().Select(attribute => {
					var properties = attribute.GetType().GetProperties().Where(a => a.Name != "Action" && a.Name != "TypeId").OrderBy(a => a.Name, StringComparer.Ordinal)
						.Select(a => (a.Name, value: a.GetValue(attribute, null)))
						.Where(a => a.value is not null && !a.value.Equals(false) && Convert.ToInt64(a.value is Enum or int ? a.value : 1) != 0)
						.Select(a => a.Name + "=" + ToText(a.value));
					return $"{(int)attribute.Action} {attribute.GetType().Name}({string.Join(" ", properties)})";
				}).OrderBy(a => a, StringComparer.Ordinal));
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}
#pragma warning restore SYSLIB0003

		[Theory]
		[InlineData("", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		// The action
		[InlineData("Action = 0", "Method: 0 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 1", "Method: 1 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 0 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 3", "Method: 3 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 3 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 4", "Method: 4 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 4 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 5", "Method: 5 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 5 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 6", "Method: 6 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 6 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 7", "Method: 7 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 7 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 8", "Method: 8 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 9", "Method: 9 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 10", "Method: 10 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 11", "Method: 11 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 12", "Method: 12 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 13", "Method: 13 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 14", "Method: 14 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 6 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 15", "Method: 15 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 7 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 16", "Method: 16 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 31", "Method: 31 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 34", "Method: 34 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 258", "Method: 258 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 32770", "Method: -32766 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Action = 65535", "Method: -1 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		// Who has the attributes
		[InlineData("Parent = type Marker", "Marker: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Marker: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = method Other", "Other: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Other: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = the assembly", "assembly: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = the assembly; Action = 8", "assembly: 8 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "assembly: 8 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = of Second", "Second: 2 SecurityPermissionAttribute(UnmanagedCode=true), 3 SecurityPermissionAttribute(Flags=8)", "Second: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true), 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = of Second; Action = of Second", "Second: 3 SecurityPermissionAttribute(UnmanagedCode=true), 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("Parent = of Second; Action = of Second; PermissionSet = of Second", "Second: 3 SecurityPermissionAttribute(Flags=8), 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 0", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 1", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 2", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 3", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 7", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 10", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 4000", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Parent = 4001", "Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		// The blob
		[InlineData("PermissionSet = of Second", "Method: 2 SecurityPermissionAttribute(Flags=8); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=Execution); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("PermissionSet = 0", "Method: 2 without attributes; Second: 3 SecurityPermissionAttribute(Flags=8)", "Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("PermissionSet = 60000", "Method: 2 without attributes; Second: 3 SecurityPermissionAttribute(Flags=8)", "throws BadImageFormatException")]
		// The flag "has security" of the method: 0x4000. The compiler wrote 0x4096.
		[InlineData("Method.Flags = 150", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("Method.Flags = 150; Parent = method Other", "Other: 2 SecurityPermissionAttribute(UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Flags=8)", "Other: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true); Second: 3 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		public void Row(string changes, string expectedDnlib, string expectedNetFramework) {
			var image = CreateImageWithChangedRow(changes);

			ClrReaderComparison.AssertReadOnNetFramework(expectedDnlib, ReadWithDnlib(image), expectedNetFramework, () => ReadWithTheRuntime(image));
		}

		// A blob is '.' (0x2E), the number of attributes, and for every attribute: the name of its type, the
		// length of the rest of the attribute, the number of fields and properties and then those like in a
		// custom attribute: 0x53 (field) or 0x54 (property), the type (2 = bool, 8 = int, 0x55 = an enum and its
		// name), the name and the value.
		[Theory]
		[InlineData("2E 01 TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 01 TYPE [ 01 54 55 FLAGS S:Flags 08 00 00 00 ]", "Method: 2 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("2E 01 TYPE [ 02 54 02 S:UnmanagedCode 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true Execution=true)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=UnmanagedCode, Execution UnmanagedCode=true)")]
		[InlineData("2E 01 TYPE [ 00 ]", "Method: 2 SecurityPermissionAttribute()", "Method: 2 SecurityPermissionAttribute()")]
		// The number of attributes
		[InlineData("", "Method: 2 without attributes", "nothing")]
		[InlineData("2E", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 00", "Method: 2 without attributes", "nothing")]
		[InlineData("2E 00 TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 without attributes", "nothing")]
		[InlineData("2E 02 TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 02 TYPE [ 01 54 02 S:UnmanagedCode 01 ] TYPE [ 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true), 2 SecurityPermissionAttribute(Execution=true)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=Execution), 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 02 TYPE [ 01 54 02 S:UnmanagedCode 01 ] TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true), 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true), 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 80 01 TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E C0 00 00 01 TYPE [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E FF FF FF FF", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:UnmanagedCode 01 ] 00 00 00", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		// The length of an attribute isn't the real length. The next attribute starts where the length says.
		// Fixed in this commit: dnlib read it after the last field or property.
		[InlineData("2E 01 TYPE 00 01 54 02 S:UnmanagedCode 01", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "throws OverflowException")]
		[InlineData("2E 01 TYPE 01 01 54 02 S:UnmanagedCode 01", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE 7F 01 54 02 S:UnmanagedCode 01", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "throws OverflowException")]
		[InlineData("2E 02 TYPE 00 01 54 02 S:UnmanagedCode 01 TYPE [ 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true), 2 SecurityPermissionAttribute(Execution=true)", "throws OverflowException")]
		[InlineData("2E 02 TYPE [ 01 54 02 S:UnmanagedCode 01 00 00 00 00 ] TYPE [ 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true), 2 SecurityPermissionAttribute(Execution=true)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=Execution), 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 02 TYPE 02 01 54 02 S:UnmanagedCode 01 TYPE [ 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true), 2 SecurityPermissionAttribute(Execution=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:UnmanagedCode 01 FF FF ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 02 TYPE [ 00 54 02 S:UnmanagedCode 01 ] TYPE [ 01 54 02 S:Execution 01 ]", "Method: 2 SecurityPermissionAttribute(), 2 SecurityPermissionAttribute(Execution=true)", "Method: 2 SecurityPermissionAttribute(), 2 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("2E 01 TYPE", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 01 TYPE 00", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 01 TYPE [ ]", "Method: 2 without attributes", "throws OverflowException")]
		// The number of fields and properties
		[InlineData("2E 01 TYPE [ 00 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute()", "Method: 2 SecurityPermissionAttribute()")]
		[InlineData("2E 01 TYPE [ 02 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 80 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		// Fields and properties
		[InlineData("2E 01 TYPE [ 01 53 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(field UnmanagedCode=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:NoSuchProperty 01 ]", "Method: 2 SecurityPermissionAttribute(NoSuchProperty=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:unmanagedcode 01 ]", "Method: 2 SecurityPermissionAttribute(unmanagedcode=true)", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 08 S:UnmanagedCode 01 00 00 00 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=1)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:UnmanagedCode 02 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 01 TYPE [ 01 54 02 S:UnmanagedCode ]", "Method: 2 without attributes", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 08 S:Flags 08 00 00 00 ]", "Method: 2 SecurityPermissionAttribute(Flags=8)", "Method: 2 SecurityPermissionAttribute(Execution=true Flags=Execution)")]
		[InlineData("2E 01 TYPE [ 01 54 55 FLAGS S:Flags 08 00 ]", "Method: 2 without attributes", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 55 S:NoSuchEnum S:Flags 08 00 00 00 ]", "Method: 2 SecurityPermissionAttribute(Flags=8)", "Method: throws TypeLoadException")]
		[InlineData("2E 01 TYPE [ 01 55 02 S:UnmanagedCode 01 ]", "Method: 2 without attributes", "Method: throws COMException")]
		[InlineData("2E 01 TYPE [ 01 54 FF S:UnmanagedCode 01 ]", "Method: 2 without attributes", "Method: throws COMException")]
		// The type of the attribute
		[InlineData("2E 01 S:System.Security.Permissions.SecurityPermissionAttribute [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 01 S:SecurityPermissionAttribute [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: throws TypeLoadException")]
		[InlineData("2E 01 S:system.security.permissions.securitypermissionattribute [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 securitypermissionattribute(UnmanagedCode=true)", "Method: throws TypeLoadException")]
		[InlineData("2E 01 S:System.Security.Permissions.SecurityPermissionAttribute,mscorlib [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: 2 SecurityPermissionAttribute(Flags=UnmanagedCode UnmanagedCode=true)")]
		[InlineData("2E 01 S:System.Security.Permissions.SecurityPermissionAttribute,NoSuchAssembly [ 01 54 02 S:UnmanagedCode 01 ]", "Method: 2 SecurityPermissionAttribute(UnmanagedCode=true)", "Method: throws FileNotFoundException")]
		[InlineData("2E 01 S:NoSuchAttribute [ 00 ]", "Method: 2 NoSuchAttribute()", "Method: throws TypeLoadException")]
		[InlineData("2E 01 00 [ 00 ]", "Method: 2 null()", "Method: throws ArgumentException")]
		[InlineData("2E 01 FF [ 00 ]", "Method: 2 without attributes", "throws OverflowException")]
		[InlineData("2E 01 TYPE:ReflectionPermissionAttribute [ 00 ]", "Method: 2 ReflectionPermissionAttribute()", "Method: 2 ReflectionPermissionAttribute()")]
		[InlineData("2E 01 TYPE:SecurityPermissionFlag [ 00 ]", "Method: 2 SecurityPermissionFlag()", "Method: throws COMException")]
		[InlineData("2E 01 TYPE:SecurityAttribute [ 00 ]", "Method: 2 SecurityAttribute()", "Method: throws COMException")]
		[InlineData("2E 01 S:System.ObsoleteAttribute [ 00 ]", "Method: 2 ObsoleteAttribute()", "Method: throws COMException")]
		[InlineData("2E 01 S:System.Object [ 00 ]", "Method: 2 Object()", "Method: throws COMException")]
		[InlineData("2E 01 S:Marker [ 00 ]", "Method: 2 Marker()", "Method: throws TypeLoadException")]
		[InlineData("2E 01 S:System.Security.Permissions.PermissionSetAttribute [ 01 54 0E S:XML S:<PermissionSet/> ]", "Method: 2 PermissionSetAttribute(XML=<PermissionSet/>)", "Method: 2 PermissionSetAttribute(XML=<PermissionSet/>)")]
		// A blob that doesn't start with '.' is the XML text of a permission set
		[InlineData("XML", "Method: 2 PermissionSetAttribute(XML=<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>)", "nothing")]
		[InlineData("XML 00", "Method: 2 PermissionSetAttribute(XML=<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>)", "nothing")]
		[InlineData("XML 00 00", "Method: 2 PermissionSetAttribute(XML=<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>{0000})", "nothing")]
		[InlineData("UTF16:<PermissionSet/>", "Method: 2 PermissionSetAttribute(XML=<PermissionSet/>)", "nothing")]
		[InlineData("UTF16:<a/>", "Method: 2 PermissionSetAttribute(XML=<a/>)", "nothing")]
		[InlineData("UTF16:text", "Method: 2 PermissionSetAttribute(XML=text)", "nothing")]
		[InlineData("3C", "Method: 2 PermissionSetAttribute(XML=)", "nothing")]
		[InlineData("00", "Method: 2 PermissionSetAttribute(XML=)", "nothing")]
		[InlineData("00 00", "Method: 2 PermissionSetAttribute(XML={0000})", "nothing")]
		[InlineData("2F 2E 00", "Method: 2 PermissionSetAttribute(XML={2E2F})", "nothing")]
		[InlineData("FF FE XML", "Method: 2 PermissionSetAttribute(XML={FEFF}<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>)", "nothing")]
		[InlineData("2E 00 XML", "Method: 2 without attributes", "nothing")]
		public void Blob(string blob, string expectedDnlib, string expectedNetFramework) {
			var image = CreateImageWithBlob(blob);

			ClrReaderComparison.AssertReadOnNetFramework(expectedDnlib, ReadMethodWithDnlib(image), expectedNetFramework, () => ReadMethodWithTheRuntime(image));
		}
	}
}
