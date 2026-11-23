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
		readonly Metadata metadata;
		readonly PEHeaders peHeaders;
		readonly Action<string, object[]> logError;
		readonly VtableFixupsChunk vtableFixups;
		readonly StubsChunk stubsChunk;
		readonly SdataChunk sdataChunk;
		readonly ExportDir exportDir;
		readonly List<VTableInfo> vtables;
		readonly List<MethodInfo> allMethodInfos;
		readonly List<MethodInfo> sortedOrdinalMethodInfos;
		readonly List<MethodInfo> sortedFuncMethodInfos;
		readonly List<MethodInfo> sortedNameMethodInfos;
		readonly CpuArch? cpuArch;
		uint exportDirOffset;

		bool Is64Bit => machine.Is64Bit();
		FileOffset ExportDirOffset => sdataChunk.FileOffset + exportDirOffset;
		RVA ExportDirRVA => sdataChunk.RVA + exportDirOffset;
		uint ExportDirSize => 0x28;
		internal bool HasExports => vtables.Count != 0;
		// File offset of IMAGE_EXPORT_DIRECTORY.TimeDateStamp. It's only valid if there are exports.
		internal FileOffset ExportDirTimeDateStampOffset => ExportDirOffset + 4;

		sealed class ExportDir : IChunk {
			readonly ManagedExportsWriter owner;
			public FileOffset FileOffset => owner.ExportDirOffset;
			public RVA RVA => owner.ExportDirRVA;
			public ExportDir(ManagedExportsWriter owner) => this.owner = owner;
			void IChunk.SetOffset(FileOffset offset, RVA rva) => throw new NotSupportedException();
			public uint GetFileLength() => owner.ExportDirSize;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			void IChunk.WriteTo(DataWriter writer) => throw new NotSupportedException();
		}

		sealed class VtableFixupsChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;
			public FileOffset FileOffset => offset;
			public RVA RVA => rva;
			public VtableFixupsChunk(ManagedExportsWriter owner) => this.owner = owner;
			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}
			public uint GetFileLength() => length;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			public void WriteTo(DataWriter writer) => owner.WriteVtableFixups(writer);
		}

		sealed class StubsChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;
			public FileOffset FileOffset => offset;
			public RVA RVA => rva;
			public StubsChunk(ManagedExportsWriter owner) => this.owner = owner;
			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}
			public uint GetFileLength() => length;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			public void WriteTo(DataWriter writer) => owner.WriteStubs(writer);
		}

		sealed class SdataChunk : IChunk {
			readonly ManagedExportsWriter owner;
			FileOffset offset;
			RVA rva;
			internal uint length;
			public FileOffset FileOffset => offset;
			public RVA RVA => rva;
			public SdataChunk(ManagedExportsWriter owner) => this.owner = owner;
			public void SetOffset(FileOffset offset, RVA rva) {
				this.offset = offset;
				this.rva = rva;
			}
			public uint GetFileLength() => length;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			public void WriteTo(DataWriter writer) => owner.WriteSdata(writer);
		}

		public ManagedExportsWriter(string moduleName, Machine machine, RelocDirectory relocDirectory, Metadata metadata, PEHeaders peHeaders, Action<string, object[]> logError) {
			this.moduleName = moduleName;
			this.machine = machine;
			this.relocDirectory = relocDirectory;
			this.metadata = metadata;
			this.peHeaders = peHeaders;
			this.logError = logError;
			vtableFixups = new VtableFixupsChunk(this);
			stubsChunk = new StubsChunk(this);
			sdataChunk = new SdataChunk(this);
			exportDir = new ExportDir(this);
			vtables = new List<VTableInfo>();
			allMethodInfos = new List<MethodInfo>();
			sortedOrdinalMethodInfos = new List<MethodInfo>();
			sortedFuncMethodInfos = new List<MethodInfo>();
			sortedNameMethodInfos = new List<MethodInfo>();
			// The error is reported later when we know that there's at least one exported method
			CpuArch.TryGetCpuArch(machine, out cpuArch);
		}

		internal void AddTextChunks(PESection textSection) {
			textSection.Add(vtableFixups, DEFAULT_VTBL_FIXUPS_ALIGNMENT);
			if (cpuArch is not null)
				textSection.Add(stubsChunk, cpuArch.GetStubAlignment(stubType));
		}

		internal void AddSdataChunks(PESection sdataSection) => sdataSection.Add(sdataChunk, DEFAULT_SDATA_ALIGNMENT);

		internal void InitializeChunkProperties() {
			if (allMethodInfos.Count == 0)
				return;
			peHeaders.ExportDirectory = exportDir;
			peHeaders.ImageCor20Header!.VtableFixups = vtableFixups;
		}

		internal void AddExportedMethods(List<MethodDef> methods, uint timestamp) {
			if (methods.Count == 0)
				return;

			// Only check for an unsupported machine when we know there's at least one exported method
			if (cpuArch is null) {
				logError("The module has exported methods but the CPU architecture isn't supported: {0} (0x{1:X4})", new object[] { machine, (ushort)machine });
				return;
			}
			if (methods.Count > 0x10000) {
				logError("Too many methods have been exported. No more than 2^16 methods can be exported. Number of exported methods: {0}", new object[] { methods.Count });
				return;
			}

			Initialize(methods, timestamp);
		}

		sealed class MethodInfo {
			public readonly MethodDef Method;
			public readonly uint StubChunkOffset;
			public int FunctionIndex;
			public ushort Ordinal;
			public uint ManagedVtblOffset;
			public uint NameOffset;
			public int NameIndex;
			public byte[] NameBytes = null!;
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
			var baseFlags = Is64Bit ? VTableFlags.Bit64 : VTableFlags.Bit32;
			uint stubOffset = 0;
			uint stubAlignment = cpuArch!.GetStubAlignment(stubType);
			uint stubCodeOffset = cpuArch.GetStubCodeOffset(stubType);
			uint stubSize = cpuArch.GetStubSize(stubType);
			foreach (var method in methods) {
				var exportInfo = method.ExportInfo;
				Debug2.Assert(exportInfo is not null);
				if (exportInfo is null)
					continue;

				var flags = baseFlags;
				if ((exportInfo.Options & MethodExportInfoOptions.FromUnmanaged) != 0)
					flags |= VTableFlags.FromUnmanaged;
				if ((exportInfo.Options & MethodExportInfoOptions.FromUnmanagedRetainAppDomain) != 0)
					flags |= VTableFlags.FromUnmanagedRetainAppDomain;
				if ((exportInfo.Options & MethodExportInfoOptions.CallMostDerived) != 0)
					flags |= VTableFlags.CallMostDerived;

				if (!dict.TryGetValue((int)flags, out var list))
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

			if (!WriteSdataBlob(timestamp)) {
				// Don't export anything. Nothing has been written yet and the chunk lengths are still 0.
				vtables.Clear();
				allMethodInfos.Clear();
				sortedOrdinalMethodInfos.Clear();
				sortedFuncMethodInfos.Clear();
				sortedNameMethodInfos.Clear();
				return;
			}

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

			public int MethodNamesCount => methodNamesCount;

			readonly struct NameInfo {
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
				return GetOffset(name, out var bytes);
			}

			uint GetOffset(string name, out byte[] bytes) {
				if (nameOffsets.TryGetValue(name, out var nameInfo)) {
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
				Debug2.Assert(name is not null);
				int size = Encoding.UTF8.GetByteCount(name);
				var bytes = new byte[size + 1];
				Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
				if (bytes[bytes.Length - 1] != 0)
					throw new ModuleWriterException();
				return bytes;
			}

			public void Write(DataWriter writer) {
				foreach (var name in names)
					writer.WriteBytes(name);
			}

			public uint[] GetMethodNameOffsets() => methodNameOffsets.ToArray();
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
		/// <returns><c>false</c> if an error was reported and nothing should be exported</returns>
		bool WriteSdataBlob(uint timestamp) {
			var stream = new MemoryStream();
			var writer = new DataWriter(stream);

			// Write all vtables (referenced from the .text section)
			Debug2.Assert((writer.Position & 7) == 0);
			foreach (var vtbl in vtables) {
				vtbl.SdataChunkOffset = (uint)writer.Position;
				foreach (var info in vtbl.Methods) {
					info.ManagedVtblOffset = (uint)writer.Position;
					writer.WriteUInt32(0x06000000 + metadata.GetRid(info.Method));
					if ((vtbl.Flags & VTableFlags.Bit64) != 0)
						writer.WriteUInt32(0);
				}
			}

			// Exports with an ordinal get that ordinal. Named exports without an ordinal get the ordinals after the
			// highest one, in name order. An export with both a name and an ordinal gets both.
			// If a name or ordinal is used more than once, the first method keeps it and the others aren't exported.
			var namesBlob = new NamesBlob(1 == 2);
			int nameIndex = 0;
			bool error = false;
			var usedOrdinals = new Dictionary<ushort, MethodInfo>();
			var usedNames = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
			var namedNoOrdinalMethodInfos = new List<MethodInfo>();
			foreach (var info in allMethodInfos) {
				var exportInfo = info.Method.ExportInfo!;
				var name = exportInfo.Name;
				var ordinal = exportInfo.Ordinal;
				if (name is null && ordinal is null)
					name = info.Method.Name;
				if (name is null ? ordinal is null : name.Length == 0) {
					error = true;
					logError("Exported method name is null or empty, method: {0} (0x{1:X8})", new object[] { info.Method, info.Method.MDToken.Raw });
					continue;
				}
				if (name is not null && usedNames.TryGetValue(name, out var otherNamed)) {
					error = true;
					logError("Exported method name {0} is used by more than one method: {1} (0x{2:X8}) and {3} (0x{4:X8})",
						new object[] { name, otherNamed.Method, 0x06000000 + metadata.GetRid(otherNamed.Method), info.Method, 0x06000000 + metadata.GetRid(info.Method) });
					continue;
				}
				if (ordinal is ushort ord) {
					if (usedOrdinals.TryGetValue(ord, out var other)) {
						error = true;
						logError("Exported method ordinal {0} is used by more than one method: {1} (0x{2:X8}) and {3} (0x{4:X8})",
							new object[] { ord, other.Method, 0x06000000 + metadata.GetRid(other.Method), info.Method, 0x06000000 + metadata.GetRid(info.Method) });
						continue;
					}
					usedOrdinals.Add(ord, info);
					info.Ordinal = ord;
					sortedOrdinalMethodInfos.Add(info);
				}
				else
					namedNoOrdinalMethodInfos.Add(info);
				if (name is not null) {
					usedNames.Add(name, info);
					info.NameOffset = namesBlob.GetMethodNameOffset(name, out info.NameBytes);
					info.NameIndex = nameIndex++;
					sortedNameMethodInfos.Add(info);
				}
			}
			Debug2.Assert(error || sortedOrdinalMethodInfos.Count + namedNoOrdinalMethodInfos.Count == allMethodInfos.Count);
			sdataBytesInfo.MethodNameOffsets = namesBlob.GetMethodNameOffsets();
			Debug2.Assert(sortedNameMethodInfos.Count == sdataBytesInfo.MethodNameOffsets.Length);
			sdataBytesInfo.moduleNameOffset = namesBlob.GetOtherNameOffset(moduleName);

			sortedOrdinalMethodInfos.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
			sortedNameMethodInfos.Sort((a, b) => CompareTo(a.NameBytes, b.NameBytes));
			namedNoOrdinalMethodInfos.Sort((a, b) => CompareTo(a.NameBytes, b.NameBytes));

			int ordinalBase, nextFreeOrdinal;
			if (sortedOrdinalMethodInfos.Count == 0) {
				ordinalBase = 0;
				nextFreeOrdinal = 0;
			}
			else {
				ordinalBase = sortedOrdinalMethodInfos[0].Ordinal;
				nextFreeOrdinal = sortedOrdinalMethodInfos[sortedOrdinalMethodInfos.Count - 1].Ordinal + 1;
			}
			int nameFuncBaseIndex = nextFreeOrdinal - ordinalBase;
			int lastFuncIndex = -1;
			foreach (var info in sortedOrdinalMethodInfos) {
				lastFuncIndex = info.Ordinal - ordinalBase;
				info.FunctionIndex = lastFuncIndex;
			}
			for (int i = 0; i < namedNoOrdinalMethodInfos.Count; i++) {
				lastFuncIndex = nameFuncBaseIndex + i;
				namedNoOrdinalMethodInfos[i].FunctionIndex = lastFuncIndex;
			}
			// Sorted by function index
			sortedFuncMethodInfos.AddRange(sortedOrdinalMethodInfos);
			sortedFuncMethodInfos.AddRange(namedNoOrdinalMethodInfos);
			int funcSize = lastFuncIndex + 1;
			if (funcSize > 0x10000) {
				logError("Exported function array is too big", Array2.Empty<object>());
				return false;
			}

			// Write IMAGE_EXPORT_DIRECTORY
			Debug2.Assert((writer.Position & 3) == 0);
			exportDirOffset = (uint)writer.Position;
			writer.WriteUInt32(0); // Characteristics
			writer.WriteUInt32(timestamp);
			writer.WriteUInt32(0); // MajorVersion, MinorVersion
			sdataBytesInfo.exportDirModuleNameStreamOffset = (uint)writer.Position;
			writer.WriteUInt32(0); // Name
			writer.WriteInt32(ordinalBase); // Base
			writer.WriteUInt32((uint)funcSize); // NumberOfFunctions
			writer.WriteInt32(sdataBytesInfo.MethodNameOffsets.Length); // NumberOfNames
			sdataBytesInfo.exportDirAddressOfFunctionsStreamOffset = (uint)writer.Position;
			writer.WriteUInt32(0); // AddressOfFunctions
			writer.WriteUInt32(0); // AddressOfNames
			writer.WriteUInt32(0); // AddressOfNameOrdinals

			sdataBytesInfo.addressOfFunctionsStreamOffset = (uint)writer.Position;
			writer.WriteZeros(funcSize * 4);
			sdataBytesInfo.addressOfNamesStreamOffset = (uint)writer.Position;
			writer.WriteZeros(sdataBytesInfo.MethodNameOffsets.Length * 4);
			sdataBytesInfo.addressOfNameOrdinalsStreamOffset = (uint)writer.Position;
			writer.WriteZeros(sdataBytesInfo.MethodNameOffsets.Length * 2);
			sdataBytesInfo.namesBlobStreamOffset = (uint)writer.Position;
			namesBlob.Write(writer);

			sdataBytesInfo.Data = stream.ToArray();
			return true;
		}

		void WriteSdata(DataWriter writer) {
			if (sdataBytesInfo.Data is null)
				return;
			PatchSdataBytesBlob();
			writer.WriteBytes(sdataBytesInfo.Data);
		}

		void PatchSdataBytesBlob() {
			uint rva = (uint)sdataChunk.RVA;
			uint namesBaseOffset = rva + sdataBytesInfo.namesBlobStreamOffset;

			var writer = new DataWriter(new MemoryStream(sdataBytesInfo.Data));

			writer.Position = sdataBytesInfo.exportDirModuleNameStreamOffset;
			writer.WriteUInt32(namesBaseOffset + sdataBytesInfo.moduleNameOffset);

			writer.Position = sdataBytesInfo.exportDirAddressOfFunctionsStreamOffset;
			writer.WriteUInt32(rva + sdataBytesInfo.addressOfFunctionsStreamOffset); // AddressOfFunctions
			if (sdataBytesInfo.MethodNameOffsets.Length != 0) {
				writer.WriteUInt32(rva + sdataBytesInfo.addressOfNamesStreamOffset); // AddressOfNames
				writer.WriteUInt32(rva + sdataBytesInfo.addressOfNameOrdinalsStreamOffset); // AddressOfNameOrdinals
			}

			uint funcBaseRva = (uint)stubsChunk.RVA;
			writer.Position = sdataBytesInfo.addressOfFunctionsStreamOffset;
			int currentFuncIndex = 0;
			foreach (var info in sortedFuncMethodInfos) {
				int zeros = info.FunctionIndex - currentFuncIndex;
				if (zeros < 0)
					throw new InvalidOperationException();
				while (zeros-- > 0)
					writer.WriteInt32(0);
				writer.WriteUInt32(funcBaseRva + info.StubChunkOffset);
				currentFuncIndex = info.FunctionIndex + 1;
			}

			var nameOffsets = sdataBytesInfo.MethodNameOffsets;
			if (nameOffsets.Length != 0) {
				writer.Position = sdataBytesInfo.addressOfNamesStreamOffset;
				foreach (var info in sortedNameMethodInfos)
					writer.WriteUInt32(namesBaseOffset + nameOffsets[info.NameIndex]);

				writer.Position = sdataBytesInfo.addressOfNameOrdinalsStreamOffset;
				foreach (var info in sortedNameMethodInfos)
					writer.WriteUInt16((ushort)info.FunctionIndex);
			}
		}

		void WriteVtableFixups(DataWriter writer) {
			if (vtables.Count == 0)
				return;

			foreach (var vtbl in vtables) {
				Debug2.Assert(vtbl.Methods.Count <= ushort.MaxValue);
				writer.WriteUInt32((uint)sdataChunk.RVA + vtbl.SdataChunkOffset);
				writer.WriteUInt16((ushort)vtbl.Methods.Count);
				writer.WriteUInt16((ushort)vtbl.Flags);
			}
		}

		void WriteStubs(DataWriter writer) {
			if (vtables.Count == 0)
				return;
			if (cpuArch is null)
				return;

			ulong imageBase = peHeaders.ImageBase;
			uint stubsBaseRva = (uint)stubsChunk.RVA;
			uint vtblBaseRva = (uint)sdataChunk.RVA;
			uint expectedOffset = 0;
			uint stubCodeOffset = cpuArch.GetStubCodeOffset(stubType);
			uint stubSize = cpuArch.GetStubSize(stubType);
			uint stubAlignment = cpuArch.GetStubAlignment(stubType);
			int zeros = (int)((stubSize + stubAlignment - 1 & ~(stubAlignment - 1)) - stubSize);
			foreach (var info in allMethodInfos) {
				uint currentOffset = info.StubChunkOffset - stubCodeOffset;
				if (expectedOffset != currentOffset)
					throw new InvalidOperationException();
				var pos = writer.Position;
				cpuArch.WriteStub(stubType, writer, imageBase, stubsBaseRva + currentOffset, vtblBaseRva + info.ManagedVtblOffset);
				Debug2.Assert(pos + stubSize == writer.Position, "The full stub wasn't written");
				if (pos + stubSize != writer.Position)
					throw new InvalidOperationException();
				if (zeros != 0)
					writer.WriteZeros(zeros);
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
