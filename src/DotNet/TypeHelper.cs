// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// Various helper methods for <see cref="IType"/> classes to prevent infinite recursion
	/// </summary>
	struct TypeHelper {
		RecursionCounter recursionCounter;

		internal static bool ContainsGenericParameter(StandAloneSig ss) => !(ss is null) && TypeHelper.ContainsGenericParameter(ss.Signature);
		internal static bool ContainsGenericParameter(InterfaceImpl ii) => !(ii is null) && TypeHelper.ContainsGenericParameter(ii.Interface);
		internal static bool ContainsGenericParameter(GenericParamConstraint gpc) => !(gpc is null) && ContainsGenericParameter(gpc.Constraint);

		internal static bool ContainsGenericParameter(MethodSpec ms) {
			if (ms is null)
				return false;

			// A normal MethodSpec should always contain generic arguments and thus
			// its MethodDef is always a generic method with generic parameters.
			return true;
		}

		internal static bool ContainsGenericParameter(MemberRef mr) {
			if (mr is null)
				return false;

			if (ContainsGenericParameter(mr.Signature))
				return true;

			var cl = mr.Class;

			if (cl is ITypeDefOrRef tdr)
				return tdr.ContainsGenericParameter;

			if (cl is MethodDef md)
				return TypeHelper.ContainsGenericParameter(md.Signature);

			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="callConv"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="callConv">Calling convention signature</param>
		/// <returns><c>true</c> if <paramref name="callConv"/> contains a <see cref="GenericVar"/>
		/// or a <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(CallingConventionSig callConv) {
			if (callConv is FieldSig fs)
				return ContainsGenericParameter(fs);

			if (callConv is MethodBaseSig mbs)
				return ContainsGenericParameter(mbs);

			if (callConv is LocalSig ls)
				return ContainsGenericParameter(ls);

			if (callConv is GenericInstMethodSig gim)
				return ContainsGenericParameter(gim);

			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="fieldSig"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="fieldSig">Field signature</param>
		/// <returns><c>true</c> if <paramref name="fieldSig"/> contains a <see cref="GenericVar"/>
		/// or a <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(FieldSig fieldSig) => new TypeHelper().ContainsGenericParameterInternal(fieldSig);

		/// <summary>
		/// Checks whether <paramref name="methodSig"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="methodSig">Method or property signature</param>
		/// <returns><c>true</c> if <paramref name="methodSig"/> contains a <see cref="GenericVar"/>
		/// or a <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(MethodBaseSig methodSig) => new TypeHelper().ContainsGenericParameterInternal(methodSig);

		/// <summary>
		/// Checks whether <paramref name="localSig"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="localSig">Local signature</param>
		/// <returns><c>true</c> if <paramref name="localSig"/> contains a <see cref="GenericVar"/>
		/// or a <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(LocalSig localSig) => new TypeHelper().ContainsGenericParameterInternal(localSig);

		/// <summary>
		/// Checks whether <paramref name="gim"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="gim">Generic method signature</param>
		/// <returns><c>true</c> if <paramref name="gim"/> contains a <see cref="GenericVar"/>
		/// or a <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(GenericInstMethodSig gim) => new TypeHelper().ContainsGenericParameterInternal(gim);

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(IType type) {
			if (type is TypeDef td)
				return ContainsGenericParameter(td);

			if (type is TypeRef tr)
				return ContainsGenericParameter(tr);

			if (type is TypeSpec ts)
				return ContainsGenericParameter(ts);

			if (type is TypeSig sig)
				return ContainsGenericParameter(sig);

			if (type is ExportedType et)
				return ContainsGenericParameter(et);

			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(TypeDef type) => new TypeHelper().ContainsGenericParameterInternal(type);

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(TypeRef type) => new TypeHelper().ContainsGenericParameterInternal(type);

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(TypeSpec type) => new TypeHelper().ContainsGenericParameterInternal(type);

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(TypeSig type) => new TypeHelper().ContainsGenericParameterInternal(type);

		/// <summary>
		/// Checks whether <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns><c>true</c> if <paramref name="type"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.</returns>
		public static bool ContainsGenericParameter(ExportedType type) => new TypeHelper().ContainsGenericParameterInternal(type);

		bool ContainsGenericParameterInternal(TypeDef type) => false;

		bool ContainsGenericParameterInternal(TypeRef type) => false;

		bool ContainsGenericParameterInternal(TypeSpec type) {
			if (type is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = ContainsGenericParameterInternal(type.TypeSig);

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameterInternal(ITypeDefOrRef tdr) {
			if (tdr is null)
				return false;
			// TypeDef and TypeRef contain no generic parameters
			return ContainsGenericParameterInternal(tdr as TypeSpec);
		}

		bool ContainsGenericParameterInternal(TypeSig type) {
			if (type is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res;
			switch (type.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
				res = ContainsGenericParameterInternal((type as TypeDefOrRefSig).TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
				res = true;
				break;

			case ElementType.FnPtr:
				res = ContainsGenericParameterInternal((type as FnPtrSig).Signature);
				break;

			case ElementType.GenericInst:
				var gi = (GenericInstSig)type;
				res = ContainsGenericParameterInternal(gi.GenericType) ||
					ContainsGenericParameter(gi.GenericArguments);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				res = ContainsGenericParameterInternal((type as NonLeafSig).Next);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				res = ContainsGenericParameterInternal((type as ModifierSig).Modifier) ||
					ContainsGenericParameterInternal((type as NonLeafSig).Next);
				break;


			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			case ElementType.Sentinel:
			default:
				res = false;
				break;
			}

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameterInternal(ExportedType type) => false;

		bool ContainsGenericParameterInternal(CallingConventionSig callConv) {
			if (callConv is FieldSig fs)
				return ContainsGenericParameterInternal(fs);

			if (callConv is MethodBaseSig mbs)
				return ContainsGenericParameterInternal(mbs);

			if (callConv is LocalSig ls)
				return ContainsGenericParameterInternal(ls);

			if (callConv is GenericInstMethodSig gim)
				return ContainsGenericParameterInternal(gim);

			return false;
		}

		bool ContainsGenericParameterInternal(FieldSig fs) {
			if (fs is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = ContainsGenericParameterInternal(fs.Type);

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameterInternal(MethodBaseSig mbs) {
			if (mbs is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = ContainsGenericParameterInternal(mbs.RetType) ||
				ContainsGenericParameter(mbs.Params) ||
				ContainsGenericParameter(mbs.ParamsAfterSentinel);

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameterInternal(LocalSig ls) {
			if (ls is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = ContainsGenericParameter(ls.Locals);

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameterInternal(GenericInstMethodSig gim) {
			if (gim is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = ContainsGenericParameter(gim.GenericArguments);

			recursionCounter.Decrement();
			return res;
		}

		bool ContainsGenericParameter(IList<TypeSig> types) {
			if (types is null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool res = false;
			int count = types.Count;
			for (int i = 0; i < count; i++) {
				var type = types[i];
				if (ContainsGenericParameter(type)) {
					res = true;
					break;
				}
			}
			recursionCounter.Decrement();
			return res;
		}
	}
}
