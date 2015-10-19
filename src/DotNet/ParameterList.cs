// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A list of all method parameters
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ParameterList : ThreadSafe.IList<Parameter> {
		readonly MethodDef method;
		readonly List<Parameter> parameters;
		readonly Parameter hiddenThisParameter;
		ParamDef hiddenThisParamDef;
		readonly Parameter returnParameter;
		int methodSigIndexBase;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets the owner method
		/// </summary>
		public MethodDef Method {
			get { return method; }
		}

		/// <summary>
		/// Gets the number of parameters, including a possible hidden 'this' parameter
		/// </summary>
		public int Count {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
					return ((ThreadSafe.IList<Parameter>)this).Count_NoLock;
				} finally { theLock.ExitReadLock(); }
#else
				return parameters.Count;
#endif
			}
		}

		/// <summary>
		/// Gets the index of the first parameter that is present in the method signature.
		/// If this is a static method, the value is 0, else it's an instance method so the
		/// index is 1 since the first parameter is the hidden 'this' parameter.
		/// </summary>
		public int MethodSigIndexBase {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return methodSigIndexBase == 1 ? 1 : 0;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
		}

		/// <summary>
		/// Gets the N'th parameter
		/// </summary>
		/// <param name="index">The parameter index</param>
		public Parameter this[int index] {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
					return ((ThreadSafe.IList<Parameter>)this).Get_NoLock(index);
				} finally { theLock.ExitReadLock(); }
#else
				return parameters[index];
#endif
			}
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the method return parameter
		/// </summary>
		public Parameter ReturnParameter {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return returnParameter;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The method with all parameters</param>
		/// <param name="declaringType"><paramref name="method"/>'s declaring type</param>
		public ParameterList(MethodDef method, TypeDef declaringType) {
			this.method = method;
			this.parameters = new List<Parameter>();
			this.methodSigIndexBase = -1;
			this.hiddenThisParameter = new Parameter(this, 0, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX);
			this.returnParameter = new Parameter(this, -1, Parameter.RETURN_TYPE_METHOD_SIG_INDEX);
			UpdateThisParameterType(declaringType);
			UpdateParameterTypes();
		}

		/// <summary>
		/// Should be called when the method's declaring type has changed
		/// </summary>
		/// <param name="methodDeclaringType">Method declaring type</param>
		internal void UpdateThisParameterType(TypeDef methodDeclaringType) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (methodDeclaringType == null)
				hiddenThisParameter.SetType(false, null);
			else if (methodDeclaringType.IsValueType)
				hiddenThisParameter.SetType(false, new ByRefSig(new ValueTypeSig(methodDeclaringType)));
			else
				hiddenThisParameter.SetType(false, new ClassSig(methodDeclaringType));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Should be called when the method sig has changed
		/// </summary>
		public void UpdateParameterTypes() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var sig = method.MethodSig;
			if (sig == null) {
				methodSigIndexBase = -1;
				parameters.Clear();
				return;
			}
			if (UpdateThisParameter_NoLock(sig))
				parameters.Clear();
			returnParameter.SetType(false, sig.RetType);
			sig.Params.ExecuteLocked<TypeSig, object, object>(null, (tsList, arg) => {
				ResizeParameters_NoLock(tsList.Count_NoLock() + methodSigIndexBase);
				if (methodSigIndexBase > 0)
					parameters[0] = hiddenThisParameter;
				for (int i = 0; i < tsList.Count_NoLock(); i++)
					parameters[i + methodSigIndexBase].SetType(true, tsList.Get_NoLock(i));
				return null;
			});
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		bool UpdateThisParameter_NoLock(MethodSig methodSig) {
			int newIndex;
			if (methodSig == null)
				newIndex = -1;
			else
				newIndex = methodSig.ImplicitThis ? 1 : 0;
			if (methodSigIndexBase == newIndex)
				return false;
			methodSigIndexBase = newIndex;
			return true;
		}

		void ResizeParameters_NoLock(int length) {
			if (parameters.Count == length)
				return;
			if (parameters.Count < length) {
				for (int i = parameters.Count; i < length; i++)
					parameters.Add(new Parameter(this, i, i - methodSigIndexBase));
			}
			else {
				while (parameters.Count > length)
					parameters.RemoveAt(parameters.Count - 1);
			}
		}

		internal ParamDef FindParamDef(Parameter param) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return FindParamDef_NoLock(param);
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
		}

		ParamDef FindParamDef_NoLock(Parameter param) {
			int seq;
			if (param.IsReturnTypeParameter)
				seq = 0;
			else if (param.IsNormalMethodParameter)
				seq = param.MethodSigIndex + 1;
			else
				return hiddenThisParamDef;

			foreach (var paramDef in method.ParamDefs.GetSafeEnumerable()) {
				if (paramDef != null && paramDef.Sequence == seq)
					return paramDef;
			}
			return null;
		}

		internal void TypeUpdated(Parameter param, bool noParamsLock) {
			var sig = method.MethodSig;
			if (sig == null)
				return;
			int index = param.MethodSigIndex;
			if (index == Parameter.RETURN_TYPE_METHOD_SIG_INDEX)
				sig.RetType = param.Type;
			else if (index >= 0) {
				if (noParamsLock)
					sig.Params.Set_NoLock(index, param.Type);
				else
					sig.Params.Set(index, param.Type);
			}
		}

		internal void CreateParamDef(Parameter param) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var paramDef = FindParamDef_NoLock(param);
			if (paramDef != null)
				return;
			if (param.IsHiddenThisParameter) {
				hiddenThisParamDef = UpdateRowId_NoLock(new ParamDefUser(UTF8String.Empty, ushort.MaxValue, 0));
				return;
			}
			int seq = param.IsReturnTypeParameter ? 0 : param.MethodSigIndex + 1;
			paramDef = UpdateRowId_NoLock(new ParamDefUser(UTF8String.Empty, (ushort)seq, 0));
			method.ParamDefs.Add(paramDef);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		ParamDef UpdateRowId_NoLock(ParamDef pd) {
			var dt = method.DeclaringType;
			if (dt == null)
				return pd;
			var module = dt.Module;
			if (module == null)
				return pd;
			return module.UpdateRowId(pd);
		}

		/// <inheritdoc/>
		public int IndexOf(Parameter item) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
				return ((ThreadSafe.IList<Parameter>)this).IndexOf_NoLock(item);
			} finally { theLock.ExitReadLock(); }
