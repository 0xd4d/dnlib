// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.MD;

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
		ArraySigBase				Array base class
			ArraySig				Array
			SZArraySig				Single dimension, zero lower limit array (i.e., THETYPE[])
		ModifierSig					C modifier base class
			CModReqdSig				C required modifier
			CModOptSig				C optional modifier
		PinnedSig					Pinned
		ValueArraySig				Value array (undocumented/not implemented by the CLR so don't use it)
		ModuleSig					Module (undocumented/not implemented by the CLR so don't use it)
*/

namespace dnlib.DotNet {
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
		public MDToken MDToken => new MDToken(Table.TypeSpec, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		bool IIsTypeOrMethod.IsMethod => false;

		/// <inheritdoc/>
		bool IIsTypeOrMethod.IsType => true;

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var type = this.RemovePinnedAndModifiers() as GenericInstSig;
				return type is null ? 0 : type.GenericArguments.Count;
			}
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var t = this.RemovePinnedAndModifiers();
				if (t is null)
					return false;
				if (t.ElementType == ElementType.GenericInst) {
					var gis = (GenericInstSig)t;
					t = gis.GenericType;
					if (t is null)
						return false;
				}
				return t.ElementType.IsValueType();
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive => ElementType.IsPrimitive();

		/// <inheritdoc/>
		public string TypeName => FullNameFactory.Name(this, false, null);

		/// <inheritdoc/>
		UTF8String IFullName.Name {
			get => new UTF8String(FullNameFactory.Name(this, false, null));
			set => throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public string ReflectionName => FullNameFactory.Name(this, true, null);

		/// <inheritdoc/>
		public string Namespace => FullNameFactory.Namespace(this, false, null);

		/// <inheritdoc/>
		public string ReflectionNamespace => FullNameFactory.Namespace(this, true, null);

		/// <inheritdoc/>
		public string FullName => FullNameFactory.FullName(this, false, null, null, null, null);

		/// <inheritdoc/>
		public string ReflectionFullName => FullNameFactory.FullName(this, true, null, null, null, null);

		/// <inheritdoc/>
		public string AssemblyQualifiedName => FullNameFactory.AssemblyQualifiedName(this, null, null);

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly => FullNameFactory.DefinitionAssembly(this);

		/// <inheritdoc/>
		public IScope Scope => FullNameFactory.Scope(this);

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType => FullNameFactory.ScopeType(this);

		/// <inheritdoc/>
		public ModuleDef Module => FullNameFactory.OwnerModule(this);

		/// <summary>
		/// <c>true</c> if it's a <see cref="TypeDefOrRefSig"/>
		/// </summary>
		public bool IsTypeDefOrRef => this is TypeDefOrRefSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="CorLibTypeSig"/>
		/// </summary>
		public bool IsCorLibType => this is CorLibTypeSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ClassSig"/>
		/// </summary>
		public bool IsClassSig => this is ClassSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ValueTypeSig"/>
		/// </summary>
		public bool IsValueTypeSig => this is ValueTypeSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericSig"/>
		/// </summary>
		public bool IsGenericParameter => this is GenericSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericVar"/>
		/// </summary>
		public bool IsGenericTypeParameter => this is GenericVar;

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericMVar"/>
		/// </summary>
		public bool IsGenericMethodParameter => this is GenericMVar;

		/// <summary>
		/// <c>true</c> if it's a <see cref="SentinelSig"/>
		/// </summary>
		public bool IsSentinel => this is SentinelSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="FnPtrSig"/>
		/// </summary>
		public bool IsFunctionPointer => this is FnPtrSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericInstSig"/>
		/// </summary>
		public bool IsGenericInstanceType => this is GenericInstSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="PtrSig"/>
		/// </summary>
		public bool IsPointer => this is PtrSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ByRefSig"/>
		/// </summary>
		public bool IsByRef => this is ByRefSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ArraySig"/> or a <see cref="SZArraySig"/>
		/// </summary>
		public bool IsSingleOrMultiDimensionalArray => this is ArraySigBase;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ArraySig"/>
		/// </summary>
		public bool IsArray => this is ArraySig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="SZArraySig"/>
		/// </summary>
		public bool IsSZArray => this is SZArraySig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ModifierSig"/>
		/// </summary>
		public bool IsModifier => this is ModifierSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="CModReqdSig"/>
		/// </summary>
		public bool IsRequiredModifier => this is CModReqdSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="CModOptSig"/>
		/// </summary>
		public bool IsOptionalModifier => this is CModOptSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="PinnedSig"/>
		/// </summary>
		public bool IsPinned => this is PinnedSig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ValueArraySig"/>
		/// </summary>
		public bool IsValueArray => this is ValueArraySig;

		/// <summary>
		/// <c>true</c> if it's a <see cref="ModuleSig"/>
		/// </summary>
		public bool IsModuleSig => this is ModuleSig;

		/// <summary>
		/// <c>true</c> if this <see cref="TypeSig"/> contains a <see cref="GenericVar"/> or a
		/// <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	public static partial class Extensions {
		/// <summary>
		/// Removes all C optional/required modifiers
		/// </summary>
		/// <param name="a">A <see cref="TypeSig"/> instance</param>
		/// <returns>Input after all modifiers</returns>
		public static TypeSig RemoveModifiers(this TypeSig a) {
			if (a is null)
				return null;
			while (true) {
				var modifier = a as ModifierSig;
				if (modifier is null)
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
			if (pinned is null)
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

		/// <summary>
		/// Returns a <see cref="TypeDefOrRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="TypeDefOrRefSig"/> or <c>null</c> if it's not a
		/// <see cref="TypeDefOrRefSig"/></returns>
		public static TypeDefOrRefSig ToTypeDefOrRefSig(this TypeSig type) => type.RemovePinnedAndModifiers() as TypeDefOrRefSig;

		/// <summary>
		/// Returns a <see cref="ClassOrValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassOrValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassOrValueTypeSig"/></returns>
		public static ClassOrValueTypeSig ToClassOrValueTypeSig(this TypeSig type) => type.RemovePinnedAndModifiers() as ClassOrValueTypeSig;

		/// <summary>
		/// Returns a <see cref="ValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ValueTypeSig"/></returns>
		public static ValueTypeSig ToValueTypeSig(this TypeSig type) => type.RemovePinnedAndModifiers() as ValueTypeSig;

		/// <summary>
		/// Returns a <see cref="ClassSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassSig"/></returns>
		public static ClassSig ToClassSig(this TypeSig type) => type.RemovePinnedAndModifiers() as ClassSig;

		/// <summary>
		/// Returns a <see cref="GenericSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericSig"/></returns>
		public static GenericSig ToGenericSig(this TypeSig type) => type.RemovePinnedAndModifiers() as GenericSig;

		/// <summary>
		/// Returns a <see cref="GenericVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericVar"/></returns>
		public static GenericVar ToGenericVar(this TypeSig type) => type.RemovePinnedAndModifiers() as GenericVar;

		/// <summary>
		/// Returns a <see cref="GenericMVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericMVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericMVar"/></returns>
		public static GenericMVar ToGenericMVar(this TypeSig type) => type.RemovePinnedAndModifiers() as GenericMVar;

		/// <summary>
		/// Returns a <see cref="GenericInstSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericInstSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericInstSig"/></returns>
		public static GenericInstSig ToGenericInstSig(this TypeSig type) => type.RemovePinnedAndModifiers() as GenericInstSig;

		/// <summary>
		/// Returns a <see cref="PtrSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="PtrSig"/> or <c>null</c> if it's not a
		/// <see cref="PtrSig"/></returns>
		public static PtrSig ToPtrSig(this TypeSig type) => type.RemovePinnedAndModifiers() as PtrSig;

		/// <summary>
		/// Returns a <see cref="ByRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ByRefSig"/> or <c>null</c> if it's not a
		/// <see cref="ByRefSig"/></returns>
		public static ByRefSig ToByRefSig(this TypeSig type) => type.RemovePinnedAndModifiers() as ByRefSig;

		/// <summary>
		/// Returns a <see cref="ArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ArraySig"/> or <c>null</c> if it's not a
		/// <see cref="ArraySig"/></returns>
		public static ArraySig ToArraySig(this TypeSig type) => type.RemovePinnedAndModifiers() as ArraySig;

		/// <summary>
		/// Returns a <see cref="SZArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="SZArraySig"/> or <c>null</c> if it's not a
		/// <see cref="SZArraySig"/></returns>
		public static SZArraySig ToSZArraySig(this TypeSig type) => type.RemovePinnedAndModifiers() as SZArraySig;

		/// <summary>
		/// Gets the next field or <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static TypeSig GetNext(this TypeSig self) => self?.Next;

		/// <summary>
		/// Gets the <see cref="TypeSig.IsValueType"/> value or <c>false</c> if
		/// <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static bool GetIsValueType(this TypeSig self) => self is null ? false : self.IsValueType;

		/// <summary>
		/// Gets the <see cref="TypeSig.IsPrimitive"/> value or <c>false</c> if
		/// <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static bool GetIsPrimitive(this TypeSig self) => self is null ? false : self.IsPrimitive;

		/// <summary>
		/// Gets the element type
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>The element type</returns>
		public static ElementType GetElementType(this TypeSig a) => a is null ? ElementType.End : a.ElementType;

		/// <summary>
		/// Gets the full name of the type
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>Full name of the type</returns>
		public static string GetFullName(this TypeSig a) => a is null ? string.Empty : a.FullName;

		/// <summary>
		/// Gets the name of the type
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>Name of the type</returns>
		public static string GetName(this TypeSig a) => a is null ? string.Empty : a.TypeName;

		/// <summary>
		/// Gets the namespace of the type
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>Namespace of the type</returns>
		public static string GetNamespace(this TypeSig a) => a is null ? string.Empty : a.Namespace;

		/// <summary>
		/// Returns the <see cref="ITypeDefOrRef"/> if it is a <see cref="TypeDefOrRefSig"/>.
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>A <see cref="ITypeDefOrRef"/> or <c>null</c> if none found</returns>
		public static ITypeDefOrRef TryGetTypeDefOrRef(this TypeSig a) => (a.RemovePinnedAndModifiers() as TypeDefOrRefSig)?.TypeDefOrRef;

		/// <summary>
		/// Returns the <see cref="TypeRef"/> if it is a <see cref="TypeDefOrRefSig"/>.
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>A <see cref="TypeRef"/> or <c>null</c> if none found</returns>
		public static TypeRef TryGetTypeRef(this TypeSig a) => (a.RemovePinnedAndModifiers() as TypeDefOrRefSig)?.TypeRef;

		/// <summary>
		/// Returns the <see cref="TypeDef"/> if it is a <see cref="TypeDefOrRefSig"/>.
		/// Nothing is resolved.
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>A <see cref="TypeDef"/> or <c>null</c> if none found</returns>
		public static TypeDef TryGetTypeDef(this TypeSig a) => (a.RemovePinnedAndModifiers() as TypeDefOrRefSig)?.TypeDef;

		/// <summary>
		/// Returns the <see cref="TypeSpec"/> if it is a <see cref="TypeDefOrRefSig"/>.
		/// </summary>
		/// <param name="a">this</param>
		/// <returns>A <see cref="TypeSpec"/> or <c>null</c> if none found</returns>
		public static TypeSpec TryGetTypeSpec(this TypeSig a) => (a.RemovePinnedAndModifiers() as TypeDefOrRefSig)?.TypeSpec;
	}

	/// <summary>
	/// Base class for element types that are last in a type sig, ie.,
	/// <see cref="TypeDefOrRefSig"/>, <see cref="GenericSig"/>, <see cref="SentinelSig"/>,
	/// <see cref="FnPtrSig"/>, <see cref="GenericInstSig"/>
	/// </summary>
	public abstract class LeafSig : TypeSig {
		/// <inheritdoc/>
		public sealed override TypeSig Next => null;
	}

	/// <summary>
	/// Wraps a <see cref="ITypeDefOrRef"/>
	/// </summary>
	public abstract class TypeDefOrRefSig : LeafSig {
		readonly ITypeDefOrRef typeDefOrRef;

		/// <summary>
		/// Gets the the <c>TypeDefOrRef</c>
		/// </summary>
		public ITypeDefOrRef TypeDefOrRef => typeDefOrRef;

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeRef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeRef => !(TypeRef is null);

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeDef"/> != <c>null</c>
		/// </summary>
		public bool IsTypeDef => !(TypeDef is null);

		/// <summary>
		/// Returns <c>true</c> if <see cref="TypeSpec"/> != <c>null</c>
		/// </summary>
		public bool IsTypeSpec => !(TypeSpec is null);

		/// <summary>
		/// Gets the <see cref="TypeRef"/> or <c>null</c> if it's not a <see cref="TypeRef"/>
		/// </summary>
		public TypeRef TypeRef => typeDefOrRef as TypeRef;

		/// <summary>
		/// Gets the <see cref="TypeDef"/> or <c>null</c> if it's not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef TypeDef => typeDefOrRef as TypeDef;

		/// <summary>
		/// Gets the <see cref="TypeSpec"/> or <c>null</c> if it's not a <see cref="TypeSpec"/>
		/// </summary>
		public TypeSpec TypeSpec => typeDefOrRef as TypeSpec;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="TypeRef"/>, <see cref="TypeDef"/> or
		/// a <see cref="TypeSpec"/></param>
		protected TypeDefOrRefSig(ITypeDefOrRef typeDefOrRef) => this.typeDefOrRef = typeDefOrRef;
	}

	/// <summary>
	/// A core library type
	/// </summary>
	public sealed class CorLibTypeSig : TypeDefOrRefSig {
		readonly ElementType elementType;

		/// <summary>
		/// Gets the element type
		/// </summary>
		public override ElementType ElementType => elementType;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="corType">The type which must be a <see cref="TypeRef"/> or a
		/// <see cref="TypeDef"/>. <see cref="TypeSpec"/> and <c>null</c> are not allowed.</param>
		/// <param name="elementType">The type's element type</param>
		public CorLibTypeSig(ITypeDefOrRef corType, ElementType elementType)
			: base(corType) {
			if (!(corType is TypeRef) && !(corType is TypeDef))
				throw new ArgumentException("corType must be a TypeDef or a TypeRef. null and TypeSpec are invalid inputs.");
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
	/// Represents a <see cref="dnlib.DotNet.ElementType.ValueType"/>
	/// </summary>
	public sealed class ValueTypeSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.ValueType;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="ITypeDefOrRef"/></param>
		public ValueTypeSig(ITypeDefOrRef typeDefOrRef)
			: base(typeDefOrRef) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Class"/>
	/// </summary>
	public sealed class ClassSig : ClassOrValueTypeSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Class;

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
		readonly ITypeOrMethodDef genericParamProvider;

		/// <summary>
		/// <c>true</c> if it has an owner <see cref="TypeDef"/> or <see cref="MethodDef"/>
		/// </summary>
		public bool HasOwner => !(genericParamProvider is null);

		/// <summary>
		/// <c>true</c> if it has an owner <see cref="TypeDef"/> (<see cref="OwnerType"/> is
		/// not <c>null</c>)
		/// </summary>
		public bool HasOwnerType => !(OwnerType is null);

		/// <summary>
		/// <c>true</c> if it has an owner <see cref="MethodDef"/> (<see cref="OwnerMethod"/> is
		/// not <c>null</c>)
		/// </summary>
		public bool HasOwnerMethod => !(OwnerMethod is null);

		/// <summary>
		/// Gets the owner type or <c>null</c> if the owner is a <see cref="MethodDef"/> or if it
		/// has no owner.
		/// </summary>
		public TypeDef OwnerType => genericParamProvider as TypeDef;

		/// <summary>
		/// Gets the owner method or <c>null</c> if the owner is a <see cref="TypeDef"/> or if it
		/// has no owner.
		/// </summary>
		public MethodDef OwnerMethod => genericParamProvider as MethodDef;

		/// <summary>
		/// Gets the generic param number
		/// </summary>
		public uint Number => number;

		/// <summary>
		/// Gets the corresponding <see cref="dnlib.DotNet.GenericParam"/> or <c>null</c> if none exists.
		/// </summary>
		public GenericParam GenericParam {
			get {
				var gpp = genericParamProvider;
				if (gpp is null)
					return null;
				var gps = gpp.GenericParameters;
				int count = gps.Count;
				for (int i = 0; i < count; i++) {
					var gp = gps[i];
					if (gp.Number == number)
						return gp;
				}
				return null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's a <c>Var</c>, <c>false</c> if it's a <c>MVar</c></param>
		/// <param name="number">Generic param number</param>
		protected GenericSig(bool isTypeVar, uint number)
			: this(isTypeVar, number, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isTypeVar"><c>true</c> if it's a <c>Var</c>, <c>false</c> if it's a <c>MVar</c></param>
		/// <param name="number">Generic param number</param>
		/// <param name="genericParamProvider">Owner method/type or <c>null</c></param>
		protected GenericSig(bool isTypeVar, uint number, ITypeOrMethodDef genericParamProvider) {
			this.isTypeVar = isTypeVar;
			this.number = number;
			this.genericParamProvider = genericParamProvider;
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>MVar</c> element type
		/// </summary>
		public bool IsMethodVar => !isTypeVar;

		/// <summary>
		/// Returns <c>true</c> if it's a <c>Var</c> element type
		/// </summary>
		public bool IsTypeVar => isTypeVar;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Var"/>
	/// </summary>
	public sealed class GenericVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Var;

		/// <inheritdoc/>
		public GenericVar(uint number)
			: base(true, number) {
		}

		/// <inheritdoc/>
		public GenericVar(int number)
			: base(true, (uint)number) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">Generic parameter number</param>
		/// <param name="genericParamProvider">Owner type or <c>null</c></param>
		public GenericVar(uint number, TypeDef genericParamProvider)
			: base(true, number, genericParamProvider) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">Generic parameter number</param>
		/// <param name="genericParamProvider">Owner type or <c>null</c></param>
		public GenericVar(int number, TypeDef genericParamProvider)
			: base(true, (uint)number, genericParamProvider) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.MVar"/>
	/// </summary>
	public sealed class GenericMVar : GenericSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.MVar;

		/// <inheritdoc/>
		public GenericMVar(uint number)
			: base(false, number) {
		}

		/// <inheritdoc/>
		public GenericMVar(int number)
			: base(false, (uint)number) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">Generic parameter number</param>
		/// <param name="genericParamProvider">Owner method or <c>null</c></param>
		public GenericMVar(uint number, MethodDef genericParamProvider)
			: base(false, number, genericParamProvider) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">Generic parameter number</param>
		/// <param name="genericParamProvider">Owner method or <c>null</c></param>
		public GenericMVar(int number, MethodDef genericParamProvider)
			: base(false, (uint)number, genericParamProvider) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Sentinel"/>
	/// </summary>
	public sealed class SentinelSig : LeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Sentinel;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.FnPtr"/>
	/// </summary>
	public sealed class FnPtrSig : LeafSig {
		readonly CallingConventionSig signature;

		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.FnPtr;

		/// <summary>
		/// Gets the signature
		/// </summary>
		public CallingConventionSig Signature => signature;

		/// <summary>
		/// Gets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig => signature as MethodSig;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="signature">The method signature</param>
		public FnPtrSig(CallingConventionSig signature) => this.signature = signature;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.GenericInst"/>
	/// </summary>
	public sealed class GenericInstSig : LeafSig {
		ClassOrValueTypeSig genericType;
		readonly IList<TypeSig> genericArgs;

		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.GenericInst;

		/// <summary>
		/// Gets the generic type
		/// </summary>
		public ClassOrValueTypeSig GenericType {
			get => genericType;
			set => genericType = value;
		}

		/// <summary>
		/// Gets the generic arguments (it's never <c>null</c>)
		/// </summary>
		public IList<TypeSig> GenericArguments => genericArgs;

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericInstSig() => genericArgs = new List<TypeSig>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		public GenericInstSig(ClassOrValueTypeSig genericType) {
			this.genericType = genericType;
			genericArgs = new List<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgCount">Number of generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, uint genArgCount) {
			this.genericType = genericType;
			genericArgs = new List<TypeSig>((int)genArgCount);
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
			genericArgs = new List<TypeSig> { genArg1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArg1">Generic argument #1</param>
		/// <param name="genArg2">Generic argument #2</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, TypeSig genArg1, TypeSig genArg2) {
			this.genericType = genericType;
			genericArgs = new List<TypeSig> { genArg1, genArg2 };
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
			genericArgs = new List<TypeSig> { genArg1, genArg2, genArg3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, params TypeSig[] genArgs) {
			this.genericType = genericType;
			genericArgs = new List<TypeSig>(genArgs);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericType">The generic type</param>
		/// <param name="genArgs">Generic arguments</param>
		public GenericInstSig(ClassOrValueTypeSig genericType, IList<TypeSig> genArgs) {
			this.genericType = genericType;
			genericArgs = new List<TypeSig>(genArgs);
		}
	}

	/// <summary>
	/// Base class of non-leaf element types
	/// </summary>
	public abstract class NonLeafSig : TypeSig {
		readonly TypeSig nextSig;

		/// <inheritdoc/>
		public sealed override TypeSig Next => nextSig;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">Next sig</param>
		protected NonLeafSig(TypeSig nextSig) => this.nextSig = nextSig;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Ptr"/>
	/// </summary>
	public sealed class PtrSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Ptr;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PtrSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.ByRef"/>
	/// </summary>
	public sealed class ByRefSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.ByRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ByRefSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Array base class
	/// </summary>
	public abstract class ArraySigBase : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		protected ArraySigBase(TypeSig arrayType)
			: base(arrayType) {
		}

		/// <summary>
		/// <c>true</c> if it's a multi-dimensional array (i.e., <see cref="ArraySig"/>),
		/// and <c>false</c> if it's a single-dimensional array (i.e., <see cref="SZArraySig"/>)
		/// </summary>
		/// <seealso cref="IsSingleDimensional"/>
		public bool IsMultiDimensional => ElementType == ElementType.Array;

		/// <summary>
		/// <c>true</c> if it's a single-dimensional array (i.e., <see cref="SZArraySig"/>),
		/// and <c>false</c> if it's a multi-dimensional array (i.e., <see cref="ArraySig"/>)
		/// </summary>
		/// <see cref="IsMultiDimensional"/>
		public bool IsSingleDimensional => ElementType == ElementType.SZArray;

		/// <summary>
		/// Gets/sets the rank (number of dimensions). This can only be set if
		/// <see cref="IsMultiDimensional"/> is <c>true</c>
		/// </summary>
		public abstract uint Rank { get; set; }

		/// <summary>
		/// Gets all sizes. If it's a <see cref="SZArraySig"/>, then it will be an empty temporary
		/// list that is re-created every time this method is called.
		/// </summary>
		/// <returns>A list of sizes</returns>
		public abstract IList<uint> GetSizes();

		/// <summary>
		/// Gets all lower bounds. If it's a <see cref="SZArraySig"/>, then it will be an empty
		/// temporary list that is re-created every time this method is called.
		/// </summary>
		/// <returns>A list of lower bounds</returns>
		public abstract IList<int> GetLowerBounds();
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Array"/>
	/// </summary>
	/// <seealso cref="SZArraySig"/>
	public sealed class ArraySig : ArraySigBase {
		uint rank;
		readonly IList<uint> sizes;
		readonly IList<int> lowerBounds;

		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Array;

		/// <summary>
		/// Gets/sets the rank (max value is <c>0x1FFFFFFF</c>)
		/// </summary>
		public override uint Rank {
			get => rank;
			set => rank = value;
		}

		/// <summary>
		/// Gets all sizes (max elements is <c>0x1FFFFFFF</c>)
		/// </summary>
		public IList<uint> Sizes => sizes;

		/// <summary>
		/// Gets all lower bounds (max elements is <c>0x1FFFFFFF</c>)
		/// </summary>
		public IList<int> LowerBounds => lowerBounds;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		public ArraySig(TypeSig arrayType)
			: base(arrayType) {
			sizes = new List<uint>();
			lowerBounds = new List<int>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arrayType">Array type</param>
		/// <param name="rank">Array rank</param>
		public ArraySig(TypeSig arrayType, uint rank)
			: base(arrayType) {
			this.rank = rank;
			sizes = new List<uint>();
			lowerBounds = new List<int>();
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

		/// <inheritdoc/>
		public override IList<uint> GetSizes() => sizes;

		/// <inheritdoc/>
		public override IList<int> GetLowerBounds() => lowerBounds;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.SZArray"/> (single dimension, zero lower bound array)
	/// </summary>
	/// <seealso cref="ArraySig"/>
	public sealed class SZArraySig : ArraySigBase {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.SZArray;

		/// <inheritdoc/>
		public override uint Rank {
			get => 1;
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public SZArraySig(TypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		public override IList<uint> GetSizes() => Array2.Empty<uint>();

		/// <inheritdoc/>
		public override IList<int> GetLowerBounds() => Array2.Empty<int>();
	}

	/// <summary>
	/// Base class for modifier type sigs
	/// </summary>
	public abstract class ModifierSig : NonLeafSig {
		readonly ITypeDefOrRef modifier;

		/// <summary>
		/// Returns the modifier type
		/// </summary>
		public ITypeDefOrRef Modifier => modifier;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modifier">Modifier type</param>
		/// <param name="nextSig">The next element type</param>
		protected ModifierSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(nextSig) => this.modifier = modifier;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.CModReqd"/>
	/// </summary>
	public sealed class CModReqdSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.CModReqd;

		/// <inheritdoc/>
		public CModReqdSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.CModOpt"/>
	/// </summary>
	public sealed class CModOptSig : ModifierSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.CModOpt;

		/// <inheritdoc/>
		public CModOptSig(ITypeDefOrRef modifier, TypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Pinned"/>
	/// </summary>
	public sealed class PinnedSig : NonLeafSig {
		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Pinned;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PinnedSig(TypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.ValueArray"/>
	/// </summary>
	public sealed class ValueArraySig : NonLeafSig {
		uint size;

		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.ValueArray;

		/// <summary>
		/// Gets/sets the size
		/// </summary>
		public uint Size {
			get => size;
			set => size = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		/// <param name="size">Size of the array</param>
		public ValueArraySig(TypeSig nextSig, uint size)
			: base(nextSig) => this.size = size;
	}

	/// <summary>
	/// Represents a <see cref="dnlib.DotNet.ElementType.Module"/>
	/// </summary>
	public sealed class ModuleSig : NonLeafSig {
		uint index;

		/// <inheritdoc/>
		public override ElementType ElementType => ElementType.Module;

		/// <summary>
		/// Gets/sets the index
		/// </summary>
		public uint Index {
			get => index;
			set => index = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="nextSig">The next element type</param>
		public ModuleSig(uint index, TypeSig nextSig)
			: base(nextSig) => this.index = index;
	}
}
