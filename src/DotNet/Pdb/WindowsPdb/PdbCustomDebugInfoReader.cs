// dnlib: See LICENSE.txt for more info

// C# & Visual Basic compiler's Custom Debug Info is "documented" in source code only, see Roslyn classes:
//	CustomDebugInfoReader, CustomDebugInfoWriter, CustomDebugInfoEncoder

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.Threading;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	/// <summary>
	/// Reads custom debug infos produced by the C# and Visual Basic compilers. They're stored in PDB files
	/// as PDB method custom attributes with the name "MD2".
	/// </summary>
	struct PdbCustomDebugInfoReader : IDisposable {
		/// <summary>
		/// Reads custom debug info
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="body">The method's body. Needs to be provided by the caller since we're called from
		/// PDB-init code when the Body property hasn't been initialized yet</param>
		/// <param name="result">Place all custom debug info in this list</param>
		/// <param name="data">Custom debug info from the PDB file</param>
		public static void Read(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result, byte[] data) {
			try {
				using (var reader = new PdbCustomDebugInfoReader(method, body, MemoryImageStream.Create(data)))
					reader.Read(result);
			}
			catch (ArgumentException) {
			}
			catch (OutOfMemoryException) {
			}
			catch (IOException) {
			}
		}

		readonly ModuleDef module;
		readonly TypeDef typeOpt;
		readonly CilBody bodyOpt;
		readonly GenericParamContext gpContext;
		readonly IBinaryReader reader;

		PdbCustomDebugInfoReader(MethodDef method, CilBody body, IBinaryReader reader) {
			module = method.Module;
			typeOpt = method.DeclaringType;
			bodyOpt = body;
			gpContext = GenericParamContext.Create(method);
			this.reader = reader;
		}

		void Read(IList<PdbCustomDebugInfo> result) {
			if (reader.Length < 4)
				return;
			int version = reader.ReadByte();
			Debug.Assert(version == CustomDebugInfoConstants.Version);
			if (version != CustomDebugInfoConstants.Version)
				return;
			int count = reader.ReadByte();
			reader.Position += 2;

			while (reader.Position + 8 <= reader.Length) {
				int recVersion = reader.ReadByte();
				Debug.Assert(recVersion == CustomDebugInfoConstants.RecordVersion);
				var recKind = (PdbCustomDebugInfoKind)reader.ReadByte();
				reader.Position++;
				int alignmentSize = reader.ReadByte();
				int recSize = reader.ReadInt32();
				if (recSize < 8 || reader.Position - 8 + (uint)recSize > reader.Length)
					return;
				if (recKind <= PdbCustomDebugInfoKind.DynamicLocals)
					alignmentSize = 0;
				if (alignmentSize > 3)
					return;
				var nextRecPos = reader.Position - 8 + recSize;

				if (recVersion == CustomDebugInfoConstants.RecordVersion) {
					var recPosEnd = reader.Position - 8 + recSize - alignmentSize;
					var cdi = ReadRecord(recKind, recPosEnd);
					Debug.Assert(cdi != null);
					Debug.Assert(reader.Position <= recPosEnd);
					if (reader.Position > recPosEnd)
						return;
					if (cdi != null) {
						Debug.Assert(cdi.Kind == recKind);
						result.Add(cdi);
					}
				}

				reader.Position = nextRecPos;
			}
		}

		PdbCustomDebugInfo ReadRecord(PdbCustomDebugInfoKind recKind, long recPosEnd) {
			IMethodDefOrRef method;
			byte[] data;
			Local local;
			int count;
			int localIndex;
			switch (recKind) {
			case PdbCustomDebugInfoKind.UsingGroups:
				count = reader.ReadUInt16();
				if (count < 0)
					return null;
				var usingCountRec = new PdbUsingGroupsCustomDebugInfo(count);
				for (int i = 0; i < count; i++)
					usingCountRec.UsingCounts.Add(reader.ReadUInt16());
				return usingCountRec;

			case PdbCustomDebugInfoKind.ForwardMethodInfo:
				method = module.ResolveToken(reader.ReadUInt32(), gpContext) as IMethodDefOrRef;
				if (method == null)
					return null;
				return new PdbForwardMethodInfoCustomDebugInfo(method);

			case PdbCustomDebugInfoKind.ForwardModuleInfo:
				method = module.ResolveToken(reader.ReadUInt32(), gpContext) as IMethodDefOrRef;
				if (method == null)
					return null;
				return new PdbForwardModuleInfoCustomDebugInfo(method);

			case PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes:
				if (bodyOpt == null)
					return null;
				count = reader.ReadInt32();
				if (count < 0)
					return null;
				var smScope = new PdbStateMachineHoistedLocalScopesCustomDebugInfo(count);
				for (int i = 0; i < count; i++) {
					uint startOffset = reader.ReadUInt32();
					uint endOffset = reader.ReadUInt32();
					if (startOffset > endOffset)
						return null;
					// Try to detect synthesized locals, whose start==end==0. The problem is that endOffset
					// read from the PDB is inclusive (add 1 to get 'end'), so a synthesized local and a
					// local at [0, 1) will be encoded the same {0, 0}.
					if (endOffset == 0)
						smScope.Scopes.Add(new StateMachineHoistedLocalScope());
					else {
						var start = GetInstruction(startOffset);
						var end = GetInstruction(endOffset + 1);
						if (start == null)
							return null;
						smScope.Scopes.Add(new StateMachineHoistedLocalScope(start, end));
					}
				}
				return smScope;

			case PdbCustomDebugInfoKind.StateMachineTypeName:
				var name = ReadUnicodeZ(recPosEnd, needZeroChar: true);
				if (name == null)
					return null;
				var type = GetNestedType(name);
				if (type == null)
					return null;
				return new PdbStateMachineTypeNameCustomDebugInfo(type);

			case PdbCustomDebugInfoKind.DynamicLocals:
				if (bodyOpt == null)
					return null;
				count = reader.ReadInt32();
				const int dynLocalRecSize = 64 + 4 + 4 + 2 * 64;
				if (reader.Position + (long)(uint)count * dynLocalRecSize > recPosEnd)
					return null;
				var dynLocListRec = new PdbDynamicLocalsCustomDebugInfo(count);
				for (int i = 0; i < count; i++) {
					reader.Position += 64;
					int flagsCount = reader.ReadInt32();
					if ((uint)flagsCount > 64)
						return null;
					var dynLocRec = new PdbDynamicLocal(flagsCount);
					var afterPos = reader.Position;

					reader.Position -= 64 + 4;
					for (int j = 0; j < flagsCount; j++)
						dynLocRec.Flags.Add(reader.ReadByte());
					reader.Position = afterPos;

					localIndex = reader.ReadInt32();
					// 'const' locals have index -1 but they're encoded as 0 by Roslyn
					if (localIndex != 0 && (uint)localIndex >= (uint)bodyOpt.Variables.Count)
						return null;

					var nameEndPos = reader.Position + 2 * 64;
					name = ReadUnicodeZ(nameEndPos, needZeroChar: false);
					reader.Position = nameEndPos;

					local = localIndex < bodyOpt.Variables.Count ? bodyOpt.Variables[localIndex] : null;
					// Roslyn writes 0 to localIndex if it's a 'const' local, try to undo that now
					if (localIndex == 0 && local != null && local.Name != name)
						local = null;
					if (local != null && local.Name == name)
						name = null;
					dynLocRec.Name = name;
					dynLocRec.Local = local;
					dynLocListRec.Locals.Add(dynLocRec);
				}
				return dynLocListRec;

			case PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap:
				data = reader.ReadBytes((int)(recPosEnd - reader.Position));
				return new PdbEditAndContinueLocalSlotMapCustomDebugInfo(data);

			case PdbCustomDebugInfoKind.EditAndContinueLambdaMap:
				data = reader.ReadBytes((int)(recPosEnd - reader.Position));
				return new PdbEditAndContinueLambdaMapCustomDebugInfo(data);

			case PdbCustomDebugInfoKind.TupleElementNames:
				if (bodyOpt == null)
					return null;
				count = reader.ReadInt32();
				if (count < 0)
					return null;
				var tupleListRec = new PdbTupleElementNamesCustomDebugInfo(count);
				for (int i = 0; i < count; i++) {
					int nameCount = reader.ReadInt32();
					if ((uint)nameCount >= 10000)
						return null;
					var tupleInfo = new PdbTupleElementNames(nameCount);

					for (int j = 0; j < nameCount; j++) {
						var s = ReadUTF8Z(recPosEnd);
						if (s == null)
							return null;
						tupleInfo.TupleElementNames.Add(s);
					}

					localIndex = reader.ReadInt32();
					uint scopeStart = reader.ReadUInt32();
					uint scopeEnd = reader.ReadUInt32();
					name = ReadUTF8Z(recPosEnd);
					if (name == null)
						return null;
					Debug.Assert(localIndex >= -1);
					// -1 = 'const' local. Only 'const' locals have a scope
					Debug.Assert((localIndex == -1) ^ (scopeStart == 0 && scopeEnd == 0));

					if (localIndex == -1) {
						local = null;
						tupleInfo.ScopeStart = GetInstruction(scopeStart);
						tupleInfo.ScopeEnd = GetInstruction(scopeEnd);
						if (tupleInfo.ScopeStart == null)
							return null;
					}
					else {
						if ((uint)localIndex >= (uint)bodyOpt.Variables.Count)
							return null;
						local = bodyOpt.Variables[localIndex];
					}

					if (local != null && local.Name == name)
						name = null;
					tupleInfo.Local = local;
					tupleInfo.Name = name;

					tupleListRec.Names.Add(tupleInfo);
				}
				return tupleListRec;

			default:
				Debug.Fail("Unknown custom debug info kind: 0x" + ((int)recKind).ToString("X"));
				data = reader.ReadBytes((int)(recPosEnd - reader.Position));
				return new PdbUnknownCustomDebugInfo(recKind, data);
			}
		}

		TypeDef GetNestedType(string name) {
			if (typeOpt == null)
				return null;
			foreach (var type in typeOpt.NestedTypes.GetSafeEnumerable()) {
				if (UTF8String.IsNullOrEmpty(type.Namespace)) {
					if (type.Name == name)
						return type;
					var typeName = type.Name.String;
					if (typeName.StartsWith(name) && typeName.Length >= name.Length + 2) {
						int i = name.Length;
						if (typeName[i] == '`') {
							Debug.Assert(i + 1 < typeName.Length);
							bool ok = true;
							i++;
							while (i < typeName.Length) {
								if (!char.IsDigit(typeName[i])) {
									ok = false;
									break;
								}
								i++;
							}
							if (ok)
								return type;
						}
					}
				}
			}
			return null;
		}

		string ReadUnicodeZ(long recPosEnd, bool needZeroChar) {
			var sb = new StringBuilder();

			for (;;) {
				if (reader.Position >= recPosEnd)
					return needZeroChar ? null : sb.ToString();
				var c = (char)reader.ReadUInt16();
				if (c == 0)
					return sb.ToString();
				sb.Append(c);
			}
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

		public void Dispose() {
			reader.Dispose();
		}
	}
}
