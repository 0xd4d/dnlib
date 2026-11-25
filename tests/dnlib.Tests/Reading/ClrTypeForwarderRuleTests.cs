// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A type forwarder is a row of the ExportedType table: the name of a type, the "forwarder" flag and the
	/// assembly that has the type now. These tests write values that a compiler doesn't write and read the
	/// rows with dnlib. The runtime that runs the tests is asked for the forwarded types by their name
	/// (Assembly.GetType() follows a forwarder). If it finds a type, dnlib must have a forwarder with that
	/// name to the assembly of the type.
	/// </summary>
	public class ClrTypeForwarderRuleTests {
		// The ExportedType table has two rows
		const string Source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Text.Encoder))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Version))]
public class C { }";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "Column = value", separated by "; ": a column of the row of System.Text.Encoder. A value is
		// a number, "of Version" (the value that the row of System.Version has), or for Implementation "itself" /
		// "the row of Version" (that row of the ExportedType table: the type is then nested in that type).
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			uint row = patcher.FindRow(Table.ExportedType, "TypeName", "Encoder");
			uint rowOfVersion = patcher.FindRow(Table.ExportedType, "TypeName", "Version");
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				patcher.SetCell(Table.ExportedType, row, parts[0], parts[1] == "of Version" ? patcher.GetCell(Table.ExportedType, rowOfVersion, parts[0]) :
					parts[1] == "itself" ? (row << 2) | 2 :
					parts[1] == "the row of Version" ? (rowOfVersion << 2) | 2 :
					uint.Parse(parts[1]));
			}
			return patcher.ToArray();
		}

		// Every row: its name, "forwarder" if it has the flag, and where the type is
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				return string.Join(", ", module.ExportedTypes.Select(a => {
					string implementation = a.Implementation is null ? "null" :
						a.Implementation is AssemblyRef ? "the core library" :
						a.Implementation is ExportedType exportedType ? "exported type " + exportedType.TypeName :
						a.Implementation.GetType().Name;
					return a.TypeNamespace + "." + a.TypeName + (a.IsForwarder ? " forwarder" : "") + " in " + implementation;
				}));
			}
		}

		// The types that the assembly has for some names
		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var names = new[] { "System.Text.Encoder", "System.Version", "System.Text.Version", "System.Encoder", "Encoder", "System.Text." };
				return string.Join(", ", names.Select(a => a + ": " + Find(loaded.Assembly, a)));
			}
		}
		static string Find(System.Reflection.Assembly assembly, string name) {
			try {
				var type = assembly.GetType(name);
				return type is null ? "null" : type.Assembly == typeof(object).Assembly ? "found" : "found in " + type.Assembly.GetName().Name;
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		[Theory]
		[InlineData("", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		// The name
		[InlineData("TypeName = of Version", "System.Text.Version forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("TypeNamespace = of Version", "System.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("TypeName = of Version; TypeNamespace = of Version", "System.Version forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("TypeName = 0", "System.Text. forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("TypeNamespace = 0", ".Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		// The flags: 0x200000 = forwarder, 1 = public, 2 = nested public
		[InlineData("Flags = 0", "System.Text.Encoder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Flags = 1", "System.Text.Encoder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Flags = 2097153", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Flags = 2097154", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Flags = 4294967295", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		// The token that the type has in its assembly: only a hint
		[InlineData("TypeDefId = 33554433", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("TypeDefId = 4294967295", "System.Text.Encoder forwarder in the core library, System.Version forwarder in the core library", "System.Text.Encoder: found, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		// Where the type is, a coded token: the row number and 0 = File, 1 = AssemblyRef, 2 = ExportedType,
		// 3 = not used
		[InlineData("Implementation = 0", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 4", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: throws BadImageFormatException, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 1", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: throws BadImageFormatException, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 4001", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: throws BadImageFormatException, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 2", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "throws BadImageFormatException")]
		[InlineData("Implementation = itself", "System.Text.Encoder forwarder in exported type Encoder, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = the row of Version", "System.Text.Encoder forwarder in exported type Version, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 4002", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "throws BadImageFormatException")]
		[InlineData("Implementation = 3", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		[InlineData("Implementation = 7", "System.Text.Encoder forwarder in null, System.Version forwarder in the core library", "System.Text.Encoder: null, System.Version: found, System.Text.Version: null, System.Encoder: null, Encoder: null, System.Text.: null")]
		public void ForwardedType(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			// Only .NET is asked: the test run on .NET Framework never ended when these tests were added, and which
			// of their cases it doesn't return from isn't known yet
			ClrReaderComparison.AssertReadAskOnlyDotNet(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
