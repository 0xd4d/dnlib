// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the GenericParamConstraint table
	/// </summary>
	public abstract class GenericParamConstraint : IHasCustomAttribute, IHasCustomDebugInformation, IContainsGenericParameter {
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
		public GenericParam Owner {
			get { return owner; }
			internal set { owner = value; }
		}
		/// <summary/>
		protected GenericParam owner;

		/// <summary>
		/// From column GenericParamConstraint.Constraint
		/// </summary>
		public ITypeDefOrRef Constraint {
			get { return constraint; }
			set { constraint = value; }
		}
		/// <summary/>
		protected ITypeDefOrRef constraint;

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes == null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() {
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag {
			get { return 20; }
		}

		/// <inheritdoc/>
		public bool HasCustomDebugInfos {
			get { return CustomDebugInfos.Count > 0; }
		}

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos == null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() {
			Interlocked.CompareExchange(ref customDebugInfos, ThreadSafeListCreator.Create<PdbCustomDebugInfo>(), null);
		}

		bool IContainsGenericParameter.ContainsGenericParameter {
			get { return TypeHelper.ContainsGenericParameter(this); }
		}
	}

	/// <summary>
	/// A GenericParamConstraintAssembly row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamConstraintUser : GenericParamConstraint {
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
	sealed class GenericParamConstraintMD : GenericParamConstraint, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly GenericParamContext gpContext;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.GenericParamConstraint, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), gpContext, list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParamConstraint</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public GenericParamConstraintMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.GenericParamConstraintTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("GenericParamConstraint rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			uint constraint = readerModule.TablesStream.ReadGenericParamConstraintRow2(origRid);
			this.constraint = readerModule.ResolveTypeDefOrRef(constraint, gpContext);
			this.owner = readerModule.GetOwner(this);
		}

		internal GenericParamConstraintMD InitializeAll() {
			MemberMDInitializer.Initialize(Owner);
			MemberMDInitializer.Initialize(Constraint);
			MemberMDInitializer.Initialize(CustomAttributes);
			return this;
		}
	}
}
