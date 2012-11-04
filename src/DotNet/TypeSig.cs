using System.Collections.Generic;
using dot10.DotNet.MD;

/*
All TypeSig classes:

TypeSig								base class
	LeafSig							base class for leaf types
		TypeDefOrRefSig				contains a ITypeDefOrRef instance
			CorLibTypeSig			simple corlib types (eg. System.Int32)
			ClassOrValueTypeSig		base class for Class/ValueType element types
				ValueTypeSig		ValueType
				ClassSig			Class
		GenericSig					base class for generic vars
			GenericVar				Generic type parameter
			GenericMVar				Generic method parameter
		SentinelSig					Sentinel
		FnPtrSig					Function pointer sig
		GenericInstSig				Generic instance type (contains a generic type + all generic args)
	NonLeafSig						base class for non-leaf types
		PtrSig						Pointer
		ByRefSig					By ref
		ArraySig					Array
		SZArraySig					Single dimension, zero lower limit array (i.e., THETYPE[])
		ModifierSig					C modifier base class
			CModReqdSig				C required modifier
			CModOptSig				C optional modifier
		PinnedSig					Pinned
		ValueArraySig				Value array (undocumented/not implemented by the CLR so don't use it)
		ModuleSig					Module (undocumented/not implemented by the CLR so don't use it)
*/

namespace dot10.DotNet {
	/// <summary>
	/// Type sig base class
	/// </summary>
	public abstract class TypeSig : IType {
		uint rid;

		/// <summary>
		/// Returns the wrapped element type. Can only be <c>null</c> if it was an invalid sig or
		/// if it's a <see cref="LeafSig"/>
		/// </summary>
		public abstract TypeSig Next { get; }

