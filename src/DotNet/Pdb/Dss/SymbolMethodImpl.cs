// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Threading;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolMethodImpl : SymbolMethod {
		readonly SymbolReaderImpl reader;
		readonly ISymUnmanagedMethod method;
		readonly ISymUnmanagedAsyncMethod asyncMethod;

		public SymbolMethodImpl(SymbolReaderImpl reader, ISymUnmanagedMethod method) {
			this.reader = reader;
			this.method = method;
			asyncMethod = method as ISymUnmanagedAsyncMethod;
		}

		public override int Token {
			get {
				uint result;
				method.GetToken(out result);
				return (int)result;
			}
		}

		public override SymbolScope RootScope {
			get {
				if (rootScope == null) {
					ISymUnmanagedScope scope;
					method.GetRootScope(out scope);
					Interlocked.CompareExchange(ref rootScope, scope == null ? null : new SymbolScopeImpl(scope, this, null), null);
				}
				return rootScope;
			}
		}
		volatile SymbolScope rootScope;

		public override IList<SymbolSequencePoint> SequencePoints {
			get {
				if (sequencePoints == null) {
					uint seqPointCount;
					method.GetSequencePointCount(out seqPointCount);
					var seqPoints = new SymbolSequencePoint[seqPointCount];

					int[] offsets = new int[seqPoints.Length];
					ISymbolDocument[] documents = new ISymbolDocument[seqPoints.Length];
					int[] lines = new int[seqPoints.Length];
					int[] columns = new int[seqPoints.Length];
					int[] endLines = new int[seqPoints.Length];
					int[] endColumns = new int[seqPoints.Length];
					var unDocs = new ISymUnmanagedDocument[seqPoints.Length];
					if (seqPoints.Length != 0) {
						uint size;
						method.GetSequencePoints((uint)seqPoints.Length, out size, offsets, unDocs, lines, columns, endLines, endColumns);
					}
					for (int i = 0; i < seqPoints.Length; i++) {
						seqPoints[i] = new SymbolSequencePoint {
							Offset = offsets[i],
							Document = new SymbolDocumentImpl(unDocs[i]),
							Line = lines[i],
							Column = columns[i],
							EndLine = endLines[i],
							EndColumn = endColumns[i],
						};
					}
					sequencePoints = seqPoints;
				}
				return sequencePoints;
			}
		}
		volatile SymbolSequencePoint[] sequencePoints;

		public int AsyncKickoffMethod {
			get {
				if (asyncMethod == null || !asyncMethod.IsAsyncMethod())
					return 0;
				return (int)asyncMethod.GetKickoffMethod();
			}
		}

		public uint? AsyncCatchHandlerILOffset {
			get {
				if (asyncMethod == null || !asyncMethod.IsAsyncMethod())
					return null;
				if (!asyncMethod.HasCatchHandlerILOffset())
					return null;
				return asyncMethod.GetCatchHandlerILOffset();
			}
		}

		public IList<SymbolAsyncStepInfo> AsyncStepInfos {
			get {
				if (asyncMethod == null || !asyncMethod.IsAsyncMethod())
					return null;
				if (asyncStepInfos == null) {
					var stepInfoCount = asyncMethod.GetAsyncStepInfoCount();
					var yieldOffsets = new uint[stepInfoCount];
					var breakpointOffsets = new uint[stepInfoCount];
					var breakpointMethods = new uint[stepInfoCount];
					asyncMethod.GetAsyncStepInfo(stepInfoCount, out stepInfoCount, yieldOffsets, breakpointOffsets, breakpointMethods);
					var res = new SymbolAsyncStepInfo[stepInfoCount];
					for (int i = 0; i < res.Length; i++)
						res[i] = new SymbolAsyncStepInfo(yieldOffsets[i], breakpointOffsets[i], breakpointMethods[i]);
					asyncStepInfos = res;
				}
				return asyncStepInfos;
			}
		}
		volatile SymbolAsyncStepInfo[] asyncStepInfos;

		public override void GetCustomDebugInfos(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			reader.GetCustomDebugInfos(this, method, body, result);
		}
	}
}
