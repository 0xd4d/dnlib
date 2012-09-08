using System;
using dot10.dotNET.MD;

namespace dot10.dotNET {
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
			get { return TypeSig == null ? string.Empty : TypeSig.Name; }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return TypeSig == null ? string.Empty : TypeSig.ReflectionName; }
		}

		/// <inheritdoc/>
		string IFullName.Namespace {
			get { return TypeSig == null ? string.Empty : TypeSig.Namespace; }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return TypeSig == null ? string.Empty : TypeSig.ReflectionNamespace; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return TypeSig == null ? string.Empty : TypeSig.FullName; }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return TypeSig == null ? string.Empty : TypeSig.ReflectionFullName; }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}

		/// <summary>
		/// From column TypeSpec.Signature
		/// </summary>
		public abstract ITypeSig TypeSig { get; set; }
	}

	/// <summary>
	/// A TypeSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeSpecUser : TypeSpec {
		ITypeSig typeSig;

		/// <inheritdoc/>
		public override ITypeSig TypeSig {
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
		public TypeSpecUser(ITypeSig typeSig) {
			this.typeSig = typeSig;
		}
	}

	/// <summary>
	/// Created from a row in the TypeSpec table
	/// </summary>
	sealed class TypeSpecMD : TypeSpec {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeSpecRow rawRow;

		UserValue<ITypeSig> typeSig;

		/// <inheritdoc/>
		public override ITypeSig TypeSig {
			get { return typeSig.Value; }
			set { typeSig.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public TypeSpecMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.TypeSpec).Rows < rid)
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
