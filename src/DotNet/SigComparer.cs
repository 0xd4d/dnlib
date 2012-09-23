using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Compares types
	/// </summary>
	public class TypeEqualityComparer : IEqualityComparer<IType>, IEqualityComparer<TypeRef>, IEqualityComparer<TypeDef>, IEqualityComparer<TypeSpec>, IEqualityComparer<TypeSig> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly TypeEqualityComparer Instance = new TypeEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public TypeEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IType x, IType y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IType obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeRef x, TypeRef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeRef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeDef x, TypeDef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeDef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeSpec x, TypeSpec y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeSpec obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(TypeSig x, TypeSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(TypeSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares fields
	/// </summary>
	public class FieldEqualityComparer : IEqualityComparer<IField>, IEqualityComparer<FieldDef>, IEqualityComparer<MemberRef> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly FieldEqualityComparer CompareDeclaringTypes = new FieldEqualityComparer(SigComparer.Options.CompareMethodFieldDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly FieldEqualityComparer DontCompareDeclaringTypes = new FieldEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public FieldEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IField x, IField y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IField obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(FieldDef x, FieldDef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(FieldDef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MemberRef x, MemberRef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MemberRef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares methods
	/// </summary>
	public class MethodEqualityComparer : IEqualityComparer<IMethod>, IEqualityComparer<MethodDef>, IEqualityComparer<MemberRef>, IEqualityComparer<MethodSpec> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly MethodEqualityComparer CompareDeclaringTypes = new MethodEqualityComparer(SigComparer.Options.CompareMethodFieldDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly MethodEqualityComparer DontCompareDeclaringTypes = new MethodEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public MethodEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(IMethod x, IMethod y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(IMethod obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodDef x, MethodDef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodDef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MemberRef x, MemberRef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MemberRef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodSpec x, MethodSpec y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodSpec obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares properties
	/// </summary>
	public class PropertyEqualityComparer : IEqualityComparer<PropertyDef> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly PropertyEqualityComparer CompareDeclaringTypes = new PropertyEqualityComparer(SigComparer.Options.ComparePropertyDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly PropertyEqualityComparer DontCompareDeclaringTypes = new PropertyEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public PropertyEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(PropertyDef x, PropertyDef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(PropertyDef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares events
	/// </summary>
	public class EventEqualityComparer : IEqualityComparer<EventDef> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Compares the declaring types
		/// </summary>
		public static readonly EventEqualityComparer CompareDeclaringTypes = new EventEqualityComparer(SigComparer.Options.CompareEventDeclaringType);

		/// <summary>
		/// Doesn't compare the declaring types
		/// </summary>
		public static readonly EventEqualityComparer DontCompareDeclaringTypes = new EventEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public EventEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(EventDef x, EventDef y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(EventDef obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
	}

	/// <summary>
	/// Compares calling convention signatures
	/// </summary>
	public class SignatureEqualityComparer : IEqualityComparer<CallingConventionSig>, IEqualityComparer<MethodBaseSig>, IEqualityComparer<MethodSig>, IEqualityComparer<PropertySig>, IEqualityComparer<FieldSig>, IEqualityComparer<LocalSig>, IEqualityComparer<GenericInstMethodSig> {
		readonly SigComparer.Options options;

		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly SignatureEqualityComparer Instance = new SignatureEqualityComparer(0);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public SignatureEqualityComparer(SigComparer.Options options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public bool Equals(CallingConventionSig x, CallingConventionSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(CallingConventionSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodBaseSig x, MethodBaseSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodBaseSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(MethodSig x, MethodSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(MethodSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(PropertySig x, PropertySig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(PropertySig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(FieldSig x, FieldSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(FieldSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(LocalSig x, LocalSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(LocalSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public bool Equals(GenericInstMethodSig x, GenericInstMethodSig y) {
			return new SigComparer { Flags = options }.Compare(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(GenericInstMethodSig obj) {
			return new SigComparer { Flags = options }.GetHashCode(obj);
		}
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
		Options options;

		/// <summary>
		/// Decides how to compare types, sigs, etc
		/// </summary>
		[Flags]
		public enum Options {
			/// <summary>
			/// Compares a method/field's declaring type.
			/// </summary>
			CompareMethodFieldDeclaringType = 1,

			/// <summary>
			/// Compares a property's declaring type
			/// </summary>
			ComparePropertyDeclaringType = 2,

			/// <summary>
			/// Compares an event's declaring type
			/// </summary>
			CompareEventDeclaringType = 4,

			/// <summary>
			/// Compares parameters after a sentinel in method sigs
			/// </summary>
			CompareSentinelParams = 8,
		}

		/// <summary>
		/// Gets/sets the options
		/// </summary>
		public Options Flags {
			get { return options; }
			set { options = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="Options.CompareMethodFieldDeclaringType"/> bit
		/// </summary>
		public bool CompareMethodFieldDeclaringType {
			get { return (options & Options.CompareMethodFieldDeclaringType) != 0; }
			set {
				if (value)
					options |= Options.CompareMethodFieldDeclaringType;
				else
					options &= ~Options.CompareMethodFieldDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="Options.ComparePropertyDeclaringType"/> bit
		/// </summary>
		public bool ComparePropertyDeclaringType {
			get { return (options & Options.ComparePropertyDeclaringType) != 0; }
			set {
				if (value)
					options |= Options.ComparePropertyDeclaringType;
				else
					options &= ~Options.ComparePropertyDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="Options.CompareEventDeclaringType"/> bit
		/// </summary>
		public bool CompareEventDeclaringType {
			get { return (options & Options.CompareEventDeclaringType) != 0; }
			set {
				if (value)
					options |= Options.CompareEventDeclaringType;
				else
					options &= ~Options.CompareEventDeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="Options.CompareSentinelParams"/> bit
		/// </summary>
		public bool CompareSentinelParams {
			get { return (options & Options.CompareSentinelParams) != 0; }
			set {
				if (value)
					options |= Options.CompareSentinelParams;
				else
					options &= ~Options.CompareSentinelParams;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Comparison options</param>
		public SigComparer(Options options) {
			this.recursionCounter = new RecursionCounter();
			this.options = options;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IType a, IType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			TypeDef tda = a as TypeDef, tdb = b as TypeDef;
			if (tda != null && tdb != null) {
				result = Compare(tda, tdb);
				goto exit;
			}
			TypeRef tra = a as TypeRef, trb = b as TypeRef;
			if (tra != null && trb != null) {
				result = Compare(tra, trb);
				goto exit;
			}
			TypeSpec tsa = a as TypeSpec, tsb = b as TypeSpec;
			if (tsa != null && tsb != null) {
				result = Compare(tsa, tsb);
				goto exit;
			}
			TypeSig sa = a as TypeSig, sb = b as TypeSig;
			if (sa != null && sb != null) {
				result = Compare(sa, sb);
				goto exit;
			}

			if (tda != null && trb != null)
				result = Compare(tda, trb);
			else if (tra != null && tdb != null)
				result = Compare(tdb, tra);
			else if (tda != null && tsb != null)
				result = Compare(tda, tsb);
			else if (tsa != null && tdb != null)
				result = Compare(tdb, tsa);
			else if (tda != null && sb != null)
				result = Compare(tda, sb);
			else if (sa != null && tdb != null)
				result = Compare(tdb, sa);
			else if (tra != null && tsb != null)
				result = Compare(tra, tsb);
			else if (tsa != null && trb != null)
				result = Compare(trb, tsa);
			else if (tra != null && sb != null)
				result = Compare(tra, sb);
			else if (sa != null && trb != null)
				result = Compare(trb, sa);
			else if (tsa != null && sb != null)
				result = Compare(tsa, sb);
			else if (sa != null && tsb != null)
				result = Compare(tsb, sa);
			else
				result = false;	// Should never be reached

exit:
			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
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
			hash = 0;	// Should never be reached
exit:
			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeRef a, TypeDef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeDef a, TypeRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result = false;

			if (UTF8String.CompareTo(a.Name, b.Name) != 0 || UTF8String.CompareTo(a.Namespace, b.Namespace) != 0)
				goto exit;

			var scope = b.ResolutionScope;
			var dtb = scope as TypeRef;
			if (dtb != null) {	// nested type
				result = Compare(a.DeclaringType, dtb);	// Compare enclosing types
				goto exit;
			}
			if (a.DeclaringType != null)
				goto exit;	// a is nested, b isn't

			var bMod = scope as IModule;
			if (bMod != null) {	// 'b' is defined in the same assembly as 'a'
				result = Compare((IModule)a.OwnerModule, (IModule)bMod);
				goto exit;
			}
			var bAsm = scope as AssemblyRef;
			if (bAsm != null) {
				var aMod = a.OwnerModule;
				result = aMod != null &&
						aMod.Assembly != null &&
						aMod.Assembly.ManifestModule == aMod &&
						Compare(aMod.Assembly, bAsm);
				goto exit;
			}
			//TODO: Handle the case where scope == null
exit:
			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSpec a, TypeDef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeDef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Compare(a, b.TypeSig);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSig a, TypeDef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeDef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			//****************************************************************************************
			// If this code gets updated, update GetHashCode(TypeSig) and Compare(TypeRef,TypeSig) too
			//****************************************************************************************
			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Compare(a, b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Compare(a, b.Next);
			else
				result = false;

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSpec a, TypeRef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeRef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Compare(a, b.TypeSig);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSig a, TypeRef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeRef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			//****************************************************************************************
			// If this code gets updated, update GetHashCode(TypeSig) and Compare(TypeDef,TypeSig) too
			//****************************************************************************************
			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Compare(a, b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Compare(a, b.Next);
			else
				result = false;

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSig a, TypeSpec b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSpec a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Compare(a.TypeSig, b);
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeRef a, TypeRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					UTF8String.CompareTo(a.Namespace, b.Namespace) == 0 &&
					CompareResolutionScope(a, b);

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		int GetHashCodeGlobalType() {
			// We don't always know the name+namespace of the global type, eg. when it's
			// referenced by a ModuleRef. Use the same hash for all global types.
			return HASHCODE_MAGIC_GLOBAL_TYPE;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeRef a) {
			// **********************************************************
			// IMPORTANT: This hash code must match the TypeDef hash code
			// **********************************************************
			if (a == null)
				return 0;
			if (IsGlobalModuleType(a))
				return GetHashCodeGlobalType();
			int hash;
			hash = UTF8String.GetHashCode(a.Name) ^
				UTF8String.GetHashCode(a.Namespace);
			if (a.ResolutionScope is TypeRef)
				hash ^= HASHCODE_MAGIC_NESTED_TYPE;
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeDef a, TypeDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					UTF8String.CompareTo(a.Namespace, b.Namespace) == 0 &&
					Compare(a.DeclaringType, b.DeclaringType) &&
					a.IsGlobalModuleType == b.IsGlobalModuleType &&
					Compare(a.OwnerModule, b.OwnerModule);

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(TypeDef a) {
			// **********************************************************
			// IMPORTANT: This hash code must match the TypeRef hash code
			// **********************************************************
			if (a == null)
				return 0;
			if (a.IsGlobalModuleType)
				return GetHashCodeGlobalType();
			int hash;
			hash = UTF8String.GetHashCode(a.Name) ^
				UTF8String.GetHashCode(a.Namespace);
			if (a.DeclaringType != null)
				hash ^= HASHCODE_MAGIC_NESTED_TYPE;
			return hash;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSpec a, TypeSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = Compare(a.TypeSig, b.TypeSig);

			recursionCounter.DecrementRecursionCounter();
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
		public bool CompareResolutionScope(TypeRef a, TypeRef b) {
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
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			TypeRef ea = ra as TypeRef, eb = rb as TypeRef;
			if (ea != null || eb != null) {	// if one of them is a TypeRef, the other one must be too
				result = Compare(ea, eb);
				goto exit;
			}
			IModule ma = ra as IModule, mb = rb as IModule;
			if (ma != null && mb != null) {	// only compare if both are modules
				result = Compare(ma, mb);
				goto exit;
			}
			AssemblyRef aa = ra as AssemblyRef, ab = rb as AssemblyRef;
			if (aa != null && ab != null) {	// only compare if both are assemblies
				result = Compare((IAssembly)aa, (IAssembly)ab);
				goto exit;
			}
			ModuleRef modRef = rb as ModuleRef;
			if (aa != null && modRef != null) {
				var bMod = b.OwnerModule;
				result = bMod != null && Compare(aa, bMod.Assembly);
				goto exit;
			}
			modRef = ra as ModuleRef;
			if (ab != null && modRef != null) {
				var aMod = a.OwnerModule;
				result = aMod != null && Compare(ab, aMod.Assembly);
				goto exit;
			}
			ModuleDef modDef = rb as ModuleDef;
			if (aa != null && modDef != null) {
				result = Compare(aa, modDef.Assembly);
				goto exit;
			}
			modDef = ra as ModuleDef;
			if (ab != null && modDef != null) {
				result = Compare(ab, modDef.Assembly);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IModule a, IModule b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: Case insensitive or case sensitive comparison???
			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0;

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares modules
		/// </summary>
		/// <param name="a">Module #1</param>
		/// <param name="b">Module #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(ModuleDef a, ModuleDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = Compare((IModule)a, (IModule)b) && Compare(a.Assembly, b.Assembly);

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares assemblies
		/// </summary>
		/// <param name="a">Assembly #1</param>
		/// <param name="b">Assembly #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IAssembly a, IAssembly b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: Case insensitive or case sensitive comparison???
			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0;

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares types
		/// </summary>
		/// <param name="a">Type #1</param>
		/// <param name="b">Type #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(TypeSig a, TypeSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
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
					result = Compare(a.Next, b.Next);
					break;

				case ElementType.Array:
					ArraySig ara = a as ArraySig, arb = b as ArraySig;
					result = ara.Rank == arb.Rank &&
							Compare(ara.Sizes, arb.Sizes) &&
							Compare(ara.LowerBounds, arb.LowerBounds) &&
							Compare(a.Next, b.Next);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					result = Compare((a as ClassOrValueTypeSig).TypeDefOrRef, (b as ClassOrValueTypeSig).TypeDefOrRef);
					break;

				case ElementType.Var:
				case ElementType.MVar:
					result = (a as GenericSig).Number == (b as GenericSig).Number;
					break;

				case ElementType.GenericInst:
					var gia = (GenericInstSig)a;
					var gib = (GenericInstSig)b;
					result = Compare(gia.GenericType, gib.GenericType) &&
							Compare(gia.GenericArguments, gib.GenericArguments);
					break;

				case ElementType.FnPtr:
					result = Compare((a as FnPtrSig).Signature, (b as FnPtrSig).Signature);
					break;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
					result = Compare((a as ModifierSig).Modifier, (b as ModifierSig).Modifier) && Compare(a.Next, b.Next);
					break;

				case ElementType.ValueArray:
					result = (a as ValueArraySig).Size == (b as ValueArraySig).Size && Compare(a.Next, b.Next);
					break;

				case ElementType.Module:
					result = (a as ModuleSig).Index == (b as ModuleSig).Index && Compare(a.Next, b.Next);
					break;

				case ElementType.End:
				case ElementType.R:
				case ElementType.Internal:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
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
				// When comparing a TypeDef/TypeRef to a TypeDefOrRefSig/Class/ValueType, the
				// ET is ignored, so we must ignore it when calculating the hash.
				hash = GetHashCode((a as TypeDefOrRefSig).TypeDefOrRef);
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
				// When comparing a TypeDef/TypeRef to a ModifierSig/PinnedSig, the ET is
				// ignored, so we must ignore it when calculating the hash.
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

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares type lists
		/// </summary>
		/// <param name="a">Type list #1</param>
		/// <param name="b">Type list #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IList<TypeSig> a, IList<TypeSig> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < a.Count; i++) {
					if (!Compare(a[i], b[i]))
						break;
				}
				result = i == a.Count;
			}

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash ^= (uint)GetHashCode(a[i]);
				hash = (hash << 13) | (hash >> 19);
			}
			recursionCounter.DecrementRecursionCounter();
			return (int)hash;
		}

		private bool Compare(IList<uint> a, IList<uint> b) {
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

		private int GetHashCode(IList<uint> a) {
			if (a == null)
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash ^= a[i];
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
		}

		private bool Compare(IList<int> a, IList<int> b) {
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

		private int GetHashCode(IList<int> a) {
			if (a == null)
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash ^= (uint)a[i];
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
		public bool Compare(CallingConventionSig a, CallingConventionSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
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
					result = ma != null && mb != null && Compare(ma, mb);
					break;

				case CallingConvention.Field:
					FieldSig fa = a as FieldSig, fb = b as FieldSig;
					result = fa != null && fb != null && Compare(fa, fb);
					break;

				case CallingConvention.LocalSig:
					LocalSig la = a as LocalSig, lb = b as LocalSig;
					result = la != null && lb != null && Compare(la, lb);
					break;

				case CallingConvention.GenericInst:
					GenericInstMethodSig ga = a as GenericInstMethodSig, gb = b as GenericInstMethodSig;
					result = ga != null && gb != null && Compare(ga, gb);
					break;

				case CallingConvention.Unmanaged:
				case CallingConvention.NativeVarArg:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
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
		public bool Compare(MethodBaseSig a, MethodBaseSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() &&
					Compare(a.RetType, b.RetType) &&
					Compare(a.Params, b.Params) &&
					(!a.Generic || a.GenParamCount == b.GenParamCount) &&
					(!CompareSentinelParams || Compare(a.ParamsAfterSentinel, b.ParamsAfterSentinel));

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			int hash;

			hash = (int)a.GetCallingConvention() ^
					GetHashCode(a.RetType) ^
					GetHashCode(a.Params);
			if (a.Generic)
				hash ^= (int)a.GenParamCount;
			if (CompareSentinelParams)
				hash ^= GetHashCode(a.ParamsAfterSentinel);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares field sigs
		/// </summary>
		/// <param name="a">Field sig #1</param>
		/// <param name="b">Field sig #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(FieldSig a, FieldSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Compare(a.Type, b.Type);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			int hash;

			hash = (int)a.GetCallingConvention() ^ GetHashCode(a.Type);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares local sigs
		/// </summary>
		/// <param name="a">Local sig #1</param>
		/// <param name="b">Local sig #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(LocalSig a, LocalSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Compare(a.Locals, b.Locals);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			int hash;

			hash = (int)a.GetCallingConvention() ^ GetHashCode(a.Locals);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares generic method instance sigs
		/// </summary>
		/// <param name="a">Generic inst method #1</param>
		/// <param name="b">Generic inst method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(GenericInstMethodSig a, GenericInstMethodSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Compare(a.GenericArguments, b.GenericArguments);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			int hash;

			hash = (int)a.GetCallingConvention() ^ GetHashCode(a.GenericArguments);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IMethod a, IMethod b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			MethodDef mda = a as MethodDef, mdb = b as MethodDef;
			if (mda != null && mdb != null) {
				result = Compare(mda, mdb);
				goto exit;
			}
			MemberRef mra = a as MemberRef, mrb = b as MemberRef;
			if (mra != null && mrb != null) {
				result = Compare(mra, mrb);
				goto exit;
			}
			MethodSpec msa = a as MethodSpec, msb = b as MethodSpec;
			if (msa != null && msb != null) {
				result = Compare(msa, msb);
				goto exit;
			}
			if (mda != null && mrb != null) {
				result = Compare(mda, mrb);
				goto exit;
			}
			if (mra != null && mdb != null) {
				result = Compare(mdb, mra);
				goto exit;
			}
			result = false;
exit:
			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
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
			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MemberRef a, MethodDef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MethodDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: If a.IsPrivateScope, then you should probably always return false since Method
			//		tokens must be used to call the method.

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Compare(a.DeclaringType, b.Class));

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares methods
		/// </summary>
		/// <param name="a">Method #1</param>
		/// <param name="b">Method #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MethodDef a, MethodDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Compare(a.DeclaringType, b.DeclaringType));

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) ^
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash ^= GetHashCode(a.DeclaringType);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares <c>MemberRef</c>s
		/// </summary>
		/// <param name="a"><c>MemberRef</c> #1</param>
		/// <param name="b"><c>MemberRef</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MemberRef a, MemberRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Compare(a.Class, b.Class));

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) ^
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash ^= GetHashCode(a.Class);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares <c>MethodSpec</c>s
		/// </summary>
		/// <param name="a"><c>MethodSpec</c> #1</param>
		/// <param name="b"><c>MethodSpec</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MethodSpec a, MethodSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = Compare(a.Method, b.Method) && Compare(a.Instantiation, b.Instantiation);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			int hash = GetHashCode(a.Method) ^ GetHashCode(a.Instantiation);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares <c>MemberRefParent</c>s
		/// </summary>
		/// <param name="a"><c>MemberRefParent</c> #1</param>
		/// <param name="b"><c>MemberRefParent</c> #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IMemberRefParent a, IMemberRefParent b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			ITypeDefOrRef ita = a as ITypeDefOrRef, itb = b as ITypeDefOrRef;
			if (ita != null && itb != null) {
				result = Compare(ita, itb);
				goto exit;
			}
			ModuleRef moda = a as ModuleRef, modb = b as ModuleRef;
			if (moda != null && modb != null) {
				result = Compare((IModule)moda, (IModule)modb);
				goto exit;
			}
			MethodDef ma = a as MethodDef, mb = b as MethodDef;
			if (ma != null && mb != null) {
				result = Compare(ma, mb);
				goto exit;
			}
			var td = a as TypeDef;
			if (td != null && modb != null) {
				result = CompareGlobal(td, modb);
				goto exit;
			}
			td = b as TypeDef;
			if (td != null && moda != null) {
				result = CompareGlobal(td, moda);
				goto exit;
			}
			var tr = a as TypeRef;
			if (tr != null && modb != null) {
				result = CompareGlobal(tr, modb);
				goto exit;
			}
			tr = b as TypeRef;
			if (tr != null && moda != null) {
				result = CompareGlobal(tr, moda);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Gets the hash code of a <c>MemberRefParent</c>
		/// </summary>
		/// <param name="a">The <c>MemberRefParent</c></param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IMemberRefParent a) {
			if (a == null)
				return 0;
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;
			int hash;

			ITypeDefOrRef ita = a as ITypeDefOrRef;
			if (ita != null) {
				hash = GetHashCode(ita);
				goto exit;
			}
			ModuleRef moda = a as ModuleRef;
			if (moda != null) {
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
			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(IField a, IField b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result;

			FieldDef fa = a as FieldDef, fb = b as FieldDef;
			if (fa != null && fb != null) {
				result = Compare(fa, fb);
				goto exit;
			}
			MemberRef ma = a as MemberRef, mb = b as MemberRef;
			if (ma != null && mb != null) {
				result = Compare(ma, mb);
				goto exit;
			}
			if (fa != null && mb != null) {
				result = Compare(fa, mb);
				goto exit;
			}
			if (fb != null && ma != null) {
				result = Compare(fb, ma);
				goto exit;
			}

			result = false;
exit:
			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
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
			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(MemberRef a, FieldDef b) {
			return Compare(b, a);
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(FieldDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: If a.IsPrivateScope, then you should probably always return false since Field
			//		tokens must be used to access the field

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Compare(a.DeclaringType, b.Class));

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares fields
		/// </summary>
		/// <param name="a">Field #1</param>
		/// <param name="b">Field #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(FieldDef a, FieldDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Compare(a.DeclaringType, b.DeclaringType));

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			int hash = UTF8String.GetHashCode(a.Name) ^
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash ^= GetHashCode(a.DeclaringType);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares properties
		/// </summary>
		/// <param name="a">Property #1</param>
		/// <param name="b">Property #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(PropertyDef a, PropertyDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: Also compare its declaring type if ComparePropertyDeclaringType is true
			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Type, b.Type);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			//TODO: Also compare its declaring type if ComparePropertyDeclaringType is true
			int hash = UTF8String.GetHashCode(a.Name) ^
					GetHashCode(a.Type);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		/// <summary>
		/// Compares events
		/// </summary>
		/// <param name="a">Event #1</param>
		/// <param name="b">Event #2</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public bool Compare(EventDef a, EventDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			//TODO: Also compare its declaring type if CompareEventDeclaringType is true
			bool result = UTF8String.CompareTo(a.Name, b.Name) == 0 &&
					Compare(a.Type, b.Type);

			recursionCounter.DecrementRecursionCounter();
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
			if (!recursionCounter.IncrementRecursionCounter())
				return 0;

			//TODO: Also compare its declaring type if CompareEventDeclaringType is true
			int hash = UTF8String.GetHashCode(a.Name) ^
					GetHashCode(a.Type);

			recursionCounter.DecrementRecursionCounter();
			return hash;
		}

		// Compares a with b, and a must be the global type
		private bool CompareGlobal(TypeDef a, ModuleRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;

			bool result = a.IsGlobalModuleType && Compare((IModule)a.OwnerModule, (IModule)b);

			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		// Compares a with b, and a must be the global type
		private bool CompareGlobal(TypeRef a, ModuleRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.IncrementRecursionCounter())
				return false;
			bool result = false;

			var scope = a.ResolutionScope;
			if (scope == null || scope is TypeRef)
				goto exit;
			var aMod = scope as IModule;
			if (aMod != null && !Compare(aMod, b))
					goto exit;
			result = IsGlobalModuleType(a);
exit:
			recursionCounter.DecrementRecursionCounter();
			return result;
		}

		static readonly UTF8String MODULE_GLOBAL_TYPE_NAME = new UTF8String("<Module>");
		private bool IsGlobalModuleType(TypeRef a) {
			var scope = a.ResolutionScope;
			var modDef = scope as ModuleDef;
			if (modDef != null)
				return IsGlobalModuleType(a, modDef);

			if (scope == null || scope is TypeRef)
				return false;

			//TODO: Use a.OwnerModule.Assembly to find the module a.ResolutionScope (asm or modref)
			//		points to. Then call IsGlobalModuleType(TypeRef,ModuleDef)
			// Until then, check the name
			return UTF8String.CompareTo(a.Name, MODULE_GLOBAL_TYPE_NAME) == 0;
		}

		bool IsGlobalModuleType(TypeRef a, ModuleDef module) {
			if (a == null || module == null || module.Types.Count == 0)
				return false;
			var global = module.Types[0];
			if (a.ResolutionScope is TypeRef != (global.DeclaringType != null))
				return false;
			return UTF8String.CompareTo(global.Namespace, a.Namespace) == 0 &&
					UTF8String.CompareTo(global.Name, a.Name) == 0;
		}
	}
}
