// dnlib: See LICENSE.txt for more info

﻿using System.IO;

namespace dnlib.PE {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static partial class PEExtensions {
		/// <summary>
		/// Calculates a PE checksum
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="length">Length of image</param>
		/// <param name="checkSumOffset">Offset of checksum</param>
		/// <returns>PE checksum</returns>
		internal static uint CalculatePECheckSum(this BinaryReader reader, long length, long checkSumOffset) {
			uint checkSum = 0;
			for (long i = 0; i < length; i += 2) {
				if (i == checkSumOffset) {
					reader.ReadUInt32();
					i += 2;
					continue;
				}
				checkSum += reader.ReadUInt16();
				checkSum = (ushort)(checkSum + (checkSum >> 16));
			}
			ulong cks = (ulong)checkSum + (ulong)length;
			return (uint)cks + (uint)(cks >> 32);
		}
	}
}
