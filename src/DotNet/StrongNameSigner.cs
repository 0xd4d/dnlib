// dnlib: See LICENSE.txt for more info

﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace dnlib.DotNet {
	/// <summary>
	/// Strong name signs an assembly. It supports normal strong name signing and the new
	/// (.NET 4.5) enhanced strong name signing.
	/// </summary>
	public struct StrongNameSigner {
		readonly Stream stream;
		readonly long baseOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">.NET PE file stream</param>
		public StrongNameSigner(Stream stream)
			: this(stream, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">.NET PE file stream</param>
		/// <param name="baseOffset">Offset in <paramref name="stream"/> of the first byte of
		/// the PE file.</param>
		public StrongNameSigner(Stream stream, long baseOffset) {
			this.stream = stream;
			this.baseOffset = baseOffset;
		}

		/// <summary>
		/// Calculates the strong name signature and writes it to the stream. The signature
		/// is also returned.
		/// </summary>
		/// <param name="snk">Strong name key used for signing</param>
		/// <param name="snSigOffset">Offset (relative to the start of the PE file) of the strong
		/// name signature.</param>
		/// <returns>The strong name signature</returns>
		public byte[] WriteSignature(StrongNameKey snk, long snSigOffset) {
			var sign = CalculateSignature(snk, snSigOffset);
			stream.Position = baseOffset + snSigOffset;
			stream.Write(sign, 0, sign.Length);
			return sign;
		}

		/// <summary>
		/// Calculates and returns the strong name signature
		/// </summary>
		/// <param name="snk">Strong name key used for signing</param>
		/// <param name="snSigOffset">Offset (relative to start of PE file) of the strong
		/// name signature.</param>
		/// <returns>The strong name signature</returns>
		public byte[] CalculateSignature(StrongNameKey snk, long snSigOffset) {
			uint snSigSize = (uint)snk.SignatureSize;
			var hashAlg = snk.HashAlgorithm == 0 ? AssemblyHashAlgorithm.SHA1 : snk.HashAlgorithm;
			var hash = StrongNameHashData(hashAlg, snSigOffset, snSigSize);
			var snSig = GetStrongNameSignature(snk, hashAlg, hash);
			if (snSig.Length != snSigSize)
				throw new InvalidOperationException("Invalid strong name signature size");
			return snSig;
		}

		/// <summary>
		/// Strong name hashes the .NET file
		/// </summary>
		/// <param name="hashAlg">Hash algorithm</param>
		/// <param name="snSigOffset">Strong name sig offset (relative to start of .NET PE file)</param>
		/// <param name="snSigSize">Size of strong name signature</param>
		/// <returns>The strong name hash of the .NET file</returns>
		byte[] StrongNameHashData(AssemblyHashAlgorithm hashAlg, long snSigOffset, uint snSigSize) {
			var reader = new BinaryReader(stream);

			snSigOffset += baseOffset;
			long snSigOffsetEnd = snSigOffset + snSigSize;

			using (var hasher = new AssemblyHash(hashAlg)) {
				byte[] buffer = new byte[0x8000];

				// Hash the DOS header. It's defined to be all data from the start of
				// the file up to the NT headers.
				stream.Position = baseOffset + 0x3C;
				uint ntHeadersOffs = reader.ReadUInt32();
				stream.Position = baseOffset;
				hasher.Hash(stream, ntHeadersOffs, buffer);

				// Hash NT headers, but hash authenticode + checksum as 0s
				stream.Position += 6;
				int numSections = reader.ReadUInt16();
				stream.Position -= 8;
				hasher.Hash(stream, 0x18, buffer);	// magic + FileHeader

				bool is32bit = reader.ReadUInt16() == 0x010B;
				stream.Position -= 2;
				int optHeaderSize = is32bit ? 0x60 : 0x70;
				if (stream.Read(buffer, 0, optHeaderSize) != optHeaderSize)
					throw new IOException("Could not read data");
				// Clear checksum
				for (int i = 0; i < 4; i++)
					buffer[0x40 + i] = 0;
				hasher.Hash(buffer, 0, optHeaderSize);

				const int imageDirsSize = 16 * 8;
				if (stream.Read(buffer, 0, imageDirsSize) != imageDirsSize)
					throw new IOException("Could not read data");
				// Clear authenticode data dir
				for (int i = 0; i < 8; i++)
					buffer[4 * 8 + i] = 0;
				hasher.Hash(buffer, 0, imageDirsSize);

				// Hash section headers
				long sectHeadersOffs = stream.Position;
				hasher.Hash(stream, (uint)numSections * 0x28, buffer);

				// Hash all raw section data but make sure we don't hash the location
				// where the strong name signature will be stored.
				for (int i = 0; i < numSections; i++) {
					stream.Position = sectHeadersOffs + i * 0x28 + 0x10;
					uint sizeOfRawData = reader.ReadUInt32();
					uint pointerToRawData = reader.ReadUInt32();

					stream.Position = baseOffset + pointerToRawData;
					while (sizeOfRawData > 0) {
						var pos = stream.Position;

						if (snSigOffset <= pos && pos < snSigOffsetEnd) {
							uint skipSize = (uint)(snSigOffsetEnd - pos);
							if (skipSize >= sizeOfRawData)
								break;
							sizeOfRawData -= skipSize;
							stream.Position += skipSize;
							continue;
						}

						if (pos >= snSigOffsetEnd) {
							hasher.Hash(stream, sizeOfRawData, buffer);
							break;
						}

						uint maxLen = (uint)Math.Min(snSigOffset - pos, sizeOfRawData);
						hasher.Hash(stream, maxLen, buffer);
						sizeOfRawData -= maxLen;
					}
				}

				return hasher.ComputeHash();
			}
		}

		/// <summary>
		/// Returns the strong name signature
		/// </summary>
		/// <param name="snk">Strong name key</param>
		/// <param name="hashAlg">Hash algorithm</param>
		/// <param name="hash">Strong name hash of the .NET PE file</param>
		/// <returns>Strong name signature</returns>
		byte[] GetStrongNameSignature(StrongNameKey snk, AssemblyHashAlgorithm hashAlg, byte[] hash) {
			using (var rsa = snk.CreateRSA()) {
				var rsaFmt = new RSAPKCS1SignatureFormatter(rsa);
				string hashName = hashAlg.GetName() ?? AssemblyHashAlgorithm.SHA1.GetName();
				rsaFmt.SetHashAlgorithm(hashName);
				var snSig = rsaFmt.CreateSignature(hash);
				Array.Reverse(snSig);
				return snSig;
			}
		}
	}
}
