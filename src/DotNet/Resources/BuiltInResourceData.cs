// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Built-in resource data
	/// </summary>
	public sealed class BuiltInResourceData : IResourceData {
		readonly ResourceTypeCode code;
		readonly object data;

		/// <summary>
		/// Gets the data
		/// </summary>
		public object Data {
			get { return data; }
		}

		/// <inheritdoc/>
		public ResourceTypeCode Code {
			get { return code; }
		}

		/// <inheritdoc/>
		public FileOffset StartOffset { get; set; }

		/// <inheritdoc/>
		public FileOffset EndOffset { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Type of data</param>
		/// <param name="data">Data</param>
		public BuiltInResourceData(ResourceTypeCode code, object data) {
			this.code = code;
			this.data = data;
		}

		/// <inheritdoc/>
		public void WriteData(BinaryWriter writer, IFormatter formatter) {
			switch (code) {
			case ResourceTypeCode.Null:
				break;

			case ResourceTypeCode.String:
				writer.Write((string)data);
				break;

			case ResourceTypeCode.Boolean:
				writer.Write((bool)data);
				break;

			case ResourceTypeCode.Char:
				writer.Write((ushort)(char)data);
				break;

			case ResourceTypeCode.Byte:
				writer.Write((byte)data);
				break;

			case ResourceTypeCode.SByte:
				writer.Write((sbyte)data);
				break;

			case ResourceTypeCode.Int16:
				writer.Write((short)data);
				break;

			case ResourceTypeCode.UInt16:
				writer.Write((ushort)data);
				break;

			case ResourceTypeCode.Int32:
				writer.Write((int)data);
				break;

			case ResourceTypeCode.UInt32:
				writer.Write((uint)data);
				break;

			case ResourceTypeCode.Int64:
				writer.Write((long)data);
				break;

			case ResourceTypeCode.UInt64:
				writer.Write((ulong)data);
				break;

			case ResourceTypeCode.Single:
				writer.Write((float)data);
				break;

			case ResourceTypeCode.Double:
				writer.Write((double)data);
				break;

			case ResourceTypeCode.Decimal:
				writer.Write((decimal)data);
				break;

			case ResourceTypeCode.DateTime:
				writer.Write(((DateTime)data).ToBinary());
				break;

			case ResourceTypeCode.TimeSpan:
				writer.Write(((TimeSpan)data).Ticks);
				break;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
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
				return string.Format("{0}: '{1}'", code, data);

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				var ary = data as byte[];
				if (ary != null)
					return string.Format("{0}: Length: {1}", code, ary.Length);
				return string.Format("{0}: '{1}'", code, data);

			default:
				return string.Format("{0}: '{1}'", code, data);
			}
		}
	}
}
