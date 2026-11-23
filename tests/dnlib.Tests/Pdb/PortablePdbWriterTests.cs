// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;
using SequencePoint = dnlib.DotNet.Pdb.SequencePoint;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Changes debug info with dnlib's API, writes a portable PDB and checks the file. The file is read with
	/// System.Reflection.Metadata (SRM), which is also what the C# compiler and the debuggers use.
	/// </summary>
	public class PortablePdbWriterTests {
		static MetadataReaderProvider OpenWithSrm(byte[] pdb) => MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb));

		// Changes the module that DebugInfoSource creates and writes it. The methods keep their tokens.
		static CompiledAssembly ChangeAndWrite(Action<ModuleDefMD> change, Action<ModuleWriterOptions>? changeOptions = null) {
			using (var module = DebugInfoSource.Load()) {
				change(module);
				return RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true, changeOptions);
			}
		}

		static uint GetMethodRid(string methodName) {
			using (var module = DebugInfoSource.Load())
				return DebugInfoSource.GetMethod(module, methodName).Rid;
		}

		// Fixed in 155e7de3: the MethodDebugInformation table was empty if no method had sequence points. That's
		// valid, but many readers then don't read the scopes and locals.
		[Fact]
		public void EveryMethodHasADebugInformationRowIfThereAreNoSequencePoints() {
			int methodCount = 0;
			var written = ChangeAndWrite(module => {
				foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
					methodCount++;
					foreach (var instruction in method.Body!.Instructions)
						instruction.SequencePoint = null;
				}
			});

			using (var pdb = OpenWithSrm(written.Pdb!)) {
				var reader = pdb.GetMetadataReader();
				Assert.Equal(methodCount, reader.MethodDebugInformation.Count);
				Assert.All(reader.MethodDebugInformation, a => Assert.True(reader.GetMethodDebugInformation(a).SequencePointsBlob.IsNil));
			}
			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!))
				Assert.Equal("sum", DebugInfoSource.GetRootScope(module, "Sum").Variables.Single().Name);
		}

		// Fixed in 2f77c12f: a sequence point with no width (same start and end) was written as the two zeros
		// that mean "hidden sequence point", so the statement's line was lost. Microsoft's PDB converter writes
		// the whole line if the column is 0 (C++/CLI does that), and one column else.
		[Fact]
		public void SequencePointWithoutWidthIsNotWrittenAsAHiddenOne() {
			var written = ChangeAndWrite(module => {
				var sequencePoints = DebugInfoSource.GetMethod(module, "Sum").Body!.Instructions.Select(a => a.SequencePoint).OfType<SequencePoint>().ToList();
				SetPosition(sequencePoints[0], line: 20, column: 0);
				SetPosition(sequencePoints[1], line: 21, column: 7);
			});

			using (var pdb = OpenWithSrm(written.Pdb!)) {
				var reader = pdb.GetMetadataReader();
				var debugInformation = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle((int)GetMethodRid("Sum")));
				var sequencePoints = debugInformation.GetSequencePoints().ToList();

				Assert.False(sequencePoints[0].IsHidden);
				Assert.Equal((20, 0, 20, 0xFFFE), (sequencePoints[0].StartLine, sequencePoints[0].StartColumn, sequencePoints[0].EndLine, sequencePoints[0].EndColumn));
				Assert.False(sequencePoints[1].IsHidden);
				Assert.Equal((21, 7, 21, 8), (sequencePoints[1].StartLine, sequencePoints[1].StartColumn, sequencePoints[1].EndLine, sequencePoints[1].EndColumn));
			}
		}

		static void SetPosition(SequencePoint sequencePoint, int line, int column) {
			sequencePoint.StartLine = line;
			sequencePoint.EndLine = line;
			sequencePoint.StartColumn = column;
			sequencePoint.EndColumn = column;
		}

		// Writes the PDB with a changed document (there's only one) and returns the PDB file
		static byte[] WriteWithChangedDocument(Action<PdbDocument> change) =>
			ChangeAndWrite(module => change(module.PdbState!.Documents.Single())).Pdb!;

		// Fixed in 255b9f7b: a name is stored as its parts and the separator between them. The writer used the
		// separator of the OS it ran on, so the file depended on the OS. Now it does what SRM does: '/' unless
		// the name has more '\'.
		[Theory]
		[InlineData(@"C:\src\project\File.cs", '\\')]
		[InlineData("/src/project/File.cs", '/')]
		[InlineData(@"C:\src/project/sub/File.cs", '/')]
		[InlineData(@"\\server\share/File.cs", '\\')]
		[InlineData("File.cs", '/')]
		public void DocumentNameIsSplitAtTheSeparatorItUsesMost(string name, char separator) {
			var writtenPdb = WriteWithChangedDocument(document => document.Url = name);

			using (var pdb = OpenWithSrm(writtenPdb)) {
				var reader = pdb.GetMetadataReader();
				var document = reader.GetDocument(reader.Documents.Single());
				Assert.Equal(name, reader.GetString(document.Name));
				Assert.Equal((byte)separator, reader.GetBlobBytes(document.Name)[0]);
			}
		}

		// Fixed in 1b491f3a: an all-zero GUID was added to the #GUID heap. The compiler writes "no GUID".
		[Fact]
		public void DocumentWithoutLanguageAndHashHasNoGuids() {
			var writtenPdb = WriteWithChangedDocument(document => {
				document.Language = Guid.Empty;
				document.CheckSumAlgorithmId = Guid.Empty;
				document.CheckSum = Array.Empty<byte>();
			});

			using (var pdb = OpenWithSrm(writtenPdb)) {
				var reader = pdb.GetMetadataReader();
				var document = reader.GetDocument(reader.Documents.Single());
				Assert.True(document.Language.IsNil);
				Assert.True(document.HashAlgorithm.IsNil);
				Assert.True(document.Hash.IsNil);
			}
		}

		// Fixed in 2f20e7b3: half of a surrogate pair isn't valid UTF-16. The compiler writes it as if it were a
		// normal character (3 bytes), dnlib wrote U+FFFD (replacement character).
		[Fact]
		public void HalfASurrogatePairInADocumentNameIsKept() {
			var writtenPdb = WriteWithChangedDocument(document => document.Url = "half\uD800pair.cs");

			var expectedBytes = Encoding.ASCII.GetBytes("half").Concat(new byte[] { 0xED, 0xA0, 0x80 }).Concat(Encoding.ASCII.GetBytes("pair.cs")).ToArray();
			Assert.True(Contains(writtenPdb, expectedBytes));
		}

		static bool Contains(byte[] data, byte[] part) {
			for (int i = 0; i + part.Length <= data.Length; i++) {
				if (data.Skip(i).Take(part.Length).SequenceEqual(part))
					return true;
			}
			return false;
		}

		// Gets the data of the only custom debug info of a local variable
		static byte[] GetCustomDebugInfoOfLocal(MetadataReader reader, string localName) {
			var local = reader.LocalVariables.Single(a => reader.GetString(reader.GetLocalVariable(a).Name) == localName);
			var customDebugInfo = reader.GetCustomDebugInformation(reader.GetCustomDebugInformation(local).Single());
			return reader.GetBlobBytes(customDebugInfo.Value);
		}

		// Fixed in bb93b4b6 and 390e1244: flags at the end that are false aren't written, but one byte always is:
		// the compiler's reader can't use empty data.
		[Theory]
		[InlineData(new[] { true }, new byte[] { 0x01 })]
		[InlineData(new[] { false, true, false, false, false, false, false, false, true }, new byte[] { 0x02, 0x01 })]
		[InlineData(new[] { true, false, false, false, false, false, false, false, false, false }, new byte[] { 0x01 })]
		[InlineData(new[] { false, false, false }, new byte[] { 0x00 })]
		[InlineData(new bool[0], new byte[] { 0x00 })]
		public void DynamicFlagsAreWrittenWithoutFalseFlagsAtTheEnd(bool[] flags, byte[] expectedData) {
			var written = ChangeAndWrite(module => {
				var local = DebugInfoSource.GetLocal(module, "SpecialLocals", "anything");
				((PdbDynamicLocalVariablesCustomDebugInfo)local.CustomDebugInfos.Single()).Flags = flags;
			});

			using (var pdb = OpenWithSrm(written.Pdb!))
				Assert.Equal(expectedData, GetCustomDebugInfoOfLocal(pdb.GetMetadataReader(), "anything"));
		}

		// Fixed in 9ebc3145: a null name was a writer error. The compiler writes an empty string for a tuple
		// element without a name.
		[Fact]
		public void TupleElementWithoutANameIsAnEmptyString() {
			var written = ChangeAndWrite(module => {
				var local = DebugInfoSource.GetLocal(module, "SpecialLocals", "pair");
				((PortablePdbTupleElementNamesCustomDebugInfo)local.CustomDebugInfos.Single()).Names[0] = null!;
			});

			using (var pdb = OpenWithSrm(written.Pdb!))
				Assert.Equal(Encoding.ASCII.GetBytes("\0second\0"), GetCustomDebugInfoOfLocal(pdb.GetMetadataReader(), "pair"));
		}

		static byte[] GetConstantSignature(MetadataReader reader, string constantName) {
			var constant = reader.LocalConstants.Select(a => reader.GetLocalConstant(a)).Single(a => reader.GetString(a.Name) == constantName);
			return reader.GetBlobBytes(constant.Signature);
		}

		// Fixed in 0f11b68b: these constants can't be created with C#, but a Windows PDB can have them. The
		// file format only allows a few types at the start of a constant's signature. The compiler writes the
		// other types as "class" + a TypeSpec, dnlib wrote them like in other signatures and nothing could read
		// them.
		[Fact]
		public void PointerConstantIsWrittenAsATypeSpec() {
			var written = ChangeAndWrite(module => {
				var pointerType = new PtrSig(module.CorLibTypes.Int32);
				DebugInfoSource.GetRootScope(module, "Sum").Constants.Add(new PdbConstant("NoPointer", pointerType, null));
			});

			using (var pdb = OpenWithSrm(written.Pdb!)) {
				var signature = GetConstantSignature(pdb.GetMetadataReader(), "NoPointer");
				Assert.Equal((byte)ElementType.Class, signature[0]);
				// The rest is the compressed TypeSpec token and no value (it's null)
				unsafe {
					fixed (byte* p = signature) {
						var blobReader = new BlobReader(p + 1, signature.Length - 1);
						Assert.Equal(HandleKind.TypeSpecification, blobReader.ReadTypeHandle().Kind);
						Assert.Equal(0, blobReader.RemainingBytes);
					}
				}
			}
			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var constant = DebugInfoSource.GetRootScope(module, "Sum").Constants.Single(a => a.Name == "NoPointer");
				Assert.Equal("System.Int32*", constant.Type!.FullName);
				Assert.Null(constant.Value);
			}
		}

		// Fixed in 0f11b68b: like the compiler, a native int constant is written as the type of its value
		[Fact]
		public void NativeIntConstantIsWrittenAsItsValuesType() {
			var written = ChangeAndWrite(module =>
				DebugInfoSource.GetRootScope(module, "Sum").Constants.Add(new PdbConstant("Native", module.CorLibTypes.IntPtr, 0x12345678)));

			using (var pdb = OpenWithSrm(written.Pdb!)) {
				var signature = GetConstantSignature(pdb.GetMetadataReader(), "Native");
				Assert.Equal(new byte[] { (byte)ElementType.I4, 0x78, 0x56, 0x34, 0x12 }, signature);
			}
		}

		const string EnumLibrarySource = @"
