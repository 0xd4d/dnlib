using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeSpec table
	/// </summary>
	public abstract class TypeSpec : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeSpec, rid); }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 13; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		public string Name {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		string IType.Namespace {
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
		public ModuleDef OwnerModule {
			get { return FullNameCreator.OwnerModule(this); }
		}

		/// <summary>
		/// From column TypeSpec.Signature
		/// </summary>
		public abstract TypeSig TypeSig { get; set; }

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeSpecUser : TypeSpec {
		TypeSig typeSig;

		/// <inheritdoc/>
		public override TypeSig TypeSig {
			get { return typeSig; }
			set { typeSig = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeSpecUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">A type sig</param>
		public TypeSpecUser(TypeSig typeSig) {
			this.typeSig = typeSig;
		}
	}

	/// <summary>
	/// Created from a row in the TypeSpec table
	/// </summary>
	sealed class TypeSpecMD : TypeSpec {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeSpecRow rawRow;

		UserValue<TypeSig> typeSig;

		/// <inheritdoc/>
		public override TypeSig TypeSig {
			get { return typeSig.Value; }
			set { typeSig.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeSpecMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.TypeSpec).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("TypeSpec rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			typeSig.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadTypeSignature(rawRow.Signature);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadTypeSpecRow(rid);
		}
	}
}
