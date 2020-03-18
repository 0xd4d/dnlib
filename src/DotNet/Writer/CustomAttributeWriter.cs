// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Helps <see cref="CustomAttributeWriter"/> write custom attributes
	/// </summary>
	public interface ICustomAttributeWriterHelper : IWriterError, IFullNameFactoryHelper {
	}

	/// <summary>
	/// Writes <see cref="CustomAttribute"/>s
	/// </summary>
	public struct CustomAttributeWriter : IDisposable {
		readonly ICustomAttributeWriterHelper helper;
		RecursionCounter recursionCounter;
		readonly MemoryStream outStream;
		readonly DataWriter writer;
		readonly bool disposeStream;
		GenericArguments genericArguments;

		/// <summary>
		/// Writes a custom attribute
		/// </summary>
		/// <param name="helper">Helper class</param>
		/// <param name="ca">The custom attribute</param>
		/// <returns>Custom attribute blob</returns>
		public static byte[] Write(ICustomAttributeWriterHelper helper, CustomAttribute ca) {
			using (var writer = new CustomAttributeWriter(helper)) {
				writer.Write(ca);
				return writer.GetResult();
			}
		}

		internal static byte[] Write(ICustomAttributeWriterHelper helper, CustomAttribute ca, DataWriterContext context) {
			using (var writer = new CustomAttributeWriter(helper, context)) {
				writer.Write(ca);
				return writer.GetResult();
			}
		}

		/// <summary>
		/// Writes custom attribute named arguments
		/// </summary>
		/// <param name="helper">Helper class</param>
		/// <param name="namedArgs">Named arguments</param>
		/// <returns>The named args blob</returns>
		internal static byte[] Write(ICustomAttributeWriterHelper helper, IList<CANamedArgument> namedArgs) {
			using (var writer = new CustomAttributeWriter(helper)) {
				writer.Write(namedArgs);
				return writer.GetResult();
			}
		}

		internal static byte[] Write(ICustomAttributeWriterHelper helper, IList<CANamedArgument> namedArgs, DataWriterContext context) {
			using (var writer = new CustomAttributeWriter(helper, context)) {
				writer.Write(namedArgs);
				return writer.GetResult();
			}
		}

		CustomAttributeWriter(ICustomAttributeWriterHelper helper) {
			this.helper = helper;
			recursionCounter = new RecursionCounter();
			outStream = new MemoryStream();
			writer = new DataWriter(outStream);
			genericArguments = null;
			disposeStream = true;
		}

		CustomAttributeWriter(ICustomAttributeWriterHelper helper, DataWriterContext context) {
			this.helper = helper;
			recursionCounter = new RecursionCounter();
			outStream = context.OutStream;
			writer = context.Writer;
			genericArguments = null;
			disposeStream = false;
			outStream.SetLength(0);
			outStream.Position = 0;
		}

		byte[] GetResult() => outStream.ToArray();

		void Write(CustomAttribute ca) {
			if (ca is null) {
				helper.Error("The custom attribute is null");
				return;
			}

			// Check whether it's raw first. If it is, we don't care whether the ctor is
			// invalid. Just use the raw data.
			if (ca.IsRawBlob) {
				if ((!(ca.ConstructorArguments is null) && ca.ConstructorArguments.Count > 0) || (!(ca.NamedArguments is null) && ca.NamedArguments.Count > 0))
					helper.Error("Raw custom attribute contains arguments and/or named arguments");
				writer.WriteBytes(ca.RawData);
				return;
			}

			if (ca.Constructor is null) {
				helper.Error("Custom attribute ctor is null");
				return;
			}

			var methodSig = GetMethodSig(ca.Constructor);
			if (methodSig is null) {
				helper.Error("Custom attribute ctor's method signature is invalid");
				return;
			}

			if (ca.ConstructorArguments.Count != methodSig.Params.Count)
				helper.Error("Custom attribute arguments count != method sig arguments count");
			if (!(methodSig.ParamsAfterSentinel is null) && methodSig.ParamsAfterSentinel.Count > 0)
				helper.Error("Custom attribute ctor has parameters after the sentinel");
			if (ca.NamedArguments.Count > ushort.MaxValue)
				helper.Error("Custom attribute has too many named arguments");

			if (ca.Constructor is MemberRef mrCtor && mrCtor.Class is TypeSpec owner && owner.TypeSig is GenericInstSig gis) {
				genericArguments = new GenericArguments();
				genericArguments.PushTypeArgs(gis.GenericArguments);
			}

			writer.WriteUInt16((ushort)1);

			int numArgs = Math.Min(methodSig.Params.Count, ca.ConstructorArguments.Count);
			for (int i = 0; i < numArgs; i++)
				WriteValue(FixTypeSig(methodSig.Params[i]), ca.ConstructorArguments[i]);

			int numNamedArgs = Math.Min((int)ushort.MaxValue, ca.NamedArguments.Count);
			writer.WriteUInt16((ushort)numNamedArgs);
			for (int i = 0; i < numNamedArgs; i++)
				Write(ca.NamedArguments[i]);
		}

		void Write(IList<CANamedArgument> namedArgs) {
			if (namedArgs is null || namedArgs.Count > 0x1FFFFFFF) {
				helper.Error("Too many custom attribute named arguments");
				namedArgs = Array2.Empty<CANamedArgument>();
			}
			writer.WriteCompressedUInt32((uint)namedArgs.Count);
			for (int i = 0; i < namedArgs.Count; i++)
				Write(namedArgs[i]);
		}

		TypeSig FixTypeSig(TypeSig type) => SubstituteGenericParameter(type.RemoveModifiers()).RemoveModifiers();

		TypeSig SubstituteGenericParameter(TypeSig type) {
			if (genericArguments is null)
				return type;
			return genericArguments.Resolve(type);
		}

		void WriteValue(TypeSig argType, CAArgument value) {
			if (argType is null || value.Type is null) {
				helper.Error("Custom attribute argument type is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			if (argType is SZArraySig arrayType) {
				var argsArray = value.Value as IList<CAArgument>;
				if (argsArray is null && !(value.Value is null))
					helper.Error("CAArgument.Value is not null or an array");
				WriteArrayValue(arrayType, argsArray);
			}
			else
				WriteElem(argType, value);

			recursionCounter.Decrement();
		}

		void WriteArrayValue(SZArraySig arrayType, IList<CAArgument> args) {
			if (arrayType is null) {
				helper.Error("Custom attribute: Array type is null");
				return;
			}

			if (args is null)
				writer.WriteUInt32(uint.MaxValue);
			else {
				writer.WriteUInt32((uint)args.Count);
				var arrayElementType = FixTypeSig(arrayType.Next);
				for (int i = 0; i < args.Count; i++)
					WriteValue(arrayElementType, args[i]);
			}
		}

		bool VerifyTypeAndValue(CAArgument value, ElementType etype) {
			if (!VerifyType(value.Type, etype)) {
				helper.Error("Custom attribute arg type != value.Type");
				return false;
			}
			if (!VerifyValue(value.Value, etype)) {
				helper.Error("Custom attribute value.Value's type != value.Type");
				return false;
			}
			return true;
		}

		bool VerifyTypeAndValue(CAArgument value, ElementType etype, Type valueType) {
			if (!VerifyType(value.Type, etype)) {
				helper.Error("Custom attribute arg type != value.Type");
				return false;
			}
			return value.Value is null || value.Value.GetType() == valueType;
		}

		static bool VerifyType(TypeSig type, ElementType etype) {
			type = type.RemoveModifiers();
			// Assume it's an enum if it's a ValueType
			return !(type is null) && (etype == type.ElementType || type.ElementType == ElementType.ValueType);
		}

		static bool VerifyValue(object o, ElementType etype) {
			if (o is null)
				return false;

			return Type.GetTypeCode(o.GetType()) switch {
				TypeCode.Boolean => etype == ElementType.Boolean,
				TypeCode.Char => etype == ElementType.Char,
				TypeCode.SByte => etype == ElementType.I1,
				TypeCode.Byte => etype == ElementType.U1,
				TypeCode.Int16 => etype == ElementType.I2,
				TypeCode.UInt16 => etype == ElementType.U2,
				TypeCode.Int32 => etype == ElementType.I4,
				TypeCode.UInt32 => etype == ElementType.U4,
				TypeCode.Int64 => etype == ElementType.I8,
				TypeCode.UInt64 => etype == ElementType.U8,
				TypeCode.Single => etype == ElementType.R4,
				TypeCode.Double => etype == ElementType.R8,
				_ => false,
			};
		}

		static ulong ToUInt64(object o) {
			ToUInt64(o, out ulong result);
			return result;
		}

		static bool ToUInt64(object o, out ulong result) {
			if (o is null) {
				result = 0;
				return false;
			}

			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:
				result = (bool)o ? 1UL : 0UL;
				return true;

			case TypeCode.Char:
				result = (ushort)(char)o;
				return true;

			case TypeCode.SByte:
				result = (ulong)(sbyte)o;
				return true;

			case TypeCode.Byte:
				result = (byte)o;
				return true;

			case TypeCode.Int16:
				result = (ulong)(short)o;
				return true;

			case TypeCode.UInt16:
				result = (ushort)o;
				return true;

			case TypeCode.Int32:
				result = (ulong)(int)o;
				return true;

			case TypeCode.UInt32:
				result = (uint)o;
				return true;

			case TypeCode.Int64:
				result = (ulong)(long)o;
				return true;

			case TypeCode.UInt64:
				result = (ulong)o;
				return true;

			case TypeCode.Single:
				result = (ulong)(float)o;
				return true;

			case TypeCode.Double:
				result = (ulong)(double)o;
				return true;
			}

			result = 0;
			return false;
		}

		static double ToDouble(object o) {
			ToDouble(o, out double result);
			return result;
		}

		static bool ToDouble(object o, out double result) {
			if (o is null) {
				result = double.NaN;
				return false;
			}

			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:
				result = (bool)o ? 1 : 0;
				return true;

			case TypeCode.Char:
				result = (ushort)(char)o;
				return true;

			case TypeCode.SByte:
				result = (sbyte)o;
				return true;

			case TypeCode.Byte:
				result = (byte)o;
				return true;

			case TypeCode.Int16:
				result = (short)o;
				return true;

			case TypeCode.UInt16:
				result = (ushort)o;
				return true;

			case TypeCode.Int32:
				result = (int)o;
				return true;

			case TypeCode.UInt32:
				result = (uint)o;
				return true;

			case TypeCode.Int64:
				result = (long)o;
				return true;

			case TypeCode.UInt64:
				result = (ulong)o;
				return true;

			case TypeCode.Single:
				result = (float)o;
				return true;

			case TypeCode.Double:
				result = (double)o;
				return true;
			}

			result = double.NaN;
			return false;
		}

		/// <summary>
		/// Write a value
		/// </summary>
		/// <param name="argType">The ctor arg type, field type, or property type</param>
		/// <param name="value">The value to write</param>
		void WriteElem(TypeSig argType, CAArgument value) {
			if (argType is null) {
				helper.Error("Custom attribute: Arg type is null");
				argType = value.Type;
				if (argType is null)
					return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			TypeSig underlyingType;
			ITypeDefOrRef tdr;
			switch (argType.ElementType) {
			case ElementType.Boolean:
				if (!VerifyTypeAndValue(value, ElementType.Boolean))
					writer.WriteBoolean(ToUInt64(value.Value) != 0);
				else
					writer.WriteBoolean((bool)value.Value);
				break;

			case ElementType.Char:
				if (!VerifyTypeAndValue(value, ElementType.Char))
					writer.WriteUInt16((ushort)ToUInt64(value.Value));
				else
					writer.WriteUInt16((ushort)(char)value.Value);
				break;

			case ElementType.I1:
				if (!VerifyTypeAndValue(value, ElementType.I1))
					writer.WriteSByte((sbyte)ToUInt64(value.Value));
				else
					writer.WriteSByte((sbyte)value.Value);
				break;

			case ElementType.U1:
				if (!VerifyTypeAndValue(value, ElementType.U1))
					writer.WriteByte((byte)ToUInt64(value.Value));
				else
					writer.WriteByte((byte)value.Value);
				break;

			case ElementType.I2:
				if (!VerifyTypeAndValue(value, ElementType.I2))
					writer.WriteInt16((short)ToUInt64(value.Value));
				else
					writer.WriteInt16((short)value.Value);
				break;

			case ElementType.U2:
				if (!VerifyTypeAndValue(value, ElementType.U2))
					writer.WriteUInt16((ushort)ToUInt64(value.Value));
				else
					writer.WriteUInt16((ushort)value.Value);
				break;

			case ElementType.I4:
				if (!VerifyTypeAndValue(value, ElementType.I4))
					writer.WriteInt32((int)ToUInt64(value.Value));
				else
					writer.WriteInt32((int)value.Value);
				break;

			case ElementType.U4:
				if (!VerifyTypeAndValue(value, ElementType.U4))
					writer.WriteUInt32((uint)ToUInt64(value.Value));
				else
					writer.WriteUInt32((uint)value.Value);
				break;

			case ElementType.I8:
				if (!VerifyTypeAndValue(value, ElementType.I8))
					writer.WriteInt64((long)ToUInt64(value.Value));
				else
					writer.WriteInt64((long)value.Value);
				break;

			case ElementType.U8:
				if (!VerifyTypeAndValue(value, ElementType.U8))
					writer.WriteUInt64(ToUInt64(value.Value));
				else
					writer.WriteUInt64((ulong)value.Value);
				break;

			case ElementType.R4:
				if (!VerifyTypeAndValue(value, ElementType.R4))
					writer.WriteSingle((float)ToDouble(value.Value));
				else
					writer.WriteSingle((float)value.Value);
				break;

			case ElementType.R8:
				if (!VerifyTypeAndValue(value, ElementType.R8))
					writer.WriteDouble(ToDouble(value.Value));
				else
					writer.WriteDouble((double)value.Value);
				break;

			case ElementType.String:
				if (VerifyTypeAndValue(value, ElementType.String, typeof(UTF8String)))
					WriteUTF8String((UTF8String)value.Value);
				else if (VerifyTypeAndValue(value, ElementType.String, typeof(string)))
					WriteUTF8String((string)value.Value);
				else
					WriteUTF8String(UTF8String.Empty);
				break;

			case ElementType.ValueType:
				tdr = ((TypeDefOrRefSig)argType).TypeDefOrRef;
				underlyingType = GetEnumUnderlyingType(argType);
				if (!(underlyingType is null))
					WriteElem(underlyingType, value);
				else if (tdr is TypeRef && TryWriteEnumUnderlyingTypeValue(value.Value)) {
					// No error. Assume it's an enum that couldn't be resolved.
				}
				else
					helper.Error("Custom attribute value is not an enum");
				break;

			case ElementType.Class:
				tdr = ((TypeDefOrRefSig)argType).TypeDefOrRef;
				if (CheckCorLibType(argType, "Type")) {
					if (CheckCorLibType(value.Type, "Type")) {
						if (value.Value is TypeSig ts)
							WriteType(ts);
						else if (value.Value is null)
							WriteUTF8String(null);
						else {
							helper.Error("Custom attribute value is not a type");
							WriteUTF8String(UTF8String.Empty);
						}
					}
					else {
						helper.Error("Custom attribute value type is not System.Type");
						WriteUTF8String(UTF8String.Empty);
					}
					break;
				}
				else if (tdr is TypeRef && TryWriteEnumUnderlyingTypeValue(value.Value)) {
					// No error. Assume it's an enum that couldn't be resolved.
					break;
				}
				goto default;

			case ElementType.SZArray:
				WriteValue(argType, value);
				break;

			case ElementType.Object:
				WriteFieldOrPropType(value.Type);
				WriteElem(value.Type, value);
				break;

			case ElementType.End:
			case ElementType.Void:
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Var:
			case ElementType.Array:
			case ElementType.GenericInst:
			case ElementType.TypedByRef:
			case ElementType.ValueArray:
			case ElementType.I:
			case ElementType.U:
			case ElementType.R:
			case ElementType.FnPtr:
			case ElementType.MVar:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				helper.Error("Invalid or unsupported element type in custom attribute");
				break;
			}

			recursionCounter.Decrement();
		}

		bool TryWriteEnumUnderlyingTypeValue(object o) {
			if (o is null)
				return false;
			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:	writer.WriteBoolean((bool)o); break;
			case TypeCode.Char:		writer.WriteUInt16((ushort)(char)o); break;
			case TypeCode.SByte:	writer.WriteSByte((sbyte)o); break;
			case TypeCode.Byte:		writer.WriteByte((byte)o); break;
			case TypeCode.Int16:	writer.WriteInt16((short)o); break;
			case TypeCode.UInt16:	writer.WriteUInt16((ushort)o); break;
			case TypeCode.Int32:	writer.WriteInt32((int)o); break;
			case TypeCode.UInt32:	writer.WriteUInt32((uint)o); break;
			case TypeCode.Int64:	writer.WriteInt64((long)o); break;
			case TypeCode.UInt64:	writer.WriteUInt64((ulong)o); break;
			default: return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the enum's underlying type
		/// </summary>
		/// <param name="type">An enum type</param>
		/// <returns>The underlying type or <c>null</c> if we couldn't resolve the type ref</returns>
		static TypeSig GetEnumUnderlyingType(TypeSig type) {
			var td = GetEnumTypeDef(type);
			if (td is null)
				return null;
			return td.GetEnumUnderlyingType().RemoveModifiers();
		}

		static TypeDef GetEnumTypeDef(TypeSig type) {
			if (type is null)
				return null;
			var td = GetTypeDef(type);
			if (td is null)
				return null;
			if (!td.IsEnum)
				return null;
			return td;
		}

		/// <summary>
		/// Converts <paramref name="type"/> to a <see cref="TypeDef"/>, possibly resolving
		/// a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="TypeDef"/> or <c>null</c> if we couldn't resolve the
		/// <see cref="TypeRef"/> or if <paramref name="type"/> is a type spec</returns>
		static TypeDef GetTypeDef(TypeSig type) {
			if (type is TypeDefOrRefSig tdr) {
				var td = tdr.TypeDef;
				if (!(td is null))
					return td;

				var tr = tdr.TypeRef;
				if (!(tr is null))
					return tr.Resolve();
			}

			return null;
		}

		void Write(CANamedArgument namedArg) {
			if (namedArg is null) {
				helper.Error("Custom attribute named arg is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			if (namedArg.IsProperty)
				writer.WriteByte((byte)SerializationType.Property);
			else
				writer.WriteByte((byte)SerializationType.Field);

			WriteFieldOrPropType(namedArg.Type);
			WriteUTF8String(namedArg.Name);
			WriteValue(namedArg.Type, namedArg.Argument);

			recursionCounter.Decrement();
		}

		void WriteFieldOrPropType(TypeSig type) {
			type = type.RemoveModifiers();
			if (type is null) {
				helper.Error("Custom attribute: Field/property type is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			ITypeDefOrRef tdr;
			switch (type.ElementType) {
			case ElementType.Boolean:	writer.WriteByte((byte)SerializationType.Boolean); break;
			case ElementType.Char:		writer.WriteByte((byte)SerializationType.Char); break;
			case ElementType.I1:		writer.WriteByte((byte)SerializationType.I1); break;
			case ElementType.U1:		writer.WriteByte((byte)SerializationType.U1); break;
			case ElementType.I2:		writer.WriteByte((byte)SerializationType.I2); break;
			case ElementType.U2:		writer.WriteByte((byte)SerializationType.U2); break;
			case ElementType.I4:		writer.WriteByte((byte)SerializationType.I4); break;
			case ElementType.U4:		writer.WriteByte((byte)SerializationType.U4); break;
			case ElementType.I8:		writer.WriteByte((byte)SerializationType.I8); break;
			case ElementType.U8:		writer.WriteByte((byte)SerializationType.U8); break;
			case ElementType.R4:		writer.WriteByte((byte)SerializationType.R4); break;
			case ElementType.R8:		writer.WriteByte((byte)SerializationType.R8); break;
			case ElementType.String:	writer.WriteByte((byte)SerializationType.String); break;
			case ElementType.Object:	writer.WriteByte((byte)SerializationType.TaggedObject); break;

			case ElementType.SZArray:
				writer.WriteByte((byte)SerializationType.SZArray);
				WriteFieldOrPropType(type.Next);
				break;

			case ElementType.Class:
				tdr = ((TypeDefOrRefSig)type).TypeDefOrRef;
				if (CheckCorLibType(type, "Type"))
					writer.WriteByte((byte)SerializationType.Type);
				else if (tdr is TypeRef) {
					// Could be an enum TypeRef that couldn't be resolved, so the code
					// assumed it's a class and created a ClassSig.
					writer.WriteByte((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				else
					goto default;
				break;

			case ElementType.ValueType:
				tdr = ((TypeDefOrRefSig)type).TypeDefOrRef;
				var enumType = GetEnumTypeDef(type);
				// If TypeRef => assume it's an enum that couldn't be resolved
				if (!(enumType is null) || tdr is TypeRef) {
					writer.WriteByte((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				else {
					helper.Error("Custom attribute type doesn't seem to be an enum.");
					writer.WriteByte((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				break;

			default:
				helper.Error("Custom attribute: Invalid type");
				writer.WriteByte((byte)0xFF);
				break;
			}

			recursionCounter.Decrement();
		}

		void WriteType(IType type) {
			if (type is null) {
				helper.Error("Custom attribute: Type is null");
				WriteUTF8String(UTF8String.Empty);
			}
			else
				WriteUTF8String(FullNameFactory.AssemblyQualifiedName(type, helper));
		}

		static bool CheckCorLibType(TypeSig ts, string name) {
			var tdrs = ts as TypeDefOrRefSig;
			if (tdrs is null)
				return false;
			return CheckCorLibType(tdrs.TypeDefOrRef, name);
		}

		static bool CheckCorLibType(ITypeDefOrRef tdr, string name) {
			if (tdr is null)
				return false;
			if (!tdr.DefinitionAssembly.IsCorLib())
				return false;
			if (tdr is TypeSpec)
				return false;
			return tdr.TypeName == name && tdr.Namespace == "System";
		}

		static MethodSig GetMethodSig(ICustomAttributeType ctor) => ctor?.MethodSig;

		void WriteUTF8String(UTF8String s) {
			if (s is null || s.Data is null)
				writer.WriteByte((byte)0xFF);
			else {
				writer.WriteCompressedUInt32((uint)s.Data.Length);
				writer.WriteBytes(s.Data);
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!disposeStream)
				return;
			if (!(outStream is null))
				outStream.Dispose();
		}
	}
}
