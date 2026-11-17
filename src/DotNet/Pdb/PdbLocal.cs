// dnlib: See LICENSE.txt for more info

#nullable enable

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
		public PdbLocal(Local? local, string? name, PdbLocalAttributes attributes) {
			Local = local;
			Name = name;
			Attributes = attributes;
		}

		/// <summary>
		/// Gets/sets the local
		/// </summary>
		public Local? Local { get; set; }

		/// <summary>
		/// Same as <see cref="Local"/> but throws an <see cref="InvalidMetadataException"/> if it's <c>null</c>
		/// </summary>
		public Local LocalOrThrow => OrThrowHelper.NotNull(Local, nameof(Local));

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Gets/sets the attributes
		/// </summary>
		public PdbLocalAttributes Attributes { get; set; }

		/// <summary>
		/// Gets the index of the local
		/// </summary>
		/// <exception cref="InvalidMetadataException"><see cref="Local"/> is <c>null</c></exception>
		public int Index => LocalOrThrow.Index;

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
