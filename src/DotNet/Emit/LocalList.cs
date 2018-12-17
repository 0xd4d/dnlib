// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.Utils;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// A collection of <see cref="Local"/>s
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(LocalList_CollectionDebugView))]
	public sealed class LocalList : IListListener<Local>, IList<Local> {
		readonly LazyList<Local> locals;

		/// <summary>
		/// Gets the number of locals
		/// </summary>
		public int Count => locals.Count;

		/// <summary>
		/// Gets the list of locals
		/// </summary>
		public IList<Local> Locals => locals;

		/// <summary>
		/// Gets the N'th local
		/// </summary>
		/// <param name="index">The local index</param>
		public Local this[int index] {
			get => locals[index];
			set => locals[index] = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LocalList() => locals = new LazyList<Local>(this);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals that will be owned by this instance</param>
		public LocalList(IList<Local> locals) {
			this.locals = new LazyList<Local>(this);
			for (int i = 0; i < locals.Count; i++)
				this.locals.Add(locals[i]);
		}

		/// <summary>
		/// Adds a new local and then returns it
		/// </summary>
		/// <param name="local">The local that should be added to the list</param>
		/// <returns>The input is always returned</returns>
		public Local Add(Local local) {
			locals.Add(local);
			return local;
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnLazyAdd(int index, ref Local value) {
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnAdd(int index, Local value) => value.Index = index;

		/// <inheritdoc/>
		void IListListener<Local>.OnRemove(int index, Local value) => value.Index = -1;

		/// <inheritdoc/>
		void IListListener<Local>.OnResize(int index) {
			for (int i = index; i < locals.Count_NoLock; i++)
				locals.Get_NoLock(i).Index = i;
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnClear() {
			foreach (var local in locals.GetEnumerable_NoLock())
				local.Index = -1;
		}

		/// <inheritdoc/>
		public int IndexOf(Local item) => locals.IndexOf(item);

		/// <inheritdoc/>
		public void Insert(int index, Local item) => locals.Insert(index, item);

		/// <inheritdoc/>
		public void RemoveAt(int index) => locals.RemoveAt(index);

		void ICollection<Local>.Add(Local item) => locals.Add(item);

		/// <inheritdoc/>
		public void Clear() => locals.Clear();

		/// <inheritdoc/>
		public bool Contains(Local item) => locals.Contains(item);

		/// <inheritdoc/>
		public void CopyTo(Local[] array, int arrayIndex) => locals.CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <inheritdoc/>
		public bool Remove(Local item) => locals.Remove(item);

		/// <inheritdoc/>
		public LazyList<Local>.Enumerator GetEnumerator() => locals.GetEnumerator();
		IEnumerator<Local> IEnumerable<Local>.GetEnumerator() => locals.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((IEnumerable<Local>)this).GetEnumerator();
	}

	/// <summary>
	/// A method local
	/// </summary>
	public sealed class Local : IVariable {
		TypeSig typeSig;
		int index;
		string name;
		PdbLocalAttributes attributes;

		/// <summary>
		/// Gets/sets the type of the local
		/// </summary>
		public TypeSig Type {
			get => typeSig;
			set => typeSig = value;
		}

		/// <summary>
		/// Local index
		/// </summary>
		public int Index {
			get => index;
			internal set => index = value;
		}

		/// <summary>
		/// Gets the name. This property is obsolete, use <see cref="PdbLocal"/> to get/set the name stored in the PDB file.
		/// </summary>
		public string Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Gets the attributes. This property is obsolete, use <see cref="PdbLocal"/> to get/set the attributes stored in the PDB file.
		/// </summary>
		public PdbLocalAttributes Attributes {
			get => attributes;
			set => attributes = value;
		}

		internal void SetName(string name) => this.name = name;
		internal void SetAttributes(PdbLocalAttributes attributes) => this.attributes = attributes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		public Local(TypeSig typeSig) => this.typeSig = typeSig;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		/// <param name="name">Name of local</param>
		public Local(TypeSig typeSig, string name) {
			this.typeSig = typeSig;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		/// <param name="name">Name of local</param>
		/// <param name="index">Index, should only be used if you don't add it to the locals list</param>
		public Local(TypeSig typeSig, string name, int index) {
			this.typeSig = typeSig;
			this.name = name;
			this.index = index;
		}

		/// <inheritdoc/>
		public override string ToString() {
			var n = name;
			if (string.IsNullOrEmpty(n))
				return $"V_{Index}";
			return n;
		}
	}
}
