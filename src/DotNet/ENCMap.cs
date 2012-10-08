using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ENCMap table
	/// </summary>
	public abstract class ENCMap : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ENCMap, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column ENCMap.Token
		/// </summary>
		public abstract uint Token { get; set; }
	}

	/// <summary>
	/// An ENCMap row created by the user and not present in the original .NET file
	/// </summary>
	public class ENCMapUser : ENCMap {
		uint token;

		/// <inheritdoc/>
		public override uint Token {
			get { return token; }
			set { token = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ENCMapUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Token</param>
		public ENCMapUser(uint token) {
			this.token = token;
		}
	}

	/// <summary>
	/// Created from a row in the ENCMap table
	/// </summary>
	sealed class ENCMapMD : ENCMap {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawENCMapRow rawRow;

		UserValue<uint> token;

		/// <inheritdoc/>
		public override uint Token {
			get { return token.Value; }
			set { token.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ENCMap</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ENCMapMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.ENCMap).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ENCMap rid {0} does not exist", rid));
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
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadENCMapRow(rid);
		}
	}
}
