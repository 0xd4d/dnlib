// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
	/// <summary>
	/// Generic parameter context
	/// </summary>
	public readonly struct GenericParamContext {
		/// <summary>
		/// Type context
		/// </summary>
		public readonly TypeDef Type;

		/// <summary>
		/// Method context
		/// </summary>
		public readonly MethodDef Method;

		/// <summary>
		/// true if <see cref="Type"/> and <see cref="Method"/> are both <c>null</c>
		/// </summary>
		public bool IsEmpty => Type is null && Method is null;

		/// <summary>
		/// Creates a new <see cref="GenericParamContext"/> instance and initializes the
		/// <see cref="Type"/> field to <paramref name="method"/>'s <see cref="MethodDef.DeclaringType"/>
		/// and the <see cref="Method"/> field to <paramref name="method"/>.
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns>A new <see cref="GenericParamContext"/> instance</returns>
		public static GenericParamContext Create(MethodDef method) {
			if (method is null)
				return new GenericParamContext();
			return new GenericParamContext(method.DeclaringType, method);
		}

		/// <summary>
		/// Creates a new <see cref="GenericParamContext"/> instance and initializes the
		/// <see cref="Type"/> field to <paramref name="type"/> and the <see cref="Method"/> field
		/// to <c>null</c>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns>A new <see cref="GenericParamContext"/> instance</returns>
		public static GenericParamContext Create(TypeDef type) => new GenericParamContext(type);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type context</param>
		public GenericParamContext(TypeDef type) {
			Type = type;
			Method = null;
		}

		/// <summary>
		/// Constructor. The <see cref="Type"/> field is set to <c>null</c> and <c>NOT</c> to
		/// <paramref name="method"/>'s <see cref="MethodDef.DeclaringType"/>. Use
		/// <see cref="Create(MethodDef)"/> if you want that behavior.
		/// </summary>
		/// <param name="method">Method context</param>
		public GenericParamContext(MethodDef method) {
			Type = null;
			Method = method;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type context</param>
		/// <param name="method">Method context</param>
		public GenericParamContext(TypeDef type, MethodDef method) {
			Type = type;
			Method = method;
		}
	}
}
