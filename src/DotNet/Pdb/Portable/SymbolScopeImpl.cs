// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolScopeImpl : SymbolScope {
		internal SymbolMethod method;
		readonly SymbolScopeImpl parent;
		readonly int startOffset;
		readonly int endOffset;
		readonly ReadOnlyCollection<SymbolScope> children;
		readonly ReadOnlyCollection<SymbolVariable> locals;
		internal readonly List<SymbolScope> childrenList;
		internal readonly List<SymbolVariable> localsList;
		internal PdbImportScope importScope;

		public override SymbolMethod Method {
			get {
				if (method != null)
					return method;
				var p = parent;
				if (p == null)
					return method;
				for (;;) {
					if (p.parent == null)
						return method = p.method;
					p = p.parent;
				}
			}
		}

		public override SymbolScope Parent {
			get { return parent; }
		}

		public override int StartOffset {
			get { return startOffset; }
		}

		public override int EndOffset {
			get { return endOffset; }
		}

		public override ReadOnlyCollection<SymbolScope> Children {
			get { return children; }
		}

		public override ReadOnlyCollection<SymbolVariable> Locals {
			get { return locals; }
		}

		public override ReadOnlyCollection<SymbolNamespace> Namespaces {
			get { return emptySymbolNamespaces; }
		}
		static readonly ReadOnlyCollection<SymbolNamespace> emptySymbolNamespaces = new ReadOnlyCollection<SymbolNamespace>(new SymbolNamespace[0]);

		public override PdbImportScope ImportScope {
			get { return importScope; }
		}

		public SymbolScopeImpl(SymbolScopeImpl parent, int startOffset, int endOffset) {
			method = null;
			this.parent = parent;
			this.startOffset = startOffset;
			this.endOffset = endOffset;
			childrenList = new List<SymbolScope>();
			localsList = new List<SymbolVariable>();
			children = new ReadOnlyCollection<SymbolScope>(childrenList);
			locals = new ReadOnlyCollection<SymbolVariable>(localsList);
		}

		IMetaData constantsMetaData;
		uint constantList;
		uint constantListEnd;

		internal void SetConstants(IMetaData metaData, uint constantList, uint constantListEnd) {
			constantsMetaData = metaData;
			this.constantList = constantList;
			this.constantListEnd = constantListEnd;
		}

		public override PdbConstant[] GetConstants(ModuleDef module, GenericParamContext gpContext) {
			if (constantList >= constantListEnd)
				return emptyPdbConstants;
			Debug.Assert(constantsMetaData != null);

			var res = new PdbConstant[constantListEnd - constantList];
			int w = 0;
			for (int i = 0; i < res.Length; i++) {
				uint rid = constantList + (uint)i;
				uint nameOffset;
				uint signature = constantsMetaData.TablesStream.ReadLocalConstantRow2(rid, out nameOffset);
				var name = constantsMetaData.StringsStream.Read(nameOffset);
				using (var stream = constantsMetaData.BlobStream.CreateStream(signature)) {
					var localConstantSigBlobReader = new LocalConstantSigBlobReader(module, stream, gpContext);
					TypeSig type;
					object value;
					bool b = localConstantSigBlobReader.Read(out type, out value);
					Debug.Assert(b);
					if (b)
						res[w++] = new PdbConstant(name, type, value);
					Debug.Assert(stream.Position == stream.Length);
				}
			}
			if (res.Length != w)
				Array.Resize(ref res, w);
			return res;
		}
		static readonly PdbConstant[] emptyPdbConstants = new PdbConstant[0];
	}
}
