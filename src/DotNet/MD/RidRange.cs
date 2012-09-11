using System.Collections.Generic;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Iterates over a range of rids
	/// </summary>
	public abstract class RidRange {
		/// <summary>
		/// Gets the number of rids it will iterate over
		/// </summary>
		public abstract uint Length { get; }

		/// <summary>
		/// Gets the <paramref name="index"/>'th rid
		/// </summary>
		/// <param name="index">Index. Must be &lt; <see cref="Length"/></param>
		/// <returns>A rid or 0 if <paramref name="index"/> is invalid</returns>
		public abstract uint this[uint index] { get; }
	}

	/// <summary>
	/// A <see cref="RidRange"/> where the rid range is contiguous
	/// </summary>
	class ContiguousRidRange : RidRange {
		/// <summary>
		/// The empty <see cref="RidRange"/>
		/// </summary>
		public static readonly ContiguousRidRange Empty = new ContiguousRidRange(0, 0);

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
		public override uint this[uint index] {
			get {
				if (index >= length)
					return 0;
				return startRid + index;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startRid">First rid to return</param>
		/// <param name="length">Number of rids to return</param>
		public ContiguousRidRange(uint startRid, uint length) {
			this.startRid = startRid;
			this.length = length;
		}
	}

	/// <summary>
	/// A <see cref="RidRange"/> where the returned rids aren't necessarily contiguous.
	/// This should be used if eg. the pointer tables are present.
	/// </summary>
	class RandomRidRange : RidRange {
		readonly IList<uint> indexToRid;

		/// <inheritdoc/>
		public override uint Length {
			get { return (uint)indexToRid.Count; }
		}

		/// <inheritdoc/>
		public override uint this[uint index] {
			get {
				if (index >= (uint)indexToRid.Count)
					return 0;
				return indexToRid[(int)index];
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public RandomRidRange() {
			this.indexToRid = new List<uint>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Approximate number of rids that will be returned</param>
		public RandomRidRange(int capacity) {
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
