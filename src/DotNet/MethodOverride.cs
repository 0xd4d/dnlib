// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Describes which method some method implements
	/// </summary>
	public struct MethodOverride {
		/// <summary>
		/// The method body. Usually a <see cref="MethodDef"/> but could be a <see cref="MemberRef"/>
		/// </summary>
		public IMethodDefOrRef MethodBody;

		/// <summary>
		/// The method <see cref="MethodBody"/> implements
		/// </summary>
		public IMethodDefOrRef MethodDeclaration;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodBody">Method body</param>
		/// <param name="methodDeclaration">The method <paramref name="methodBody"/> implements</param>
		public MethodOverride(IMethodDefOrRef methodBody, IMethodDefOrRef methodDeclaration) {
			this.MethodBody = methodBody;
			this.MethodDeclaration = methodDeclaration;
		}
	}
}
