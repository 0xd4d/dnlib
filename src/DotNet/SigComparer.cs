using System;
using System.Collections.Generic;
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
	public enum SigComparerOptions {
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
		/// Compares parameters after a sentinel in method sigs
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
	}

	/// <summary>
	/// Compares types, signatures, methods, fields, properties, events
	/// </summary>
	public struct SigComparer {
		const int HASHCODE_MAGIC_GLOBAL_TYPE = 1654396648;
		const int HASHCODE_MAGIC_NESTED_TYPE = -1049070942;
		const int HASHCODE_MAGIC_ET_MODULE = -299744851;
		const int HASHCODE_MAGIC_ET_VALUEARRAY = -674970533;
		const int HASHCODE_MAGIC_ET_FNPTR = -1333778933;
		const int HASHCODE_MAGIC_ET_GENERICINST = -2050514639;
		const int HASHCODE_MAGIC_ET_VAR = 1288450097;
		const int HASHCODE_MAGIC_ET_MVAR = -990598495;
		const int HASHCODE_MAGIC_ET_ARRAY = -96331531;
		const int HASHCODE_MAGIC_ET_SZARRAY = 871833535;
		const int HASHCODE_MAGIC_ET_BYREF = -634749586;
		const int HASHCODE_MAGIC_ET_PTR = 1976400808;
		const int HASHCODE_MAGIC_ET_SENTINEL = 68439620;

		RecursionCounter recursionCounter;
		SigComparerOptions options;

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
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public SigComparer(SigComparerOptions options) {
			this.recursionCounter = new RecursionCounter();
			this.options = options;
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

			if (UTF8String.CompareTo(a.Name, b.Name) != 0 || UTF8String.CompareTo(a.Namespace, b.Namespace) != 0)
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
			if (result && a.IsGlobalModuleType)
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

			if (UTF8String.CompareTo(a.Name, b.TypeName) != 0 || UTF8String.CompareTo(a.Namespace, b.TypeNamespace) != 0)
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
			if (result && a.IsGlobalModuleType)
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

			//************************************************************
			// If this code gets updated, update GetHashCode(TypeSig),
			// Equals(TypeRef,TypeSig) and EqualsTypeSig,ExportedType) too
			//************************************************************
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

			bool result = UTF8String.CompareTo(a.Name, b.TypeName) == 0 &&
					UTF8String.CompareTo(a.Namespace, b.TypeNamespace) == 0 &&
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
			// Equals(TypeDef,TypeSig) and Equals(TypeSig,ExportedType) too
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

			//********************************************************
			// If this code gets updated, update GetHashCode(TypeSig),
			// Equals(TypeDef,TypeSig) and Equals(TypeRef,TypeSig) too
			//********************************************************
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					UTF8String.CompareTo(a.Namespace, b.Namespace) == 0 &&
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
			// ***********************************************************************
			// IMPORTANT: This hash code must match the TypeDef/ExportedType hash code
			// ***********************************************************************
			if (a == null)
				return 0;
			int hash;
			hash = UTF8String.GetHashCode(a.Name) +
				UTF8String.GetHashCode(a.Namespace);
			if (a.ResolutionScope is TypeRef)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
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

			bool result = UTF8String.CompareTo(a.TypeName, b.TypeName) == 0 &&
					UTF8String.CompareTo(a.TypeNamespace, b.TypeNamespace) == 0 &&
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
			// ******************************************************************
			// IMPORTANT: This hash code must match the TypeDef/TypeRef hash code
			// ******************************************************************
			if (a == null)
				return 0;
			int hash;
			hash = UTF8String.GetHashCode(a.TypeName) +
				UTF8String.GetHashCode(a.TypeNamespace);
			if (a.Implementation is ExportedType)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					UTF8String.CompareTo(a.Namespace, b.Namespace) == 0 &&
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
			// ***********************************************************************
			// IMPORTANT: This hash code must match the TypeRef/ExportedType hash code
			// ***********************************************************************
			if (a == null)
				return 0;
			if (a.IsGlobalModuleType)
				return GetHashCodeGlobalType();
			int hash;
			hash = UTF8String.GetHashCode(a.Name) +
				UTF8String.GetHashCode(a.Namespace);
			if (a.DeclaringType != null)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
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

			return UTF8String.CaseInsensitiveCompareTo(a.Name, b.Name) == 0;
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

			return UTF8String.CaseInsensitiveCompareTo(a.Name, b.Name) == 0;
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		bool Equals(IModule a, IModule b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			return UTF8String.CaseInsensitiveCompareTo(a.Name, b.Name) == 0;
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

			bool result = UTF8String.CaseInsensitiveCompareTo(a.Name, b.Name) == 0 &&
				(!CompareAssemblyPublicKeyToken || PublicKeyBase.TokenCompareTo(a.PublicKeyOrToken, b.PublicKeyOrToken) == 0) &&
				(!CompareAssemblyVersion || Utils.CompareTo(a.Version, b.Version) == 0) &&
				(!CompareAssemblyLocale || Utils.LocaleCompareTo(a.Locale, b.Locale) == 0);

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
				ArraySig ara = (ArraySig)a;
				hash = HASHCODE_MAGIC_ET_ARRAY + (int)ara.Rank + GetHashCode(ara.Sizes) +
						GetHashCode(ara.LowerBounds) + GetHashCode(ara.Next);
				break;

			case ElementType.Var:
				hash = HASHCODE_MAGIC_ET_VAR + (int)(a as GenericVar).Number;
				break;

			case ElementType.MVar:
				hash = HASHCODE_MAGIC_ET_MVAR + (int)(a as GenericMVar).Number;
				break;

			case ElementType.GenericInst:
				var gia = (GenericInstSig)a;
				hash = HASHCODE_MAGIC_ET_GENERICINST + GetHashCode(gia.GenericType) + GetHashCode(gia.GenericArguments);
				break;

			case ElementType.FnPtr:
				hash = HASHCODE_MAGIC_ET_FNPTR + GetHashCode((a as FnPtrSig).Signature);
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

		int GetHashCode(IList<uint> a) {
			if (a == null)
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += a[i];
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
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

		int GetHashCode(IList<int> a) {
			if (a == null)
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += (uint)a[i];
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
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
				hash = (int)a.GetCallingConvention();
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
					Equals(a.RetType, b.RetType) &&
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

			hash = (int)a.GetCallingConvention() +
					GetHashCode(a.RetType) +
					GetHashCode(a.Params);
			if (a.Generic)
				hash += (int)a.GenParamCount;
			if (CompareSentinelParams)
				hash += GetHashCode(a.ParamsAfterSentinel);

			recursionCounter.Decrement();
			return hash;
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

			hash = (int)a.GetCallingConvention() + GetHashCode(a.Type);

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

			hash = (int)a.GetCallingConvention() + GetHashCode(a.Locals);

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

			hash = (int)a.GetCallingConvention() + GetHashCode(a.GenericArguments);

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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			int hash = UTF8String.GetHashCode(a.Name) +
					GetHashCode(a.Signature);
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
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = GetHashCode(a.Method) + GetHashCode(a.Instantiation);

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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
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
	}
}
