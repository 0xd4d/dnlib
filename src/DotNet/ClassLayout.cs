// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ClassLayout table
	/// </summary>
	public abstract class ClassLayout : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ClassLayout, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <summary>
		/// From column ClassLayout.PackingSize
		/// </summary>
		public ushort PackingSize {
			get => packingSize;
			set => packingSize = value;
		}
		/// <summary/>
		protected ushort packingSize;

		/// <summary>
		/// From column ClassLayout.ClassSize
		/// </summary>
		public uint ClassSize {
			get => classSize;
			set => classSize = value;
		}
		/// <summary/>
		protected uint classSize;
	}

	/// <summary>
	/// A ClassLayout row created by the user and not present in the original .NET file
	/// </summary>
	public class ClassLayoutUser : ClassLayout {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ClassLayoutUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="packingSize">PackingSize</param>
		/// <param name="classSize">ClassSize</param>
		public ClassLayoutUser(ushort packingSize, uint classSize) {
			this.packingSize = packingSize;
			this.classSize = classSize;
		}
	}

	/// <summary>
	/// Created from a row in the ClassLayout table
	/// </summary>
	sealed class ClassLayoutMD : ClassLayout, IMDTokenProviderMD {
		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ClassLayout</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ClassLayoutMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ClassLayoutTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"ClassLayout rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			bool b = readerModule.TablesStream.TryReadClassLayoutRow(origRid, out var row);
			Debug.Assert(b);
			classSize = row.ClassSize;
			packingSize = row.PackingSize;
		}
	}
}
