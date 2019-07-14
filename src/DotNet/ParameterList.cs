// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.Threading;
using dnlib.Utils;

namespace dnlib.DotNet {
	/// <summary>
	/// A list of all method parameters
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(ParameterList_CollectionDebugView))]
	public sealed class ParameterList : IList<Parameter> {
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
		public MethodDef Method => method;

		/// <summary>
		/// Gets the number of parameters, including a possible hidden 'this' parameter
		/// </summary>
		public int Count {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return parameters.Count;
#if THREAD_SAFE
				}
				finally { theLock.ExitReadLock(); }
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
#endif
				return parameters[index];
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set => throw new NotSupportedException();
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
			parameters = new List<Parameter>();
			methodSigIndexBase = -1;
			hiddenThisParameter = new Parameter(this, 0, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX);
			returnParameter = new Parameter(this, -1, Parameter.RETURN_TYPE_METHOD_SIG_INDEX);
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
			if (methodDeclaringType is null)
				hiddenThisParameter.Type = null;
			else if (methodDeclaringType.IsValueType)
				hiddenThisParameter.Type = new ByRefSig(new ValueTypeSig(methodDeclaringType));
			else
				hiddenThisParameter.Type = new ClassSig(methodDeclaringType);
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
			if (sig is null) {
				methodSigIndexBase = -1;
				parameters.Clear();
				return;
			}
			if (UpdateThisParameter_NoLock(sig))
				parameters.Clear();
			returnParameter.Type = sig.RetType;
			ResizeParameters_NoLock(sig.Params.Count + methodSigIndexBase);
			if (methodSigIndexBase > 0)
				parameters[0] = hiddenThisParameter;
			for (int i = 0; i < sig.Params.Count; i++)
				parameters[i + methodSigIndexBase].Type = sig.Params[i];
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		bool UpdateThisParameter_NoLock(MethodSig methodSig) {
			int newIndex;
			if (methodSig is null)
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

			var paramDefs = method.ParamDefs;
			int count = paramDefs.Count;
			for (int i = 0; i < count; i++) {
				var paramDef = paramDefs[i];
				if (!(paramDef is null) && paramDef.Sequence == seq)
					return paramDef;
			}
			return null;
		}

		internal void TypeUpdated(Parameter param) {
			var sig = method.MethodSig;
			if (sig is null)
				return;
			int index = param.MethodSigIndex;
			if (index == Parameter.RETURN_TYPE_METHOD_SIG_INDEX)
				sig.RetType = param.Type;
			else if (index >= 0)
				sig.Params[index] = param.Type;
		}

		internal void CreateParamDef(Parameter param) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var paramDef = FindParamDef_NoLock(param);
			if (!(paramDef is null))
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
			if (dt is null)
				return pd;
			var module = dt.Module;
			if (module is null)
				return pd;
			return module.UpdateRowId(pd);
		}

		/// <inheritdoc/>
		public int IndexOf(Parameter item) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return parameters.IndexOf(item);
#if THREAD_SAFE
			}
			finally { theLock.ExitReadLock(); }
#endif
		}

		void IList<Parameter>.Insert(int index, Parameter item) => throw new NotSupportedException();
		void IList<Parameter>.RemoveAt(int index) => throw new NotSupportedException();
		void ICollection<Parameter>.Add(Parameter item) => throw new NotSupportedException();
		void ICollection<Parameter>.Clear() => throw new NotSupportedException();

		bool ICollection<Parameter>.Contains(Parameter item) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return parameters.Contains(item);
#if THREAD_SAFE
			}
			finally { theLock.ExitReadLock(); }
#endif
		}

		void ICollection<Parameter>.CopyTo(Parameter[] array, int arrayIndex) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			parameters.CopyTo(array, arrayIndex);
#if THREAD_SAFE
			}
			finally { theLock.ExitReadLock(); }
