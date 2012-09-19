using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the StandAloneSig table
	/// </summary>
	public abstract class StandAloneSig : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.StandAloneSig, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 11; }
		}

		/// <summary>
		/// From column StandAloneSig.Signature
		/// </summary>
		public abstract CallingConventionSig Signature { get; set; }

		/// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get { return Signature as MethodSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Gets/sets the locals sig
		/// </summary>
		public LocalSig LocalSig {
			get { return Signature as LocalSig; }
			set { Signature = value; }
		}
	}

	/// <summary>
	/// A StandAloneSig row created by the user and not present in the original .NET file
	/// </summary>
	public class StandAloneSigUser : StandAloneSig {
		CallingConventionSig signature;

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature; }
			set { signature = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public StandAloneSigUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localSig">A locals sig</param>
		public StandAloneSigUser(LocalSig localSig) {
			this.signature = localSig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodSig">A method sig</param>
		public StandAloneSigUser(MethodSig methodSig) {
			this.signature = methodSig;
		}
	}

	/// <summary>
	/// Created from a row in the StandAloneSig table
	/// </summary>
	sealed class StandAloneSigMD : StandAloneSig {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawStandAloneSigRow rawRow;

		UserValue<CallingConventionSig> signature;

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature.Value; }
			set { signature.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>StandAloneSig</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public StandAloneSigMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.StandAloneSig).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("StandAloneSig rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			signature.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Signature);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadStandAloneSigRow(rid);
		}
	}
}