public enum SmallEnum : byte { Zero, One, Two, Three }
public enum SignedEnum : sbyte { MinusTwo = -2 }
public enum BoolEnum : byte { }
public enum CharEnum : ushort { }
";

		// The enums above. C# has no bool and char enums, but IL has: their underlying types are changed.
		static ModuleDefMD LoadEnumLibrary() {
			var module = ModuleDefMD.Load(CSharpCompiler.Compile(EnumLibrarySource, new CSharpCompilerOptions { AssemblyName = "EnumLibrary" }).Image);
			SetUnderlyingType(module, "BoolEnum", module.CorLibTypes.Boolean);
			SetUnderlyingType(module, "CharEnum", module.CorLibTypes.Char);
			return module;
		}

		static void SetUnderlyingType(ModuleDef module, string enumName, TypeSig underlyingType) {
			// The type of an enum's only instance field
			var valueField = module.Find(enumName, isReflectionName: true)!.Fields.Single(a => !a.IsStatic);
			valueField.FieldSig!.Type = underlyingType;
		}

		// An assembly resolver that only knows one assembly
		sealed class OneAssemblyResolver : IAssemblyResolver {
			readonly AssemblyDef assembly;
			public OneAssemblyResolver(AssemblyDef assembly) => this.assembly = assembly;
			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) =>
				assembly?.Name == this.assembly.Name ? this.assembly : null;
		}

		// A Windows PDB stores a Boolean, Char, SByte or Byte constant as a 16-bit number, and that's what the
		// constant's value is if dnlib can't know that its type is an enum: the enum is in another assembly, and
		// no other assemblies are read when a PDB is read. The portable PDB writer knows the enum's underlying
		// type if it finds the assembly. The compiler writes the underlying type, the value and the enum type.
		[Theory]
		[InlineData("SmallEnum", (short)3, new byte[] { (byte)ElementType.U1, 3 })]
		[InlineData("SignedEnum", (short)-2, new byte[] { (byte)ElementType.I1, 0xFE })]
		[InlineData("BoolEnum", (short)1, new byte[] { (byte)ElementType.Boolean, 1 })]
		[InlineData("CharEnum", (ushort)'x', new byte[] { (byte)ElementType.Char, (byte)'x', 0 })]
		public void EnumConstantWithA16BitValueIsWrittenAsItsUnderlyingType(string enumName, object valueInWindowsPdb, byte[] expectedStart) {
			using (var library = LoadEnumLibrary()) {
				var written = ChangeAndWrite(module => {
					module.Context = new ModuleContext(new OneAssemblyResolver(library.Assembly!));
					module.Context.Resolver = new Resolver(module.Context.AssemblyResolver);
					var enumType = new TypeRefUser(module, string.Empty, enumName, library.Assembly!.ToAssemblyRef());
					DebugInfoSource.GetRootScope(module, "Sum").Constants.Add(new PdbConstant("EnumConstant", new ValueTypeSig(enumType), valueInWindowsPdb));
				});

				using (var pdb = OpenWithSrm(written.Pdb!)) {
					var signature = GetConstantSignature(pdb.GetMetadataReader(), "EnumConstant");
					Assert.Equal(expectedStart, signature.Take(expectedStart.Length));
					// The rest is the compressed token of the enum type
					Assert.Equal(expectedStart.Length + 1, signature.Length);
				}
			}
		}

		// Fixed in 5ea0663e: the LocalScope rows of a method are sorted by where they start and by their length.
		// A reader puts a scope into the scope before it if it has the same range, but the writer didn't keep
		// such scopes in their order: the locals and constants of a scope moved to another scope.
		[Fact]
		public void NestedScopesWithTheSameRangeStayInTheirOrder() {
			const int depth = 40;
			var written = ChangeAndWrite(module => {
				var scope = DebugInfoSource.GetRootScope(module, "Sum");
				for (int i = 1; i <= depth; i++) {
					var child = new PdbScope { Start = scope.Start, End = scope.End };
					child.Constants.Add(new PdbConstant("Depth" + i, module.CorLibTypes.Int32, i));
					scope.Scopes.Add(child);
					scope = child;
				}
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var scope = DebugInfoSource.GetRootScope(module, "Sum");
				Assert.Equal("sum", scope.Variables.Single().Name);
				for (int i = 1; i <= depth; i++) {
					scope = scope.Scopes.Single(a => a.Constants.Count != 0);
					Assert.Equal("Depth" + i, scope.Constants.Single().Name);
				}
			}
		}

		// The names of the streams (the tables and the heaps) in a metadata file
		static List<string> GetStreamNames(byte[] metadata) {
			var reader = new BinaryReader(new MemoryStream(metadata));
			// Signature, major and minor version, reserved
			reader.BaseStream.Position = 4 + 2 + 2 + 4;
			int versionLength = reader.ReadInt32();
			reader.BaseStream.Position += versionLength;
			// Flags
			reader.BaseStream.Position += 2;
			int streamCount = reader.ReadUInt16();
			var names = new List<string>();
			for (int i = 0; i < streamCount; i++) {
				// Offset and size
				reader.BaseStream.Position += 4 + 4;
				// The name ends with a zero and is followed by zeros until the next offset that's a multiple of 4
				var name = new StringBuilder();
				for (byte b = reader.ReadByte(); b != 0; b = reader.ReadByte())
					name.Append((char)b);
				reader.BaseStream.Position = (reader.BaseStream.Position + 3) & ~3;
				names.Add(name.ToString());
			}
			return names;
		}

		// Fixed in c71c4f28: an empty heap wasn't written, and some readers (eg. Unity's Mono) need all of them.
		// A PDB never has user strings (#US).
		[Fact]
		public void AllHeapsAreWrittenByDefault() {
			var written = ChangeAndWrite(module => { });

			Assert.Equal(new[] { "#Blob", "#GUID", "#Pdb", "#Strings", "#US", "#~" }, GetStreamNames(written.Pdb!).OrderBy(a => a, StringComparer.Ordinal));
		}

		[Fact]
		public void EmptyHeapsAreNotWrittenIfTheDebugFlagsAreCleared() {
			var written = ChangeAndWrite(module => { }, options => options.MetadataOptions.DebugFlags = 0);

			Assert.Equal(new[] { "#Blob", "#GUID", "#Pdb", "#Strings", "#~" }, GetStreamNames(written.Pdb!).OrderBy(a => a, StringComparer.Ordinal));
		}

		// Fixed in d99579cd: the PDB stream had to be empty. If it wasn't, the PDB ID was written to the wrong
		// place, the PDB's checksum included the data before it, and old data after it stayed in the stream.
		[Fact]
		public void PdbIsWrittenAtTheStreamsPosition() {
			var expected = ChangeAndWrite(module => { });
			var stream = new MemoryStream();
			var dataBeforePdb = Enumerable.Repeat((byte)0xAA, 10).ToArray();
			stream.Write(dataBeforePdb, 0, dataBeforePdb.Length);
			// Old data that's longer than the PDB
			stream.Write(new byte[expected.Pdb!.Length * 2], 0, expected.Pdb.Length * 2);
			stream.Position = dataBeforePdb.Length;

			var written = ChangeAndWrite(module => { }, options => options.PdbStream = stream);

			Assert.Equal(expected.Image, written.Image);
			Assert.Equal(dataBeforePdb.Concat(expected.Pdb), stream.ToArray());
			Assert.Equal(stream.Length, stream.Position);
		}

		// Fixed in d99579cd: the writer threw
		[Fact]
		public void PdbCanBeWrittenToAStreamThatCanNotSeek() {
			var expected = ChangeAndWrite(module => { });
			var stream = new WriteOnlyStream();

			ChangeAndWrite(module => { }, options => options.PdbStream = stream);

			Assert.Equal(expected.Pdb, stream.GetWrittenData());
		}

		// The PDB's ID is in the PE file, and a checksum of the PDB too if the original file had one. dnlib
		// checks both when it reads the files.
		[Fact]
		public void WrittenPdbHasTheIdAndTheChecksumThatAreInThePEFile() {
			var written = ChangeAndWrite(module => { });

			var options = new ModuleCreationOptions {
				PdbFileOrData = written.Pdb,
				PdbOptions = PdbReaderOptions.ValidatePdbChecksum,
				TryToLoadPdbFromDisk = false,
			};
			using (var module = ModuleDefMD.Load(written.Image, options))
				Assert.NotNull(module.PdbState);

			var changedPdb = (byte[])written.Pdb!.Clone();
			changedPdb[changedPdb.Length - 1] ^= 1;
			options.PdbFileOrData = changedPdb;
			using (var module = ModuleDefMD.Load(written.Image, options))
				Assert.Null(module.PdbState);
		}
	}
}
