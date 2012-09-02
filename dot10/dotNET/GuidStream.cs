using System;
using dot10.IO;

namespace dot10.dotNET {
	public class GuidStream : DotNetStream {
		/// <inheritdoc/>
		public GuidStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <inheritdoc/>
		public override bool IsValidIndex(uint index) {
			return index == 0 || IsValidOffset((index - 1) * 16, 16);
		}

		/// <summary>
		/// Read a <see cref="Guid"/>
		/// </summary>
		/// <param name="index">Index into this stream</param>
		/// <returns>A <see cref="Guid"/> or null if <paramref name="index"/> is 0 or invalid</returns>
		public Guid? Read(uint index) {
			if (index == 0 || !IsValidIndex(index))
				return null;
			imageStream.Position = (index - 1) * 16;
			return new Guid(imageStream.ReadBytes(16));
		}
	}
}
