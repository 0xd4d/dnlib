// dnlib: See LICENSE.txt for more info

using System.Collections.ObjectModel;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolMethodImpl : SymbolMethod {
		readonly int token;
		readonly SymbolScope rootScope;
		readonly ReadOnlyCollection<SymbolSequencePoint> sequencePoints;
		readonly int iteratorKickoffMethod;
		readonly int asyncKickoffMethod;
		readonly uint? asyncCatchHandlerILOffset;
		readonly ReadOnlyCollection<SymbolAsyncStepInfo> asyncStepInfos;

		public override int Token {
			get { return token; }
		}

		public override SymbolScope RootScope {
			get { return rootScope; }
		}

		public override ReadOnlyCollection<SymbolSequencePoint> SequencePoints {
			get { return sequencePoints; }
		}

		public override int IteratorKickoffMethod {
			get { return iteratorKickoffMethod; }
		}

		public override int AsyncKickoffMethod {
			get { return asyncKickoffMethod; }
		}

		public override uint? AsyncCatchHandlerILOffset {
			get { return asyncCatchHandlerILOffset; }
		}

		public override ReadOnlyCollection<SymbolAsyncStepInfo> AsyncStepInfos {
			get { return asyncStepInfos; }
		}

		public SymbolMethodImpl(int token, SymbolScope rootScope, ReadOnlyCollection<SymbolSequencePoint> sequencePoints, int iteratorKickoffMethod, int asyncKickoffMethod, uint? asyncCatchHandlerILOffset, ReadOnlyCollection<SymbolAsyncStepInfo> asyncStepInfos) {
			this.token = token;
			this.rootScope = rootScope;
			this.sequencePoints = sequencePoints;
			this.iteratorKickoffMethod = iteratorKickoffMethod;
			this.asyncKickoffMethod = asyncKickoffMethod;
			this.asyncCatchHandlerILOffset = asyncCatchHandlerILOffset;
			this.asyncStepInfos = asyncStepInfos;
		}
	}
}