#else
			return parameters.IndexOf(item);
#endif
		}

		void IList<Parameter>.Insert(int index, Parameter item) {
			throw new NotSupportedException();
		}

		void IList<Parameter>.RemoveAt(int index) {
			throw new NotSupportedException();
		}

		void ICollection<Parameter>.Add(Parameter item) {
			throw new NotSupportedException();
		}

		void ICollection<Parameter>.Clear() {
			throw new NotSupportedException();
		}

		bool ICollection<Parameter>.Contains(Parameter item) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
				return ((ThreadSafe.IList<Parameter>)this).Contains_NoLock(item);
			} finally { theLock.ExitReadLock(); }
#else
			return parameters.Contains(item);
#endif
		}

		void ICollection<Parameter>.CopyTo(Parameter[] array, int arrayIndex) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
				((ThreadSafe.IList<Parameter>)this).CopyTo_NoLock(array, arrayIndex);
			} finally { theLock.ExitReadLock(); }
#else
			parameters.CopyTo(array, arrayIndex);
#endif
		}

		bool ICollection<Parameter>.IsReadOnly {
			get { return true; }
		}

		bool ICollection<Parameter>.Remove(Parameter item) {
			throw new NotSupportedException();
		}

		IEnumerator<Parameter> IEnumerable<Parameter>.GetEnumerator() {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
				return ((ThreadSafe.IList<Parameter>)this).GetEnumerator_NoLock();
			} finally { theLock.ExitReadLock(); }
