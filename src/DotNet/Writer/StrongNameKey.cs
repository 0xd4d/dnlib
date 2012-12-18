using System;
using System.IO;
using System.Security.Cryptography;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Thrown if the strong name key is invalid
	/// </summary>
	[Serializable]
	public class InvalidStrongNameKeyException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public InvalidStrongNameKeyException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public InvalidStrongNameKeyException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Other exception</param>
		public InvalidStrongNameKeyException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Stores a strong name key pair
	/// </summary>
	public sealed class StrongNameKey {
		const uint CALG_RSA_SIGN = 0x00002400;
		byte[] publicKey;
		AssemblyHashAlgorithm hashAlg;
		uint bitLength;
		byte[] publicExponent;
		byte[] modulus;
		byte[] prime1;
		byte[] prime2;
		byte[] exponent1;
		byte[] exponent2;
		byte[] coefficient;
		byte[] privateExponent;

		/// <summary>
		/// Gets the public key
		/// </summary>
		public byte[] PublicKey {
			get { return publicKey ?? (publicKey = CreatePublicKey()); }
		}

		/// <summary>
		/// Gets the strong name signature size in bytes
		/// </summary>
		public int SignatureSize {
			get { return modulus.Length; }
		}

		/// <summary>
		/// Gets/sets the public key hash algorithm. It's usually <see cref="AssemblyHashAlgorithm.SHA1"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlg; }
			set {
				if (hashAlg == value)
					return;
				publicKey = null;
				hashAlg = value;
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public StrongNameKey() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="keyData">Strong name key data</param>
		/// <exception cref="InvalidStrongNameKeyException">Strong name key is invalid</exception>
		public StrongNameKey(byte[] keyData) {
			Initialize(new BinaryReader(new MemoryStream(keyData)));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Strong name key file</param>
		/// <exception cref="InvalidStrongNameKeyException">Strong name key is invalid</exception>
		public StrongNameKey(string filename) {
			using (var fileStream = File.OpenRead(filename))
				Initialize(new BinaryReader(fileStream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Strong name key stream</param>
		/// <exception cref="InvalidStrongNameKeyException">Strong name key is invalid</exception>
		public StrongNameKey(Stream stream) {
			Initialize(new BinaryReader(stream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Strong name key reader</param>
		/// <exception cref="InvalidStrongNameKeyException">Strong name key is invalid</exception>
		public StrongNameKey(BinaryReader reader) {
			Initialize(reader);
		}

		/// <summary>
		/// Initializes the public/private key pair data
		/// </summary>
		/// <param name="reader">Public/private key pair reader</param>
		/// <exception cref="InvalidStrongNameKeyException">Strong name key is invalid</exception>
		public void Initialize(BinaryReader reader) {
			/*
			 * Links:
			 *	http://msdn.microsoft.com/en-us/library/cc250013%28v=prot.20%29.aspx
			 *	http://msdn.microsoft.com/en-us/library/windows/desktop/aa387689%28v=vs.85%29.aspx
			 *
			 *	struct PublicKeyBlob {
			 *	  unsigned int SigAlgID;	// sig algorithm used to create the sig (00002400 = CALG_RSA_SIGN)
			 *	  unsigned int HashAlgID;	// hash alg used to create the sig (usually 00008004 = CALG_SHA1)
			 *	  ULONG        cbPublicKey;	// Size of the data that follows
			 *	  // the rest is here
			 *	}
			 *
			 *	typedef struct _PUBLICKEYSTRUC {
			 *	  BYTE   bType;
			 *	  BYTE   bVersion;
			 *	  WORD   reserved;
			 *	  ALG_ID aiKeyAlg;
			 *	} BLOBHEADER, PUBLICKEYSTRUC;
			 *
			 *	typedef struct _RSAPUBKEY {
			 *	  DWORD magic;
			 *	  DWORD bitlen;
			 *	  DWORD pubexp;
			 *	} RSAPUBKEY;
			 *
			 * Format of public key
			 *	PublicKeyBlob
			 *	PUBLICKEYSTRUC	publickeystruc;
			 *	RSAPUBKEY		rsapubkey;
			 *	BYTE			modulus[rsapubkey.bitlen/8]
			 *
			 * Format of public/private key pair
			 *	PUBLICKEYSTRUC	publickeystruc;
			 *	RSAPUBKEY		rsapubkey;
			 *	BYTE			modulus[rsapubkey.bitlen/8];
			 *	BYTE			prime1[rsapubkey.bitlen/16];		// aka P
			 *	BYTE			prime2[rsapubkey.bitlen/16];		// aka Q
			 *	BYTE			exponent1[rsapubkey.bitlen/16];		// aka DP
			 *	BYTE			exponent2[rsapubkey.bitlen/16];		// aka DQ
			 *	BYTE			coefficient[rsapubkey.bitlen/16];	// aka IQ
			 *	BYTE			privateExponent[rsapubkey.bitlen/8];// aka D
			 */

			try {
				publicKey = null;

				// Read PUBLICKEYSTRUC
				if (reader.ReadByte() != 7)
					throw new InvalidStrongNameKeyException("Not a public/private key pair");
				if (reader.ReadByte() != 2)
					throw new InvalidStrongNameKeyException("Invalid version");
				reader.ReadUInt16();	// reserved
				if (reader.ReadUInt32() != CALG_RSA_SIGN)
					throw new InvalidStrongNameKeyException("Not RSA sign");

				// Read RSAPUBKEY
				if (reader.ReadUInt32() != 0x32415352)	// magic = RSA2
					throw new InvalidStrongNameKeyException("Invalid RSA2 magic");
				bitLength = reader.ReadUInt32();
				publicExponent = ReadData(reader, 4);

				int len8 = (int)(bitLength / 8);
				int len16 = (int)(bitLength / 16);

				// Read the rest
				modulus = ReadData(reader, len8);
				prime1 = ReadData(reader, len16);
				prime2 = ReadData(reader, len16);
				exponent1 = ReadData(reader, len16);
				exponent2 = ReadData(reader, len16);
				coefficient = ReadData(reader, len16);
				privateExponent = ReadData(reader, len8);
			}
			catch (IOException ex) {
				throw new InvalidStrongNameKeyException("Couldn't read strong name key", ex);
			}
		}

		static byte[] ReadData(BinaryReader reader, int len) {
			var data = reader.ReadBytes(len);
			if (data.Length != len)
				throw new InvalidStrongNameKeyException("Can't read more bytes");
			Array.Reverse(data);
			return data;
		}

		byte[] CreatePublicKey() {
			uint halg = (uint)(hashAlg == 0 ? AssemblyHashAlgorithm.SHA1 : hashAlg);

			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);
			writer.Write(CALG_RSA_SIGN);	// SigAlgID
			writer.Write(halg);				// HashAlgID
			writer.Write(0x14 + modulus.Length);	// cbPublicKey
			writer.Write((byte)6);			// bType (public key)
			writer.Write((byte)2);			// bVersion
			writer.Write((ushort)0);		// reserved
			writer.Write(CALG_RSA_SIGN);	// aiKeyAlg
			writer.Write(0x31415352);		// magic (RSA1)
			writer.Write(bitLength);		// bitlen
			WriteReverse(writer, publicExponent);	// pubexp
			WriteReverse(writer, modulus);	// modulus
			return outStream.ToArray();
		}

		static void WriteReverse(BinaryWriter writer, byte[] data) {
			var d = (byte[])data.Clone();
			Array.Reverse(d);
			writer.Write(d);
		}

		/// <summary>
		/// Creates an <see cref="RSA"/> instance
		/// </summary>
		public RSA CreateRSA() {
			var rsaParams = new RSAParameters {
				Exponent = publicExponent,
				Modulus = modulus,
				P = prime1,
				Q = prime2,
				DP = exponent1,
				DQ = exponent2,
				InverseQ = coefficient,
				D = privateExponent,
			};
			var rsa = RSA.Create();
			try {
				rsa.ImportParameters(rsaParams);
				return rsa;
			}
			catch {
				((IDisposable)rsa).Dispose();
				throw;
			}
		}
	}
}
