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
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Low level access to a .NET file's metadata
	/// </summary>
	public sealed class DotNetFile : IDisposable {
		IMetaData metaData;

		enum MetaDataType {
			Unknown,
			Compressed,	// #~ (normal)
			ENC,		// #- (edit and continue)
		}

		/// <summary>
		/// Returns a <see cref="IMetaData"/>
		/// </summary>
		public IMetaData MetaData {
			get { return metaData; }
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="fileName">The file to load</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(string fileName) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(fileName));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="data">The .NET file data</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(byte[] data) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(data));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IntPtr addr) {
			IPEImage peImage = null;

			// We don't know what layout it is. Memory is more common so try that first.
			try {
				return Load(peImage = new PEImage(addr, ImageLayout.Memory, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				peImage = null;
			}

			try {
				return Load(peImage = new PEImage(addr, ImageLayout.File, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <param name="imageLayout">Image layout of the file in memory</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IntPtr addr, ImageLayout imageLayout) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(addr, imageLayout, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IPEImage peImage) {
			return Load(peImage, true);
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IPEImage peImage, bool verify) {
			IImageStream cor20HeaderStream = null, mdHeaderStream = null;
			MetaData md = null;
			try {
				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				if (dotNetDir.VirtualAddress == 0)
					throw new BadImageFormatException(".NET data directory RVA is 0");
				if (dotNetDir.Size < 0x48)
					throw new BadImageFormatException(".NET data directory size < 0x48");
				var cor20Header = new ImageCor20Header(cor20HeaderStream = peImage.CreateStream(dotNetDir.VirtualAddress, 0x48), verify);
				if (cor20Header.HasNativeHeader)
					throw new BadImageFormatException(".NET native header isn't supported");	//TODO: Fix this
				if (cor20Header.MetaData.VirtualAddress == 0)
					throw new BadImageFormatException(".NET MetaData RVA is 0");
				if (cor20Header.MetaData.Size < 16)
					throw new BadImageFormatException(".NET MetaData size is too small");
				var mdSize = cor20Header.MetaData.Size;
				var mdRva = cor20Header.MetaData.VirtualAddress;
				var mdHeader = new MetaDataHeader(mdHeaderStream = peImage.CreateStream(mdRva, mdSize), verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if (sh.Offset + sh.StreamSize < sh.Offset || sh.Offset + sh.StreamSize > mdSize)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				switch (GetMetaDataType(mdHeader.StreamHeaders)) {
				case MetaDataType.Compressed:
					md = new CompressedMetaData(peImage, cor20Header, mdHeader);
					break;

				case MetaDataType.ENC:
					md = new ENCMetaData(peImage, cor20Header, mdHeader);
					break;

				default:
					throw new BadImageFormatException("No #~ or #- stream found");
				}
				md.Initialize();

				return new DotNetFile(md);
			}
			catch {
				if (md != null)
					md.Dispose();
				throw;
			}
			finally {
				if (cor20HeaderStream != null)
					cor20HeaderStream.Dispose();
				if (mdHeaderStream != null)
					mdHeaderStream.Dispose();
			}
		}

		DotNetFile(IMetaData metaData) {
			this.metaData = metaData;
		}

		static MetaDataType GetMetaDataType(IList<StreamHeader> streamHeaders) {
			foreach (var sh in streamHeaders) {
				if (sh.Name == "#~")
					return MetaDataType.Compressed;
				if (sh.Name == "#-")
					return MetaDataType.ENC;
			}
			return MetaDataType.Unknown;
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (metaData != null)
				metaData.Dispose();
			metaData = null;
		}
	}
}
