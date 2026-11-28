// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The heaps of the writer (#Strings, #US, #Blob, #GUID) are <see cref="IOffsetHeap{TValue}"/>s: while the
	/// module is written, a listener of the writer's events can get the bytes of every value that was added and
	/// its offset, and replace them with other bytes of the same length, eg. to patch a value in the file.
	/// </summary>
	public class HeapRawDataTests {
		const int Number = 0x12345678;
		static readonly Guid NewMvid = new Guid("01234567-89ab-cdef-0123-456789abcdef");

		// Ns.Type has a field "Hello" with the constant Number and a method that loads the string "Hello"
		static CreatedModule CreateModule() {
			var created = new CreatedModule();
			var field = created.AddField("Hello", created.Module.CorLibTypes.Int32);
			field.Attributes |= FieldAttributes.Literal | FieldAttributes.HasDefault;
			field.Constant = new ConstantUser(Number);
			created.AddMethod("Method", OpCodes.Ldstr.ToInstruction("Hello"), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			return created;
		}

		// Calls 'change' with the writer's metadata when its heaps have all their values and before they are written (the #Strings heap gets its last strings when the offsets of the chunks are calculated)
		static byte[] Write(CreatedModule created, Action<Metadata> change) {
			var options = new ModuleWriterOptions(created.Module);
			options.WriterEvent += (sender, e) => {
				if (e.Event == ModuleWriterEvent.BeginWriteChunks)
					change(e.Writer.Metadata);
			};
			var stream = new MemoryStream();
			created.Module.Write(stream, options);
			return stream.ToArray();
		}

		static KeyValuePair<uint, byte[]> Find<T>(IOffsetHeap<T> heap, byte[] rawData) =>
			Assert.Single(heap.GetAllRawData(), a => a.Value.SequenceEqual(rawData));

		static byte[] Replace(byte[] data, byte[] oldBytes, byte[] newBytes) {
			var result = (byte[])data.Clone();
			for (int i = 0; i + oldBytes.Length <= data.Length; i++) {
				if (data.Skip(i).Take(oldBytes.Length).SequenceEqual(oldBytes)) {
					Array.Copy(newBytes, 0, result, i, newBytes.Length);
					return result;
				}
			}
			throw new InvalidOperationException("Not found");
		}

		// A string is stored as UTF-8 bytes and a 0 byte
		[Fact]
		public void AStringIsReplaced() {
			var image = Write(CreateModule(), metadata => {
				var heap = metadata.StringsHeap;
				var raw = Encoding.UTF8.GetBytes("Hello\0");
				Assert.Equal(raw.Length, heap.GetRawDataSize("Hello"));
				var hello = Find(heap, raw);
				heap.SetRawData(hello.Key, Encoding.UTF8.GetBytes("Jello\0"));
			});

			using (var module = ModuleDefMD.Load(image)) {
				var type = CreatedModule.GetType(module);
				Assert.NotNull(type.FindField("Jello"));
				Assert.Null(type.FindField("Hello"));
			}
		}

		// A user string is stored as its length, UTF-16 characters and a byte that says if it has special characters
		[Fact]
		public void AUserStringIsReplaced() {
			var image = Write(CreateModule(), metadata => {
				var heap = metadata.USHeap;
				var hello = Assert.Single(heap.GetAllRawData());
				Assert.Equal(hello.Value.Length, heap.GetRawDataSize("Hello"));
				heap.SetRawData(hello.Key, Replace(hello.Value, Encoding.Unicode.GetBytes("H"), Encoding.Unicode.GetBytes("Y")));
			});

			using (var module = ModuleDefMD.Load(image)) {
				var method = CreatedModule.GetType(module).FindMethod("Method")!;
				Assert.Equal("Yello", method.Body!.Instructions[0].Operand);
			}
		}

		// A blob is stored as its compressed length and its bytes
		[Fact]
		public void ABlobIsReplaced() {
			var image = Write(CreateModule(), metadata => {
				var heap = metadata.BlobHeap;
				var value = BitConverter.GetBytes(Number);
				Assert.Equal(5, heap.GetRawDataSize(value));
				var constant = Find(heap, new byte[] { 4 }.Concat(value).ToArray());
				heap.SetRawData(constant.Key, new byte[] { 4 }.Concat(BitConverter.GetBytes(42)).ToArray());
			});

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(42, CreatedModule.GetType(module).FindField("Hello")!.Constant!.Value);
		}

		// A GUID is stored as its 16 bytes. The offset is the GUID's index.
		[Fact]
		public void AGuidIsReplaced() {
			var created = CreateModule();
			var image = Write(created, metadata => {
				var heap = metadata.GuidHeap;
				Assert.Equal(16, heap.GetRawDataSize(Guid.Empty));
				var mvid = Find(heap, created.Module.Mvid!.Value.ToByteArray());
				heap.SetRawData(mvid.Key, NewMvid.ToByteArray());
			});

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(NewMvid, module.Mvid);
		}

		[Fact]
		public void TheNewDataMustHaveTheSameLength() {
			Assert.Throws<InvalidOperationException>(() => Write(CreateModule(), metadata => {
				var heap = metadata.StringsHeap;
				heap.SetRawData(Find(heap, Encoding.UTF8.GetBytes("Hello\0")).Key, Encoding.UTF8.GetBytes("Hi\0"));
			}));
			Assert.Throws<InvalidOperationException>(() => Write(CreateModule(), metadata => {
				var heap = metadata.BlobHeap;
				heap.SetRawData(Find(heap, new byte[] { 4 }.Concat(BitConverter.GetBytes(Number)).ToArray()).Key, new byte[] { 1, 2 });
			}));
			Assert.Throws<InvalidOperationException>(() => Write(CreateModule(), metadata => {
				var heap = metadata.USHeap;
				heap.SetRawData(heap.GetAllRawData().Single().Key, new byte[] { 1, 2 });
			}));
		}

		[Fact]
		public void TheNewDataCanNotBeNull() {
			var metadata = Metadata.Create(new ModuleDefUser("M"), new UniqueChunkList<ByteArrayChunk>(), new MethodBodyChunks(false), new NetResources(4));

			Assert.Throws<ArgumentNullException>(() => metadata.StringsHeap.SetRawData(1, null!));
			Assert.Throws<ArgumentNullException>(() => metadata.BlobHeap.SetRawData(1, null!));
			Assert.Throws<ArgumentNullException>(() => metadata.USHeap.SetRawData(1, null!));
		}
	}
}
