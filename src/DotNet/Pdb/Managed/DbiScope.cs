// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiScope : ISymbolScope2 {
		public DbiScope(string name, uint offset, uint length) {
			Name = name;
			BeginOffset = offset;
			EndOffset = offset + length;

			Namespaces = new List<DbiNamespace>();
			Variables = new List<DbiVariable>();
			Children = new List<DbiScope>();
		}

		public string Name { get; private set; }
		public uint BeginOffset { get; internal set; }
		public uint EndOffset { get; internal set; }

		public IList<DbiNamespace> Namespaces { get; private set; }
		public IList<DbiVariable> Variables { get; private set; }
		public IList<DbiScope> Children { get; private set; }

		List<OemInfo> oemInfos;
		List<ConstantInfo> constants;

		struct ConstantInfo {
			public string Name;
			public uint SignatureToken;
			public object Value;
			public ConstantInfo(string name, uint signatureToken, object value) {
				Name = name;
				SignatureToken = signatureToken;
				Value = value;
			}
		}

		internal struct OemInfo {
			public readonly string Name;
			public readonly byte[] Data;
			public OemInfo(string name, byte[] data) {
				Name = name;
				Data = data;
			}
			public override string ToString() {
				return Name + " = (" + Data.Length.ToString() + " bytes)";
			}
		}

		static readonly byte[] dotNetOemGuid = new byte[] {
			0xC9, 0x3F, 0xEA, 0xC6, 0xB3, 0x59, 0xD6, 0x49, 0xBC, 0x25, 0x09, 0x02, 0xBB, 0xAB, 0xB4, 0x60
		};

		public void Read(RecursionCounter counter, IImageStream stream, uint scopeEnd) {
			if (!counter.Increment())
				throw new PdbException("Scopes too deep");

			while (stream.Position < scopeEnd) {
				var size = stream.ReadUInt16();
				var begin = stream.Position;
				var end = begin + size;

				var type = (SymbolType)stream.ReadUInt16();
				DbiScope child = null;
				uint? childEnd = null;
				string name;
				switch (type) {
					case SymbolType.S_BLOCK32: {
						stream.Position += 4;
						childEnd = stream.ReadUInt32();
						var len = stream.ReadUInt32();
						var addr = PdbAddress.ReadAddress(stream);
						name = PdbReader.ReadCString(stream);
						child = new DbiScope(name, addr.Offset, len);
						break;
					}
					case SymbolType.S_UNAMESPACE:
						Namespaces.Add(new DbiNamespace(PdbReader.ReadCString(stream)));
						break;
					case SymbolType.S_MANSLOT: {
						var variable = new DbiVariable();
						variable.Read(stream);
						Variables.Add(variable);
						break;
					}
					case SymbolType.S_OEM:
						if (stream.Position + 20 > end)
							break;
						if (!ReadAndCompareBytes(stream, end, dotNetOemGuid)) {
							Debug.Fail("Unknown OEM record GUID, not .NET GUID");
							break;
						}
						stream.Position += 4;// typeIndex or 0
						name = ReadUnicodeString(stream, end);
						Debug.Assert(name != null);
						if (name == null)
							break;
						var data = stream.ReadBytes((int)(end - stream.Position));
						if (oemInfos == null)
							oemInfos = new List<OemInfo>(1);
						oemInfos.Add(new OemInfo(name, data));	
						break;
					case SymbolType.S_MANCONSTANT:
						uint signatureToken = stream.ReadUInt32();
						object value;
						if (!NumericReader.TryReadNumeric(stream, end, out value))
							break;
						name = PdbReader.ReadCString(stream);
						if (constants == null)
							constants = new List<ConstantInfo>();
						constants.Add(new ConstantInfo(name, signatureToken, value));
						break;
					case SymbolType.S_END:
						break;
					default:
						break;
				}

				stream.Position = end;
				if (child != null) {
					child.Read(counter, stream, childEnd.Value);
					Children.Add(child);
					child = null;
				}
			}
			counter.Decrement();
			if (stream.Position != scopeEnd)
				Debugger.Break();
		}

		static string ReadUnicodeString(IImageStream stream, long end) {
			var sb = new StringBuilder();
			for (;;) {
				if (stream.Position + 2 > end)
					return null;
				var c = (char)stream.ReadUInt16();
				if (c == 0)
					break;
				sb.Append(c);
			}
			return sb.ToString();
		}

		static bool ReadAndCompareBytes(IImageStream stream, long end, byte[] bytes) {
			if (stream.Position + bytes.Length > end)
				return false;
			for (int i = 0; i < bytes.Length; i++) {
				if (stream.ReadByte() != bytes[i])
					return false;
			}
			return true;
		}

		public PdbConstant[] GetConstants(ModuleDefMD module, GenericParamContext gpContext) {
			if (constants == null)
				return emptySymbolConstants;
			var res = new PdbConstant[constants.Count];
			for (int i = 0; i < res.Length; i++) {
				var info = constants[i];
				TypeSig signature;
				var saSig = module.ResolveToken(info.SignatureToken, gpContext) as StandAloneSig;
				var fieldSig = saSig == null ? null : saSig.Signature as FieldSig;
				if (fieldSig == null) {
					Debug.Fail("Constant without a signature");
					signature = null;
				}
				else
					signature = fieldSig.Type;
				res[i] = new PdbConstant(info.Name, signature, info.Value);
			}
			return res;
		}
		static readonly PdbConstant[] emptySymbolConstants = new PdbConstant[0];

		internal byte[] GetSymAttribute(string name) {
			if (oemInfos == null)
				return null;
			foreach (var info in oemInfos) {
				if (info.Name == name)
					return info.Data;
			}
			return null;
		}

		#region ISymbolScope

		int ISymbolScope.StartOffset {
			get { return (int)BeginOffset; }
		}

		int ISymbolScope.EndOffset {
			get { return (int)EndOffset; }
		}

		ISymbolScope[] ISymbolScope.GetChildren() {
			var scopes = new ISymbolScope[Children.Count];
			for (int i = 0; i < scopes.Length; i++)
				scopes[i] = Children[i];
			return scopes;
		}

		ISymbolVariable[] ISymbolScope.GetLocals() {
			var vars = new ISymbolVariable[Variables.Count];
			for (int i = 0; i < vars.Length; i++)
				vars[i] = Variables[i];
			return vars;
		}

		ISymbolNamespace[] ISymbolScope.GetNamespaces() {
			var nss = new ISymbolNamespace[Namespaces.Count];
			for (int i = 0; i < nss.Length; i++)
				nss[i] = Namespaces[i];
			return nss;
		}

		ISymbolMethod ISymbolScope.Method {
			get { throw new NotImplementedException(); }
		}

		ISymbolScope ISymbolScope.Parent {
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}