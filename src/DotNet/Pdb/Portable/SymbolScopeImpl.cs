// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolScopeImpl : SymbolScope {
		readonly PortablePdbReader owner;
		internal SymbolMethod method;
		readonly SymbolScopeImpl parent;
		readonly int startOffset;
		readonly int endOffset;
		internal readonly List<SymbolScope> childrenList;
		internal readonly List<SymbolVariable> localsList;
		internal PdbImportScope importScope;
		readonly PdbCustomDebugInfo[] customDebugInfos;

		public override SymbolMethod Method {
			get {
				if (!(method is null))
					return method;
				var p = parent;
				if (p is null)
					return method;
				for (;;) {
					if (p.parent is null)
						return method = p.method;
					p = p.parent;
				}
			}
		}

		public override SymbolScope Parent => parent;
		public override int StartOffset => startOffset;
		public override int EndOffset => endOffset;
		public override IList<SymbolScope> Children => childrenList;
		public override IList<SymbolVariable> Locals => localsList;
		public override IList<SymbolNamespace> Namespaces => Array2.Empty<SymbolNamespace>();
		public override IList<PdbCustomDebugInfo> CustomDebugInfos => customDebugInfos;
		public override PdbImportScope ImportScope => importScope;

		public SymbolScopeImpl(PortablePdbReader owner, SymbolScopeImpl parent, int startOffset, int endOffset, PdbCustomDebugInfo[] customDebugInfos) {
			this.owner = owner;
			method = null;
			this.parent = parent;
			this.startOffset = startOffset;
			this.endOffset = endOffset;
			childrenList = new List<SymbolScope>();
			localsList = new List<SymbolVariable>();
			this.customDebugInfos = customDebugInfos;
		}

		Metadata constantsMetadata;
		uint constantList;
		uint constantListEnd;

		internal void SetConstants(Metadata metadata, uint constantList, uint constantListEnd) {
			constantsMetadata = metadata;
			this.constantList = constantList;
			this.constantListEnd = constantListEnd;
		}

		public override IList<PdbConstant> GetConstants(ModuleDef module, GenericParamContext gpContext) {
			if (constantList >= constantListEnd)
				return Array2.Empty<PdbConstant>();
			Debug.Assert(!(constantsMetadata is null));

			var res = new PdbConstant[constantListEnd - constantList];
			int w = 0;
			for (int i = 0; i < res.Length; i++) {
				uint rid = constantList + (uint)i;
				bool b = constantsMetadata.TablesStream.TryReadLocalConstantRow(rid, out var row);
				Debug.Assert(b);
				var name = constantsMetadata.StringsStream.Read(row.Name);
				if (!constantsMetadata.BlobStream.TryCreateReader(row.Signature, out var reader))
					continue;
				var localConstantSigBlobReader = new LocalConstantSigBlobReader(module, ref reader, gpContext);
				b = localConstantSigBlobReader.Read(out var type, out object value);
				Debug.Assert(b);
				if (b) {
					var pdbConstant = new PdbConstant(name, type, value);
					int token = new MDToken(Table.LocalConstant, rid).ToInt32();
					owner.GetCustomDebugInfos(token, gpContext, pdbConstant.CustomDebugInfos);
					res[w++] = pdbConstant;
				}
			}
			if (res.Length != w)
				Array.Resize(ref res, w);
			return res;
		}
	}
}
