// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The Module table and the Assembly table have one row in every file that a compiler writes. These tests
	/// add rows and read the name and the id of the module and the name of the assembly with dnlib and with
	/// the runtime that runs the tests. If the runtime loads the file, dnlib must read the same values.
	/// </summary>
	public class ClrModuleAndAssemblyRowRuleTests {
		const string Source = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")]
public class C { }
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);

		// Adds a string to the end of the #Strings heap and returns its offset
		static uint AddString(ref byte[] image, string text) {
			var block = new MetadataBlock(image);
			if (block.Streams.Any(a => a.Name == ClrReaderComparison.PaddingStreamName))
				block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Strings");
			uint offset = (uint)heap.Data.Length;
			heap.Data = heap.Data.Concat(Encoding.UTF8.GetBytes(text)).Concat(new byte[1]).ToArray();
			image = block.Write();
			return offset;
		}

		// The changes, separated by "; ":
		//	Table+							adds a row to the table: a copy of its first row
		//	Table(row).Column = value		changes a cell. A value is a number or 'text' (a new string).
		static byte[] CreateImage(string changes) {
			var image = baseImage;
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				if (change.EndsWith("+")) {
					image = MetadataPatcher.AddCopyOfRow(image, (Table)Enum.Parse(typeof(Table), change.TrimEnd('+')), 1, ClrReaderComparison.PaddingStreamName);
					continue;
				}
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				int open = parts[0].IndexOf('('), close = parts[0].IndexOf(").");
				var table = (Table)Enum.Parse(typeof(Table), parts[0].Substring(0, open));
				uint rid = uint.Parse(parts[0].Substring(open + 1, close - open - 1));
				uint value = parts[1].StartsWith("'") ? AddString(ref image, parts[1].Trim('\'')) : uint.Parse(parts[1]);
				image = new MetadataPatcher(image).SetCell(table, rid, parts[0].Substring(close + 2), value).ToArray();
			}
			return image;
		}

		static string Describe(string? moduleName, Guid? mvid, string? assemblyName) =>
			$"module {moduleName ?? "null"}, {(mvid is null ? "no id" : mvid == Guid.Empty ? "id 0" : "its id")}; assembly {assemblyName ?? "null"}";

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image))
				return Describe(module.Name, module.Mvid, module.Assembly?.FullName);
		}

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var module = loaded.Assembly.ManifestModule;
				return Describe(module.ScopeName, module.ModuleVersionId, loaded.Assembly.FullName);
			}
		}

		[Theory]
		[InlineData("", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		// Two rows in the Module table
		[InlineData("Module+", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Module(2).Name = 'Second'", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Module(1).Name = 'First'", "module First, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module First, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Module(2).Mvid = 0", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Module(1).Mvid = 0", "module TestAssembly.dll, no id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, id 0; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Module+; Module(3).Name = 'Third'", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		// Two rows in the Assembly table
		[InlineData("Assembly+", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Assembly+; Assembly(2).Name = 'Second'", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Assembly+; Assembly(1).Name = 'First'", "module TestAssembly.dll, its id; assembly First, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly First, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Assembly+; Assembly(2).MajorVersion = 9", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Assembly+; Assembly(1).MajorVersion = 9", "module TestAssembly.dll, its id; assembly TestAssembly, Version=9.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=9.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Assembly+; Assembly+; Assembly(3).Name = 'Third'", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module+; Assembly+; Module(2).Name = 'Second'; Assembly(2).Name = 'Second'", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		// The values of the only row
		[InlineData("Module(1).Name = 0", "module , its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module , its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module(1).Mvid = 0", "module TestAssembly.dll, no id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, id 0; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module(1).Mvid = 2", "module TestAssembly.dll, no id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "throws BadImageFormatException")]
		[InlineData("Module(1).Generation = 65535", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Module(1).EncId = 1; Module(1).EncBaseId = 1", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "module TestAssembly.dll, its id; assembly TestAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		public void ModuleAndAssembly(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
