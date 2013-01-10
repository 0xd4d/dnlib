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
using System.Diagnostics;
using dnlib.PE;
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// All native vtables
	/// </summary>
	[DebuggerDisplay("RVA = {RVA}, Count = {VTables.Count}")]
	public sealed class VTableFixups : IEnumerable<VTable> {
		RVA rva;
		List<VTable> vtables;

		/// <summary>
		/// Gets/sets the RVA of the vtable fixups
		/// </summary>
		public RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <summary>
		/// Gets all <see cref="VTable"/>s
		/// </summary>
		public List<VTable> VTables {
			get { return vtables; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public VTableFixups() {
			this.vtables = new List<VTable>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public VTableFixups(ModuleDefMD module) {
			Initialize(module);
		}

		void Initialize(ModuleDefMD module) {
			var info = module.MetaData.ImageCor20Header.VTableFixups;
			if (info.VirtualAddress == 0 || info.Size == 0) {
				this.vtables = new List<VTable>();
				return;
			}
			this.rva = info.VirtualAddress;
			this.vtables = new List<VTable>((int)info.Size / 8);

			var peImage = module.MetaData.PEImage;
			using (var reader = peImage.CreateFullStream()) {
				reader.Position = (long)peImage.ToFileOffset(info.VirtualAddress);
				long endPos = reader.Position + info.Size;
				while (reader.Position + 8 <= endPos && reader.CanRead(8)) {
					RVA tableRva = (RVA)reader.ReadUInt32();
					int numSlots = reader.ReadUInt16();
					var flags = (VTableFlags)reader.ReadUInt16();
					var vtable = new VTable(tableRva, flags, numSlots);
					vtables.Add(vtable);

					var pos = reader.Position;
					reader.Position = (long)peImage.ToFileOffset(tableRva);
					int slotSize = vtable.Is64Bit ? 8 : 4;
					while (numSlots-- > 0 && reader.CanRead(slotSize)) {
						vtable.Methods.Add(module.ResolveToken(reader.ReadUInt32()) as IMethod);
						if (slotSize == 8)
							reader.ReadUInt32();
					}
					reader.Position = pos;
				}
			}
		}

		/// <inheritdoc/>
		public IEnumerator<VTable> GetEnumerator() {
			return vtables.GetEnumerator();
		}

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	/// <summary>
	/// See COR_VTABLE_XXX in CorHdr.h
	/// </summary>
	[Flags]
	public enum VTableFlags : ushort {
		/// <summary>
		/// 32-bit vtable slots
		/// </summary>
		_32Bit				= 0x01,

		/// <summary>
		/// 64-bit vtable slots
		/// </summary>
		_64Bit				= 0x02,

		/// <summary>
		/// Transition from unmanaged code
		/// </summary>
		FromUnmanaged		= 0x04,

		/// <summary>
		/// Also retain app domain
		/// </summary>
		FromUnmanagedRetainAppDomain = 0x08,

		/// <summary>
		/// Call most derived method
		/// </summary>
		CallMostDerived		= 0x10,
	}

	/// <summary>
	/// One VTable accessed by native code
	/// </summary>
	public sealed class VTable : IEnumerable<IMethod> {
		RVA rva;
		VTableFlags flags;
		List<IMethod> methods;

		/// <summary>
		/// Gets/sets the <see cref="RVA"/> of this vtable
		/// </summary>
		public RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <summary>
		/// Gets/sets the flags
		/// </summary>
		public VTableFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// <c>true</c> if each vtable slot is 32 bits in size
		/// </summary>
		public bool Is32Bit {
			get { return (flags & VTableFlags._32Bit) != 0; }
		}

		/// <summary>
		/// <c>true</c> if each vtable slot is 64 bits in size
		/// </summary>
		public bool Is64Bit {
			get { return (flags & VTableFlags._64Bit) != 0; }
		}

		/// <summary>
		/// Gets the vtable methods
		/// </summary>
		public List<IMethod> Methods {
			get { return methods; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public VTable() {
			this.methods = new List<IMethod>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public VTable(VTableFlags flags) {
			this.flags = flags;
			this.methods = new List<IMethod>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of this vtable</param>
		/// <param name="flags">Flgas</param>
		/// <param name="numSlots">Number of methods in vtable</param>
		public VTable(RVA rva, VTableFlags flags, int numSlots) {
			this.rva = rva;
			this.flags = flags;
			this.methods = new List<IMethod>(numSlots);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of this vtable</param>
		/// <param name="flags">Flgas</param>
		/// <param name="methods">Vtable methods</param>
		public VTable(RVA rva, VTableFlags flags, IEnumerable<IMethod> methods) {
			this.rva = rva;
			this.flags = flags;
			this.methods = new List<IMethod>(methods);
		}

		/// <inheritdoc/>
		public IEnumerator<IMethod> GetEnumerator() {
			return methods.GetEnumerator();
		}

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <inheritdoc/>
		public override string ToString() {
			if (methods.Count == 0)
				return string.Format("{0} {1:X8}", methods.Count, (uint)rva);
			return string.Format("{0} {1:X8} {2}", methods.Count, (uint)rva, methods[0]);
		}
	}
}
