// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.PE;

namespace dnlib.DotNet {
	/// <summary>
	/// All native vtables
	/// </summary>
	[DebuggerDisplay("RVA = {RVA}, Count = {VTables.Count}")]
	public sealed class VTableFixups : IEnumerable<VTable> {
		RVA rva;
		IList<VTable> vtables;

		/// <summary>
		/// Gets/sets the RVA of the vtable fixups
		/// </summary>
		public RVA RVA {
			get => rva;
			set => rva = value;
		}

		/// <summary>
		/// Gets all <see cref="VTable"/>s
		/// </summary>
		public IList<VTable> VTables => vtables;

		/// <summary>
		/// Default constructor
		/// </summary>
		public VTableFixups() => vtables = new List<VTable>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public VTableFixups(ModuleDefMD module) => Initialize(module);

		void Initialize(ModuleDefMD module) {
			var info = module.Metadata.ImageCor20Header.VTableFixups;
			if (info.VirtualAddress == 0 || info.Size == 0) {
				vtables = new List<VTable>();
				return;
			}
			rva = info.VirtualAddress;
			vtables = new List<VTable>((int)info.Size / 8);

			var peImage = module.Metadata.PEImage;
			var reader = peImage.CreateReader();
			reader.Position = (uint)peImage.ToFileOffset(info.VirtualAddress);
			ulong endPos = (ulong)reader.Position + info.Size;
			while ((ulong)reader.Position + 8 <= endPos && reader.CanRead(8U)) {
				var tableRva = (RVA)reader.ReadUInt32();
				int numSlots = reader.ReadUInt16();
				var flags = (VTableFlags)reader.ReadUInt16();
				var vtable = new VTable(tableRva, flags, numSlots);
				vtables.Add(vtable);

				var pos = reader.Position;
				reader.Position = (uint)peImage.ToFileOffset(tableRva);
				uint slotSize = vtable.Is64Bit ? 8U : 4;
				while (numSlots-- > 0 && reader.CanRead(slotSize)) {
					vtable.Methods.Add(module.ResolveToken(reader.ReadUInt32()) as IMethod);
					if (slotSize == 8)
						reader.ReadUInt32();
				}
				reader.Position = pos;
			}
		}

		/// <inheritdoc/>
		public IEnumerator<VTable> GetEnumerator() => vtables.GetEnumerator();

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// See COR_VTABLE_XXX in CorHdr.h
	/// </summary>
	[Flags]
	public enum VTableFlags : ushort {
		/// <summary>
		/// 32-bit vtable slots
		/// </summary>
		Bit32				= 0x01,

		/// <summary>
		/// 64-bit vtable slots
		/// </summary>
		Bit64				= 0x02,

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
		readonly IList<IMethod> methods;

		/// <summary>
		/// Gets/sets the <see cref="RVA"/> of this vtable
		/// </summary>
		public RVA RVA {
			get => rva;
			set => rva = value;
		}

		/// <summary>
		/// Gets/sets the flags
		/// </summary>
		public VTableFlags Flags {
			get => flags;
			set => flags = value;
		}

		/// <summary>
		/// <c>true</c> if each vtable slot is 32 bits in size
		/// </summary>
		public bool Is32Bit => (flags & VTableFlags.Bit32) != 0;

		/// <summary>
		/// <c>true</c> if each vtable slot is 64 bits in size
		/// </summary>
		public bool Is64Bit => (flags & VTableFlags.Bit64) != 0;

		/// <summary>
		/// Gets the vtable methods
		/// </summary>
		public IList<IMethod> Methods => methods;

		/// <summary>
		/// Default constructor
		/// </summary>
		public VTable() => methods = new List<IMethod>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public VTable(VTableFlags flags) {
			this.flags = flags;
			methods = new List<IMethod>();
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
			methods = new List<IMethod>(numSlots);
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
		public IEnumerator<IMethod> GetEnumerator() => methods.GetEnumerator();

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc/>
		public override string ToString() {
			if (methods.Count == 0)
				return $"{methods.Count} {(uint)rva:X8}";
			return $"{methods.Count} {(uint)rva:X8} {methods[0]}";
		}
	}
}
