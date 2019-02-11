// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class DataReaderIStream : IStream, IDisposable {
		readonly DataReaderFactory dataReaderFactory;
		DataReader reader;
		readonly string name;

		const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);
		const int STG_E_CANTSAVE = unchecked((int)0x80030103);

		public DataReaderIStream(DataReaderFactory dataReaderFactory)
			: this(dataReaderFactory, dataReaderFactory.CreateReader(), string.Empty) {
		}

		DataReaderIStream(DataReaderFactory dataReaderFactory, DataReader reader, string name) {
			this.dataReaderFactory = dataReaderFactory ?? throw new ArgumentNullException(nameof(dataReaderFactory));
			this.reader = reader;
			this.name = name ?? string.Empty;
		}

		public void Clone(out IStream ppstm) => ppstm = new DataReaderIStream(dataReaderFactory, reader, name);

		public void Commit(int grfCommitFlags) {
		}

		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) {
			if (cb > int.MaxValue)
				cb = int.MaxValue;
			else if (cb < 0)
				cb = 0;
			int sizeToRead = (int)cb;

			if ((ulong)reader.Position + (uint)sizeToRead > reader.Length)
				sizeToRead = (int)(reader.Length - Math.Min(reader.Position, reader.Length));

			var buffer = new byte[sizeToRead];
			Read(buffer, sizeToRead, pcbRead);
			if (pcbRead != IntPtr.Zero)
				Marshal.WriteInt64(pcbRead, Marshal.ReadInt32(pcbRead));
			pstm.Write(buffer, buffer.Length, pcbWritten);
			if (pcbWritten != IntPtr.Zero)
				Marshal.WriteInt64(pcbWritten, Marshal.ReadInt32(pcbWritten));
		}

		public void LockRegion(long libOffset, long cb, int dwLockType) => Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);

		public void Read(byte[] pv, int cb, IntPtr pcbRead) {
			if (cb < 0)
				cb = 0;

			cb = (int)Math.Min(reader.BytesLeft, (uint)cb);
			reader.ReadBytes(pv, 0, cb);

			if (pcbRead != IntPtr.Zero)
				Marshal.WriteInt32(pcbRead, cb);
		}

		public void Revert() {
		}

		enum STREAM_SEEK {
			SET = 0,
			CUR = 1,
			END = 2,
		}

		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) {
			switch ((STREAM_SEEK)dwOrigin) {
			case STREAM_SEEK.SET:
				reader.Position = (uint)dlibMove;
				break;

			case STREAM_SEEK.CUR:
				reader.Position = (uint)(reader.Position + dlibMove);
				break;

			case STREAM_SEEK.END:
				reader.Position = (uint)(reader.Length + dlibMove);
				break;
			}

			if (plibNewPosition != IntPtr.Zero)
				Marshal.WriteInt64(plibNewPosition, reader.Position);
		}

		public void SetSize(long libNewSize) => Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);

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

		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag) {
			var s = new System.Runtime.InteropServices.ComTypes.STATSTG();

			// s.atime = ???;
			s.cbSize = reader.Length;
			s.clsid = Guid.Empty;
			// s.ctime = ???;
			s.grfLocksSupported = 0;
			s.grfMode = 0;
			s.grfStateBits = 0;
			// s.mtime = ???;
			if ((grfStatFlag & (int)STATFLAG.NONAME) == 0)
				s.pwcsName = name;
			s.reserved = 0;
			s.type = (int)STGTY.STREAM;

			pstatstg = s;
		}

		public void UnlockRegion(long libOffset, long cb, int dwLockType) => Marshal.ThrowExceptionForHR(STG_E_INVALIDFUNCTION);
		public void Write(byte[] pv, int cb, IntPtr pcbWritten) => Marshal.ThrowExceptionForHR(STG_E_CANTSAVE);
		public void Dispose() { }
	}
}
