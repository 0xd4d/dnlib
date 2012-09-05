using System;
using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the InterfaceImpl table
	/// </summary>
	[DebuggerDisplay("{Interface}")]
	public abstract class InterfaceImpl : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.InterfaceImpl, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 5; }
		}

		/// <summary>
		/// From column InterfaceImpl.Class
		/// </summary>
		public abstract TypeDef Class { get; set; }

		/// <summary>
		/// From column InterfaceImpl.Interface
		/// </summary>
		public abstract ITypeDefOrRef Interface { get; set; }
	}

	/// <summary>
	/// An InterfaceImpl row created by the user and not present in the original .NET file
	/// </summary>
	public class InterfaceImplUser : InterfaceImpl {
		TypeDef @class;
		ITypeDefOrRef @interface;

		/// <inheritdoc/>
		public override TypeDef Class {
			get { return @class; }
			set { @class = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Interface {
			get { return @interface; }
			set { @interface = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public InterfaceImplUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="class">The type</param>
		/// <param name="interface">The interface <paramref name="class"/> implements</param>
		public InterfaceImplUser(TypeDef @class, ITypeDefOrRef @interface) {
			this.@class = @class;
			this.@interface = @interface;
		}
	}

	/// <summary>
	/// Created from a row in the InterfaceImpl table
	/// </summary>
	sealed class InterfaceImplMD : InterfaceImpl {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawInterfaceImplRow rawRow;

		UserValue<TypeDef> @class;
		UserValue<ITypeDefOrRef> @interface;

		/// <inheritdoc/>
		public override TypeDef Class {
			get { return @class.Value; }
			set { @class.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Interface {
			get { return @interface.Value; }
			set { @interface.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>InterfaceImpl</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public InterfaceImplMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.InterfaceImpl).Rows < rid)
				throw new BadImageFormatException(string.Format("InterfaceImpl rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			@class.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDef(rawRow.Class);
			};
			@interface.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Interface);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadInterfaceImplRow(rid);
		}
	}
}
