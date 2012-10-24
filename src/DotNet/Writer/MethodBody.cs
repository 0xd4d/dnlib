using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Method body chunk
	/// </summary>
	public class MethodBody : IChunk {
		const uint EXTRA_SECTIONS_ALIGNMENT = 4;

		readonly bool isTiny;
		readonly byte[] code;
		readonly byte[] extraSections;
		uint length;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
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
			: this(code, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		public MethodBody(byte[] code, byte[] extraSections) {
			this.isTiny = (code[0] & 3) == 2;
			this.code = code;
			this.extraSections = extraSections;
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
		public uint GetLength() {
			return length;
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
