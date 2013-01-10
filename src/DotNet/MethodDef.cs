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
using dnlib.Utils;
using dnlib.PE;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Method table
	/// </summary>
	public abstract class MethodDef : IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IMethodDefOrRef, IMemberForwarded, ICustomAttributeType, ITypeOrMethodDef, IManagedEntryPoint, IListListener<GenericParam>, IListListener<ParamDef> {
		internal static readonly UTF8String staticConstructorName = ".cctor";
		static readonly UTF8String instanceConstructorName = ".ctor";

		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// All parameters
		/// </summary>
		protected ParameterList parameterList;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Method, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 1; }
		}

		/// <summary>
		/// From column Method.RVA
		/// </summary>
		public abstract RVA RVA { get; set; }

		/// <summary>
		/// From column Method.ImplFlags
		/// </summary>
		public abstract MethodImplAttributes ImplAttributes { get; set; }

		/// <summary>
		/// From column Method.Flags
		/// </summary>
		public abstract MethodAttributes Attributes { get; set; }

		/// <summary>
		/// From column Method.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Method.Signature
		/// </summary>
		public abstract CallingConventionSig Signature { get; set; }

		/// <summary>
		/// From column Method.ParamList
		/// </summary>
		public abstract IList<ParamDef> ParamDefs { get; }

		/// <inheritdoc/>
		public abstract IList<GenericParam> GenericParameters { get; }

		/// <inheritdoc/>
		public abstract IList<DeclSecurity> DeclSecurities { get; }

		/// <inheritdoc/>
		public abstract ImplMap ImplMap { get; set; }

		/// <summary>
		/// Gets/sets the method body. See also <see cref="Body"/>
		/// </summary>
		public abstract MethodBody MethodBody { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets the methods this method implements
		/// </summary>
		public abstract IList<MethodOverride> Overrides { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get { return DeclaringType2; }
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType != null)
					currentDeclaringType.Methods.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.Methods.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMethod.DeclaringType {
			get { return DeclaringType; }
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public abstract TypeDef DeclaringType2 { get; set; }

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var dt = DeclaringType;
				return dt == null ? null : dt.Module;
			}
		}

		/// <summary>
		/// Gets/sets the CIL method body
		/// </summary>
		public CilBody Body {
			get { return MethodBody as CilBody; }
			set { MethodBody = value; }
		}

		/// <summary>
		/// Gets/sets the native method body
		/// </summary>
		public NativeMethodBody NativeBody {
			get { return MethodBody as NativeMethodBody; }
			set { MethodBody = value; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="GenericParam"/> in <see cref="GenericParameters"/>
		/// </summary>
		public bool HasGenericParameters {
			get { return GenericParameters.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if it has a <see cref="Body"/>
		/// </summary>
		public bool HasBody {
			get { return Body != null; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="MethodOverride"/> in <see cref="Overrides"/>
		/// </summary>
		public bool HasOverrides {
			get { return Overrides.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ImplMap"/> is not <c>null</c>
		/// </summary>
		public bool HasImplMap {
			get { return ImplMap != null; }
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var declaringType = DeclaringType;
				return FullNameCreator.MethodFullName(declaringType == null ? null : declaringType.FullName, Name, MethodSig);
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get { return Signature as MethodSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Gets the parameters
		/// </summary>
		public ParameterList Parameters {
			get { return parameterList; }
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
				var sig = MethodSig;
				return sig == null ? 0 : (int)sig.GenParamCount;
			}
		}

		/// <summary>
		/// <c>true</c> if the method has a hidden 'this' parameter
		/// </summary>
		public bool HasThis {
			get { return MethodSig == null ? false : MethodSig.HasThis; }
		}

		/// <summary>
		/// <c>true</c> if the method has an explicit 'this' parameter
		/// </summary>
		public bool ExplicitThis {
			get { return MethodSig == null ? false : MethodSig.ExplicitThis; }
		}

		/// <summary>
		/// Gets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get { return MethodSig == null ? 0 : MethodSig.CallingConvention & CallingConvention.Mask; }
		}

		/// <summary>
		/// Gets/sets the method return type
		/// </summary>
		public TypeSig ReturnType {
			get { return MethodSig == null ? null : MethodSig.RetType; }
			set {
				if (MethodSig != null)
					MethodSig.RetType = value;
			}
		}

		/// <summary>
		/// Gets/sets the method access
		/// </summary>
		public MethodAttributes Access {
			get { return Attributes & MethodAttributes.MemberAccessMask; }
			set { Attributes = (Attributes & ~MethodAttributes.MemberAccessMask) | (value & MethodAttributes.MemberAccessMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsCompilerControlled {
			get { return IsPrivateScope; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsPrivateScope {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamANDAssem"/> is set
		/// </summary>
		public bool IsFamilyAndAssembly {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Assembly"/> is set
		/// </summary>
		public bool IsAssembly {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Family"/> is set
		/// </summary>
		public bool IsFamily {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamORAssem"/> is set
		/// </summary>
		public bool IsFamilyOrAssembly {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Static"/> bit
		/// </summary>
		public bool IsStatic {
			get { return (Attributes & MethodAttributes.Static) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.Static;
				else
					Attributes &= ~MethodAttributes.Static;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Final"/> bit
		/// </summary>
		public bool IsFinal {
			get { return (Attributes & MethodAttributes.Final) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.Final;
				else
					Attributes &= ~MethodAttributes.Final;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Virtual"/> bit
		/// </summary>
		public bool IsVirtual {
			get { return (Attributes & MethodAttributes.Virtual) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.Virtual;
				else
					Attributes &= ~MethodAttributes.Virtual;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HideBySig"/> bit
		/// </summary>
		public bool IsHideBySig {
			get { return (Attributes & MethodAttributes.HideBySig) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.HideBySig;
				else
					Attributes &= ~MethodAttributes.HideBySig;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.NewSlot"/> bit
		/// </summary>
		public bool IsNewSlot {
			get { return (Attributes & MethodAttributes.NewSlot) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.NewSlot;
				else
					Attributes &= ~MethodAttributes.NewSlot;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.ReuseSlot"/> bit
		/// </summary>
		public bool IsReuseSlot {
			get { return (Attributes & MethodAttributes.NewSlot) == 0; }
			set {
				if (value)
					Attributes &= ~MethodAttributes.NewSlot;
				else
					Attributes |= MethodAttributes.NewSlot;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.CheckAccessOnOverride"/> bit
		/// </summary>
		public bool IsCheckAccessOnOverride {
			get { return (Attributes & MethodAttributes.CheckAccessOnOverride) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.CheckAccessOnOverride;
				else
					Attributes &= ~MethodAttributes.CheckAccessOnOverride;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get { return (Attributes & MethodAttributes.Abstract) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.Abstract;
				else
					Attributes &= ~MethodAttributes.Abstract;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Attributes & MethodAttributes.SpecialName) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.SpecialName;
				else
					Attributes &= ~MethodAttributes.SpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.PinvokeImpl"/> bit
		/// </summary>
		public bool IsPinvokeImpl {
			get { return (Attributes & MethodAttributes.PinvokeImpl) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.PinvokeImpl;
				else
					Attributes &= ~MethodAttributes.PinvokeImpl;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.UnmanagedExport"/> bit
		/// </summary>
		public bool IsUnmanagedExport {
			get { return (Attributes & MethodAttributes.UnmanagedExport) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.UnmanagedExport;
				else
					Attributes &= ~MethodAttributes.UnmanagedExport;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get { return (Attributes & MethodAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.RTSpecialName;
				else
					Attributes &= ~MethodAttributes.RTSpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get { return (Attributes & MethodAttributes.HasSecurity) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.HasSecurity;
				else
					Attributes &= ~MethodAttributes.HasSecurity;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RequireSecObject"/> bit
		/// </summary>
		public bool IsRequireSecObject {
			get { return (Attributes & MethodAttributes.RequireSecObject) != 0; }
			set {
				if (value)
					Attributes |= MethodAttributes.RequireSecObject;
				else
					Attributes &= ~MethodAttributes.RequireSecObject;
			}
		}

		/// <summary>
		/// Gets/sets the code type
		/// </summary>
		public MethodImplAttributes CodeType {
			get { return ImplAttributes & MethodImplAttributes.CodeTypeMask; }
			set { ImplAttributes = (ImplAttributes & ~MethodImplAttributes.CodeTypeMask) | (value & MethodImplAttributes.CodeTypeMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.IL"/> is set
		/// </summary>
		public bool IsIL {
			get { return (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Native"/> is set
		/// </summary>
		public bool IsNative {
			get { return (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.OPTIL"/> is set
		/// </summary>
		public bool IsOPTIL {
			get { return (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Runtime"/> is set
		/// </summary>
		public bool IsRuntime {
			get { return (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Unmanaged"/> bit
		/// </summary>
		public bool IsUnmanaged {
			get { return (ImplAttributes & MethodImplAttributes.Unmanaged) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.Unmanaged;
				else
					ImplAttributes &= ~MethodImplAttributes.Unmanaged;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Managed"/> bit
		/// </summary>
		public bool IsManaged {
			get { return (ImplAttributes & MethodImplAttributes.Unmanaged) == 0; }
			set {
				if (value)
					ImplAttributes &= ~MethodImplAttributes.Unmanaged;
				else
					ImplAttributes |= MethodImplAttributes.Unmanaged;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.ForwardRef"/> bit
		/// </summary>
		public bool IsForwardRef {
			get { return (ImplAttributes & MethodImplAttributes.ForwardRef) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.ForwardRef;
				else
					ImplAttributes &= ~MethodImplAttributes.ForwardRef;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.PreserveSig"/> bit
		/// </summary>
		public bool IsPreserveSig {
			get { return (ImplAttributes & MethodImplAttributes.PreserveSig) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.PreserveSig;
				else
					ImplAttributes &= ~MethodImplAttributes.PreserveSig;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.InternalCall"/> bit
		/// </summary>
		public bool IsInternalCall {
			get { return (ImplAttributes & MethodImplAttributes.InternalCall) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.InternalCall;
				else
					ImplAttributes &= ~MethodImplAttributes.InternalCall;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Synchronized"/> bit
		/// </summary>
		public bool IsSynchronized {
			get { return (ImplAttributes & MethodImplAttributes.Synchronized) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.Synchronized;
				else
					ImplAttributes &= ~MethodImplAttributes.Synchronized;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoInlining"/> bit
		/// </summary>
		public bool IsNoInlining {
			get { return (ImplAttributes & MethodImplAttributes.NoInlining) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.NoInlining;
				else
					ImplAttributes &= ~MethodImplAttributes.NoInlining;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.AggressiveInlining"/> bit
		/// </summary>
		public bool IsAggressiveInlining {
			get { return (ImplAttributes & MethodImplAttributes.AggressiveInlining) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.AggressiveInlining;
				else
					ImplAttributes &= ~MethodImplAttributes.AggressiveInlining;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoOptimization"/> bit
		/// </summary>
		public bool IsNoOptimization {
			get { return (ImplAttributes & MethodImplAttributes.NoOptimization) != 0; }
			set {
				if (value)
					ImplAttributes |= MethodImplAttributes.NoOptimization;
				else
					ImplAttributes &= ~MethodImplAttributes.NoOptimization;
			}
		}

		/// <summary>
		/// <c>true</c> if this is the static type constructor
		/// </summary>
		public bool IsStaticConstructor {
			get { return IsRuntimeSpecialName && IsSpecialName && UTF8String.Equals(Name, staticConstructorName); }
		}

		/// <summary>
		/// <c>true</c> if this is an instance constructor
		/// </summary>
		public bool IsInstanceConstructor {
			get { return IsRuntimeSpecialName && IsSpecialName && UTF8String.Equals(Name, instanceConstructorName); }
		}

		/// <summary>
		/// <c>true</c> if this is a static or an instance constructor
		/// </summary>
		public bool IsConstructor {
			get { return IsStaticConstructor || IsInstanceConstructor; }
		}

		/// <inheritdoc/>
		public virtual void OnLazyAdd(int index, ref GenericParam value) {
#if DEBUG
			if (value.Owner != this)
				throw new InvalidOperationException("Added generic param's Owner != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnAdd(int index, GenericParam value) {
			if (value.Owner != null)
				throw new InvalidOperationException("Generic param is already owned by another type/method. Set Owner to null first.");
			value.Owner = this;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnRemove(int index, GenericParam value) {
			value.Owner = null;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnClear() {
			foreach (var gp in GenericParameters)
				gp.Owner = null;
		}

		/// <inheritdoc/>
		public virtual void OnLazyAdd(int index, ref ParamDef value) {
#if DEBUG
			if (value.DeclaringMethod != this)
				throw new InvalidOperationException("Added param's DeclaringMethod != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnAdd(int index, ParamDef value) {
			if (value.DeclaringMethod != null)
				throw new InvalidOperationException("Param is already owned by another method. Set DeclaringMethod to null first.");
			value.DeclaringMethod = this;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnRemove(int index, ParamDef value) {
			value.DeclaringMethod = null;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnClear() {
			foreach (var pd in ParamDefs)
				pd.DeclaringMethod = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Method row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodDefUser : MethodDef {
		RVA rva;
		MethodImplAttributes implFlags;
		MethodAttributes flags;
		UTF8String name;
		CallingConventionSig signature;
		IList<ParamDef> parameters;
		LazyList<GenericParam> genericParams;
		IList<DeclSecurity> declSecurities = new List<DeclSecurity>();
		ImplMap implMap;
		MethodBody methodBody;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		IList<MethodOverride> overrides = new List<MethodOverride>();
		TypeDef declaringType;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplAttributes {
			get { return implFlags; }
			set { implFlags = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Attributes {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature; }
			set {
				signature = value;
				parameterList.UpdateParameterTypes();
			}
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamDefs {
			get { return parameters; }
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParameters {
			get { return genericParams; }
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get { return declSecurities; }
		}

		/// <inheritdoc/>
		public override ImplMap ImplMap {
			get { return implMap; }
			set { implMap = value; }
		}

		/// <inheritdoc/>
		public override MethodBody MethodBody {
			get { return methodBody; }
			set { methodBody = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		public override IList<MethodOverride> Overrides {
			get { return overrides; }
		}

		/// <inheritdoc/>
		public override TypeDef DeclaringType2 {
			get { return declaringType; }
			set {
				declaringType = value;
				parameterList.UpdateThisParameterType();
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodDefUser() {
			this.parameters = new LazyList<ParamDef>(this);
			this.genericParams = new LazyList<GenericParam>(this);
			this.parameterList = new ParameterList(this);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		public MethodDefUser(UTF8String name)
			: this(name, null, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig)
			: this(name, methodSig, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodAttributes flags)
			: this(name, methodSig, 0, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags)
			: this(name, methodSig, implFlags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags, MethodAttributes flags) {
			this.name = name;
			this.signature = methodSig;
			this.implFlags = implFlags;
			this.flags = flags;
			this.parameters = new LazyList<ParamDef>(this);
			this.genericParams = new LazyList<GenericParam>(this);
			this.parameterList = new ParameterList(this);
		}
	}

	/// <summary>
	/// Created from a row in the Method table
	/// </summary>
	sealed class MethodDefMD : MethodDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodRow rawRow;

		UserValue<RVA> rva;
		UserValue<MethodImplAttributes> implFlags;
		UserValue<MethodAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<CallingConventionSig> signature;
		LazyList<ParamDef> parameters;
		LazyList<GenericParam> genericParams;
		LazyList<DeclSecurity> declSecurities;
		UserValue<ImplMap> implMap;
		UserValue<MethodBody> methodBody;
		CustomAttributeCollection customAttributeCollection;
		IList<MethodOverride> overrides;
		UserValue<TypeDef> declaringType;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva.Value; }
			set { rva.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplAttributes {
			get { return implFlags.Value; }
			set { implFlags.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Attributes {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature.Value; }
			set {
				signature.Value = value;
				parameterList.UpdateParameterTypes();
			}
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamDefs {
			get {
				if (parameters == null) {
					var list = readerModule.MetaData.GetParamRidList(rid);
					parameters = new LazyList<ParamDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveParam(((RidList)list2)[index]));
				}
				return parameters;
			}
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParameters {
			get {
				if (genericParams == null) {
					var list = readerModule.MetaData.GetGenericParamRidList(Table.Method, rid);
					genericParams = new LazyList<GenericParam>((int)list.Length, this, list, (list2, index) => readerModule.ResolveGenericParam(((RidList)list2)[index]));
				}
				return genericParams;
			}
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null) {
					var list = readerModule.MetaData.GetDeclSecurityRidList(Table.Method, rid);
					declSecurities = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
				}
				return declSecurities;
			}
		}

		/// <inheritdoc/>
		public override ImplMap ImplMap {
			get { return implMap.Value; }
			set { implMap.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodBody MethodBody {
			get { return methodBody.Value; }
			set { methodBody.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Method, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override IList<MethodOverride> Overrides {
			get {
				if (overrides != null)
					return overrides;
				var dt = DeclaringType as TypeDefMD;
				if (dt == null)
					return overrides = new List<MethodOverride>();
				return overrides = dt.GetMethodOverrides(this);
			}
		}

		/// <inheritdoc/>
		public override TypeDef DeclaringType2 {
			get { return declaringType.Value; }
			set {
				declaringType.Value = value;
				parameterList.UpdateThisParameterType();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Method</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MethodTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Method rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
			this.parameterList = new ParameterList(this);
		}

		void Initialize() {
			rva.ReadOriginalValue = () => {
				InitializeRawRow();
				return (RVA)rawRow.RVA;
			};
			implFlags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodImplAttributes)rawRow.ImplFlags;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			signature.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Signature);
			};
			implMap.ReadOriginalValue = () => {
				return readerModule.ResolveImplMap(readerModule.MetaData.GetImplMapRid(Table.Method, rid));
			};
			methodBody.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadMethodBody(this, rawRow);
			};
			declaringType.ReadOriginalValue = () => {
				return readerModule.GetOwnerType(this);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodRow(rid);
		}

		internal MethodDefMD InitializeAll() {
			MemberMDInitializer.Initialize(RVA);
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(ImplAttributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(Signature);
			MemberMDInitializer.Initialize(ImplMap);
			MemberMDInitializer.Initialize(MethodBody);
			MemberMDInitializer.Initialize(DeclaringType);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(Overrides);
			MemberMDInitializer.Initialize(ParamDefs);
			MemberMDInitializer.Initialize(GenericParameters);
			MemberMDInitializer.Initialize(DeclSecurities);
			return this;
		}

		/// <inheritdoc/>
		public override void OnLazyAdd(int index, ref GenericParam value) {
			if (value.Owner != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadGenericParam(value.Rid).InitializeAll());
				value.Owner = this;
			}
		}

		/// <inheritdoc/>
		public override void OnLazyAdd(int index, ref ParamDef value) {
			if (value.DeclaringMethod != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadParam(value.Rid).InitializeAll());
				value.DeclaringMethod = this;
			}
		}
	}
}
