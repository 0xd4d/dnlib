// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Text;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Portable {
	interface IPortablePdbCustomDebugInfoWriterHelper : IWriterError {
	}

	readonly struct PortablePdbCustomDebugInfoWriter {
		readonly IPortablePdbCustomDebugInfoWriterHelper helper;
		readonly SerializerMethodContext methodContext;
		readonly Metadata systemMetadata;
		readonly MemoryStream outStream;
		readonly DataWriter writer;

		public static byte[] Write(IPortablePdbCustomDebugInfoWriterHelper helper, SerializerMethodContext methodContext, Metadata systemMetadata, PdbCustomDebugInfo cdi, DataWriterContext context) {
			var writer = new PortablePdbCustomDebugInfoWriter(helper, methodContext, systemMetadata, context);
			return writer.Write(cdi);
		}

		PortablePdbCustomDebugInfoWriter(IPortablePdbCustomDebugInfoWriterHelper helper, SerializerMethodContext methodContext, Metadata systemMetadata, DataWriterContext context) {
			this.helper = helper;
			this.methodContext = methodContext;
			this.systemMetadata = systemMetadata;
			outStream = context.OutStream;
			writer = context.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;
		}

		byte[] Write(PdbCustomDebugInfo cdi) {
			switch (cdi.Kind) {
			case PdbCustomDebugInfoKind.UsingGroups:
			case PdbCustomDebugInfoKind.ForwardMethodInfo:
			case PdbCustomDebugInfoKind.ForwardModuleInfo:
			case PdbCustomDebugInfoKind.StateMachineTypeName:
			case PdbCustomDebugInfoKind.DynamicLocals:
			case PdbCustomDebugInfoKind.TupleElementNames:
			case PdbCustomDebugInfoKind.IteratorMethod:
			case PdbCustomDebugInfoKind.SourceServer:
			default:
				helper.Error("Unreachable code, caller should filter these out");
				return null;

			case PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes:
				WriteStateMachineHoistedLocalScopes((PdbStateMachineHoistedLocalScopesCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap:
				WriteEditAndContinueLocalSlotMap((PdbEditAndContinueLocalSlotMapCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.EditAndContinueLambdaMap:
				WriteEditAndContinueLambdaMap((PdbEditAndContinueLambdaMapCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.Unknown:
				WriteUnknown((PdbUnknownCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.TupleElementNames_PortablePdb:
				WriteTupleElementNames((PortablePdbTupleElementNamesCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.DefaultNamespace:
				WriteDefaultNamespace((PdbDefaultNamespaceCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.DynamicLocalVariables:
				WriteDynamicLocalVariables((PdbDynamicLocalVariablesCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.EmbeddedSource:
				WriteEmbeddedSource((PdbEmbeddedSourceCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.SourceLink:
				WriteSourceLink((PdbSourceLinkCustomDebugInfo)cdi);
				break;

			case PdbCustomDebugInfoKind.AsyncMethod:
				WriteAsyncMethodSteppingInformation((PdbAsyncMethodCustomDebugInfo)cdi);
				break;
			}
			return outStream.ToArray();
		}

		void WriteUTF8Z(string s) {
			var bytes = Encoding.UTF8.GetBytes(s);
			writer.WriteBytes(bytes);
			writer.WriteByte(0);
		}

		void WriteStateMachineHoistedLocalScopes(PdbStateMachineHoistedLocalScopesCustomDebugInfo cdi) {
			if (!methodContext.HasBody) {
				helper.Error("Method has no body, can't write custom debug info: " + cdi.Kind);
				return;
			}
			var cdiScopes = cdi.Scopes;
			int count = cdiScopes.Count;
			for (int i = 0; i < count; i++) {
				var scope = cdiScopes[i];
				uint startOffset, endOffset;
				if (scope.IsSynthesizedLocal) {
					startOffset = 0;
					endOffset = 0;
				}
				else {
					var startInstr = scope.Start;
					if (startInstr is null) {
						helper.Error("Instruction is null");
						return;
					}
					startOffset = methodContext.GetOffset(startInstr);
					endOffset = methodContext.GetOffset(scope.End);
				}
				if (startOffset > endOffset) {
					helper.Error("End instruction is before start instruction");
					return;
				}
				writer.WriteUInt32(startOffset);
				writer.WriteUInt32(endOffset - startOffset);
			}
		}

		void WriteEditAndContinueLocalSlotMap(PdbEditAndContinueLocalSlotMapCustomDebugInfo cdi) {
			var d = cdi.Data;
			if (d is null) {
				helper.Error("Data blob is null");
				return;
			}
			writer.WriteBytes(d);
		}

		void WriteEditAndContinueLambdaMap(PdbEditAndContinueLambdaMapCustomDebugInfo cdi) {
			var d = cdi.Data;
			if (d is null) {
				helper.Error("Data blob is null");
				return;
			}
			writer.WriteBytes(d);
		}

		void WriteUnknown(PdbUnknownCustomDebugInfo cdi) {
			var d = cdi.Data;
			if (d is null) {
				helper.Error("Data blob is null");
				return;
			}
			writer.WriteBytes(d);
		}

		void WriteTupleElementNames(PortablePdbTupleElementNamesCustomDebugInfo cdi) {
			var cdiNames = cdi.Names;
			int count = cdiNames.Count;
			for (int i = 0; i < count; i++) {
				var name = cdiNames[i];
				if (name is null) {
					helper.Error("Tuple name is null");
					return;
				}
				WriteUTF8Z(name);
			}
		}

		void WriteDefaultNamespace(PdbDefaultNamespaceCustomDebugInfo cdi) {
			var ns = cdi.Namespace;
			if (ns is null) {
				helper.Error("Default namespace is null");
				return;
			}
			var bytes = Encoding.UTF8.GetBytes(ns);
			writer.WriteBytes(bytes);
		}

		void WriteDynamicLocalVariables(PdbDynamicLocalVariablesCustomDebugInfo cdi) {
			var flags = cdi.Flags;
			for (int i = 0; i < flags.Length; i += 8)
				writer.WriteByte(ToByte(flags, i));
		}

		static byte ToByte(bool[] flags, int index) {
			int res = 0;
			int bit = 1;
			for (int i = index; i < flags.Length; i++, bit <<= 1) {
				if (flags[i])
					res |= bit;
			}
			return (byte)res;
		}

		void WriteEmbeddedSource(PdbEmbeddedSourceCustomDebugInfo cdi) {
			var d = cdi.SourceCodeBlob;
			if (d is null) {
				helper.Error("Source code blob is null");
				return;
			}
			writer.WriteBytes(d);
		}

		void WriteSourceLink(PdbSourceLinkCustomDebugInfo cdi) {
			var d = cdi.FileBlob;
			if (d is null) {
				helper.Error("Source link blob is null");
				return;
			}
			writer.WriteBytes(d);
		}

		void WriteAsyncMethodSteppingInformation(PdbAsyncMethodCustomDebugInfo cdi) {
			if (!methodContext.HasBody) {
				helper.Error("Method has no body, can't write custom debug info: " + cdi.Kind);
				return;
			}

			uint catchHandlerOffset;
			if (cdi.CatchHandlerInstruction is null)
				catchHandlerOffset = 0;
			else
				catchHandlerOffset = methodContext.GetOffset(cdi.CatchHandlerInstruction) + 1;
			writer.WriteUInt32(catchHandlerOffset);

			var cdiStepInfos = cdi.StepInfos;
			int count = cdiStepInfos.Count;
			for (int i = 0; i < count; i++) {
				var info = cdiStepInfos[i];
				if (info.YieldInstruction is null) {
					helper.Error("YieldInstruction is null");
					return;
				}
				if (info.BreakpointMethod is null) {
					helper.Error("BreakpointMethod is null");
					return;
				}
				if (info.BreakpointInstruction is null) {
					helper.Error("BreakpointInstruction is null");
					return;
				}
				uint yieldOffset = methodContext.GetOffset(info.YieldInstruction);
				uint resumeOffset;
				if (methodContext.IsSameMethod(info.BreakpointMethod))
					resumeOffset = methodContext.GetOffset(info.BreakpointInstruction);
				else
					resumeOffset = GetOffsetSlow(info.BreakpointMethod, info.BreakpointInstruction);
				uint resumeMethodRid = systemMetadata.GetRid(info.BreakpointMethod);
				writer.WriteUInt32(yieldOffset);
				writer.WriteUInt32(resumeOffset);
				writer.WriteCompressedUInt32(resumeMethodRid);
			}
		}

		uint GetOffsetSlow(MethodDef method, Instruction instr) {
			var body = method.Body;
			if (body is null) {
				helper.Error("Method has no body");
				return uint.MaxValue;
			}
			var instrs = body.Instructions;
			uint offset = 0;
			for (int i = 0; i < instrs.Count; i++) {
				var instr2 = instrs[i];
				if (instr2 == instr)
					return offset;
				offset += (uint)instr2.GetSize();
			}
			helper.Error("Couldn't find an instruction, maybe it was removed. It's still being referenced by some code or by the PDB");
			return uint.MaxValue;
		}
	}
}
