// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Text;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Portable {
	struct LocalConstantSigBlobWriter {
		readonly IWriterError helper;
		readonly MetaData systemMetaData;

		LocalConstantSigBlobWriter(IWriterError helper, MetaData systemMetaData) {
			this.helper = helper;
			this.systemMetaData = systemMetaData;
		}

		public static void Write(IWriterError helper, MetaData systemMetaData, BinaryWriter writer, TypeSig type, object value) {
			var sigWriter = new LocalConstantSigBlobWriter(helper, systemMetaData);
			sigWriter.Write(writer, type, value);
		}

		void Write(BinaryWriter writer, TypeSig type, object value) {
			for (; ; type = type.Next) {
				if (type == null)
					return;

				var et = type.ElementType;
				writer.Write((byte)et);
				switch (et) {
				case ElementType.Boolean:
				case ElementType.Char:
				case ElementType.I1:
				case ElementType.U1:
				case ElementType.I2:
				case ElementType.U2:
				case ElementType.I4:
				case ElementType.U4:
				case ElementType.I8:
				case ElementType.U8:
					WritePrimitiveValue(writer, et, value);
					return;

				case ElementType.R4:
					if (value is float)
						writer.Write((float)value);
					else {
						helper.Error("Expected a Single constant");
						writer.Write((float)0);
					}
					return;

				case ElementType.R8:
					if (value is double)
						writer.Write((double)value);
					else {
						helper.Error("Expected a Double constant");
						writer.Write((double)0);
					}
					return;

				case ElementType.String:
					if (value == null)
						writer.Write((byte)0xFF);
					else if (value is string)
						writer.Write(Encoding.Unicode.GetBytes((string)value));
					else
						helper.Error("Expected a String constant");
					return;

				case ElementType.Ptr:
				case ElementType.ByRef:
					WriteTypeDefOrRef(writer, new TypeSpecUser(type));
					return;

				case ElementType.Object:
					return;

				case ElementType.ValueType:
					var tdr = ((ValueTypeSig)type).TypeDefOrRef;
					var td = tdr.ResolveTypeDef();
					if (td == null)
						helper.Error(string.Format("Couldn't resolve type 0x{0:X8}", tdr == null ? 0 : tdr.MDToken.Raw));
					else if (td.IsEnum) {
						var underlyingType = td.GetEnumUnderlyingType().RemovePinnedAndModifiers();
						switch (underlyingType.GetElementType()) {
						case ElementType.Boolean:
						case ElementType.Char:
						case ElementType.I1:
						case ElementType.U1:
						case ElementType.I2:
						case ElementType.U2:
						case ElementType.I4:
						case ElementType.U4:
						case ElementType.I8:
						case ElementType.U8:
							writer.BaseStream.Position--;
							writer.Write((byte)underlyingType.GetElementType());
							WritePrimitiveValue(writer, underlyingType.GetElementType(), value);
							WriteTypeDefOrRef(writer, tdr);
							return;
						default:
							helper.Error("Invalid enum underlying type");
							return;
						}
					}
					else {
						WriteTypeDefOrRef(writer, tdr);
						UTF8String ns, name;
						bool valueWritten = false;
						if (GetName(tdr, out ns, out name) && ns == stringSystem && tdr.DefinitionAssembly.IsCorLib()) {
							if (name == stringDecimal) {
								if (value is decimal) {
									var bits = decimal.GetBits((decimal)value);
									writer.Write((byte)((((uint)bits[3] >> 31) << 7) | (((uint)bits[3] >> 16) & 0x7F)));
									writer.Write(bits[0]);
									writer.Write(bits[1]);
									writer.Write(bits[2]);
								}
								else {
									helper.Error("Expected a Decimal constant");
									writer.Write(new byte[13]);
								}
								valueWritten = true;
							}
							else if (name == stringDateTime) {
								if (value is DateTime)
									writer.Write(((DateTime)value).Ticks);
								else {
									helper.Error("Expected a DateTime constant");
									writer.Write(0L);
								}
								valueWritten = true;
							}
						}
						if (!valueWritten) {
							if (value is byte[])
								writer.Write((byte[])value);
							else if (value != null) {
								helper.Error("Unsupported constant: " + value.GetType().FullName);
								return;
							}
						}
					}
					return;

				case ElementType.Class:
					WriteTypeDefOrRef(writer, ((ClassSig)type).TypeDefOrRef);
					if (value is byte[])
						writer.Write((byte[])value);
					else if (value != null)
						helper.Error("Expected a null constant");
					return;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
					WriteTypeDefOrRef(writer, ((ModifierSig)type).Modifier);
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
					WriteTypeDefOrRef(writer, new TypeSpecUser(type));
					return;

				case ElementType.End:
				case ElementType.Void:
				case ElementType.ValueArray:
				case ElementType.R:
				case ElementType.Internal:
				case ElementType.Module:
				case ElementType.Sentinel:
				case ElementType.Pinned:
				default:
					helper.Error("Unsupported element type in LocalConstant sig blob: " + et.ToString());
					return;
				}
			}
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

		void WritePrimitiveValue(BinaryWriter writer, ElementType et, object value) {
			switch (et) {
			case ElementType.Boolean:
				if (value is bool)
					writer.Write((bool)value);
				else {
					helper.Error("Expected a Boolean constant");
					writer.Write(false);
				}
				break;

			case ElementType.Char:
				if (value is char)
					writer.Write((ushort)(char)value);
				else {
					helper.Error("Expected a Char constant");
					writer.Write((ushort)0);
				}
				break;

			case ElementType.I1:
				if (value is sbyte)
					writer.Write((sbyte)value);
				else {
					helper.Error("Expected a SByte constant");
					writer.Write((sbyte)0);
				}
				break;

			case ElementType.U1:
				if (value is byte)
					writer.Write((byte)value);
				else {
					helper.Error("Expected a Byte constant");
					writer.Write((byte)0);
				}
				break;

			case ElementType.I2:
				if (value is short)
					writer.Write((short)value);
				else {
					helper.Error("Expected an Int16 constant");
					writer.Write((short)0);
				}
				break;

			case ElementType.U2:
				if (value is ushort)
					writer.Write((ushort)value);
				else {
					helper.Error("Expected a UInt16 constant");
					writer.Write((ushort)0);
				}
				break;

			case ElementType.I4:
				if (value is int)
					writer.Write((int)value);
				else {
					helper.Error("Expected an Int32 constant");
					writer.Write((int)0);
				}
				break;

			case ElementType.U4:
				if (value is uint)
					writer.Write((uint)value);
				else {
					helper.Error("Expected a UInt32 constant");
					writer.Write((uint)0);
				}
				break;

			case ElementType.I8:
				if (value is long)
					writer.Write((long)value);
				else {
					helper.Error("Expected an Int64 constant");
					writer.Write((long)0);
				}
				break;

			case ElementType.U8:
				if (value is ulong)
					writer.Write((ulong)value);
				else {
					helper.Error("Expected a UInt64 constant");
					writer.Write((ulong)0);
				}
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void WriteTypeDefOrRef(BinaryWriter writer, ITypeDefOrRef tdr) {
			uint codedToken;
			if (!MD.CodedToken.TypeDefOrRef.Encode(systemMetaData.GetToken(tdr), out codedToken)) {
				helper.Error("Couldn't encode a TypeDefOrRef");
				return;
			}
			writer.WriteCompressedUInt32(codedToken);
		}
	}
}
