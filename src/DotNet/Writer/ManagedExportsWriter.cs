// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	sealed class ManagedExportsWriter {
		const uint DEFAULT_VTBL_FIXUPS_ALIGNMENT = 4;
		const uint DEFAULT_SDATA_ALIGNMENT = 8;
		const StubType stubType = StubType.Export;
		readonly string moduleName;
		readonly Machine machine;
		readonly RelocDirectory relocDirectory;
		readonly MetaData metaData;
		readonly PEHeaders peHeaders;
		readonly LogError logError;
		readonly VtableFixupsChunk vtableFixups;
		readonly StubsChunk stubsChunk;
		readonly SdataChunk sdataChunk;
		readonly ExportDir exportDir;
		readonly List<VTableInfo> vtables;
		readonly List<MethodInfo> allMethodInfos;
		readonly List<MethodInfo> sortedOrdinalMethodInfos;
		readonly List<MethodInfo> sortedNameMethodInfos;
		readonly CpuArch cpuArch;
		uint exportDirOffset;

		bool Is64Bit {
			get { return machine == Machine.IA64 || machine == Machine.AMD64 || machine == Machine.ARM64; }
		}

		FileOffset ExportDirOffset {
			get { return sdataChunk.FileOffset + exportDirOffset; }
		}

		RVA ExportDirRVA {
			get { return sdataChunk.RVA + exportDirOffset; }
		}

		uint ExportDirSize {
			get { return 0x28; }
		}

		internal bool HasExports {
			get { return vtables.Count != 0; }
		}

		sealed class ExportDir : IChunk {
			readonly ManagedExportsWriter owner;

			public FileOffset FileOffset {
				get { return owner.ExportDirOffset; }
			}

			public RVA RVA {
				get { return owner.ExportDirRVA; }
			}

			public ExportDir(ManagedExportsWriter owner) {
				this.owner = owner;
			}

			void IChunk.SetOffset(FileOffset offset, RVA rva) {
				throw new NotSupportedException();
			}

			public uint GetFileLength() {
				return owner.ExportDirSize;
			}

			public uint GetVirtualSize() {
				return GetFileLength();
			}

			void IChunk.WriteTo(BinaryWriter writer) {
				throw new NotSupportedException();
			}
		}

		sealed class VtableFixupsChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;

			public FileOffset FileOffset {
				get { return offset; }
			}

			public RVA RVA {
				get { return rva; }
			}

			public VtableFixupsChunk(ManagedExportsWriter owner) {
				this.owner = owner;
			}

			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}

			public uint GetFileLength() {
				return length;
			}

			public uint GetVirtualSize() {
				return GetFileLength();
			}

			public void WriteTo(BinaryWriter writer) {
				owner.WriteVtableFixups(writer);
			}
		}

		sealed class StubsChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;

			public FileOffset FileOffset {
				get { return offset; }
			}

			public RVA RVA {
				get { return rva; }
			}

			public StubsChunk(ManagedExportsWriter owner) {
				this.owner = owner;
			}

			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}

			public uint GetFileLength() {
				return length;
			}

			public uint GetVirtualSize() {
				return GetFileLength();
			}

			public void WriteTo(BinaryWriter writer) {
				owner.WriteStubs(writer);
			}
		}

		sealed class SdataChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;

			public FileOffset FileOffset {
				get { return offset; }
			}

			public RVA RVA {
				get { return rva; }
			}

			public SdataChunk(ManagedExportsWriter owner) {
				this.owner = owner;
			}

			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}

			public uint GetFileLength() {
				return length;
			}

			public uint GetVirtualSize() {
				return GetFileLength();
			}

			public void WriteTo(BinaryWriter writer) {
				owner.WriteSdata(writer);
			}
		}

		internal delegate void LogError(string format, params object[] args);

		public ManagedExportsWriter(string moduleName, Machine machine, RelocDirectory relocDirectory, MetaData metaData, PEHeaders peHeaders, LogError logError) {
			this.moduleName = moduleName;
			this.machine = machine;
			this.relocDirectory = relocDirectory;
			this.metaData = metaData;
			this.peHeaders = peHeaders;
			this.logError = logError;
			vtableFixups = new VtableFixupsChunk(this);
			stubsChunk = new StubsChunk(this);
			sdataChunk = new SdataChunk(this);
			exportDir = new ExportDir(this);
			vtables = new List<VTableInfo>();
			allMethodInfos = new List<MethodInfo>();
			sortedOrdinalMethodInfos = new List<MethodInfo>();
			sortedNameMethodInfos = new List<MethodInfo>();
			// The error is reported later when we know that there's at least one exported method
			CpuArch.TryGetCpuArch(machine, out cpuArch);
		}

		internal void AddTextChunks(PESection textSection) {
			textSection.Add(vtableFixups, DEFAULT_VTBL_FIXUPS_ALIGNMENT);
			if (cpuArch != null)
				textSection.Add(stubsChunk, cpuArch.GetStubAlignment(stubType));
		}

		internal void AddSdataChunks(PESection sdataSection) {
			sdataSection.Add(sdataChunk, DEFAULT_SDATA_ALIGNMENT);
		}

		internal void InitializeChunkProperties() {
			if (allMethodInfos.Count == 0)
				return;
			peHeaders.ExportDirectory = exportDir;
			peHeaders.ImageCor20Header.VtableFixups = vtableFixups;
		}

		internal void AddExportedMethods(List<MethodDef> methods, uint timestamp) {
			if (methods.Count == 0)
				return;

			// Only check for an unsupported machine when we know there's at least one exported method
			if (cpuArch == null) {
				logError("The module has exported methods but the CPU architecture isn't supported: {0} (0x{1:X4})", machine, (ushort)machine);
				return;
			}
			if (methods.Count > 0x10000) {
				logError("Too many methods have been exported. No more than 2^16 methods can be exported. Number of exported methods: {0}", methods.Count);
				return;
			}

			Initialize(methods, timestamp);
		}

		sealed class MethodInfo {
			public readonly MethodDef Method;
			public readonly uint StubChunkOffset;
			public int FunctionIndex;
			public uint ManagedVtblOffset;
			public uint NameOffset;
			public int NameIndex;
			public byte[] NameBytes;
			public MethodInfo(MethodDef method, uint stubChunkOffset) {
				Method = method;
				StubChunkOffset = stubChunkOffset;
			}
		}

		sealed class VTableInfo {
			public uint SdataChunkOffset { get; set; }
			public readonly VTableFlags Flags;
			public readonly List<MethodInfo> Methods;
			public VTableInfo(VTableFlags flags) {
				Flags = flags;
				Methods = new List<MethodInfo>();
			}
		}

		void Initialize(List<MethodDef> methods, uint timestamp) {
			var dict = new Dictionary<int, List<VTableInfo>>();
			var baseFlags = Is64Bit ? VTableFlags._64Bit : VTableFlags._32Bit;
			uint stubOffset = 0;
			uint stubAlignment = cpuArch.GetStubAlignment(stubType);
			uint stubCodeOffset = cpuArch.GetStubCodeOffset(stubType);
			uint stubSize = cpuArch.GetStubSize(stubType);
			foreach (var method in methods) {
				var exportInfo = method.ExportInfo;
				Debug.Assert(exportInfo != null);
				if (exportInfo == null)
					continue;

				var flags = baseFlags;
				if ((exportInfo.Options & MethodExportInfoOptions.FromUnmanaged) != 0)
					flags |= VTableFlags.FromUnmanaged;
				if ((exportInfo.Options & MethodExportInfoOptions.FromUnmanagedRetainAppDomain) != 0)
					flags |= VTableFlags.FromUnmanagedRetainAppDomain;
				if ((exportInfo.Options & MethodExportInfoOptions.CallMostDerived) != 0)
					flags |= VTableFlags.CallMostDerived;

				List<VTableInfo> list;
				if (!dict.TryGetValue((int)flags, out list))
					dict.Add((int)flags, list = new List<VTableInfo>());
				if (list.Count == 0 || list[list.Count - 1].Methods.Count >= ushort.MaxValue)
					list.Add(new VTableInfo(flags));
				var info = new MethodInfo(method, stubOffset + stubCodeOffset);
				allMethodInfos.Add(info);
				list[list.Count - 1].Methods.Add(info);
				stubOffset = (stubOffset + stubSize + stubAlignment - 1) & ~(stubAlignment - 1);
			}

			foreach (var kv in dict)
				vtables.AddRange(kv.Value);

			WriteSdataBlob(timestamp);

			vtableFixups.length = (uint)vtables.Count * 8;
			stubsChunk.length = stubOffset;
			sdataChunk.length = (uint)sdataBytesInfo.Data.Length;

			uint expectedOffset = 0;
			foreach (var info in allMethodInfos) {
				uint currentOffset = info.StubChunkOffset - stubCodeOffset;
				if (expectedOffset != currentOffset)
					throw new InvalidOperationException();
				cpuArch.WriteStubRelocs(stubType, relocDirectory, stubsChunk, currentOffset);
				expectedOffset = (currentOffset + stubSize + stubAlignment - 1) & ~(stubAlignment - 1);
			}
			if (expectedOffset != stubOffset)
				throw new InvalidOperationException();
		}

		struct NamesBlob {
			readonly Dictionary<string, NameInfo> nameOffsets;
			readonly List<byte[]> names;
			readonly List<uint> methodNameOffsets;
			uint currentOffset;
			int methodNamesCount;
			bool methodNamesIsFrozen;

			public int MethodNamesCount {
				get { return methodNamesCount; }
			}

			struct NameInfo {
				public readonly uint Offset;
				public readonly byte[] Bytes;
				public NameInfo(uint offset, byte[] bytes) {
					Offset = offset;
					Bytes = bytes;
				}
			}

			public NamesBlob(bool dummy) {
				nameOffsets = new Dictionary<string, NameInfo>(StringComparer.Ordinal);
				names = new List<byte[]>();
				methodNameOffsets = new List<uint>();
				currentOffset = 0;
				methodNamesCount = 0;
				methodNamesIsFrozen = false;
			}

			public uint GetMethodNameOffset(string name, out byte[] bytes) {
				if (methodNamesIsFrozen)
					throw new InvalidOperationException();
				methodNamesCount++;
				uint offset = GetOffset(name, out bytes);
				methodNameOffsets.Add(offset);
				return offset;
			}

			public uint GetOtherNameOffset(string name) {
				methodNamesIsFrozen = true;
				byte[] bytes;
				return GetOffset(name, out bytes);
			}

			uint GetOffset(string name, out byte[] bytes) {
				NameInfo nameInfo;
				if (nameOffsets.TryGetValue(name, out nameInfo)) {
					bytes = nameInfo.Bytes;
					return nameInfo.Offset;
				}
				bytes = GetNameASCIIZ(name);
				names.Add(bytes);
				uint offset = currentOffset;
				nameOffsets.Add(name, new NameInfo(offset, bytes));
				currentOffset += (uint)bytes.Length;
				return offset;
			}

			// If this method gets updated, also update the reader (MethodExportInfoProvider)
			static byte[] GetNameASCIIZ(string name) {
				Debug.Assert(name != null);
				int size = Encoding.UTF8.GetByteCount(name);
				var bytes = new byte[size + 1];
				Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
				if (bytes[bytes.Length - 1] != 0)
					throw new ModuleWriterException();
				return bytes;
			}

			public void Write(BinaryWriter writer) {
				foreach (var name in names)
					writer.Write(name);
			}

			public uint[] GetMethodNameOffsets() {
				return methodNameOffsets.ToArray();
			}
		}

		struct SdataBytesInfo {
			public byte[] Data;
			public uint namesBlobStreamOffset;
			public uint moduleNameOffset;
			public uint exportDirModuleNameStreamOffset;
			public uint exportDirAddressOfFunctionsStreamOffset;
			public uint addressOfFunctionsStreamOffset;
			public uint addressOfNamesStreamOffset;
			public uint addressOfNameOrdinalsStreamOffset;
			public uint[] MethodNameOffsets;
		}
		SdataBytesInfo sdataBytesInfo;

		/// <summary>
		/// Writes the .sdata blob. We could write the data in any order, but we write the data in the same order as ILASM
		/// </summary>
		/// <param name="timestamp">PE timestamp</param>
		void WriteSdataBlob(uint timestamp) {
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);

			// Write all vtables (referenced from the .text section)
			Debug.Assert((writer.BaseStream.Position & 7) == 0);
			foreach (var vtbl in vtables) {
				vtbl.SdataChunkOffset = (uint)writer.BaseStream.Position;
				foreach (var info in vtbl.Methods) {
					info.ManagedVtblOffset = (uint)writer.BaseStream.Position;
					writer.Write(0x06000000 + metaData.GetRid(info.Method));
					if ((vtbl.Flags & VTableFlags._64Bit) != 0)
						writer.Write(0U);
				}
			}

			var namesBlob = new NamesBlob(1 == 2);
			int nameIndex = 0;
			bool error = false;
			foreach (var info in allMethodInfos) {
				var exportInfo = info.Method.ExportInfo;
				var name = exportInfo.Name;
				if (name == null) {
					if (exportInfo.Ordinal != null) {
						sortedOrdinalMethodInfos.Add(info);
						continue;
					}
					name = info.Method.Name;
				}
				if (string.IsNullOrEmpty(name)) {
					error = true;
					logError("Exported method name is null or empty, method: {0} (0x{1:X8})", info.Method, info.Method.MDToken.Raw);
					continue;
				}
				info.NameOffset = namesBlob.GetMethodNameOffset(name, out info.NameBytes);
				info.NameIndex = nameIndex++;
				sortedNameMethodInfos.Add(info);
			}
			Debug.Assert(error || sortedOrdinalMethodInfos.Count + sortedNameMethodInfos.Count == allMethodInfos.Count);
			sdataBytesInfo.MethodNameOffsets = namesBlob.GetMethodNameOffsets();
			Debug.Assert(sortedNameMethodInfos.Count == sdataBytesInfo.MethodNameOffsets.Length);
			sdataBytesInfo.moduleNameOffset = namesBlob.GetOtherNameOffset(moduleName);

			sortedOrdinalMethodInfos.Sort((a, b) => a.Method.ExportInfo.Ordinal.Value.CompareTo(b.Method.ExportInfo.Ordinal.Value));
			sortedNameMethodInfos.Sort((a, b) => CompareTo(a.NameBytes, b.NameBytes));

			int ordinalBase, nextFreeOrdinal;
			if (sortedOrdinalMethodInfos.Count == 0) {
				ordinalBase = 0;
				nextFreeOrdinal = 0;
			}
			else {
				ordinalBase = sortedOrdinalMethodInfos[0].Method.ExportInfo.Ordinal.Value;
				nextFreeOrdinal = sortedOrdinalMethodInfos[sortedOrdinalMethodInfos.Count - 1].Method.ExportInfo.Ordinal.Value + 1;
			}
			int nameFuncBaseIndex = nextFreeOrdinal - ordinalBase;
			int lastFuncIndex = 0;
			for (int i = 0; i < sortedOrdinalMethodInfos.Count; i++) {
				int index = sortedOrdinalMethodInfos[i].Method.ExportInfo.Ordinal.Value - ordinalBase;
				sortedOrdinalMethodInfos[i].FunctionIndex = index;
				lastFuncIndex = index;
			}
			for (int i = 0; i < sortedNameMethodInfos.Count; i++) {
				lastFuncIndex = nameFuncBaseIndex + i;
				sortedNameMethodInfos[i].FunctionIndex = lastFuncIndex;
			}
			int funcSize = lastFuncIndex + 1;
			if (funcSize > 0x10000) {
				logError("Exported function array is too big");
				return;
			}

			// Write IMAGE_EXPORT_DIRECTORY
			Debug.Assert((writer.BaseStream.Position & 3) == 0);
			exportDirOffset = (uint)writer.BaseStream.Position;
			writer.Write(0U); // Characteristics
			writer.Write(timestamp);
			writer.Write(0U); // MajorVersion, MinorVersion
			sdataBytesInfo.exportDirModuleNameStreamOffset = (uint)writer.BaseStream.Position;
			writer.Write(0U); // Name
			writer.Write(ordinalBase); // Base
			writer.Write((uint)funcSize); // NumberOfFunctions
			writer.Write(sdataBytesInfo.MethodNameOffsets.Length); // NumberOfNames
			sdataBytesInfo.exportDirAddressOfFunctionsStreamOffset = (uint)writer.BaseStream.Position;
			writer.Write(0U); // AddressOfFunctions
			writer.Write(0U); // AddressOfNames
			writer.Write(0U); // AddressOfNameOrdinals

			sdataBytesInfo.addressOfFunctionsStreamOffset = (uint)writer.BaseStream.Position;
			WriteZeroes(writer, funcSize * 4);
			sdataBytesInfo.addressOfNamesStreamOffset = (uint)writer.BaseStream.Position;
			WriteZeroes(writer, sdataBytesInfo.MethodNameOffsets.Length * 4);
			sdataBytesInfo.addressOfNameOrdinalsStreamOffset = (uint)writer.BaseStream.Position;
			WriteZeroes(writer, sdataBytesInfo.MethodNameOffsets.Length * 2);
			sdataBytesInfo.namesBlobStreamOffset = (uint)writer.BaseStream.Position;
			namesBlob.Write(writer);

			sdataBytesInfo.Data = stream.ToArray();
		}

		void WriteSdata(BinaryWriter writer) {
			if (sdataBytesInfo.Data == null)
				return;
			PatchSdataBytesBlob();
			writer.Write(sdataBytesInfo.Data);
		}

		void PatchSdataBytesBlob() {
			uint rva = (uint)sdataChunk.RVA;
			uint namesBaseOffset = rva + sdataBytesInfo.namesBlobStreamOffset;

			var writer = new BinaryWriter(new MemoryStream(sdataBytesInfo.Data));

			writer.BaseStream.Position = sdataBytesInfo.exportDirModuleNameStreamOffset;
			writer.Write(namesBaseOffset + sdataBytesInfo.moduleNameOffset);

			writer.BaseStream.Position = sdataBytesInfo.exportDirAddressOfFunctionsStreamOffset;
			writer.Write(rva + sdataBytesInfo.addressOfFunctionsStreamOffset); // AddressOfFunctions
			if (sdataBytesInfo.MethodNameOffsets.Length != 0) {
				writer.Write(rva + sdataBytesInfo.addressOfNamesStreamOffset); // AddressOfNames
				writer.Write(rva + sdataBytesInfo.addressOfNameOrdinalsStreamOffset); // AddressOfNameOrdinals
			}

			uint funcBaseRva = (uint)stubsChunk.RVA;
			writer.BaseStream.Position = sdataBytesInfo.addressOfFunctionsStreamOffset;
			int currentFuncIndex = 0;
			foreach (var info in sortedOrdinalMethodInfos) {
				int zeroes = info.FunctionIndex - currentFuncIndex;
				if (zeroes < 0)
					throw new InvalidOperationException();
				while (zeroes-- > 0)
					writer.Write(0);
				writer.Write(funcBaseRva + info.StubChunkOffset);
				currentFuncIndex = info.FunctionIndex + 1;
			}
			foreach (var info in sortedNameMethodInfos) {
				if (info.FunctionIndex != currentFuncIndex++)
					throw new InvalidOperationException();
				writer.Write(funcBaseRva + info.StubChunkOffset);
			}

			var nameOffsets = sdataBytesInfo.MethodNameOffsets;
			if (nameOffsets.Length != 0) {
				writer.BaseStream.Position = sdataBytesInfo.addressOfNamesStreamOffset;
				foreach (var info in sortedNameMethodInfos)
					writer.Write(namesBaseOffset + nameOffsets[info.NameIndex]);

				writer.BaseStream.Position = sdataBytesInfo.addressOfNameOrdinalsStreamOffset;
				foreach (var info in sortedNameMethodInfos)
					writer.Write((ushort)info.FunctionIndex);
			}
		}

		static void WriteZeroes(BinaryWriter writer, int count) {
			while (count >= 8) {
				writer.Write(0UL);
				count -= 8;
			}
			for (int i = 0; i < count; i++)
				writer.Write((byte)0);
		}

		void WriteVtableFixups(BinaryWriter writer) {
			if (vtables.Count == 0)
				return;

			foreach (var vtbl in vtables) {
				Debug.Assert(vtbl.Methods.Count <= ushort.MaxValue);
				writer.Write((uint)sdataChunk.RVA + vtbl.SdataChunkOffset);
				writer.Write((ushort)vtbl.Methods.Count);
				writer.Write((ushort)vtbl.Flags);
			}
		}

		void WriteStubs(BinaryWriter writer) {
			if (vtables.Count == 0)
				return;
			if (cpuArch == null)
				return;

			ulong imageBase = peHeaders.ImageBase;
			uint stubsBaseRva = (uint)stubsChunk.RVA;
			uint vtblBaseRva = (uint)sdataChunk.RVA;
			uint expectedOffset = 0;
			uint stubCodeOffset = cpuArch.GetStubCodeOffset(stubType);
			uint stubSize = cpuArch.GetStubSize(stubType);
			uint stubAlignment = cpuArch.GetStubAlignment(stubType);
			int zeroes = (int)((stubSize + stubAlignment - 1 & ~(stubAlignment - 1)) - stubSize);
			foreach (var info in allMethodInfos) {
				uint currentOffset = info.StubChunkOffset - stubCodeOffset;
				if (expectedOffset != currentOffset)
					throw new InvalidOperationException();
				var pos = writer.BaseStream.Position;
				cpuArch.WriteStub(stubType, writer, imageBase, stubsBaseRva + currentOffset, vtblBaseRva + info.ManagedVtblOffset);
				Debug.Assert(pos + stubSize == writer.BaseStream.Position, "The full stub wasn't written");
				if (pos + stubSize != writer.BaseStream.Position)
					throw new InvalidOperationException();
				if (zeroes != 0)
					WriteZeroes(writer, zeroes);
				expectedOffset = (currentOffset + stubSize + stubAlignment - 1) & ~(stubAlignment - 1);
			}
			if (expectedOffset != stubsChunk.length)
				throw new InvalidOperationException();
		}

		static int CompareTo(byte[] a, byte[] b) {
			if (a == b)
				return 0;
			int max = Math.Min(a.Length, b.Length);
			for (int i = 0; i < max; i++) {
				int c = a[i] - b[i];
				if (c != 0)
					return c;
			}
			return a.Length - b.Length;
		}
	}
}
