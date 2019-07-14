// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Event table
	/// </summary>
	public abstract class EventDef : IHasCustomAttribute, IHasSemantic, IHasCustomDebugInformation, IFullName, IMemberDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.Event, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 10;

		/// <inheritdoc/>
		public int HasSemanticTag => 0;

		/// <summary>
		/// From column Event.EventFlags
		/// </summary>
		public EventAttributes Attributes {
			get => (EventAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary/>
		protected int attributes;

		/// <summary>
		/// From column Event.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Event.EventType
		/// </summary>
		public ITypeDefOrRef EventType {
			get => eventType;
			set => eventType = value;
		}
		/// <summary/>
		protected ITypeDefOrRef eventType;

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes is null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() =>
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 10;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos is null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() =>
			Interlocked.CompareExchange(ref customDebugInfos, new List<PdbCustomDebugInfo>(), null);

		/// <summary>
		/// Gets/sets the adder method
		/// </summary>
		public MethodDef AddMethod {
			get {
				if (otherMethods is null)
					InitializeEventMethods();
				return addMethod;
			}
			set {
				if (otherMethods is null)
					InitializeEventMethods();
				addMethod = value;
			}
		}

		/// <summary>
		/// Gets/sets the invoker method
		/// </summary>
		public MethodDef InvokeMethod {
			get {
				if (otherMethods is null)
					InitializeEventMethods();
				return invokeMethod;
			}
			set {
				if (otherMethods is null)
					InitializeEventMethods();
				invokeMethod = value;
			}
		}

		/// <summary>
		/// Gets/sets the remover method
		/// </summary>
		public MethodDef RemoveMethod {
			get {
				if (otherMethods is null)
					InitializeEventMethods();
				return removeMethod;
			}
			set {
				if (otherMethods is null)
					InitializeEventMethods();
				removeMethod = value;
			}
		}

		/// <summary>
		/// Gets the other methods
		/// </summary>
		public IList<MethodDef> OtherMethods {
			get {
				if (otherMethods is null)
					InitializeEventMethods();
				return otherMethods;
			}
		}

		void InitializeEventMethods() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (otherMethods is null)
				InitializeEventMethods_NoLock();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Initializes <see cref="otherMethods"/>, <see cref="addMethod"/>,
		/// <see cref="invokeMethod"/> and <see cref="removeMethod"/>.
		/// </summary>
		protected virtual void InitializeEventMethods_NoLock() =>
			otherMethods = new List<MethodDef>();

		/// <summary/>
		protected MethodDef addMethod;
		/// <summary/>
		protected MethodDef invokeMethod;
		/// <summary/>
		protected MethodDef removeMethod;
		/// <summary/>
		protected IList<MethodDef> otherMethods;

		/// <summary>Reset <see cref="AddMethod"/>, <see cref="InvokeMethod"/>, <see cref="RemoveMethod"/>, <see cref="OtherMethods"/></summary>
		protected void ResetMethods() => otherMethods = null;

		/// <summary>
		/// <c>true</c> if there are no methods attached to this event
		/// </summary>
		public bool IsEmpty =>
			// The first property access initializes the other fields we access here
			AddMethod is null &&
			removeMethod is null &&
			invokeMethod is null &&
			otherMethods.Count == 0;

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

		/// <summary>
		/// <c>true</c> if <see cref="OtherMethods"/> is not empty
		/// </summary>
		public bool HasOtherMethods => OtherMethods.Count > 0;

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get => declaringType2;
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType == value)
					return;
				if (!(currentDeclaringType is null))
					currentDeclaringType.Events.Remove(this);	// Will set DeclaringType2 = null
				if (!(value is null))
					value.Events.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => declaringType2;

		/// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public TypeDef DeclaringType2 {
			get => declaringType2;
			set => declaringType2 = value;
		}
		/// <summary/>
		protected TypeDef declaringType2;

		/// <inheritdoc/>
		public ModuleDef Module => declaringType2?.Module;

		/// <summary>
		/// Gets the full name of the event
		/// </summary>
		public string FullName => FullNameFactory.EventFullName(declaringType2?.FullName, name, eventType, null, null);

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => false;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => true;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, EventAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get => ((EventAttributes)attributes & EventAttributes.SpecialName) != 0;
			set => ModifyAttributes(value, EventAttributes.SpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get => ((EventAttributes)attributes & EventAttributes.RTSpecialName) != 0;
			set => ModifyAttributes(value, EventAttributes.RTSpecialName);
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// An Event row created by the user and not present in the original .NET file
	/// </summary>
	public class EventDefUser : EventDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public EventDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public EventDefUser(UTF8String name)
			: this(name, null, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="type">Type</param>
		public EventDefUser(UTF8String name, ITypeDefOrRef type)
			: this(name, type, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="type">Type</param>
		/// <param name="flags">Flags</param>
		public EventDefUser(UTF8String name, ITypeDefOrRef type, EventAttributes flags) {
			this.name = name;
			eventType = type;
			attributes = (int)flags;
		}
	}

	/// <summary>
	/// Created from a row in the Event table
	/// </summary>
	sealed class EventDefMD : EventDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Event, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(declaringType2), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Event</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public EventDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.EventTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Event rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadEventRow(origRid, out var row);
			Debug.Assert(b);
			attributes = row.EventFlags;
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			declaringType2 = readerModule.GetOwnerType(this);
			eventType = readerModule.ResolveTypeDefOrRef(row.EventType, new GenericParamContext(declaringType2));
		}

		internal EventDefMD InitializeAll() {
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(EventType);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(AddMethod);
			MemberMDInitializer.Initialize(InvokeMethod);
			MemberMDInitializer.Initialize(RemoveMethod);
			MemberMDInitializer.Initialize(OtherMethods);
			MemberMDInitializer.Initialize(DeclaringType);
			return this;
		}

		/// <inheritdoc/>
		protected override void InitializeEventMethods_NoLock() {
			IList<MethodDef> newOtherMethods;
			var dt = declaringType2 as TypeDefMD;
			if (dt is null)
				newOtherMethods = new List<MethodDef>();
			else
				dt.InitializeEvent(this, out addMethod, out invokeMethod, out removeMethod, out newOtherMethods);
			otherMethods = newOtherMethods;
		}
	}
}
