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
using dnlib.Utils;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the GenericParamConstraint table
	/// </summary>
	public abstract class GenericParamConstraint : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParamConstraint, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 20; }
		}

		/// <summary>
		/// Gets the owner generic param
		/// </summary>
		public abstract GenericParam Owner { get; internal set; }

		/// <summary>
		/// From column GenericParamConstraint.Constraint
		/// </summary>
		public abstract ITypeDefOrRef Constraint { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}
	}

	/// <summary>
	/// A GenericParamConstraintAssembly row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamConstraintUser : GenericParamConstraint {
		GenericParam owner;
		ITypeDefOrRef constraint;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override GenericParam Owner {
			get { return owner; }
			internal set { owner = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Constraint {
			get { return constraint; }
			set { constraint = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericParamConstraintUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="constraint">The constraint</param>
		public GenericParamConstraintUser(ITypeDefOrRef constraint) {
			this.constraint = constraint;
		}
	}

	/// <summary>
	/// Created from a row in the GenericParamConstraint table
	/// </summary>
	sealed class GenericParamConstraintMD : GenericParamConstraint {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawGenericParamConstraintRow rawRow;

		UserValue<GenericParam> owner;
		UserValue<ITypeDefOrRef> constraint;
		CustomAttributeCollection customAttributeCollection;

		/// <inheritdoc/>
		public override GenericParam Owner {
			get { return owner.Value; }
			internal set { owner.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Constraint {
			get { return constraint.Value; }
			set { constraint.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.GenericParamConstraint, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParamConstraint</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public GenericParamConstraintMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.GenericParamConstraintTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("GenericParamConstraint rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			owner.ReadOriginalValue = () => {
				return readerModule.GetOwner(this);
			};
			constraint.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Constraint);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadGenericParamConstraintRow(rid);
		}

		internal GenericParamConstraintMD InitializeAll() {
			MemberMDInitializer.Initialize(Owner);
			MemberMDInitializer.Initialize(Constraint);
			MemberMDInitializer.Initialize(CustomAttributes);
			return this;
		}
	}
}
