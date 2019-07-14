// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// Searches for a type according to custom attribute search rules: first try the
	/// current assembly, and if that fails, try mscorlib
	/// </summary>
	sealed class CAAssemblyRefFinder : IAssemblyRefFinder {
		readonly ModuleDef module;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module to search first</param>
		public CAAssemblyRefFinder(ModuleDef module) => this.module = module;

		/// <inheritdoc/>
		public AssemblyRef FindAssemblyRef(TypeRef nonNestedTypeRef) {
			var modAsm = module.Assembly;
			if (!(modAsm is null)) {
				var type = modAsm.Find(nonNestedTypeRef);
				if (!(type is null))
					return module.UpdateRowId(new AssemblyRefUser(modAsm));
			}
			else if (!(module.Find(nonNestedTypeRef) is null))
				return AssemblyRef.CurrentAssembly;

			var corLibAsm = module.Context.AssemblyResolver.Resolve(module.CorLibTypes.AssemblyRef, module);
			if (!(corLibAsm is null)) {
				var type = corLibAsm.Find(nonNestedTypeRef);
				if (!(type is null))
					return module.CorLibTypes.AssemblyRef;
			}

			if (!(modAsm is null))
				return module.UpdateRowId(new AssemblyRefUser(modAsm));
			return AssemblyRef.CurrentAssembly;
		}
	}

	/// <summary>
	/// Thrown by CustomAttributeReader when it fails to parse a custom attribute blob
	/// </summary>
	[Serializable]
	public class CABlobParserException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public CABlobParserException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public CABlobParserException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Other exception</param>
		public CABlobParserException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected CABlobParserException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Reads custom attributes from the #Blob stream
	/// </summary>
	public struct CustomAttributeReader {
		readonly ModuleDef module;
		DataReader reader;
		readonly uint caBlobOffset;
		readonly GenericParamContext gpContext;
		GenericArguments genericArguments;
		RecursionCounter recursionCounter;
		bool verifyReadAllBytes;

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="offset">Offset of custom attribute in the #Blob stream</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDefMD readerModule, ICustomAttributeType ctor, uint offset) => Read(readerModule, ctor, offset, new GenericParamContext());

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="offset">Offset of custom attribute in the #Blob stream</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDefMD readerModule, ICustomAttributeType ctor, uint offset, GenericParamContext gpContext) {
			var caReader = new CustomAttributeReader(readerModule, offset, gpContext);
			try {
				if (ctor is null)
					return caReader.CreateRaw(ctor);
				return caReader.Read(ctor);
			}
			catch (CABlobParserException) {
				return caReader.CreateRaw(ctor);
			}
			catch (IOException) {
				return caReader.CreateRaw(ctor);
			}
		}

		CustomAttribute CreateRaw(ICustomAttributeType ctor) => new CustomAttribute(ctor, GetRawBlob());

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="caBlob">CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDef module, byte[] caBlob, ICustomAttributeType ctor) =>
			Read(module, ByteArrayDataReaderFactory.CreateReader(caBlob), ctor, new GenericParamContext());

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">A reader positioned at the the first byte of the CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDef module, DataReader reader, ICustomAttributeType ctor) =>
			Read(module, ref reader, ctor, new GenericParamContext());

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="caBlob">CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDef module, byte[] caBlob, ICustomAttributeType ctor, GenericParamContext gpContext) =>
			Read(module, ByteArrayDataReaderFactory.CreateReader(caBlob), ctor, gpContext);

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">A stream positioned at the the first byte of the CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		public static CustomAttribute Read(ModuleDef module, DataReader reader, ICustomAttributeType ctor, GenericParamContext gpContext) =>
			Read(module, ref reader, ctor, gpContext);

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">A stream positioned at the the first byte of the CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance</returns>
		static CustomAttribute Read(ModuleDef module, ref DataReader reader, ICustomAttributeType ctor, GenericParamContext gpContext) {
			var caReader = new CustomAttributeReader(module, ref reader, gpContext);
			CustomAttribute ca;
			try {
				if (ctor is null)
					ca = caReader.CreateRaw(ctor);
				else
					ca = caReader.Read(ctor);
			}
			catch (CABlobParserException) {
				ca = caReader.CreateRaw(ctor);
			}
			catch (IOException) {
				ca = caReader.CreateRaw(ctor);
			}
			return ca;
		}

		/// <summary>
		/// Reads custom attribute named arguments
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">A reader positioned at the the first byte of the CA blob</param>
		/// <param name="numNamedArgs">Number of named arguments to read from <paramref name="reader"/></param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A list of <see cref="CANamedArgument"/>s or <c>null</c> if some error
		/// occurred.</returns>
		internal static List<CANamedArgument> ReadNamedArguments(ModuleDef module, ref DataReader reader, int numNamedArgs, GenericParamContext gpContext) {
			try {
				var caReader = new CustomAttributeReader(module, ref reader, gpContext);
				var namedArgs = caReader.ReadNamedArguments(numNamedArgs);
				reader.CurrentOffset = caReader.reader.CurrentOffset;
				return namedArgs;
			}
			catch (CABlobParserException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
		}

		CustomAttributeReader(ModuleDefMD readerModule, uint offset, GenericParamContext gpContext) {
			module = readerModule;
			caBlobOffset = offset;
			reader = readerModule.BlobStream.CreateReader(offset);
			genericArguments = null;
			recursionCounter = new RecursionCounter();
			verifyReadAllBytes = false;
			this.gpContext = gpContext;
		}

		CustomAttributeReader(ModuleDef module, ref DataReader reader, GenericParamContext gpContext) {
			this.module = module;
			caBlobOffset = 0;
			this.reader = reader;
			genericArguments = null;
			recursionCounter = new RecursionCounter();
			verifyReadAllBytes = false;
			this.gpContext = gpContext;
		}

		byte[] GetRawBlob() => reader.ToArray();

		CustomAttribute Read(ICustomAttributeType ctor) {
			var methodSig = ctor?.MethodSig;
			if (methodSig is null)
				throw new CABlobParserException("ctor is null or not a method");

			if (ctor is MemberRef mrCtor && mrCtor.Class is TypeSpec owner && owner.TypeSig is GenericInstSig gis) {
				genericArguments = new GenericArguments();
				genericArguments.PushTypeArgs(gis.GenericArguments);
			}

			var methodSigParams = methodSig.Params;
			bool isEmpty = methodSigParams.Count == 0 && reader.Position == reader.Length;
			if (!isEmpty && reader.ReadUInt16() != 1)
				throw new CABlobParserException("Invalid CA blob prolog");

			var ctorArgs = new List<CAArgument>(methodSigParams.Count);
			int count = methodSigParams.Count;
			for (int i = 0; i < count; i++)
				ctorArgs.Add(ReadFixedArg(FixTypeSig(methodSigParams[i])));

			// Some tools don't write the next ushort if there are no named arguments.
			int numNamedArgs = reader.Position == reader.Length ? 0 : reader.ReadUInt16();
			var namedArgs = ReadNamedArguments(numNamedArgs);

			// verifyReadAllBytes will be set when we guess the underlying type of an enum.
			// To make sure we guessed right, verify that we read all bytes.
			if (verifyReadAllBytes && reader.Position != reader.Length)
				throw new CABlobParserException("Not all CA blob bytes were read");

			return new CustomAttribute(ctor, ctorArgs, namedArgs, caBlobOffset);
		}

		List<CANamedArgument> ReadNamedArguments(int numNamedArgs) {
			var namedArgs = new List<CANamedArgument>(numNamedArgs);
			for (int i = 0; i < numNamedArgs; i++) {
				if (reader.Position == reader.Length)
					break;
				namedArgs.Add(ReadNamedArgument());
			}
			return namedArgs;
		}

		TypeSig FixTypeSig(TypeSig type) => SubstituteGenericParameter(type.RemoveModifiers()).RemoveModifiers();

		TypeSig SubstituteGenericParameter(TypeSig type) {
			if (genericArguments is null)
				return type;
			return genericArguments.Resolve(type);
		}

		CAArgument ReadFixedArg(TypeSig argType) {
			if (!recursionCounter.Increment())
				throw new CABlobParserException("Too much recursion");
			if (argType is null)
				throw new CABlobParserException("null argType");
			CAArgument result;

			if (argType is SZArraySig arrayType)
				result = ReadArrayArgument(arrayType);
			else
				result = ReadElem(argType);

			recursionCounter.Decrement();
			return result;
		}

		CAArgument ReadElem(TypeSig argType) {
			if (argType is null)
				throw new CABlobParserException("null argType");
			var value = ReadValue((SerializationType)argType.ElementType, argType, out var realArgType);
			if (realArgType is null)
				throw new CABlobParserException("Invalid arg type");

			// One example when this is true is when prop/field type is object and
			// value type is string[]
			if (value is CAArgument)
				return (CAArgument)value;

			return new CAArgument(realArgType, value);
		}

		object ReadValue(SerializationType etype, TypeSig argType, out TypeSig realArgType) {
			if (!recursionCounter.Increment())
				throw new CABlobParserException("Too much recursion");

			object result;
			switch (etype) {
			case SerializationType.Boolean:
				realArgType = module.CorLibTypes.Boolean;
				result = reader.ReadByte() != 0;
				break;

			case SerializationType.Char:
				realArgType = module.CorLibTypes.Char;
				result = reader.ReadChar();
				break;

			case SerializationType.I1:
				realArgType = module.CorLibTypes.SByte;
				result = reader.ReadSByte();
				break;

			case SerializationType.U1:
				realArgType = module.CorLibTypes.Byte;
				result = reader.ReadByte();
				break;

			case SerializationType.I2:
				realArgType = module.CorLibTypes.Int16;
				result = reader.ReadInt16();
				break;

			case SerializationType.U2:
				realArgType = module.CorLibTypes.UInt16;
				result = reader.ReadUInt16();
				break;

			case SerializationType.I4:
				realArgType = module.CorLibTypes.Int32;
				result = reader.ReadInt32();
				break;

			case SerializationType.U4:
				realArgType = module.CorLibTypes.UInt32;
				result = reader.ReadUInt32();
				break;

			case SerializationType.I8:
				realArgType = module.CorLibTypes.Int64;
				result = reader.ReadInt64();
				break;

			case SerializationType.U8:
				realArgType = module.CorLibTypes.UInt64;
				result = reader.ReadUInt64();
				break;

			case SerializationType.R4:
				realArgType = module.CorLibTypes.Single;
				result = reader.ReadSingle();
				break;

			case SerializationType.R8:
				realArgType = module.CorLibTypes.Double;
				result = reader.ReadDouble();
				break;

			case SerializationType.String:
				realArgType = module.CorLibTypes.String;
				result = ReadUTF8String();
				break;

			// It's ET.ValueType if it's eg. a ctor enum arg type
			case (SerializationType)ElementType.ValueType:
				if (argType is null)
					throw new CABlobParserException("Invalid element type");
				realArgType = argType;
				result = ReadEnumValue(GetEnumUnderlyingType(argType));
				break;

			// It's ET.Object if it's a ctor object arg type
			case (SerializationType)ElementType.Object:
			case SerializationType.TaggedObject:
				realArgType = ReadFieldOrPropType();
				var arraySig = realArgType as SZArraySig;
				if (!(arraySig is null))
					result = ReadArrayArgument(arraySig);
				else
					result = ReadValue((SerializationType)realArgType.ElementType, realArgType, out var tmpType);
				break;

			// It's ET.Class if it's eg. a ctor System.Type arg type
			case (SerializationType)ElementType.Class:
				var tdr = argType as TypeDefOrRefSig;
				if (!(tdr is null) && tdr.DefinitionAssembly.IsCorLib() && tdr.Namespace == "System") {
					if (tdr.TypeName == "Type") {
						result = ReadValue(SerializationType.Type, tdr, out realArgType);
						break;
					}
					if (tdr.TypeName == "String") {
						result = ReadValue(SerializationType.String, tdr, out realArgType);
						break;
					}
					if (tdr.TypeName == "Object") {
						result = ReadValue(SerializationType.TaggedObject, tdr, out realArgType);
						break;
					}
				}

				// Assume it's an enum that couldn't be resolved
				realArgType = argType;
				result = ReadEnumValue(null);
				break;

			case SerializationType.Type:
				realArgType = argType;
				result = ReadType(true);
				break;

			case SerializationType.Enum:
				realArgType = ReadType(false);
				result = ReadEnumValue(GetEnumUnderlyingType(realArgType));
				break;

			default:
				throw new CABlobParserException("Invalid element type");
			}

			recursionCounter.Decrement();
			return result;
		}

		object ReadEnumValue(TypeSig underlyingType) {
			if (!(underlyingType is null)) {
				if (underlyingType.ElementType < ElementType.Boolean || underlyingType.ElementType > ElementType.U8)
					throw new CABlobParserException("Invalid enum underlying type");
				return ReadValue((SerializationType)underlyingType.ElementType, underlyingType, out var realArgType);
			}

			// We couldn't resolve the type ref. It should be an enum, but we don't know for sure.
			// Most enums use Int32 as the underlying type. Assume that's true also in this case.
			// Since we're guessing, verify that we've read all CA blob bytes. If we haven't, then
			// we probably guessed wrong.
			verifyReadAllBytes = true;
			return reader.ReadInt32();
		}

		TypeSig ReadType(bool canReturnNull) {
			var name = ReadUTF8String();
			if (canReturnNull && name is null)
				return null;
			var asmRefFinder = new CAAssemblyRefFinder(module);
			var type = TypeNameParser.ParseAsTypeSigReflection(module, UTF8String.ToSystemStringOrEmpty(name), asmRefFinder, gpContext);
			if (type is null)
				throw new CABlobParserException("Could not parse type");
			return type;
		}

		/// <summary>
		/// Gets the enum's underlying type
		/// </summary>
		/// <param name="type">An enum type</param>
		/// <returns>The underlying type or <c>null</c> if we couldn't resolve the type ref</returns>
		/// <exception cref="CABlobParserException">If <paramref name="type"/> is not an enum or <c>null</c></exception>
		static TypeSig GetEnumUnderlyingType(TypeSig type) {
			if (type is null)
				throw new CABlobParserException("null enum type");
			var td = GetTypeDef(type);
			if (td is null)
				return null;
			if (!td.IsEnum)
				throw new CABlobParserException("Not an enum");
			return td.GetEnumUnderlyingType().RemoveModifiers();
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

		CAArgument ReadArrayArgument(SZArraySig arrayType) {
			if (!recursionCounter.Increment())
				throw new CABlobParserException("Too much recursion");
			var arg = new CAArgument(arrayType);

			int arrayCount = reader.ReadInt32();
			if (arrayCount == -1) {	// -1 if it's null
			}
			else if (arrayCount < 0)
				throw new CABlobParserException("Array is too big");
			else {
				var array = new List<CAArgument>(arrayCount);
				arg.Value = array;
				for (int i = 0; i < arrayCount; i++)
					array.Add(ReadFixedArg(FixTypeSig(arrayType.Next)));
			}

			recursionCounter.Decrement();
			return arg;
		}

		CANamedArgument ReadNamedArgument() {
			bool isField;
			switch ((SerializationType)reader.ReadByte()) {
			case SerializationType.Property:isField = false; break;
			case SerializationType.Field:	isField = true; break;
			default: throw new CABlobParserException("Named argument is not a field/property");
			}

			var fieldPropType = ReadFieldOrPropType();
			var name = ReadUTF8String();
			var argument = ReadFixedArg(fieldPropType);

			return new CANamedArgument(isField, fieldPropType, name, argument);
		}

		TypeSig ReadFieldOrPropType() {
			if (!recursionCounter.Increment())
				throw new CABlobParserException("Too much recursion");
			TypeSig result;
			switch ((SerializationType)reader.ReadByte()) {
			case SerializationType.Boolean: result = module.CorLibTypes.Boolean; break;
			case SerializationType.Char:	result = module.CorLibTypes.Char; break;
			case SerializationType.I1:		result = module.CorLibTypes.SByte; break;
			case SerializationType.U1:		result = module.CorLibTypes.Byte; break;
			case SerializationType.I2:		result = module.CorLibTypes.Int16; break;
			case SerializationType.U2:		result = module.CorLibTypes.UInt16; break;
			case SerializationType.I4:		result = module.CorLibTypes.Int32; break;
			case SerializationType.U4:		result = module.CorLibTypes.UInt32; break;
			case SerializationType.I8:		result = module.CorLibTypes.Int64; break;
			case SerializationType.U8:		result = module.CorLibTypes.UInt64; break;
			case SerializationType.R4:		result = module.CorLibTypes.Single; break;
			case SerializationType.R8:		result = module.CorLibTypes.Double; break;
			case SerializationType.String:	result = module.CorLibTypes.String; break;
			case SerializationType.SZArray: result = new SZArraySig(ReadFieldOrPropType()); break;
			case SerializationType.Type:	result = new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type")); break;
			case SerializationType.TaggedObject: result = module.CorLibTypes.Object; break;
			case SerializationType.Enum:	result = ReadType(false); break;
			default: throw new CABlobParserException("Invalid type");
			}
			recursionCounter.Decrement();
			return result;
		}

		UTF8String ReadUTF8String() {
			if (reader.ReadByte() == 0xFF)
				return null;
			reader.Position--;
			if (!reader.TryReadCompressedUInt32(out uint len))
				throw new CABlobParserException("Could not read compressed UInt32");
			if (len == 0)
				return UTF8String.Empty;
			return new UTF8String(reader.ReadBytes((int)len));
		}
	}
}
