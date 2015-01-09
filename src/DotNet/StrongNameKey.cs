// dnlib: See LICENSE.txt for more info

﻿using System;
using System.IO;
using System.Security.Cryptography;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Thrown if the strong name key or public key is invalid
	/// </summary>
	[Serializable]
	public class InvalidKeyException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public InvalidKeyException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public InvalidKeyException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Other exception</param>
		public InvalidKeyException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Type of signature algorithm. See WinCrypt.h in the Windows SDK
	/// </summary>
	public enum SignatureAlgorithm : uint {
		/// <summary>
		/// RSA signature algorithm
		/// </summary>
		CALG_RSA_SIGN = 0x00002400,
	}

	static class StrongNameUtils {
		public static byte[] ReadBytesReverse(this BinaryReader reader, int len) {
			var data = reader.ReadBytes(len);
			if (data.Length != len)
				throw new InvalidKeyException("Can't read more bytes");
			Array.Reverse(data);
			return data;
		}

		public static void WriteReverse(this BinaryWriter writer, byte[] data) {
			var d = (byte[])data.Clone();
			Array.Reverse(d);
			writer.Write(d);
		}
	}

	/// <summary>
	/// A public key
	/// </summary>
	public sealed class StrongNamePublicKey {
		const uint RSA1_SIG = 0x31415352;
		SignatureAlgorithm signatureAlgorithm;
		AssemblyHashAlgorithm hashAlgorithm;
		byte[] modulus;
		byte[] publicExponent;

		/// <summary>
		/// Gets/sets the signature algorithm
		/// </summary>
		public SignatureAlgorithm SignatureAlgorithm {
			get { return signatureAlgorithm; }
			set { signatureAlgorithm = value; }
		}

		/// <summary>
		/// Gets/sets the hash algorithm
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlgorithm; }
			set { hashAlgorithm = value; }
		}

		/// <summary>
		/// Gets/sets the modulus
		/// </summary>
		public byte[] Modulus {
			get { return modulus; }
			set { modulus = value; }
		}

		/// <summary>
		/// Gets/sets the public exponent
		/// </summary>
		public byte[] PublicExponent {
			get { return publicExponent; }
			set {
				if (value == null || value.Length != 4)
					throw new ArgumentException("PublicExponent must be exactly 4 bytes");
				publicExponent = value;
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public StrongNamePublicKey() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modulus">Modulus</param>
		/// <param name="publicExponent">Public exponent</param>
		public StrongNamePublicKey(byte[] modulus, byte[] publicExponent)
			: this(modulus, publicExponent, AssemblyHashAlgorithm.SHA1, SignatureAlgorithm.CALG_RSA_SIGN) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modulus">Modulus</param>
		/// <param name="publicExponent">Public exponent</param>
		/// <param name="hashAlgorithm">Hash algorithm</param>
		public StrongNamePublicKey(byte[] modulus, byte[] publicExponent, AssemblyHashAlgorithm hashAlgorithm)
			: this(modulus, publicExponent, hashAlgorithm, SignatureAlgorithm.CALG_RSA_SIGN) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modulus">Modulus</param>
		/// <param name="publicExponent">Public exponent</param>
		/// <param name="hashAlgorithm">Hash algorithm</param>
		/// <param name="signatureAlgorithm">Signature algorithm</param>
		public StrongNamePublicKey(byte[] modulus, byte[] publicExponent, AssemblyHashAlgorithm hashAlgorithm, SignatureAlgorithm signatureAlgorithm) {
			this.signatureAlgorithm = signatureAlgorithm;
			this.hashAlgorithm = hashAlgorithm;
			this.modulus = modulus;
			this.publicExponent = publicExponent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="pk">Public key</param>
		public StrongNamePublicKey(PublicKey pk)
			: this(pk.Data) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="pk">Public key data</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNamePublicKey(byte[] pk) {
			Initialize(new BinaryReader(new MemoryStream(pk)));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Public key file</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNamePublicKey(string filename) {
			using (var fileStream = File.OpenRead(filename))
				Initialize(new BinaryReader(fileStream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Public key stream</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNamePublicKey(Stream stream) {
			Initialize(new BinaryReader(stream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Public key reader</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNamePublicKey(BinaryReader reader) {
			Initialize(reader);
		}

		void Initialize(BinaryReader reader) {
			try {
				// Read PublicKeyBlob
				signatureAlgorithm = (SignatureAlgorithm)reader.ReadUInt32();
				hashAlgorithm = (AssemblyHashAlgorithm)reader.ReadUInt32();
				int pkLen = reader.ReadInt32();

				// Read PUBLICKEYSTRUC
				if (reader.ReadByte() != 6)
					throw new InvalidKeyException("Not a public key");
				if (reader.ReadByte() != 2)
					throw new InvalidKeyException("Invalid version");
				reader.ReadUInt16();	// reserved
				if ((SignatureAlgorithm)reader.ReadUInt32() != SignatureAlgorithm.CALG_RSA_SIGN)
					throw new InvalidKeyException("Not RSA sign");

				// Read RSAPUBKEY
				if (reader.ReadUInt32() != RSA1_SIG)	// magic = RSA1
					throw new InvalidKeyException("Invalid RSA1 magic");
				uint bitLength = reader.ReadUInt32();
				publicExponent = reader.ReadBytesReverse(4);

				modulus = reader.ReadBytesReverse((int)(bitLength / 8));
			}
			catch (IOException ex) {
				throw new InvalidKeyException("Invalid public key", ex);
			}
		}

		/// <summary>
		/// Creates a public key blob
		/// </summary>
		public byte[] CreatePublicKey() {
			return CreatePublicKey(signatureAlgorithm, hashAlgorithm, modulus, publicExponent);
		}

		internal static byte[] CreatePublicKey(SignatureAlgorithm sigAlg, AssemblyHashAlgorithm hashAlg, byte[] modulus, byte[] publicExponent) {
			if (sigAlg != SignatureAlgorithm.CALG_RSA_SIGN)
				throw new ArgumentException("Signature algorithm must be RSA");
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);
			writer.Write((uint)sigAlg);		// SigAlgID
			writer.Write((uint)hashAlg);	// HashAlgID
			writer.Write(0x14 + modulus.Length);// cbPublicKey
			writer.Write((byte)6);			// bType (public key)
			writer.Write((byte)2);			// bVersion
			writer.Write((ushort)0);		// reserved
			writer.Write((uint)sigAlg);		// aiKeyAlg
			writer.Write(RSA1_SIG);			// magic (RSA1)
			writer.Write(modulus.Length * 8);	// bitlen
			writer.WriteReverse(publicExponent);// pubexp
			writer.WriteReverse(modulus);	// modulus
			return outStream.ToArray();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Utils.ToHex(CreatePublicKey(), false);
		}
	}

	/// <summary>
	/// Stores a strong name key pair
	/// </summary>
	public sealed class StrongNameKey {
		const uint RSA2_SIG = 0x32415352;
		byte[] publicKey;
		AssemblyHashAlgorithm hashAlg;
		byte[] publicExponent;
		byte[] modulus;
		byte[] prime1;
		byte[] prime2;
		byte[] exponent1;
		byte[] exponent2;
		byte[] coefficient;
		byte[] privateExponent;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets the public key
		/// </summary>
		public byte[] PublicKey {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (publicKey == null)
					publicKey = CreatePublicKey_NoLock();
				return publicKey;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets the strong name signature size in bytes
		/// </summary>
		public int SignatureSize {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return modulus.Length;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets the public key hash algorithm. It's usually <see cref="AssemblyHashAlgorithm.SHA1"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return hashAlg;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (hashAlg == value)
					return;
				publicKey = null;
				hashAlg = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets the public exponent
		/// </summary>
		public byte[] PublicExponent {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return publicExponent;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null || value.Length != 4)
					throw new ArgumentException("PublicExponent must be exactly 4 bytes");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				publicExponent = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets the modulus
		/// </summary>
		public byte[] Modulus {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return modulus;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				modulus = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets prime1
		/// </summary>
		public byte[] Prime1 {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return prime1;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				prime1 = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets prime2
		/// </summary>
		public byte[] Prime2 {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return prime2;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				prime2 = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets exponent1
		/// </summary>
		public byte[] Exponent1 {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return exponent1;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				exponent1 = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets exponent2
		/// </summary>
		public byte[] Exponent2 {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return exponent2;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				exponent2 = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets the coefficient
		/// </summary>
		public byte[] Coefficient {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return coefficient;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				coefficient = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets/sets the private exponent
		/// </summary>
		public byte[] PrivateExponent {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return privateExponent;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				privateExponent = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
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
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNameKey(byte[] keyData) {
			Initialize(new BinaryReader(new MemoryStream(keyData)));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Strong name key file</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNameKey(string filename) {
			using (var fileStream = File.OpenRead(filename))
				Initialize(new BinaryReader(fileStream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Strong name key stream</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNameKey(Stream stream) {
			Initialize(new BinaryReader(stream));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Strong name key reader</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
		public StrongNameKey(BinaryReader reader) {
			Initialize(reader);
		}

		/// <summary>
		/// Initializes the public/private key pair data
		/// </summary>
		/// <param name="reader">Public/private key pair reader</param>
		/// <exception cref="InvalidKeyException">Strong name key is invalid</exception>
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
					throw new InvalidKeyException("Not a public/private key pair");
				if (reader.ReadByte() != 2)
					throw new InvalidKeyException("Invalid version");
				reader.ReadUInt16();	// reserved
				if ((SignatureAlgorithm)reader.ReadUInt32() != SignatureAlgorithm.CALG_RSA_SIGN)
					throw new InvalidKeyException("Not RSA sign");

				// Read RSAPUBKEY
				if (reader.ReadUInt32() != RSA2_SIG)	// magic = RSA2
					throw new InvalidKeyException("Invalid RSA2 magic");
				uint bitLength = reader.ReadUInt32();
				publicExponent = reader.ReadBytesReverse(4);

				int len8 = (int)(bitLength / 8);
				int len16 = (int)(bitLength / 16);

				// Read the rest
				modulus = reader.ReadBytesReverse(len8);
				prime1 = reader.ReadBytesReverse(len16);
				prime2 = reader.ReadBytesReverse(len16);
				exponent1 = reader.ReadBytesReverse(len16);
				exponent2 = reader.ReadBytesReverse(len16);
				coefficient = reader.ReadBytesReverse(len16);
				privateExponent = reader.ReadBytesReverse(len8);
			}
			catch (IOException ex) {
				throw new InvalidKeyException("Couldn't read strong name key", ex);
			}
		}

		byte[] CreatePublicKey_NoLock() {
			var halg = hashAlg == 0 ? AssemblyHashAlgorithm.SHA1 : hashAlg;
			return StrongNamePublicKey.CreatePublicKey(SignatureAlgorithm.CALG_RSA_SIGN, halg, modulus, publicExponent);
		}

		/// <summary>
		/// Creates an <see cref="RSA"/> instance
		/// </summary>
		public RSA CreateRSA() {
			RSAParameters rsaParams;
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			rsaParams = new RSAParameters {
				Exponent = publicExponent,
				Modulus = modulus,
				P = prime1,
				Q = prime2,
				DP = exponent1,
				DQ = exponent2,
				InverseQ = coefficient,
				D = privateExponent,
			};
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
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

		/// <summary>
		/// Creates a strong name blob
		/// </summary>
		public byte[] CreateStrongName() {
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);
			writer.Write((byte)7);			// bType (public/private key)
			writer.Write((byte)2);			// bVersion
			writer.Write((ushort)0);		// reserved
			writer.Write((uint)SignatureAlgorithm.CALG_RSA_SIGN);	// aiKeyAlg
			writer.Write(RSA2_SIG);			// magic (RSA2)
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			writer.Write(modulus.Length * 8);	// bitlen
			writer.WriteReverse(publicExponent);
			writer.WriteReverse(modulus);
			writer.WriteReverse(prime1);
			writer.WriteReverse(prime2);
			writer.WriteReverse(exponent1);
			writer.WriteReverse(exponent2);
			writer.WriteReverse(coefficient);
			writer.WriteReverse(privateExponent);
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
			return outStream.ToArray();
		}

		/// <summary>
		/// Creates a counter signature, just like
		/// <c>sn -a IdentityPubKey.snk IdentityKey.snk SignaturePubKey.snk</c> can do.
		/// The public key <c>sn</c> prints is <paramref name="signaturePubKey"/>'s value.
		/// </summary>
		/// <param name="identityPubKey">Identity public key</param>
		/// <param name="identityKey">Identity strong name key pair</param>
		/// <param name="signaturePubKey">Signature public key</param>
		/// <returns>The counter signature as a hex string</returns>
		public static string CreateCounterSignatureAsString(StrongNamePublicKey identityPubKey, StrongNameKey identityKey, StrongNamePublicKey signaturePubKey) {
			var counterSignature = CreateCounterSignature(identityPubKey, identityKey, signaturePubKey);
			return Utils.ToHex(counterSignature, false);
		}

		/// <summary>
		/// Creates a counter signature, just like
		/// <c>sn -a IdentityPubKey.snk IdentityKey.snk SignaturePubKey.snk</c> can do.
		/// The public key <c>sn</c> prints is <paramref name="signaturePubKey"/>'s value.
		/// </summary>
		/// <param name="identityPubKey">Identity public key</param>
		/// <param name="identityKey">Identity strong name key pair</param>
		/// <param name="signaturePubKey">Signature public key</param>
		/// <returns>The counter signature</returns>
		public static byte[] CreateCounterSignature(StrongNamePublicKey identityPubKey, StrongNameKey identityKey, StrongNamePublicKey signaturePubKey) {
			var hash = AssemblyHash.Hash(signaturePubKey.CreatePublicKey(), identityPubKey.HashAlgorithm);
			using (var rsa = identityKey.CreateRSA()) {
				var rsaFmt = new RSAPKCS1SignatureFormatter(rsa);
				string hashName = identityPubKey.HashAlgorithm.GetName();
				rsaFmt.SetHashAlgorithm(hashName);
				var snSig = rsaFmt.CreateSignature(hash);
				Array.Reverse(snSig);
				return snSig;
			}
		}
	}
}
