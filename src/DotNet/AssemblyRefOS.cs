using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRefOS table
	/// </summary>
	public abstract class AssemblyRefOS : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyRefOS, rid); }
		}

		/// <summary>
		/// From column AssemblyRefOS.OSPlatformId
		/// </summary>
		public abstract uint OSPlatformId { get; set; }

		/// <summary>
		/// From column AssemblyRefOS.OSMajorVersion
		/// </summary>
		public abstract uint OSMajorVersion { get; set; }

		/// <summary>
		/// From column AssemblyRefOS.OSMinorVersion
		/// </summary>
		public abstract uint OSMinorVersion { get; set; }

		/// <summary>
		/// From column AssemblyRefOS.AssemblyRef
		/// </summary>
		public abstract AssemblyRef AssemblyRef { get; set; }
	}

	/// <summary>
	/// A AssemblyRefOS row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyRefOSUser : AssemblyRefOS {
		uint osPlatformId;
		uint osMajorVersion;
		uint osMinorVersion;
		AssemblyRef assemblyRef;

		/// <inheritdoc/>
		public override uint OSPlatformId {
			get { return osPlatformId; }
			set { osPlatformId = value; }
		}

		/// <inheritdoc/>
		public override uint OSMajorVersion {
			get { return osMajorVersion; }
			set { osMajorVersion = value; }
		}

		/// <inheritdoc/>
		public override uint OSMinorVersion {
			get { return osMinorVersion; }
			set { osMinorVersion = value; }
		}

		/// <inheritdoc/>
		public override AssemblyRef AssemblyRef {
			get { return assemblyRef; }
			set { assemblyRef = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyRefOSUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="osPlatformId">OSPlatformId</param>
		/// <param name="osMajorVersion">OSMajorVersion</param>
		/// <param name="osMinorVersion">OSMinorVersion</param>
		/// <param name="assemblyRef">AssemblyRef</param>
		public AssemblyRefOSUser(uint osPlatformId, uint osMajorVersion, uint osMinorVersion, AssemblyRef assemblyRef) {
			this.osPlatformId = osPlatformId;
			this.osMajorVersion = osMajorVersion;
			this.osMinorVersion = osMinorVersion;
			this.assemblyRef = assemblyRef;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyRefOS table
	/// </summary>
	sealed class AssemblyRefOSMD : AssemblyRefOS {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRefOSRow rawRow;

		UserValue<uint> osPlatformId;
		UserValue<uint> osMajorVersion;
		UserValue<uint> osMinorVersion;
		UserValue<AssemblyRef> assemblyRef;

		/// <inheritdoc/>
		public override uint OSPlatformId {
			get { return osPlatformId.Value; }
			set { osPlatformId.Value = value; }
		}

		/// <inheritdoc/>
		public override uint OSMajorVersion {
			get { return osMajorVersion.Value; }
			set { osMajorVersion.Value = value; }
		}

		/// <inheritdoc/>
		public override uint OSMinorVersion {
			get { return osMinorVersion.Value; }
			set { osMinorVersion.Value = value; }
		}

		/// <inheritdoc/>
		public override AssemblyRef AssemblyRef {
			get { return assemblyRef.Value; }
			set { assemblyRef.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyRefOS</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public AssemblyRefOSMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.AssemblyRefOS).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("AssemblyRefOS rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			osPlatformId.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.OSPlatformId;
			};
			osMajorVersion.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.OSMajorVersion;
			};
			osMinorVersion.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.OSMinorVersion;
			};
			assemblyRef.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveAssemblyRef(rawRow.AssemblyRef);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRefOSRow(rid);
		}
	}
}
