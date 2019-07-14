// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiScope : SymbolScope {
		readonly SymbolMethod method;
		readonly SymbolScope parent;
		internal int startOffset;
		internal int endOffset;
		readonly List<SymbolScope> childrenList;
		readonly List<SymbolVariable> localsList;
		readonly List<SymbolNamespace> namespacesList;

		public override SymbolMethod Method => method;
		public override SymbolScope Parent => parent;
		public override int StartOffset => startOffset;
		public override int EndOffset => endOffset;
		public override IList<SymbolScope> Children => childrenList;
		public override IList<SymbolVariable> Locals => localsList;
		public override IList<SymbolNamespace> Namespaces => namespacesList;
		public override IList<PdbCustomDebugInfo> CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();
		public override PdbImportScope ImportScope => null;

		public DbiScope(SymbolMethod method, SymbolScope parent, string name, uint offset, uint length) {
			this.method = method;
			this.parent = parent;
			Name = name;
			startOffset = (int)offset;
			endOffset = (int)(offset + length);

			childrenList = new List<SymbolScope>();
			localsList = new List<SymbolVariable>();
			namespacesList = new List<SymbolNamespace>();
		}

		public string Name { get; private set; }

		List<OemInfo> oemInfos;
		List<ConstantInfo> constants;

		readonly struct ConstantInfo {
			public readonly string Name;
			public readonly uint SignatureToken;
			public readonly object Value;
			public ConstantInfo(string name, uint signatureToken, object value) {
				Name = name;
				SignatureToken = signatureToken;
				Value = value;
			}
		}

		internal readonly struct OemInfo {
			public readonly string Name;
			public readonly byte[] Data;
			public OemInfo(string name, byte[] data) {
				Name = name;
				Data = data;
			}
			public override string ToString() => Name + " = (" + Data.Length.ToString() + " bytes)";
		}

		static readonly byte[] dotNetOemGuid = new byte[] {
			0xC9, 0x3F, 0xEA, 0xC6, 0xB3, 0x59, 0xD6, 0x49, 0xBC, 0x25, 0x09, 0x02, 0xBB, 0xAB, 0xB4, 0x60
		};

		public void Read(RecursionCounter counter, ref DataReader reader, uint scopeEnd) {
			if (!counter.Increment())
				throw new PdbException("Scopes too deep");

			while (reader.Position < scopeEnd) {
				var size = reader.ReadUInt16();
				var begin = reader.Position;
				var end = begin + size;

				var type = (SymbolType)reader.ReadUInt16();
				DbiScope child = null;
				uint? childEnd = null;
				string name;
				switch (type) {
					case SymbolType.S_BLOCK32: {
						reader.Position += 4;
						childEnd = reader.ReadUInt32();
						var len = reader.ReadUInt32();
						var addr = PdbAddress.ReadAddress(ref reader);
						name = PdbReader.ReadCString(ref reader);
						child = new DbiScope(method, this, name, addr.Offset, len);
						break;
					}
					case SymbolType.S_UNAMESPACE:
						namespacesList.Add(new DbiNamespace(PdbReader.ReadCString(ref reader)));
						break;
					case SymbolType.S_MANSLOT: {
						var variable = new DbiVariable();
						if (variable.Read(ref reader))
							localsList.Add(variable);
						break;
					}
					case SymbolType.S_OEM:
						if ((ulong)reader.Position + 20 > end)
							break;
						if (!ReadAndCompareBytes(ref reader, end, dotNetOemGuid)) {
							Debug.Fail("Unknown OEM record GUID, not .NET GUID");
							break;
						}
						reader.Position += 4;// typeIndex or 0
						name = ReadUnicodeString(ref reader, end);
						Debug.Assert(!(name is null));
						if (name is null)
							break;
						var data = reader.ReadBytes((int)(end - reader.Position));
						if (oemInfos is null)
							oemInfos = new List<OemInfo>(1);
						oemInfos.Add(new OemInfo(name, data));	
						break;
					case SymbolType.S_MANCONSTANT:
						uint signatureToken = reader.ReadUInt32();
						object value;
						if (!NumericReader.TryReadNumeric(ref reader, end, out value))
							break;
						name = PdbReader.ReadCString(ref reader);
						if (constants is null)
							constants = new List<ConstantInfo>();
						constants.Add(new ConstantInfo(name, signatureToken, value));
						break;
					case SymbolType.S_END:
						break;
					default:
						break;
				}

				reader.Position = end;
				if (!(child is null)) {
					child.Read(counter, ref reader, childEnd.Value);
					childrenList.Add(child);
					child = null;
				}
			}
			counter.Decrement();
			if (reader.Position != scopeEnd)
				Debugger.Break();
		}

		static string ReadUnicodeString(ref DataReader reader, uint end) {
			var sb = new StringBuilder();
			for (;;) {
				if ((ulong)reader.Position + 2 > end)
					return null;
				var c = reader.ReadChar();
				if (c == 0)
					break;
				sb.Append(c);
			}
			return sb.ToString();
		}

		static bool ReadAndCompareBytes(ref DataReader reader, uint end, byte[] bytes) {
			if ((ulong)reader.Position + (uint)bytes.Length > end)
				return false;
			for (int i = 0; i < bytes.Length; i++) {
				if (reader.ReadByte() != bytes[i])
					return false;
			}
			return true;
		}

		public override IList<PdbConstant> GetConstants(ModuleDef module, GenericParamContext gpContext) {
			if (constants is null)
				return Array2.Empty<PdbConstant>();
			var res = new PdbConstant[constants.Count];
			for (int i = 0; i < res.Length; i++) {
				var info = constants[i];
				TypeSig signature;
				var saSig = module.ResolveToken(info.SignatureToken, gpContext) as StandAloneSig;
				var fieldSig = saSig is null ? null : saSig.Signature as FieldSig;
				if (fieldSig is null) {
					Debug.Fail("Constant without a signature");
					signature = null;
				}
				else
					signature = fieldSig.Type;
				res[i] = new PdbConstant(info.Name, signature, info.Value);
			}
			return res;
		}

		internal byte[] GetSymAttribute(string name) {
			if (oemInfos is null)
				return null;
			foreach (var info in oemInfos) {
				if (info.Name == name)
					return info.Data;
			}
			return null;
		}
	}
}
