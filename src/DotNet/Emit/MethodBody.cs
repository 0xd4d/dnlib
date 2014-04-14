/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System.Collections.Generic;
using dnlib.PE;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

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
			get { return rva; }
			set { rva = value; }
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
		public NativeMethodBody(RVA rva) {
			this.rva = rva;
		}
	}

	/// <summary>
	/// CIL (managed code) body
	/// </summary>
	public sealed class CilBody : MethodBody {
		bool initLocals;
		ushort maxStack;
		uint localVarSigTok;
		readonly ThreadSafe.IList<Instruction> instructions;
		readonly ThreadSafe.IList<ExceptionHandler> exceptionHandlers;
		readonly LocalList localList;

		/// <summary>
		/// Gets/sets the init locals flag. This is only valid if the method has any locals.
		/// </summary>
		public bool InitLocals {
			get { return initLocals; }
			set { initLocals = value; }
		}

		/// <summary>
		/// Gets/sets max stack value from the fat method header.
		/// </summary>
		public ushort MaxStack {
			get { return maxStack; }
			set { maxStack = value; }
		}

		/// <summary>
		/// Gets/sets the locals metadata token
		/// </summary>
		public uint LocalVarSigTok {
			get { return localVarSigTok; }
			set { localVarSigTok = value; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Instructions"/> is not empty
		/// </summary>
		public bool HasInstructions {
			get { return instructions.Count > 0; }
		}

		/// <summary>
		/// Gets the instructions
		/// </summary>
		public ThreadSafe.IList<Instruction> Instructions {
			get { return instructions; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ExceptionHandlers"/> is not empty
		/// </summary>
		public bool HasExceptionHandlers {
			get { return exceptionHandlers.Count > 0; }
		}

		/// <summary>
		/// Gets the exception handlers
		/// </summary>
		public ThreadSafe.IList<ExceptionHandler> ExceptionHandlers {
			get { return exceptionHandlers; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables {
			get { return localList.Count > 0; }
		}

		/// <summary>
		/// Gets the locals
		/// </summary>
		public LocalList Variables {// Only called Variables for compat w/ older code. Locals is a better and more accurate name
			get { return localList; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public CilBody() {
			this.initLocals = true;
			this.instructions = ThreadSafeListCreator.Create<Instruction>();
			this.exceptionHandlers = ThreadSafeListCreator.Create<ExceptionHandler>();
			this.localList = new LocalList();
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
			this.instructions = ThreadSafeListCreator.MakeThreadSafe(instructions);
			this.exceptionHandlers = ThreadSafeListCreator.MakeThreadSafe(exceptionHandlers);
			this.localList = new LocalList(locals);
		}

		/// <summary>
		/// Shorter instructions are converted to the longer form, eg. <c>Ldc_I4_1</c> is
		/// converted to <c>Ldc_I4</c> with a <c>1</c> as the operand.
		/// </summary>
		/// <param name="parameters">All method parameters, including the hidden 'this' parameter
		/// if it's an instance method. Use <see cref="MethodDef.Parameters"/>.</param>
		public void SimplifyMacros(IList<Parameter> parameters) {
			instructions.SimplifyMacros(localList, parameters);
		}

		/// <summary>
		/// Optimizes instructions by using the shorter form if possible. Eg. <c>Ldc_I4</c> <c>1</c>
		/// will be replaced with <c>Ldc_I4_1</c>.
		/// </summary>
		public void OptimizeMacros() {
			instructions.OptimizeMacros();
		}

		/// <summary>
		/// Short branch instructions are converted to the long form, eg. <c>Beq_S</c> is
		/// converted to <c>Beq</c>.
		/// </summary>
		public void SimplifyBranches() {
			instructions.SimplifyBranches();
		}

		/// <summary>
		/// Optimizes branches by using the smallest possible branch
		/// </summary>
		public void OptimizeBranches() {
			instructions.OptimizeBranches();
		}

		/// <summary>
		/// Updates each instruction's offset
		/// </summary>
		/// <returns>Total size in bytes of all instructions</returns>
		public uint UpdateInstructionOffsets() {
			return instructions.UpdateInstructionOffsets();
		}
	}
}
