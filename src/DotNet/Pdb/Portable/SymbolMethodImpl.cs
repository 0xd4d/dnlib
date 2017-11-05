// dnlib: See LICENSE.txt for more info

using System.Collections.ObjectModel;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolMethodImpl : SymbolMethod {
		readonly int token;
		readonly SymbolScope rootScope;
		readonly ReadOnlyCollection<SymbolSequencePoint> sequencePoints;
		readonly bool isAsyncMethod;
		readonly int kickoffMethod;
		readonly uint? catchHandlerILOffset;
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

		public override bool IsAsyncMethod {
			get { return isAsyncMethod; }
		}

		public override int KickoffMethod {
			get { return kickoffMethod; }
		}

		public override uint? CatchHandlerILOffset {
			get { return catchHandlerILOffset; }
		}

		public override ReadOnlyCollection<SymbolAsyncStepInfo> AsyncStepInfos {
			get { return asyncStepInfos; }
		}

		public SymbolMethodImpl(int token, SymbolScope rootScope, ReadOnlyCollection<SymbolSequencePoint> sequencePoints, bool isAsyncMethod, int kickoffMethod, uint? catchHandlerILOffset, ReadOnlyCollection<SymbolAsyncStepInfo> asyncStepInfos) {
			this.token = token;
			this.rootScope = rootScope;
			this.sequencePoints = sequencePoints;
			this.isAsyncMethod = isAsyncMethod;
			this.kickoffMethod = kickoffMethod;
			this.catchHandlerILOffset = catchHandlerILOffset;
			this.asyncStepInfos = asyncStepInfos;
		}
	}
}
