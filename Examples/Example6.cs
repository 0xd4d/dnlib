using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.Examples {
	/// <summary>
	/// This example shows how to create a module writer listener that gets notified of various
	/// events. This listener just adds a new PE section to the image and prints the new RIDs.
	/// It also shows how to add some dummy .NET heaps, and simple obfuscation that will break
	/// most libraries that open .NET assemblies.
	/// </summary>
	public class Example6 : IModuleWriterListener {
		public static void Run() {
			new Example6().DoIt();
		}

		void DoIt() {
			string destFileName = @"c:\output.dll";

			// Open the current module
			var mod = ModuleDefMD.Load(typeof(Example6).Module);

			// Create the writer options
			var opts = new ModuleWriterOptions(mod);

			// Add a listener that gets notified during the writing process
			opts.Listener = this;

			// This is normally 16 but setting it to a value less than 14 will fool some
			// apps into thinking that there's no .NET metadata available
			opts.PEHeadersOptions.NumberOfRvaAndSizes = 13;

			// Add extra data. This will break most libraries that open .NET assemblies.
			// Any value can be written here.
			opts.MetaDataOptions.TablesHeapOptions.ExtraData = 0x12345678;

			// Add a few dummy heaps
			opts.MetaDataOptions.OtherHeaps.Add(new MyHeap("#US "));
			opts.MetaDataOptions.OtherHeaps.Add(new MyHeap("#Strings "));
			opts.MetaDataOptions.OtherHeaps.Add(new MyHeap("#Strimgs"));
			opts.MetaDataOptions.OtherHeaps.Add(new MyHeap("#GU1D"));
			opts.MetaDataOptions.OtherHeapsEnd.Add(new MyHeap("#US "));
			opts.MetaDataOptions.OtherHeapsEnd.Add(new MyHeap("#Strings "));

			// Write the module. The listener will get notified, see OnWriterEvent() below
			mod.Write(destFileName, opts);
		}

		// A simple heap (must implement the IHeap interface). It just writes 10 bytes to the file.
		class MyHeap : IHeap {
			string name;
			FileOffset offset;
			RVA rva;

			// This is the data. I chose 10 bytes, but any non-zero value can be used
			byte[] heapData = new byte[10];

			public MyHeap(string name) {
				this.name = name;
			}

			// The rest of the code is just for implementing the required interface

			public string Name {
				get { return name; }
			}

			public bool IsEmpty {
				get { return false; }
			}

			public void SetReadOnly() {
			}

			public FileOffset FileOffset {
				get { return offset; }
			}

			public RVA RVA {
				get { return rva; }
			}

			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}

			public uint GetFileLength() {
				return (uint)heapData.Length;
			}

			public uint GetVirtualSize() {
				return GetFileLength();
			}

			public void WriteTo(BinaryWriter writer) {
				writer.Write(heapData);
			}
		}

		// Gets notified during module writing
		public void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt) {
			switch (evt) {
			case ModuleWriterEvent.PESectionsCreated:
				// Add a PE section
				var sect1 = new PESection(".dummy", 0x40000040);
				writer.Sections.Add(sect1);
				// Let's add data
				sect1.Add(new ByteArrayChunk(new byte[123]), 4);
				sect1.Add(new ByteArrayChunk(new byte[10]), 4);
				break;

			case ModuleWriterEvent.MDEndCreateTables:
				// All types, methods etc have gotten their new RIDs. Let's print the new values
				Console.WriteLine("Old -> new type and method tokens");
				foreach (var type in writer.Module.GetTypes()) {
					Console.WriteLine("TYPE: {0:X8} -> {1:X8} {2}",
						type.MDToken.Raw,
						new MDToken(Table.TypeDef, writer.MetaData.GetRid(type)).Raw,
						type.FullName);
					foreach (var method in type.Methods)
						Console.WriteLine("  METH: {0:X8} -> {1:X8} {2}",
							method.MDToken.Raw,
							new MDToken(Table.Method, writer.MetaData.GetRid(method)).Raw,
							method.FullName);
				}
				break;

			default:
				break;
			}
		}
	}
}
