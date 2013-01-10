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
using System.Diagnostics;
using dnlib.Utils;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
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
		public abstract PInvokeAttributes Attributes { get; set; }

		/// <summary>
		/// From column ImplMap.ImportName
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column ImplMap.ImportScope
		/// </summary>
		public abstract ModuleRef Module { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="PInvokeAttributes.NoMangle"/> bit
		/// </summary>
		public bool IsNoMangle {
			get { return (Attributes & PInvokeAttributes.NoMangle) != 0; }
			set {
				if (value)
					Attributes |= PInvokeAttributes.NoMangle;
				else
					Attributes &= ~PInvokeAttributes.NoMangle;
			}
		}

		/// <summary>
		/// Gets/sets the char set
		/// </summary>
		public PInvokeAttributes CharSet {
			get { return Attributes & PInvokeAttributes.CharSetMask; }
			set { Attributes = (Attributes & ~PInvokeAttributes.CharSetMask) | (value & PInvokeAttributes.CharSetMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetNotSpec"/> is set
		/// </summary>
		public bool IsCharSetNotSpec {
			get { return (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetNotSpec; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAnsi"/> is set
		/// </summary>
		public bool IsCharSetAnsi {
			get { return (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetUnicode"/> is set
		/// </summary>
		public bool IsCharSetUnicode {
			get { return (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAuto"/> is set
		/// </summary>
		public bool IsCharSetAuto {
			get { return (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAuto; }
		}

		/// <summary>
		/// Gets/sets best fit
		/// </summary>
		public PInvokeAttributes BestFit {
			get { return Attributes & PInvokeAttributes.BestFitMask; }
			set { Attributes = (Attributes & ~PInvokeAttributes.BestFitMask) | (value & PInvokeAttributes.BestFitMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitUseAssem"/> is set
		/// </summary>
		public bool IsBestFitUseAssem {
			get { return (Attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitUseAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitEnabled"/> is set
		/// </summary>
		public bool IsBestFitEnabled {
			get { return (Attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitDisabled"/> is set
		/// </summary>
		public bool IsBestFitDisabled {
			get { return (Attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitDisabled; }
		}

		/// <summary>
		/// Gets/sets throw on unmappable char
		/// </summary>
		public PInvokeAttributes ThrowOnUnmappableChar {
			get { return Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask; }
			set { Attributes = (Attributes & ~PInvokeAttributes.ThrowOnUnmappableCharMask) | (value & PInvokeAttributes.ThrowOnUnmappableCharMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharUseAssem"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharUseAssem {
			get { return (Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharUseAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharEnabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharEnabled {
			get { return (Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharDisabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharDisabled {
			get { return (Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharDisabled; }
		}

		/// <summary>
		/// Gets/sets the <see cref="PInvokeAttributes.SupportsLastError"/> bit
		/// </summary>
		public bool SupportsLastError {
			get { return (Attributes & PInvokeAttributes.SupportsLastError) != 0; }
			set {
				if (value)
					Attributes |= PInvokeAttributes.SupportsLastError;
				else
					Attributes &= ~PInvokeAttributes.SupportsLastError;
			}
		}

		/// <summary>
		/// Gets/sets calling convention
		/// </summary>
		public PInvokeAttributes CallConv {
			get { return Attributes & PInvokeAttributes.CallConvMask; }
			set { Attributes = (Attributes & ~PInvokeAttributes.CallConvMask) | (value & PInvokeAttributes.CallConvMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvWinapi"/> is set
		/// </summary>
		public bool IsCallConvWinapi {
			get { return (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvWinapi; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvCdecl"/> is set
		/// </summary>
		public bool IsCallConvCdecl {
			get { return (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvCdecl; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvStdcall"/> is set
		/// </summary>
		public bool IsCallConvStdcall {
			get { return (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvStdcall; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvThiscall"/> is set
		/// </summary>
		public bool IsCallConvThiscall {
			get { return (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvThiscall; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvFastcall"/> is set
		/// </summary>
		public bool IsCallConvFastcall {
			get { return (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvFastcall; }
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
		public override PInvokeAttributes Attributes {
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
		public override ModuleRef Module {
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
		public override PInvokeAttributes Attributes {
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
		public override ModuleRef Module {
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
			if (readerModule.TablesStream.ImplMapTable.IsInvalidRID(rid))
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
