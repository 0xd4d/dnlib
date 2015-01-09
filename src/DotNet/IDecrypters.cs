// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.PE;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet {
	/// <summary>
	/// Interface to decrypt methods
	/// </summary>
	public interface IMethodDecrypter {
		/// <summary>
		/// Gets the method's body
		/// </summary>
		/// <param name="rid"><c>Method</c> rid</param>
		/// <param name="rva">The <see cref="RVA"/> found in the method's <c>Method</c> row</param>
		/// <param name="parameters">The method's parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="methodBody">Updated with the method's <see cref="MethodBody"/> if this
		/// method returns <c>true</c></param>
		/// <returns><c>true</c> if the method body was decrypted, <c>false</c> if the method isn't
		/// encrypted and the default <see cref="MethodDef"/> body reader code should be used.</returns>
		bool GetMethodBody(uint rid, RVA rva, IList<Parameter> parameters, GenericParamContext gpContext, out MethodBody methodBody);
	}

	/// <summary>
	/// Interface to decrypt strings
	/// </summary>
	public interface IStringDecrypter {
		/// <summary>
		/// Reads a string
		/// </summary>
		/// <param name="token">String token</param>
		/// <returns>A string or <c>null</c> if we should read it from the #US heap</returns>
		string ReadUserString(uint token);
	}
}
