using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ENCLog table
	/// </summary>
	public abstract class ENCLog : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ENCLog, rid); }
		}

		/// <summary>
		/// From column ENCLog.Token
		/// </summary>
		public abstract uint Token { get; set; }

		/// <summary>
		/// From column ENCLog.FuncCode
		/// </summary>
		public abstract uint FuncCode { get; set; }
	}

	/// <summary>
	/// A ENCLog row created by the user and not present in the original .NET file
	/// </summary>
	public class ENCLogUser : ENCLog {
		uint token;
		uint funcCode;

		/// <inheritdoc/>
		public override uint Token {
			get { return token; }
			set { token = value; }
		}

		/// <inheritdoc/>
		public override uint FuncCode {
			get { return funcCode; }
			set { funcCode = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ENCLogUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Token</param>
		/// <param name="funcCode">FuncCode</param>
		public ENCLogUser(uint token, uint funcCode) {
			this.token = token;
			this.funcCode = funcCode;
		}
	}

	/// <summary>
	/// Created from a row in the ENCLog table
	/// </summary>
	sealed class ENCLogMD : ENCLog {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawENCLogRow rawRow;

		UserValue<uint> token;
		UserValue<uint> funcCode;

		/// <inheritdoc/>
		public override uint Token {
			get { return token.Value; }
			set { token.Value = value; }
		}

		/// <inheritdoc/>
		public override uint FuncCode {
			get { return funcCode.Value; }
			set { funcCode.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ENCLog</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ENCLogMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.ENCLog).Rows < rid)
				throw new BadImageFormatException(string.Format("ENCLog rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			token.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Token;
			};
			funcCode.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.FuncCode;
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadENCLogRow(rid);
		}
	}
}
