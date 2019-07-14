// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	sealed class SerializerMethodContext {
		readonly Dictionary<Instruction, uint> toOffset;
		readonly IWriterError helper;
		MethodDef method;
		CilBody body;
		uint bodySize;
		bool dictInitd;

		public bool HasBody => !(body is null);

		public SerializerMethodContext(IWriterError helper) {
			toOffset = new Dictionary<Instruction, uint>();
			this.helper = helper;
		}

		internal void SetBody(MethodDef method) {
			if (this.method != method) {
				toOffset.Clear();
				this.method = method;
				body = method?.Body;
				dictInitd = false;
			}
		}

		public uint GetOffset(Instruction instr) {
			if (!dictInitd) {
				Debug.Assert(!(body is null));
				if (body is null)
					return 0;
				InitializeDict();
			}
			if (instr is null)
				return bodySize;
			if (toOffset.TryGetValue(instr, out uint offset))
				return offset;
			helper.Error("Couldn't find an instruction, maybe it was removed. It's still being referenced by some code or by the PDB");
			return bodySize;
		}

		public bool IsSameMethod(MethodDef method) => this.method == method;

		void InitializeDict() {
			Debug.Assert(!(body is null));
			Debug.Assert(toOffset.Count == 0);
			uint offset = 0;
			var instrs = body.Instructions;
			for(int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				toOffset[instr] = offset;
				offset += (uint)instr.GetSize();
			}
			bodySize = offset;
			dictInitd = true;
		}
	}
}
