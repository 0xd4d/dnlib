// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

/*
All signature classes:

CallingConventionSig
	FieldSig
	MethodBaseSig
		MethodSig
		PropertySig
	LocalSig
	GenericInstMethodSig
*/

namespace dnlib.DotNet {
	/// <summary>
	/// Base class for sigs with a calling convention
	/// </summary>
	public abstract class CallingConventionSig : IContainsGenericParameter {
		/// <summary>
		/// The calling convention
		/// </summary>
		protected CallingConvention callingConvention;

		byte[] extraData;

		/// <summary>
		/// Gets/sets the extra data found after the signature
		/// </summary>
		public byte[] ExtraData {
			get { return extraData; }
			set { extraData = value; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.Default"/> is set
		/// </summary>
		public bool IsDefault {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.Default; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.C"/> is set
		/// </summary>
		public bool IsC {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.C; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.StdCall"/> is set
		/// </summary>
		public bool IsStdCall {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.StdCall; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.ThisCall"/> is set
		/// </summary>
		public bool IsThisCall {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.ThisCall; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.FastCall"/> is set
		/// </summary>
		public bool IsFastCall {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.FastCall; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.VarArg"/> is set
		/// </summary>
		public bool IsVarArg {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.VarArg; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.Field"/> is set
		/// </summary>
		public bool IsField {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.Field; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.LocalSig"/> is set
		/// </summary>
		public bool IsLocalSig {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.LocalSig; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.Property"/> is set
		/// </summary>
		public bool IsProperty {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.Property; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.Unmanaged"/> is set
		/// </summary>
		public bool IsUnmanaged {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.Unmanaged; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.GenericInst"/> is set
		/// </summary>
		public bool IsGenericInst {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.GenericInst; }
		}

		/// <summary>
		/// Returns <c>true</c> if <see cref="CallingConvention.NativeVarArg"/> is set
		/// </summary>
		public bool IsNativeVarArg {
			get { return (callingConvention & CallingConvention.Mask) == CallingConvention.NativeVarArg; }
		}

		/// <summary>
		/// Gets/sets the <see cref="CallingConvention.Generic"/> bit
		/// </summary>
		public bool Generic {
			get { return (callingConvention & CallingConvention.Generic) != 0; }
			set {
				if (value)
					callingConvention |= CallingConvention.Generic;
				else
					callingConvention &= ~CallingConvention.Generic;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="CallingConvention.HasThis"/> bit
		/// </summary>
		public bool HasThis {
			get { return (callingConvention & CallingConvention.HasThis) != 0; }
			set {
				if (value)
					callingConvention |= CallingConvention.HasThis;
				else
					callingConvention &= ~CallingConvention.HasThis;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="CallingConvention.ExplicitThis"/> bit
		/// </summary>
		public bool ExplicitThis {
			get { return (callingConvention & CallingConvention.ExplicitThis) != 0; }
			set {
				if (value)
					callingConvention |= CallingConvention.ExplicitThis;
				else
					callingConvention &= ~CallingConvention.ExplicitThis;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="CallingConvention.ReservedByCLR"/> bit
		/// </summary>
		public bool ReservedByCLR {
			get { return (callingConvention & CallingConvention.ReservedByCLR) != 0; }
			set {
				if (value)
					callingConvention |= CallingConvention.ReservedByCLR;
				else
					callingConvention &= ~CallingConvention.ReservedByCLR;
			}
		}

		/// <summary>
		/// <c>true</c> if there's an implicit <c>this</c> parameter
		/// </summary>
		public bool ImplicitThis {
			get { return HasThis && !ExplicitThis; }
		}

		/// <summary>
		/// <c>true</c> if this <see cref="CallingConventionSig"/> contains a
		/// <see cref="GenericVar"/> or a <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter {
			get { return TypeHelper.ContainsGenericParameter(this); }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		protected CallingConventionSig() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">The calling convention</param>
		protected CallingConventionSig(CallingConvention callingConvention) {
			this.callingConvention = callingConvention;
		}

		/// <summary>
		/// Gets the calling convention
		/// </summary>
		public CallingConvention GetCallingConvention() {
			return callingConvention;
		}
	}

	/// <summary>
	/// A field signature
	/// </summary>
	public sealed class FieldSig : CallingConventionSig {
		TypeSig type;

		/// <summary>
		/// Gets/sets the field type
		/// </summary>
		public TypeSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldSig() {
			this.callingConvention = CallingConvention.Field;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Field type</param>
		public FieldSig(TypeSig type) {
			this.callingConvention = CallingConvention.Field;
			this.type = type;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Field type</param>
		/// <param name="callingConvention">The calling convention (must have Field set)</param>
		internal FieldSig(CallingConvention callingConvention, TypeSig type) {
			this.callingConvention = callingConvention;
			this.type = type;
		}

		/// <summary>
		/// Clone this
		/// </summary>
		public FieldSig Clone() {
			return new FieldSig(callingConvention, type);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullNameCreator.FullName(type == null ? null : type, false);
		}
	}

	/// <summary>
	/// Method sig base class
	/// </summary>
	public abstract class MethodBaseSig : CallingConventionSig {
		/// <summary/>
		protected TypeSig retType;
		/// <summary/>
		protected ThreadSafe.IList<TypeSig> parameters;
		/// <summary/>
		protected uint genParamCount;
		/// <summary/>
		protected ThreadSafe.IList<TypeSig> paramsAfterSentinel;

		/// <summary>
		/// Gets/sets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get { return callingConvention; }
			set { callingConvention = value; }
		}

		/// <summary>
		/// Gets/sets the return type
		/// </summary>
		public TypeSig RetType {
			get { return retType; }
			set { retType = value; }
		}

		/// <summary>
		/// Gets the parameters. This is never <c>null</c>
		/// </summary>
		public ThreadSafe.IList<TypeSig> Params {
			get { return parameters; }
		}

		/// <summary>
		/// Gets/sets the generic param count
		/// </summary>
		public uint GenParamCount {
			get { return genParamCount; }
			set { genParamCount = value; }
		}

		/// <summary>
		/// Gets the parameters that are present after the sentinel. Note that this is <c>null</c>
		/// if there's no sentinel. It can still be empty even if it's not <c>null</c>.
		/// </summary>
		public ThreadSafe.IList<TypeSig> ParamsAfterSentinel {
			get { return paramsAfterSentinel; }
			set { paramsAfterSentinel = value; }
		}
	}

	/// <summary>
	/// A method signature
	/// </summary>
	public sealed class MethodSig : MethodBaseSig {
		uint origToken;

		/// <summary>
		/// Gets/sets the original token. It's set when reading calli instruction operands
		/// and it's a hint to the module writer if it tries to re-use the same token.
		/// </summary>
		public uint OriginalToken {
			get { return origToken; }
			set { origToken = value; }
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateStatic(TypeSig retType) {
			return new MethodSig(CallingConvention.Default, 0, retType);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateStatic(TypeSig retType, TypeSig argType1) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateStatic(TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateStatic(TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateStatic(TypeSig retType, params TypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default, 0, retType, argTypes);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateInstance(TypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateInstance(TypeSig retType, TypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateInstance(TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateInstance(TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateInstance(TypeSig retType, params TypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argTypes);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, TypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, TypeSig retType, TypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, TypeSig retType, params TypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argTypes);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, TypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, TypeSig retType, TypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, TypeSig retType, params TypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argTypes);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodSig() {
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		public MethodSig(CallingConvention callingConvention) {
			this.callingConvention = callingConvention;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, TypeSig argType1) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1, argType2);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1, argType2, argType3);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, params TypeSig[] argTypes) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argTypes);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, IList<TypeSig> argTypes) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argTypes);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		/// <param name="paramsAfterSentinel">Parameters after sentinel</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, IList<TypeSig> argTypes, IList<TypeSig> paramsAfterSentinel) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argTypes);
			this.paramsAfterSentinel = paramsAfterSentinel == null ? null : ThreadSafeListCreator.Create<TypeSig>(paramsAfterSentinel);
		}

		/// <summary>
		/// Clone this
		/// </summary>
		public MethodSig Clone() {
			return new MethodSig(callingConvention, genParamCount, retType, parameters, paramsAfterSentinel);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullNameCreator.MethodSigFullName(this);
		}
	}

	/// <summary>
	/// A property signature
	/// </summary>
	public sealed class PropertySig : MethodBaseSig {
		/// <summary>
		/// Creates a static PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static PropertySig CreateStatic(TypeSig retType) {
			return new PropertySig(false, retType);
		}

		/// <summary>
		/// Creates a static PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static PropertySig CreateStatic(TypeSig retType, TypeSig argType1) {
			return new PropertySig(false, retType, argType1);
		}

		/// <summary>
		/// Creates a static PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static PropertySig CreateStatic(TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new PropertySig(false, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates a static PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static PropertySig CreateStatic(TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new PropertySig(false, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates a static PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static PropertySig CreateStatic(TypeSig retType, params TypeSig[] argTypes) {
			return new PropertySig(false, retType, argTypes);
		}

		/// <summary>
		/// Creates an instance PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static PropertySig CreateInstance(TypeSig retType) {
			return new PropertySig(true, retType);
		}

		/// <summary>
		/// Creates an instance PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static PropertySig CreateInstance(TypeSig retType, TypeSig argType1) {
			return new PropertySig(true, retType, argType1);
		}

		/// <summary>
		/// Creates an instance PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static PropertySig CreateInstance(TypeSig retType, TypeSig argType1, TypeSig argType2) {
			return new PropertySig(true, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates an instance PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static PropertySig CreateInstance(TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			return new PropertySig(true, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates an instance PropertySig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static PropertySig CreateInstance(TypeSig retType, params TypeSig[] argTypes) {
			return new PropertySig(true, retType, argTypes);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PropertySig() {
			this.callingConvention = CallingConvention.Property;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention (must have Property set)</param>
		internal PropertySig(CallingConvention callingConvention) {
			this.callingConvention = callingConvention;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		public PropertySig(bool hasThis) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		/// <param name="retType">Return type</param>
		public PropertySig(bool hasThis, TypeSig retType) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public PropertySig(bool hasThis, TypeSig retType, TypeSig argType1) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public PropertySig(bool hasThis, TypeSig retType, TypeSig argType1, TypeSig argType2) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1, argType2);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public PropertySig(bool hasThis, TypeSig retType, TypeSig argType1, TypeSig argType2, TypeSig argType3) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argType1, argType2, argType3);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hasThis"><c>true</c> if instance, <c>false</c> if static</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public PropertySig(bool hasThis, TypeSig retType, params TypeSig[] argTypes) {
			this.callingConvention = CallingConvention.Property | (hasThis ? CallingConvention.HasThis : 0);
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argTypes);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		/// <param name="paramsAfterSentinel">Parameters after sentinel</param>
		internal PropertySig(CallingConvention callingConvention, uint genParamCount, TypeSig retType, IList<TypeSig> argTypes, IList<TypeSig> paramsAfterSentinel) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = ThreadSafeListCreator.Create<TypeSig>(argTypes);
			this.paramsAfterSentinel = paramsAfterSentinel == null ? null : ThreadSafeListCreator.Create<TypeSig>(paramsAfterSentinel);
		}

		/// <summary>
		/// Clone this
		/// </summary>
		public PropertySig Clone() {
			return new PropertySig(callingConvention, genParamCount, retType, parameters, paramsAfterSentinel);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullNameCreator.PropertySigFullName(this);
		}
	}

	/// <summary>
	/// A local variables signature
	/// </summary>
	public sealed class LocalSig : CallingConventionSig {
		readonly ThreadSafe.IList<TypeSig> locals;

		/// <summary>
		/// All local types. This is never <c>null</c>.
		/// </summary>
		public ThreadSafe.IList<TypeSig> Locals {
			get { return locals; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LocalSig() {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention (must have LocalSig set)</param>
		/// <param name="count">Number of locals</param>
		internal LocalSig(CallingConvention callingConvention, uint count) {
			this.callingConvention = callingConvention;
			this.locals = ThreadSafeListCreator.Create<TypeSig>((int)count);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local1">Local type #1</param>
		public LocalSig(TypeSig local1) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>(local1);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local1">Local type #1</param>
		/// <param name="local2">Local type #2</param>
		public LocalSig(TypeSig local1, TypeSig local2) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>(local1, local2);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local1">Local type #1</param>
		/// <param name="local2">Local type #2</param>
		/// <param name="local3">Local type #3</param>
		public LocalSig(TypeSig local1, TypeSig local2, TypeSig local3) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>(local1, local2, local3);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals</param>
		public LocalSig(params TypeSig[] locals) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>(locals);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals</param>
		public LocalSig(IList<TypeSig> locals) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.Create<TypeSig>(locals);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locals">All locals (this instance now owns it)</param>
		/// <param name="dummy">Dummy</param>
		internal LocalSig(IList<TypeSig> locals, bool dummy) {
			this.callingConvention = CallingConvention.LocalSig;
			this.locals = ThreadSafeListCreator.MakeThreadSafe(locals);
		}

		/// <summary>
		/// Clone this
		/// </summary>
		public LocalSig Clone() {
			return new LocalSig(locals);
		}
	}

	/// <summary>
	/// An instantiated generic method signature
	/// </summary>
	public sealed class GenericInstMethodSig : CallingConventionSig {
		readonly ThreadSafe.IList<TypeSig> genericArgs;

		/// <summary>
		/// Gets the generic arguments (must be instantiated types, i.e., closed types)
		/// </summary>
		public ThreadSafe.IList<TypeSig> GenericArguments {
			get { return genericArgs; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericInstMethodSig() {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention (must have GenericInst set)</param>
		/// <param name="size">Number of generic args</param>
		internal GenericInstMethodSig(CallingConvention callingConvention, uint size) {
			this.callingConvention = callingConvention;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>((int)size);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arg1">Generic arg #1</param>
		public GenericInstMethodSig(TypeSig arg1) {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>(arg1);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arg1">Generic arg #1</param>
		/// <param name="arg2">Generic arg #2</param>
		public GenericInstMethodSig(TypeSig arg1, TypeSig arg2) {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>(arg1, arg2);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arg1">Generic arg #1</param>
		/// <param name="arg2">Generic arg #2</param>
		/// <param name="arg3">Generic arg #3</param>
		public GenericInstMethodSig(TypeSig arg1, TypeSig arg2, TypeSig arg3) {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>(arg1, arg2, arg3);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="args">Generic args</param>
		public GenericInstMethodSig(params TypeSig[] args) {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>(args);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="args">Generic args</param>
		public GenericInstMethodSig(IList<TypeSig> args) {
			this.callingConvention = CallingConvention.GenericInst;
			this.genericArgs = ThreadSafeListCreator.Create<TypeSig>(args);
		}

		/// <summary>
		/// Clone this
		/// </summary>
		public GenericInstMethodSig Clone() {
			return new GenericInstMethodSig(genericArgs);
		}
	}

	public static partial class Extensions {
		/// <summary>
		/// Gets the field type
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>Field type or <c>null</c> if none</returns>
		public static TypeSig GetFieldType(this FieldSig sig) {
			return sig == null ? null : sig.Type;
		}

		/// <summary>
		/// Gets the return type
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>Return type or <c>null</c> if none</returns>
		public static TypeSig GetRetType(this MethodBaseSig sig) {
			return sig == null ? null : sig.RetType;
		}

		/// <summary>
		/// Gets the parameters
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>The parameters</returns>
		public static IList<TypeSig> GetParams(this MethodBaseSig sig) {
			return sig == null ? ThreadSafeListCreator.Create<TypeSig>() : sig.Params;
		}

		/// <summary>
		/// Gets the parameter count
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>Parameter count</returns>
		public static int GetParamCount(this MethodBaseSig sig) {
			return sig == null ? 0 : sig.Params.Count;
		}

		/// <summary>
		/// Gets the generic parameter count
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>Generic parameter count</returns>
		public static uint GetGenParamCount(this MethodBaseSig sig) {
			return sig == null ? 0 : sig.GenParamCount;
		}

		/// <summary>
		/// Gets the parameters after the sentinel
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>Parameters after sentinel or <c>null</c> if none</returns>
		public static IList<TypeSig> GetParamsAfterSentinel(this MethodBaseSig sig) {
			return sig == null ? null : sig.ParamsAfterSentinel;
		}

		/// <summary>
		/// Gets the locals
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>All locals</returns>
		public static IList<TypeSig> GetLocals(this LocalSig sig) {
			return sig == null ? ThreadSafeListCreator.Create<TypeSig>() : sig.Locals;
		}

		/// <summary>
		/// Gets the generic arguments
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>All generic arguments</returns>
		public static IList<TypeSig> GetGenericArguments(this GenericInstMethodSig sig) {
			return sig == null ? ThreadSafeListCreator.Create<TypeSig>() : sig.GenericArguments;
		}

		/// <summary>
		/// Gets the <see cref="CallingConventionSig.IsDefault"/> property
		/// </summary>
		/// <param name="sig">this</param>
		/// <returns>The type's <see cref="CallingConventionSig.IsDefault"/> property or
		/// <c>false</c> if input is<c>null</c></returns>
		public static bool GetIsDefault(this CallingConventionSig sig) {
			return sig == null ? false : sig.IsDefault;
		}
	}
}
