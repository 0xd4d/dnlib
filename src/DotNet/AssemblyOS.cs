using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyOS table
	/// </summary>
	public abstract class AssemblyOS : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyOS, rid); }
		}

		/// <summary>
		/// From column AssemblyOS.OSPlatformId
		/// </summary>
		public abstract uint OSPlatformId { get; set; }

		/// <summary>
		/// From column AssemblyOS.OSMajorVersion
		/// </summary>
		public abstract uint OSMajorVersion { get; set; }

		/// <summary>
		/// From column AssemblyOS.OSMinorVersion
		/// </summary>
		public abstract uint OSMinorVersion { get; set; }
	}

	/// <summary>
	/// A AssemblyOS row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyOSUser : AssemblyOS {
		uint osPlatformId;
		uint osMajorVersion;
		uint osMinorVersion;

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

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyOSUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="osPlatformId">OSPlatformId</param>
		/// <param name="osMajorVersion">OSMajorVersion</param>
		/// <param name="osMinorVersion">OSMinorVersion</param>
		public AssemblyOSUser(uint osPlatformId, uint osMajorVersion, uint osMinorVersion) {
			this.osPlatformId = osPlatformId;
			this.osMajorVersion = osMajorVersion;
			this.osMinorVersion = osMinorVersion;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyOS table
	/// </summary>
	sealed class AssemblyOSMD : AssemblyOS {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyOSRow rawRow;

		UserValue<uint> osPlatformId;
		UserValue<uint> osMajorVersion;
		UserValue<uint> osMinorVersion;

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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyOS</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public AssemblyOSMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.AssemblyOS).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("AssemblyOS rid {0} does not exist", rid));
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
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyOSRow(rid);
		}
	}
}
