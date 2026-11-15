// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Implements <see cref="IStream"/> and uses a <see cref="Stream"/> as the underlying
	/// stream.
	/// </summary>
	sealed class StreamIStream : IStream {
		readonly Stream stream;
		readonly string name;
		// Offset in stream of position 0 of this IStream
		readonly long baseOffset;

		const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Source stream</param>
		public StreamIStream(Stream stream)
			: this(stream, string.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Source stream</param>
		/// <param name="name">Name of original file or <c>null</c> if unknown.</param>
		public StreamIStream(Stream stream, string name)
			: this(stream, name, 0) {
		}

		StreamIStream(Stream stream, string name, long baseOffset) {
			this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
			this.name = name ?? string.Empty;
			this.baseOffset = baseOffset;
		}

		/// <summary>
		/// Creates a new empty <see cref="IStream"/> that starts at the stream's current position.
		/// Any data in the stream after that position is removed.
		/// </summary>
		/// <param name="stream">Stream, must be seekable</param>
		public static StreamIStream CreateEmpty(Stream stream) {
			long pos = stream.Position;
			if (stream.Length != pos)
				stream.SetLength(pos);
			return new StreamIStream(stream, string.Empty, pos);
		}

		/// <summary>
		/// Gets the length of this <see cref="IStream"/>
		/// </summary>
		public long Length => stream.Length - baseOffset;

		/// <summary>
		/// Offset in the stream of position 0 of this <see cref="IStream"/>
		/// </summary>
		public long BaseOffset => baseOffset;

		/// <inheritdoc/>
		public void Clone(out IStream ppstm) {
			Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);
			throw new Exception();
		}

		/// <inheritdoc/>
		public void Commit(int grfCommitFlags) => stream.Flush();

		/// <inheritdoc/>
		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) {
			if (cb > int.MaxValue)
				cb = int.MaxValue;
			else if (cb < 0)
				cb = 0;
			int sizeToRead = (int)cb;

			if (stream.Position + sizeToRead < sizeToRead || stream.Position + sizeToRead > stream.Length)
				sizeToRead = (int)(stream.Length - Math.Min(stream.Position, stream.Length));

			var buffer = new byte[sizeToRead];
			Read(buffer, sizeToRead, pcbRead);
			if (pcbRead != IntPtr.Zero)
				Marshal.WriteInt64(pcbRead, Marshal.ReadInt32(pcbRead));
			pstm.Write(buffer, buffer.Length, pcbWritten);
			if (pcbWritten != IntPtr.Zero)
				Marshal.WriteInt64(pcbWritten, Marshal.ReadInt32(pcbWritten));
		}

		/// <inheritdoc/>
		public void LockRegion(long libOffset, long cb, int dwLockType) =>
			Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);

		/// <inheritdoc/>
		public void Read(byte[] pv, int cb, IntPtr pcbRead) {
			if (cb < 0)
				cb = 0;

			cb = stream.Read(pv, 0, cb);

			if (pcbRead != IntPtr.Zero)
				Marshal.WriteInt32(pcbRead, cb);
		}

		/// <inheritdoc/>
		public void Revert() {
		}

		enum STREAM_SEEK {
			SET = 0,
			CUR = 1,
			END = 2,
		}

		/// <inheritdoc/>
		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) {
			long newPos;
			switch ((STREAM_SEEK)dwOrigin) {
			case STREAM_SEEK.SET:
				newPos = dlibMove;
				break;

			case STREAM_SEEK.CUR:
				newPos = stream.Position - baseOffset + dlibMove;
				break;

			case STREAM_SEEK.END:
				newPos = Length + dlibMove;
				break;

			default:
				newPos = stream.Position - baseOffset;
				break;
			}
			// Don't allow access to data before the start
			if (newPos < 0)
				Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);
			stream.Position = baseOffset + newPos;

			if (plibNewPosition != IntPtr.Zero)
				Marshal.WriteInt64(plibNewPosition, newPos);
		}

		/// <inheritdoc/>
		public void SetSize(long libNewSize) {
			if (libNewSize < 0)
				Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);
			stream.SetLength(baseOffset + libNewSize);
		}

		enum STATFLAG {
			DEFAULT = 0,
			NONAME = 1,
			NOOPEN = 2,
		}

		enum STGTY {
			STORAGE = 1,
			STREAM = 2,
			LOCKBYTES = 3,
			PROPERTY = 4,
		}

		/// <inheritdoc/>
		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag) {
			var s = new System.Runtime.InteropServices.ComTypes.STATSTG();

			// s.atime = ???;
			s.cbSize = Length;
			s.clsid = Guid.Empty;
			// s.ctime = ???;
			s.grfLocksSupported = 0;
			s.grfMode = 2;
			s.grfStateBits = 0;
			// s.mtime = ???;
			if ((grfStatFlag & (int)STATFLAG.NONAME) == 0)
				s.pwcsName = name;
			s.reserved = 0;
			s.type = (int)STGTY.STREAM;

			pstatstg = s;
		}

		/// <inheritdoc/>
		public void UnlockRegion(long libOffset, long cb, int dwLockType) =>
			Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);

		/// <inheritdoc/>
		public void Write(byte[] pv, int cb, IntPtr pcbWritten) {
			stream.Write(pv, 0, cb);
			if (pcbWritten != IntPtr.Zero)
				Marshal.WriteInt32(pcbWritten, cb);
		}
	}
}
