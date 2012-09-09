using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodPtr table
	/// </summary>
	public abstract class MethodPtr : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodPtr, rid); }
		}

		/// <summary>
		/// From column MethodPtr.Method
		/// </summary>
		public abstract uint Method { get; set; }
	}

	/// <summary>
	/// A MethodPtr row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodPtrUser : MethodPtr {
		uint method;

		/// <inheritdoc/>
		public override uint Method {
			get { return method; }
			set { method = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodPtrUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method rid</param>
		public MethodPtrUser(uint method) {
			this.method = method;
		}
	}

	/// <summary>
	/// Created from a row in the MethodPtr table
	/// </summary>
	sealed class MethodPtrMD : MethodPtr {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodPtrRow rawRow;

		UserValue<uint> method;

		/// <inheritdoc/>
		public override uint Method {
			get { return method.Value; }
			set { method.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodPtr</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public MethodPtrMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.MethodPtr).Rows < rid)
				throw new BadImageFormatException(string.Format("MethodPtr rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			method.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Method;
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodPtrRow(rid);
		}
	}
}
