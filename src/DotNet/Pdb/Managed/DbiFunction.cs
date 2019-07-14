// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiFunction : SymbolMethod {
		public override int Token => token;
		internal int token;

		internal PdbReader reader;

		public string Name { get; private set; }
		public PdbAddress Address { get; private set; }
		public DbiScope Root { get; private set; }
		public List<SymbolSequencePoint> Lines {
			get => lines;
			set => lines = value;
		}
		List<SymbolSequencePoint> lines;

		public void Read(ref DataReader reader, uint recEnd) {
			reader.Position += 4;
			var end = reader.ReadUInt32();
			reader.Position += 4;
			var len = reader.ReadUInt32();
			reader.Position += 8;
			token = reader.ReadInt32();
			Address = PdbAddress.ReadAddress(ref reader);
			reader.Position += 1 + 2;
			Name = PdbReader.ReadCString(ref reader);

			reader.Position = recEnd;
			Root = new DbiScope(this, null, "", Address.Offset, len);
			Root.Read(new RecursionCounter(), ref reader, end);
			FixOffsets(new RecursionCounter(), Root);
		}

		void FixOffsets(RecursionCounter counter, DbiScope scope) {
			if (!counter.Increment())
				return;

			scope.startOffset -= (int)Address.Offset;
			scope.endOffset -= (int)Address.Offset;
			var children = scope.Children;
			int count = children.Count;
			for (int i = 0; i < count; i++)
				FixOffsets(counter, (DbiScope)children[i]);

			counter.Decrement();
		}

		public override SymbolScope RootScope => Root;

		public override IList<SymbolSequencePoint> SequencePoints {
			get {
				var l = lines;
				if (l is null)
					return Array2.Empty<SymbolSequencePoint>();
				return l;
			}
		}

		const string asyncMethodInfoAttributeName = "asyncMethodInfo";
		public int AsyncKickoffMethod {
			get {
				var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
				if (data is null || data.Length < 4)
					return 0;
				return BitConverter.ToInt32(data, 0);
			}
		}

		public uint? AsyncCatchHandlerILOffset {
			get {
				var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
				if (data is null || data.Length < 8)
					return null;
				uint token = BitConverter.ToUInt32(data, 4);
				return token == uint.MaxValue ? (uint?)null : token;
			}
		}

		public IList<SymbolAsyncStepInfo> AsyncStepInfos {
			get {
				if (asyncStepInfos is null)
					asyncStepInfos = CreateSymbolAsyncStepInfos();
				return asyncStepInfos;
			}
		}
		volatile SymbolAsyncStepInfo[] asyncStepInfos;

		SymbolAsyncStepInfo[] CreateSymbolAsyncStepInfos() {
			var data = Root.GetSymAttribute(asyncMethodInfoAttributeName);
			if (data is null || data.Length < 12)
				return Array2.Empty<SymbolAsyncStepInfo>();
			int pos = 8;
			int count = BitConverter.ToInt32(data, pos);
			pos += 4;
			if (pos + (long)count * 12 > data.Length)
				return Array2.Empty<SymbolAsyncStepInfo>();
			if (count == 0)
				return Array2.Empty<SymbolAsyncStepInfo>();
			var res = new SymbolAsyncStepInfo[count];
			for (int i = 0; i < res.Length; i++) {
				res[i] = new SymbolAsyncStepInfo(BitConverter.ToUInt32(data, pos), BitConverter.ToUInt32(data, pos + 8), BitConverter.ToUInt32(data, pos + 4));
				pos += 12;
			}
			return res;
		}

		public override void GetCustomDebugInfos(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) =>
			reader.GetCustomDebugInfos(this, method, body, result);
	}
}
