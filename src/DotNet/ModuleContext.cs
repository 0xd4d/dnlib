// dnlib: See LICENSE.txt for more info

using System.Threading;

﻿namespace dnlib.DotNet {
	/// <summary>
	/// <see cref="ModuleDef"/> context
	/// </summary>
	public class ModuleContext {
		IAssemblyResolver assemblyResolver;
		IResolver resolver;

		/// <summary>
		/// Gets/sets the assembly resolver. This is never <c>null</c>.
		/// </summary>
		public IAssemblyResolver AssemblyResolver {
			get {
				if (assemblyResolver == null)
					Interlocked.CompareExchange(ref assemblyResolver, NullResolver.Instance, null);
				return assemblyResolver;
			}
			set { assemblyResolver = value; }
		}

		/// <summary>
		/// Gets/sets the resolver. This is never <c>null</c>.
		/// </summary>
		public IResolver Resolver {
			get {
				if (resolver == null)
					Interlocked.CompareExchange(ref resolver, NullResolver.Instance, null);
				return resolver;
			}
			set { resolver = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleContext() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">Assembly resolver or <c>null</c></param>
		public ModuleContext(IAssemblyResolver assemblyResolver)
			: this(assemblyResolver, new Resolver(assemblyResolver)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resolver">Type/method/field resolver or <c>null</c></param>
		public ModuleContext(IResolver resolver)
			: this(null, resolver) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">Assembly resolver or <c>null</c></param>
		/// <param name="resolver">Type/method/field resolver or <c>null</c></param>
		public ModuleContext(IAssemblyResolver assemblyResolver, IResolver resolver) {
			this.assemblyResolver = assemblyResolver;
			this.resolver = resolver;
			if (resolver == null && assemblyResolver != null)
				this.resolver = new Resolver(assemblyResolver);
		}
	}
}
