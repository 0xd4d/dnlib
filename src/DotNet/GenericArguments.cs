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

namespace dnlib.DotNet {
	struct GenericArgumentsStack {
		readonly List<IList<TypeSig>> argsStack;
		readonly bool isTypeVar;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's for generic types, <c>false</c> if generic methods</param>
		public GenericArgumentsStack(bool isTypeVar) {
			this.argsStack = new List<IList<TypeSig>>();
			this.isTypeVar = isTypeVar;
		}

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="args">The generic arguments</param>
		public void Push(IList<TypeSig> args) {
			argsStack.Add(args);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> Pop() {
			int index = argsStack.Count - 1;
			var result = argsStack[index];
			argsStack.RemoveAt(index);
			return result;
		}

		/// <summary>
		/// Resolves a generic argument
		/// </summary>
		/// <param name="number">Generic variable number</param>
		/// <returns>A <see cref="TypeSig"/> or <c>null</c> if none was found</returns>
		public TypeSig Resolve(uint number) {
			TypeSig result = null;
			for (int i = argsStack.Count - 1; i >= 0; i--) {
				var args = argsStack[i];
				if (number >= args.Count)
					return null;
				var typeSig = args[(int)number];
				var gvar = typeSig as GenericSig;
				if (gvar == null || gvar.IsTypeVar != isTypeVar)
					return typeSig;
				result = gvar;
				number = gvar.Number;
			}
			return result;
		}
	}

	sealed class GenericArguments {
		GenericArgumentsStack typeArgsStack = new GenericArgumentsStack(true);
		GenericArgumentsStack methodArgsStack = new GenericArgumentsStack(false);

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="typeArgs">The generic arguments</param>
		public void PushTypeArgs(IList<TypeSig> typeArgs) {
			typeArgsStack.Push(typeArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopTypeArgs() {
			return typeArgsStack.Pop();
		}

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="methodArgs">The generic arguments</param>
		public void PushMethodArgs(IList<TypeSig> methodArgs) {
			methodArgsStack.Push(methodArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopMethodArgs() {
			return methodArgsStack.Pop();
		}

		/// <summary>
		/// Replaces a generic type/method var with its generic argument (if any). If
		/// <paramref name="typeSig"/> isn't a generic type/method var or if it can't
		/// be resolved, it itself is returned. Else the resolved type is returned.
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns>New <see cref="TypeSig"/> which is never <c>null</c> unless
		/// <paramref name="typeSig"/> is <c>null</c></returns>
		public TypeSig Resolve(TypeSig typeSig) {
			if (typeSig == null)
				return null;

			var sig = typeSig;

			var genericMVar = sig as GenericMVar;
			if (genericMVar != null) {
				var newSig = methodArgsStack.Resolve(genericMVar.Number);
				if (newSig == null || newSig == sig)
					return sig;
				sig = newSig;
			}

			var genericVar = sig as GenericVar;
			if (genericVar != null) {
				var newSig = typeArgsStack.Resolve(genericVar.Number);
				if (newSig == null || newSig == sig)
					return sig;
				sig = newSig;
			}

			return sig;
		}
	}
}
