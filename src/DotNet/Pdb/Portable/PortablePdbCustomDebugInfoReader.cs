// dnlib: See LICENSE.txt for more info

// See Roslyn files: MethodDebugInfo.Portable.cs, MetadataWriter.PortablePdb.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	struct PortablePdbCustomDebugInfoReader : IDisposable {
		public static PdbCustomDebugInfo Read(ModuleDef module, TypeDef typeOpt, CilBody bodyOpt, GenericParamContext gpContext, Guid kind, byte[] data) {
			try {
				using (var reader = new PortablePdbCustomDebugInfoReader(module, typeOpt, bodyOpt, gpContext, MemoryImageStream.Create(data))) {
					var cdi = reader.Read(kind);
					Debug.Assert(reader.reader.Position == reader.reader.Length);
					return cdi;
				}
			}
			catch (ArgumentException) {
			}
			catch (OutOfMemoryException) {
			}
			catch (IOException) {
			}
			return null;
		}

		readonly ModuleDef module;
		readonly TypeDef typeOpt;
		readonly CilBody bodyOpt;
		readonly GenericParamContext gpContext;
		readonly IBinaryReader reader;

		PortablePdbCustomDebugInfoReader(ModuleDef module, TypeDef typeOpt, CilBody bodyOpt, GenericParamContext gpContext, IBinaryReader reader) {
			this.module = module;
			this.typeOpt = typeOpt;
			this.bodyOpt = bodyOpt;
			this.gpContext = gpContext;
			this.reader = reader;
		}

		PdbCustomDebugInfo Read(Guid kind) {
			if (kind == CustomDebugInfoGuids.AsyncMethodSteppingInformationBlob)
				return ReadAsyncMethodSteppingInformationBlob();
			if (kind == CustomDebugInfoGuids.DefaultNamespace)
				return ReadDefaultNamespace();
			if (kind == CustomDebugInfoGuids.DynamicLocalVariables)
				return ReadDynamicLocalVariables(reader.Length);
			if (kind == CustomDebugInfoGuids.EmbeddedSource)
				return ReadEmbeddedSource();
			if (kind == CustomDebugInfoGuids.EncLambdaAndClosureMap)
				return ReadEncLambdaAndClosureMap(reader.Length);
			if (kind == CustomDebugInfoGuids.EncLocalSlotMap)
				return ReadEncLocalSlotMap(reader.Length);
			if (kind == CustomDebugInfoGuids.SourceLink)
				return ReadSourceLink();
			if (kind == CustomDebugInfoGuids.StateMachineHoistedLocalScopes)
				return ReadStateMachineHoistedLocalScopes();
			if (kind == CustomDebugInfoGuids.TupleElementNames)
				return ReadTupleElementNames();
			Debug.Fail("Unknown custom debug info guid: " + kind.ToString());
			return new PdbUnknownCustomDebugInfo(kind, reader.ReadRemainingBytes());
		}

		PdbCustomDebugInfo ReadAsyncMethodSteppingInformationBlob() {
			if (bodyOpt == null)
				return null;
			uint catchHandlerOffset = reader.ReadUInt32() - 1;
			Instruction catchHandler;
			if (catchHandlerOffset == uint.MaxValue)
				catchHandler = null;
			else {
				catchHandler = GetInstruction(catchHandlerOffset);
				Debug.Assert(catchHandler != null);
				if (catchHandler == null)
					return null;
			}
			var asyncInfo = new PdbAsyncMethodSteppingInformationCustomDebugInfo();
			asyncInfo.CatchHandler = catchHandler;
			while (reader.Position < reader.Length) {
				var yieldInstr = GetInstruction(reader.ReadUInt32());
				Debug.Assert(yieldInstr != null);
				if (yieldInstr == null)
					return null;
				uint resumeOffset = reader.ReadUInt32();
				var moveNextRid = reader.ReadCompressedUInt32();
				var moveNextToken = new MDToken(Table.Method, moveNextRid);
				MethodDef moveNextMethod;
				Instruction resumeInstr;
				if (gpContext.Method != null && moveNextToken == gpContext.Method.MDToken) {
					moveNextMethod = gpContext.Method;
					resumeInstr = GetInstruction(resumeOffset);
				}
				else {
					moveNextMethod = module.ResolveToken(moveNextToken, gpContext) as MethodDef;
					Debug.Assert(moveNextMethod != null);
					if (moveNextMethod == null)
						return null;
					resumeInstr = GetInstruction(moveNextMethod, resumeOffset);
				}
				Debug.Assert(resumeInstr != null);
				if (resumeInstr == null)
					return null;
				asyncInfo.AsyncStepInfos.Add(new PdbAsyncStepInfo(yieldInstr, moveNextMethod, resumeInstr));
			}
			return asyncInfo;
		}

		PdbCustomDebugInfo ReadDefaultNamespace() {
			var defaultNs = Encoding.UTF8.GetString(reader.ReadRemainingBytes());
			return new PdbDefaultNamespaceCustomDebugInfo(defaultNs);
		}

		PdbCustomDebugInfo ReadDynamicLocalVariables(long recPosEnd) {
			var flags = new bool[(int)reader.Length * 8];
			int w = 0;
			while (reader.Position < reader.Length) {
				int b = reader.ReadByte();
				for (int i = 1; i < 0x100; i <<= 1)
					flags[w++] = (b & i) != 0;
			}
			return new PdbDynamicLocalVariablesCustomDebugInfo(flags);
		}

		PdbCustomDebugInfo ReadEmbeddedSource() {
			return new PdbEmbeddedSourceCustomDebugInfo(reader.ReadRemainingBytes());
		}

		PdbCustomDebugInfo ReadEncLambdaAndClosureMap(long recPosEnd) {
			var data = reader.ReadBytes((int)(recPosEnd - reader.Position));
			return new PdbEditAndContinueLambdaMapCustomDebugInfo(data);
		}

		PdbCustomDebugInfo ReadEncLocalSlotMap(long recPosEnd) {
			var data = reader.ReadBytes((int)(recPosEnd - reader.Position));
			return new PdbEditAndContinueLocalSlotMapCustomDebugInfo(data);
		}

		PdbCustomDebugInfo ReadSourceLink() {
			return new PdbSourceLinkCustomDebugInfo(reader.ReadRemainingBytes());
		}

		PdbCustomDebugInfo ReadStateMachineHoistedLocalScopes() {
			if (bodyOpt == null)
				return null;
			int count = (int)(reader.Length / 8);
			var smScope = new PdbStateMachineHoistedLocalScopesCustomDebugInfo(count);
			for (int i = 0; i < count; i++) {
				uint startOffset = reader.ReadUInt32();
				uint length = reader.ReadUInt32();
				if (startOffset == 0 && length == 0)
					smScope.Scopes.Add(new StateMachineHoistedLocalScope());
				else {
					var start = GetInstruction(startOffset);
					var end = GetInstruction(startOffset + length);
					Debug.Assert(start != null);
					if (start == null)
						return null;
					smScope.Scopes.Add(new StateMachineHoistedLocalScope(start, end));
				}
			}
			return smScope;
		}

		PdbCustomDebugInfo ReadTupleElementNames() {
			var tupleListRec = new PortablePdbTupleElementNamesCustomDebugInfo();
			while (reader.Position < reader.Length) {
				var name = ReadUTF8Z(reader.Length);
				tupleListRec.Names.Add(name);
			}
			return tupleListRec;
		}

		string ReadUTF8Z(long recPosEnd) {
			if (reader.Position > recPosEnd)
				return null;
			var bytes = reader.ReadBytesUntilByte(0);
			if (bytes == null)
				return null;
			var s = Encoding.UTF8.GetString(bytes);
			reader.Position++;
			return s;
		}

		Instruction GetInstruction(uint offset) {
			var instructions = bodyOpt.Instructions;
			int lo = 0, hi = instructions.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;
				var instr = instructions[i];
				if (instr.Offset == offset)
					return instr;
				if (offset < instr.Offset)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return null;
		}

		static Instruction GetInstruction(MethodDef method, uint offset) {
			if (method == null)
				return null;
			var body = method.Body;
			if (body == null)
				return null;
			var instructions = body.Instructions;
			int lo = 0, hi = instructions.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;
				var instr = instructions[i];
				if (instr.Offset == offset)
					return instr;
				if (offset < instr.Offset)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return null;
		}

		public void Dispose() {
			reader.Dispose();
		}
	}
}
