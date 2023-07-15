// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Base class of all user data
	/// </summary>
	public abstract class UserResourceData : IResourceData {
		readonly UserResourceType type;

		/// <summary>
		/// Full name including assembly of type
		/// </summary>
		public string TypeName => type.Name;

		/// <summary>
		/// User type code
		/// </summary>
		public ResourceTypeCode Code => type.Code;

		/// <inheritdoc cref="IResourceData.StartOffset" />
		public FileOffset StartOffset { get; set; }

		/// <inheritdoc cref="IFileSection.EndOffset" />
		public FileOffset EndOffset { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">User resource type</param>
		public UserResourceData(UserResourceType type) => this.type = type;

		/// <inheritdoc/>
		public abstract void WriteData(ResourceBinaryWriter writer, IFormatter formatter);
	}

	/// <summary>
	/// Binary data
	/// </summary>
	public sealed class BinaryResourceData : UserResourceData {
		byte[] data;
		SerializationFormat format;

		/// <summary>
		/// Gets the raw data
		/// </summary>
		public byte[] Data => data;

		/// <summary>
		/// Gets the serialization format of <see cref="Data"/>
		/// </summary>
		public SerializationFormat Format => format;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">User resource type</param>
		/// <param name="data">Raw serialized data</param>
		/// <param name="format"></param>
		public BinaryResourceData(UserResourceType type, byte[] data, SerializationFormat format)
			: base(type) {
			this.data = data;
			this.format = format;
		}

		/// <inheritdoc/>
		public override void WriteData(ResourceBinaryWriter writer, IFormatter formatter) {
			if (writer.ReaderType == ResourceReaderType.ResourceReader && format != SerializationFormat.BinaryFormatter)
				throw new NotSupportedException($"Unsupported serialization format: {format} for {writer.ReaderType}");

			if (writer.ReaderType == ResourceReaderType.DeserializingResourceReader) {
				writer.Write7BitEncodedInt((int)format);
				writer.Write7BitEncodedInt(data.Length);
			}
			writer.Write(data);
		}

		/// <inheritdoc/>
		public override string ToString() => $"Binary: Length: {data.Length} Format: {format}";
	}

	/// <summary>
	/// Specifies how the data in <see cref="BinaryResourceData"/> should be deserialized.
	/// </summary>
	public enum SerializationFormat {
		/// <summary>
		///	The data can be deserialized using <see cref="BinaryFormatter"/>.
		/// </summary>
		BinaryFormatter = 1,

		/// <summary>
		/// The data can be deserialized by passing in the raw data into
		/// the <see cref="System.ComponentModel.TypeConverter.ConvertFrom(object)"/> method.
		/// </summary>
		TypeConverterByteArray = 2,

		/// <summary>
		/// The data can be deserialized by passing the UTF-8 string obtained from the raw data into
		/// the <see cref="System.ComponentModel.TypeConverter.ConvertFromInvariantString(string)"/> method.
		/// </summary>
		TypeConverterString = 3,

		/// <summary>
		/// The data can be deserialized by creating a new instance of the type using a
		/// constructor with a single <see cref="Stream"/> parameter and passing in
		/// a <see cref="MemoryStream"/> of the raw data into it.
		/// </summary>
		ActivatorStream = 4,
	}
}
