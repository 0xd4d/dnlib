// dnlib: See LICENSE.txt for more info

using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	static class NumericReader {
		public static bool TryReadNumeric(ref DataReader reader, ulong end, out object value) {
			value = null;
			ulong position = reader.Position;
			if (position + 2 > end)
				return false;
			var numLeaf = (NumericLeaf)reader.ReadUInt16();
			if (numLeaf < NumericLeaf.LF_NUMERIC) {
				value = (short)numLeaf;
				return true;
			}

			switch (numLeaf) {
			case NumericLeaf.LF_CHAR:
				if (position > end)
					return false;
				value = reader.ReadSByte();
				return true;

			case NumericLeaf.LF_SHORT:
				if (position + 2 > end)
					return false;
				value = reader.ReadInt16();
				return true;

			case NumericLeaf.LF_USHORT:
				if (position + 2 > end)
					return false;
				value = reader.ReadUInt16();
				return true;

			case NumericLeaf.LF_LONG:
				if (position + 4 > end)
					return false;
				value = reader.ReadInt32();
				return true;

			case NumericLeaf.LF_ULONG:
				if (position + 4 > end)
					return false;
				value = reader.ReadUInt32();
				return true;

			case NumericLeaf.LF_REAL32:
				if (position + 4 > end)
					return false;
				value = reader.ReadSingle();
				return true;

			case NumericLeaf.LF_REAL64:
				if (position + 8 > end)
					return false;
				value = reader.ReadDouble();
				return true;

			case NumericLeaf.LF_QUADWORD:
				if (position + 8 > end)
					return false;
				value = reader.ReadInt64();
				return true;

			case NumericLeaf.LF_UQUADWORD:
				if (position + 8 > end)
					return false;
				value = reader.ReadUInt64();
				return true;

			case NumericLeaf.LF_VARSTRING:
				if (position + 2 > end)
					return false;
				int varStrLen = reader.ReadUInt16();
				if (position + (uint)varStrLen > end)
					return false;
				value = reader.ReadUtf8String(varStrLen);
				return true;

			case NumericLeaf.LF_VARIANT:
				if (position + 0x10 > end)
					return false;
				int v0 = reader.ReadInt32();
				int v1 = reader.ReadInt32();
				int v2 = reader.ReadInt32();
				int v3 = reader.ReadInt32();
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
