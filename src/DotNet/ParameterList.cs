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
using System.Diagnostics;

namespace dnlib.DotNet {
	/// <summary>
	/// A list of all method parameters
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ParameterList : IList<Parameter> {
		MethodDef method;
		List<Parameter> parameters;
		Parameter hiddenThisParameter;
		ParamDef hiddenThisParamDef;
		Parameter returnParameter;
		int methodSigIndexBase;

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
			get { return parameters.Count; }
		}

		/// <summary>
		/// Gets the index of the first parameter that is present in the method signature.
		/// If this is a static method, the value is 0, else it's an instance method so the
		/// index is 1 since the first parameter is the hidden 'this' parameter.
		/// </summary>
		public int MethodSigIndexBase {
			get { return methodSigIndexBase == 1 ? 1 : 0; }
		}

		/// <summary>
		/// Gets the N'th parameter
		/// </summary>
		/// <param name="index">The parameter index</param>
		public Parameter this[int index] {
			get { return parameters[index]; }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the method return parameter
		/// </summary>
		public Parameter ReturnParameter {
			get { return returnParameter; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The method with all parameters</param>
		internal ParameterList(MethodDef method) {
			this.method = method;
			this.parameters = new List<Parameter>();
			this.methodSigIndexBase = -1;
			this.hiddenThisParameter = new Parameter(this, 0, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX);
			this.returnParameter = new Parameter(this, -1, Parameter.RETURN_TYPE_METHOD_SIG_INDEX);
			UpdateThisParameterType();
			UpdateParameterTypes();
		}

		/// <summary>
		/// Should be called when the method's declaring type has changed
		/// </summary>
		public void UpdateThisParameterType() {
			var declaringType = method.DeclaringType;
			if (declaringType == null)
				return;

			if (declaringType.IsValueType)
				hiddenThisParameter.Type = new ByRefSig(new ValueTypeSig(declaringType));
			else
				hiddenThisParameter.Type = new ClassSig(declaringType);
		}

		/// <summary>
		/// Should be called when the method sig has changed
		/// </summary>
		public void UpdateParameterTypes() {
			if (method.MethodSig == null) {
				parameters.Clear();
				return;
			}
			if (UpdateThisParameter())
				parameters.Clear();
			returnParameter.Type = method.MethodSig.RetType;
			var methodSigParams = method.MethodSig.Params;
			ResizeParameters(methodSigParams.Count + methodSigIndexBase);
			if (methodSigIndexBase > 0)
				parameters[0] = hiddenThisParameter;
			for (int i = 0; i < methodSigParams.Count; i++)
				parameters[i + methodSigIndexBase].Type = methodSigParams[i];
		}

		bool UpdateThisParameter() {
			int newIndex;
			if (method.MethodSig == null)
				newIndex = -1;
			else
				newIndex = method.MethodSig.ImplicitThis ? 1 : 0;
			if (methodSigIndexBase == newIndex)
				return false;
			methodSigIndexBase = newIndex;
			return true;
		}

		void ResizeParameters(int length) {
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
			int seq;
			if (param.IsReturnTypeParameter)
				seq = 0;
			else if (param.IsNormalMethodParameter)
				seq = param.MethodSigIndex + 1;
			else
				return hiddenThisParamDef;

			foreach (var paramDef in method.ParamDefs) {
				if (paramDef != null && paramDef.Sequence == seq)
					return paramDef;
			}
			return null;
		}

		internal void TypeUpdated(Parameter param) {
			int index = param.MethodSigIndex;
			if (index == Parameter.RETURN_TYPE_METHOD_SIG_INDEX)
				method.MethodSig.RetType = param.Type;
			else if (index >= 0)
				method.MethodSig.Params[index] = param.Type;
		}

		internal void CreateParamDef(Parameter param) {
			var paramDef = FindParamDef(param);
			if (paramDef != null)
				return;
			if (param.IsHiddenThisParameter) {
				hiddenThisParamDef = UpdateRowId(new ParamDefUser(UTF8String.Empty, ushort.MaxValue, 0));
				return;
			}
			int seq = param.IsReturnTypeParameter ? 0 : param.MethodSigIndex + 1;
			paramDef = UpdateRowId(new ParamDefUser(UTF8String.Empty, (ushort)seq, 0));
			method.ParamDefs.Add(paramDef);
		}

		ParamDef UpdateRowId(ParamDef pd) {
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
			return parameters.IndexOf(item);
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
			return parameters.Contains(item);
		}

		void ICollection<Parameter>.CopyTo(Parameter[] array, int arrayIndex) {
			parameters.CopyTo(array, arrayIndex);
		}

		int ICollection<Parameter>.Count {
			get { return parameters.Count; }
		}

		bool ICollection<Parameter>.IsReadOnly {
			get { return true; }
		}

		bool ICollection<Parameter>.Remove(Parameter item) {
			throw new NotSupportedException();
		}

		IEnumerator<Parameter> IEnumerable<Parameter>.GetEnumerator() {
			return parameters.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return ((IEnumerable<Parameter>)this).GetEnumerator();
		}
	}

	/// <summary>
	/// A method parameter
	/// </summary>
	public sealed class Parameter : IVariable {
		ParameterList parameterList;
		TypeSig typeSig;
		int paramIndex;
		int methodSigIndex;

		/// <summary>
		/// The hidden 'this' parameter's <see cref="MethodSigIndex"/>
		/// </summary>
		public const int HIDDEN_THIS_METHOD_SIG_INDEX = -2;

		/// <summary>
		/// The return type parameter's <see cref="MethodSigIndex"/>
		/// </summary>
		public const int RETURN_TYPE_METHOD_SIG_INDEX = -1;

		/// <summary>
		/// <c>true</c> if this <see cref="Parameter"/> is inserted in a <see cref="ParameterList"/>
		/// </summary>
		public bool IsInserted {
			get { return parameterList != null; }
		}

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
					parameterList.TypeUpdated(this);
			}
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
			if (string.IsNullOrEmpty(Name)) {
				if (IsReturnTypeParameter)
					return "RET_PARAM";
				return string.Format("A_{0}", paramIndex);
			}
			return Name;
		}
	}
}
