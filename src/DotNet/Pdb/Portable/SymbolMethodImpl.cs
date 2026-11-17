// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolMethodImpl : SymbolMethod {
		readonly PortablePdbReader reader;
		readonly int token;
		readonly SymbolScope rootScope;
		readonly SymbolSequencePoint[] sequencePoints;
		readonly int kickoffMethod;

		public override int Token => token;
		public override SymbolScope RootScope => rootScope;
		public override IList<SymbolSequencePoint> SequencePoints => sequencePoints;
		public int KickoffMethod => kickoffMethod;

		public SymbolMethodImpl(PortablePdbReader reader, int token, SymbolScope rootScope, SymbolSequencePoint[] sequencePoints, int kickoffMethod) {
			this.reader = reader;
			this.token = token;
			this.rootScope = rootScope;
			this.sequencePoints = sequencePoints;
			this.kickoffMethod = kickoffMethod;
		}

		public override void GetCustomDebugInfos(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) =>
			reader.GetCustomDebugInfos(this, method, body, result);
	}
}
