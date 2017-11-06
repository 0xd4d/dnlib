// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolMethodImpl : SymbolMethod {
		readonly int token;
		readonly SymbolScope rootScope;
		readonly SymbolSequencePoint[] sequencePoints;
		readonly int iteratorKickoffMethod;
		readonly int asyncKickoffMethod;
		readonly uint? asyncCatchHandlerILOffset;
		readonly SymbolAsyncStepInfo[] asyncStepInfos;

		public override int Token {
			get { return token; }
		}

		public override SymbolScope RootScope {
			get { return rootScope; }
		}

		public override IList<SymbolSequencePoint> SequencePoints {
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

		public override IList<SymbolAsyncStepInfo> AsyncStepInfos {
			get { return asyncStepInfos; }
		}

		public SymbolMethodImpl(int token, SymbolScope rootScope, SymbolSequencePoint[] sequencePoints, int iteratorKickoffMethod, int asyncKickoffMethod, uint? asyncCatchHandlerILOffset, SymbolAsyncStepInfo[] asyncStepInfos) {
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
