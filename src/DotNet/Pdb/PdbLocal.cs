// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A local variable
	/// </summary>
	public sealed class PdbLocal : IHasCustomDebugInformation {
		/// <summary>
		/// Constructor
		/// </summary>
		public PdbLocal() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local"></param>
		/// <param name="name"></param>
		/// <param name="attributes"></param>
		public PdbLocal(Local local, string name, PdbLocalAttributes attributes) {
			Local = local;
			Name = name;
			Attributes = attributes;
		}

		/// <summary>
		/// Gets/sets the local
		/// </summary>
		public Local Local { get; set; }

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets/sets the attributes
		/// </summary>
		public PdbLocalAttributes Attributes { get; set; }

		/// <summary>
		/// Gets the index of the local
		/// </summary>
		public int Index => Local.Index;

		/// <summary>
		/// true if it should be hidden in debugger variables windows. Not all compiler generated locals have this flag set.
		/// </summary>
		public bool IsDebuggerHidden {
			get => (Attributes & PdbLocalAttributes.DebuggerHidden) != 0;
			set {
				if (value)
					Attributes |= PdbLocalAttributes.DebuggerHidden;
				else
					Attributes &= ~PdbLocalAttributes.DebuggerHidden;
			}
		}

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 24;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos => customDebugInfos;
		readonly IList<PdbCustomDebugInfo> customDebugInfos = new List<PdbCustomDebugInfo>();
	}
}
