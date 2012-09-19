using System.Collections.Generic;

namespace dot10.DotNet {
	class GenericArgumentsStack<TGenericType> where TGenericType : GenericSig {
		List<IList<TypeSig>> argsStack = new List<IList<TypeSig>>();

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
		/// Replaces a generic type/method var with its generic argument (if any)
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns>New <see cref="TypeSig"/> which is never <c>null</c> unless
		/// <paramref name="typeSig"/> is <c>null</c></returns>
		public TypeSig Resolve(TypeSig typeSig) {
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
