// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Stores a list of rids
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public readonly struct RidList : IEnumerable<uint> {
		readonly uint startRid;
		readonly uint length;
		readonly IList<uint> rids;

		/// <summary>
		/// Gets the empty instance
		/// </summary>
		public static readonly RidList Empty = Create(0, 0);

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="startRid"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static RidList Create(uint startRid, uint length) => new RidList(startRid, length);

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="rids">List of valid rids</param>
		/// <returns></returns>
		public static RidList Create(IList<uint> rids) => new RidList(rids);

		RidList(uint startRid, uint length) {
			this.startRid = startRid;
			this.length = length;
			rids = null;
		}

		RidList(IList<uint> rids) {
			this.rids = rids ?? throw new ArgumentNullException(nameof(rids));
			startRid = 0;
			length = (uint)rids.Count;
		}

		/// <summary>
		/// Gets the <paramref name="index"/>'th rid
		/// </summary>
		/// <param name="index">Index. Must be &lt; <see cref="Count"/></param>
		/// <returns>A rid or 0 if <paramref name="index"/> is invalid</returns>
		public uint this[int index] {
			get {
				if (!(rids is null)) {
					if ((uint)index >= (uint)rids.Count)
						return 0;
					return rids[index];
				}
				else {
					if ((uint)index >= length)
						return 0;
					return startRid + (uint)index;
				}
			}
		}

		/// <summary>
		/// Gets the number of rids it will iterate over
		/// </summary>
		public int Count => (int)length;

		/// <summary>
		/// Enumerator
		/// </summary>
		public struct Enumerator : IEnumerator<uint> {
			readonly uint startRid;
			readonly uint length;
			readonly IList<uint> rids;
			uint index;
			uint current;

			internal Enumerator(in RidList list) {
				startRid = list.startRid;
				length = list.length;
				rids = list.rids;
				index = 0;
				current = 0;
			}

			/// <summary>
			/// Gets the current rid
			/// </summary>
			public uint Current => current;
			object IEnumerator.Current => current;

			/// <summary>
			/// Disposes this instance
			/// </summary>
			public void Dispose() { }

			/// <summary>
			/// Moves to the next rid
			/// </summary>
			/// <returns></returns>
			public bool MoveNext() {
				if (rids is null && index < length) {
					current = startRid + index;
					index++;
					return true;
				}
				return MoveNextOther();
			}

			bool MoveNextOther() {
				if (index >= length) {
					current = 0;
					return false;
				}
				if (!(rids is null))
					current = rids[(int)index];
				else
					current = startRid + index;
				index++;
				return true;
			}

			void IEnumerator.Reset() => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public Enumerator GetEnumerator() => new Enumerator(this);
		IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
