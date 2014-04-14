/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

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
		public MDToken MDToken {
			get { return new MDToken(Table.ClassLayout, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column ClassLayout.PackingSize
		/// </summary>
		public abstract ushort PackingSize { get; set; }

		/// <summary>
		/// From column ClassLayout.ClassSize
		/// </summary>
		public abstract uint ClassSize { get; set; }
	}

	/// <summary>
	/// A ClassLayout row created by the user and not present in the original .NET file
	/// </summary>
	public class ClassLayoutUser : ClassLayout {
		ushort packingSize;
		uint classSize;

		/// <inheritdoc/>
		public override ushort PackingSize {
			get { return packingSize; }
			set { packingSize = value; }
		}

		/// <inheritdoc/>
		public override uint ClassSize {
			get { return classSize; }
			set { classSize = value; }
		}

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
	sealed class ClassLayoutMD : ClassLayout {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawClassLayoutRow rawRow;

		UserValue<ushort> packingSize;
		UserValue<uint> classSize;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override ushort PackingSize {
			get { return packingSize.Value; }
			set { packingSize.Value = value; }
		}

		/// <inheritdoc/>
		public override uint ClassSize {
			get { return classSize.Value; }
			set { classSize.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ClassLayout</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ClassLayoutMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ClassLayoutTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ClassLayout rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			packingSize.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return rawRow.PackingSize;
			};
			classSize.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return rawRow.ClassSize;
			};
#if THREAD_SAFE
			packingSize.Lock = theLock;
			classSize.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadClassLayoutRow(rid);
		}
	}
}
