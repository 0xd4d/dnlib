using System.Collections.Generic;
using dot10.PE;
using dot10.DotNet.MD;
using dot10.DotNet.Emit;

namespace dot10.DotNet {
	/// <summary>
	/// Interface to decrypt methods
	/// </summary>
	public interface IMethodDecrypter {
		/// <summary>
		/// Checks whether <see cref="GetMethodBody"/> can be called
		/// </summary>
		/// <param name="rid"><c>Method</c> rid</param>
		/// <returns><c>true</c> if <see cref="GetMethodBody"/> can be called, <c>false</c>
		/// otherwise. If <c>false</c>, the normal method body parser code is called.</returns>
		bool HasMethodBody(uint rid);

		/// <summary>
		/// Gets the method's body
		/// </summary>
		/// <param name="rid"><c>Method</c> rid</param>
		/// <param name="rva">The <see cref="RVA"/> found in the method's <c>Method</c> row</param>
		/// <param name="parameters">The method's parameters</param>
		/// <returns>The method's <see cref="MethodBody"/></returns>
		MethodBody GetMethodBody(uint rid, RVA rva, IList<Parameter> parameters);
	}
}
