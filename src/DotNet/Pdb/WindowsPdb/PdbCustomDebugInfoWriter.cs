// dnlib: See LICENSE.txt for more info

// C# & Visual Basic compiler's Custom Debug Info is "documented" in source code only, see Roslyn classes:
//	CustomDebugInfoReader, CustomDebugInfoWriter, CustomDebugInfoEncoder

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	sealed class PdbCustomDebugInfoWriterContext {
		public ILogger Logger;
		public readonly MemoryStream MemoryStream;
		public readonly BinaryWriter Writer;
		public readonly Dictionary<Instruction, uint> InstructionToOffsetDict;

		public PdbCustomDebugInfoWriterContext() {
			MemoryStream = new MemoryStream();
			Writer = new BinaryWriter(MemoryStream);
			InstructionToOffsetDict = new Dictionary<Instruction, uint>();
		}
	}

	/// <summary>
	/// Writes custom debug infos produced by the C# and Visual Basic compilers. They're stored in PDB files
	/// as PDB method custom attributes with the name "MD2".
	/// </summary>
	struct PdbCustomDebugInfoWriter {
		readonly MetaData metaData;
		readonly MethodDef method;
		readonly ILogger logger;
		readonly MemoryStream memoryStream;
		readonly BinaryWriter writer;
		readonly Dictionary<Instruction, uint> instructionToOffsetDict;
		uint bodySize;
		bool instructionToOffsetDictInitd;

		/// <summary>
		/// Returns the raw custom debug info or null if there was an error
		/// </summary>
		/// <param name="metaData">Metadata</param>
		/// <param name="context">Writer context</param>
		/// <param name="method">Method</param>
		/// <param name="customDebugInfos">Custom debug infos to write</param>
		/// <returns></returns>
		public static byte[] Write(MetaData metaData, MethodDef method, PdbCustomDebugInfoWriterContext context, IList<PdbCustomDebugInfo> customDebugInfos) {
			var writer = new PdbCustomDebugInfoWriter(metaData, method, context);
			return writer.Write(customDebugInfos);
		}

		PdbCustomDebugInfoWriter(MetaData metaData, MethodDef method, PdbCustomDebugInfoWriterContext context) {
			this.metaData = metaData;
			this.method = method;
			this.logger = context.Logger;
			this.memoryStream = context.MemoryStream;
			this.writer = context.Writer;
			this.instructionToOffsetDict = context.InstructionToOffsetDict;
			this.bodySize = 0;
			this.instructionToOffsetDictInitd = false;
			memoryStream.SetLength(0);
			memoryStream.Position = 0;
		}

		void InitializeInstructionDictionary() {
			Debug.Assert(!instructionToOffsetDictInitd);
			instructionToOffsetDict.Clear();
			var body = method.Body;
			if (body == null)
				return;
			var instrs = body.Instructions;
			uint offset = 0;
			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				instructionToOffsetDict[instr] = offset;
				offset += (uint)instr.GetSize();
			}
			bodySize = offset;
			instructionToOffsetDictInitd = true;
		}

		uint GetInstructionOffset(Instruction instr, bool nullIsEndOfMethod) {
			if (!instructionToOffsetDictInitd)
				InitializeInstructionDictionary();
			if (instr == null) {
				if (nullIsEndOfMethod)
					return bodySize;
				Error("Instruction is null");
				return uint.MaxValue;
			}
			uint offset;
			if (instructionToOffsetDict.TryGetValue(instr, out offset))
				return offset;
			Error("Instruction is missing in body but it's still being referenced by PDB data. Method {0} (0x{1:X8}), instruction: {2}", method, method.MDToken.Raw, instr);
			return uint.MaxValue;
		}

		void Error(string message, params object[] args) {
			logger.Log(this, LoggerEvent.Error, message, args);
		}

		byte[] Write(IList<PdbCustomDebugInfo> customDebugInfos) {
			if (customDebugInfos.Count == 0)
				return null;
			if (customDebugInfos.Count > byte.MaxValue) {
				Error("Too many custom debug infos. Count must be <= 255");
				return null;
			}

			writer.Write((byte)CustomDebugInfoConstants.Version);
			writer.Write((byte)customDebugInfos.Count);
			writer.Write((ushort)0);

			for (int i = 0; i < customDebugInfos.Count; i++) {
				var info = customDebugInfos[i];
				if (info == null) {
					Error("Custom debug info is null");
					return null;
				}
				if ((uint)info.Kind > byte.MaxValue) {
					Error("Invalid custom debug info kind");
					return null;
				}

				var recordPos = writer.BaseStream.Position;
				writer.Write((byte)CustomDebugInfoConstants.RecordVersion);
				writer.Write((byte)info.Kind);
				writer.Write((ushort)0);
				writer.Write((uint)0);

				int count, j, k;
				uint token;
				switch (info.Kind) {
				case PdbCustomDebugInfoKind.UsingGroups:
					var usingRec = info as PdbUsingGroupsCustomDebugInfo;
					if (usingRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = usingRec.UsingCounts.Count;
					if (count > ushort.MaxValue) {
						Error("UsingCounts contains more than 0xFFFF elements");
						return null;
					}
					writer.Write((ushort)count);
					for (j = 0; j < count; j++)
						writer.Write(usingRec.UsingCounts[j]);
					break;

				case PdbCustomDebugInfoKind.ForwardMethodInfo:
					var fwdMethodRec = info as PdbForwardMethodInfoCustomDebugInfo;
					if (fwdMethodRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					token = GetMethodToken(fwdMethodRec.Method);
					if (token == 0)
						return null;
					writer.Write(token);
					break;

				case PdbCustomDebugInfoKind.ForwardModuleInfo:
					var fwdModRec = info as PdbForwardModuleInfoCustomDebugInfo;
					if (fwdModRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					token = GetMethodToken(fwdModRec.Method);
					if (token == 0)
						return null;
					writer.Write(token);
					break;

				case PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes:
					var smLocalScopesRec = info as PdbStateMachineHoistedLocalScopesCustomDebugInfo;
					if (smLocalScopesRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = smLocalScopesRec.Scopes.Count;
					writer.Write(count);
					for (j = 0; j < count; j++) {
						var scope = smLocalScopesRec.Scopes[j];
						if (scope.IsSynthesizedLocal) {
							writer.Write(0);
							writer.Write(0);
						}
						else {
							writer.Write(GetInstructionOffset(scope.Start, nullIsEndOfMethod: false));
							writer.Write(GetInstructionOffset(scope.End, nullIsEndOfMethod: true) - 1);
						}
					}
					break;

				case PdbCustomDebugInfoKind.StateMachineTypeName:
					var smTypeRec = info as PdbStateMachineTypeNameCustomDebugInfo;
					if (smTypeRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					var type = smTypeRec.Type;
					if (type == null) {
						Error("State machine type is null");
						return null;
					}
					WriteUnicodeZ(MetadataNameToRoslynName(type.Name));
					break;

				case PdbCustomDebugInfoKind.DynamicLocals:
					var dynLocListRec = info as PdbDynamicLocalsCustomDebugInfo;
					if (dynLocListRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = dynLocListRec.Locals.Count;
					writer.Write(count);
					for (j = 0; j < count; j++) {
						var dynLoc = dynLocListRec.Locals[j];
						if (dynLoc == null) {
							Error("Dynamic local is null");
							return null;
						}
						if (dynLoc.Flags.Count > 64) {
							Error("Dynamic local flags is longer than 64 bytes");
							return null;
						}
						var name = dynLoc.Name;
						if (name == null)
							name = string.Empty;
						if (name.Length > 64) {
							Error("Dynamic local name is longer than 64 chars");
							return null;
						}
						if (name.IndexOf('\0') >= 0) {
							Error("Dynamic local name contains a NUL char");
							return null;
						}

						for (k = 0; k < dynLoc.Flags.Count; k++)
							writer.Write(dynLoc.Flags[k]);
						while (k++ < 64)
							writer.Write((byte)0);
						writer.Write(dynLoc.Flags.Count);

						if (dynLoc.Local == null)
							writer.Write(0);
						else
							writer.Write(dynLoc.Local.Index);

						for (k = 0; k < name.Length; k++)
							writer.Write((ushort)name[k]);
						while (k++ < 64)
							writer.Write((ushort)0);
					}
					break;

				case PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap:
					var encLocalMapRec = info as PdbEditAndContinueLocalSlotMapCustomDebugInfo;
					if (encLocalMapRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					writer.Write(encLocalMapRec.Data);
					break;

				case PdbCustomDebugInfoKind.EditAndContinueLambdaMap:
					var encLambdaRec = info as PdbEditAndContinueLambdaMapCustomDebugInfo;
					if (encLambdaRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					writer.Write(encLambdaRec.Data);
					break;

				case PdbCustomDebugInfoKind.TupleElementNames:
					var tupleListRec = info as PdbTupleElementNamesCustomDebugInfo;
					if (tupleListRec == null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = tupleListRec.Names.Count;
					writer.Write(count);
					for (j = 0; j < count; j++) {
						var tupleInfo = tupleListRec.Names[j];
						if (tupleInfo == null) {
							Error("Tuple name info is null");
							return null;
						}
						writer.Write(tupleInfo.TupleElementNames.Count);
						for (k = 0; k < tupleInfo.TupleElementNames.Count; k++)
							WriteUTF8Z(tupleInfo.TupleElementNames[k]);

						if (tupleInfo.Local == null) {
							writer.Write(-1);
							writer.Write(GetInstructionOffset(tupleInfo.ScopeStart, nullIsEndOfMethod: false));
							writer.Write(GetInstructionOffset(tupleInfo.ScopeEnd, nullIsEndOfMethod: true));
						}
						else {
							writer.Write(tupleInfo.Local.Index);
							writer.Write(0L);
						}
						WriteUTF8Z(tupleInfo.Name);
					}
					break;

				default:
					var unkRec = info as PdbUnknownCustomDebugInfo;
					if (unkRec == null) {
						Error("Unsupported custom debug info class {0}", info.GetType());
						return null;
					}
					writer.Write(unkRec.Data);
					break;
				}

				var pos = writer.BaseStream.Position;
				var recLen = (pos - recordPos);
				var alignedLen = (recLen + 3) & ~3;
				if (alignedLen > uint.MaxValue) {
					Error("Custom debug info record is too big");
					return null;
				}
				writer.BaseStream.Position = recordPos + 3;
				if (info.Kind <= PdbCustomDebugInfoKind.DynamicLocals)
					writer.Write((byte)0);
				else
					writer.Write((byte)(alignedLen - recLen));
				writer.Write((uint)alignedLen);

				writer.BaseStream.Position = pos;
				while (writer.BaseStream.Position < recordPos + alignedLen)
					writer.Write((byte)0);
			}

			return memoryStream.ToArray();
		}

		string MetadataNameToRoslynName(string name) {
			if (name == null)
				return name;
			int index = name.LastIndexOf('`');
			if (index < 0)
				return name;
			return name.Substring(0, index);
		}

		void WriteUnicodeZ(string s) {
			if (s == null) {
				Error("String is null");
				return;
			}

			if (s.IndexOf('\0') >= 0) {
				Error("String contains a NUL char: {0}", s);
				return;
			}

			for (int i = 0; i < s.Length; i++)
				writer.Write((ushort)s[i]);
			writer.Write((ushort)0);
		}

		void WriteUTF8Z(string s) {
			if (s == null) {
				Error("String is null");
				return;
			}

			if (s.IndexOf('\0') >= 0) {
				Error("String contains a NUL char: {0}", s);
				return;
			}

			writer.Write(Encoding.UTF8.GetBytes(s));
			writer.Write((byte)0);
		}

		uint GetMethodToken(IMethodDefOrRef method) {
			if (method == null) {
				Error("Method is null");
				return 0;
			}

			var md = method as MethodDef;
			if (md != null) {
				uint rid = metaData.GetRid(md);
				if (rid == 0) {
					Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, metaData.Module);
					return 0;
				}
				return new MDToken(md.MDToken.Table, rid).Raw;
			}

			var mr = method as MemberRef;
			if (mr != null && mr.IsMethodRef)
				return metaData.GetToken(mr).Raw;

			Error("Not a method");
			return 0;
		}
	}
}
