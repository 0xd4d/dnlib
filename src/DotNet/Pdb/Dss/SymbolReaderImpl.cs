// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolReaderImpl : SymbolReader {
		ModuleDef module;
		ISymUnmanagedReader reader;
		object[] objsToKeepAlive;

		const int E_FAIL = unchecked((int)0x80004005);

		public SymbolReaderImpl(ISymUnmanagedReader reader, object[] objsToKeepAlive) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			this.objsToKeepAlive = objsToKeepAlive ?? throw new ArgumentNullException(nameof(objsToKeepAlive));
		}

		~SymbolReaderImpl() => Dispose(false);

		public override PdbFileKind PdbFileKind => PdbFileKind.WindowsPDB;

		public override int UserEntryPoint {
			get {
				int hr = reader.GetUserEntryPoint(out uint token);
				if (hr == E_FAIL)
					token = 0;
				else
					Marshal.ThrowExceptionForHR(hr);
				return (int)token;
			}
		}

		public override IList<SymbolDocument> Documents {
			get {
				if (documents is null) {
					reader.GetDocuments(0, out uint numDocs, null);
					var unDocs = new ISymUnmanagedDocument[numDocs];
					reader.GetDocuments((uint)unDocs.Length, out numDocs, unDocs);
					var docs = new SymbolDocument[numDocs];
					for (uint i = 0; i < numDocs; i++)
						docs[i] = new SymbolDocumentImpl(unDocs[i]);
					documents = docs;
				}
				return documents;
			}
		}
		volatile SymbolDocument[] documents;

		public override void Initialize(ModuleDef module) => this.module = module;

		public override SymbolMethod GetMethod(MethodDef method, int version) {
			int hr = reader.GetMethodByVersion(method.MDToken.Raw, version, out var unMethod);
			if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod is null ? null : new SymbolMethodImpl(this, unMethod);
		}

		internal void GetCustomDebugInfos(SymbolMethodImpl symMethod, MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			var asyncMethod = PseudoCustomDebugInfoFactory.TryCreateAsyncMethod(method.Module, method, body, symMethod.AsyncKickoffMethod, symMethod.AsyncStepInfos, symMethod.AsyncCatchHandlerILOffset);
			if (!(asyncMethod is null))
				result.Add(asyncMethod);

			const string CDI_NAME = "MD2";
			reader.GetSymAttribute(method.MDToken.Raw, CDI_NAME, 0, out uint bufSize, null);
			if (bufSize == 0)
				return;
			var cdiData = new byte[bufSize];
			reader.GetSymAttribute(method.MDToken.Raw, CDI_NAME, (uint)cdiData.Length, out bufSize, cdiData);
			PdbCustomDebugInfoReader.Read(method, body, result, cdiData);
		}

		public override void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
			if (token == 0x00000001)
				GetCustomDebugInfos_ModuleDef(result);
		}

		void GetCustomDebugInfos_ModuleDef(IList<PdbCustomDebugInfo> result) {
			var sourceLinkData = GetSourceLinkData();
			if (!(sourceLinkData is null))
				result.Add(new PdbSourceLinkCustomDebugInfo(sourceLinkData));
			var sourceServerData = GetSourceServerData();
			if (!(sourceServerData is null))
				result.Add(new PdbSourceServerCustomDebugInfo(sourceServerData));
		}

		byte[] GetSourceLinkData() {
			if (reader is ISymUnmanagedReader4 reader4) {
				// It returns data that it owns. The data is freed once its Destroy() method is called
				Debug.Assert(reader is ISymUnmanagedDispose);
				// Despite its name, it seems to only return source link data, and not source server data
				if (reader4.GetSourceServerData(out var srcLinkData, out int sizeData) == 0) {
					if (sizeData == 0)
						return Array2.Empty<byte>();
					var data = new byte[sizeData];
					Marshal.Copy(srcLinkData, data, 0, data.Length);
					return data;
				}
			}
			return null;
		}

		byte[] GetSourceServerData() {
			if (reader is ISymUnmanagedSourceServerModule srcSrvModule) {
				var srcSrvData = IntPtr.Zero;
				try {
					// This method only returns source server data, not source link data
					if (srcSrvModule.GetSourceServerData(out int sizeData, out srcSrvData) == 0) {
						if (sizeData == 0)
							return Array2.Empty<byte>();
						var data = new byte[sizeData];
						Marshal.Copy(srcSrvData, data, 0, data.Length);
						return data;
					}
				}
				finally {
					if (srcSrvData != IntPtr.Zero)
						Marshal.FreeCoTaskMem(srcSrvData);
				}
			}
			return null;
		}

		public override void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing) {
			(reader as ISymUnmanagedDispose)?.Destroy();
			var o = objsToKeepAlive;
			if (!(o is null)) {
				foreach (var obj in o)
					(obj as IDisposable)?.Dispose();
			}
			module = null;
			reader = null;
			objsToKeepAlive = null;
		}

		public bool MatchesModule(Guid pdbId, uint stamp, uint age) {
			if (reader is ISymUnmanagedReader4 reader4) {
				int hr = reader4.MatchesModule(pdbId, stamp, age, out bool result);
				if (hr < 0)
					return false;
				return result;
			}

			// There seems to be no other method that can verify that we opened the correct PDB, so return true
			return true;
		}
	}
}
