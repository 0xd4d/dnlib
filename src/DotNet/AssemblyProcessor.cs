using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyProcessor table
	/// </summary>
	public abstract class AssemblyProcessor : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyProcessor, rid); }
		}

		/// <summary>
		/// From column AssemblyProcessor.Processor
		/// </summary>
		public abstract uint Processor { get; set; }
	}

	/// <summary>
	/// A AssemblyProcessor row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyProcessorUser : AssemblyProcessor {
		uint processor;

		/// <inheritdoc/>
		public override uint Processor {
			get { return processor; }
			set { processor = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyProcessorUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processor">Processor</param>
		public AssemblyProcessorUser(uint processor) {
			this.processor = processor;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyProcessor table
	/// </summary>
	sealed class AssemblyProcessorMD : AssemblyProcessor {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyProcessorRow rawRow;

		UserValue<uint> processor;

		/// <inheritdoc/>
		public override uint Processor {
			get { return processor.Value; }
			set { processor.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyProcessor</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyProcessorMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.AssemblyProcessor).Rows < rid)
				throw new BadImageFormatException(string.Format("AssemblyProcessor rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			processor.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Processor;
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyProcessorRow(rid);
		}
	}
}
