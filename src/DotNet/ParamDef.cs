// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
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
	/// A high-level representation of a row in the Param table
	/// </summary>
	[DebuggerDisplay("{Sequence} {Name}")]
	public abstract class ParamDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal, IHasCustomDebugInformation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Param, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		public int HasFieldMarshalTag {
			get { return 1; }
		}

		/// <summary>
		/// Gets the declaring method
		/// </summary>
		public MethodDef DeclaringMethod {
			get { return declaringMethod; }
			internal set { declaringMethod = value; }
		}
		/// <summary/>
		protected MethodDef declaringMethod;

		/// <summary>
		/// From column Param.Flags
		/// </summary>
		public ParamAttributes Attributes {
			get { return (ParamAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Param.Sequence
		/// </summary>
		public ushort Sequence {
			get { return sequence; }
			set { sequence = value; }
		}
		/// <summary/>
		protected ushort sequence;

		/// <summary>
		/// From column Param.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <inheritdoc/>
		public MarshalType MarshalType {
			get {
				if (!marshalType_isInitialized)
					InitializeMarshalType();
				return marshalType;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				marshalType = value;
				marshalType_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected MarshalType marshalType;
		/// <summary/>
		protected bool marshalType_isInitialized;

		void InitializeMarshalType() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (marshalType_isInitialized)
				return;
			marshalType = GetMarshalType_NoLock();
			marshalType_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="marshalType"/></summary>
		protected virtual MarshalType GetMarshalType_NoLock() {
			return null;
		}

		/// <summary>Reset <see cref="MarshalType"/></summary>
		protected void ResetMarshalType() {
			marshalType_isInitialized = false;
		}

		/// <inheritdoc/>
		public Constant Constant {
			get {
				if (!constant_isInitialized)
					InitializeConstant();
				return constant;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				constant = value;
				constant_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected Constant constant;
		/// <summary/>
		protected bool constant_isInitialized;

		void InitializeConstant() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (constant_isInitialized)
				return;
			constant = GetConstant_NoLock();
			constant_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="constant"/></summary>
		protected virtual Constant GetConstant_NoLock() {
			return null;
		}

		/// <summary>Reset <see cref="Constant"/></summary>
		protected void ResetConstant() {
			constant_isInitialized = false;
		}

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
			get { return 4; }
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

		/// <summary>
		/// <c>true</c> if <see cref="Constant"/> is not <c>null</c>
		/// </summary>
		public bool HasConstant {
			get { return Constant != null; }
		}

		/// <summary>
		/// Gets the constant element type or <see cref="dnlib.DotNet.ElementType.End"/> if there's no constant
		/// </summary>
		public ElementType ElementType {
			get {
				var c = Constant;
				return c == null ? ElementType.End : c.Type;
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="MarshalType"/> is not <c>null</c>
		/// </summary>
		public bool HasMarshalType {
			get { return MarshalType != null; }
		}

		/// <inheritdoc/>
		public string FullName {
			get {
				var n = name;
				if (UTF8String.IsNullOrEmpty(n))
					return string.Format("A_{0}", sequence);
				return n.String;
			}
		}

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, ParamAttributes flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.In"/> bit
		/// </summary>
		public bool IsIn {
			get { return ((ParamAttributes)attributes & ParamAttributes.In) != 0; }
			set { ModifyAttributes(value, ParamAttributes.In); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Out"/> bit
		/// </summary>
		public bool IsOut {
			get { return ((ParamAttributes)attributes & ParamAttributes.Out) != 0; }
			set { ModifyAttributes(value, ParamAttributes.Out); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Lcid"/> bit
		/// </summary>
		public bool IsLcid {
			get { return ((ParamAttributes)attributes & ParamAttributes.Lcid) != 0; }
			set { ModifyAttributes(value, ParamAttributes.Lcid); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Retval"/> bit
		/// </summary>
		public bool IsRetval {
			get { return ((ParamAttributes)attributes & ParamAttributes.Retval) != 0; }
			set { ModifyAttributes(value, ParamAttributes.Retval); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Optional"/> bit
		/// </summary>
		public bool IsOptional {
			get { return ((ParamAttributes)attributes & ParamAttributes.Optional) != 0; }
			set { ModifyAttributes(value, ParamAttributes.Optional); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get { return ((ParamAttributes)attributes & ParamAttributes.HasDefault) != 0; }
			set { ModifyAttributes(value, ParamAttributes.HasDefault); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.HasFieldMarshal"/> bit
		/// </summary>
		public bool HasFieldMarshal {
			get { return ((ParamAttributes)attributes & ParamAttributes.HasFieldMarshal) != 0; }
			set { ModifyAttributes(value, ParamAttributes.HasFieldMarshal); }
		}
	}

	/// <summary>
	/// A Param row created by the user and not present in the original .NET file
	/// </summary>
	public class ParamDefUser : ParamDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ParamDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ParamDefUser(UTF8String name)
			: this(name, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		public ParamDefUser(UTF8String name, ushort sequence)
			: this(name, sequence, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		/// <param name="flags">Flags</param>
		public ParamDefUser(UTF8String name, ushort sequence, ParamAttributes flags) {
			this.name = name;
			this.sequence = sequence;
			this.attributes = (int)flags;
		}
	}

	/// <summary>
	/// Created from a row in the Param table
	/// </summary>
	sealed class ParamDefMD : ParamDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override MarshalType GetMarshalType_NoLock() {
			return readerModule.ReadMarshalType(Table.Param, origRid, GenericParamContext.Create(declaringMethod));
		}

		/// <inheritdoc/>
		protected override Constant GetConstant_NoLock() {
			return readerModule.ResolveConstant(readerModule.MetaData.GetConstantRid(Table.Param, origRid));
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Param, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), GenericParamContext.Create(declaringMethod), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Param</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ParamDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ParamTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Param rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint name = readerModule.TablesStream.ReadParamRow(origRid, out this.attributes, out this.sequence);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.declaringMethod = readerModule.GetOwner(this);
		}

		internal ParamDefMD InitializeAll() {
			MemberMDInitializer.Initialize(DeclaringMethod);
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(Sequence);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(MarshalType);
			MemberMDInitializer.Initialize(Constant);
			MemberMDInitializer.Initialize(CustomAttributes);
			return this;
		}
	}
}
