// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Validates a PDB file against the PDB checksum debug directory entries in the PE file if
	/// <see cref="PdbReaderOptions.ValidatePdbChecksum"/> is set.
	///
	/// https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#pdb-checksum-debug-directory-entry-type-19
	/// </summary>
	static class PdbChecksumValidator {
		readonly struct PdbChecksum {
			public readonly ChecksumAlgorithm Algorithm;
			public readonly byte[] Checksum;
			public PdbChecksum(ChecksumAlgorithm algorithm, byte[] checksum) {
				Algorithm = algorithm;
				Checksum = checksum;
			}
		}

		// A range of the PDB file that is hashed as if all its bytes were 0
		readonly struct ZeroedRange {
			public readonly uint Offset;
			public readonly uint Length;
			public ZeroedRange(uint offset, uint length) {
				Offset = offset;
				Length = length;
			}
		}

		/// <summary>
		/// Checks a Portable PDB file. Returns <c>true</c> if it's valid or if it shouldn't or can't be validated.
		/// </summary>
		/// <param name="pdbContext">PDB context</param>
		/// <param name="pdbData">The whole PDB file</param>
		/// <param name="pdbIdOffset">Offset of the 20-byte PDB ID in <paramref name="pdbData"/></param>
		public static bool IsValidPortablePdb(PdbReaderContext pdbContext, DataReader pdbData, uint pdbIdOffset) {
			if (!TryGetChecksums(pdbContext, out var checksums))
				return true;
			// The PDB ID is the GUID and timestamp, which MatchesModule() has already compared with the CodeView entry
			const uint PdbIdSize = 20;
			if ((ulong)pdbIdOffset + PdbIdSize > pdbData.Length)
				return false;
			return ChecksumMatches(checksums, pdbData, new[] { new ZeroedRange(pdbIdOffset, PdbIdSize) });
		}

		/// <summary>
		/// Checks a Windows PDB file. Returns <c>true</c> if it's valid or if it shouldn't or can't be validated.
		/// </summary>
		/// <param name="pdbContext">PDB context</param>
		/// <param name="pdbData">The whole PDB file</param>
		public static bool IsValidWindowsPdb(PdbReaderContext pdbContext, DataReader pdbData) {
			if (!TryGetChecksums(pdbContext, out var checksums))
				return true;
			if (!TryGetWindowsPdbSignature(pdbData, out uint signatureOffset, out uint timestamp))
				return false;
			// The checksum doesn't include the GUID and timestamp so they must match the CodeView entry.
			// The readers have already compared the GUID and age, but not always the timestamp.
			if (pdbContext.CodeViewDebugDirectory is not ImageDebugDirectory codeViewDir || codeViewDir.TimeDateStamp != timestamp)
				return false;
			// PDB info stream: uint version, uint timestamp, uint age, Guid guid. The age isn't zeroed.
			var ranges = new[] {
				new ZeroedRange(signatureOffset + 4, 4),
				new ZeroedRange(signatureOffset + 12, 16),
			};
			return ChecksumMatches(checksums, pdbData, ranges);
		}

		// Returns false if it's not enabled or if there are no PDB checksum entries with a supported algorithm
		static bool TryGetChecksums(PdbReaderContext pdbContext, [NotNullWhen(true)] out List<PdbChecksum>? checksums) {
			checksums = null;
			if ((pdbContext.Options & PdbReaderOptions.ValidatePdbChecksum) == 0)
				return false;
			foreach (var entry in pdbContext.GetDebugDirectoryEntries(ImageDebugType.PdbChecksum)) {
				// Only version 1.0 has been defined
				if (entry.MajorVersion != 1 || entry.MinorVersion != 0)
					continue;
				if (entry.SizeOfData > Hasher.MaxPdbChecksumEntrySize)
					continue;
				var reader = pdbContext.CreateReader(entry.AddressOfRawData, entry.SizeOfData);
				if (TryReadChecksum(ref reader, out var checksum))
					(checksums ??= new List<PdbChecksum>()).Add(checksum);
			}
			return checksums is not null;
		}

		static bool TryReadChecksum(ref DataReader reader, out PdbChecksum checksum) {
			checksum = default;
			try {
				var name = reader.TryReadZeroTerminatedUtf8String();
				if (!Hasher.TryGetChecksumAlgorithm(name, out var algorithm, out int size) || (uint)size != reader.BytesLeft)
					return false;
				checksum = new PdbChecksum(algorithm, reader.ReadRemainingBytes());
				return true;
			}
			catch (IOException) {
			}
			catch (ArgumentException) {
			}
			return false;
		}

		static bool ChecksumMatches(List<PdbChecksum> checksums, DataReader pdbData, ZeroedRange[] zeroedRanges) {
			// There can be more than one entry, eg. if both private and public PDBs were created
			var hashes = new Dictionary<ChecksumAlgorithm, byte[]>();
			foreach (var checksum in checksums) {
				if (!hashes.TryGetValue(checksum.Algorithm, out var hash))
					hashes.Add(checksum.Algorithm, hash = Hash(checksum.Algorithm, pdbData, zeroedRanges));
				if (Equals(hash, checksum.Checksum))
					return true;
			}
			return false;
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		static byte[] Hash(ChecksumAlgorithm algorithm, DataReader pdbData, ZeroedRange[] zeroedRanges) {
			var buffer = new byte[(int)Math.Min(0x10000, pdbData.Length)];
			using (var hasher = Hasher.CreateHasher(algorithm)) {
				pdbData.Position = 0;
				while (pdbData.BytesLeft > 0) {
					uint pos = pdbData.Position;
					int len = (int)Math.Min((uint)buffer.Length, pdbData.BytesLeft);
					pdbData.ReadBytes(buffer, 0, len);
					foreach (var range in zeroedRanges) {
						ulong start = Math.Max(range.Offset, pos);
						ulong end = Math.Min((ulong)range.Offset + range.Length, (ulong)pos + (uint)len);
						for (ulong i = start; i < end; i++)
							buffer[(int)(i - pos)] = 0;
					}
					hasher.TransformBlock(buffer, 0, len, buffer, 0);
				}
				hasher.TransformFinalBlock(Array2.Empty<byte>(), 0, 0);
				return hasher.Hash!;
			}
		}

		/// <summary>
		/// Gets the file offset of the PDB info stream (stream #1) and the timestamp stored in it
		/// </summary>
		static bool TryGetWindowsPdbSignature(DataReader reader, out uint signatureOffset, out uint timestamp) {
			signatureOffset = 0;
			timestamp = 0;
			try {
				const string SIG = "Microsoft C/C++ MSF 7.00\r\n\u001ADS\0";
				reader.Position = 0;
				if (reader.ReadString(SIG.Length, Encoding.ASCII) != SIG)
					return false;
				reader.Position = 0x20;
				uint pageSize = reader.ReadUInt32();
				/*uint fpm = */reader.ReadUInt32();
				uint pageCount = reader.ReadUInt32();
				uint rootSize = reader.ReadUInt32();
				/*uint reserved = */reader.ReadUInt32();
				const uint RootPagePointersOffset = 0x34;
				if (pageSize < 0x200 || pageSize > 0x8000 || (pageSize & (pageSize - 1)) != 0)
					return false;
				if ((ulong)pageCount * pageSize > reader.Length)
					return false;

				// Returns the file offset of a byte in the root directory stream. The offset must be 4-byte aligned.
				bool TryGetRootOffset(ref DataReader r, uint rootOffset, out uint fileOffset) {
					fileOffset = 0;
					if ((ulong)rootOffset + 4 > rootSize)
						return false;
					uint rootPageIndex = rootOffset / pageSize;
					uint pointersPerPage = pageSize / 4;
					r.Position = RootPagePointersOffset + rootPageIndex / pointersPerPage * 4;
					uint ptrPage = r.ReadUInt32();
					if (ptrPage >= pageCount)
						return false;
					r.Position = ptrPage * pageSize + rootPageIndex % pointersPerPage * 4;
					uint rootPage = r.ReadUInt32();
					if (rootPage >= pageCount)
						return false;
					fileOffset = rootPage * pageSize + rootOffset % pageSize;
					return true;
				}

				bool TryReadRootUInt32(ref DataReader r, uint rootOffset, out uint value) {
					value = 0;
					if (!TryGetRootOffset(ref r, rootOffset, out uint fileOffset))
						return false;
					r.Position = fileOffset;
					value = r.ReadUInt32();
					return true;
				}

				// Root directory: uint streamCount, uint streamSizes[streamCount], uint streamPages[streamCount][]
				const int STREAM_PDB_INFO = 1;
				if (!TryReadRootUInt32(ref reader, 0, out uint streamCount) || streamCount <= STREAM_PDB_INFO)
					return false;
				if (!TryReadRootUInt32(ref reader, 4, out uint stream0Size))
					return false;
				if (!TryReadRootUInt32(ref reader, 4 + STREAM_PDB_INFO * 4, out uint infoStreamSize))
					return false;
				// The info stream is at least 28 bytes so all of version, timestamp, age and GUID are in its first page
				if (infoStreamSize == uint.MaxValue || infoStreamSize < 28)
					return false;
				uint stream0Pages = stream0Size == uint.MaxValue ? 0 : (uint)(((ulong)stream0Size + pageSize - 1) / pageSize);
				ulong infoPageRootOffset = 4 + (ulong)streamCount * 4 + (ulong)stream0Pages * 4;
				if (infoPageRootOffset > uint.MaxValue)
					return false;
				if (!TryReadRootUInt32(ref reader, (uint)infoPageRootOffset, out uint infoStreamPage) || infoStreamPage >= pageCount)
					return false;

				signatureOffset = infoStreamPage * pageSize;
				reader.Position = signatureOffset + 4;
				timestamp = reader.ReadUInt32();
				return true;
			}
			catch (IOException) {
			}
			catch (ArgumentException) {
			}
			return false;
		}
	}
}
