// dnlib: See LICENSE.txt for more info

using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	static class NumericReader {
		public static bool TryReadNumeric(IImageStream stream, long end, out object value) {
			value = null;
			if (stream.Position + 2 > end)
				return false;
			var numLeaf = (NumericLeaf)stream.ReadUInt16();
			if (numLeaf < NumericLeaf.LF_NUMERIC) {
				value = (short)numLeaf;
				return true;
			}

			switch (numLeaf) {
			case NumericLeaf.LF_CHAR:
				if (stream.Position > end)
					return false;
				value = stream.ReadSByte();
				return true;

			case NumericLeaf.LF_SHORT:
				if (stream.Position + 2 > end)
					return false;
				value = stream.ReadInt16();
				return true;

			case NumericLeaf.LF_USHORT:
				if (stream.Position + 2 > end)
					return false;
				value = stream.ReadUInt16();
				return true;

			case NumericLeaf.LF_LONG:
				if (stream.Position + 4 > end)
					return false;
				value = stream.ReadInt32();
				return true;

			case NumericLeaf.LF_ULONG:
				if (stream.Position + 4 > end)
					return false;
				value = stream.ReadUInt32();
				return true;

			case NumericLeaf.LF_REAL32:
				if (stream.Position + 4 > end)
					return false;
				value = stream.ReadSingle();
				return true;

			case NumericLeaf.LF_REAL64:
				if (stream.Position + 8 > end)
					return false;
				value = stream.ReadDouble();
				return true;

			case NumericLeaf.LF_QUADWORD:
				if (stream.Position + 8 > end)
					return false;
				value = stream.ReadInt64();
				return true;

			case NumericLeaf.LF_UQUADWORD:
				if (stream.Position + 8 > end)
					return false;
				value = stream.ReadUInt64();
				return true;

			case NumericLeaf.LF_VARSTRING:
				if (stream.Position + 2 > end)
					return false;
				int varStrLen = stream.ReadUInt16();
				if (stream.Position + varStrLen > end)
					return false;
				value = Encoding.UTF8.GetString(stream.ReadBytes(varStrLen));
				return true;

			case NumericLeaf.LF_VARIANT:
				if (stream.Position + 0x10 > end)
					return false;
				int v0 = stream.ReadInt32();
				int v1 = stream.ReadInt32();
				int v2 = stream.ReadInt32();
				int v3 = stream.ReadInt32();
				byte scale = (byte)(v0 >> 16);
				if (scale <= 28)
					value = new decimal(v2, v3, v1, v0 < 0, scale);
				else
					value = null;
				return true;

			default:
				return false;
			}
		}
	}
}
