using System.Collections.Generic;

namespace dot10.DotNet {
	class GenericArgumentsStack<TGenericType> where TGenericType : GenericSig {
		List<IList<ITypeSig>> argsStack = new List<IList<ITypeSig>>();

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="args">The generic arguments</param>
		public void Push(IList<ITypeSig> args) {
			argsStack.Add(args);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<ITypeSig> Pop() {
			int index = argsStack.Count - 1;
			var result = argsStack[index];
			argsStack.RemoveAt(index);
			return result;
		}

		/// <summary>
		/// Resolves a generic argument
		/// </summary>
		/// <param name="number">Generic variable number</param>
		/// <returns>A <see cref="ITypeSig"/> or <c>null</c> if none was found</returns>
		public ITypeSig Resolve(uint number) {
			ITypeSig result = null;
			for (int i = argsStack.Count - 1; i >= 0; i--) {
				var args = argsStack[i];
				if (number >= args.Count)
					return null;
				var typeSig = args[(int)number];
				var gvar = typeSig as TGenericType;
				if (gvar == null)
					return typeSig;
				result = gvar;
				number = gvar.Number;
			}
			return result;
		}
	}

	class GenericArguments {
		const int MAX_RESOLVE_TRIES = 30;
		GenericArgumentsStack<GenericVar> typeArgsStack = new GenericArgumentsStack<GenericVar>();
		GenericArgumentsStack<GenericMVar> methodArgsStack = new GenericArgumentsStack<GenericMVar>();

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="typeArgs">The generic arguments</param>
		public void PushTypeArgs(IList<ITypeSig> typeArgs) {
			typeArgsStack.Push(typeArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<ITypeSig> PopTypeArgs() {
			return typeArgsStack.Pop();
		}

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="methodArgs">The generic arguments</param>
		public void PushMethodArgs(IList<ITypeSig> methodArgs) {
			methodArgsStack.Push(methodArgs);
		}

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<ITypeSig> PopMethodArgs() {
			return methodArgsStack.Pop();
		}

		/// <summary>
		/// Replaces a generic type/method var with its generic argument (if any)
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns>New <see cref="ITypeSig"/> which is never <c>null</c> unless
		/// <paramref name="typeSig"/> is <c>null</c></returns>
		public ITypeSig Resolve(ITypeSig typeSig) {
			if (typeSig == null)
				return null;
			var sig = typeSig;
			for (int i = 0; i < MAX_RESOLVE_TRIES; i++) {
				bool updated = false;

				var genericVar = sig as GenericVar;
				if (genericVar != null) {
					var newSig = typeArgsStack.Resolve(genericVar.Number);
					if (newSig == null || newSig == sig)
						return sig;
					sig = newSig;
					updated = true;
				}

				var genericMVar = sig as GenericMVar;
				if (genericMVar != null) {
					var newSig = methodArgsStack.Resolve(genericMVar.Number);
					if (newSig == null || newSig == sig)
						return sig;
					sig = newSig;
					updated = true;
				}

				if (!updated)
					break;
			}

			return sig;
		}
	}
}
