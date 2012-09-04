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
}
