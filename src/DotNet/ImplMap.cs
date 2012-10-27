using System;
using System.Diagnostics;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ImplMap table
	/// </summary>
	[DebuggerDisplay("{Scope} {Name}")]
	public abstract class ImplMap : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ImplMap, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column ImplMap.MappingFlags
		/// </summary>
		public abstract PInvokeAttributes Flags { get; set; }

		/// <summary>
		/// From column ImplMap.ImportName
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column ImplMap.ImportScope
		/// </summary>
		public abstract ModuleRef Scope { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="PInvokeAttributes.NoMangle"/> bit
		/// </summary>
		public bool IsNoMangle {
			get { return (Flags & PInvokeAttributes.NoMangle) != 0; }
			set {
				if (value)
					Flags |= PInvokeAttributes.NoMangle;
				else
					Flags &= ~PInvokeAttributes.NoMangle;
			}
		}

		/// <summary>
		/// Gets/sets the char set
		/// </summary>
		public PInvokeAttributes CharSet {
			get { return Flags & PInvokeAttributes.CharSetMask; }
			set { Flags = (Flags & ~PInvokeAttributes.CharSetMask) | (value & PInvokeAttributes.CharSetMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetNotSpec"/> is set
		/// </summary>
		public bool IsCharSetNotSpec {
			get { return (Flags & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetNotSpec; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAnsi"/> is set
		/// </summary>
		public bool IsCharSetAnsi {
			get { return (Flags & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetUnicode"/> is set
		/// </summary>
		public bool IsCharSetUnicode {
			get { return (Flags & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAuto"/> is set
		/// </summary>
		public bool IsCharSetAuto {
			get { return (Flags & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAuto; }
		}

		/// <summary>
		/// Gets/sets best fit
		/// </summary>
		public PInvokeAttributes BestFit {
			get { return Flags & PInvokeAttributes.BestFitMask; }
			set { Flags = (Flags & ~PInvokeAttributes.BestFitMask) | (value & PInvokeAttributes.BestFitMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitUseAssem"/> is set
		/// </summary>
		public bool IsBestFitUseAssem {
			get { return (Flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitUseAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitEnabled"/> is set
		/// </summary>
		public bool IsBestFitEnabled {
			get { return (Flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitDisabled"/> is set
		/// </summary>
		public bool IsBestFitDisabled {
			get { return (Flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitDisabled; }
		}

		/// <summary>
		/// Gets/sets throw on unmappable char
		/// </summary>
		public PInvokeAttributes ThrowOnUnmappableChar {
			get { return Flags & PInvokeAttributes.ThrowOnUnmappableCharMask; }
			set { Flags = (Flags & ~PInvokeAttributes.ThrowOnUnmappableCharMask) | (value & PInvokeAttributes.ThrowOnUnmappableCharMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharUseAssem"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharUseAssem {
			get { return (Flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharUseAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharEnabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharEnabled {
			get { return (Flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharDisabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharDisabled {
			get { return (Flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharDisabled; }
		}

		/// <summary>
		/// Gets/sets the <see cref="PInvokeAttributes.SupportsLastError"/> bit
		/// </summary>
		public bool SupportsLastError {
			get { return (Flags & PInvokeAttributes.SupportsLastError) != 0; }
			set {
				if (value)
					Flags |= PInvokeAttributes.SupportsLastError;
				else
					Flags &= ~PInvokeAttributes.SupportsLastError;
			}
		}

		/// <summary>
		/// Gets/sets calling convention
		/// </summary>
		public PInvokeAttributes CallConv {
			get { return Flags & PInvokeAttributes.CallConvMask; }
			set { Flags = (Flags & ~PInvokeAttributes.CallConvMask) | (value & PInvokeAttributes.CallConvMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvWinapi"/> is set
		/// </summary>
		public bool IsCallConvWinapi {
			get { return (Flags & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvWinapi; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvCdecl"/> is set
		/// </summary>
		public bool IsCallConvCdecl {
			get { return (Flags & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvCdecl; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvStdcall"/> is set
		/// </summary>
		public bool IsCallConvStdcall {
			get { return (Flags & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvStdcall; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvThiscall"/> is set
		/// </summary>
		public bool IsCallConvThiscall {
			get { return (Flags & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvThiscall; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvFastcall"/> is set
		/// </summary>
		public bool IsCallConvFastcall {
			get { return (Flags & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvFastcall; }
		}
	}

	/// <summary>
	/// An ImplMap row created by the user and not present in the original .NET file
	/// </summary>
	public class ImplMapUser : ImplMap {
		PInvokeAttributes flags;
		UTF8String name;
		ModuleRef scope;

		/// <summary>
		/// From column ImplMap.MappingFlags
		/// </summary>
		public override PInvokeAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// From column ImplMap.ImportName
		/// </summary>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// From column ImplMap.ImportScope
		/// </summary>
		public override ModuleRef Scope {
			get { return scope; }
			set { scope = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ImplMapUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <param name="name">Name</param>
		/// <param name="flags">Flags</param>
		public ImplMapUser(ModuleRef scope, UTF8String name, PInvokeAttributes flags) {
			this.scope = scope;
			this.name = name;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <param name="name">Name</param>
		/// <param name="flags">Flags</param>
		public ImplMapUser(ModuleRef scope, string name, PInvokeAttributes flags)
			: this(scope, new UTF8String(name), flags) {
		}
	}

	/// <summary>
	/// Created from a row in the ImplMap table
	/// </summary>
	sealed class ImplMapMD : ImplMap {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawImplMapRow rawRow;

		UserValue<PInvokeAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<ModuleRef> scope;

		/// <summary>
		/// From column ImplMap.MappingFlags
		/// </summary>
		public override PInvokeAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <summary>
		/// From column ImplMap.ImportName
		/// </summary>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <summary>
		/// From column ImplMap.ImportScope
		/// </summary>
		public override ModuleRef Scope {
			get { return scope.Value; }
			set { scope.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ImplMap</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ImplMapMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.ImplMap).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ImplMap rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (PInvokeAttributes)rawRow.MappingFlags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.ImportName);
			};
			scope.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveModuleRef(rawRow.ImportScope);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadImplMapRow(rid);
		}
	}
}
