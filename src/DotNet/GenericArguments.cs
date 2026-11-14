// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

namespace dnlib.DotNet {
	readonly struct GenericArgumentsStack {
		readonly List<IList<TypeSig>> argsStack;
		readonly bool isTypeVar;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's for generic types, <c>false</c> if generic methods</param>
		public GenericArgumentsStack(bool isTypeVar) {
			argsStack = new List<IList<TypeSig>>();
			this.isTypeVar = isTypeVar;
		}

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="args">The generic arguments</param>
		public void Push(IList<TypeSig> args) => argsStack.Add(args);

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
				if (gvar is null || gvar.IsTypeVar != isTypeVar)
					return typeSig;
				result = gvar;
				number = gvar.Number;
			}
			return result;
		}
	}

	/// <summary>
	/// Replaces generic type/method var with its generic argument
	/// </summary>
	sealed class GenericArguments {
		GenericArgumentsStack typeArgsStack = new GenericArgumentsStack(true);
		GenericArgumentsStack methodArgsStack = new GenericArgumentsStack(false);

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="typeArgs">The generic arguments</param>
		public void PushTypeArgs(IList<TypeSig> typeArgs) => typeArgsStack.Push(typeArgs);

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopTypeArgs() => typeArgsStack.Pop();

		/// <summary>
		/// Pushes generic arguments
		/// </summary>
		/// <param name="methodArgs">The generic arguments</param>
		public void PushMethodArgs(IList<TypeSig> methodArgs) => methodArgsStack.Push(methodArgs);

		/// <summary>
		/// Pops generic arguments
		/// </summary>
		/// <returns>The popped generic arguments</returns>
		public IList<TypeSig> PopMethodArgs() => methodArgsStack.Pop();

		/// <summary>
		/// Replaces a generic type/method var with its generic argument (if any). If
		/// <paramref name="typeSig"/> isn't a generic type/method var or if it can't
		/// be resolved, it itself is returned. Else the resolved type is returned.
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns>New <see cref="TypeSig"/> which is never <c>null</c> unless
		/// <paramref name="typeSig"/> is <c>null</c></returns>
		public TypeSig Resolve(TypeSig typeSig) {
			if (typeSig is null)
				return null;

			var sig = typeSig;

			if (sig is GenericMVar genericMVar) {
				var newSig = methodArgsStack.Resolve(genericMVar.Number);
				if (newSig is null || newSig == sig)
					return sig;
				return newSig;
			}

			if (sig is GenericVar genericVar) {
				var newSig = typeArgsStack.Resolve(genericVar.Number);
				if (newSig is null || newSig == sig)
					return sig;
				return newSig;
			}

			return sig;
		}
	}

	/// <summary>
	/// Creates copies of signatures where generic type vars (<c>!n</c>) have been replaced with
	/// generic arguments. Generic method vars (<c>!!n</c>) are left as is.
	/// </summary>
	struct GenericTypeArgsSubstitutor {
		readonly IList<TypeSig> typeArgs;
		RecursionCounter recursionCounter;

		GenericTypeArgsSubstitutor(IList<TypeSig> typeArgs) {
			this.typeArgs = typeArgs;
			recursionCounter = new RecursionCounter();
		}

		public static MethodSig Substitute(MethodSig sig, IList<TypeSig> typeArgs) {
			if (sig is null || typeArgs is null || !TypeHelper.ContainsGenericParameter(sig))
				return sig;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sig);
		}

		public static FieldSig Substitute(FieldSig sig, IList<TypeSig> typeArgs) {
			if (sig is null || typeArgs is null || !TypeHelper.ContainsGenericParameter(sig))
				return sig;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sig);
		}

		public static PropertySig Substitute(PropertySig sig, IList<TypeSig> typeArgs) {
			if (sig is null || typeArgs is null || !TypeHelper.ContainsGenericParameter(sig))
				return sig;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sig);
		}

		public static CallingConventionSig Substitute(CallingConventionSig sig, IList<TypeSig> typeArgs) {
			if (sig is null || typeArgs is null || !TypeHelper.ContainsGenericParameter(sig))
				return sig;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sig);
		}

		public static TypeSig Substitute(TypeSig sig, IList<TypeSig> typeArgs) {
			if (sig is null || typeArgs is null || !TypeHelper.ContainsGenericParameter(sig))
				return sig;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sig);
		}

		public static IList<TypeSig> Substitute(IList<TypeSig> sigs, IList<TypeSig> typeArgs) {
			if (sigs is null || typeArgs is null)
				return sigs;
			return new GenericTypeArgsSubstitutor(typeArgs).Create(sigs);
		}

		CallingConventionSig Create(CallingConventionSig sig) =>
			sig switch {
				MethodSig ms => Create(ms),
				FieldSig fs => Create(fs),
				PropertySig ps => Create(ps),
				_ => sig,
			};

		MethodSig Create(MethodSig sig) {
			var paramsAfterSentinel = sig.ParamsAfterSentinel;
			var newSig = new MethodSig(sig.CallingConvention, sig.GenParamCount, Create(sig.RetType), Create(sig.Params),
				paramsAfterSentinel is null ? null : Create(paramsAfterSentinel));
			newSig.OriginalToken = sig.OriginalToken;
			newSig.ExtraData = sig.ExtraData;
			return newSig;
		}

		PropertySig Create(PropertySig sig) {
			var paramsAfterSentinel = sig.ParamsAfterSentinel;
			var newSig = new PropertySig(sig.CallingConvention, sig.GenParamCount, Create(sig.RetType), Create(sig.Params),
				paramsAfterSentinel is null ? null : Create(paramsAfterSentinel));
			newSig.ExtraData = sig.ExtraData;
			return newSig;
		}

		FieldSig Create(FieldSig sig) {
			var newSig = new FieldSig(sig.GetCallingConvention(), Create(sig.Type));
			newSig.ExtraData = sig.ExtraData;
			return newSig;
		}

		IList<TypeSig> Create(IList<TypeSig> sigs) {
			var list = new List<TypeSig>(sigs.Count);
			for (int i = 0; i < sigs.Count; i++)
				list.Add(Create(sigs[i]));
			return list;
		}

		TypeSig Create(TypeSig sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return sig;

			TypeSig result;
			switch (sig) {
			case GenericVar gv:
				result = gv.Number < (uint)typeArgs.Count ? typeArgs[(int)gv.Number] ?? sig : sig;
				break;
			case GenericInstSig gis:
				var genericType = Create(gis.GenericType) as ClassOrValueTypeSig ?? gis.GenericType;
				result = new GenericInstSig(genericType, Create(gis.GenericArguments));
				break;
			case FnPtrSig fnPtr:
				result = new FnPtrSig(Create(fnPtr.Signature));
				break;
			case PtrSig ptr:
				result = new PtrSig(Create(ptr.Next));
				break;
			case ByRefSig byRef:
				result = new ByRefSig(Create(byRef.Next));
				break;
			case SZArraySig szArray:
				result = new SZArraySig(Create(szArray.Next));
				break;
			case ArraySig array:
				result = new ArraySig(Create(array.Next), array.Rank, array.Sizes, array.LowerBounds);
				break;
			case CModReqdSig cmodReqd:
				result = new CModReqdSig(cmodReqd.Modifier, Create(cmodReqd.Next));
				break;
			case CModOptSig cmodOpt:
				result = new CModOptSig(cmodOpt.Modifier, Create(cmodOpt.Next));
				break;
			case PinnedSig pinned:
				result = new PinnedSig(Create(pinned.Next));
				break;
			case ValueArraySig valueArray:
				result = new ValueArraySig(Create(valueArray.Next), valueArray.Size);
				break;
			case ModuleSig moduleSig:
				result = new ModuleSig(moduleSig.Index, Create(moduleSig.Next));
				break;
			default:
				result = sig;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}
	}
}
