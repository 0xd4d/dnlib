// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Built-in resource data
	/// </summary>
	public sealed class BuiltInResourceData : IResourceData {
		readonly ResourceTypeCode code;
		readonly object? data;

		/// <summary>
		/// Gets the data
		/// </summary>
		public object? Data => data;

		/// <inheritdoc/>
		public ResourceTypeCode Code => code;

		/// <inheritdoc/>
		public FileOffset StartOffset { get; set; }

		/// <inheritdoc/>
		public FileOffset EndOffset { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Type of data</param>
		/// <param name="data">Data</param>
		public BuiltInResourceData(ResourceTypeCode code, object? data) {
			if (code != ResourceTypeCode.Null && data is null)
				throw new ArgumentNullException();
			this.code = code;
			this.data = data;
		}

		/// <inheritdoc/>
		public void WriteData(BinaryWriter writer, IFormatter formatter) {
			switch (code) {
			case ResourceTypeCode.Null:
				break;

			case ResourceTypeCode.String:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((string)data);
				break;

			case ResourceTypeCode.Boolean:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((bool)data);
				break;

			case ResourceTypeCode.Char:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((ushort)(char)data);
				break;

			case ResourceTypeCode.Byte:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((byte)data);
				break;

			case ResourceTypeCode.SByte:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((sbyte)data);
				break;

			case ResourceTypeCode.Int16:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((short)data);
				break;

			case ResourceTypeCode.UInt16:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((ushort)data);
				break;

			case ResourceTypeCode.Int32:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((int)data);
				break;

			case ResourceTypeCode.UInt32:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((uint)data);
				break;

			case ResourceTypeCode.Int64:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((long)data);
				break;

			case ResourceTypeCode.UInt64:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((ulong)data);
				break;

			case ResourceTypeCode.Single:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((float)data);
				break;

			case ResourceTypeCode.Double:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((double)data);
				break;

			case ResourceTypeCode.Decimal:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write((decimal)data);
				break;

			case ResourceTypeCode.DateTime:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write(((DateTime)data).ToBinary());
				break;

			case ResourceTypeCode.TimeSpan:
				Debug.Assert(data is not null); // Verified in ctor
				writer.Write(((TimeSpan)data).Ticks);
				break;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				Debug.Assert(data is not null); // Verified in ctor
				var ary = (byte[])data;
				writer.Write(ary.Length);
				writer.Write(ary);
				break;

			default:
				throw new InvalidOperationException("Unknown resource type code");
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			switch (code) {
			case ResourceTypeCode.Null:
				return "null";

			case ResourceTypeCode.String:
			case ResourceTypeCode.Boolean:
			case ResourceTypeCode.Char:
			case ResourceTypeCode.Byte:
			case ResourceTypeCode.SByte:
			case ResourceTypeCode.Int16:
			case ResourceTypeCode.UInt16:
			case ResourceTypeCode.Int32:
			case ResourceTypeCode.UInt32:
			case ResourceTypeCode.Int64:
			case ResourceTypeCode.UInt64:
			case ResourceTypeCode.Single:
			case ResourceTypeCode.Double:
			case ResourceTypeCode.Decimal:
			case ResourceTypeCode.DateTime:
			case ResourceTypeCode.TimeSpan:
				return $"{code}: '{data}'";

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				var ary = data as byte[];
				if (ary is not null)
					return $"{code}: Length: {ary.Length}";
				return $"{code}: '{data}'";

			default:
				return $"{code}: '{data}'";
			}
		}
	}
}
