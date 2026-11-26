// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// All files that dnlib reads and writes are little endian: the first byte of a number is its lowest byte.
	/// The code must not depend on the byte order of the CPU it runs on (.NET also runs on big endian CPUs,
	/// eg. s390x), so it can't copy a number to or from memory, or use <see cref="BitConverter"/>.
	/// These tests check the bytes of the code that was changed for GitHub discussion 563. They pass on any
	/// CPU, but only a big endian CPU would have shown the bugs.
	/// </summary>
	public unsafe class ByteOrderTests {
		static string Hex(byte[] bytes) => BitConverter.ToString(bytes);

		// Fixed in this commit: 4 and 8 byte values were copied to the array with the byte order of the CPU
		[Fact]
		public void ArrayWriterWritesLittleEndianValues() {
			var data = new byte[1 + 1 + 2 + 2 + 4 + 4 + 8 + 8 + 4 + 8];
			var writer = new ArrayWriter(data);

			writer.WriteSByte(-2);
			writer.WriteByte(0x12);
			writer.WriteInt16(unchecked((short)0x8234));
			writer.WriteUInt16(0xFEDC);
			writer.WriteInt32(unchecked((int)0x81234567));
			writer.WriteUInt32(0xFEDCBA98);
			writer.WriteInt64(unchecked((long)0x8123456789ABCDEF));
			writer.WriteUInt64(0xFEDCBA9876543210);
			writer.WriteSingle(1.5f);
			writer.WriteDouble(-2.25);

			Assert.Equal(data.Length, writer.Position);
			Assert.Equal("FE-12-34-82-DC-FE-67-45-23-81-98-BA-DC-FE-EF-CD-AB-89-67-45-23-81-10-32-54-76-98-BA-DC-FE" +
				"-00-00-C0-3F-00-00-00-00-00-00-02-C0", Hex(data));
		}

		[Fact]
		public void ArrayWriterWritesAtItsPositionAndNotPastTheEnd() {
			var data = new byte[9];
			var writer = new ArrayWriter(data);
			writer.Position = 3;

			writer.WriteUInt32(0x11223344);

			Assert.Equal(7, writer.Position);
			Assert.Equal("00-00-00-44-33-22-11-00-00", Hex(data));
			// Fixed in this commit: memory after the array was written
			Assert.Throws<IndexOutOfRangeException>(() => writer.WriteUInt32(1));
			Assert.Throws<IndexOutOfRangeException>(() => writer.WriteDouble(1));
		}

		// The way a UTF-16 string is read if the CPU isn't little endian. A little endian CPU copies the memory.
		[Theory]
		[InlineData("")]
		[InlineData("a")]
		[InlineData("text é 中文 😀 \0 end")]
		[InlineData("\uD800 ￿﻿Ā\u0001")]
		public void Utf16StringIsReadByteByByte(string text) {
			// The bytes of the file: low byte first. One byte before the string so that it's not aligned.
			var bytes = new byte[1 + text.Length * 2 + 1];
			for (int i = 0; i < text.Length; i++) {
				bytes[1 + i * 2] = (byte)text[i];
				bytes[1 + i * 2 + 1] = (byte)(text[i] >> 8);
			}

			fixed (byte* p = bytes)
				Assert.Equal(text, DataStream.ReadUtf16StringByteByByte(p + 1, text.Length));

			// Every stream reads the same string on this CPU
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					reader.Position = 1;
					Assert.Equal(text, reader.ReadUtf16String(text.Length));
				}
			}
		}

		// The streams that copy numbers from memory are only used by little endian CPUs
		[Fact]
		public void StreamOfTheFactoryReadsLittleEndianValues() {
			var bytes = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x41, 0x00, 0x42, 0x00 };
			var stream = DataStreamFactory.Create(bytes);

			if (!BitConverter.IsLittleEndian)
				Assert.StartsWith("Aligned", stream.GetType().Name);
			Assert.Equal(0x0201, stream.ReadUInt16(1));
			Assert.Equal(0x04030201U, stream.ReadUInt32(1));
			Assert.Equal(0x0807060504030201UL, stream.ReadUInt64(1));
			Assert.Equal(BitConverter.Int64BitsToDouble(0x0807060504030201), stream.ReadDouble(1));
			Assert.Equal("AB", stream.ReadUtf16String(9, 2));
			fixed (byte* p = bytes) {
				var nativeStream = DataStreamFactory.Create(p);
				if (!BitConverter.IsLittleEndian)
					Assert.StartsWith("Aligned", nativeStream.GetType().Name);
				Assert.Equal(0x04030201U, nativeStream.ReadUInt32(1));
				Assert.Equal(0x0807060504030201UL, nativeStream.ReadUInt64(1));
				Assert.Equal("AB", nativeStream.ReadUtf16String(9, 2));
			}
		}

		// Fixed in this commit: the writer created the bytes of a constant with BitConverter
		[Fact]
		public void ConstantsAreWrittenAsLittleEndianValues() {
			var module = new ModuleDefUser("test.dll");
			new AssemblyDefUser("test").Modules.Add(module);
			var type = new TypeDefUser("", "Constants", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			var types = module.CorLibTypes;
			var constants = new (string name, CorLibTypeSig type, object value, string bytes)[] {
				("Boolean", types.Boolean, true, "01"),
				("False", types.Boolean, false, "00"),
				("Char", types.Char, '\u1234', "34-12"),
				("SByte", types.SByte, (sbyte)-2, "FE"),
				("Byte", types.Byte, (byte)0x12, "12"),
				("Int16", types.Int16, unchecked((short)0x8234), "34-82"),
				("UInt16", types.UInt16, (ushort)0xFEDC, "DC-FE"),
				("Int32", types.Int32, unchecked((int)0x81234567), "67-45-23-81"),
				("UInt32", types.UInt32, 0xFEDCBA98, "98-BA-DC-FE"),
				("Int64", types.Int64, unchecked((long)0x8123456789ABCDEF), "EF-CD-AB-89-67-45-23-81"),
				("UInt64", types.UInt64, 0xFEDCBA9876543210, "10-32-54-76-98-BA-DC-FE"),
				("Single", types.Single, 1.5f, "00-00-C0-3F"),
				("Double", types.Double, -2.25, "00-00-00-00-00-00-02-C0"),
				("String", types.String, "a\u4E2D", "61-00-2D-4E"),
			};
			foreach (var constant in constants) {
				var field = new FieldDefUser(constant.name, new FieldSig(constant.type), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
				field.Constant = new ConstantUser(constant.value, constant.type.ElementType);
				type.Fields.Add(field);
			}
			var stream = new System.IO.MemoryStream();

			module.Write(stream);

			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				var writtenType = written.Find("Constants", isReflectionName: true)!;
				foreach (var constant in constants) {
					var field = writtenType.FindField(constant.name)!;
					uint rid = written.Metadata.GetConstantRid(Table.Field, field.Rid);
					Assert.True(written.TablesStream.TryReadConstantRow(rid, out var row));
					Assert.Equal(constant.bytes, Hex(written.BlobStream.Read(row.Value)!));
					Assert.Equal(constant.value, field.Constant!.Value);
				}
			}
		}

		// Used instead of BitConverter, which uses the byte order of the CPU
		[Fact]
		public void HelpersReadAndCreateLittleEndianBytes() {
			var data = new byte[] { 0x00, 0x78, 0x56, 0x34, 0x92, 0xFF };

			Assert.Equal(0x92345678U, dnlib.DotNet.Utils.ReadUInt32LittleEndian(data, 1));
			Assert.Equal(unchecked((int)0x92345678), dnlib.DotNet.Utils.ReadInt32LittleEndian(data, 1));
			Assert.Equal(0xFF923456U, dnlib.DotNet.Utils.ReadUInt32LittleEndian(data, 2));
			Assert.Throws<IndexOutOfRangeException>(() => dnlib.DotNet.Utils.ReadUInt32LittleEndian(data, 3));

			Assert.Equal("34-12", Hex(dnlib.DotNet.Utils.GetLittleEndianBytes((ushort)0x1234)));
			Assert.Equal("78-56-34-12", Hex(dnlib.DotNet.Utils.GetLittleEndianBytes(0x12345678U)));
			Assert.Equal("EF-CD-AB-89-67-45-23-01", Hex(dnlib.DotNet.Utils.GetLittleEndianBytes(0x0123456789ABCDEFUL)));
			Assert.Equal("00-00-C0-3F", Hex(dnlib.DotNet.Utils.GetLittleEndianBytes(1.5f)));
			Assert.Equal("00-00-00-00-00-00-02-C0", Hex(dnlib.DotNet.Utils.GetLittleEndianBytes(-2.25)));
		}
	}
}
