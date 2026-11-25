// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Thrown by <see cref="ModuleComparer"/> when two modules aren't the same
	/// </summary>
	public sealed class ModuleDifferenceException : Exception {
		public ModuleDifferenceException(string message) : base(message) { }
	}

	/// <summary>
	/// Checks that two modules are the same by comparing their dumps (<see cref="ModuleDumper"/>).
	/// It's used to check that nothing is lost or changed when a module is written and read again.
	/// </summary>
	public static class ModuleComparer {
		/// <summary>
		/// Throws a <see cref="ModuleDifferenceException"/> that shows the first difference if the modules
		/// aren't the same
		/// </summary>
		/// <param name="expected">Eg. the original module</param>
		/// <param name="actual">Eg. the module that was written and read again</param>
		/// <param name="options">What to compare or <c>null</c> to use the default options</param>
		public static void AssertEqual(ModuleDef expected, ModuleDef actual, ModuleDumpOptions? options = null) {
			AssertSameDump("module header", ModuleDumper.DumpModuleHeader(expected, options), ModuleDumper.DumpModuleHeader(actual, options));

			// One type at a time, so the dump of a big module is never in memory as one string
			using (var expectedTypes = expected.GetTypes().GetEnumerator())
			using (var actualTypes = actual.GetTypes().GetEnumerator()) {
				for (int index = 0; ; index++) {
					bool hasExpected = expectedTypes.MoveNext();
					bool hasActual = actualTypes.MoveNext();
					if (!hasExpected && !hasActual)
						break;
					if (!hasExpected)
						throw new ModuleDifferenceException($"Extra type: {actualTypes.Current.FullName} (the expected module has {index} types)");
					if (!hasActual)
						throw new ModuleDifferenceException($"Missing type: {expectedTypes.Current.FullName} (the actual module has {index} types)");
					AssertSameDump($"type #{index} {expectedTypes.Current.FullName}",
						ModuleDumper.DumpType(expectedTypes.Current, options), ModuleDumper.DumpType(actualTypes.Current, options));
				}
			}
		}

		static void AssertSameDump(string what, string expected, string actual) {
			if (expected == actual)
				return;
			var expectedLines = expected.Split('\n');
			var actualLines = actual.Split('\n');
			int line = 0;
			while (line < expectedLines.Length && line < actualLines.Length && expectedLines[line] == actualLines[line])
				line++;

			var message = new StringBuilder();
			message.AppendLine($"Difference in {what}, line {line + 1} of its dump:");
			message.AppendLine("  expected: " + GetLine(expectedLines, line));
			message.AppendLine("  actual:   " + GetLine(actualLines, line));
			message.AppendLine("It's in:");
			foreach (var parent in GetParentLines(expectedLines, line))
				message.AppendLine("  " + parent);
			throw new ModuleDifferenceException(message.ToString());
		}

		static string GetLine(string[] lines, int index) {
			if (index >= lines.Length)
				return "<end of dump>";
			const int maxLength = 1000;
			var line = lines[index].TrimStart(' ');
			return line.Length <= maxLength ? line : line.Substring(0, maxLength) + "...";
		}

		// The lines that the line belongs to, outermost first: each has less indentation than the next one,
		// eg. "type ...", "method ...", "body"
		static List<string> GetParentLines(string[] lines, int index) {
			var parents = new List<string>();
			int indentation = index < lines.Length ? GetIndentation(lines[index]) : int.MaxValue;
			for (int i = Math.Min(index, lines.Length) - 1; i >= 0 && indentation > 0; i--) {
				int parentIndentation = GetIndentation(lines[i]);
				if (parentIndentation < indentation) {
					parents.Add(GetLine(lines, i));
					indentation = parentIndentation;
				}
			}
			parents.Reverse();
			return parents;
		}

		static int GetIndentation(string line) {
			int count = 0;
			while (count < line.Length && line[count] == ' ')
				count++;
			return count;
		}
	}
}
