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
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Event table
	/// </summary>
	public abstract class EventDef : IHasCustomAttribute, IHasSemantic, IFullName, IMemberRef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		/// <summary>
		/// The lock
		/// </summary>
		internal readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Event, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 10; }
		}

		/// <inheritdoc/>
		public int HasSemanticTag {
			get { return 0; }
		}

		/// <summary>
		/// From column Event.EventFlags
		/// </summary>
		public EventAttributes Attributes {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock();
				try {
					return Attributes_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
			set {
				theLock.EnterWriteLock();
				try {
					Attributes_NoLock = value;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return Attributes_NoLock; }
			set { Attributes_NoLock = value; }
#endif
		}

		/// <summary>
		/// From column Event.EventFlags
		/// </summary>
		protected abstract EventAttributes Attributes_NoLock { get; set; }

		/// <summary>
		/// From column Event.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Event.EventType
		/// </summary>
		public abstract ITypeDefOrRef EventType { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets/sets the adder method
		/// </summary>
		public abstract MethodDef AddMethod { get; set; }

		/// <summary>
		/// Gets/sets the invoker method
		/// </summary>
		public abstract MethodDef InvokeMethod { get; set; }

		/// <summary>
		/// Gets/sets the remover method
		/// </summary>
		public abstract MethodDef RemoveMethod { get; set; }

		/// <summary>
		/// Gets the other methods
		/// </summary>
		public abstract ThreadSafe.IList<MethodDef> OtherMethods { get; }

		/// <summary>
		/// <c>true</c> if there are no methods attached to this event
		/// </summary>
		public bool IsEmpty {
			get {
				return AddMethod == null &&
					RemoveMethod == null &&
					InvokeMethod == null &&
					OtherMethods.Count == 0;
			}
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="OtherMethods"/> is not empty
		/// </summary>
		public bool HasOtherMethods {
			get { return OtherMethods.Count > 0; }
		}

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get { return DeclaringType2; }
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType == value)
					return;
				if (currentDeclaringType != null)
					currentDeclaringType.Events.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.Events.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public TypeDef DeclaringType2 {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock(); try {
					return DeclaringType2_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
			set {
				theLock.EnterWriteLock(); try {
					DeclaringType2_NoLock = value;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return DeclaringType2_NoLock; }
			set { DeclaringType2_NoLock = value; }
#endif
		}

		/// <summary>
		/// No-lock version of <see cref="DeclaringType2"/>.
		/// </summary>
		internal abstract TypeDef DeclaringType2_NoLock { get; set; }

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var dt = DeclaringType;
				return dt == null ? null : dt.Module;
			}
		}

		/// <summary>
		/// Gets the full name of the event
		/// </summary>
		public string FullName {
			get {
				var dt = DeclaringType;
				return FullNameCreator.EventFullName(dt == null ? null : dt.FullName, Name, EventType);
			}
		}

		/// <summary>
		/// Set or clear flags in <see cref="Attributes_NoLock"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, EventAttributes flags) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				if (set)
					Attributes_NoLock |= flags;
				else
					Attributes_NoLock &= ~flags;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Attributes & EventAttributes.SpecialName) != 0; }
			set { ModifyAttributes(value, EventAttributes.SpecialName); }
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get { return (Attributes & EventAttributes.RTSpecialName) != 0; }
			set { ModifyAttributes(value, EventAttributes.RTSpecialName); }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An Event row created by the user and not present in the original .NET file
	/// </summary>
	public class EventDefUser : EventDef {
		EventAttributes flags;
		UTF8String name;
		ITypeDefOrRef type;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		MethodDef addMethod;
		MethodDef invokeMethod;
		MethodDef removeMethod;
		readonly ThreadSafe.IList<MethodDef> otherMethods = ThreadSafeListCreator.Create<MethodDef>();
		TypeDef declaringType;

		/// <inheritdoc/>
		protected override EventAttributes Attributes_NoLock {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef EventType {
			get { return type; }
			set { type = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		public override MethodDef AddMethod {
			get { return addMethod; }
			set { addMethod = value; }
		}

		/// <inheritdoc/>
		public override MethodDef InvokeMethod {
			get { return invokeMethod; }
			set { invokeMethod = value; }
		}

		/// <inheritdoc/>
		public override MethodDef RemoveMethod {
			get { return removeMethod; }
			set { removeMethod = value; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<MethodDef> OtherMethods {
			get { return otherMethods; }
		}

		/// <inheritdoc/>
		internal override TypeDef DeclaringType2_NoLock {
			get { return declaringType; }
			set { declaringType = value; }
		}

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
			this.type = type;
			this.flags = flags;
		}
	}

	/// <summary>
	/// Created from a row in the Event table
	/// </summary>
	sealed class EventDefMD : EventDef {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawEventRow rawRow;

		UserValue<EventAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<ITypeDefOrRef> type;
		CustomAttributeCollection customAttributeCollection;
		MethodDef addMethod;
		MethodDef invokeMethod;
		MethodDef removeMethod;
		ThreadSafe.IList<MethodDef> otherMethods;
		UserValue<TypeDef> declaringType;

		/// <inheritdoc/>
		protected override EventAttributes Attributes_NoLock {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef EventType {
			get { return type.Value; }
			set { type.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Event, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override MethodDef AddMethod {
			get { InitializeEventMethods_NoLock(); return addMethod; }
			set { InitializeEventMethods_NoLock(); addMethod = value; }
		}

		/// <inheritdoc/>
		public override MethodDef InvokeMethod {
			get { InitializeEventMethods_NoLock(); return invokeMethod; }
			set { InitializeEventMethods_NoLock(); invokeMethod = value; }
		}

		/// <inheritdoc/>
		public override MethodDef RemoveMethod {
			get { InitializeEventMethods_NoLock(); return removeMethod; }
			set { InitializeEventMethods_NoLock(); removeMethod = value; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<MethodDef> OtherMethods {
			get { InitializeEventMethods_NoLock(); return otherMethods; }
		}

		/// <inheritdoc/>
		internal override TypeDef DeclaringType2_NoLock {
			get { return declaringType.Value; }
			set { declaringType.Value = value; }
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
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.EventTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Event rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return (EventAttributes)rawRow.EventFlags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			type.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.ResolveTypeDefOrRef(rawRow.EventType);
			};
			declaringType.ReadOriginalValue = () => {
				return readerModule.GetOwnerType(this);
			};
#if THREAD_SAFE
			// flags.Lock = theLock;			No lock for this one
			name.Lock = theLock;
			type.Lock = theLock;
			// declaringType.Lock = theLock;	No lock for this one
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadEventRow(rid);
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

		void InitializeEventMethods_NoLock() {
			if (otherMethods != null)
				return;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
			if (otherMethods != null) return;
#endif
			ThreadSafe.IList<MethodDef> newOtherMethods;
			var dt = DeclaringType2_NoLock as TypeDefMD;
			if (dt == null)
				newOtherMethods = ThreadSafeListCreator.Create<MethodDef>();
			else
				dt.InitializeEvent(this, out addMethod, out invokeMethod, out removeMethod, out newOtherMethods);
			otherMethods = newOtherMethods;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}
	}
}
