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

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public abstract class MethodSpec : IHasCustomAttribute, IMethod {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodSpec, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 21; }
		}

		/// <summary>
		/// From column MethodSpec.Method
		/// </summary>
		public abstract IMethodDefOrRef Method { get; set; }

		/// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		public abstract CallingConventionSig Instantiation { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		MethodSig IMethod.MethodSig {
			get {
				var m = Method;
				return m == null ? null : m.MethodSig;
			}
			set {
				var m = Method;
				if (m != null)
					m.MethodSig = value;
			}
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get {
				var m = Method;
				return m == null ? UTF8String.Empty : m.Name;
			}
			set {
				var m = Method;
				if (m != null)
					m.Name = value;
			}
		}

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get {
				var m = Method;
				return m == null ? null : m.DeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the generic instance method sig
		/// </summary>
		public GenericInstMethodSig GenericInstMethodSig {
			get { return Instantiation as GenericInstMethodSig; }
			set { Instantiation = value; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsMethod {
			get { return true; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsType {
			get { return false; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = GenericInstMethodSig;
				return sig == null ? 0 : sig.GenericArguments.Count;
			}
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var m = Method;
				return m == null ? null : m.Module;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var gims = GenericInstMethodSig;
				var methodGenArgs = gims == null ? null : gims.GenericArguments;
				var method = Method;
				var methodDef = method as MethodDef;
				if (methodDef != null) {
					var declaringType = methodDef.DeclaringType;
					return FullNameCreator.MethodFullName(declaringType == null ? null : declaringType.FullName, methodDef.Name, methodDef.MethodSig, null, methodGenArgs);
				}

				var memberRef = method as MemberRef;
				if (memberRef != null) {
					var methodSig = memberRef.MethodSig;
					if (methodSig != null)
						return FullNameCreator.MethodFullName(memberRef.GetDeclaringTypeFullName(), memberRef.Name, methodSig, null, methodGenArgs);
				}

				return string.Empty;
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MethodSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodSpecUser : MethodSpec {
		IMethodDefOrRef method;
		CallingConventionSig instantiation;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override IMethodDefOrRef Method {
			get { return method; }
			set { method = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Instantiation {
			get { return instantiation; }
			set { instantiation = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodSpecUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		public MethodSpecUser(IMethodDefOrRef method)
			: this(method, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		/// <param name="sig">The instantiated method sig</param>
		public MethodSpecUser(IMethodDefOrRef method, GenericInstMethodSig sig) {
			this.method = method;
			this.instantiation = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MethodSpec table
	/// </summary>
	sealed class MethodSpecMD : MethodSpec {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawMethodSpecRow rawRow;

		UserValue<IMethodDefOrRef> method;
		UserValue<CallingConventionSig> instantiation;
		CustomAttributeCollection customAttributeCollection;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override IMethodDefOrRef Method {
			get { return method.Value; }
			set { method.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Instantiation {
			get { return instantiation.Value; }
			set { instantiation.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MethodSpec, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodSpecMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MethodSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("MethodSpec rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			method.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.ResolveMethodDefOrRef(rawRow.Method);
			};
			instantiation.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.ReadSignature(rawRow.Instantiation);
			};
#if THREAD_SAFE
			method.Lock = theLock;
			instantiation.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodSpecRow(rid);
		}
	}
}
