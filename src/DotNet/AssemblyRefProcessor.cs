using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRefProcessor table
	/// </summary>
	public abstract class AssemblyRefProcessor : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyRefProcessor, rid); }
		}

		/// <summary>
		/// From column AssemblyRefProcessor.Processor
		/// </summary>
		public abstract uint Processor { get; set; }

		/// <summary>
		/// From column AssemblyRefProcessor.AssemblyRef
		/// </summary>
		public abstract AssemblyRef AssemblyRef { get; set; }
	}

	/// <summary>
	/// A AssemblyRefProcessor row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyRefProcessorUser : AssemblyRefProcessor {
		uint processor;
		AssemblyRef assemblyRef;

		/// <inheritdoc/>
		public override uint Processor {
			get { return processor; }
			set { processor = value; }
		}

		/// <inheritdoc/>
		public override AssemblyRef AssemblyRef {
			get { return assemblyRef; }
			set { assemblyRef = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyRefProcessorUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processor">Processor</param>
		/// <param name="assemblyRef">AssemblyRef</param>
		public AssemblyRefProcessorUser(uint processor, AssemblyRef assemblyRef) {
			this.processor = processor;
			this.assemblyRef = assemblyRef;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyRefProcessor table
	/// </summary>
	sealed class AssemblyRefProcessorMD : AssemblyRefProcessor {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRefProcessorRow rawRow;

		UserValue<uint> processor;
		UserValue<AssemblyRef> assemblyRef;

		/// <inheritdoc/>
		public override uint Processor {
			get { return processor.Value; }
			set { processor.Value = value; }
		}

		/// <inheritdoc/>
		public override AssemblyRef AssemblyRef {
			get { return assemblyRef.Value; }
			set { assemblyRef.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyRefProcessor</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyRefProcessorMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.AssemblyRefProcessor).Rows < rid)
				throw new BadImageFormatException(string.Format("AssemblyRefProcessor rid {0} does not exist", rid));
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
			assemblyRef.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveAssemblyRef(rawRow.AssemblyRef);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRefProcessorRow(rid);
		}
	}
}
