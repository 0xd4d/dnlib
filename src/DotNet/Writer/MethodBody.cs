// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Method body chunk
	/// </summary>
	public sealed class MethodBody : IChunk {
		const uint EXTRA_SECTIONS_ALIGNMENT = 4;

		readonly bool isTiny;
		readonly byte[] code;
		readonly byte[] extraSections;
		uint length;
		FileOffset offset;
		RVA rva;
		uint localVarSigTok;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the code
		/// </summary>
		public byte[] Code {
			get { return code; }
		}

		/// <summary>
		/// Gets the extra sections (exception handlers) or <c>null</c>
		/// </summary>
		public byte[] ExtraSections {
			get { return extraSections; }
		}

		/// <summary>
		/// Gets the token of the locals
		/// </summary>
		public uint LocalVarSigTok {
			get { return localVarSigTok; }
		}

		/// <summary>
		/// <c>true</c> if it's a fat body
		/// </summary>
		public bool IsFat {
			get { return !isTiny; }
		}

		/// <summary>
		/// <c>true</c> if it's a tiny body
		/// </summary>
		public bool IsTiny {
			get { return isTiny; }
		}

		/// <summary>
		/// <c>true</c> if there's an extra section
		/// </summary>
		public bool HasExtraSections {
			get { return extraSections != null && extraSections.Length > 0; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		public MethodBody(byte[] code)
			: this(code, null, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		public MethodBody(byte[] code, byte[] extraSections)
			: this(code, extraSections, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		/// <param name="localVarSigTok">Token of locals</param>
		public MethodBody(byte[] code, byte[] extraSections, uint localVarSigTok) {
			this.isTiny = (code[0] & 3) == 2;
			this.code = code;
			this.extraSections = extraSections;
			this.localVarSigTok = localVarSigTok;
		}

		/// <summary>
		/// Gets the approximate size of the method body (code + exception handlers)
		/// </summary>
		public int GetSizeOfMethodBody() {
			int len = code.Length;
			if (extraSections != null) {
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
				len += extraSections.Length;
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
			}
			return len;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			if (HasExtraSections) {
				RVA rva2 = rva + (uint)code.Length;
				rva2 = rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT);
				rva2 += (uint)extraSections.Length;
				length = (uint)rva2 - (uint)rva;
			}
			else
				length = (uint)code.Length;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(code);
			if (HasExtraSections) {
				RVA rva2 = rva + (uint)code.Length;
				writer.WriteZeros((int)rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT) - (int)rva2);
				writer.Write(extraSections);
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(code) + Utils.GetHashCode(extraSections);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as MethodBody;
			if (other == null)
				return false;
			return Utils.Equals(code, other.code) &&
				Utils.Equals(extraSections, other.extraSections);
		}
	}
}
