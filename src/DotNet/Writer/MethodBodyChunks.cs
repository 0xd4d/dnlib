// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores all method body chunks
	/// </summary>
	public sealed class MethodBodyChunks : IChunk {
		const uint FAT_BODY_ALIGNMENT = 4;
		Dictionary<MethodBody, MethodBody> tinyMethodsDict;
		Dictionary<MethodBody, MethodBody> fatMethodsDict;
		readonly List<MethodBody> tinyMethods;
		readonly List<MethodBody> fatMethods;
		readonly List<ReusedMethodInfo> reusedMethods;
		readonly Dictionary<uint, MethodBody> rvaToReusedMethod;
		readonly bool shareBodies;
		FileOffset offset;
		RVA rva;
		uint length;
		bool setOffsetCalled;
		bool overlapsChecked;
		readonly bool alignFatBodies;
		uint savedBytes;

		readonly struct ReusedMethodInfo {
			public readonly MethodBody MethodBody;
			public readonly RVA RVA;
			/// <summary>Number of bytes written at <see cref="RVA"/></summary>
			public readonly uint Length;
			public ReusedMethodInfo(MethodBody methodBody, RVA rva, uint length) {
				MethodBody = methodBody;
				RVA = rva;
				Length = length;
			}
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the number of bytes saved by re-using method bodies
		/// </summary>
		public uint SavedBytes => savedBytes;

		internal bool CanReuseOldBodyLocation { get; set; }
		/// <summary>
		/// Checks if a body with this RVA and size can be overwritten without touching other data.
		/// Must be set if <see cref="CanReuseOldBodyLocation"/> is <c>true</c>.
		/// </summary>
		internal Func<RVA, uint, bool> IsReusableLocation { get; set; }
		internal bool ReusedAllMethodBodyLocations => tinyMethods.Count == 0 && fatMethods.Count == 0;
		internal bool HasReusedMethods => reusedMethods.Count > 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="shareBodies"><c>true</c> if bodies can be shared</param>
		public MethodBodyChunks(bool shareBodies) {
			this.shareBodies = shareBodies;
			alignFatBodies = true;
			if (shareBodies) {
				tinyMethodsDict = new Dictionary<MethodBody, MethodBody>();
				fatMethodsDict = new Dictionary<MethodBody, MethodBody>();
			}
			tinyMethods = new List<MethodBody>();
			fatMethods = new List<MethodBody>();
			reusedMethods = new List<ReusedMethodInfo>();
			rvaToReusedMethod = new Dictionary<uint, MethodBody>();
		}

		/// <summary>
		/// Adds a <see cref="MethodBody"/> and returns the one that has been cached
		/// </summary>
		/// <param name="methodBody">The method body</param>
		/// <returns>The cached method body</returns>
		public MethodBody Add(MethodBody methodBody) => Add(methodBody, 0, 0);

		internal MethodBody Add(MethodBody methodBody, RVA origRva, uint origSize) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			// A fat body must be 4-byte aligned or the 64-bit CLR won't read it. The original body could've been
			// a tiny body, which can be at any address.
			bool isAligned = methodBody.IsTiny || ((uint)origRva & (FAT_BODY_ALIGNMENT - 1)) == 0;
			if (CanReuseOldBodyLocation && origRva != 0 && origSize != 0 && isAligned && methodBody.CanReuse(origRva, origSize) && IsReusableLocation(origRva, origSize)) {
				if (rvaToReusedMethod.TryGetValue((uint)origRva, out var reusedMethod)) {
					if (methodBody.Equals(reusedMethod))
						return reusedMethod;
				}
				else {
					rvaToReusedMethod.Add((uint)origRva, methodBody);
					reusedMethods.Add(new ReusedMethodInfo(methodBody, origRva, methodBody.GetLengthAt(origRva)));
					return methodBody;
				}
			}
			if (shareBodies) {
				var dict = methodBody.IsFat ? fatMethodsDict : tinyMethodsDict;
				if (dict.TryGetValue(methodBody, out var cached)) {
					savedBytes += (uint)methodBody.GetApproximateSizeOfMethodBody();
					return cached;
				}
				dict[methodBody] = methodBody;
			}
			var list = methodBody.IsFat ? fatMethods : tinyMethods;
			list.Add(methodBody);
			return methodBody;
		}

		/// <summary>Removes the specified method body from this chunk</summary>
		/// <param name="methodBody">The method body</param>
		/// <returns><see langword="true" /> if the method body is removed</returns>
		public bool Remove(MethodBody methodBody) {
			if (methodBody is null)
				throw new ArgumentNullException(nameof(methodBody));
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			if (CanReuseOldBodyLocation)
				throw new InvalidOperationException("Reusing old body locations is enabled. Can't remove bodies.");

			var list = methodBody.IsFat ? fatMethods : tinyMethods;
			return list.Remove(methodBody);
		}

		/// <summary>
		/// Moves reused bodies whose old location overlaps another reused body's old location to the
		/// new location, so no reused body overwrites another one. The body with the lowest RVA is kept. Must be called after all bodies
		/// have been added and before <see cref="OverlapsReusedBody(RVA, uint)"/> is called.
		/// </summary>
		internal void RemoveOverlappingReusedBodies() {
			if (reusedMethods.Count == 0)
				return;
			// The RVAs are unique (see rvaToReusedMethod) so the order doesn't depend on the sort implementation
			var sorted = reusedMethods.ToArray();
			Array.Sort(sorted, (a, b) => ((uint)a.RVA).CompareTo((uint)b.RVA));
			reusedMethods.Clear();
			ulong prevEnd = 0;
			foreach (var info in sorted) {
				if ((uint)info.RVA < prevEnd) {
					// Each body is only stored once, and all methods using it get its new RVA
					rvaToReusedMethod.Remove((uint)info.RVA);
					var list = info.MethodBody.IsFat ? fatMethods : tinyMethods;
					list.Add(info.MethodBody);
					continue;
				}
				reusedMethods.Add(info);
				prevEnd = (ulong)(uint)info.RVA + info.Length;
			}
			overlapsChecked = true;
		}

		/// <summary>
		/// Checks if a reused body is written to some byte in the range
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="size">Size</param>
		internal bool OverlapsReusedBody(RVA rva, uint size) {
			if (!overlapsChecked && reusedMethods.Count != 0)
				throw new InvalidOperationException();
			// reusedMethods is sorted and the bodies don't overlap, so the ends are sorted too
			ulong start = (uint)rva;
			ulong end = start + size;
			int lo = 0, hi = reusedMethods.Count - 1;
			while (lo <= hi) {
				int mid = lo + (hi - lo) / 2;
				var info = reusedMethods[mid];
				ulong infoStart = (uint)info.RVA;
				ulong infoEnd = infoStart + info.Length;
				if (infoEnd <= start)
					lo = mid + 1;
				else if (infoStart >= end)
					hi = mid - 1;
				else
					return true;
			}
			return false;
		}

		internal void InitializeReusedMethodBodies(Func<RVA, FileOffset> getNewFileOffset) {
			foreach (var info in reusedMethods) {
				var offset = getNewFileOffset(info.RVA);
				info.MethodBody.SetOffset(offset, info.RVA);
			}
		}

		internal void WriteReusedMethodBodies(DataWriter writer, long destStreamBaseOffset) {
			foreach (var info in reusedMethods) {
				Debug.Assert(info.MethodBody.RVA == info.RVA);
				if (info.MethodBody.RVA != info.RVA)
					throw new InvalidOperationException();
				writer.Position = destStreamBaseOffset + (uint)info.MethodBody.FileOffset;
				info.MethodBody.VerifyWriteTo(writer);
			}
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
			this.offset = offset;
			this.rva = rva;

			tinyMethodsDict = null;
			fatMethodsDict = null;

			var rva2 = rva;
			foreach (var mb in tinyMethods) {
				mb.SetOffset(offset, rva2);
				uint len = mb.GetFileLength();
				rva2 += len;
				offset += len;
			}

			foreach (var mb in fatMethods) {
				if (alignFatBodies) {
					uint padding = (uint)rva2.AlignUp(FAT_BODY_ALIGNMENT) - (uint)rva2;
					rva2 += padding;
					offset += padding;
				}
				mb.SetOffset(offset, rva2);
				uint len = mb.GetFileLength();
				rva2 += len;
				offset += len;
			}

			length = (uint)rva2 - (uint)rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public uint CalculateAlignment() => 0;

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			var rva2 = rva;
			foreach (var mb in tinyMethods) {
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetFileLength();
			}

			foreach (var mb in fatMethods) {
				if (alignFatBodies) {
					int padding = (int)rva2.AlignUp(FAT_BODY_ALIGNMENT) - (int)rva2;
					writer.WriteZeroes(padding);
					rva2 += (uint)padding;
				}
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetFileLength();
			}
		}
	}
}
