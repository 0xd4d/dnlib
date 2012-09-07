namespace dot10.dotNET.Hi {
	/// <summary>
	/// Interface to access a type signature
	/// </summary>
	public interface ITypeSig : ISignature, IFullName {
		/// <summary>
		/// Returns the wrapped element type. Can only be null if it was an invalid sig or
		/// if it's a <see cref="LeafSig"/>
		/// </summary>
		ITypeSig Next { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Returns the last element type in the signature but could be null if we parsed invalid
		/// metadata.
		/// </summary>
		/// <returns>The last element type in the signature</returns>
		public static LeafSig GetLeafSig(this ITypeSig self) {
			while (self != null) {
				var leaf = self as LeafSig;
				if (leaf != null)
					return leaf;
				self = self.Next;
			}
			return null;
		}
	}

	/// <summary>
	/// Base class for element types that are last in a type sig, ie.,
	/// <see cref="TypeDefOrRefSig"/> and <see cref="GenericSig"/>
	/// </summary>
	public abstract class LeafSig : ITypeSig {
		/// <inheritdoc/>
		public abstract ITypeSig Next { get; }

		/// <inheritdoc/>
		public abstract string Name { get; }

		/// <inheritdoc/>
		public abstract string ReflectionName { get; }

		/// <inheritdoc/>
		public abstract string Namespace { get; }

		/// <inheritdoc/>
		public abstract string ReflectionNamespace { get; }

		/// <inheritdoc/>
		public abstract string FullName { get; }

		/// <inheritdoc/>
		public abstract string ReflectionFullName { get; }

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// Wraps a <see cref="ITypeDefOrRef"/>
	/// </summary>
	public sealed class TypeDefOrRefSig : LeafSig {
		const string nullName = "<<<NULL>>>";
		readonly ITypeDefOrRef typeDefOrRef;

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

		/// <inheritdoc/>
		public override string Name {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.Name;
			}
		}

		/// <inheritdoc/>
		public override string ReflectionName {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.ReflectionName;
			}
		}

		/// <inheritdoc/>
		public override string Namespace {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.Namespace;
			}
		}

		/// <inheritdoc/>
		public override string ReflectionNamespace {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.ReflectionNamespace;
			}
		}

		/// <inheritdoc/>
		public override string FullName {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.FullName;
			}
		}

		/// <inheritdoc/>
		public override string ReflectionFullName {
			get {
				if (typeDefOrRef == null)
					return nullName;
				return typeDefOrRef.ReflectionFullName;
			}
		}

		/// <inheritdoc/>
		public override ITypeSig Next {
			get { return null; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefOrRef">A <see cref="TypeRef"/>, <see cref="TypeDef"/> or
		/// a <see cref="TypeSpec"/></param>
		public TypeDefOrRefSig(ITypeDefOrRef typeDefOrRef) {
			this.typeDefOrRef = typeDefOrRef;
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

		/// <inheritdoc/>
		public override string Name {
			get { return GetName(); }
		}

		/// <inheritdoc/>
		public override string ReflectionName {
			get { return GetName(); }
		}

		/// <inheritdoc/>
		public override string Namespace {
			get { return string.Empty; }
		}

		/// <inheritdoc/>
		public override string ReflectionNamespace {
			get { return string.Empty; }
		}

		/// <inheritdoc/>
		public override string FullName {
			get { return GetName(); }
		}

		/// <inheritdoc/>
		public override string ReflectionFullName {
			get { return GetName(); }
		}

		/// <inheritdoc/>
		public override ITypeSig Next {
			get { return null; }
		}

		string GetName() {
			return string.Format("{0}{1}", isTypeVar ? "!" : "!!", number);
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Var"/>
	/// </summary>
	public sealed class VarSig : GenericSig {
		/// <inheritdoc/>
		public VarSig(uint number)
			: base(true, number) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.MVar"/>
	/// </summary>
	public sealed class MVarSig : GenericSig {
		/// <inheritdoc/>
		public MVarSig(uint number)
			: base(false, number) {
		}
	}

	/// <summary>
	/// Base class of non-leaf element types
	/// </summary>
	public abstract class NonLeafSig : ITypeSig {
		const string nullName = "<<<NULL>>>";
		readonly ITypeSig nextSig;

		/// <inheritdoc/>
		public string Name {
			get {
				return GetName(nextSig == null ? nullName : nextSig.Name);
			}
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get {
				return GetReflectionName(nextSig == null ? nullName : nextSig.ReflectionName);
			}
		}

		/// <inheritdoc/>
		public string Namespace {
			get { return nextSig == null ? nullName : nextSig.Namespace; }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return nextSig == null ? nullName : nextSig.ReflectionNamespace; }
		}

		/// <inheritdoc/>
		public string FullName {
			get {
				return GetFullName(nextSig == null ? nullName : nextSig.FullName);
			}
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get {
				return GetReflectionFullName(nextSig == null ? nullName : nextSig.ReflectionFullName);
			}
		}

		/// <inheritdoc/>
		public ITypeSig Next {
			get { return nextSig; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">Next sig</param>
		protected NonLeafSig(ITypeSig nextSig) {
			this.nextSig = nextSig;
		}

		/// <summary>
		/// Returns the name of this type.
		/// </summary>
		/// <param name="name">The element type's <see cref="Name"/></param>
		protected virtual string GetName(string name) {
			return name;
		}

		/// <summary>
		/// Returns the reflection name of this type.
		/// </summary>
		/// <param name="reflectionName">The element type's <see cref="ReflectionName"/></param>
		protected virtual string GetReflectionName(string reflectionName) {
			return reflectionName;
		}

		/// <summary>
		/// Returns the full name of this type.
		/// </summary>
		/// <param name="fullName">The element type's <see cref="FullName"/></param>
		protected virtual string GetFullName(string fullName) {
			return fullName;
		}

		/// <summary>
		/// Returns the reflection name of this type. It can be passed to
		/// <see cref="System.Type.GetType(string)"/> to load the type.
		/// </summary>
		/// <param name="reflectionFullName">The element type's <see cref="ReflectionFullName"/></param>
		protected virtual string GetReflectionFullName(string reflectionFullName) {
			return reflectionFullName;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Ptr"/>
	/// </summary>
	public sealed class PtrSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PtrSig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			return name + '*';
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			return reflectionName + '*';
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			return fullName + '*';
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			return reflectionFullName + '*';
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.ByRef"/>
	/// </summary>
	public sealed class ByRefSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ByRefSig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			return name + '&';
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			return reflectionName + '&';
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			return fullName + '&';
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			return reflectionFullName + '&';
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.ValueType"/>
	/// </summary>
	public sealed class ValueTypeSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ValueTypeSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Class"/>
	/// </summary>
	public sealed class ClassSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ClassSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Array"/>
	/// </summary>
	/// <seealso cref="SZArraySig"/>
	public sealed class ArraySig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public ArraySig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			throw new System.NotImplementedException();	//TODO:
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.GenericInst"/>
	/// </summary>
	public sealed class GenericInstSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public GenericInstSig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			throw new System.NotImplementedException();	//TODO:
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.FnPtr"/>
	/// </summary>
	public sealed class FnPtrSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public FnPtrSig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			throw new System.NotImplementedException();	//TODO:
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			throw new System.NotImplementedException();	//TODO:
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.SZArray"/> (single dimension, zero lower bound array)
	/// </summary>
	/// <seealso cref="ArraySig"/>
	public sealed class SZArraySig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public SZArraySig(ITypeSig nextSig)
			: base(nextSig) {
		}

		/// <inheritdoc/>
		protected override string GetName(string name) {
			return name + "[]";
		}

		/// <inheritdoc/>
		protected override string GetReflectionName(string reflectionName) {
			return reflectionName + "[]";
		}

		/// <inheritdoc/>
		protected override string GetFullName(string fullName) {
			return fullName + "[]";
		}

		/// <inheritdoc/>
		protected override string GetReflectionFullName(string reflectionFullName) {
			return reflectionFullName + "[]";
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
		protected ModifierSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(nextSig) {
			this.modifier = modifier;
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.CModReqd"/>
	/// </summary>
	public sealed class CModReqdSig : ModifierSig {
		/// <inheritdoc/>
		public CModReqdSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.CModOpt"/>
	/// </summary>
	public sealed class CModOptSig : ModifierSig {
		/// <inheritdoc/>
		public CModOptSig(ITypeDefOrRef modifier, ITypeSig nextSig)
			: base(modifier, nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Sentinel"/>
	/// </summary>
	public sealed class SentinelSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public SentinelSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}

	/// <summary>
	/// Represents a <see cref="ElementType.Pinned"/>
	/// </summary>
	public sealed class PinnedSig : NonLeafSig {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nextSig">The next element type</param>
		public PinnedSig(ITypeSig nextSig)
			: base(nextSig) {
		}
	}
}
