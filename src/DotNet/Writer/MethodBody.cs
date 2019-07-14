// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
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
		readonly uint localVarSigTok;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the code
		/// </summary>
		public byte[] Code => code;

		/// <summary>
		/// Gets the extra sections (exception handlers) or <c>null</c>
		/// </summary>
		public byte[] ExtraSections => extraSections;

		/// <summary>
		/// Gets the token of the locals
		/// </summary>
		public uint LocalVarSigTok => localVarSigTok;

		/// <summary>
		/// <c>true</c> if it's a fat body
		/// </summary>
		public bool IsFat => !isTiny;

		/// <summary>
		/// <c>true</c> if it's a tiny body
		/// </summary>
		public bool IsTiny => isTiny;

		/// <summary>
		/// <c>true</c> if there's an extra section
		/// </summary>
		public bool HasExtraSections => !(extraSections is null) && extraSections.Length > 0;

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
			isTiny = (code[0] & 3) == 2;
			this.code = code;
			this.extraSections = extraSections;
			this.localVarSigTok = localVarSigTok;
		}

		/// <summary>
		/// Gets the approximate size of the method body (code + exception handlers)
		/// </summary>
		public int GetApproximateSizeOfMethodBody() {
			int len = code.Length;
			if (!(extraSections is null)) {
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
				len += extraSections.Length;
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
			}
			return len;
		}

		internal bool CanReuse(RVA origRva, uint origSize) {
			uint length;
			if (HasExtraSections) {
				var rva2 = origRva + (uint)code.Length;
				rva2 = rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT);
				rva2 += (uint)extraSections.Length;
				length = (uint)rva2 - (uint)origRva;
			}
			else
				length = (uint)code.Length;
			return length <= origSize;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			Debug.Assert(this.rva == 0);
			this.offset = offset;
			this.rva = rva;
			if (HasExtraSections) {
				var rva2 = rva + (uint)code.Length;
				rva2 = rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT);
				rva2 += (uint)extraSections.Length;
				length = (uint)rva2 - (uint)rva;
			}
			else
				length = (uint)code.Length;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			writer.WriteBytes(code);
			if (HasExtraSections) {
				var rva2 = rva + (uint)code.Length;
				writer.WriteZeroes((int)rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT) - (int)rva2);
				writer.WriteBytes(extraSections);
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() => Utils.GetHashCode(code) + Utils.GetHashCode(extraSections);

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as MethodBody;
			if (other is null)
				return false;
			return Utils.Equals(code, other.code) &&
				Utils.Equals(extraSections, other.extraSections);
		}
	}
}
