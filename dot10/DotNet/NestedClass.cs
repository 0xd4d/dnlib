using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the NestedClass table
	/// </summary>
	public abstract class NestedClass : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.NestedClass, rid); }
		}

		/// <summary>
		/// From column NestedClass.NestedClass
		/// </summary>
		public abstract TypeDef NestedType { get; set; }

		/// <summary>
		/// From column NestedClass.EnclosingClass
		/// </summary>
		public abstract TypeDef EnclosingType { get; set; }
	}

	/// <summary>
	/// A NestedClass row created by the user and not present in the original .NET file
	/// </summary>
	public class NestedClassUser : NestedClass {
		TypeDef nestedType;
		TypeDef enclosingType;

		/// <inheritdoc/>
		public override TypeDef NestedType {
			get { return nestedType; }
			set { nestedType = value; }
		}

		/// <inheritdoc/>
		public override TypeDef EnclosingType {
			get { return enclosingType; }
			set { enclosingType = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public NestedClassUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nestedClass">Nested type</param>
		/// <param name="enclosingClass">Enclosing type</param>
		public NestedClassUser(TypeDef nestedClass, TypeDef enclosingClass) {
			this.nestedType = nestedClass;
			this.enclosingType = enclosingClass;
		}
	}

	/// <summary>
	/// Created from a row in the NestedClass table
	/// </summary>
	sealed class NestedClassMD : NestedClass {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawNestedClassRow rawRow;

		UserValue<TypeDef> nestedType;
		UserValue<TypeDef> enclosingType;

		/// <inheritdoc/>
		public override TypeDef NestedType {
			get { return nestedType.Value; }
			set { nestedType.Value = value; }
		}

		/// <inheritdoc/>
		public override TypeDef EnclosingType {
			get { return enclosingType.Value; }
			set { enclosingType.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>NestedClass</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public NestedClassMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.NestedClass).Rows < rid)
				throw new BadImageFormatException(string.Format("NestedClass rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			nestedType.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDef(rawRow.NestedClass);
			};
			enclosingType.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDef(rawRow.EnclosingClass);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadNestedClassRow(rid);
		}
	}
}
