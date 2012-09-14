using System.Collections.Generic;
using System.Diagnostics;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A list of all method parameters
	/// </summary>
	[DebuggerDisplay("Count = {Length}")]
	public class ParameterList {
		MethodDef method;
		List<Parameter> parameters;
		Parameter hiddenThisParameter;
		int methodSigIndexBase;

		/// <summary>
		/// Gets the number of parameters, including a possible hidden 'this' parameter
		/// </summary>
		public int Length {
			get { return parameters.Count; }
		}

		/// <summary>
		/// Gets the N'th parameter
		/// </summary>
		/// <param name="index">The parameter index</param>
		public Parameter this[int index] {
			get { return parameters[index]; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The method with all parameters</param>
		internal ParameterList(MethodDef method) {
			this.method = method;
			this.parameters = new List<Parameter>();
			this.methodSigIndexBase = -1;
			this.hiddenThisParameter = new Parameter(this, 0, -1);
			//TODO: Update hiddenThisParameter.TypeSig
			UpdateParameterTypes();
		}

		/// <summary>
		/// Should be called when the method sig has changed
		/// </summary>
		internal void UpdateParameterTypes() {
			if (method.MethodSig == null) {
				parameters.Clear();
				return;
			}
			if (UpdateThisParameter())
				parameters.Clear();
			var methodSigParams = method.MethodSig.Params;
			ResizeParameters(methodSigParams.Count);
			if (methodSigIndexBase > 0)
				parameters[0] = hiddenThisParameter;
			for (int i = 0; i < methodSigParams.Count; i++)
				parameters[i + methodSigIndexBase].TypeSig = methodSigParams[i];
		}

		bool UpdateThisParameter() {
			int newIndex;
			if (method.MethodSig == null)
				newIndex = -1;
			else
				newIndex = method.MethodSig.HasThis && !method.MethodSig.ExplicitThis ? 1 : 0;
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
			if (param.MethodSigIndex < 0)
				return null;
			foreach (var paramDef in method.ParamList) {
				if (paramDef != null && paramDef.Sequence + 1 == param.MethodSigIndex)
					return paramDef;
			}
			return null;
		}
	}

	/// <summary>
	/// A method parameter
	/// </summary>
	public class Parameter {
		ParameterList parameterList;
		ITypeSig typeSig;
		int paramIndex;
		int methodSigIndex;

		/// <summary>
		/// Gets the parameter index. If the method has a hidden 'this' parameter, that parameter
		/// has index 0 and the remaining parameters in the method signature start from index 1.
		/// </summary>
		public int Index {
			get { return paramIndex; }
		}

		/// <summary>
		/// Gets the index of the parameter in the method signature. It's -1 if it's the hidden
		/// 'this' parameter.
		/// </summary>
		public int MethodSigIndex {
			get { return methodSigIndex; }
		}

		/// <summary>
		/// Gets the parameter type
		/// </summary>
		public ITypeSig TypeSig {
			get { return typeSig; }
			internal set { typeSig = value; }
		}

		/// <summary>
		/// Gets the <see cref="dot10.DotNet.ParamDef"/> or null if not present
		/// </summary>
		public ParamDef ParamDef {
			get { return parameterList.FindParamDef(this); }
		}

		/// <summary>
		/// Gets the name from <see cref="ParamDef"/>. If <see cref="ParamDef"/> is <c>null</c>,
		/// an empty string is returned.
		/// </summary>
		public string Name {
			get {
				var paramDef = ParamDef;
				return paramDef == null || UTF8String.IsNullOrEmpty(paramDef.Name) ? string.Empty : paramDef.Name.String;
			}
		}

		internal Parameter(ParameterList parameterList, int paramIndex, int methodSigIndex) {
			this.parameterList = parameterList;
			this.paramIndex = paramIndex;
			this.methodSigIndex = methodSigIndex;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} {1}", typeSig == null ? "<<<NULL>>>" : typeSig.FullName, Name);
		}
	}
}
