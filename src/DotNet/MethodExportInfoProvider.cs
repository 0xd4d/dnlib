// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.PE;
using dnlib.IO;
using System.Diagnostics;

namespace dnlib.DotNet {
	sealed class MethodExportInfoProvider {
		readonly Dictionary<uint, MethodExportInfo> toInfo;

		public MethodExportInfoProvider(ModuleDefMD module) {
			toInfo = new Dictionary<uint, MethodExportInfo>();
			try {
				Initialize(module);
			}
			catch (OutOfMemoryException) {
			}
			catch (IOException) {
			}
		}

		void Initialize(ModuleDefMD module) {
			var vtblHdr = module.Metadata.ImageCor20Header.VTableFixups;
			if (vtblHdr.VirtualAddress == 0 || vtblHdr.Size == 0)
				return;

			var peImage = module.Metadata.PEImage;
			var exportHdr = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[0];
			if (exportHdr.VirtualAddress == 0 || exportHdr.Size < 0x28)
				return;

			if (!CpuArch.TryGetCpuArch(peImage.ImageNTHeaders.FileHeader.Machine, out var cpuArch)) {
				Debug.Fail($"Exported methods: Unsupported machine: {peImage.ImageNTHeaders.FileHeader.Machine}");
				return;
			}

			var reader = peImage.CreateReader();
			var offsetToInfo = GetOffsetToExportInfoDictionary(ref reader, peImage, exportHdr, cpuArch);
			reader.Position = (uint)peImage.ToFileOffset(vtblHdr.VirtualAddress);
			ulong endPos = (ulong)reader.Position + vtblHdr.Size;
			while ((ulong)reader.Position + 8 <= endPos && reader.CanRead(8U)) {
				var tableRva = (RVA)reader.ReadUInt32();
				int numSlots = reader.ReadUInt16();
				var flags = (VTableFlags)reader.ReadUInt16();
				bool is64bit = (flags & VTableFlags.Bit64) != 0;
				var exportOptions = ToMethodExportInfoOptions(flags);

				var pos = reader.Position;
				reader.Position = (uint)peImage.ToFileOffset(tableRva);
				uint slotSize = is64bit ? 8U : 4;
				while (numSlots-- > 0 && reader.CanRead(slotSize)) {
					var tokenPos = reader.Position;
					uint token = reader.ReadUInt32();
					if (offsetToInfo.TryGetValue(tokenPos, out var exportInfo))
						toInfo[token] = new MethodExportInfo(exportInfo.Name, exportInfo.Ordinal, exportOptions);
					if (slotSize == 8)
						reader.ReadUInt32();
				}
				reader.Position = pos;
			}
		}

		static MethodExportInfoOptions ToMethodExportInfoOptions(VTableFlags flags) {
			var res = MethodExportInfoOptions.None;
			if ((flags & VTableFlags.FromUnmanaged) != 0)
				res |= MethodExportInfoOptions.FromUnmanaged;
			if ((flags & VTableFlags.FromUnmanagedRetainAppDomain) != 0)
				res |= MethodExportInfoOptions.FromUnmanagedRetainAppDomain;
			if ((flags & VTableFlags.CallMostDerived) != 0)
				res |= MethodExportInfoOptions.CallMostDerived;
			return res;
		}

		static Dictionary<uint, MethodExportInfo> GetOffsetToExportInfoDictionary(ref DataReader reader, IPEImage peImage, ImageDataDirectory exportHdr, CpuArch cpuArch) {
			reader.Position = (uint)peImage.ToFileOffset(exportHdr.VirtualAddress);
			// Skip Characteristics(4), TimeDateStamp(4), MajorVersion(2), MinorVersion(2), Name(4)
			reader.Position += 16;
			uint ordinalBase = reader.ReadUInt32();
			int numFuncs = reader.ReadInt32();
			int numNames = reader.ReadInt32();
			uint offsetOfFuncs = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			uint offsetOfNames = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			uint offsetOfNameIndexes = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());

			var names = ReadNames(ref reader, peImage, numNames, offsetOfNames, offsetOfNameIndexes);
			reader.Position = offsetOfFuncs;
			var allInfos = new MethodExportInfo[numFuncs];
			var dict = new Dictionary<uint, MethodExportInfo>(numFuncs);
			for (int i = 0; i < allInfos.Length; i++) {
				var nextOffset = reader.Position + 4;
				uint funcRva = 0;
				var rva = (RVA)reader.ReadUInt32();
				reader.Position = (uint)peImage.ToFileOffset(rva);
				bool rvaValid = rva != 0 && cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out funcRva);
				uint funcOffset = rvaValid ? (uint)peImage.ToFileOffset((RVA)funcRva) : 0;
				var exportInfo = new MethodExportInfo((ushort)(ordinalBase + (uint)i));
				if (funcOffset != 0)
					dict[funcOffset] = exportInfo;
				allInfos[i] = exportInfo;
				reader.Position = nextOffset;
			}

			foreach (var info in names) {
				int index = info.Index;
				if ((uint)index >= (uint)numFuncs)
					continue;
				allInfos[index].Ordinal = null;
				allInfos[index].Name = info.Name;
			}

			return dict;
		}

		static NameAndIndex[] ReadNames(ref DataReader reader, IPEImage peImage, int numNames, uint offsetOfNames, uint offsetOfNameIndexes) {
			var names = new NameAndIndex[numNames];

			reader.Position = offsetOfNameIndexes;
			for (int i = 0; i < names.Length; i++)
				names[i].Index = reader.ReadUInt16();

			var currentOffset = offsetOfNames;
			for (int i = 0; i < names.Length; i++, currentOffset += 4) {
				reader.Position = currentOffset;
				uint offsetOfName = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
				names[i].Name = ReadMethodNameASCIIZ(ref reader, offsetOfName);
			}

			return names;
		}

		struct NameAndIndex {
			public string Name;
			public int Index;
		}

		// If this method gets updated, also update the writer (ManagedExportsWriter)
		static string ReadMethodNameASCIIZ(ref DataReader reader, uint offset) {
			reader.Position = offset;
			return reader.TryReadZeroTerminatedUtf8String() ?? string.Empty;
		}

		public MethodExportInfo GetMethodExportInfo(uint token) {
			if (toInfo.Count == 0)
				return null;
			if (toInfo.TryGetValue(token, out var info))
				return new MethodExportInfo(info.Name, info.Ordinal, info.Options);
			return null;
		}
	}
}
