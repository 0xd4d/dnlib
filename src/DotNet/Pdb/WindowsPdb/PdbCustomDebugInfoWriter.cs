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
		public readonly DataWriter Writer;
		public readonly Dictionary<Instruction, uint> InstructionToOffsetDict;

		public PdbCustomDebugInfoWriterContext() {
			MemoryStream = new MemoryStream();
			Writer = new DataWriter(MemoryStream);
			InstructionToOffsetDict = new Dictionary<Instruction, uint>();
		}
	}

	/// <summary>
	/// Writes custom debug infos produced by the C# and Visual Basic compilers. They're stored in PDB files
	/// as PDB method custom attributes with the name "MD2".
	/// </summary>
	struct PdbCustomDebugInfoWriter {
		readonly Metadata metadata;
		readonly MethodDef method;
		readonly ILogger logger;
		readonly MemoryStream memoryStream;
		readonly DataWriter writer;
		readonly Dictionary<Instruction, uint> instructionToOffsetDict;
		uint bodySize;
		bool instructionToOffsetDictInitd;

		/// <summary>
		/// Returns the raw custom debug info or null if there was an error
		/// </summary>
		/// <param name="metadata">Metadata</param>
		/// <param name="context">Writer context</param>
		/// <param name="method">Method</param>
		/// <param name="customDebugInfos">Custom debug infos to write</param>
		/// <returns></returns>
		public static byte[] Write(Metadata metadata, MethodDef method, PdbCustomDebugInfoWriterContext context, IList<PdbCustomDebugInfo> customDebugInfos) {
			var writer = new PdbCustomDebugInfoWriter(metadata, method, context);
			return writer.Write(customDebugInfos);
		}

		PdbCustomDebugInfoWriter(Metadata metadata, MethodDef method, PdbCustomDebugInfoWriterContext context) {
			this.metadata = metadata;
			this.method = method;
			logger = context.Logger;
			memoryStream = context.MemoryStream;
			writer = context.Writer;
			instructionToOffsetDict = context.InstructionToOffsetDict;
			bodySize = 0;
			instructionToOffsetDictInitd = false;
			memoryStream.SetLength(0);
			memoryStream.Position = 0;
		}

		void InitializeInstructionDictionary() {
			Debug.Assert(!instructionToOffsetDictInitd);
			instructionToOffsetDict.Clear();
			var body = method.Body;
			if (body is null)
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
			if (instr is null) {
				if (nullIsEndOfMethod)
					return bodySize;
				Error("Instruction is null");
				return uint.MaxValue;
			}
			if (instructionToOffsetDict.TryGetValue(instr, out uint offset))
				return offset;
			Error("Instruction is missing in body but it's still being referenced by PDB data. Method {0} (0x{1:X8}), instruction: {2}", method, method.MDToken.Raw, instr);
			return uint.MaxValue;
		}

		void Error(string message, params object[] args) => logger.Log(this, LoggerEvent.Error, message, args);

		byte[] Write(IList<PdbCustomDebugInfo> customDebugInfos) {
			if (customDebugInfos.Count == 0)
				return null;
			if (customDebugInfos.Count > byte.MaxValue) {
				Error("Too many custom debug infos. Count must be <= 255");
				return null;
			}

			writer.WriteByte(CustomDebugInfoConstants.Version);
			writer.WriteByte((byte)customDebugInfos.Count);
			writer.WriteUInt16(0);

			for (int i = 0; i < customDebugInfos.Count; i++) {
				var info = customDebugInfos[i];
				if (info is null) {
					Error("Custom debug info is null");
					return null;
				}
				if ((uint)info.Kind > byte.MaxValue) {
					Error("Invalid custom debug info kind");
					return null;
				}

				var recordPos = writer.Position;
				writer.WriteByte(CustomDebugInfoConstants.RecordVersion);
				writer.WriteByte((byte)info.Kind);
				writer.WriteUInt16(0);
				writer.WriteUInt32(0);

				int count, j, k;
				uint token;
				switch (info.Kind) {
				case PdbCustomDebugInfoKind.UsingGroups:
					var usingRec = info as PdbUsingGroupsCustomDebugInfo;
					if (usingRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = usingRec.UsingCounts.Count;
					if (count > ushort.MaxValue) {
						Error("UsingCounts contains more than 0xFFFF elements");
						return null;
					}
					writer.WriteUInt16((ushort)count);
					for (j = 0; j < count; j++)
						writer.WriteUInt16(usingRec.UsingCounts[j]);
					break;

				case PdbCustomDebugInfoKind.ForwardMethodInfo:
					var fwdMethodRec = info as PdbForwardMethodInfoCustomDebugInfo;
					if (fwdMethodRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					token = GetMethodToken(fwdMethodRec.Method);
					if (token == 0)
						return null;
					writer.WriteUInt32(token);
					break;

				case PdbCustomDebugInfoKind.ForwardModuleInfo:
					var fwdModRec = info as PdbForwardModuleInfoCustomDebugInfo;
					if (fwdModRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					token = GetMethodToken(fwdModRec.Method);
					if (token == 0)
						return null;
					writer.WriteUInt32(token);
					break;

				case PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes:
					var smLocalScopesRec = info as PdbStateMachineHoistedLocalScopesCustomDebugInfo;
					if (smLocalScopesRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = smLocalScopesRec.Scopes.Count;
					writer.WriteInt32(count);
					for (j = 0; j < count; j++) {
						var scope = smLocalScopesRec.Scopes[j];
						if (scope.IsSynthesizedLocal) {
							writer.WriteInt32(0);
							writer.WriteInt32(0);
						}
						else {
							writer.WriteUInt32(GetInstructionOffset(scope.Start, nullIsEndOfMethod: false));
							writer.WriteUInt32(GetInstructionOffset(scope.End, nullIsEndOfMethod: true) - 1);
						}
					}
					break;

				case PdbCustomDebugInfoKind.StateMachineTypeName:
					var smTypeRec = info as PdbStateMachineTypeNameCustomDebugInfo;
					if (smTypeRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					var type = smTypeRec.Type;
					if (type is null) {
						Error("State machine type is null");
						return null;
					}
					WriteUnicodeZ(MetadataNameToRoslynName(type.Name));
					break;

				case PdbCustomDebugInfoKind.DynamicLocals:
					var dynLocListRec = info as PdbDynamicLocalsCustomDebugInfo;
					if (dynLocListRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = dynLocListRec.Locals.Count;
					writer.WriteInt32(count);
					for (j = 0; j < count; j++) {
						var dynLoc = dynLocListRec.Locals[j];
						if (dynLoc is null) {
							Error("Dynamic local is null");
							return null;
						}
						if (dynLoc.Flags.Count > 64) {
							Error("Dynamic local flags is longer than 64 bytes");
							return null;
						}
						var name = dynLoc.Name;
						if (name is null)
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
							writer.WriteByte(dynLoc.Flags[k]);
						while (k++ < 64)
							writer.WriteByte(0);
						writer.WriteInt32(dynLoc.Flags.Count);

						if (dynLoc.Local is null)
							writer.WriteInt32(0);
						else
							writer.WriteInt32(dynLoc.Local.Index);

						for (k = 0; k < name.Length; k++)
							writer.WriteUInt16(name[k]);
						while (k++ < 64)
							writer.WriteUInt16(0);
					}
					break;

				case PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap:
					var encLocalMapRec = info as PdbEditAndContinueLocalSlotMapCustomDebugInfo;
					if (encLocalMapRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					writer.WriteBytes(encLocalMapRec.Data);
					break;

				case PdbCustomDebugInfoKind.EditAndContinueLambdaMap:
					var encLambdaRec = info as PdbEditAndContinueLambdaMapCustomDebugInfo;
					if (encLambdaRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					writer.WriteBytes(encLambdaRec.Data);
					break;

				case PdbCustomDebugInfoKind.TupleElementNames:
					var tupleListRec = info as PdbTupleElementNamesCustomDebugInfo;
					if (tupleListRec is null) {
						Error("Unsupported custom debug info type {0}", info.GetType());
						return null;
					}
					count = tupleListRec.Names.Count;
					writer.WriteInt32(count);
					for (j = 0; j < count; j++) {
						var tupleInfo = tupleListRec.Names[j];
						if (tupleInfo is null) {
							Error("Tuple name info is null");
							return null;
						}
						writer.WriteInt32(tupleInfo.TupleElementNames.Count);
						for (k = 0; k < tupleInfo.TupleElementNames.Count; k++)
							WriteUTF8Z(tupleInfo.TupleElementNames[k]);

						if (tupleInfo.Local is null) {
							writer.WriteInt32(-1);
							writer.WriteUInt32(GetInstructionOffset(tupleInfo.ScopeStart, nullIsEndOfMethod: false));
							writer.WriteUInt32(GetInstructionOffset(tupleInfo.ScopeEnd, nullIsEndOfMethod: true));
						}
						else {
							writer.WriteInt32(tupleInfo.Local.Index);
							writer.WriteInt64(0L);
						}
						WriteUTF8Z(tupleInfo.Name);
					}
					break;

				default:
					var unkRec = info as PdbUnknownCustomDebugInfo;
					if (unkRec is null) {
						Error("Unsupported custom debug info class {0}", info.GetType());
						return null;
					}
					writer.WriteBytes(unkRec.Data);
					break;
				}

				var pos = writer.Position;
				var recLen = (pos - recordPos);
				var alignedLen = (recLen + 3) & ~3;
				if (alignedLen > uint.MaxValue) {
					Error("Custom debug info record is too big");
					return null;
				}
				writer.Position = recordPos + 3;
				if (info.Kind <= PdbCustomDebugInfoKind.DynamicLocals)
					writer.WriteByte(0);
				else
					writer.WriteByte((byte)(alignedLen - recLen));
				writer.WriteUInt32((uint)alignedLen);

				writer.Position = pos;
				while (writer.Position < recordPos + alignedLen)
					writer.WriteByte(0);
			}

			return memoryStream.ToArray();
		}

		string MetadataNameToRoslynName(string name) {
			if (name is null)
				return name;
			int index = name.LastIndexOf('`');
			if (index < 0)
				return name;
			return name.Substring(0, index);
		}

		void WriteUnicodeZ(string s) {
			if (s is null) {
				Error("String is null");
				return;
			}

			if (s.IndexOf('\0') >= 0) {
				Error("String contains a NUL char: {0}", s);
				return;
			}

			for (int i = 0; i < s.Length; i++)
				writer.WriteUInt16(s[i]);
			writer.WriteUInt16(0);
		}

		void WriteUTF8Z(string s) {
			if (s is null) {
				Error("String is null");
				return;
			}

			if (s.IndexOf('\0') >= 0) {
				Error("String contains a NUL char: {0}", s);
				return;
			}

			writer.WriteBytes(Encoding.UTF8.GetBytes(s));
			writer.WriteByte(0);
		}

		uint GetMethodToken(IMethodDefOrRef method) {
			if (method is null) {
				Error("Method is null");
				return 0;
			}

			if (method is MethodDef md) {
				uint rid = metadata.GetRid(md);
				if (rid == 0) {
					Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, metadata.Module);
					return 0;
				}
				return new MDToken(md.MDToken.Table, rid).Raw;
			}

			if (method is MemberRef mr && mr.IsMethodRef)
				return metadata.GetToken(mr).Raw;

			Error("Not a method");
			return 0;
		}
	}
}
