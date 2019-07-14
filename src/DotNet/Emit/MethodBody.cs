// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Pdb;
using dnlib.PE;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Method body base class
	/// </summary>
	public abstract class MethodBody {
	}

	/// <summary>
	/// A native method body
	/// </summary>
	public sealed class NativeMethodBody : MethodBody {
		RVA rva;

		/// <summary>
		/// Gets/sets the RVA of the native method body
		/// </summary>
		public RVA RVA {
			get => rva;
			set => rva = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public NativeMethodBody() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of method body</param>
		public NativeMethodBody(RVA rva) => this.rva = rva;
	}

	/// <summary>
	/// CIL (managed code) body
	/// </summary>
	public sealed class CilBody : MethodBody {
		bool keepOldMaxStack;
		bool initLocals;
		byte headerSize;
		ushort maxStack;
		uint localVarSigTok;
		readonly IList<Instruction> instructions;
		readonly IList<ExceptionHandler> exceptionHandlers;
		readonly LocalList localList;

		/// <summary>
		/// Size of a small header
		/// </summary>
		public const byte SMALL_HEADER_SIZE = 1;

		/// <summary>
		/// Gets/sets a flag indicating whether the original max stack value should be used.
		/// </summary>
		public bool KeepOldMaxStack {
			get => keepOldMaxStack;
			set => keepOldMaxStack = value;
		}

		/// <summary>
		/// Gets/sets the init locals flag. This is only valid if the method has any locals.
		/// </summary>
		public bool InitLocals {
			get => initLocals;
			set => initLocals = value;
		}

		/// <summary>
		/// Gets/sets the size in bytes of the method body header. The instructions immediately follow
		/// the header.
		/// </summary>
		public byte HeaderSize {
			get => headerSize;
			set => headerSize = value;
		}

		/// <summary>
		/// <c>true</c> if it was a small body header (<see cref="HeaderSize"/> is <c>1</c>)
		/// </summary>
		public bool IsSmallHeader => headerSize == SMALL_HEADER_SIZE;

		/// <summary>
		/// <c>true</c> if it was a big body header
		/// </summary>
		public bool IsBigHeader => headerSize != SMALL_HEADER_SIZE;

		/// <summary>
		/// Gets/sets max stack value from the fat method header.
		/// </summary>
		public ushort MaxStack {
			get => maxStack;
			set => maxStack = value;
		}

		/// <summary>
		/// Gets/sets the locals metadata token
		/// </summary>
		public uint LocalVarSigTok {
			get => localVarSigTok;
			set => localVarSigTok = value;
		}

		/// <summary>
		/// <c>true</c> if <see cref="Instructions"/> is not empty
		/// </summary>
		public bool HasInstructions => instructions.Count > 0;

		/// <summary>
		/// Gets the instructions
		/// </summary>
		public IList<Instruction> Instructions => instructions;

		/// <summary>
		/// <c>true</c> if <see cref="ExceptionHandlers"/> is not empty
		/// </summary>
		public bool HasExceptionHandlers => exceptionHandlers.Count > 0;

		/// <summary>
		/// Gets the exception handlers
		/// </summary>
		public IList<ExceptionHandler> ExceptionHandlers => exceptionHandlers;

		/// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables => localList.Count > 0;

		/// <summary>
		/// Gets the locals
		/// </summary>
		public LocalList Variables => localList;

		/// <summary>
		/// Gets/sets the PDB method. This is <c>null</c> if no PDB has been loaded or if there's
		/// no PDB info for this method.
		/// </summary>
		public PdbMethod PdbMethod {
			get => pdbMethod;
			set => pdbMethod = value;
		}
		PdbMethod pdbMethod;

		/// <summary>
		/// <c>true</c> if <see cref="PdbMethod"/> is not <c>null</c>
		/// </summary>
		public bool HasPdbMethod => !(PdbMethod is null);

		/// <summary>
		/// Gets the total size of the body in the PE file, including header, IL bytes, and exception handlers.
		/// This property returns 0 if the size is unknown.
		/// </summary>
		internal uint MetadataBodySize { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public CilBody() {
			initLocals = true;
			instructions = new List<Instruction>();
			exceptionHandlers = new List<ExceptionHandler>();
			localList = new LocalList();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initLocals">Init locals flag</param>
		/// <param name="instructions">All instructions. This instance will own the list.</param>
		/// <param name="exceptionHandlers">All exception handlers. This instance will own the list.</param>
		/// <param name="locals">All locals. This instance will own the locals in the list.</param>
		public CilBody(bool initLocals, IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers, IList<Local> locals) {
			this.initLocals = initLocals;
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
			localList = new LocalList(locals);
		}

		/// <summary>
		/// Shorter instructions are converted to the longer form, eg. <c>Ldc_I4_1</c> is
		/// converted to <c>Ldc_I4</c> with a <c>1</c> as the operand.
		/// </summary>
		/// <param name="parameters">All method parameters, including the hidden 'this' parameter
		/// if it's an instance method. Use <see cref="MethodDef.Parameters"/>.</param>
		public void SimplifyMacros(IList<Parameter> parameters) => instructions.SimplifyMacros(localList, parameters);

		/// <summary>
		/// Optimizes instructions by using the shorter form if possible. Eg. <c>Ldc_I4</c> <c>1</c>
		/// will be replaced with <c>Ldc_I4_1</c>.
		/// </summary>
		public void OptimizeMacros() => instructions.OptimizeMacros();

		/// <summary>
		/// Short branch instructions are converted to the long form, eg. <c>Beq_S</c> is
		/// converted to <c>Beq</c>.
		/// </summary>
		public void SimplifyBranches() => instructions.SimplifyBranches();

		/// <summary>
		/// Optimizes branches by using the smallest possible branch
		/// </summary>
		public void OptimizeBranches() => instructions.OptimizeBranches();

		/// <summary>
		/// Updates each instruction's offset
		/// </summary>
		/// <returns>Total size in bytes of all instructions</returns>
		public uint UpdateInstructionOffsets() => instructions.UpdateInstructionOffsets();
	}
}
