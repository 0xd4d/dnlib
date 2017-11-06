// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.Utils;
using dnlib.Threading;
using dnlib.DotNet.Pdb;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// A collection of <see cref="Local"/>s
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public sealed class LocalList : IListListener<Local>, ThreadSafe.IList<Local> {
		readonly LazyList<Local> locals;

		/// <summary>
		/// Gets the number of locals
		/// </summary>
		public int Count {
			get { return locals.Count; }
		}

		/// <summary>
		/// Gets the list of locals
		/// </summary>
		public ThreadSafe.IList<Local> Locals {
			get { return locals; }
		}

		/// <summary>
		/// Gets the N'th local
		/// </summary>
		/// <param name="index">The local index</param>
		public Local this[int index] {
			get { return locals[index]; }
			set { locals[index] = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LocalList() {
			this.locals = new LazyList<Local>(this);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals that will be owned by this instance</param>
		public LocalList(IEnumerable<Local> locals) {
			this.locals = new LazyList<Local>(this);
			foreach (var local in locals.GetSafeEnumerable())
				this.locals.Add(local);
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
		void IListListener<Local>.OnAdd(int index, Local value) {
			value.Index = index;
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnRemove(int index, Local value) {
			value.Index = -1;
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnResize(int index) {
			for (int i = index; i < locals.Count_NoLock(); i++)
				locals.Get_NoLock(i).Index = i;
		}

		/// <inheritdoc/>
		void IListListener<Local>.OnClear() {
			foreach (var local in locals.GetEnumerable_NoLock())
				local.Index = -1;
		}

		/// <inheritdoc/>
		public int IndexOf(Local item) {
			return locals.IndexOf(item);
		}

		/// <inheritdoc/>
		public void Insert(int index, Local item) {
			locals.Insert(index, item);
		}

		/// <inheritdoc/>
		public void RemoveAt(int index) {
			locals.RemoveAt(index);
		}

		void ICollection<Local>.Add(Local item) {
			locals.Add(item);
		}

		/// <inheritdoc/>
		public void Clear() {
			locals.Clear();
		}

		/// <inheritdoc/>
		public bool Contains(Local item) {
			return locals.Contains(item);
		}

		/// <inheritdoc/>
		public void CopyTo(Local[] array, int arrayIndex) {
			locals.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc/>
		public bool IsReadOnly {
			get { return false; }
		}

		/// <inheritdoc/>
		public bool Remove(Local item) {
			return locals.Remove(item);
		}

		/// <inheritdoc/>
		public IEnumerator<Local> GetEnumerator() {
			return locals.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return ((IEnumerable<Local>)this).GetEnumerator();
		}

#if THREAD_SAFE
		/// <inheritdoc/>
		public int IndexOf_NoLock(Local item) {
			return locals.IndexOf_NoLock(item);
		}

		/// <inheritdoc/>
		public void Insert_NoLock(int index, Local item) {
			locals.Insert_NoLock(index, item);
		}

		/// <inheritdoc/>
		public void RemoveAt_NoLock(int index) {
			locals.RemoveAt_NoLock(index);
		}

		/// <inheritdoc/>
		public Local Get_NoLock(int index) {
			return locals.Get_NoLock(index);
		}

		/// <inheritdoc/>
		public void Set_NoLock(int index, Local value) {
			locals.Set_NoLock(index, value);
		}

		/// <inheritdoc/>
		public void Add_NoLock(Local item) {
			locals.Add_NoLock(item);
		}

		/// <inheritdoc/>
		public void Clear_NoLock() {
			locals.Clear_NoLock();
		}

		/// <inheritdoc/>
		public bool Contains_NoLock(Local item) {
			return locals.Contains_NoLock(item);
		}

		/// <inheritdoc/>
		public void CopyTo_NoLock(Local[] array, int arrayIndex) {
			locals.CopyTo_NoLock(array, arrayIndex);
		}

		/// <inheritdoc/>
		public int Count_NoLock {
			get { return locals.Count_NoLock; }
		}

		/// <inheritdoc/>
		public bool IsReadOnly_NoLock {
			get { return locals.IsReadOnly_NoLock; }
		}

		/// <inheritdoc/>
		public bool Remove_NoLock(Local item) {
			return locals.Remove_NoLock(item);
		}

		/// <inheritdoc/>
		public IEnumerator<Local> GetEnumerator_NoLock() {
			return locals.GetEnumerator_NoLock();
		}

		/// <inheritdoc/>
		public TRetType ExecuteLocked<TArgType, TRetType>(TArgType arg, ExecuteLockedDelegate<Local, TArgType, TRetType> handler) {
			return locals.ExecuteLocked<TArgType, TRetType>(arg, (tsList, arg2) => handler(this, arg2));
		}
#endif
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
			get { return typeSig; }
			set { typeSig = value; }
		}

		/// <summary>
		/// Local index
		/// </summary>
		public int Index {
			get { return index; }
			internal set { index = value; }
		}

		/// <summary>
		/// Gets the name. This property is obsolete, use <see cref="PdbLocal"/> to get/set the name stored in the PDB file.
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets the attributes. This property is obsolete, use <see cref="PdbLocal"/> to get/set the attributes stored in the PDB file.
		/// </summary>
		public PdbLocalAttributes Attributes {
			get { return attributes; }
			set { attributes = value; }
		}

		internal void SetName(string name) {
			this.name = name;
		}

		internal void SetAttributes(PdbLocalAttributes attributes) {
			this.attributes = attributes;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		public Local(TypeSig typeSig) {
			this.typeSig = typeSig;
		}

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
				return string.Format("V_{0}", Index);
			return n;
		}
	}
}
