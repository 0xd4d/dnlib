using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodImpl table
	/// </summary>
	public abstract class MethodImpl : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodImpl, rid); }
		}

		/// <summary>
		/// From column MethodImpl.Class
		/// </summary>
		public abstract TypeDef Class { get; set; }

		/// <summary>
		/// From column MethodImpl.MethodBody
		/// </summary>
		public abstract IMethodDefOrRef MethodBody { get; set; }

		/// <summary>
		/// From column MethodImpl.MethodDeclaration
		/// </summary>
		public abstract IMethodDefOrRef MethodDeclaration { get; set; }
	}

	/// <summary>
	/// A MethodImpl row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodImplUser : MethodImpl {
		TypeDef @class;
		IMethodDefOrRef methodBody;
		IMethodDefOrRef methodDeclaration;

		/// <inheritdoc/>
		public override TypeDef Class {
			get { return @class; }
			set { @class = value; }
		}

		/// <inheritdoc/>
		public override IMethodDefOrRef MethodBody {
			get { return methodBody; }
			set { methodBody = value; }
		}

		/// <inheritdoc/>
		public override IMethodDefOrRef MethodDeclaration {
			get { return methodDeclaration; }
			set { methodDeclaration = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodImplUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="class">Class</param>
		/// <param name="methodBody">MethodBody</param>
		/// <param name="methodDeclaration">MethodDeclaration</param>
		public MethodImplUser(TypeDef @class, IMethodDefOrRef methodBody, IMethodDefOrRef methodDeclaration) {
			this.@class = @class;
			this.methodBody = methodBody;
			this.methodDeclaration = methodDeclaration;
		}
	}

	/// <summary>
	/// Created from a row in the MethodImpl table
	/// </summary>
	sealed class MethodImplMD : MethodImpl {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodImplRow rawRow;

		UserValue<TypeDef> @class;
		UserValue<IMethodDefOrRef> methodBody;
		UserValue<IMethodDefOrRef> methodDeclaration;

		/// <inheritdoc/>
		public override TypeDef Class {
			get { return @class.Value; }
			set { @class.Value = value; }
		}

		/// <inheritdoc/>
		public override IMethodDefOrRef MethodBody {
			get { return methodBody.Value; }
			set { methodBody.Value = value; }
		}

		/// <inheritdoc/>
		public override IMethodDefOrRef MethodDeclaration {
			get { return methodDeclaration.Value; }
			set { methodDeclaration.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodImpl</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public MethodImplMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.MethodImpl).Rows < rid)
				throw new BadImageFormatException(string.Format("MethodImpl rid {0} does not exist", rid));
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
			methodBody.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveMethodDefOrRef(rawRow.MethodBody);
			};
			methodDeclaration.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveMethodDefOrRef(rawRow.MethodDeclaration);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodImplRow(rid);
		}
	}
}
