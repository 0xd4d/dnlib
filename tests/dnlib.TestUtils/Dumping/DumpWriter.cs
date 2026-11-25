// dnlib: See LICENSE.txt for more info

using System.Text;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Writes the lines of a dump. Each line is indented to show what it belongs to, eg. a custom attribute
	/// is below its method and indented more than the method.
	/// </summary>
	sealed class DumpWriter {
		readonly StringBuilder text = new StringBuilder();
		int indentation;

		public void Indent() => indentation++;
		public void Unindent() => indentation--;

		public void Line(string line) {
			text.Append(' ', indentation * 2);
			text.Append(line);
			text.Append('\n');
		}

		public override string ToString() => text.ToString();
	}
}
