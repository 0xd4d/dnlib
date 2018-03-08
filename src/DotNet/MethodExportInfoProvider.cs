// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.PE;
using dnlib.IO;
using System.Diagnostics;
using System.Text;

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
			var vtblHdr = module.MetaData.ImageCor20Header.VTableFixups;
			if (vtblHdr.VirtualAddress == 0 || vtblHdr.Size == 0)
				return;

			var peImage = module.MetaData.PEImage;
			var exportHdr = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[0];
			if (exportHdr.VirtualAddress == 0 || exportHdr.Size < 0x28)
				return;

			CpuArch cpuArch;
			if (!CpuArch.TryGetCpuArch(peImage.ImageNTHeaders.FileHeader.Machine, out cpuArch)) {
				Debug.Fail(string.Format("Exported methods: Unsupported machine: {0}", peImage.ImageNTHeaders.FileHeader.Machine));
				return;
			}

			using (var reader = peImage.CreateFullStream()) {
				var offsetToInfo = GetOffsetToExportInfoDictionary(reader, peImage, exportHdr, cpuArch);
				reader.Position = (long)peImage.ToFileOffset(vtblHdr.VirtualAddress);
				long endPos = reader.Position + vtblHdr.Size;
				while (reader.Position + 8 <= endPos && reader.CanRead(8)) {
					var tableRva = (RVA)reader.ReadUInt32();
					int numSlots = reader.ReadUInt16();
					var flags = (VTableFlags)reader.ReadUInt16();
					bool is64bit = (flags & VTableFlags._64Bit) != 0;
					var exportOptions = ToMethodExportInfoOptions(flags);

					var pos = reader.Position;
					reader.Position = (long)peImage.ToFileOffset(tableRva);
					int slotSize = is64bit ? 8 : 4;
					while (numSlots-- > 0 && reader.CanRead(slotSize)) {
						var tokenPos = reader.Position;
						MethodExportInfo exportInfo;
						uint token = reader.ReadUInt32();
						bool b = offsetToInfo.TryGetValue(tokenPos, out exportInfo);
						Debug.Assert(token == 0 || b);
						if (b) {
							exportInfo = new MethodExportInfo(exportInfo.Name, exportInfo.Ordinal, exportOptions);
							toInfo[token] = exportInfo;
						}
						if (slotSize == 8)
							reader.ReadUInt32();
					}
					reader.Position = pos;
				}
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

		static Dictionary<long, MethodExportInfo> GetOffsetToExportInfoDictionary(IImageStream reader, IPEImage peImage, ImageDataDirectory exportHdr, CpuArch cpuArch) {
			reader.Position = (long)peImage.ToFileOffset(exportHdr.VirtualAddress);
			// Skip Characteristics(4), TimeDateStamp(4), MajorVersion(2), MinorVersion(2), Name(4)
			reader.Position += 16;
			uint ordinalBase = reader.ReadUInt32();
			int numFuncs = reader.ReadInt32();
			int numNames = reader.ReadInt32();
			long offsetOfFuncs = (long)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			long offsetOfNames = (long)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			long offsetOfNameIndexes = (long)peImage.ToFileOffset((RVA)reader.ReadUInt32());

			var names = ReadNames(reader, peImage, numNames, offsetOfNames, offsetOfNameIndexes);
			reader.Position = offsetOfFuncs;
			var allInfos = new MethodExportInfo[numFuncs];
			var dict = new Dictionary<long, MethodExportInfo>(numFuncs);
			for (int i = 0; i < allInfos.Length; i++) {
				var currOffset = reader.Position;
				var nextOffset = reader.Position + 4;
				uint funcRva = 0;
				var rva = (RVA)reader.ReadUInt32();
				reader.Position = (long)peImage.ToFileOffset(rva);
				bool rvaValid = rva != 0 && cpuArch.TryGetExportedRvaFromStub(reader, peImage, out funcRva);
				long funcOffset = rvaValid ? (long)peImage.ToFileOffset((RVA)funcRva) : 0;
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

		static NameAndIndex[] ReadNames(IImageStream reader, IPEImage peImage, int numNames, long offsetOfNames, long offsetOfNameIndexes) {
			var names = new NameAndIndex[numNames];

			reader.Position = offsetOfNameIndexes;
			for (int i = 0; i < names.Length; i++)
				names[i].Index = reader.ReadUInt16();

			var currentOffset = offsetOfNames;
			for (int i = 0; i < names.Length; i++, currentOffset += 4) {
				reader.Position = currentOffset;
				long offsetOfName = (long)peImage.ToFileOffset((RVA)reader.ReadUInt32());
				names[i].Name = ReadMethodNameASCIIZ(reader, offsetOfName);
			}

			return names;
		}

		struct NameAndIndex {
			public string Name;
			public int Index;
		}

		// If this method gets updated, also update the writer (ManagedExportsWriter)
		static string ReadMethodNameASCIIZ(IImageStream reader, long offset) {
			reader.Position = offset;
			var stringData = reader.ReadBytesUntilByte(0);
			return Encoding.UTF8.GetString(stringData);
		}

		public MethodExportInfo GetMethodExportInfo(uint token) {
			if (toInfo.Count == 0)
				return null;
			MethodExportInfo info;
			if (toInfo.TryGetValue(token, out info))
				return new MethodExportInfo(info.Name, info.Ordinal, info.Options);
			return null;
		}
	}
}