#endif
		}

		bool ICollection<Parameter>.IsReadOnly => true;
		bool ICollection<Parameter>.Remove(Parameter item) => throw new NotSupportedException();

		/// <summary>
		/// Enumerator
		/// </summary>
		public struct Enumerator : IEnumerator<Parameter> {
			readonly ParameterList list;
			List<Parameter>.Enumerator listEnumerator;
			Parameter current;

			internal Enumerator(ParameterList list) {
				this.list = list;
				current = default;
#if THREAD_SAFE
				list.theLock.EnterReadLock(); try {
#endif
				listEnumerator = list.parameters.GetEnumerator();
#if THREAD_SAFE
				} finally { list.theLock.ExitReadLock(); }
#endif
			}

			/// <summary>
			/// Gets the current value
			/// </summary>
			public Parameter Current => current;
			Parameter IEnumerator<Parameter>.Current => current;
			object System.Collections.IEnumerator.Current => current;

			/// <summary>
			/// Moves to the next element in the collection
			/// </summary>
			/// <returns></returns>
			public bool MoveNext() {
#if THREAD_SAFE
				list.theLock.EnterWriteLock(); try {
#endif
				var res = listEnumerator.MoveNext();
				current = listEnumerator.Current;
				return res;
#if THREAD_SAFE
				} finally { list.theLock.ExitWriteLock(); }
#endif
			}

			/// <summary>
			/// Disposes the enumerator
			/// </summary>
			public void Dispose() => listEnumerator.Dispose();

			void System.Collections.IEnumerator.Reset() => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the list enumerator
		/// </summary>
		/// <returns></returns>
		public Enumerator GetEnumerator() => new Enumerator(this);
		IEnumerator<Parameter> IEnumerable<Parameter>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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
		public int Index => paramIndex;

		/// <summary>
		/// Gets the index of the parameter in the method signature. See also
		/// <see cref="HIDDEN_THIS_METHOD_SIG_INDEX"/> and <see cref="RETURN_TYPE_METHOD_SIG_INDEX"/>
		/// </summary>
		public int MethodSigIndex => methodSigIndex;

		/// <summary>
		/// <c>true</c> if it's a normal visible method parameter, i.e., it's not the hidden
		/// 'this' parameter and it's not the method return type parameter.
		/// </summary>
		public bool IsNormalMethodParameter => methodSigIndex >= 0;

		/// <summary>
		/// <c>true</c> if it's the hidden 'this' parameter
		/// </summary>
		public bool IsHiddenThisParameter => methodSigIndex == HIDDEN_THIS_METHOD_SIG_INDEX;

		/// <summary>
		/// <c>true</c> if it's the method return type parameter
		/// </summary>
		public bool IsReturnTypeParameter => methodSigIndex == RETURN_TYPE_METHOD_SIG_INDEX;

		/// <summary>
		/// Gets the parameter type
		/// </summary>
		public TypeSig Type {
			get => typeSig;
			set {
				typeSig = value;
				if (!(parameterList is null))
					parameterList.TypeUpdated(this);
			}
		}

		/// <summary>
		/// Gets the owner method
		/// </summary>
		public MethodDef Method => parameterList?.Method;

		/// <summary>
		/// Gets the <see cref="dnlib.DotNet.ParamDef"/> or <c>null</c> if not present
		/// </summary>
		public ParamDef ParamDef => parameterList?.FindParamDef(this);

		/// <summary>
		/// <c>true</c> if it has a <see cref="dnlib.DotNet.ParamDef"/>
		/// </summary>
		public bool HasParamDef => !(ParamDef is null);

		/// <summary>
		/// Gets the name from <see cref="ParamDef"/>. If <see cref="ParamDef"/> is <c>null</c>,
		/// an empty string is returned.
		/// </summary>
		public string Name {
			get {
				var paramDef = ParamDef;
				return paramDef is null ? string.Empty : UTF8String.ToSystemStringOrEmpty(paramDef.Name);
			}
			set {
				var paramDef = ParamDef;
				if (!(paramDef is null))
					paramDef.Name = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index</param>
		public Parameter(int paramIndex) {
			this.paramIndex = paramIndex;
			methodSigIndex = paramIndex;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paramIndex">Parameter index</param>
		/// <param name="type">Parameter type</param>
		public Parameter(int paramIndex, TypeSig type) {
			this.paramIndex = paramIndex;
			methodSigIndex = paramIndex;
			typeSig = type;
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
			typeSig = type;
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
			if (!(parameterList is null))
				parameterList.CreateParamDef(this);
		}

		/// <inheritdoc/>
		public override string ToString() {
			var name = Name;
			if (string.IsNullOrEmpty(name)) {
				if (IsReturnTypeParameter)
					return "RET_PARAM";
				return $"A_{paramIndex}";
			}
			return name;
		}
	}
}
