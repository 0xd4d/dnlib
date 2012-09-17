using System.Collections.Generic;
using System.Diagnostics;

namespace dot10.DotNet.Emit {
	/// <summary>
	/// Stores a collection of <see cref="Local"/>
	/// </summary>
	[DebuggerDisplay("Count = {Length}")]
	public class LocalList {
		List<Local> locals;

		/// <summary>
		/// Gets the number of locals
		/// </summary>
		public int Length {
			get { return locals.Count; }
		}

		/// <summary>
		/// Gets the list of locals
		/// </summary>
		public IList<Local> Locals {
			get { return locals; }
		}

		/// <summary>
		/// Gets the N'th local
		/// </summary>
		/// <param name="index">The local index</param>
		public Local this[int index] {
			get { return locals[index]; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LocalList() {
			this.locals = new List<Local>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals that will be owned by this instance</param>
		public LocalList(IEnumerable<Local> locals) {
			this.locals = new List<Local>(locals);
		}
	}

	/// <summary>
	/// A method local
	/// </summary>
	[DebuggerDisplay("{typeSig}")]
	public sealed class Local {
		ITypeSig typeSig;

		/// <summary>
		/// Gets/sets the type of the local
		/// </summary>
		public ITypeSig Type {
			get { return typeSig; }
			set { typeSig = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		public Local(ITypeSig typeSig) {
			this.typeSig = typeSig;
		}
	}
}
