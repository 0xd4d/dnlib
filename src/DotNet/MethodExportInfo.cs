// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.DotNet {
	/// <summary>
	/// Contains the name and ordinal of a method that gets exported to unmanaged code.
	/// </summary>
	[DebuggerDisplay("{Ordinal} {Name} {Options}")]
	public sealed class MethodExportInfo {
		MethodExportInfoOptions options;
		ushort? ordinal;
		string name;

		const MethodExportInfoOptions DefaultOptions = MethodExportInfoOptions.FromUnmanaged;

		/// <summary>
		/// Gets the ordinal or null
		/// </summary>
		public ushort? Ordinal {
			get { return ordinal; }
			set { ordinal = value; }
		}

		/// <summary>
		/// Gets the name. If it's null, and <see cref="Ordinal"/> is also null, the name of the method
		/// (<see cref="MethodDef.Name"/>) is used as the exported name.
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets the options
		/// </summary>
		public MethodExportInfoOptions Options {
			get { return options; }
			set { options = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public MethodExportInfo() {
			options = DefaultOptions;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name or null to export by ordinal</param>
		public MethodExportInfo(string name) {
			options = DefaultOptions;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ordinal">Ordinal</param>
		public MethodExportInfo(ushort ordinal) {
			options = DefaultOptions;
			this.ordinal = ordinal;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name or null to export by ordinal</param>
		/// <param name="ordinal">Ordinal or null to export by name</param>
		public MethodExportInfo(string name, ushort? ordinal) {
			options = DefaultOptions;
			this.name = name;
			this.ordinal = ordinal;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name or null to export by ordinal</param>
		/// <param name="ordinal">Ordinal or null to export by name</param>
		/// <param name="options">Options</param>
		public MethodExportInfo(string name, ushort? ordinal, MethodExportInfoOptions options) {
			this.options = options;
			this.name = name;
			this.ordinal = ordinal;
		}
	}

	/// <summary>
	/// Exported method options
	/// </summary>
	public enum MethodExportInfoOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None							= 0,

		/// <summary>
		/// Transition from unmanaged code
		/// </summary>
		FromUnmanaged					= 0x00000001,

		/// <summary>
		/// Also retain app domain
		/// </summary>
		FromUnmanagedRetainAppDomain	= 0x00000002,

		/// <summary>
		/// Call most derived method
		/// </summary>
		CallMostDerived					= 0x00000004,
	}
}