#else
			return parameters.GetEnumerator();
#endif
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return ((IEnumerable<Parameter>)this).GetEnumerator();
		}

#if THREAD_SAFE
		int ThreadSafe.IList<Parameter>.IndexOf_NoLock(Parameter item) {
			return parameters.IndexOf(item);
		}

		void ThreadSafe.IList<Parameter>.Insert_NoLock(int index, Parameter item) {
			throw new NotSupportedException();
		}

		void ThreadSafe.IList<Parameter>.RemoveAt_NoLock(int index) {
			throw new NotSupportedException();
		}

		Parameter ThreadSafe.IList<Parameter>.Get_NoLock(int index) {
			return parameters[index];
		}

		void ThreadSafe.IList<Parameter>.Set_NoLock(int index, Parameter value) {
			throw new NotSupportedException();
		}

		void ThreadSafe.IList<Parameter>.Add_NoLock(Parameter item) {
			throw new NotSupportedException();
		}

		void ThreadSafe.IList<Parameter>.Clear_NoLock() {
			throw new NotSupportedException();
		}

		bool ThreadSafe.IList<Parameter>.Contains_NoLock(Parameter item) {
			return parameters.Contains(item);
		}

		void ThreadSafe.IList<Parameter>.CopyTo_NoLock(Parameter[] array, int arrayIndex) {
			parameters.CopyTo(array, arrayIndex);
		}

		bool ThreadSafe.IList<Parameter>.Remove_NoLock(Parameter item) {
			throw new NotSupportedException();
		}

		IEnumerator<Parameter> ThreadSafe.IList<Parameter>.GetEnumerator_NoLock() {
			return parameters.GetEnumerator();
		}

		int ThreadSafe.IList<Parameter>.Count_NoLock {
			get { return parameters.Count; }
		}

		bool ThreadSafe.IList<Parameter>.IsReadOnly_NoLock {
			get { return true; }
		}

		TRetType ThreadSafe.IList<Parameter>.ExecuteLocked<TArgType, TRetType>(TArgType arg, ExecuteLockedDelegate<Parameter, TArgType, TRetType> handler) {
			theLock.EnterWriteLock(); try {
				return handler(this, arg);
			} finally { theLock.ExitWriteLock(); }
		}
