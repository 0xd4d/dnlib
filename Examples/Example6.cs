using System;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.Examples {
	/// <summary>
	/// This example shows how to add a module writer listener that gets notified of various
	/// events. This listener just adds a new PE section to the image and prints the new RIDs.
	/// It also shows how to add some dummy .NET heaps, and simple obfuscation that will break
	/// most libraries that open .NET assemblies.
	/// </summary>
	public class Example6 {
		public static void Run() => new Example6().DoIt();

		void DoIt() {
			string destFileName = @"c:\output.dll";

			// Open the current module
			var mod = ModuleDefMD.Load(typeof(Example6).Module);

			// Create the writer options
			var opts = new ModuleWriterOptions(mod);

			// Add a listener that gets notified during the writing process
			opts.WriterEvent += OnWriterEvent;

			// This is normally 16 but setting it to a value less than 14 will fool some
			// apps into thinking that there's no .NET metadata available
			opts.PEHeadersOptions.NumberOfRvaAndSizes = 13;

			// Add extra data. This will break most libraries that open .NET assemblies.
			// Any value can be written here.
			opts.MetadataOptions.TablesHeapOptions.ExtraData = 0x12345678;

			// Add a few dummy heaps
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#US "));
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#Strings "));
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#Strimgs"));
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#GU1D"));
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#US "));
			opts.MetadataOptions.CustomHeaps.Add(new MyHeap("#Strings "));
			opts.MetadataOptions.MetadataHeapsAdded += (s, e) => {
				// You could sort the heaps here
			};

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

			public MyHeap(string name) => this.name = name;

			// The rest of the code is just for implementing the required interface

			public string Name => name;
			public bool IsEmpty => false;

			public void SetReadOnly() {
			}

			public FileOffset FileOffset => offset;
			public RVA RVA => rva;

			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}

			public uint GetFileLength() => (uint)heapData.Length;
			public uint GetVirtualSize() => GetFileLength();
			public void WriteTo(DataWriter writer) => writer.WriteBytes(heapData);
		}

		// Gets notified during module writing
		void OnWriterEvent(object sender, ModuleWriterEventArgs e) {
			switch (e.Event) {
			case ModuleWriterEvent.PESectionsCreated:
				// Add a PE section
				var sect1 = new PESection(".dummy", 0x40000040);
				e.Writer.Sections.Add(sect1);
				// Let's add data
				sect1.Add(new ByteArrayChunk(new byte[123]), 4);
				sect1.Add(new ByteArrayChunk(new byte[10]), 4);
				break;

			case ModuleWriterEvent.MDEndCreateTables:
				// All types, methods etc have gotten their new RIDs. Let's print the new values
				Console.WriteLine("Old -> new type and method tokens");
				foreach (var type in e.Writer.Module.GetTypes()) {
					Console.WriteLine("TYPE: {0:X8} -> {1:X8} {2}",
						type.MDToken.Raw,
						new MDToken(Table.TypeDef, e.Writer.Metadata.GetRid(type)).Raw,
						type.FullName);
					foreach (var method in type.Methods)
						Console.WriteLine("  METH: {0:X8} -> {1:X8} {2}",
							method.MDToken.Raw,
							new MDToken(Table.Method, e.Writer.Metadata.GetRid(method)).Raw,
							method.FullName);
				}
				break;

			default:
				break;
			}
		}
	}
}
