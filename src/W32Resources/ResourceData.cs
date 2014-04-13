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
using System.IO;
using System.Threading;
using dnlib.IO;

namespace dnlib.W32Resources {
	/// <summary>
	/// A resource blob
	/// </summary>
	public sealed class ResourceData : ResourceDirectoryEntry, IDisposable {
		IBinaryReader reader;
		uint codePage;
		uint reserved;

		/// <summary>
		/// Gets/sets the data reader. This instance owns the reader.
		/// </summary>
		public IBinaryReader Data {
			get { return reader; }
			set {
				var oldValue = Interlocked.Exchange(ref reader, value);
				if (oldValue != value && oldValue != null)
					oldValue.Dispose();
			}
		}

		/// <summary>
		/// Gets/sets the code page
		/// </summary>
		public uint CodePage {
			get { return codePage; }
			set { codePage = value; }
		}

		/// <summary>
		/// Gets/sets the reserved field
		/// </summary>
		public uint Reserved {
			get { return reserved; }
			set { reserved = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceData(ResourceName name)
			: base(name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Raw data. This instance owns this reader.</param>
		/// <param name="name">Name</param>
		public ResourceData(ResourceName name, IBinaryReader reader)
			: base(name) {
			this.reader = reader;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Raw data. This instance owns this reader.</param>
		/// <param name="name">Name</param>
		/// <param name="codePage">Code page</param>
		/// <param name="reserved">Reserved value</param>
		public ResourceData(ResourceName name, IBinaryReader reader, uint codePage, uint reserved)
			: base(name) {
			this.reader = reader;
			this.codePage = codePage;
			this.reserved = reserved;
		}

		/// <summary>
		/// Gets the data as a <see cref="Stream"/>. It shares the file position with <see cref="Data"/>
		/// </summary>
		public Stream ToDataStream() {
			return Data.CreateStream();
		}

		/// <inheritdoc/>
		public void Dispose() {
			var oldValue = Interlocked.Exchange(ref reader, null);
			if (oldValue != null)
				oldValue.Dispose();
		}
	}
}
