// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Helps <see cref="CustomAttributeWriter"/> write custom attributes
	/// </summary>
	public interface ICustomAttributeWriterHelper : IWriterError, IFullNameCreatorHelper {
	}

	/// <summary>
	/// Writes <see cref="CustomAttribute"/>s
	/// </summary>
	public struct CustomAttributeWriter : IDisposable {
		readonly ICustomAttributeWriterHelper helper;
		RecursionCounter recursionCounter;
		readonly MemoryStream outStream;
		readonly BinaryWriter writer;
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

		internal static byte[] Write(ICustomAttributeWriterHelper helper, CustomAttribute ca, BinaryWriterContext context) {
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

		internal static byte[] Write(ICustomAttributeWriterHelper helper, IList<CANamedArgument> namedArgs, BinaryWriterContext context) {
			using (var writer = new CustomAttributeWriter(helper, context)) {
				writer.Write(namedArgs);
				return writer.GetResult();
			}
		}

		CustomAttributeWriter(ICustomAttributeWriterHelper helper) {
			this.helper = helper;
			this.recursionCounter = new RecursionCounter();
			this.outStream = new MemoryStream();
			this.writer = new BinaryWriter(outStream);
			this.genericArguments = null;
			this.disposeStream = true;
		}

		CustomAttributeWriter(ICustomAttributeWriterHelper helper, BinaryWriterContext context) {
			this.helper = helper;
			this.recursionCounter = new RecursionCounter();
			this.outStream = context.OutStream;
			this.writer = context.Writer;
			this.genericArguments = null;
			this.disposeStream = false;
			outStream.SetLength(0);
			outStream.Position = 0;
		}

		byte[] GetResult() {
			return outStream.ToArray();
		}

		void Write(CustomAttribute ca) {
			if (ca == null) {
				helper.Error("The custom attribute is null");
				return;
			}

			// Check whether it's raw first. If it is, we don't care whether the ctor is
			// invalid. Just use the raw data.
			if (ca.IsRawBlob) {
				if ((ca.ConstructorArguments != null && ca.ConstructorArguments.Count > 0) || (ca.NamedArguments != null && ca.NamedArguments.Count > 0))
					helper.Error("Raw custom attribute contains arguments and/or named arguments");
				writer.Write(ca.RawData);
				return;
			}

			if (ca.Constructor == null) {
				helper.Error("Custom attribute ctor is null");
				return;
			}

			var methodSig = GetMethodSig(ca.Constructor);
			if (methodSig == null) {
				helper.Error("Custom attribute ctor's method signature is invalid");
				return;
			}

			if (ca.ConstructorArguments.Count != methodSig.Params.Count)
				helper.Error("Custom attribute arguments count != method sig arguments count");
			if (methodSig.ParamsAfterSentinel != null && methodSig.ParamsAfterSentinel.Count > 0)
				helper.Error("Custom attribute ctor has parameters after the sentinel");
			if (ca.NamedArguments.Count > ushort.MaxValue)
				helper.Error("Custom attribute has too many named arguments");

			// A generic custom attribute isn't allowed by most .NET languages (eg. C#) but
			// the CLR probably supports it.
			var mrCtor = ca.Constructor as MemberRef;
			if (mrCtor != null) {
				var owner = mrCtor.Class as TypeSpec;
				if (owner != null) {
					var gis = owner.TypeSig as GenericInstSig;
					if (gis != null) {
						genericArguments = new GenericArguments();
						genericArguments.PushTypeArgs(gis.GenericArguments);
					}
				}
			}

			writer.Write((ushort)1);

			int numArgs = Math.Min(methodSig.Params.Count, ca.ConstructorArguments.Count);
			for (int i = 0; i < numArgs; i++)
				WriteValue(FixTypeSig(methodSig.Params[i]), ca.ConstructorArguments[i]);

			int numNamedArgs = Math.Min((int)ushort.MaxValue, ca.NamedArguments.Count);
			writer.Write((ushort)numNamedArgs);
			for (int i = 0; i < numNamedArgs; i++)
				Write(ca.NamedArguments[i]);
		}

		void Write(IList<CANamedArgument> namedArgs) {
			if (namedArgs == null || namedArgs.Count > 0x1FFFFFFF) {
				helper.Error("Too many custom attribute named arguments");
				namedArgs = new CANamedArgument[0];
			}
			writer.WriteCompressedUInt32((uint)namedArgs.Count);
			for (int i = 0; i < namedArgs.Count; i++)
				Write(namedArgs[i]);
		}

		TypeSig FixTypeSig(TypeSig type) {
			return SubstituteGenericParameter(type.RemoveModifiers()).RemoveModifiers();
		}

		TypeSig SubstituteGenericParameter(TypeSig type) {
			if (genericArguments == null)
				return type;
			return genericArguments.Resolve(type);
		}

		void WriteValue(TypeSig argType, CAArgument value) {
			if (argType == null || value.Type == null) {
				helper.Error("Custom attribute argument type is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			var arrayType = argType as SZArraySig;
			if (arrayType != null) {
				var argsArray = value.Value as IList<CAArgument>;
				if (argsArray == null && value.Value != null)
					helper.Error("CAArgument.Value is not null or an array");
				WriteArrayValue(arrayType, argsArray);
			}
			else
				WriteElem(argType, value);

			recursionCounter.Decrement();
		}

		void WriteArrayValue(SZArraySig arrayType, IList<CAArgument> args) {
			if (arrayType == null) {
				helper.Error("Custom attribute: Array type is null");
				return;
			}

			if (args == null)
				writer.Write(uint.MaxValue);
			else {
				writer.Write((uint)args.Count);
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
			return value.Value == null || value.Value.GetType() == valueType;
		}

		static bool VerifyType(TypeSig type, ElementType etype) {
			type = type.RemoveModifiers();
			// Assume it's an enum if it's a ValueType
			return type != null && (etype == type.ElementType || type.ElementType == ElementType.ValueType);
		}

		static bool VerifyValue(object o, ElementType etype) {
			if (o == null)
				return false;

			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:	return etype == ElementType.Boolean;
			case TypeCode.Char:		return etype == ElementType.Char;
			case TypeCode.SByte:	return etype == ElementType.I1;
			case TypeCode.Byte:		return etype == ElementType.U1;
			case TypeCode.Int16:	return etype == ElementType.I2;
			case TypeCode.UInt16:	return etype == ElementType.U2;
			case TypeCode.Int32:	return etype == ElementType.I4;
			case TypeCode.UInt32:	return etype == ElementType.U4;
			case TypeCode.Int64:	return etype == ElementType.I8;
			case TypeCode.UInt64:	return etype == ElementType.U8;
			case TypeCode.Single:	return etype == ElementType.R4;
			case TypeCode.Double:	return etype == ElementType.R8;
			default: return false;
			}
		}

		static ulong ToUInt64(object o) {
			ulong result;
			ToUInt64(o, out result);
			return result;
		}

		static bool ToUInt64(object o, out ulong result) {
			if (o == null) {
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
			double result;
			ToDouble(o, out result);
			return result;
		}

		static bool ToDouble(object o, out double result) {
			if (o == null) {
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
			if (argType == null) {
				helper.Error("Custom attribute: Arg type is null");
				argType = value.Type;
				if (argType == null)
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
					writer.Write(ToUInt64(value.Value) != 0);
				else
					writer.Write((bool)value.Value);
				break;

			case ElementType.Char:
				if (!VerifyTypeAndValue(value, ElementType.Char))
					writer.Write((ushort)ToUInt64(value.Value));
				else
					writer.Write((ushort)(char)value.Value);
				break;

			case ElementType.I1:
				if (!VerifyTypeAndValue(value, ElementType.I1))
					writer.Write((sbyte)ToUInt64(value.Value));
				else
					writer.Write((sbyte)value.Value);
				break;

			case ElementType.U1:
				if (!VerifyTypeAndValue(value, ElementType.U1))
					writer.Write((byte)ToUInt64(value.Value));
				else
					writer.Write((byte)value.Value);
				break;

			case ElementType.I2:
				if (!VerifyTypeAndValue(value, ElementType.I2))
					writer.Write((short)ToUInt64(value.Value));
				else
					writer.Write((short)value.Value);
				break;

			case ElementType.U2:
				if (!VerifyTypeAndValue(value, ElementType.U2))
					writer.Write((ushort)ToUInt64(value.Value));
				else
					writer.Write((ushort)value.Value);
				break;

			case ElementType.I4:
				if (!VerifyTypeAndValue(value, ElementType.I4))
					writer.Write((int)ToUInt64(value.Value));
				else
					writer.Write((int)value.Value);
				break;

			case ElementType.U4:
				if (!VerifyTypeAndValue(value, ElementType.U4))
					writer.Write((uint)ToUInt64(value.Value));
				else
					writer.Write((uint)value.Value);
				break;

			case ElementType.I8:
				if (!VerifyTypeAndValue(value, ElementType.I8))
					writer.Write((long)ToUInt64(value.Value));
				else
					writer.Write((long)value.Value);
				break;

			case ElementType.U8:
				if (!VerifyTypeAndValue(value, ElementType.U8))
					writer.Write(ToUInt64(value.Value));
				else
					writer.Write((ulong)value.Value);
				break;

			case ElementType.R4:
				if (!VerifyTypeAndValue(value, ElementType.R4))
					writer.Write((float)ToDouble(value.Value));
				else
					writer.Write((float)value.Value);
				break;

			case ElementType.R8:
				if (!VerifyTypeAndValue(value, ElementType.R8))
					writer.Write(ToDouble(value.Value));
				else
					writer.Write((double)value.Value);
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
				if (underlyingType != null)
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
						var ts = value.Value as TypeSig;
						if (ts != null)
							WriteType(ts);
						else if (value.Value == null)
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
			if (o == null)
				return false;
			switch (Type.GetTypeCode(o.GetType())) {
			case TypeCode.Boolean:	writer.Write((bool)o); break;
			case TypeCode.Char:		writer.Write((ushort)(char)o); break;
			case TypeCode.SByte:	writer.Write((sbyte)o); break;
			case TypeCode.Byte:		writer.Write((byte)o); break;
			case TypeCode.Int16:	writer.Write((short)o); break;
			case TypeCode.UInt16:	writer.Write((ushort)o); break;
			case TypeCode.Int32:	writer.Write((int)o); break;
			case TypeCode.UInt32:	writer.Write((uint)o); break;
			case TypeCode.Int64:	writer.Write((long)o); break;
			case TypeCode.UInt64:	writer.Write((ulong)o); break;
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
			if (td == null)
				return null;
			return td.GetEnumUnderlyingType().RemoveModifiers();
		}

		static TypeDef GetEnumTypeDef(TypeSig type) {
			if (type == null)
				return null;
			var td = GetTypeDef(type);
			if (td == null)
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
			var tdr = type as TypeDefOrRefSig;
			if (tdr != null) {
				var td = tdr.TypeDef;
				if (td != null)
					return td;

				var tr = tdr.TypeRef;
				if (tr != null)
					return tr.Resolve();
			}

			return null;
		}

		void Write(CANamedArgument namedArg) {
			if (namedArg == null) {
				helper.Error("Custom attribute named arg is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			if (namedArg.IsProperty)
				writer.Write((byte)SerializationType.Property);
			else
				writer.Write((byte)SerializationType.Field);

			WriteFieldOrPropType(namedArg.Type);
			WriteUTF8String(namedArg.Name);
			WriteValue(namedArg.Type, namedArg.Argument);

			recursionCounter.Decrement();
		}

		void WriteFieldOrPropType(TypeSig type) {
			type = type.RemoveModifiers();
			if (type == null) {
				helper.Error("Custom attribute: Field/property type is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			ITypeDefOrRef tdr;
			switch (type.ElementType) {
			case ElementType.Boolean:	writer.Write((byte)SerializationType.Boolean); break;
			case ElementType.Char:		writer.Write((byte)SerializationType.Char); break;
			case ElementType.I1:		writer.Write((byte)SerializationType.I1); break;
			case ElementType.U1:		writer.Write((byte)SerializationType.U1); break;
			case ElementType.I2:		writer.Write((byte)SerializationType.I2); break;
			case ElementType.U2:		writer.Write((byte)SerializationType.U2); break;
			case ElementType.I4:		writer.Write((byte)SerializationType.I4); break;
			case ElementType.U4:		writer.Write((byte)SerializationType.U4); break;
			case ElementType.I8:		writer.Write((byte)SerializationType.I8); break;
			case ElementType.U8:		writer.Write((byte)SerializationType.U8); break;
			case ElementType.R4:		writer.Write((byte)SerializationType.R4); break;
			case ElementType.R8:		writer.Write((byte)SerializationType.R8); break;
			case ElementType.String:	writer.Write((byte)SerializationType.String); break;
			case ElementType.Object:	writer.Write((byte)SerializationType.TaggedObject); break;

			case ElementType.SZArray:
				writer.Write((byte)SerializationType.SZArray);
				WriteFieldOrPropType(type.Next);
				break;

			case ElementType.Class:
				tdr = ((TypeDefOrRefSig)type).TypeDefOrRef;
				if (CheckCorLibType(type, "Type"))
					writer.Write((byte)SerializationType.Type);
				else if (tdr is TypeRef) {
					// Could be an enum TypeRef that couldn't be resolved, so the code
					// assumed it's a class and created a ClassSig.
					writer.Write((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				else
					goto default;
				break;

			case ElementType.ValueType:
				tdr = ((TypeDefOrRefSig)type).TypeDefOrRef;
				var enumType = GetEnumTypeDef(type);
				// If TypeRef => assume it's an enum that couldn't be resolved
				if (enumType != null || tdr is TypeRef) {
					writer.Write((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				else {
					helper.Error("Custom attribute type doesn't seem to be an enum.");
					writer.Write((byte)SerializationType.Enum);
					WriteType(tdr);
				}
				break;

			default:
				helper.Error("Custom attribute: Invalid type");
				writer.Write((byte)0xFF);
				break;
			}

			recursionCounter.Decrement();
		}

		void WriteType(IType type) {
			if (type == null) {
				helper.Error("Custom attribute: Type is null");
				WriteUTF8String(UTF8String.Empty);
			}
			else
				WriteUTF8String(FullNameCreator.AssemblyQualifiedName(type, helper));
		}

		static bool CheckCorLibType(TypeSig ts, string name) {
			var tdrs = ts as TypeDefOrRefSig;
			if (tdrs == null)
				return false;
			return CheckCorLibType(tdrs.TypeDefOrRef, name);
		}

		static bool CheckCorLibType(ITypeDefOrRef tdr, string name) {
			if (tdr == null)
				return false;
			if (!tdr.DefinitionAssembly.IsCorLib())
				return false;
			if (tdr is TypeSpec)
				return false;
			return tdr.TypeName == name && tdr.Namespace == "System";
		}

		static MethodSig GetMethodSig(ICustomAttributeType ctor) {
			return ctor == null ? null : ctor.MethodSig;
		}

		void WriteUTF8String(UTF8String s) {
			if ((object)s == null || s.Data == null)
				writer.Write((byte)0xFF);
			else {
				writer.WriteCompressedUInt32((uint)s.Data.Length);
				writer.Write(s.Data);
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!disposeStream)
				return;
			if (outStream != null)
				outStream.Dispose();
			if (writer != null)
				((IDisposable)writer).Dispose();
		}
	}
}
