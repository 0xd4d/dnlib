/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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
using System.Collections.Generic;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #GUID heap
	/// </summary>
	public sealed class GuidHeap : HeapBase {
		List<Guid> guids = new List<Guid>();

		/// <inheritdoc/>
		public override string Name {
			get { return "#GUID"; }
		}

		/// <summary>
		/// Adds a guid to the #GUID heap
		/// </summary>
		/// <param name="guid">The guid</param>
		/// <returns>The index of the guid in the #GUID heap</returns>
		public uint Add(Guid? guid) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #GUID when it's read-only");
			if (guid == null)
				return 0;

			// The number of GUIDs will almost always be 1 so there's no need for a dictionary.
			// The only table that contains GUIDs is the Module table, and it has three GUID
			// columns. Only one of them (Mvid) is normally set and the others are null.
			int index = guids.IndexOf(guid.Value);
			if (index >= 0)
				return (uint)index + 1;

			guids.Add(guid.Value);
			return (uint)guids.Count;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			return (uint)guids.Count * 16;
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			foreach (var guid in guids)
				writer.Write(guid.ToByteArray());
		}
	}
}
