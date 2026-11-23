// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes field marshal blobs
	/// </summary>
	public readonly struct MarshalBlobWriter : IDisposable, IFullNameFactoryHelper {
		readonly ModuleDef module;
		readonly MemoryStream outStream;
		readonly DataWriter writer;
		readonly IWriterError helper;
		readonly bool optimizeCustomAttributeSerializedTypeNames;

		/// <summary>
		/// Creates a field marshal blob from <paramref name="marshalType"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="marshalType">Marshal type</param>
		/// <param name="helper">Helps this class</param>
		/// <returns>A field marshal blob or <c>null</c> if <paramref name="marshalType"/> is
		/// <c>null</c></returns>
		[return: NotNullIfNotNull(nameof(marshalType))]
		public static byte[]? Write(ModuleDef module, MarshalType? marshalType, IWriterError helper) =>
			Write(module, marshalType, helper, false);

		/// <summary>
		/// Creates a field marshal blob from <paramref name="marshalType"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="marshalType">Marshal type</param>
		/// <param name="helper">Helps this class</param>
		/// <param name="optimizeCustomAttributeSerializedTypeNames">Optimize serialized type strings in custom attributes.
		/// For more info, see <see cref="MetadataFlags.OptimizeCustomAttributeSerializedTypeNames"/></param>
		/// <returns>A field marshal blob or <c>null</c> if <paramref name="marshalType"/> is
		/// <c>null</c></returns>
		[return: NotNullIfNotNull(nameof(marshalType))]
		public static byte[]? Write(ModuleDef module, MarshalType? marshalType, IWriterError helper, bool optimizeCustomAttributeSerializedTypeNames) {
			using (var writer = new MarshalBlobWriter(module, helper, optimizeCustomAttributeSerializedTypeNames))
				return writer.Write(marshalType);
		}

		MarshalBlobWriter(ModuleDef module, IWriterError helper, bool optimizeCustomAttributeSerializedTypeNames) {
			this.module = module;
			outStream = new MemoryStream();
			writer = new DataWriter(outStream);
			this.helper = helper;
			this.optimizeCustomAttributeSerializedTypeNames = optimizeCustomAttributeSerializedTypeNames;
		}

		[return: NotNullIfNotNull(nameof(marshalType))]
		byte[]? Write(MarshalType? marshalType) {
			if (marshalType is null)
				return null;

			var type = marshalType.NativeType;
			if (type != NativeType.RawBlob) {
				if ((uint)type > byte.MaxValue)
					helper.Error("Invalid MarshalType.NativeType");
				writer.WriteByte((byte)type);
			}
			bool canWrite = true;
			switch (type) {
			case NativeType.FixedSysString:
				// All fields are optional: if it's a MarshalType, only the native type is written.
				// The same is true for the other native types that have their own class.
				if (marshalType is not FixedSysStringMarshalType fixedSysString)
					break;
				if (fixedSysString.IsSizeValid)
					WriteCompressedUInt32((uint)fixedSysString.Size);
				break;

			case NativeType.SafeArray:
				if (marshalType is not SafeArrayMarshalType safeArray)
					break;
				if (UpdateCanWrite(safeArray.IsVariantTypeValid, "VariantType", ref canWrite))
					WriteCompressedUInt32((uint)safeArray.VariantType);
				if (UpdateCanWrite(safeArray.IsUserDefinedSubTypeValid, "UserDefinedSubType", ref canWrite)) {
					var subTypeName = FullNameFactory.AssemblyQualifiedName(safeArray.UserDefinedSubType!, null, null, out bool hasError);
					if (hasError)
						helper.Error("Marshal type: UserDefinedSubType has a null value or is too deeply nested");
					Write(WriterUtils.ToUTF8StringKeepUnpairedSurrogates(subTypeName));
				}
				break;

			case NativeType.FixedArray:
				if (marshalType is not FixedArrayMarshalType fixedArray)
					break;
				if (UpdateCanWrite(fixedArray.IsSizeValid, "Size", ref canWrite))
					WriteCompressedUInt32((uint)fixedArray.Size);
				if (UpdateCanWrite(fixedArray.IsElementTypeValid, "ElementType", ref canWrite))
					WriteCompressedUInt32((uint)fixedArray.ElementType);
				break;

			case NativeType.Array:
				if (marshalType is not ArrayMarshalType array)
					break;
				if (UpdateCanWrite(array.IsElementTypeValid, "ElementType", ref canWrite))
					WriteCompressedUInt32((uint)array.ElementType);
				if (UpdateCanWrite(array.IsParamNumberValid, "ParamNumber", ref canWrite))
					WriteCompressedUInt32((uint)array.ParamNumber);
				if (UpdateCanWrite(array.IsSizeValid, "Size", ref canWrite))
					WriteCompressedUInt32((uint)array.Size);
				if (UpdateCanWrite(array.IsFlagsValid, "Flags", ref canWrite))
					WriteCompressedUInt32((uint)array.Flags);
				break;

			case NativeType.CustomMarshaler:
				if (marshalType is not CustomMarshalType custMarshaler) {
					helper.Error("Marshal type: a custom marshaler must be a CustomMarshalType");
					break;
				}
				Write(custMarshaler.Guid);
				Write(custMarshaler.NativeTypeName);
				var cm = custMarshaler.CustomMarshaler;
				UTF8String? cmName;
				if (cm is null)
					cmName = custMarshaler.CustomMarshalerTypeName;
				else {
					var cmFullName = FullNameFactory.AssemblyQualifiedName(cm, this, null, out bool hasError);
					if (hasError)
						helper.Error("Marshal type: Custom marshaler type has a null value or is too deeply nested");
					cmName = WriterUtils.ToUTF8StringKeepUnpairedSurrogates(cmFullName);
				}
				Write(cmName);
				Write(custMarshaler.Cookie);
				break;

			case NativeType.IUnknown:
			case NativeType.IDispatch:
			case NativeType.IntF:
				if (marshalType is not InterfaceMarshalType iface)
					break;
				if (iface.IsIidParamIndexValid)
					WriteCompressedUInt32((uint)iface.IidParamIndex);
				break;

			case NativeType.RawBlob:
				if (marshalType is not RawMarshalType rawMarshalType) {
					helper.Error("Marshal type: a raw blob must be a RawMarshalType");
					break;
				}
				var data = rawMarshalType.Data;
				if (data is not null)
					writer.WriteBytes(data);
				break;

			default:
				break;
			}

			return outStream.ToArray();
		}

		bool UpdateCanWrite(bool isValid, string field, ref bool canWriteMore) {
			if (!canWriteMore) {
				if (isValid)
					helper.Error2("MarshalType field {0} is valid even though a previous field was invalid.", field);
				return canWriteMore;
			}

			if (!isValid)
				canWriteMore = false;

			return canWriteMore;
		}

		uint WriteCompressedUInt32(uint value) => writer.WriteCompressedUInt32(helper, value);

		void Write(UTF8String? s) => writer.Write(helper, s);

		/// <inheritdoc/>
		public void Dispose() => outStream?.Dispose();

		bool IFullNameFactoryHelper.MustUseAssemblyName(IType? type) =>
			FullNameFactory.MustUseAssemblyName(module, type, optimizeCustomAttributeSerializedTypeNames);
	}
}
