using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ParamPtr table
	/// </summary>
	public abstract class ParamPtr : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ParamPtr, rid); }
		}

		/// <summary>
		/// From column ParamPtr.Param
		/// </summary>
		public abstract uint Param { get; set; }
	}

	/// <summary>
	/// A ParamPtr row created by the user and not present in the original .NET file
	/// </summary>
	public class ParamPtrUser : ParamPtr {
		uint param;

		/// <inheritdoc/>
		public override uint Param {
			get { return param; }
			set { param = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ParamPtrUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="param">Param rid</param>
		public ParamPtrUser(uint param) {
			this.param = param;
		}
	}

	/// <summary>
	/// Created from a row in the ParamPtr table
	/// </summary>
	sealed class ParamPtrMD : ParamPtr {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawParamPtrRow rawRow;

		UserValue<uint> param;

		/// <inheritdoc/>
		public override uint Param {
			get { return param.Value; }
			set { param.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ParamPtr</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ParamPtrMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.ParamPtr).Rows < rid)
				throw new BadImageFormatException(string.Format("ParamPtr rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			param.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Param;
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadParamPtrRow(rid);
		}
	}
}
