using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Searches for a type according to custom attribute search rules: first try the
	/// current assembly, and if that fails, try mscorlib
	/// </summary>
	class CAAssemblyRefFinder : IAssemblyRefFinder {
		ModuleDef module;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module to search first</param>
		public CAAssemblyRefFinder(ModuleDef module) {
			this.module = module;
		}

		/// <inheritdoc/>
		public AssemblyRef FindAssemblyRef(TypeRef nonNestedTypeRef) {
			var asm = module.Assembly;
			if (asm == null)
				return null;
			var type = asm.Find(nonNestedTypeRef);
			if (type != null)
				return module.UpdateRowId(new AssemblyRefUser(asm));

			// Assume it's in corlib without actually verifying it
			return module.CorLibTypes.AssemblyRef;
		}
	}

	[Serializable]
	class CABlobParsingException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public CABlobParsingException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public CABlobParsingException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Other exception</param>
		public CABlobParsingException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Reads custom attributes from the #Blob stream
	/// </summary>
	public struct CustomAttributeReader : IDisposable {
		ModuleDef ownerModule;
		IImageStream reader;
		ICustomAttributeType ctor;
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
		public static CustomAttribute Read(ModuleDefMD readerModule, ICustomAttributeType ctor, uint offset) {
			if (ctor == null)
				return CreateEmpty(ctor);
			using (var reader = new CustomAttributeReader(readerModule, ctor, offset)) {
				try {
					return reader.Read();
				}
				catch (CABlobParsingException) {
					return new CustomAttribute(ctor, reader.GetRawBlob());
				}
				catch (IOException) {
					return new CustomAttribute(ctor, reader.GetRawBlob());
				}
			}
		}

		/// <summary>
		/// Reads a custom attribute
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="stream">A stream positioned at the the first byte of the CA blob</param>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance or <c>null</c> if one of the
		/// args is <c>null</c> or if we failed to parse the CA blob</returns>
		public static CustomAttribute Read(ModuleDef ownerModule, IImageStream stream, ICustomAttributeType ctor) {
			if (stream == null || ctor == null)
				return null;
			try {
				using (var reader = new CustomAttributeReader(ownerModule, stream, ctor))
					return reader.Read();
			}
			catch (CABlobParsingException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
		}

		static CustomAttribute CreateEmpty(ICustomAttributeType ctor) {
			return new CustomAttribute(ctor, new byte[0]);
		}

		CustomAttributeReader(ModuleDefMD readerModule, ICustomAttributeType ctor, uint offset) {
			this.ownerModule = readerModule;
			this.reader = readerModule.BlobStream.CreateStream(offset);
			this.ctor = ctor;
			this.genericArguments = null;
			this.recursionCounter = new RecursionCounter();
			this.verifyReadAllBytes = false;
		}

		CustomAttributeReader(ModuleDef ownerModule, IImageStream reader, ICustomAttributeType ctor) {
			this.ownerModule = ownerModule;
			this.reader = reader;
			this.ctor = ctor;
			this.genericArguments = null;
			this.recursionCounter = new RecursionCounter();
			this.verifyReadAllBytes = false;
		}

		byte[] GetRawBlob() {
			reader.Position = 0;
			return reader.ReadBytes((int)reader.Length);
		}

		CustomAttribute Read() {
			if (reader.ReadUInt16() != 1)
				throw new CABlobParsingException("Invalid CA blob prolog");

			var methodSig = ctor == null ? null : ctor.Signature as MethodSig;
			if (methodSig == null)
				throw new CABlobParsingException("ctor is null or not a method");

			var mrCtor = ctor as MemberRef;
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

			var ctorArgs = new List<CAArgument>(methodSig.Params.Count);
			for (int i = 0; i < methodSig.Params.Count; i++)
				ctorArgs.Add(ReadFixedArg(FixTypeSig(methodSig.Params[i])));

			int numNamedArgs = reader.ReadUInt16();
			var namedArgs = new List<CANamedArgument>(numNamedArgs);
			for (int i = 0; i < numNamedArgs; i++)
				namedArgs.Add(ReadNamedArgument());

			// verifyReadAllBytes will be set when we guess the underlying type of an enum.
			// To make sure we guessed right, verify that we read all bytes.
			if (verifyReadAllBytes && reader.Position != reader.Length)
				throw new CABlobParsingException("Not all CA blob bytes were read");

			return new CustomAttribute(ctor, ctorArgs, namedArgs);
		}

		TypeSig FixTypeSig(TypeSig type) {
			return TypeSig.RemoveModifiers(SubstituteGenericParameter(TypeSig.RemoveModifiers(type)));
		}

		TypeSig SubstituteGenericParameter(TypeSig type) {
			if (genericArguments == null)
				return type;
			return genericArguments.Resolve(type);
		}

		CAArgument ReadFixedArg(TypeSig argType) {
			if (!recursionCounter.Increment())
				throw new CABlobParsingException("Too much recursion");
			if (argType == null)
				throw new CABlobParsingException("null argType");
			CAArgument result;

			var arrayType = argType as SZArraySig;
			if (arrayType != null)
				result = ReadArrayArgument(arrayType);
			else
				result = ReadElem(argType);

			recursionCounter.Decrement();
			return result;
		}

		CAArgument ReadElem(TypeSig argType) {
			if (argType == null)
				throw new CABlobParsingException("null argType");
			TypeSig realArgType;
			var value = ReadValue((SerializationType)argType.ElementType, argType, out realArgType);
			if (realArgType == null)
				throw new CABlobParsingException("Invalid arg type");

			// One example when this is true is when prop/field type is object and
			// value type is string[]
			var argument = value as CAArgument;
			if (argument != null)
				return argument;

			return new CAArgument(realArgType, value);
		}

		object ReadValue(SerializationType etype, TypeSig argType, out TypeSig realArgType) {
			if (!recursionCounter.Increment())
				throw new CABlobParsingException("Too much recursion");

			object result;
			switch (etype) {
			case SerializationType.Boolean:
				realArgType = ownerModule.CorLibTypes.Boolean;
				result = reader.ReadByte() != 0;
				break;

			case SerializationType.Char:
				realArgType = ownerModule.CorLibTypes.Char;
				result = reader.ReadChar();
				break;

			case SerializationType.I1:
				realArgType = ownerModule.CorLibTypes.SByte;
				result = reader.ReadSByte();
				break;

			case SerializationType.U1:
				realArgType = ownerModule.CorLibTypes.Byte;
				result = reader.ReadByte();
				break;

			case SerializationType.I2:
				realArgType = ownerModule.CorLibTypes.Int16;
				result = reader.ReadInt16();
				break;

			case SerializationType.U2:
				realArgType = ownerModule.CorLibTypes.UInt16;
				result = reader.ReadUInt16();
				break;

			case SerializationType.I4:
				realArgType = ownerModule.CorLibTypes.Int32;
				result = reader.ReadInt32();
				break;

			case SerializationType.U4:
				realArgType = ownerModule.CorLibTypes.UInt32;
				result = reader.ReadUInt32();
				break;

			case SerializationType.I8:
				realArgType = ownerModule.CorLibTypes.Int64;
				result = reader.ReadInt64();
				break;

			case SerializationType.U8:
				realArgType = ownerModule.CorLibTypes.UInt64;
				result = reader.ReadUInt64();
				break;

			case SerializationType.R4:
				realArgType = ownerModule.CorLibTypes.Single;
				result = reader.ReadSingle();
				break;

			case SerializationType.R8:
				realArgType = ownerModule.CorLibTypes.Double;
				result = reader.ReadDouble();
				break;

			case SerializationType.String:
				realArgType = ownerModule.CorLibTypes.String;
				result = ReadUTF8String();
				break;

			// It's ET.ValueType if it's eg. a ctor enum arg type
			case (SerializationType)ElementType.ValueType:
				if (argType == null)
					throw new CABlobParsingException("Invalid element type");
				realArgType = argType;
				result = ReadEnumValue(GetEnumUnderlyingType(argType));
				break;

			// It's ET.Object if it's a ctor object arg type
			case (SerializationType)ElementType.Object:
			case SerializationType.TaggedObject:
				realArgType = ReadFieldOrPropType();
				var arraySig = realArgType as SZArraySig;
				if (arraySig != null)
					result = ReadArrayArgument(arraySig);
				else {
					TypeSig tmpType;
					result = ReadValue((SerializationType)realArgType.ElementType, realArgType, out tmpType);
				}
				break;

			// It's ET.Class if it's eg. a ctor System.Type arg type
			case (SerializationType)ElementType.Class:
				var tdr = argType as TypeDefOrRefSig;
				if (tdr != null && tdr.DefinitionAssembly.IsCorLib() && tdr.Namespace == "System") {
					if (tdr.Name == "Type") {
						result = ReadValue(SerializationType.Type, tdr, out realArgType);
						break;
					}
					if (tdr.Name == "String") {
						result = ReadValue(SerializationType.String, tdr, out realArgType);
						break;
					}
					if (tdr.Name == "Object") {
						result = ReadValue(SerializationType.TaggedObject, tdr, out realArgType);
						break;
					}
				}
				throw new CABlobParsingException("Invalid element type");

			case SerializationType.Type:
				realArgType = argType;
				result = ReadType();
				break;

			case SerializationType.Enum:
				realArgType = ReadType();
				result = ReadEnumValue(GetEnumUnderlyingType(realArgType));
				break;

			default:
				throw new CABlobParsingException("Invalid element type");
			}

			recursionCounter.Decrement();
			return result;
		}

		object ReadEnumValue(TypeSig underlyingType) {
			if (underlyingType != null) {
				if (underlyingType.ElementType < ElementType.Boolean || underlyingType.ElementType > ElementType.U8)
					throw new CABlobParsingException("Invalid enum underlying type");
				TypeSig realArgType;
				return ReadValue((SerializationType)underlyingType.ElementType, underlyingType, out realArgType);
			}

			// We couldn't resolve the type ref. It should be an enum, but we don't know for sure.
			// Most enums use Int32 as the underlying type. Assume that's true also in this case.
			// Since we're guessing, verify that we've read all CA blob bytes. If we haven't, then
			// we probably guessed wrong.
			verifyReadAllBytes = true;
			return reader.ReadInt32();
		}

		TypeSig ReadType() {
			var name = ReadUTF8String();
			var asmRefFinder = new CAAssemblyRefFinder(ownerModule);
			var type = TypeNameParser.ParseAsTypeSigReflectionNoThrow(ownerModule, UTF8String.ToSystemStringOrEmpty(name), asmRefFinder);
			if (type == null)
				throw new CABlobParsingException("Could not parse type");
			return type;
		}

		/// <summary>
		/// Gets the enum's underlying type
		/// </summary>
		/// <param name="type">An enum type</param>
		/// <returns>The underlying type or <c>null</c> if we couldn't resolve the type ref</returns>
		/// <exception cref="CABlobParsingException">If <paramref name="type"/> is not an enum or <c>null</c></exception>
		static TypeSig GetEnumUnderlyingType(TypeSig type) {
			if (type == null)
				throw new CABlobParsingException("null enum type");
			var td = GetTypeDef(type);
			if (td == null)
				return null;
			if (!td.IsEnum)
				throw new CABlobParsingException("Not an enum");
			return TypeSig.RemoveModifiers(td.GetEnumUnderlyingType());
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

		CAArgument ReadArrayArgument(SZArraySig arrayType) {
			if (!recursionCounter.Increment())
				throw new CABlobParsingException("Too much recursion");
			var arg = new CAArgument(arrayType);

			int arrayCount = reader.ReadInt32();
			if (arrayCount == -1) {	// -1 if it's null
			}
			else if (arrayCount < 0)
				throw new CABlobParsingException("Array is too big");
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
			case SerializationType.Property: isField = false; break;
			case SerializationType.Field: isField = true; break;
			default: throw new CABlobParsingException("Named argument is not a field/property");
			}

			TypeSig fieldPropType = ReadFieldOrPropType();
			var name = ReadUTF8String();
			var argument = ReadFixedArg(fieldPropType);

			return new CANamedArgument(isField, fieldPropType, name, argument);
		}

		TypeSig ReadFieldOrPropType() {
			if (!recursionCounter.Increment())
				throw new CABlobParsingException("Too much recursion");
			TypeSig result;
			switch ((SerializationType)reader.ReadByte()) {
			case SerializationType.Boolean: result = ownerModule.CorLibTypes.Boolean; break;
			case SerializationType.Char:	result = ownerModule.CorLibTypes.Char; break;
			case SerializationType.I1:		result = ownerModule.CorLibTypes.SByte; break;
			case SerializationType.U1:		result = ownerModule.CorLibTypes.Byte; break;
			case SerializationType.I2:		result = ownerModule.CorLibTypes.Int16; break;
			case SerializationType.U2:		result = ownerModule.CorLibTypes.UInt16; break;
			case SerializationType.I4:		result = ownerModule.CorLibTypes.Int32; break;
			case SerializationType.U4:		result = ownerModule.CorLibTypes.UInt32; break;
			case SerializationType.I8:		result = ownerModule.CorLibTypes.Int64; break;
			case SerializationType.U8:		result = ownerModule.CorLibTypes.UInt64; break;
			case SerializationType.R4:		result = ownerModule.CorLibTypes.Single; break;
			case SerializationType.R8:		result = ownerModule.CorLibTypes.Double; break;
			case SerializationType.String:	result = ownerModule.CorLibTypes.String; break;
			case SerializationType.SZArray: result = new SZArraySig(ReadFieldOrPropType()); break;
			case SerializationType.Type:	result = new ClassSig(ownerModule.UpdateRowId(new TypeRefUser(ownerModule, "System", "Type", ownerModule.CorLibTypes.AssemblyRef))); break;
			case SerializationType.TaggedObject: result = ownerModule.CorLibTypes.Object; break;
			case SerializationType.Enum:	result = ReadType(); break;
			default: throw new CABlobParsingException("Invalid type");
			}
			recursionCounter.Decrement();
			return result;
		}

		UTF8String ReadUTF8String() {
			if (reader.ReadByte() == 0xFF)
				return null;
			reader.Position--;
			uint len;
			if (!reader.ReadCompressedUInt32(out len))
				throw new CABlobParsingException("Could not read compressed UInt32");
			if (len > (uint)int.MaxValue)
				throw new CABlobParsingException("Too long string");
			if (len == 0)
				return UTF8String.Empty;
			return new UTF8String(reader.ReadBytes((int)len));
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (reader != null)
				reader.Dispose();
		}
	}
}