		/// <summary>
		/// Gets the element type
		/// </summary>
		public abstract ElementType ElementType { get; }

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeSpec, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsMethod {
			get { return false; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsType {
			get { return true; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var type = this.RemovePinnedAndModifiers() as GenericInstSig;
				return type == null ? 0 : type.GenericArguments.Count;
			}
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				return this.RemovePinnedAndModifiers() is ValueTypeSig;
			}
		}

		/// <inheritdoc/>
		public string TypeName {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		public string Namespace {
			get { return FullNameCreator.Namespace(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true); }
		}

		/// <inheritdoc/>
		public string AssemblyQualifiedName {
			get { return FullNameCreator.AssemblyQualifiedName(this); }
		}

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly {
			get { return FullNameCreator.DefinitionAssembly(this); }
		}

		/// <inheritdoc/>
		public IScope Scope {
			get { return FullNameCreator.Scope(this); }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType {
			get { return FullNameCreator.ScopeType(this); }
		}

		/// <inheritdoc/>
		public ModuleDef OwnerModule {
			get { return FullNameCreator.OwnerModule(this); }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="TypeDefOrRefSig"/>
		/// </summary>
		public bool IsTypeDefOrRef {
			get { return this is TypeDefOrRefSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="CorLibTypeSig"/>
		/// </summary>
		public bool IsCorLibType {
			get { return this is CorLibTypeSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ClassSig"/>
		/// </summary>
		public bool IsClassSig {
			get { return this is ClassSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ValueTypeSig"/>
		/// </summary>
		public bool IsValueTypeSig {
			get { return this is ValueTypeSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericSig"/>
		/// </summary>
		public bool IsGenericParameter {
			get { return this is GenericSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericVar"/>
		/// </summary>
		public bool IsGenericTypeParameter {
			get { return this is GenericVar; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericMVar"/>
		/// </summary>
		public bool IsGenericMethodParameter {
			get { return this is GenericMVar; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="SentinelSig"/>
		/// </summary>
		public bool IsSentinel {
			get { return this is SentinelSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="FnPtrSig"/>
		/// </summary>
		public bool IsFunctionPointer {
			get { return this is FnPtrSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericInstSig"/>
		/// </summary>
		public bool IsGenericInstanceType {
			get { return this is GenericInstSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="PtrSig"/>
		/// </summary>
		public bool IsPointer {
			get { return this is PtrSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ByRefSig"/>
		/// </summary>
		public bool IsByRef {
			get { return this is ByRefSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ArraySig"/>
		/// </summary>
		public bool IsArray {
			get { return this is ArraySig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="SZArraySig"/>
		/// </summary>
		public bool IsSZArray {
			get { return this is SZArraySig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ModifierSig"/>
		/// </summary>
		public bool IsModifier {
			get { return this is ModifierSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="CModReqdSig"/>
		/// </summary>
		public bool IsRequiredModifier {
			get { return this is CModReqdSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="CModOptSig"/>
		/// </summary>
		public bool IsOptionalModifier {
			get { return this is CModOptSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="PinnedSig"/>
		/// </summary>
		public bool IsPinned {
			get { return this is PinnedSig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ValueArraySig"/>
		/// </summary>
		public bool IsValueArray {
			get { return this is ValueArraySig; }
		}

		/// <summary>
		/// <c>true</c> if it's a <see cref="ModuleSig"/>
		/// </summary>
		public bool IsModuleSig {
			get { return this is ModuleSig; }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	static partial class Extensions {
		/// <summary>
		/// Removes all C optional/required modifiers
		/// </summary>
		/// <param name="a">A <see cref="TypeSig"/> instance</param>
		/// <returns>Input after all modifiers</returns>
		public static TypeSig RemoveModifiers(this TypeSig a) {
			if (a == null)
				return null;
			while (true) {
				var modifier = a as ModifierSig;
				if (modifier == null)
					return a;
				a = a.Next;
			}
		}

		/// <summary>
		/// Removes pinned signature
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>Input after pinned signature</returns>
		public static TypeSig RemovePinned(this TypeSig a) {
			var pinned = a as PinnedSig;
			if (pinned == null)
				return a;
			return pinned.Next;
		}

		/// <summary>
		/// Removes all modifiers and pinned sig
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>Inputer after modifiers and pinned signature</returns>
		public static TypeSig RemovePinnedAndModifiers(this TypeSig a) {
			a = a.RemoveModifiers();
			a = a.RemovePinned();
			a = a.RemoveModifiers();
			return a;
		}
	}

	/// <summary>
	/// Base class for element types that are last in a type sig, ie.,
	/// <see cref="TypeDefOrRefSig"/>, <see cref="GenericSig"/>, <see cref="SentinelSig"/>,
	/// <see cref="FnPtrSig"/>, <see cref="GenericInstSig"/>
	/// </summary>
	public abstract class LeafSig : TypeSig {
		/// <inheritdoc/>
		public sealed override TypeSig Next {
			get { return null; }
		}
	}

	/// <summary>
	/// Wraps a <see cref="ITypeDefOrRef"/>
	/// </summary>
	public abstract class TypeDefOrRefSig : LeafSig {
		readonly ITypeDefOrRef typeDefOrRef;

		/// <summary>
		/// Gets the the <c>TypeDefOrRef</c>
		/// </summary>
		public ITypeDefOrRef TypeDefOrRef {
			get { return typeDefOrRef; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeRef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeRef {
			get { return TypeRef != null; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeDef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeDef {
			get { return TypeDef != null; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeSpec"/> != <c>null</c>
		/// </summary>
		public bool IsTypeSpec {
			get { return TypeSpec != null; }
		}

		/// <summary>
		/// Gets the <see cref="TypeRef"/> or <c>null</c> if it's not a <see cref="TypeRef"/>
		/// </summary>
		public TypeRef TypeRef {
			get { return typeDefOrRef as TypeRef; }
		}

		/// <summary>
		/// Gets the <see cref="TypeDef"/> or <c>null</c> if it's not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef TypeDef {
			get { return typeDefOrRef as TypeDef; }
		}

		/// <summary>
		/// Gets the <see cref="TypeSpec"/> or <c>null</c> if it's not a <see cref="TypeSpec"/>
		/// </summary>
		public TypeSpec TypeSpec {
			get { return typeDefOrRef as TypeSpec; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="TypeRef"/>, <see cref="TypeDef"/> or
		/// a <see cref="TypeSpec"/></param>
		protected TypeDefOrRefSig(ITypeDefOrRef typeDefOrRef) {
			this.typeDefOrRef = typeDefOrRef;
		}
	}

	/// <summary>
	/// A core library type
	/// </summary>
	public sealed class CorLibTypeSig : TypeDefOrRefSig {
		readonly ElementType elementType;

		/// <summary>
		/// Gets the element type
		/// </summary>
		public override ElementType ElementType {
			get { return elementType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="corType">The type</param>
		/// <param name="elementType">The type's element type</param>
		public CorLibTypeSig(TypeRef corType, ElementType elementType)
			: base(corType) {
			this.elementType = elementType;
		}
	}

	/// <summary>
	/// Base class for class/valuetype element types
	/// </summary>
	public abstract class ClassOrValueTypeSig : TypeDefOrRefSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		protected ClassOrValueTypeSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ValueType"/>
	/// </summary>
	public sealed class ValueTypeSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ValueType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		public ValueTypeSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Class"/>
	/// </summary>
	public sealed class ClassSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Class; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		public ClassSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Generic method/type var base class
	/// </summary>
	public abstract class GenericSig : LeafSig {
		readonly bool isTypeVar;
		readonly uint number;

		/// <summary>
		/// Gets the generic param number
		/// </summary>
		public uint Number {
			get { return number; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's a <c>Var</c>, <c>false</c> if it's a <c>MVar</c></param>
		/// <param name="number">Generic param number</param>
		protected GenericSig(bool isTypeVar, uint number) {
			this.isTypeVar = isTypeVar;
			this.number = number;
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>MVar</c> element type
		/// </summary>
		public bool IsMethodVar {
			get { return !isTypeVar; }
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>Var</c> element type
		/// </summary>
		public bool IsTypeVar {
			get { return isTypeVar; }
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Var"/>
	/// </summary>
	public sealed class GenericVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Var; }
		}

		/// <inheritdoc/>
		public GenericVar(uint number)
			: base(true, number) {
		}

		/// <inheritdoc/>
		public GenericVar(int number)
			: base(true, (uint)number) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.MVar"/>
	/// </summary>
	public sealed class GenericMVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.MVar; }
		}

		/// <inheritdoc/>
		public GenericMVar(uint number)
			: base(false, number) {
		}

		/// <inheritdoc/>
		public GenericMVar(int number)
			: base(true, (uint)number) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Sentinel"/>
	/// </summary>
	public sealed class SentinelSig : LeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Sentinel; }
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.FnPtr"/>
	/// </summary>
	public sealed class FnPtrSig : LeafSig {
		readonly CallingConventionSig signature;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.FnPtr; }
		}

		/// <summary>
		/// Gets the signature
		/// </summary>
		public CallingConventionSig Signature {
			get { return signature; }
		}

		/// <summary>
		/// Gets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get { return signature as MethodSig; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="signature">The method signature</param>
		public FnPtrSig(CallingConventionSig signature) {
			this.signature = signature;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.GenericInst"/>
	/// </summary>
	public sealed class GenericInstSig : LeafSig {
		ClassOrValueTypeSig genericType;
		readonly List<TypeSig> genericArgs;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.GenericInst; }
		}

		/// <summary>
		/// Gets the generic type
		/// </summary>
		public ClassOrValueTypeSig GenericType {
			get { return genericType; }
			set { genericType = value; }
		}

		/// <summary>
		/// Gets/sets the generic arguments (it's never <c>null</c>)
		/// </summary>
		public IList<TypeSig> GenericArguments {
			get { return genericArgs; }
			set {
				genericArgs.Clear();
				if (value != null)
					genericArgs.AddRange(value);
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericInstSig() {
			this.genericArgs = new List<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		public GenericInstSig(ClassOrValueTypeSig genericType) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgCount">Number of generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, uint genArgCount) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig>((int)genArgCount);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgCount">Number of generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, int genArgCount)
			: this(genericType, (uint)genArgCount) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, TypeSig genArg1) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig> { genArg1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		/// <param name="genArg2">Generic argument #2</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, TypeSig genArg1, TypeSig genArg2) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig> { genArg1, genArg2 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		/// <param name="genArg2">Generic argument #2</param>
		/// <param name="genArg3">Generic argument #3</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, TypeSig genArg1, TypeSig genArg2, TypeSig genArg3) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig> { genArg1, genArg2, genArg3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, params TypeSig[] genArgs) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig>(genArgs);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, IList<TypeSig> genArgs) {
			this.genericType = genericType;
			this.genericArgs = new List<TypeSig>(genArgs);
		}
	}

	/// <summary>
	/// Base class of non-leaf element types
	/// </summary>
	public abstract class NonLeafSig : TypeSig {
		readonly TypeSig nextSig;

		/// <inheritdoc/>
		public sealed override TypeSig Next {
			get { return nextSig; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">Next sig</param>
		protected NonLeafSig(TypeSig nextSig) {
			this.nextSig = nextSig;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Ptr"/>
	/// </summary>
	public sealed class PtrSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Ptr; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PtrSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ByRef"/>
	/// </summary>
	public sealed class ByRefSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ByRef; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ByRefSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Array"/>
	/// </summary>
	/// <seealso cref="SZArraySig"/>
	public sealed class ArraySig : NonLeafSig {
		uint rank;
		readonly IList<uint> sizes;
		readonly IList<int> lowerBounds;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Array; }
		}

		/// <summary>
		/// Gets/sets the rank (max value is <c>0x1FFFFFFF</c>)
		/// </summary>
		public uint Rank {
			get { return rank; }
			set { rank = value; }
		}

		/// <summary>
		/// Gets all sizes (max elements is <c>0x1FFFFFFF</c>)
		/// </summary>
		public IList<uint> Sizes {
			get { return sizes; }
		}

		/// <summary>
		/// Gets all lower bounds (max elements is <c>0x1FFFFFFF</c>)
		/// </summary>
		public IList<int> LowerBounds {
			get { return lowerBounds; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		public ArraySig(TypeSig arrayType)
			: base(arrayType) {
			this.sizes = new List<uint>();
			this.lowerBounds = new List<int>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		public ArraySig(TypeSig arrayType, uint rank)
			: base(arrayType) {
			this.rank = rank;
			this.sizes = new List<uint>();
			this.lowerBounds = new List<int>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		public ArraySig(TypeSig arrayType, int rank)
			: this(arrayType, (uint)rank) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		/// <param name="sizes">Sizes list. <c>This instance will be the owner of this list.</c></param>
		/// <param name="lowerBounds">Lower bounds list. <c>This instance will be the owner of this list.</c></param>
		public ArraySig(TypeSig arrayType, uint rank, IEnumerable<uint> sizes, IEnumerable<int> lowerBounds)
			: base(arrayType) {
			this.rank = rank;
			this.sizes = new List<uint>(sizes);
			this.lowerBounds = new List<int>(lowerBounds);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		/// <param name="sizes">Sizes list. <c>This instance will be the owner of this list.</c></param>
		/// <param name="lowerBounds">Lower bounds list. <c>This instance will be the owner of this list.</c></param>
		public ArraySig(TypeSig arrayType, int rank, IEnumerable<uint> sizes, IEnumerable<int> lowerBounds)
			: this(arrayType, (uint)rank, sizes, lowerBounds) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		/// <param name="sizes">Sizes list. <c>This instance will be the owner of this list.</c></param>
		/// <param name="lowerBounds">Lower bounds list. <c>This instance will be the owner of this list.</c></param>
		internal ArraySig(TypeSig arrayType, uint rank, IList<uint> sizes, IList<int> lowerBounds)
			: base(arrayType) {
			this.rank = rank;
			this.sizes = sizes;
			this.lowerBounds = lowerBounds;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.SZArray"/> (single dimension, zero lower bound array)
	/// </summary>
	/// <seealso cref="ArraySig"/>
	public sealed class SZArraySig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.SZArray; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public SZArraySig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Base class for modifier type sigs
	/// </summary>
	public abstract class ModifierSig : NonLeafSig {
		readonly ITypeDefOrRef modifier;

		/// <summary>
		/// Returns the modifier type
		/// </summary>
		public ITypeDefOrRef Modifier {
			get { return modifier; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifier">Modifier type</param>
		/// <param name="nextSig">The next element type</param>
		protected ModifierSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(nextSig) {
			this.modifier = modifier;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.CModReqd"/>
	/// </summary>
	public sealed class CModReqdSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.CModReqd; }
		}

		/// <inheritdoc/>
		public CModReqdSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.CModOpt"/>
	/// </summary>
	public sealed class CModOptSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.CModOpt; }
		}

		/// <inheritdoc/>
		public CModOptSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Pinned"/>
	/// </summary>
	public sealed class PinnedSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Pinned; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PinnedSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.ValueArray"/>
	/// </summary>
	public sealed class ValueArraySig : NonLeafSig {
		uint size;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.ValueArray; }
		}

		/// <summary>
		/// Gets/sets the size
		/// </summary>
		public uint Size {
			get { return size; }
			set { size = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		/// <param name="size">Size of the array</param>
		public ValueArraySig(TypeSig nextSig, uint size)
			: base(nextSig) {
			this.size = size;
		}
	}

	/// <summary>
	/// Represents a <see cref="dot10.DotNet.ElementType.Module"/>
	/// </summary>
	public sealed class ModuleSig : NonLeafSig {
		uint index;

		/// <inheritdoc/>
		public override ElementType ElementType {
			get { return ElementType.Module; }
		}

		/// <summary>
		/// Gets/sets the index
		/// </summary>
		public uint Index {
			get { return index; }
			set { index = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="nextSig">The next element type</param>
		public ModuleSig(uint index, TypeSig nextSig)
			: base(nextSig) {
			this.index = index;
		}
	}
}
