// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using System.Diagnostics;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Stores a list of rids
	/// </summary>
	[DebuggerDisplay("Length = {Length}")]
	public abstract class RidList {
		/// <summary>
		/// The empty <see cref="RidList"/>
		/// </summary>
		public static readonly RidList Empty = new ContiguousRidList(0, 0);

		/// <summary>
		/// Gets the number of rids it will iterate over (UInt32)
		/// </summary>
		public abstract uint Length { get; }

		/// <summary>
		/// Gets the number of rids it will iterate over (Int32)
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets the <paramref name="index"/>'th rid
		/// </summary>
		/// <param name="index">Index. Must be &lt; <see cref="Length"/></param>
		/// <returns>A rid or 0 if <paramref name="index"/> is invalid</returns>
		public abstract uint this[uint index] { get; }

		/// <summary>
		/// Gets the <paramref name="index"/>'th rid
		/// </summary>
		/// <param name="index">Index. Must be &lt; <see cref="Length"/></param>
		/// <returns>A rid or 0 if <paramref name="index"/> is invalid</returns>
		public abstract uint this[int index] { get; }
	}

	/// <summary>
	/// A <see cref="RidList"/> where the rids are contiguous
	/// </summary>
	sealed class ContiguousRidList : RidList {
		readonly uint startRid;
		readonly uint length;

		/// <summary>
		/// Gets the start rid
		/// </summary>
		public uint StartRID {
			get { return startRid; }
		}

		/// <inheritdoc/>
		public override uint Length {
			get { return length; }
		}

		/// <inheritdoc/>
		public override int Count {
			get { return (int)length; }
		}

		/// <inheritdoc/>
		public override uint this[uint index] {
			get {
				if (index >= length)
					return 0;
				return startRid + index;
			}
		}

		/// <inheritdoc/>
		public override uint this[int index] {
			get { return this[(uint)index]; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startRid">First rid to return</param>
		/// <param name="length">Number of rids to return</param>
		public ContiguousRidList(uint startRid, uint length) {
			this.startRid = startRid;
			this.length = length;
		}
	}

	/// <summary>
	/// A <see cref="RidList"/> where the returned rids aren't necessarily contiguous.
	/// This should be used if eg. the pointer tables are present.
	/// </summary>
	sealed class RandomRidList : RidList {
		readonly IList<uint> indexToRid;

		/// <inheritdoc/>
		public override uint Length {
			get { return (uint)indexToRid.Count; }
		}

		/// <inheritdoc/>
		public override int Count {
			get { return indexToRid.Count; }
		}

		/// <inheritdoc/>
		public override uint this[uint index] {
			get {
				if (index >= (uint)indexToRid.Count)
					return 0;
				return indexToRid[(int)index];
			}
		}

		/// <inheritdoc/>
		public override uint this[int index] {
			get { return this[(uint)index]; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public RandomRidList() {
			this.indexToRid = new List<uint>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Approximate number of rids that will be returned</param>
		public RandomRidList(int capacity) {
			this.indexToRid = new List<uint>(capacity);
		}

		/// <summary>
		/// Add a new rid that should be returned
		/// </summary>
		/// <param name="rid">The rid</param>
		public void Add(uint rid) {
			indexToRid.Add(rid);
		}
	}
}
