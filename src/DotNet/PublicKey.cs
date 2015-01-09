// dnlib: See LICENSE.txt for more info

using dnlib.Threading;

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Represents a public key
	/// </summary>
	public sealed class PublicKey : PublicKeyBase {
		const AssemblyHashAlgorithm DEFAULT_ALGORITHM = AssemblyHashAlgorithm.SHA1;
		PublicKeyToken publicKeyToken;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets the <see cref="PublicKeyToken"/>
		/// </summary>
		public override PublicKeyToken Token {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (publicKeyToken == null && !IsNullOrEmpty_NoLock)
					publicKeyToken = AssemblyHash.CreatePublicKeyToken(data);
				return publicKeyToken;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <inheritdoc/>
		public override byte[] Data {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return data;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (data == value)
					return;
				data = value;
				publicKeyToken = null;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PublicKey() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Public key data</param>
		public PublicKey(byte[] data)
			: base(data) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexString">Public key data as a hex string or the string <c>"null"</c>
		/// to set public key data to <c>null</c></param>
		public PublicKey(string hexString)
			: base(hexString) {
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if ((object)this == obj)
				return true;
			var other = obj as PublicKey;
			if (other == null)
				return false;
			return Utils.Equals(Data, other.Data);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(Data);
		}
	}
}
