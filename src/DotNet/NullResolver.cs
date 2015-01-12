// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// A resolver that always fails
	/// </summary>
	public sealed class NullResolver : IAssemblyResolver, IResolver {
		/// <summary>
		/// The one and only instance of this type
		/// </summary>
		public static readonly NullResolver Instance = new NullResolver();

		NullResolver() {
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			return null;
		}

		/// <inheritdoc/>
		public bool AddToCache(AssemblyDef asm) {
			return true;
		}

		/// <inheritdoc/>
		public bool Remove(AssemblyDef asm) {
			return false;
		}

		/// <inheritdoc/>
		public void Clear() {
		}

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef, ModuleDef sourceModule) {
			return null;
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			return null;
		}
	}
}
