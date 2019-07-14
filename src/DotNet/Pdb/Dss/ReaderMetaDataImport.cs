// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Pdb.Dss {
	sealed unsafe class ReaderMetaDataImport : MetaDataImport, IDisposable {
		Metadata metadata;
		byte* blobPtr;
		IntPtr addrToFree;

		public ReaderMetaDataImport(Metadata metadata) {
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			var reader = metadata.BlobStream.CreateReader();
			addrToFree = Marshal.AllocHGlobal((int)reader.BytesLeft);
			blobPtr = (byte*)addrToFree;
			if (blobPtr is null)
				throw new OutOfMemoryException();
			reader.ReadBytes(blobPtr, (int)reader.BytesLeft);
		}

		~ReaderMetaDataImport() => Dispose(false);

		public override void GetTypeRefProps(uint tr, uint* ptkResolutionScope, ushort* szName, uint cchName, uint* pchName) {
			var token = new MDToken(tr);
			if (token.Table != Table.TypeRef)
				throw new ArgumentException();
			if (!metadata.TablesStream.TryReadTypeRefRow(token.Rid, out var row))
				throw new ArgumentException();
			if (!(ptkResolutionScope is null))
				*ptkResolutionScope = row.ResolutionScope;
			if (!(szName is null) || !(pchName is null)) {
				var typeNamespace = metadata.StringsStream.ReadNoNull(row.Namespace);
				var typeName = metadata.StringsStream.ReadNoNull(row.Name);
				CopyTypeName(typeNamespace, typeName, szName, cchName, pchName);
			}
		}

		public override void GetTypeDefProps(uint td, ushort* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends) {
			var token = new MDToken(td);
			if (token.Table != Table.TypeDef)
				throw new ArgumentException();
			if (!metadata.TablesStream.TryReadTypeDefRow(token.Rid, out var row))
				throw new ArgumentException();
			if (!(pdwTypeDefFlags is null))
				*pdwTypeDefFlags = row.Flags;
			if (!(ptkExtends is null))
				*ptkExtends = row.Extends;
			if (!(szTypeDef is null) || !(pchTypeDef is null)) {
				var typeNamespace = metadata.StringsStream.ReadNoNull(row.Namespace);
				var typeName = metadata.StringsStream.ReadNoNull(row.Name);
				CopyTypeName(typeNamespace, typeName, szTypeDef, cchTypeDef, pchTypeDef);
			}
		}

		public override void GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig) {
			var token = new MDToken(mdSig);
			if (token.Table != Table.StandAloneSig)
				throw new ArgumentException();
			if (!metadata.TablesStream.TryReadStandAloneSigRow(token.Rid, out var row))
				throw new ArgumentException();
			if (!metadata.BlobStream.TryCreateReader(row.Signature, out var reader))
				throw new ArgumentException();
			if (!(ppvSig is null))
				*ppvSig = blobPtr + (reader.StartOffset - (uint)metadata.BlobStream.StartOffset);
			if (!(pcbSig is null))
				*pcbSig = reader.Length;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing) {
			metadata = null;
			var addrToFreeTmp = Interlocked.Exchange(ref addrToFree, IntPtr.Zero);
			blobPtr = null;
			if (addrToFreeTmp != IntPtr.Zero)
				Marshal.FreeHGlobal(addrToFreeTmp);
		}
	}
}