#endif
	}

	/// <summary>
	/// A method parameter
	/// </summary>
	public sealed class Parameter : IVariable {
		readonly ParameterList parameterList;
		TypeSig typeSig;
		readonly int paramIndex;
		readonly int methodSigIndex;

		/// <summary>
		/// The hidden 'this' parameter's <see cref="MethodSigIndex"/>
		/// </summary>
		public const int HIDDEN_THIS_METHOD_SIG_INDEX = -2;

		/// <summary>
		/// The return type parameter's <see cref="MethodSigIndex"/>
		/// </summary>
		public const int RETURN_TYPE_METHOD_SIG_INDEX = -1;

		/// <summary>
		/// Gets the parameter index. If the method has a hidden 'this' parameter, that parameter
		/// has index 0 and the remaining parameters in the method signature start from index 1.
		/// The method return parameter has index <c>-1</c>.
		/// </summary>
		public int Index {
			get { return paramIndex; }
		}

		/// <summary>
		/// Gets the index of the parameter in the method signature. See also
		/// <see cref="HIDDEN_THIS_METHOD_SIG_INDEX"/> and <see cref="RETURN_TYPE_METHOD_SIG_INDEX"/>
		/// </summary>
		public int MethodSigIndex {
			get { return methodSigIndex; }
		}

		/// <summary>
		/// <c>true</c> if it's a normal visible method parameter, i.e., it's not the hidden
		/// 'this' parameter and it's not the method return type parameter.
		/// </summary>
		public bool IsNormalMethodParameter {
			get { return methodSigIndex >= 0; }
		}

		/// <summary>
		/// <c>true</c> if it's the hidden 'this' parameter
		/// </summary>
		public bool IsHiddenThisParameter {
			get { return methodSigIndex == HIDDEN_THIS_METHOD_SIG_INDEX; }
		}

		/// <summary>
		/// <c>true</c> if it's the method return type parameter
		/// </summary>
		public bool IsReturnTypeParameter {
			get { return methodSigIndex == RETURN_TYPE_METHOD_SIG_INDEX; }
		}

		/// <summary>
		/// Gets the parameter type
		/// </summary>
		public TypeSig Type {
			get { return typeSig; }
			set {
				typeSig = value;
				if (parameterList != null)
					parameterList.TypeUpdated(this, false);
			}
		}

		/// <summary>
		/// This method does exactly what the <see cref="Type"/> setter does except that it
		/// uses the no-lock method if <paramref name="noParamsLock"/> is <c>true</c>.
		/// </summary>
		/// <param name="noParamsLock"><c>true</c> if <c>MethodSig.Params</c> lock is being held by
		/// us</param>
		/// <param name="type"></param>
		internal void SetType(bool noParamsLock, TypeSig type) {
			typeSig = type;
			if (parameterList != null)
				parameterList.TypeUpdated(this, noParamsLock);
		}

		/// <summary>
		/// Gets the owner method
		/// </summary>
		public MethodDef Method {
			get { return parameterList == null ? null : parameterList.Method; }
		}

		/// <summary>
		/// Gets the <see cref="dnlib.DotNet.ParamDef"/> or <c>null</c> if not present
		/// </summary>
		public ParamDef ParamDef {
			get { return parameterList == null ? null : parameterList.FindParamDef(this); }
		}

		/// <summary>
		/// <c>true</c> if it has a <see cref="dnlib.DotNet.ParamDef"/>
		/// </summary>
		public bool HasParamDef {
			get { return ParamDef != null; }
		}

		/// <summary>
		/// Gets the name from <see cref="ParamDef"/>. If <see cref="ParamDef"/> is <c>null</c>,
		/// an empty string is returned.
		/// </summary>
		public string Name {
			get {
				var paramDef = ParamDef;
				return paramDef == null ? string.Empty : UTF8String.ToSystemStringOrEmpty(paramDef.Name);
			}
			set {
				var paramDef = ParamDef;
				if (paramDef != null)
					paramDef.Name = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index</param>
		public Parameter(int paramIndex) {
			this.paramIndex = paramIndex;
			this.methodSigIndex = paramIndex;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index</param>
		/// <param name="type">Parameter type</param>
		public Parameter(int paramIndex, TypeSig type) {
			this.paramIndex = paramIndex;
			this.methodSigIndex = paramIndex;
			this.typeSig = type;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index (0 is hidden this param if it exists)</param>
		/// <param name="methodSigIndex">Index in method signature</param>
		public Parameter(int paramIndex, int methodSigIndex) {
			this.paramIndex = paramIndex;
			this.methodSigIndex = methodSigIndex;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index (0 is hidden this param if it exists)</param>
		/// <param name="methodSigIndex">Index in method signature</param>
		/// <param name="type">Parameter type</param>
		public Parameter(int paramIndex, int methodSigIndex, TypeSig type) {
			this.paramIndex = paramIndex;
			this.methodSigIndex = methodSigIndex;
			this.typeSig = type;
		}

		internal Parameter(ParameterList parameterList, int paramIndex, int methodSigIndex) {
			this.parameterList = parameterList;
			this.paramIndex = paramIndex;
			this.methodSigIndex = methodSigIndex;
		}

		/// <summary>
		/// Creates a <see cref="dnlib.DotNet.ParamDef"/> if it doesn't already exist
		/// </summary>
		public void CreateParamDef() {
			if (parameterList != null)
				parameterList.CreateParamDef(this);
		}

		/// <inheritdoc/>
		public override string ToString() {
			var name = Name;
			if (string.IsNullOrEmpty(name)) {
				if (IsReturnTypeParameter)
					return "RET_PARAM";
				return string.Format("A_{0}", paramIndex);
			}
			return name;
		}
	}
}
