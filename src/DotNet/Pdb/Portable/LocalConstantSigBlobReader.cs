// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	struct LocalConstantSigBlobReader {
		readonly ModuleDef module;
		readonly IImageStream reader;
		/*readonly*/ GenericParamContext gpContext;
		RecursionCounter recursionCounter;

		public LocalConstantSigBlobReader(ModuleDef module, IImageStream reader, GenericParamContext gpContext) {
			this.module = module;
			this.reader = reader;
			this.gpContext = gpContext;
			recursionCounter = default(RecursionCounter);
		}

		public bool Read(out TypeSig type, out object value) {
			try {
				return ReadCore(out type, out value);
			}
			catch {
			}
			type = null;
			value = null;
			return false;
		}

		bool ReadCore(out TypeSig type, out object value) {
			if (!recursionCounter.Increment()) {
				type = null;
				value = null;
				return false;
			}

			bool res;
			ITypeDefOrRef tdr;
			UTF8String ns, name;
			var et = (ElementType)reader.ReadByte();
			switch (et) {
			case ElementType.Boolean:
				type = module.CorLibTypes.Boolean;
				value = reader.ReadBoolean();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.Char:
				type = module.CorLibTypes.Char;
				value = (char)reader.ReadUInt16();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.I1:
				type = module.CorLibTypes.SByte;
				value = reader.ReadSByte();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.U1:
				type = module.CorLibTypes.Byte;
				value = reader.ReadByte();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.I2:
				type = module.CorLibTypes.Int16;
				value = reader.ReadInt16();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.U2:
				type = module.CorLibTypes.UInt16;
				value = reader.ReadUInt16();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.I4:
				type = module.CorLibTypes.Int32;
				value = reader.ReadInt32();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.U4:
				type = module.CorLibTypes.UInt32;
				value = reader.ReadUInt32();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.I8:
				type = module.CorLibTypes.Int64;
				value = reader.ReadInt64();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.U8:
				type = module.CorLibTypes.UInt64;
				value = reader.ReadUInt64();
				if (reader.Position < reader.Length)
					type = ReadTypeDefOrRefSig();
				res = true;
				break;

			case ElementType.R4:
				type = module.CorLibTypes.Single;
				value = reader.ReadSingle();
				res = true;
				break;

			case ElementType.R8:
				type = module.CorLibTypes.Double;
				value = reader.ReadDouble();
				res = true;
				break;

			case ElementType.String:
				type = module.CorLibTypes.String;
				value = ReadString();
				res = true;
				break;

			case ElementType.Ptr:
				res = Read(out type, out value);
				if (res)
					type = new PtrSig(type);
				break;

			case ElementType.ByRef:
				res = Read(out type, out value);
				if (res)
					type = new ByRefSig(type);
				break;

			case ElementType.Object:
				type = module.CorLibTypes.Object;
				value = null;
				res = true;
				break;

			case ElementType.ValueType:
				tdr = ReadTypeDefOrRef();
				type = tdr.ToTypeSig();
				value = null;
				if (GetName(tdr, out ns, out name) && ns == stringSystem && tdr.DefinitionAssembly.IsCorLib()) {
					if (name == stringDecimal) {
						if (reader.Length - reader.Position != 13)
							goto default;
						try {
							byte b = reader.ReadByte();
							value = new Decimal(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), (b & 0x80) != 0, (byte)(b & 0x7F));
						}
						catch {
							goto default;
						}
					}
					else if (name == stringDateTime) {
						if (reader.Length - reader.Position != 8)
							goto default;
						try {
							value = new DateTime(reader.ReadInt64());
						}
						catch {
							goto default;
						}
					}
				}
				if (value == null && reader.Position != reader.Length)
					value = reader.ReadRemainingBytes();
				res = true;
				break;

			case ElementType.Class:
				type = new ClassSig(ReadTypeDefOrRef());
				value = reader.Position == reader.Length ? null : reader.ReadRemainingBytes();
				res = true;
				break;

			case ElementType.CModReqd:
				tdr = ReadTypeDefOrRef();
				res = Read(out type, out value);
				if (res)
					type = new CModReqdSig(tdr, type);
				break;

			case ElementType.CModOpt:
				tdr = ReadTypeDefOrRef();
				res = Read(out type, out value);
				if (res)
					type = new CModOptSig(tdr, type);
				break;

			case ElementType.Var:
			case ElementType.Array:
			case ElementType.GenericInst:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.FnPtr:
			case ElementType.SZArray:
			case ElementType.MVar:
			case ElementType.End:
			case ElementType.Void:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				Debug.Fail("Unsupported element type in LocalConstant sig blob: " + et.ToString());
				res = false;
				type = null;
				value = null;
				break;
			}

			recursionCounter.Decrement();
			return res;
		}
		static readonly UTF8String stringSystem = new UTF8String("System");
		static readonly UTF8String stringDecimal = new UTF8String("Decimal");
		static readonly UTF8String stringDateTime = new UTF8String("DateTime");

		static bool GetName(ITypeDefOrRef tdr, out UTF8String @namespace, out UTF8String name) {
			var tr = tdr as TypeRef;
			if (tr != null) {
				@namespace = tr.Namespace;
				name = tr.Name;
				return true;
			}

			var td = tdr as TypeDef;
			if (td != null) {
				@namespace = td.Namespace;
				name = td.Name;
				return true;
			}

			@namespace = null;
			name = null;
			return false;
		}

		TypeSig ReadTypeDefOrRefSig() {
			uint codedToken;
			if (!reader.ReadCompressedUInt32(out codedToken))
				return null;
			ISignatureReaderHelper helper = module;
			var tdr = helper.ResolveTypeDefOrRef(codedToken, gpContext);
			return tdr.ToTypeSig();
		}

		ITypeDefOrRef ReadTypeDefOrRef() {
			uint codedToken;
			if (!reader.ReadCompressedUInt32(out codedToken))
				return null;
			ISignatureReaderHelper helper = module;
			var tdr = helper.ResolveTypeDefOrRef(codedToken, gpContext);
			var corType = module.CorLibTypes.GetCorLibTypeSig(tdr);
			if (corType != null)
				return corType.TypeDefOrRef;
			return tdr;
		}

		string ReadString() {
			if (reader.Position == reader.Length)
				return string.Empty;
			byte b = reader.ReadByte();
			if (b == 0xFF && reader.Position == reader.Length)
				return null;
			reader.Position--;
			return Encoding.Unicode.GetString(reader.ReadRemainingBytes());
		}
	}
}
