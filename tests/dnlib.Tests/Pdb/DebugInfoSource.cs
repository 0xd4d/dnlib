// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// A small assembly with a portable PDB for the PDB tests. It has the local variables and constants that
	/// need special debug info.
	/// </summary>
	static class DebugInfoSource {
		const string Source = @"
using System;
using System.Collections.Generic;

public class Generic<T> {
	public enum Kind : byte { None, Some }
}

public static class Code {
	public static int Sum(int count) {
		int sum = 0;
		for (int i = 0; i < count; i++) {
			int twice = i * 2;
			sum += twice;
		}
		return sum;
	}

	public static int NoLocals() {
		return 1;
	}

	public static void Constants() {
		const int[] NoArray = null;
		const List<string> NoList = null;
		const string NoString = null;
		const Generic<int>.Kind NestedInGeneric = Generic<int>.Kind.Some;
		const DayOfWeek FromAnotherAssembly = DayOfWeek.Friday;
		const decimal Price = 12.5m;
		const char Letter = 'x';
	}

	public static object SpecialLocals() {
		dynamic anything = 1;
		Dictionary<string, dynamic> map = null;
		(int first, string second) pair = (1, null);
		return (object)anything ?? (object)map ?? pair;
	}
}
";

		static readonly Lazy<CompiledAssembly> compiled = new Lazy<CompiledAssembly>(() =>
			// Not optimized: the compiler keeps all locals and their scopes
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { AssemblyName = "DebugInfo", PdbKind = PdbKind.Portable, Optimize = false }));

		/// <summary>
		/// The assembly and its portable PDB. The source file's name is <c>DebugInfo.cs</c>.
		/// </summary>
		public static CompiledAssembly Compiled => compiled.Value;

		/// <summary>
		/// Reads the assembly and its PDB. Every call returns a new module that the caller can change.
		/// </summary>
		public static ModuleDefMD Load() => RoundTrip.LoadWithPdb(Compiled.Image, Compiled.Pdb!);

		/// <summary>
		/// Gets a method of the <c>Code</c> class
		/// </summary>
		public static MethodDef GetMethod(ModuleDef module, string name) => module.Find("Code", isReflectionName: true)!.FindMethod(name)!;

		/// <summary>
		/// Gets the outermost scope of a method of the <c>Code</c> class
		/// </summary>
		public static PdbScope GetRootScope(ModuleDef module, string methodName) => GetMethod(module, methodName).Body!.PdbMethod!.Scope!;

		/// <summary>
		/// Gets a local variable of a method of the <c>Code</c> class. It can be in any scope.
		/// </summary>
		public static PdbLocal GetLocal(ModuleDef module, string methodName, string localName) =>
			GetScopes(GetRootScope(module, methodName)).SelectMany(a => a.Variables).Single(a => a.Name == localName);

		/// <summary>
		/// Gets a scope and all scopes in it, outer scopes first
		/// </summary>
		public static IEnumerable<PdbScope> GetScopes(PdbScope scope) {
			yield return scope;
			foreach (var child in scope.Scopes) {
				foreach (var nested in GetScopes(child))
					yield return nested;
			}
		}
	}
}
