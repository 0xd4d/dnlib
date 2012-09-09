using System;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Encodes and decodes tokens/coded tokens
	/// </summary>
	class CodedTokenInfo {
		readonly Table[] tableTypes;
		readonly int bits;
		readonly int mask;

		/// <summary>
		/// Returns all types of tables
		/// </summary>
		public Table[] TableTypes {
			get { return tableTypes; }
		}

		/// <summary>
		/// Returns the number of bits that is used to encode table type
		/// </summary>
		public int Bits {
			get { return bits; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bits">Number of bits used to encode token type</param>
		/// <param name="tableTypes">All table types</param>
		internal CodedTokenInfo(int bits, Table[] tableTypes) {
			this.bits = bits;
			this.mask = (1 << bits) - 1;
			this.tableTypes = tableTypes;
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <returns>Coded token</returns>
		/// <seealso cref="Encode(MDToken,out uint)"/>
		public uint Encode(MDToken token) {
			return Encode(token.Raw);
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <returns>Coded token</returns>
		/// <seealso cref="Encode(uint,out uint)"/>
		public uint Encode(uint token) {
			uint codedToken;
			Encode(token, out codedToken);
			return codedToken;
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <param name="codedToken">Coded token</param>
		/// <returns>true if successful</returns>
		public bool Encode(MDToken token, out uint codedToken) {
			return Encode(token.Raw, out codedToken);
		}

		/// <summary>
		/// Encodes a token
		/// </summary>
		/// <param name="token">The token</param>
		/// <param name="codedToken">Coded token</param>
		/// <returns>true if successful</returns>
		public bool Encode(uint token, out uint codedToken) {
			int index = Array.IndexOf(tableTypes, (Table)(token >> 24));
			if (index < 0) {
				codedToken = uint.MaxValue;
				return false;
			}
			codedToken = ((token & 0x00FFFFFF) << bits) | (uint)index;
			return true;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <returns>Decoded token or 0 on failure</returns>
		/// <seealso cref="Decode(uint,out MDToken)"/>
		public MDToken Decode2(uint codedToken) {
			uint token;
			Decode(codedToken, out token);
			return new MDToken(token);
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <returns>Decoded token or 0 on failure</returns>
		/// <seealso cref="Decode(uint,out uint)"/>
		public uint Decode(uint codedToken) {
			uint token;
			Decode(codedToken, out token);
			return token;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <param name="token">Decoded token</param>
		/// <returns>true if successful</returns>
		public bool Decode(uint codedToken, out MDToken token) {
			uint decodedToken;
			bool result = Decode(codedToken, out decodedToken);
			token = new MDToken(decodedToken);
			return result;
		}

		/// <summary>
		/// Decodes a coded token
		/// </summary>
		/// <param name="codedToken">The coded token</param>
		/// <param name="token">Decoded token</param>
		/// <returns>true if successful</returns>
		public bool Decode(uint codedToken, out uint token) {
			uint rid = codedToken >> bits;
			int index = (int)(codedToken & mask);
			if (rid > 0x00FFFFFF || index > tableTypes.Length) {
				token = 0;
				return false;
			}

			token = ((uint)tableTypes[index] << 24) | rid;
			return true;
		}
	}
}
