using System;
using System.Collections.Generic;
using System.Reflection;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Compares types
	/// </summary>
	public sealed class TypeEqualityComparer : IEqualityComparer<IType>, IEqualityComparer<ITypeDefOrRef>, IEqualityComparer<TypeRef>, IEqualityComparer<TypeDef>, IEqualityComparer<TypeSpec>, IEqualityComparer<TypeSig>, IEqualityComparer<ExportedType> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly TypeEqualityComparer Instance = new TypeEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public TypeEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IType x, IType y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IType obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(ITypeDefOrRef x, ITypeDefOrRef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(ITypeDefOrRef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeRef x, TypeRef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeRef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeDef x, TypeDef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeDef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeSpec x, TypeSpec y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeSpec obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeSig x, TypeSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(ExportedType x, ExportedType y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(ExportedType obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares fields
	/// </summary>
	public sealed class FieldEqualityComparer : IEqualityComparer<IField>, IEqualityComparer<FieldDef>, IEqualityComparer<MemberRef> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly FieldEqualityComparer CompareDeclaringTypes = new FieldEqualityComparer(SigComparerOptions.CompareMethodFieldDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly FieldEqualityComparer DontCompareDeclaringTypes = new FieldEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public FieldEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IField x, IField y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IField obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(FieldDef x, FieldDef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(FieldDef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MemberRef x, MemberRef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MemberRef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares methods
	/// </summary>
	public sealed class MethodEqualityComparer : IEqualityComparer<IMethod>, IEqualityComparer<MethodDef>, IEqualityComparer<MemberRef>, IEqualityComparer<MethodSpec> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly MethodEqualityComparer CompareDeclaringTypes = new MethodEqualityComparer(SigComparerOptions.CompareMethodFieldDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly MethodEqualityComparer DontCompareDeclaringTypes = new MethodEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public MethodEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IMethod x, IMethod y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IMethod obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodDef x, MethodDef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodDef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MemberRef x, MemberRef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MemberRef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodSpec x, MethodSpec y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodSpec obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares properties
	/// </summary>
	public sealed class PropertyEqualityComparer : IEqualityComparer<PropertyDef> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly PropertyEqualityComparer CompareDeclaringTypes = new PropertyEqualityComparer(SigComparerOptions.ComparePropertyDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly PropertyEqualityComparer DontCompareDeclaringTypes = new PropertyEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public PropertyEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(PropertyDef x, PropertyDef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(PropertyDef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares events
	/// </summary>
	public sealed class EventEqualityComparer : IEqualityComparer<EventDef> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly EventEqualityComparer CompareDeclaringTypes = new EventEqualityComparer(SigComparerOptions.CompareEventDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly EventEqualityComparer DontCompareDeclaringTypes = new EventEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public EventEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(EventDef x, EventDef y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(EventDef obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares calling convention signatures
	/// </summary>
	public sealed class SignatureEqualityComparer : IEqualityComparer<CallingConventionSig>, IEqualityComparer<MethodBaseSig>, IEqualityComparer<MethodSig>, IEqualityComparer<PropertySig>, IEqualityComparer<FieldSig>, IEqualityComparer<LocalSig>, IEqualityComparer<GenericInstMethodSig> {
		readonly SigComparerOptions options;

		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly SignatureEqualityComparer Instance = new SignatureEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public SignatureEqualityComparer(SigComparerOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(CallingConventionSig x, CallingConventionSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(CallingConventionSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodBaseSig x, MethodBaseSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodBaseSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodSig x, MethodSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(PropertySig x, PropertySig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(PropertySig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(FieldSig x, FieldSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(FieldSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(LocalSig x, LocalSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(LocalSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(GenericInstMethodSig x, GenericInstMethodSig y) {
			return new SigComparer { Options = options }.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(GenericInstMethodSig obj) {
			return new SigComparer { Options = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Decides how to compare types, sigs, etc
	/// </summary>
	[Flags]
	public enum SigComparerOptions : uint {
		/// <summary>
		/// Don't compare a type's (assembly/module) scope
		/// </summary>
		DontCompareTypeScope = 1,

		/// <summary>
		/// Compares a method/field's declaring type.
		/// </summary>
		CompareMethodFieldDeclaringType = 2,

		/// <summary>
		/// Compares a property's declaring type
		/// </summary>
		ComparePropertyDeclaringType = 4,

		/// <summary>
		/// Compares an event's declaring type
		/// </summary>
		CompareEventDeclaringType = 8,

		/// <summary>
		/// Compares parameters after a sentinel in method sigs. Should not be enabled when
		/// comparing <see cref="MethodSig"/>s against <see cref="MethodInfo"/>s since it's
		/// not possible to get those sentinel params from a <see cref="MethodInfo"/>.
		/// </summary>
		CompareSentinelParams = 0x10,

		/// <summary>
		/// Compares assembly public key token
		/// </summary>
		CompareAssemblyPublicKeyToken = 0x20,

		/// <summary>
		/// Compares assembly version
		/// </summary>
		CompareAssemblyVersion = 0x40,

		/// <summary>
		/// Compares assembly locale
		/// </summary>
		CompareAssemblyLocale = 0x80,

		/// <summary>
		/// If set, a <see cref="TypeRef"/> and an <see cref="ExportedType"/> can reference the
		/// global <c>&lt;Module&gt;</c> type.
		/// </summary>
		TypeRefCanReferenceGlobalType = 0x100,

		/// <summary>
		/// Don't compare a method/property's return type
		/// </summary>
		DontCompareReturnType = 0x200,

		/// <summary>
		/// If set, all generic parameters are replaced with their generic arguments prior
		/// to comparing types. You should enable this when comparing a method, field, property
		/// or an event to a <see cref="MethodInfo"/>, <see cref="FieldInfo"/>,
		/// <see cref="PropertyInfo"/> or an <see cref="EventInfo"/> if the owner type could
		/// be a generic instance type.
		/// </summary>
		SubstituteGenericParameters = 0x400,
	}

	/// <summary>
	/// Compares types, signatures, methods, fields, properties, events
	/// </summary>
	public struct SigComparer {
		const int HASHCODE_MAGIC_GLOBAL_TYPE = 1654396648;
		const int HASHCODE_MAGIC_NESTED_TYPE = -1049070942;
		const int HASHCODE_MAGIC_ET_MODULE = -299744851;
		const int HASHCODE_MAGIC_ET_VALUEARRAY = -674970533;
		const int HASHCODE_MAGIC_ET_GENERICINST = -2050514639;
		const int HASHCODE_MAGIC_ET_VAR = 1288450097;
		const int HASHCODE_MAGIC_ET_MVAR = -990598495;
		const int HASHCODE_MAGIC_ET_ARRAY = -96331531;
		const int HASHCODE_MAGIC_ET_SZARRAY = 871833535;
		const int HASHCODE_MAGIC_ET_BYREF = -634749586;
		const int HASHCODE_MAGIC_ET_PTR = 1976400808;
		const int HASHCODE_MAGIC_ET_SENTINEL = 68439620;

		// FnPtr is mapped to System.IntPtr, so use the same hash code for both
		// IMPORTANT: This must match GetHashCode(TYPE)
		static readonly int HASHCODE_MAGIC_ET_FNPTR_AND_I = UTF8String.GetHashCode(new UTF8String("System")) + UTF8String.GetHashCode(new UTF8String("IntPtr"));

		RecursionCounter recursionCounter;
		SigComparerOptions options;
		GenericArguments genericArguments;

		/// <summary>
		/// Gets/sets the options
		/// </summary>
		public SigComparerOptions Options {
			get { return options; }
			set { options = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.DontCompareTypeScope"/> bit
		/// </summary>
		public bool DontCompareTypeScope {
			get { return (options & SigComparerOptions.DontCompareTypeScope) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.DontCompareTypeScope;
				else
					options &= ~SigComparerOptions.DontCompareTypeScope;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareMethodFieldDeclaringType"/> bit
		/// </summary>
		public bool CompareMethodFieldDeclaringType {
			get { return (options & SigComparerOptions.CompareMethodFieldDeclaringType) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareMethodFieldDeclaringType;
				else
					options &= ~SigComparerOptions.CompareMethodFieldDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.ComparePropertyDeclaringType"/> bit
		/// </summary>
		public bool ComparePropertyDeclaringType {
			get { return (options & SigComparerOptions.ComparePropertyDeclaringType) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.ComparePropertyDeclaringType;
				else
					options &= ~SigComparerOptions.ComparePropertyDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareEventDeclaringType"/> bit
		/// </summary>
		public bool CompareEventDeclaringType {
			get { return (options & SigComparerOptions.CompareEventDeclaringType) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareEventDeclaringType;
				else
					options &= ~SigComparerOptions.CompareEventDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareSentinelParams"/> bit
		/// </summary>
		public bool CompareSentinelParams {
			get { return (options & SigComparerOptions.CompareSentinelParams) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareSentinelParams;
				else
					options &= ~SigComparerOptions.CompareSentinelParams;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareAssemblyPublicKeyToken"/> bit
		/// </summary>
		public bool CompareAssemblyPublicKeyToken {
			get { return (options & SigComparerOptions.CompareAssemblyPublicKeyToken) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareAssemblyPublicKeyToken;
				else
					options &= ~SigComparerOptions.CompareAssemblyPublicKeyToken;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareAssemblyVersion"/> bit
		/// </summary>
		public bool CompareAssemblyVersion {
			get { return (options & SigComparerOptions.CompareAssemblyVersion) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareAssemblyVersion;
				else
					options &= ~SigComparerOptions.CompareAssemblyVersion;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.CompareAssemblyLocale"/> bit
		/// </summary>
		public bool CompareAssemblyLocale {
			get { return (options & SigComparerOptions.CompareAssemblyLocale) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.CompareAssemblyLocale;
				else
					options &= ~SigComparerOptions.CompareAssemblyLocale;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.TypeRefCanReferenceGlobalType"/> bit
		/// </summary>
		public bool TypeRefCanReferenceGlobalType {
			get { return (options & SigComparerOptions.TypeRefCanReferenceGlobalType) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.TypeRefCanReferenceGlobalType;
				else
					options &= ~SigComparerOptions.TypeRefCanReferenceGlobalType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.DontCompareReturnType"/> bit
		/// </summary>
		public bool DontCompareReturnType {
			get { return (options & SigComparerOptions.DontCompareReturnType) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.DontCompareReturnType;
				else
					options &= ~SigComparerOptions.DontCompareReturnType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="SigComparerOptions.SubstituteGenericParameters"/> bit
		/// </summary>
		public bool SubstituteGenericParameters {
			get { return (options & SigComparerOptions.SubstituteGenericParameters) != 0; }
			set {
				if (value)
					options |= SigComparerOptions.SubstituteGenericParameters;
				else
					options &= ~SigComparerOptions.SubstituteGenericParameters;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public SigComparer(SigComparerOptions options) {
			this.recursionCounter = new RecursionCounter();
			this.options = options;
			this.genericArguments = null;
		}

		SigComparerOptions ClearOptions(SigComparerOptions flags) {
			var old = options;
			options &= ~flags;
			return old;
		}

		SigComparerOptions SetOptions(SigComparerOptions flags) {
			var old = options;
			options |= flags;
			return old;
		}

		void RestoreOptions(SigComparerOptions oldFlags) {
			options = oldFlags;
		}

		void InitializeGenericArguments() {
			if (genericArguments == null)
				genericArguments = new GenericArguments();
		}

		static GenericInstSig GetGenericInstanceType(IMemberRefParent parent) {
			var ts = parent as TypeSpec;
			if (ts == null)
				return null;
			return TypeSig.RemoveModifiers(ts.TypeSig) as GenericInstSig;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IType a, IType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			TypeDef tda = a as TypeDef, tdb = b as TypeDef;
			if (tda != null && tdb != null) {
				result = Equals(tda, tdb);
				goto exit;
			}
			TypeRef tra = a as TypeRef, trb = b as TypeRef;
			if (tra != null && trb != null) {
				result = Equals(tra, trb);
				goto exit;
			}
			TypeSpec tsa = a as TypeSpec, tsb = b as TypeSpec;
			if (tsa != null && tsb != null) {
				result = Equals(tsa, tsb);
				goto exit;
			}
			TypeSig sa = a as TypeSig, sb = b as TypeSig;
			if (sa != null && sb != null) {
				result = Equals(sa, sb);
				goto exit;
			}
			ExportedType eta = a as ExportedType, etb = b as ExportedType;
			if (eta != null && etb != null) {
				result = Equals(eta, etb);
				goto exit;
			}

			if (tda != null && trb != null)
				result = Equals(tda, trb);		// TypeDef vs TypeRef
			else if (tra != null && tdb != null)
				result = Equals(tdb, tra);		// TypeDef vs TypeRef
			else if (tda != null && tsb != null)
				result = Equals(tda, tsb);		// TypeDef vs TypeSpec
			else if (tsa != null && tdb != null)
				result = Equals(tdb, tsa);		// TypeDef vs TypeSpec
			else if (tda != null && sb != null)
				result = Equals(tda, sb);		// TypeDef vs TypeSig
			else if (sa != null && tdb != null)
				result = Equals(tdb, sa);		// TypeDef vs TypeSig
			else if (tda != null && etb != null)
				result = Equals(tda, etb);		// TypeDef vs ExportedType
			else if (eta != null && tdb != null)
				result = Equals(tdb, eta);		// TypeDef vs ExportedType
			else if (tra != null && tsb != null)
				result = Equals(tra, tsb);		// TypeRef vs TypeSpec
			else if (tsa != null && trb != null)
				result = Equals(trb, tsa);		// TypeRef vs TypeSpec
			else if (tra != null && sb != null)
				result = Equals(tra, sb);		// TypeRef vs TypeSig
			else if (sa != null && trb != null)
				result = Equals(trb, sa);		// TypeRef vs TypeSig
			else if (tra != null && etb != null)
				result = Equals(tra, etb);		// TypeRef vs ExportedType
			else if (eta != null && trb != null)
				result = Equals(trb, eta);		// TypeRef vs ExportedType
			else if (tsa != null && sb != null)
				result = Equals(tsa, sb);		// TypeSpec vs TypeSig
			else if (sa != null && tsb != null)
				result = Equals(tsb, sa);		// TypeSpec vs TypeSig
			else if (tsa != null && etb != null)
				result = Equals(tsa, etb);		// TypeSpec vs ExportedType
			else if (eta != null && tsb != null)
				result = Equals(tsb, eta);		// TypeSpec vs ExportedType
			else if (sa != null && etb != null)
				result = Equals(sa, etb);		// TypeSig vs ExportedType
			else if (eta != null && sb != null)
				result = Equals(sb, eta);		// TypeSig vs ExportedType
			else
				result = false;	// Should never be reached

exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IType a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			var td = a as TypeDef;
			if (td != null) {
				hash = GetHashCode(td);
				goto exit;
			}
			var tr = a as TypeRef;
			if (tr != null) {
				hash = GetHashCode(tr);
				goto exit;
			}
			var ts = a as TypeSpec;
			if (ts != null) {
				hash = GetHashCode(ts);
				goto exit;
			}
			var sig = a as TypeSig;
			if (sig != null) {
				hash = GetHashCode(sig);
				goto exit;
			}
			var et = a as ExportedType;
			if (et != null) {
				hash = GetHashCode(et);
				goto exit;
			}
			hash = 0;	// Should never be reached
exit:
			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, TypeDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, TypeRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result = false;

			if (!UTF8String.Equals(a.Name, b.Name) || !UTF8String.Equals(a.Namespace, b.Namespace))
				goto exit;

			var scope = b.ResolutionScope;
			var dtb = scope as TypeRef;
			if (dtb != null) {	// nested type
				result = Equals(a.DeclaringType, dtb);	// Compare enclosing types
				goto exit;
			}
			if (a.DeclaringType != null)
				goto exit;	// a is nested, b isn't

			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			var bMod = scope as IModule;
			if (bMod != null) {	// 'b' is defined in the same assembly as 'a'
				result = Equals((IModule)a.OwnerModule, (IModule)bMod) &&
						Equals(a.DefinitionAssembly, b.DefinitionAssembly);
				goto exit;
			}
			var bAsm = scope as AssemblyRef;
			if (bAsm != null) {
				var aMod = a.OwnerModule;
				result = aMod != null && Equals(aMod.Assembly, bAsm);
				goto exit;
			}
			//TODO: Handle the case where scope == null
exit:
			if (result && !TypeRefCanReferenceGlobalType && a.IsGlobalModuleType)
				result = false;
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, TypeDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result = false;

			if (!UTF8String.Equals(a.Name, b.TypeName) || !UTF8String.Equals(a.Namespace, b.TypeNamespace))
				goto exit;

			var scope = b.Implementation;
			var dtb = scope as ExportedType;
			if (dtb != null) {	// nested type
				result = Equals(a.DeclaringType, dtb);	// Compare enclosing types
				goto exit;
			}
			if (a.DeclaringType != null)
				goto exit;	// a is nested, b isn't

			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			var bFile = scope as FileDef;
			if (bFile != null) {
				result = Equals(a.OwnerModule, bFile) &&
						Equals(a.DefinitionAssembly, b.DefinitionAssembly);
				goto exit;
			}
			var bAsm = scope as AssemblyRef;
			if (bAsm != null) {
				var aMod = a.OwnerModule;
				result = aMod != null && Equals(aMod.Assembly, bAsm);
				goto exit;
			}
exit:
			if (result && !TypeRefCanReferenceGlobalType && a.IsGlobalModuleType)
				result = false;
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, TypeDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a, b.TypeSig);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, TypeDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			//*************************************************************
			// If this code gets updated, update GetHashCode(TypeSig),
			// Equals(TypeRef,TypeSig) and Equals(TypeSig,ExportedType) too
			//*************************************************************
			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Equals(a, (IType)b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Equals(a, b.Next);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, TypeRef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a, b.TypeSig);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, TypeRef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.TypeName) &&
					UTF8String.Equals(a.Namespace, b.TypeNamespace) &&
					EqualsScope(a, b);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, TypeRef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			//*************************************************************
			// If this code gets updated, update GetHashCode(TypeSig),
			// Equals(TypeRef,TypeSig) and Equals(TypeSig,ExportedType) too
			//*************************************************************
			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Equals(a, (IType)b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Equals(a, b.Next);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, TypeSpec b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.TypeSig, b);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, TypeSpec b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.TypeSig, b);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, TypeSig b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			//*************************************************************
			// If this code gets updated, update GetHashCode(TypeSig),
			// Equals(TypeRef,TypeSig) and Equals(TypeSig,ExportedType) too
			//*************************************************************
			var a2 = a as TypeDefOrRefSig;
			if (a2 != null)
				result = Equals(a2.TypeDefOrRef, b);
			else if (a is ModifierSig || a is PinnedSig)
				result = Equals(a.Next, b);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		int GetHashCodeGlobalType() {
			// We don't always know the name+namespace of the global type, eg. when it's
			// referenced by a ModuleRef. Use the same hash for all global types.
			return HASHCODE_MAGIC_GLOBAL_TYPE;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, TypeRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					UTF8String.Equals(a.Namespace, b.Namespace) &&
					EqualsResolutionScope(a, b);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeRef a) {
			// ************************************************************************************
			// IMPORTANT: This hash code must match the Type/TypeRef/TypeDef/ExportedType
			// hash code and HASHCODE_MAGIC_ET_FNPTR_AND_I constant
			// ************************************************************************************

			// See GetHashCode(Type) for the reason why null returns GetHashCodeGlobalType()
			if (a == null)
				return TypeRefCanReferenceGlobalType ? GetHashCodeGlobalType() : 0;
			int hash;
			hash = UTF8String.GetHashCode(a.Name);
			if (a.ResolutionScope is TypeRef)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += UTF8String.GetHashCode(a.Namespace);
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, ExportedType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.TypeName, b.TypeName) &&
					UTF8String.Equals(a.TypeNamespace, b.TypeNamespace) &&
					EqualsImplementation(a, b);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(ExportedType a) {
			// ************************************************************************************
			// IMPORTANT: This hash code must match the Type/TypeRef/TypeDef/ExportedType
			// hash code and HASHCODE_MAGIC_ET_FNPTR_AND_I constant
			// ************************************************************************************

			// See GetHashCode(Type) for the reason why null returns GetHashCodeGlobalType()
			if (a == null)
				return TypeRefCanReferenceGlobalType ? GetHashCodeGlobalType() : 0;
			int hash;
			hash = UTF8String.GetHashCode(a.TypeName);
			if (a.Implementation is ExportedType)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += UTF8String.GetHashCode(a.TypeNamespace);
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, TypeDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					UTF8String.Equals(a.Namespace, b.Namespace) &&
					Equals(a.DeclaringType, b.DeclaringType) &&
					(DontCompareTypeScope || Equals(a.OwnerModule, b.OwnerModule));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeDef a) {
			// ************************************************************************************
			// IMPORTANT: This hash code must match the Type/TypeRef/TypeDef/ExportedType
			// hash code and HASHCODE_MAGIC_ET_FNPTR_AND_I constant
			// ************************************************************************************

			// See GetHashCode(Type) for the reason why null returns GetHashCodeGlobalType()
			if (a == null || a.IsGlobalModuleType)
				return GetHashCodeGlobalType();
			int hash;
			hash = UTF8String.GetHashCode(a.Name);
			if (a.DeclaringType != null)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += UTF8String.GetHashCode(a.Namespace);
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, TypeSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals(a.TypeSig, b.TypeSig);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeSpec a) {
			if (a == null)
				return 0;
			return GetHashCode(a.TypeSig);
		}

		/// <summary>
		/// Compares resolution scopes
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool EqualsResolutionScope(TypeRef a, TypeRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			var ra = a.ResolutionScope;
			var rb = b.ResolutionScope;
			if (ra == rb)
				return true;
			if (ra == null || rb == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			TypeRef ea = ra as TypeRef, eb = rb as TypeRef;
			if (ea != null || eb != null) {	// if one of them is a TypeRef, the other one must be too
				result = Equals(ea, eb);
				goto exit;
			}
			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			IModule ma = ra as IModule, mb = rb as IModule;
			if (ma != null && mb != null) {	// only compare if both are modules
				result = Equals(ma, mb) && Equals(a.DefinitionAssembly, b.DefinitionAssembly);
				goto exit;
			}
			AssemblyRef aa = ra as AssemblyRef, ab = rb as AssemblyRef;
			if (aa != null && ab != null) {	// only compare if both are assemblies
				result = Equals((IAssembly)aa, (IAssembly)ab);
				goto exit;
			}
			ModuleRef modRef = rb as ModuleRef;
			if (aa != null && modRef != null) {
				var bMod = b.OwnerModule;
				result = bMod != null && Equals(aa, bMod.Assembly);
				goto exit;
			}
			modRef = ra as ModuleRef;
			if (ab != null && modRef != null) {
				var aMod = a.OwnerModule;
				result = aMod != null && Equals(ab, aMod.Assembly);
				goto exit;
			}
			ModuleDef modDef = rb as ModuleDef;
			if (aa != null && modDef != null) {
				result = Equals(aa, modDef.Assembly);
				goto exit;
			}
			modDef = ra as ModuleDef;
			if (ab != null && modDef != null) {
				result = Equals(ab, modDef.Assembly);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares implementation
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool EqualsImplementation(ExportedType a, ExportedType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			var ia = a.Implementation;
			var ib = b.Implementation;
			if (ia == ib)
				return true;
			if (ia == null || ib == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			ExportedType ea = ia as ExportedType, eb = ib as ExportedType;
			if (ea != null || eb != null) {	// if one of them is a ExportedType, the other one must be too
				result = Equals(ea, eb);
				goto exit;
			}
			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			FileDef fa = ia as FileDef, fb = ib as FileDef;
			if (fa != null && fb != null) {	// only compare if both are files
				result = Equals(fa, fb);
				goto exit;
			}
			AssemblyRef aa = ia as AssemblyRef, ab = ib as AssemblyRef;
			if (aa != null && ab != null) {	// only compare if both are assemblies
				result = Equals((IAssembly)aa, (IAssembly)ab);
				goto exit;
			}
			if (fa != null && ab != null) {
				result = Equals(a.DefinitionAssembly, ab);
				goto exit;
			}
			if (fb != null && aa != null) {
				result = Equals(b.DefinitionAssembly, aa);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares resolution scope and implementation
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool EqualsScope(TypeRef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			var ra = a.ResolutionScope;
			var ib = b.Implementation;
			if (ra == ib)
				return true;
			if (ra == null || ib == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var ea = ra as TypeRef;
			var eb = ib as ExportedType;
			if (ea != null || eb != null) {	// If one is a nested type, the other one must be too
				result = Equals(ea, eb);
				goto exit;
			}
			var ma = ra as IModule;
			var fb = ib as FileDef;
			if (ma != null && fb != null) {
				result = Equals(ma, fb) && Equals(a.DefinitionAssembly, b.DefinitionAssembly);
				goto exit;
			}
			var aa = ra as AssemblyRef;
			var ab = ib as AssemblyRef;
			if (aa != null && ab != null) {
				result = Equals(aa, ab);
				goto exit;
			}
			if (ma != null && ab != null) {
				result = Equals(a.DefinitionAssembly, ab);
				goto exit;
			}
			if (fb != null && aa != null) {
				result = Equals(b.DefinitionAssembly, aa);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares files
		/// </summary>
		/// <param name="a">File #1</param>
		/// <param name="b">File #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(FileDef a, FileDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name);
		}

		/// <summary>
		/// Compares a module with a file
		/// </summary>
		/// <param name="a">Module</param>
		/// <param name="b">File</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IModule a, FileDef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;

			//TODO: You should compare against the module's file name, not the name in the metadata!
			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name);
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal bool Equals(IModule a, IModule b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name);
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(ModuleDef a, ModuleDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			bool result = Equals((IModule)a, (IModule)b) && Equals(a.Assembly, b.Assembly);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares assemblies
		/// </summary>
		/// <param name="a">Assembly #1</param>
		/// <param name="b">Assembly #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IAssembly a, IAssembly b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.CaseInsensitiveEquals(a.Name, b.Name) &&
				(!CompareAssemblyPublicKeyToken || PublicKeyBase.TokenEquals(a.PublicKeyOrToken, b.PublicKeyOrToken)) &&
				(!CompareAssemblyVersion || Utils.Equals(a.Version, b.Version)) &&
				(!CompareAssemblyLocale || Utils.LocaleEquals(a.Locale, b.Locale));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, TypeSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.ElementType != b.ElementType) {
				// Signatures must be identical. It's possible to have a U4 in a sig (short form
				// of System.UInt32), or a ValueType + System.UInt32 TypeRef (long form), but these
				// should not match in a sig (also the long form is invalid).
				result = false;
			}
			else {
				switch (a.ElementType) {
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
				case ElementType.TypedByRef:
				case ElementType.I:
				case ElementType.U:
				case ElementType.Object:
				case ElementType.Sentinel:
					result = true;
					break;

				case ElementType.Ptr:
				case ElementType.ByRef:
				case ElementType.SZArray:
				case ElementType.Pinned:
					result = Equals(a.Next, b.Next);
					break;

				case ElementType.Array:
					ArraySig ara = a as ArraySig, arb = b as ArraySig;
					//TODO: Should Sizes and LowerBounds be compared at all?
					result = ara.Rank == arb.Rank &&
							Equals(ara.Sizes, arb.Sizes) &&
							Equals(ara.LowerBounds, arb.LowerBounds) &&
							Equals(a.Next, b.Next);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					result = Equals((IType)(a as ClassOrValueTypeSig).TypeDefOrRef, (IType)(b as ClassOrValueTypeSig).TypeDefOrRef);
					break;

				case ElementType.Var:
				case ElementType.MVar:
					result = (a as GenericSig).Number == (b as GenericSig).Number;
					break;

				case ElementType.GenericInst:
					var gia = (GenericInstSig)a;
					var gib = (GenericInstSig)b;
					result = Equals(gia.GenericType, gib.GenericType) &&
							Equals(gia.GenericArguments, gib.GenericArguments);
					break;

				case ElementType.FnPtr:
					result = Equals((a as FnPtrSig).Signature, (b as FnPtrSig).Signature);
					break;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
					result = Equals((IType)(a as ModifierSig).Modifier, (IType)(b as ModifierSig).Modifier) && Equals(a.Next, b.Next);
					break;

				case ElementType.ValueArray:
					result = (a as ValueArraySig).Size == (b as ValueArraySig).Size && Equals(a.Next, b.Next);
					break;

				case ElementType.Module:
					result = (a as ModuleSig).Index == (b as ModuleSig).Index && Equals(a.Next, b.Next);
					break;

				case ElementType.End:
				case ElementType.R:
				case ElementType.Internal:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			if (genericArguments != null)
				a = genericArguments.Resolve(a);

			switch (a.ElementType) {
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
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				// When comparing an ExportedType/TypeDef/TypeRef to a TypeDefOrRefSig/Class/ValueType,
				// the ET is ignored, so we must ignore it when calculating the hash.
				hash = GetHashCode((IType)(a as TypeDefOrRefSig).TypeDefOrRef);
				break;

			case ElementType.Sentinel:
				hash = HASHCODE_MAGIC_ET_SENTINEL;
				break;

			case ElementType.Ptr:
				hash = HASHCODE_MAGIC_ET_PTR + GetHashCode(a.Next);
				break;

			case ElementType.ByRef:
				hash = HASHCODE_MAGIC_ET_BYREF + GetHashCode(a.Next);
				break;

			case ElementType.SZArray:
				hash = HASHCODE_MAGIC_ET_SZARRAY + GetHashCode(a.Next);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
				// When comparing an ExportedType/TypeDef/TypeRef to a ModifierSig/PinnedSig,
				// the ET is ignored, so we must ignore it when calculating the hash.
				hash = GetHashCode(a.Next);
				break;

			case ElementType.Array:
				// Don't include sizes and lower bounds since GetHashCode(Type) doesn't (and can't).
				ArraySig ara = (ArraySig)a;
				hash = HASHCODE_MAGIC_ET_ARRAY + (int)ara.Rank + GetHashCode(ara.Next);
				break;

			case ElementType.Var:
				hash = HASHCODE_MAGIC_ET_VAR + (int)(a as GenericVar).Number;
				break;

			case ElementType.MVar:
				hash = HASHCODE_MAGIC_ET_MVAR + (int)(a as GenericMVar).Number;
				break;

			case ElementType.GenericInst:
				var gia = (GenericInstSig)a;
				hash = HASHCODE_MAGIC_ET_GENERICINST;
				if (SubstituteGenericParameters) {
					InitializeGenericArguments();
					genericArguments.PushTypeArgs(gia.GenericArguments);
					hash += GetHashCode(gia.GenericType);
					genericArguments.PopTypeArgs();
				}
				else
					hash += GetHashCode(gia.GenericType);
				hash += GetHashCode(gia.GenericArguments);
				break;

			case ElementType.FnPtr:
				hash = HASHCODE_MAGIC_ET_FNPTR_AND_I;
				break;

			case ElementType.ValueArray:
				hash = HASHCODE_MAGIC_ET_VALUEARRAY + (int)(a as ValueArraySig).Size + GetHashCode(a.Next);
				break;

			case ElementType.Module:
				hash = HASHCODE_MAGIC_ET_MODULE + (int)(a as ModuleSig).Index + GetHashCode(a.Next);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				hash = 0;
				break;
			}

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares type lists
		/// </summary>
		/// <param name="a">Type list #1</param>
		/// <param name="b">Type list #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IList<TypeSig> a, IList<TypeSig> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < a.Count; i++) {
					if (!Equals(a[i], b[i]))
						break;
				}
				result = i == a.Count;
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type list
		/// </summary>
		/// <param name="a">The type list</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IList<TypeSig> a) {
			//************************************************************************
			// IMPORTANT: This code must match any other GetHashCode(IList<SOME_TYPE>)
			//************************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += (uint)GetHashCode(a[i]);
				hash = (hash << 13) | (hash >> 19);
			}
			recursionCounter.Decrement();
			return (int)hash;
		}

		bool Equals(IList<uint> a, IList<uint> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		bool Equals(IList<int> a, IList<int> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Compares signatures
		/// </summary>
		/// <param name="a">Sig #1</param>
		/// <param name="b">Sig #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(CallingConventionSig a, CallingConventionSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.GetCallingConvention() != b.GetCallingConvention())
				result = false;
			else {
				switch (a.GetCallingConvention() & CallingConvention.Mask) {
				case CallingConvention.Default:
				case CallingConvention.C:
				case CallingConvention.StdCall:
				case CallingConvention.ThisCall:
				case CallingConvention.FastCall:
				case CallingConvention.VarArg:
				case CallingConvention.Property:
					MethodBaseSig ma = a as MethodBaseSig, mb = b as MethodBaseSig;
					result = ma != null && mb != null && Equals(ma, mb);
					break;

				case CallingConvention.Field:
					FieldSig fa = a as FieldSig, fb = b as FieldSig;
					result = fa != null && fb != null && Equals(fa, fb);
					break;

				case CallingConvention.LocalSig:
					LocalSig la = a as LocalSig, lb = b as LocalSig;
					result = la != null && lb != null && Equals(la, lb);
					break;

				case CallingConvention.GenericInst:
					GenericInstMethodSig ga = a as GenericInstMethodSig, gb = b as GenericInstMethodSig;
					result = ga != null && gb != null && Equals(ga, gb);
					break;

				case CallingConvention.Unmanaged:
				case CallingConvention.NativeVarArg:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a sig
		/// </summary>
		/// <param name="a">The sig</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(CallingConventionSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			switch (a.GetCallingConvention() & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Property:
				MethodBaseSig ma = a as MethodBaseSig;
				hash = ma == null ? 0 : GetHashCode(ma);
				break;

			case CallingConvention.Field:
				FieldSig fa = a as FieldSig;
				hash = fa == null ? 0 : GetHashCode(fa);
				break;

			case CallingConvention.LocalSig:
				LocalSig la = a as LocalSig;
				hash = la == null ? 0 : GetHashCode(la);
				break;

			case CallingConvention.GenericInst:
				GenericInstMethodSig ga = a as GenericInstMethodSig;
				hash = ga == null ? 0 : GetHashCode(ga);
				break;

			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			default:
				hash = GetHashCode_CallingConvention(a);
				break;
			}

			return hash;
		}

		/// <summary>
		/// Compares method/property sigs
		/// </summary>
		/// <param name="a">Method/property #1</param>
		/// <param name="b">Method/property #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBaseSig a, MethodBaseSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() &&
					(DontCompareReturnType || Equals(a.RetType, b.RetType)) &&
					Equals(a.Params, b.Params) &&
					(!a.Generic || a.GenParamCount == b.GenParamCount) &&
					(!CompareSentinelParams || Equals(a.ParamsAfterSentinel, b.ParamsAfterSentinel));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a method/property sig
		/// </summary>
		/// <param name="a">The method/property sig</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(MethodBaseSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) +
					GetHashCode(a.Params);
			if (!DontCompareReturnType)
				hash += GetHashCode(a.RetType);
			if (a.Generic)
				hash += GetHashCode_ElementType_MVar((int)a.GenParamCount);
			if (CompareSentinelParams)
				hash += GetHashCode(a.ParamsAfterSentinel);

			recursionCounter.Decrement();
			return hash;
		}

		int GetHashCode_CallingConvention(CallingConventionSig a) {
			return GetHashCode(a.GetCallingConvention());
		}

		int GetHashCode(CallingConvention a) {
			//*******************************************************************
			// IMPORTANT: This hash must match the Reflection call conv hash code
			//*******************************************************************

			switch (a & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Property:
			case CallingConvention.GenericInst:
			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			case CallingConvention.Field:
				return (int)(a & (CallingConvention.Generic | CallingConvention.HasThis | CallingConvention.ExplicitThis));

			case CallingConvention.LocalSig:
			default:
				return (int)a;
			}
		}

		/// <summary>
		/// Compares field sigs
		/// </summary>
		/// <param name="a">Field sig #1</param>
		/// <param name="b">Field sig #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(FieldSig a, FieldSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.Type, b.Type);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a field sig
		/// </summary>
		/// <param name="a">The field sig</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(FieldSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.Type);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares local sigs
		/// </summary>
		/// <param name="a">Local sig #1</param>
		/// <param name="b">Local sig #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(LocalSig a, LocalSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.Locals, b.Locals);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a local sig
		/// </summary>
		/// <param name="a">The local sig</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(LocalSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.Locals);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares generic method instance sigs
		/// </summary>
		/// <param name="a">Generic inst method #1</param>
		/// <param name="b">Generic inst method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(GenericInstMethodSig a, GenericInstMethodSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.GenericArguments, b.GenericArguments);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a generic instance method sig
		/// </summary>
		/// <param name="a">The generic inst method sig</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(GenericInstMethodSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.GenericArguments);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IMethod a, IMethod b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			MethodDef mda = a as MethodDef, mdb = b as MethodDef;
			if (mda != null && mdb != null) {
				result = Equals(mda, mdb);
				goto exit;
			}
			MemberRef mra = a as MemberRef, mrb = b as MemberRef;
			if (mra != null && mrb != null) {
				result = Equals(mra, mrb);
				goto exit;
			}
			MethodSpec msa = a as MethodSpec, msb = b as MethodSpec;
			if (msa != null && msb != null) {
				result = Equals(msa, msb);
				goto exit;
			}
			if (mda != null && mrb != null) {
				result = Equals(mda, mrb);
				goto exit;
			}
			if (mra != null && mdb != null) {
				result = Equals(mdb, mra);
				goto exit;
			}
			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a method
		/// </summary>
		/// <param name="a">The method</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IMethod a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			MethodDef mda = a as MethodDef;
			if (mda != null) {
				hash = GetHashCode(mda);
				goto exit;
			}
			MemberRef mra = a as MemberRef;
			if (mra != null) {
				hash = GetHashCode(mra);
				goto exit;
			}
			MethodSpec msa = a as MethodSpec;
			if (msa != null) {
				hash = GetHashCode(msa);
				goto exit;
			}
			hash = 0;
exit:
			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MemberRef a, MethodDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			//TODO: If a.IsPrivateScope, then you should probably always return false since Method
			//		tokens must be used to call the method.

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodDef a, MethodDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a method
		/// </summary>
		/// <param name="a">The method</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(MethodDef a) {
			// ************************************************************
			// IMPORTANT: This hash code must match the MemberRef hash code
			// ************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) +
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares <c>MemberRef</c>s
		/// </summary>
		/// <param name="a"><c>MemberRef</c> #1</param>
		/// <param name="b"><c>MemberRef</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MemberRef a, MemberRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.Class, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a <c>MemberRef</c>
		/// </summary>
		/// <param name="a">The <c>MemberRef</c></param>
		/// <returns>The hash code</returns>
		public int GetHashCode(MemberRef a) {
			// *********************************************************************
			// IMPORTANT: This hash code must match the MethodDef/FieldDef hash code
			// *********************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name);
			GenericInstSig git;
			if (SubstituteGenericParameters && (git = GetGenericInstanceType(a.Class)) != null) {
				InitializeGenericArguments();
				genericArguments.PushTypeArgs(git.GenericArguments);
				hash += GetHashCode(a.Signature);
				genericArguments.PopTypeArgs();
			}
			else
				hash += GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.Class);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares <c>MethodSpec</c>s
		/// </summary>
		/// <param name="a"><c>MethodSpec</c> #1</param>
		/// <param name="b"><c>MethodSpec</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodSpec a, MethodSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals(a.Method, b.Method) && Equals(a.Instantiation, b.Instantiation);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a <c>MethodSpec</c>
		/// </summary>
		/// <param name="a">The <c>MethodSpec</c></param>
		/// <returns>The hash code</returns>
		public int GetHashCode(MethodSpec a) {
			// *************************************************************
			// IMPORTANT: This hash code must match the MethodBase hash code
			// *************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			// We must do this or it won't get the same hash code as some MethodInfos
			var oldOptions = SetOptions(SigComparerOptions.SubstituteGenericParameters);
			var gim = a.GenericInstMethodSig;
			if (gim != null) {
				InitializeGenericArguments();
				genericArguments.PushMethodArgs(gim.GenericArguments);
			}
			int hash = GetHashCode(a.Method);
			if (gim != null)
				genericArguments.PopMethodArgs();
			RestoreOptions(oldOptions);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares <c>MemberRefParent</c>s
		/// </summary>
		/// <param name="a"><c>MemberRefParent</c> #1</param>
		/// <param name="b"><c>MemberRefParent</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IMemberRefParent a, IMemberRefParent b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			ITypeDefOrRef ita = a as ITypeDefOrRef, itb = b as ITypeDefOrRef;
			if (ita != null && itb != null) {
				result = Equals((IType)ita, (IType)itb);
				goto exit;
			}
			ModuleRef moda = a as ModuleRef, modb = b as ModuleRef;
			if (moda != null && modb != null) {
				ModuleDef omoda = moda.OwnerModule, omodb = modb.OwnerModule;
				result = Equals((IModule)moda, (IModule)modb) &&
						Equals(omoda == null ? null : omoda.Assembly, omodb == null ? null : omodb.Assembly);
				goto exit;
			}
			MethodDef ma = a as MethodDef, mb = b as MethodDef;
			if (ma != null && mb != null) {
				result = Equals(ma, mb);
				goto exit;
			}
			var td = a as TypeDef;
			if (td != null && modb != null) {
				result = EqualsGlobal(td, modb);
				goto exit;
			}
			td = b as TypeDef;
			if (td != null && moda != null) {
				result = EqualsGlobal(td, moda);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a <c>MemberRefParent</c>
		/// </summary>
		/// <param name="a">The <c>MemberRefParent</c></param>
		/// <returns>The hash code</returns>
		int GetHashCode(IMemberRefParent a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			ITypeDefOrRef ita = a as ITypeDefOrRef;
			if (ita != null) {
				hash = GetHashCode((IType)ita);
				goto exit;
			}
			if (a is ModuleRef) {
				hash = GetHashCodeGlobalType();
				goto exit;
			}
			MethodDef ma = a as MethodDef;
			if (ma != null) {
				// Only use the declaring type so we get the same hash code when hashing a MethodBase.
				hash = GetHashCode(ma.DeclaringType);
				goto exit;
			}
			hash = 0;
exit:
			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IField a, IField b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			FieldDef fa = a as FieldDef, fb = b as FieldDef;
			if (fa != null && fb != null) {
				result = Equals(fa, fb);
				goto exit;
			}
			MemberRef ma = a as MemberRef, mb = b as MemberRef;
			if (ma != null && mb != null) {
				result = Equals(ma, mb);
				goto exit;
			}
			if (fa != null && mb != null) {
				result = Equals(fa, mb);
				goto exit;
			}
			if (fb != null && ma != null) {
				result = Equals(fb, ma);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a field
		/// </summary>
		/// <param name="a">The field</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IField a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			FieldDef fa = a as FieldDef;
			if (fa != null) {
				hash = GetHashCode(fa);
				goto exit;
			}
			MemberRef ma = a as MemberRef;
			if (ma != null) {
				hash = GetHashCode(ma);
				goto exit;
			}
			hash = 0;
exit:
			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MemberRef a, FieldDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(FieldDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			//TODO: If a.IsPrivateScope, then you should probably always return false since Field
			//		tokens must be used to access the field

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(FieldDef a, FieldDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a field
		/// </summary>
		/// <param name="a">The field</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(FieldDef a) {
			// ************************************************************
			// IMPORTANT: This hash code must match the MemberRef hash code
			// ************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) +
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares properties
		/// </summary>
		/// <param name="a">Property #1</param>
		/// <param name="b">Property #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(PropertyDef a, PropertyDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals(a.Type, b.Type) &&
					(!ComparePropertyDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a property
		/// </summary>
		/// <param name="a">The property</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(PropertyDef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) +
					GetHashCode(a.Type);
			if (ComparePropertyDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Compares events
		/// </summary>
		/// <param name="a">Event #1</param>
		/// <param name="b">Event #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(EventDef a, EventDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.Equals(a.Name, b.Name) &&
					Equals((IType)a.Type, (IType)b.Type) &&
					(!CompareEventDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of an event
		/// </summary>
		/// <param name="a">The event</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(EventDef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) +
					GetHashCode((IType)a.Type);
			if (CompareEventDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		// Compares a with b, and a must be the global type
		bool EqualsGlobal(TypeDef a, ModuleRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.IsGlobalModuleType &&
				Equals((IModule)a.OwnerModule, (IModule)b) &&
				Equals(a.DefinitionAssembly, b.OwnerModule == null ? null : b.OwnerModule.Assembly);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, IType b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IType a, Type b) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var td = a as TypeDef;
			if (td != null) {
				result = Equals(td, b);
				goto exit;
			}
			var tr = a as TypeRef;
			if (tr != null) {
				result = Equals(tr, b);
				goto exit;
			}
			var ts = a as TypeSpec;
			if (ts != null) {
				result = Equals(ts, b);
				goto exit;
			}
			var sig = a as TypeSig;
			if (sig != null) {
				result = Equals(sig, b);
				goto exit;
			}
			var et = a as ExportedType;
			if (et != null) {
				result = Equals(et, b);
				goto exit;
			}
			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, TypeDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeDef a, Type b) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (b == null)
				return a.IsGlobalModuleType;
			if (!recursionCounter.Increment())
				return false;

			bool result = !b.HasElementType &&
					UTF8String.ToSystemStringOrEmpty(a.Name) == b.Name &&
					NamespaceEquals(a.Namespace, b) &&
					EnclosingTypeEquals(a.DeclaringType, b.DeclaringType) &&
					(DontCompareTypeScope || Equals(a.OwnerModule, b.Module));

			recursionCounter.Decrement();
			return result;
		}

		bool EnclosingTypeEquals(TypeDef a, Type b) {
			// b == null doesn't mean that b is the global type
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a, b);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, TypeRef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="b">Type #1</param>
		/// <param name="a">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeRef a, Type b) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (b == null)
				return false;	// Must use a ModuleRef to reference the global type, so always fail
			if (!recursionCounter.Increment())
				return false;
			bool result = false;

			if (b.HasElementType)
				goto exit;
			if (UTF8String.ToSystemStringOrEmpty(a.Name) != b.Name || !NamespaceEquals(a.Namespace, b))
				goto exit;

			var scope = a.ResolutionScope;
			var dta = scope as TypeRef;
			if (dta != null) {	// nested type
				result = Equals(dta, b.DeclaringType);	// Compare enclosing types
				goto exit;
			}
			if (b.IsNested)
				goto exit;	// b is nested, a isn't

			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			var aMod = scope as IModule;
			if (aMod != null) {	// 'a' is defined in the same assembly as 'b'
				result = Equals(aMod, b.Module) &&
						Equals(a.DefinitionAssembly, b.Assembly);
				goto exit;
			}
			var aAsm = scope as AssemblyRef;
			if (aAsm != null) {
				result = Equals(aAsm, b.Assembly);
				goto exit;
			}
			//TODO: Handle the case where scope == null
exit:
			recursionCounter.Decrement();
			return result;
		}

		static bool NamespaceEquals(UTF8String a, Type b) {
			if (b.IsNested)
				return true;
			return UTF8String.ToSystemStringOrEmpty(a) == (b.Namespace ?? string.Empty);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, TypeSpec b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSpec a, Type b) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (b == null)
				return false;	// Must use a ModuleRef to reference the global type, so always fail
			return Equals(a.TypeSig, b);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, TypeSig b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, Type b) {
			return Equals(a, b, false);
		}

		bool Equals(ITypeDefOrRef a, Type b, bool treatAsGenericInst) {
			var ts = a as TypeSpec;
			if (ts != null)
				return Equals(ts.TypeSig, b, treatAsGenericInst);
			return Equals(a, b);
		}

		/// <summary>
		/// Checks whether it's FnPtr&amp;, FnPtr*, FnPtr[], or FnPtr[...]
		/// </summary>
		/// <param name="a">The type</param>
		static bool IsFnPtrElementType(Type a) {
			if (a == null || !a.HasElementType)
				return false;
			var et = a.GetElementType();
			if (et == null || et.HasElementType)
				return false;
			if (!IsSystemIntPtr(et))	// FnPtr is mapped to System.IntPtr
				return false;
			if (!a.FullName.StartsWith("(fnptr)"))
				return false;

			return true;
		}

		static bool IsSystemIntPtr(Type a) {
			return a != null &&
				a.IsValueType &&
				a.FullName == "System.IntPtr" &&
				IsCorLib(a.Assembly);
		}

		static bool IsCorLib(Assembly assembly) {
			var asmName = assembly.GetName();
			return (asmName.Name == "mscorlib" || asmName.Name == "System.Runtime") &&
					string.IsNullOrEmpty(asmName.CultureInfo.Name);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <param name="treatAsGenericInst"><c>true</c> if we should treat <paramref name="b"/>
		/// as a generic instance type</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(TypeSig a, Type b, bool treatAsGenericInst) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (b == null)
				return false;	// Must use a ModuleRef to reference the global type, so always fail
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (genericArguments != null)
				a = genericArguments.Resolve(a);

			switch (a.ElementType) {
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
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
				result = Equals(((TypeDefOrRefSig)a).TypeDefOrRef, b, treatAsGenericInst);
				break;

			case ElementType.Ptr:
				if (!b.IsPointer)
					result = false;
				else if (IsFnPtrElementType(b)) {
					a = TypeSig.RemoveModifiers(a.Next);
					result = a != null && a.ElementType == ElementType.FnPtr;
				}
				else
					result = Equals(a.Next, b.GetElementType());
				break;

			case ElementType.ByRef:
				if (!b.IsByRef)
					result = false;
				else if (IsFnPtrElementType(b)) {
					a = TypeSig.RemoveModifiers(a.Next);
					result = a != null && a.ElementType == ElementType.FnPtr;
				}
				else
					result = Equals(a.Next, b.GetElementType());
				break;

			case ElementType.SZArray:
				if (!b.IsArray || !IsSZArray(b))
					result = false;
				else if (IsFnPtrElementType(b)) {
					a = TypeSig.RemoveModifiers(a.Next);
					result = a != null && a.ElementType == ElementType.FnPtr;
				}
				else
					result = Equals(a.Next, b.GetElementType());
				break;

			case ElementType.Pinned:
				result = Equals(a.Next, b, treatAsGenericInst);
				break;

			case ElementType.Array:
				if (!b.IsArray || IsSZArray(b))
					result = false;
				else {
					ArraySig ara = a as ArraySig;
					result = ara.Rank == b.GetArrayRank() &&
						(IsFnPtrElementType(b) ?
								(a = TypeSig.RemoveModifiers(a.Next)) != null && a.ElementType == ElementType.FnPtr :
								Equals(a.Next, b.GetElementType()));
				}
				break;

			case ElementType.ValueType:
			case ElementType.Class:
				result = Equals((a as ClassOrValueTypeSig).TypeDefOrRef, b, treatAsGenericInst);
				break;

			case ElementType.Var:
				result = b.IsGenericParameter &&
						b.GenericParameterPosition == (a as GenericSig).Number &&
						b.DeclaringMethod == null;
				break;

			case ElementType.MVar:
				result = b.IsGenericParameter &&
						b.GenericParameterPosition == (a as GenericSig).Number &&
						b.DeclaringMethod != null;
				break;

			case ElementType.GenericInst:
				if (!(b.IsGenericType && !b.IsGenericTypeDefinition) && !treatAsGenericInst) {
					result = false;
					break;
				}
				var gia = (GenericInstSig)a;
				if (SubstituteGenericParameters) {
					InitializeGenericArguments();
					genericArguments.PushTypeArgs(gia.GenericArguments);
					result = Equals(gia.GenericType, b.GetGenericTypeDefinition());
					genericArguments.PopTypeArgs();
				}
				else
					result = Equals(gia.GenericType, b.GetGenericTypeDefinition());
				result = result && Equals(gia.GenericArguments, b.GetGenericArguments());
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				result = Equals(a.Next, b, treatAsGenericInst);
				break;

			case ElementType.FnPtr:
				// At least in method sigs, this will be mapped to System.IntPtr
				result = IsSystemIntPtr(b);
				break;

			case ElementType.Sentinel:
			case ElementType.ValueArray:
			case ElementType.Module:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = false;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}

		static bool IsSZArray(Type a) {
			if (a == null || !a.IsArray)
				return false;
			var prop = a.GetType().GetProperty("IsSzArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (prop != null)
				return (bool)prop.GetValue(a, new object[0]);
			return a.Name.EndsWith("[]");
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(Type a, ExportedType b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="b">Type #1</param>
		/// <param name="a">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(ExportedType a, Type b) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (b == null)
				return false;	// Must use a ModuleRef to reference the global type, so always fail
			if (!recursionCounter.Increment())
				return false;
			bool result = false;

			if (b.HasElementType)
				goto exit;
			if (UTF8String.ToSystemStringOrEmpty(a.TypeName) != b.Name || !NamespaceEquals(a.TypeNamespace, b))
				goto exit;

			var scope = a.Implementation;
			var dta = scope as ExportedType;
			if (dta != null) {	// nested type
				result = Equals(dta, b.DeclaringType);	// Compare enclosing types
				goto exit;
			}
			if (b.IsNested)
				goto exit;	// b is nested, a isn't

			if (DontCompareTypeScope) {
				result = true;
				goto exit;
			}
			var aFile = scope as FileDef;
			if (aFile != null) {
				result = Equals(aFile, b.Module) &&
						Equals(a.DefinitionAssembly, b.Assembly);
				goto exit;
			}
			var aAsm = scope as AssemblyRef;
			if (aAsm != null) {
				result = Equals(aAsm, b.Assembly);
				goto exit;
			}
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(Type a) {
			return GetHashCode(a, false);
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <param name="treatAsGenericInst"><c>true</c> if we should treat <paramref name="a"/>
		/// as a generic instance type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(Type a, bool treatAsGenericInst) {
			if (a == null)	// Could be global type
				return GetHashCode_TypeDef(a);
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			switch (treatAsGenericInst ? ElementType.GenericInst : GetElementType(a)) {
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
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				hash = GetHashCode_TypeDef(a);
				break;

			case ElementType.FnPtr:
				hash = HASHCODE_MAGIC_ET_FNPTR_AND_I;
				break;

			case ElementType.Sentinel:
				hash = HASHCODE_MAGIC_ET_SENTINEL;
				break;

			case ElementType.Ptr:
				hash = HASHCODE_MAGIC_ET_PTR +
					(IsFnPtrElementType(a) ? HASHCODE_MAGIC_ET_FNPTR_AND_I : GetHashCode(a.GetElementType()));
				break;

			case ElementType.ByRef:
				hash = HASHCODE_MAGIC_ET_BYREF +
					(IsFnPtrElementType(a) ? HASHCODE_MAGIC_ET_FNPTR_AND_I : GetHashCode(a.GetElementType()));
				break;

			case ElementType.SZArray:
				hash = HASHCODE_MAGIC_ET_SZARRAY +
					(IsFnPtrElementType(a) ? HASHCODE_MAGIC_ET_FNPTR_AND_I : GetHashCode(a.GetElementType()));
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
				hash = GetHashCode(a.GetElementType());
				break;

			case ElementType.Array:
				// The type doesn't store sizes and lower bounds, so can't use them to
				// create the hash
				hash = HASHCODE_MAGIC_ET_ARRAY + a.GetArrayRank() +
					(IsFnPtrElementType(a) ? HASHCODE_MAGIC_ET_FNPTR_AND_I : GetHashCode(a.GetElementType()));
				break;

			case ElementType.Var:
				hash = HASHCODE_MAGIC_ET_VAR + a.GenericParameterPosition;
				break;

			case ElementType.MVar:
				hash = HASHCODE_MAGIC_ET_MVAR + a.GenericParameterPosition;
				break;

			case ElementType.GenericInst:
				hash = HASHCODE_MAGIC_ET_GENERICINST + GetHashCode(a.GetGenericTypeDefinition()) + GetHashCode(a.GetGenericArguments());
				break;

			case ElementType.ValueArray:
			case ElementType.Module:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				hash = 0;
				break;
			}

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Gets the hash code of a type list
		/// </summary>
		/// <param name="a">The type list</param>
		/// <returns>The hash code</returns>
		int GetHashCode(IList<Type> a) {
			//************************************************************************
			// IMPORTANT: This code must match any other GetHashCode(IList<SOME_TYPE>)
			//************************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += (uint)GetHashCode(a[i]);
				hash = (hash << 13) | (hash >> 19);
			}
			recursionCounter.Decrement();
			return (int)hash;
		}

		/// <summary>
		/// Gets the hash code of a list with only generic type parameters (<see cref="ElementType.Var"/>)
		/// </summary>
		/// <param name="numGenericParams">Number of generic type parameters</param>
		/// <returns>Hash code</returns>
		static int GetHashCode_ElementType_Var(int numGenericParams) {
			return GetHashCode(numGenericParams, HASHCODE_MAGIC_ET_VAR);
		}

		/// <summary>
		/// Gets the hash code of a list with only generic method parameters (<see cref="ElementType.MVar"/>)
		/// </summary>
		/// <param name="numGenericParams">Number of generic method parameters</param>
		/// <returns>Hash code</returns>
		static int GetHashCode_ElementType_MVar(int numGenericParams) {
			return GetHashCode(numGenericParams, HASHCODE_MAGIC_ET_MVAR);
		}

		static int GetHashCode(int numGenericParams, int etypeHashCode) {
			//************************************************************************
			// IMPORTANT: This code must match any other GetHashCode(IList<SOME_TYPE>)
			//************************************************************************
			uint hash = 0;
			for (int i = 0; i < numGenericParams; i++) {
				hash += (uint)(etypeHashCode + i);
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
		}

		/// <summary>
		/// Gets a <see cref="Type"/>'s <see cref="ElementType"/>
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The type's element type</returns>
		static ElementType GetElementType(Type a) {
			if (a == null)
				return ElementType.End;	// Any invalid one is good enough
			if (a.IsArray)
				return IsSZArray(a) ? ElementType.SZArray : ElementType.Array;
			if (a.IsByRef)
				return ElementType.ByRef;
			if (a.IsPointer)
				return ElementType.Ptr;
			if (a.IsGenericParameter)
				return a.DeclaringMethod == null ? ElementType.Var : ElementType.MVar;
			if (a.IsGenericType && !a.IsGenericTypeDefinition)
				return ElementType.GenericInst;

			if (IsCorLib(a.Assembly) && (a.Namespace ?? string.Empty) == "System") {
				switch (a.Name) {
				case "Void": return ElementType.Void;
				case "Boolean": return ElementType.Boolean;
				case "Char": return ElementType.Char;
				case "SByte": return ElementType.I1;
				case "Byte": return ElementType.U1;
				case "Int16": return ElementType.I2;
				case "UInt16": return ElementType.U2;
				case "Int32": return ElementType.I4;
				case "UInt32": return ElementType.U4;
				case "Int64": return ElementType.I8;
				case "UInt64": return ElementType.U8;
				case "Single": return ElementType.R4;
				case "Double": return ElementType.R8;
				case "String": return ElementType.String;
				case "TypedReference": return ElementType.TypedByRef;
					//TODO: FnPtr is mapped to System.IntPtr too!
				case "IntPtr": return ElementType.I;
				case "UIntPtr": return ElementType.U;
				case "Object": return ElementType.Object;
				}
			}

			return a.IsValueType ? ElementType.ValueType : ElementType.Class;
		}

		/// <summary>
		/// Gets the hash code of a TypeDef type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode_TypeDef(Type a) {
			// ************************************************************************************
			// IMPORTANT: This hash code must match the Type/TypeRef/TypeDef/ExportedType
			// hash code and HASHCODE_MAGIC_ET_FNPTR_AND_I constant
			// ************************************************************************************

			// A global method/field's declaring type is null. This is the reason we must
			// return GetHashCodeGlobalType() here.
			if (a == null)
				return GetHashCodeGlobalType();
			int hash;
			hash = UTF8String.GetHashCode(new UTF8String(a.Name));
			if (a.IsNested)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += UTF8String.GetHashCode(new UTF8String(a.Namespace ?? string.Empty));
			return hash;
		}

		/// <summary>
		/// Compares type lists
		/// </summary>
		/// <param name="a">Type list #1</param>
		/// <param name="b">Type list #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IList<TypeSig> a, IList<Type> b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < a.Count; i++) {
					if (!Equals(a[i], b[i]))
						break;
				}
				result = i == a.Count;
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(ModuleDef a, Module b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals((IModule)a, b) && Equals(a.Assembly, b.Assembly);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares a file and a module
		/// </summary>
		/// <param name="a">File</param>
		/// <param name="b">Module</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(FileDef a, Module b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;

			// Use b.Name since it's the filename we want to compare, not b.ScopeName
			return UTF8String.ToSystemStringOrEmpty(a.Name).Equals(b.Name, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal bool Equals(IModule a, Module b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			// Use b.ScopeName and not b.Name since b.Name is just the file name w/o path
			return UTF8String.ToSystemStringOrEmpty(a.Name).Equals(b.ScopeName, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Compares assemblies
		/// </summary>
		/// <param name="a">Assembly #1</param>
		/// <param name="b">Assembly #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IAssembly a, Assembly b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			var bAsmName = b.GetName();
			bool result = UTF8String.ToSystemStringOrEmpty(a.Name).Equals(bAsmName.Name, StringComparison.OrdinalIgnoreCase) &&
				(!CompareAssemblyPublicKeyToken || PublicKeyBase.TokenEquals(a.PublicKeyOrToken, new PublicKeyToken(bAsmName.GetPublicKeyToken()))) &&
				(!CompareAssemblyVersion || Utils.Equals(a.Version, bAsmName.Version)) &&
				(!CompareAssemblyLocale || Utils.LocaleEquals(a.Locale, bAsmName.CultureInfo.Name));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares method declaring types
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool DeclaringTypeEquals(IMethod a, MethodBase b) {
			// If this is disabled, always return true, even if one is null, etc.
			if (!CompareMethodFieldDeclaringType)
				return true;

			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var md = a as MethodDef;
			if (md != null) {
				result = DeclaringTypeEquals(md, b);
				goto exit;
			}
			var mr = a as MemberRef;
			if (mr != null) {
				result = DeclaringTypeEquals(mr, b);
				goto exit;
			}
			var ms = a as MethodSpec;
			if (ms != null) {
				result = DeclaringTypeEquals(ms, b);
				goto exit;
			}
			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		bool DeclaringTypeEquals(MethodDef a, MethodBase b) {
			// If this is disabled, always return true, even if one is null, etc.
			if (!CompareMethodFieldDeclaringType)
				return true;
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.DeclaringType, b.DeclaringType);
		}

		bool DeclaringTypeEquals(MemberRef a, MethodBase b) {
			// If this is disabled, always return true, even if one is null, etc.
			if (!CompareMethodFieldDeclaringType)
				return true;
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.Class, b.DeclaringType, b.Module);
		}

		bool DeclaringTypeEquals(MethodSpec a, MethodBase b) {
			// If this is disabled, always return true, even if one is null, etc.
			if (!CompareMethodFieldDeclaringType)
				return true;
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return DeclaringTypeEquals(a.Method, b);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBase a, IMethod b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(IMethod a, MethodBase b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var md = a as MethodDef;
			if (md != null) {
				result = Equals(md, b);
				goto exit;
			}
			var mr = a as MemberRef;
			if (mr != null) {
				result = Equals(mr, b);
				goto exit;
			}
			var ms = a as MethodSpec;
			if (ms != null) {
				result = Equals(ms, b);
				goto exit;
			}
			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBase a, MethodDef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodDef a, MethodBase b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			var amSig = a.MethodSig;
			bool result = UTF8String.ToSystemStringOrEmpty(a.Name) == b.Name &&
					((amSig.Generic && b.IsGenericMethodDefinition && b.IsGenericMethod) ||
					(!amSig.Generic && !b.IsGenericMethodDefinition && !b.IsGenericMethod)) &&
					amSig != null && Equals(amSig, b) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares method sigs
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBase a, MethodSig b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares method sigs
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodSig a, MethodBase b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals(a.GetCallingConvention(), b) &&
					(DontCompareReturnType || ReturnTypeEquals(a.RetType, b)) &&
					Equals(a.Params, b.GetParameters(), b.DeclaringType) &&
					(!a.Generic || a.GenParamCount == b.GetGenericArguments().Length);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBase a, MemberRef b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MemberRef a, MethodBase b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (b.IsGenericMethod && !b.IsGenericMethodDefinition) {
				// 'a' must be a method ref in a generic type. This comparison must match
				// the MethodSpec vs MethodBase comparison code.
				result = a.IsMethodRef && a.MethodSig.Generic;

				var oldOptions = ClearOptions(SigComparerOptions.CompareMethodFieldDeclaringType);
				result = result && Equals(a, b.Module.ResolveMethod(b.MetadataToken));
				RestoreOptions(oldOptions);
				result = result && DeclaringTypeEquals(a, b);

				result = result && GenericMethodArgsEquals((int)a.MethodSig.GenParamCount, b.GetGenericArguments());
			}
			else {
				var amSig = a.MethodSig;
				result = UTF8String.ToSystemStringOrEmpty(a.Name) == b.Name &&
						((amSig.Generic && b.IsGenericMethodDefinition && b.IsGenericMethod) ||
						(!amSig.Generic && !b.IsGenericMethodDefinition && !b.IsGenericMethod)) &&
						amSig != null;

				GenericInstSig git;
				if (SubstituteGenericParameters && (git = GetGenericInstanceType(a.Class)) != null) {
					InitializeGenericArguments();
					genericArguments.PushTypeArgs(git.GenericArguments);
					result = result && Equals(amSig, b);
					genericArguments.PopTypeArgs();
				}
				else {
					result = result && Equals(amSig, b);
				}

				result = result && (!CompareMethodFieldDeclaringType || Equals(a.Class, b.DeclaringType, b.Module));
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares generic method args, making sure <paramref name="methodGenArgs"/> only
		/// contains <see cref="ElementType.MVar"/>s.
		/// </summary>
		/// <param name="numMethodArgs">Number of generic method args in method #1</param>
		/// <param name="methodGenArgs">Generic method args in method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool GenericMethodArgsEquals(int numMethodArgs, IList<Type> methodGenArgs) {
			if (numMethodArgs != methodGenArgs.Count)
				return false;
			for (int i = 0; i < numMethodArgs; i++) {
				if (GetElementType(methodGenArgs[i]) != ElementType.MVar)
					return false;
			}
			return true;
		}

		bool Equals(IMemberRefParent a, Type b, Module bModule) {
			// Global methods and fields have their DeclaringType set to null. Assume
			// null always means the global type.
			if (a == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			ITypeDefOrRef ita = a as ITypeDefOrRef;
			if (ita != null) {
				result = Equals((IType)ita, b);
				goto exit;
			}
			ModuleRef moda = a as ModuleRef;
			if (moda != null) {
				ModuleDef omoda = moda.OwnerModule;
				result = b == null &&	// b == null => it's the global type
						Equals(moda, bModule) &&
						Equals(omoda == null ? null : omoda.Assembly, bModule.Assembly);
				goto exit;
			}
			MethodDef ma = a as MethodDef;
			if (ma != null) {
				result = Equals(ma.DeclaringType, b);
				goto exit;
			}
			var td = a as TypeDef;
			if (td != null && b == null) {
				result = td.IsGlobalModuleType;
				goto exit;
			}

			result = false;
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodBase a, MethodSpec b) {
			return Equals(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Equals(MethodSpec a, MethodBase b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			// Make sure it's a MethodSpec
			bool result = b.IsGenericMethod && !b.IsGenericMethodDefinition;

			// Don't compare declaring types yet because the resolved method has the wrong
			// declaring type (its declaring type is a generic type def).
			// NOTE: We must not push generic method args when comparing a.Method
			var oldOptions = ClearOptions(SigComparerOptions.CompareMethodFieldDeclaringType);
			result = result && Equals(a.Method, b.Module.ResolveMethod(b.MetadataToken));
			RestoreOptions(oldOptions);
			result = result && DeclaringTypeEquals(a.Method, b);

			var gim = a.GenericInstMethodSig;
			result = result && gim != null && Equals(gim.GenericArguments, b.GetGenericArguments());

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a <c>MethodBase</c>
		/// </summary>
		/// <param name="a">The <c>MethodBase</c></param>
		/// <returns>The hash code</returns>
		public int GetHashCode(MethodBase a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			// ***********************************************************************
			// IMPORTANT: This hash code must match the MemberRef/MethodSpec hash code
			// ***********************************************************************
			int hash = UTF8String.GetHashCode(new UTF8String(a.Name)) +
					GetHashCode_MethodSig(a);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		int GetHashCode_MethodSig(MethodBase a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a.CallingConvention, a.IsGenericMethod) +
					GetHashCode(a.GetParameters(), a.DeclaringType);
			if (!DontCompareReturnType)
				hash += GetHashCode_ReturnType(a);
			if (a.IsGenericMethod)
				hash += GetHashCode_ElementType_MVar(a.GetGenericArguments().Length);

			recursionCounter.Decrement();
			return hash;
		}

		/// <summary>
		/// Gets the hash code of a parameter list
		/// </summary>
		/// <param name="a">The type list</param>
		/// <param name="declaringType">Declaring type of method that owns parameter <paramref name="a"/></param>
		/// <returns>The hash code</returns>
		int GetHashCode(IList<ParameterInfo> a, Type declaringType) {
			//************************************************************************
			// IMPORTANT: This code must match any other GetHashCode(IList<SOME_TYPE>)
			//************************************************************************
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += (uint)GetHashCode(a[i], declaringType);
				hash = (hash << 13) | (hash >> 19);
			}
			recursionCounter.Decrement();
			return (int)hash;
		}

		int GetHashCode_ReturnType(MethodBase a) {
			var mi = a as MethodInfo;
			if (mi != null)
				return GetHashCode(mi.ReturnParameter, a.DeclaringType);
			return GetHashCode(typeof(void));
		}

		int GetHashCode(ParameterInfo a, Type declaringType) {
			return GetHashCode(a.ParameterType, MustTreatParamTypeAsGenericInstType(a, declaringType));
		}

		/// <summary>
		/// Checks whether a parameter type should be treated as if it is really a generic instance
		/// type and not a generic type definition. In the .NET metadata (method sig), the parameter
		/// is a generic instance type, but the CLR treats it as if it's just a generic type def.
		/// This seems to happen only if the parameter type is exactly the same type as the
		/// declaring type, eg. a method similar to: <c>MyType&lt;!0&gt; MyType::SomeMethod()</c>.
		/// </summary>
		/// <param name="p">Parameter</param>
		/// <param name="declaringType">Declaring type of method which owns <paramref name="p"/></param>
		static bool MustTreatParamTypeAsGenericInstType(ParameterInfo p, Type declaringType) {
			return declaringType != null &&
				declaringType.IsGenericTypeDefinition &&
				p.ParameterType == declaringType;
		}

		/// <summary>
		/// Compares calling conventions
		/// </summary>
		/// <param name="a">Calling convention</param>
		/// <param name="b">Method</param>
		/// <returns></returns>
		static bool Equals(CallingConvention a, MethodBase b) {
			var bc = b.CallingConvention;

			if (((a & CallingConvention.Generic) != 0) != b.IsGenericMethod)
				return false;
			if (((a & CallingConvention.HasThis) != 0) != ((bc & CallingConventions.HasThis) != 0))
				return false;
			if (((a & CallingConvention.ExplicitThis) != 0) != ((bc & CallingConventions.ExplicitThis) != 0))
				return false;

			var cca = a & CallingConvention.Mask;
			switch (bc & CallingConventions.Any) {
			case CallingConventions.Standard:
				if (cca == CallingConvention.VarArg || cca == CallingConvention.NativeVarArg)
					return false;
				break;

			case CallingConventions.VarArgs:
				if (cca != CallingConvention.VarArg && cca != CallingConvention.NativeVarArg)
					return false;
				break;

			case CallingConventions.Any:
			default:
				break;
			}

			return true;
		}

		static int GetHashCode_CallingConvention(CallingConventions a, bool isGeneric) {
			//**************************************************************
			// IMPORTANT: This hash must match the other call conv hash code
			//**************************************************************

			CallingConvention cc = 0;

			if (isGeneric)
				cc |= CallingConvention.Generic;
			if ((a & CallingConventions.HasThis) != 0)
				cc |= CallingConvention.HasThis;
			if ((a & CallingConventions.ExplicitThis) != 0)
				cc |= CallingConvention.ExplicitThis;

			return (int)cc;
		}

		/// <summary>
		/// Compares return types
		/// </summary>
		/// <param name="a">Return type #1</param>
		/// <param name="b">MethodBase</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool ReturnTypeEquals(TypeSig a, MethodBase b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			var mi = b as MethodInfo;
			if (mi != null)
				result = Equals(a, mi.ReturnParameter, b.DeclaringType);
			else if (b is ConstructorInfo)
				result = IsSystemVoid(a);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		static bool IsSystemVoid(TypeSig a) {
			return a != null && a.FullName == "System.Void" && a.DefinitionAssembly.IsCorLib();
		}

		/// <summary>
		/// Compares parameter lists
		/// </summary>
		/// <param name="a">Type list #1</param>
		/// <param name="b">Type list #2</param>
		/// <param name="declaringType">Declaring type of method that owns parameter <paramref name="b"/></param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IList<TypeSig> a, IList<ParameterInfo> b, Type declaringType) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < a.Count; i++) {
					if (!Equals(a[i], b[i], declaringType))
						break;
				}
				result = i == a.Count;
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Compares parameter types
		/// </summary>
		/// <param name="a">Parameter type #1</param>
		/// <param name="b">Parameter #2</param>
		/// <param name="declaringType">Declaring type of method that owns parameter <paramref name="b"/></param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(TypeSig a, ParameterInfo b, Type declaringType) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			TypeSig a2;
			bool result = ModifiersEquals(a, b, out a2) &&
						Equals(a2, b.ParameterType, MustTreatParamTypeAsGenericInstType(b, declaringType));

			recursionCounter.Decrement();
			return result;
		}

		bool ModifiersEquals(TypeSig a, ParameterInfo b, out TypeSig aAfterModifiers) {
			aAfterModifiers = a;
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var reqMods2 = b.GetRequiredCustomModifiers();
			var optMods2 = b.GetOptionalCustomModifiers();
			// Exit quickly if this is the common case
			if (!(a is ModifierSig)) {
				result = reqMods2.Length == 0 && optMods2.Length == 0;
				goto exit;
			}

			var reqMods1 = new List<ITypeDefOrRef>(reqMods2.Length);
			var optMods1 = new List<ITypeDefOrRef>(optMods2.Length);
			while (true) {
				var modifierSig = aAfterModifiers as ModifierSig;
				if (modifierSig == null)
					break;
				if (modifierSig is CModOptSig)
					optMods1.Add(modifierSig.Modifier);
				else
					reqMods1.Add(modifierSig.Modifier);

				// This can only loop forever if the user created a loop. It's not possible
				// to create a loop with invalid metadata.
				aAfterModifiers = aAfterModifiers.Next;
			}

			result = reqMods1.Count == reqMods2.Length &&
					optMods1.Count == optMods2.Length &&
					ModifiersEquals(reqMods1, reqMods2) &&
					ModifiersEquals(optMods1, optMods2);

exit:
			recursionCounter.Decrement();
			return result;
		}

		bool ModifiersEquals(IList<ITypeDefOrRef> a, IList<Type> b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < b.Count; i++) {
					int index = IndexOf(a, b[i]);
					if (index < 0)
						break;
					a.RemoveAt(index);
				}
				result = i == b.Count;
			}

			recursionCounter.Decrement();
			return result;
		}

		int IndexOf(IList<ITypeDefOrRef> list, Type t) {
			for (int i = 0; i < list.Count; i++) {
				if (Equals(list[i], t))
					return i;
			}
			return -1;
		}

		//TODO: Compare fields
		//TODO: Compare properties
		//TODO: Compare events
	}
}
