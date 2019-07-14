// dnlib: See LICENSE.txt for more info

using System;
using System.Text;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Portable {
	readonly struct LocalConstantSigBlobWriter {
		readonly IWriterError helper;
		readonly Metadata systemMetadata;

		LocalConstantSigBlobWriter(IWriterError helper, Metadata systemMetadata) {
			this.helper = helper;
			this.systemMetadata = systemMetadata;
		}

		public static void Write(IWriterError helper, Metadata systemMetadata, DataWriter writer, TypeSig type, object value) {
			var sigWriter = new LocalConstantSigBlobWriter(helper, systemMetadata);
			sigWriter.Write(writer, type, value);
		}

		void Write(DataWriter writer, TypeSig type, object value) {
			for (; ; type = type.Next) {
				if (type is null)
					return;

				var et = type.ElementType;
				writer.WriteByte((byte)et);
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
						writer.WriteSingle((float)value);
					else {
						helper.Error("Expected a Single constant");
						writer.WriteSingle(0);
					}
					return;

				case ElementType.R8:
					if (value is double)
						writer.WriteDouble((double)value);
					else {
						helper.Error("Expected a Double constant");
						writer.WriteDouble(0);
					}
					return;

				case ElementType.String:
					if (value is null)
						writer.WriteByte((byte)0xFF);
					else if (value is string)
						writer.WriteBytes(Encoding.Unicode.GetBytes((string)value));
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
					if (td is null)
						helper.Error($"Couldn't resolve type 0x{tdr?.MDToken.Raw ?? 0:X8}");
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
							writer.Position--;
							writer.WriteByte((byte)underlyingType.GetElementType());
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
						bool valueWritten = false;
						if (GetName(tdr, out var ns, out var name) && ns == stringSystem && tdr.DefinitionAssembly.IsCorLib()) {
							if (name == stringDecimal) {
								if (value is decimal) {
									var bits = decimal.GetBits((decimal)value);
									writer.WriteByte((byte)((((uint)bits[3] >> 31) << 7) | (((uint)bits[3] >> 16) & 0x7F)));
									writer.WriteInt32(bits[0]);
									writer.WriteInt32(bits[1]);
									writer.WriteInt32(bits[2]);
								}
								else {
									helper.Error("Expected a Decimal constant");
									writer.WriteBytes(new byte[13]);
								}
								valueWritten = true;
							}
							else if (name == stringDateTime) {
								if (value is DateTime)
									writer.WriteInt64(((DateTime)value).Ticks);
								else {
									helper.Error("Expected a DateTime constant");
									writer.WriteInt64(0);
								}
								valueWritten = true;
							}
						}
						if (!valueWritten) {
							if (value is byte[])
								writer.WriteBytes((byte[])value);
							else if (!(value is null)) {
								helper.Error("Unsupported constant: " + value.GetType().FullName);
								return;
							}
						}
					}
					return;

				case ElementType.Class:
					WriteTypeDefOrRef(writer, ((ClassSig)type).TypeDefOrRef);
					if (value is byte[])
						writer.WriteBytes((byte[])value);
					else if (!(value is null))
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
			if (tdr is TypeRef tr) {
				@namespace = tr.Namespace;
				name = tr.Name;
				return true;
			}

			if (tdr is TypeDef td) {
				@namespace = td.Namespace;
				name = td.Name;
				return true;
			}

			@namespace = null;
			name = null;
			return false;
		}

		void WritePrimitiveValue(DataWriter writer, ElementType et, object value) {
			switch (et) {
			case ElementType.Boolean:
				if (value is bool)
					writer.WriteBoolean((bool)value);
				else {
					helper.Error("Expected a Boolean constant");
					writer.WriteBoolean(false);
				}
				break;

			case ElementType.Char:
				if (value is char)
					writer.WriteUInt16((char)value);
				else {
					helper.Error("Expected a Char constant");
					writer.WriteUInt16(0);
				}
				break;

			case ElementType.I1:
				if (value is sbyte)
					writer.WriteSByte((sbyte)value);
				else {
					helper.Error("Expected a SByte constant");
					writer.WriteSByte(0);
				}
				break;

			case ElementType.U1:
				if (value is byte)
					writer.WriteByte((byte)value);
				else {
					helper.Error("Expected a Byte constant");
					writer.WriteByte(0);
				}
				break;

			case ElementType.I2:
				if (value is short)
					writer.WriteInt16((short)value);
				else {
					helper.Error("Expected an Int16 constant");
					writer.WriteInt16(0);
				}
				break;

			case ElementType.U2:
				if (value is ushort)
					writer.WriteUInt16((ushort)value);
				else {
					helper.Error("Expected a UInt16 constant");
					writer.WriteUInt16(0);
				}
				break;

			case ElementType.I4:
				if (value is int)
					writer.WriteInt32((int)value);
				else {
					helper.Error("Expected an Int32 constant");
					writer.WriteInt32(0);
				}
				break;

			case ElementType.U4:
				if (value is uint)
					writer.WriteUInt32((uint)value);
				else {
					helper.Error("Expected a UInt32 constant");
					writer.WriteUInt32(0);
				}
				break;

			case ElementType.I8:
				if (value is long)
					writer.WriteInt64((long)value);
				else {
					helper.Error("Expected an Int64 constant");
					writer.WriteInt64(0);
				}
				break;

			case ElementType.U8:
				if (value is ulong)
					writer.WriteUInt64((ulong)value);
				else {
					helper.Error("Expected a UInt64 constant");
					writer.WriteUInt64(0);
				}
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void WriteTypeDefOrRef(DataWriter writer, ITypeDefOrRef tdr) {
			if (!MD.CodedToken.TypeDefOrRef.Encode(systemMetadata.GetToken(tdr), out uint codedToken)) {
				helper.Error("Couldn't encode a TypeDefOrRef");
				return;
			}
			writer.WriteCompressedUInt32(codedToken);
		}
	}
}
