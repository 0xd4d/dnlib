// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiFunction : SymbolMethod {
		public override int Token {
			get { return token; }
		}
		internal int token;

		internal PdbReader reader;

		public string Name { get; private set; }
		public PdbAddress Address { get; private set; }
		public DbiScope Root { get; private set; }
		public List<SymbolSequencePoint> Lines {
			get { return lines; }
			set { lines = value; }
		}
		List<SymbolSequencePoint> lines;

		public void Read(IImageStream stream, long recEnd) {
			stream.Position += 4;
			var end = stream.ReadUInt32();
			stream.Position += 4;
			var len = stream.ReadUInt32();
			stream.Position += 8;
			token = stream.ReadInt32();
			Address = PdbAddress.ReadAddress(stream);
			stream.Position += 1 + 2;
			Name = PdbReader.ReadCString(stream);

			stream.Position = recEnd;
			Root = new DbiScope(this, null, "", Address.Offset, len);
			Root.Read(new RecursionCounter(), stream, end);
			FixOffsets(new RecursionCounter(), Root);
		}

		void FixOffsets(RecursionCounter counter, DbiScope scope) {
			if (!counter.Increment())
				return;

			scope.startOffset -= (int)Address.Offset;
			scope.endOffset -= (int)Address.Offset;
			foreach (var child in scope.Children)
				FixOffsets(counter, (DbiScope)child);

			counter.Decrement();
		}

		public override SymbolScope RootScope {
			get { return Root; }
		}

		public override IList<SymbolSequencePoint> SequencePoints {
			get {
				var l = lines;
				if (l == null)
					return emptySymbolSequencePoints;
				return l;
			}
		}
		static readonly SymbolSequencePoint[] emptySymbolSequencePoints = new SymbolSequencePoint[0];

		const string asyncMethodInfoAttributeName = "asyncMethodInfo";
		public int AsyncKickoffMethod {
			get {
				var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
				if (data == null)
					return 0;
				return BitConverter.ToInt32(data, 0);
			}
		}

		public uint? AsyncCatchHandlerILOffset {
			get {
				var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
				if (data == null)
					return null;
				uint token = BitConverter.ToUInt32(data, 4);
				return token == uint.MaxValue ? (uint?)null : token;
			}
		}

		public IList<SymbolAsyncStepInfo> AsyncStepInfos {
			get {
				if (asyncStepInfos == null)
					asyncStepInfos = CreateSymbolAsyncStepInfos();
				return asyncStepInfos;
			}
		}
		volatile SymbolAsyncStepInfo[] asyncStepInfos;

		SymbolAsyncStepInfo[] CreateSymbolAsyncStepInfos() {
			var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
			if (data == null)
				return emptySymbolAsyncStepInfos;
			int pos = 8;
			int count = BitConverter.ToInt32(data, pos);
			pos += 4;
			if (pos + (long)count * 12 > data.Length)
				return emptySymbolAsyncStepInfos;
			if (count == 0)
				return emptySymbolAsyncStepInfos;
			var res = new SymbolAsyncStepInfo[count];
			for (int i = 0; i < res.Length; i++) {
				res[i] = new SymbolAsyncStepInfo(BitConverter.ToUInt32(data, pos), BitConverter.ToUInt32(data, pos + 8), BitConverter.ToUInt32(data, pos + 4));
				pos += 12;
			}
			return res;
		}
		static readonly SymbolAsyncStepInfo[] emptySymbolAsyncStepInfos = new SymbolAsyncStepInfo[0];

		public override void GetCustomDebugInfos(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			reader.GetCustomDebugInfos(this, method, body, result);
		}
	}
}
