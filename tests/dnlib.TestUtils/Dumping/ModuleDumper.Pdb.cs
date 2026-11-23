// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;

namespace dnlib.TestUtils.Dumping {
	// Debug info (what dnlib read from a PDB file). Only written if ModuleDumpOptions.Pdb is set.
	public sealed partial class ModuleDumper {
		void WritePdbState(ModuleDef module) {
			var pdbState = module.PdbState;
			if (!options.Pdb || pdbState is null)
				return;
			// Not compared: PdbState.PdbFileKind. A Windows PDB can be written as a portable PDB.
			writer.Line("pdb userentrypoint=" + ReferenceFormatter.Format(pdbState.UserEntryPoint));
			writer.Indent();

			// Not compared: the order of the documents. The writer writes them in the order they're first used.
			var documents = new List<string>();
			foreach (var document in pdbState.Documents) {
				var dumper = new ModuleDumper(options);
				dumper.writer.Line("document " + DumpText.Quote(document.Url) + " language=" + document.Language + " vendor=" + document.LanguageVendor +
					" type=" + document.DocumentType + " checksumalgorithm=" + document.CheckSumAlgorithmId + " checksum=" + DumpText.Hex(document.CheckSum));
				dumper.writer.Indent();
				dumper.WriteCustomDebugInfos(document);
				documents.Add(dumper.writer.ToString());
			}
			documents.Sort(StringComparer.Ordinal);
			foreach (var document in documents) {
				foreach (var line in document.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
					writer.Line(line);
			}
			writer.Unindent();
		}

		void WriteCustomDebugInfos(IHasCustomDebugInformation owner) {
			if (!options.Pdb)
				return;
			foreach (var customDebugInfo in owner.CustomDebugInfos)
				writer.Line("customdebuginfo " + FormatPdbObject(customDebugInfo, 0));
		}

		static string FormatSequencePoint(SequencePoint? sequencePoint) {
			if (sequencePoint is null)
				return string.Empty;
			return " @ " + DumpText.Quote(sequencePoint.Document?.Url) + " " +
				sequencePoint.StartLine.ToString(CultureInfo.InvariantCulture) + "," + sequencePoint.StartColumn.ToString(CultureInfo.InvariantCulture) + "-" +
				sequencePoint.EndLine.ToString(CultureInfo.InvariantCulture) + "," + sequencePoint.EndColumn.ToString(CultureInfo.InvariantCulture);
		}

		void WritePdbMethod(PdbMethod? pdbMethod) {
			var scope = pdbMethod?.Scope;
			if (!options.Pdb || scope is null)
				return;
			// Not compared: a root scope without anything in it. The portable PDB reader creates one for
			// every method, also for methods that have no debug info.
			if (IsEmpty(scope))
				return;
			WritePdbScope(scope);
		}

		static bool IsEmpty(PdbScope scope) =>
			scope.Scopes.Count == 0 && scope.Variables.Count == 0 && scope.Constants.Count == 0 &&
			scope.Namespaces.Count == 0 && scope.ImportScope is null && scope.CustomDebugInfos.Count == 0;

		void WritePdbScope(PdbScope scope) {
			writer.Line("scope " + FormatTarget(scope.Start) + "-" + FormatTarget(scope.End));
			writer.Indent();
			for (var importScope = scope.ImportScope; importScope is not null; importScope = importScope.Parent) {
				writer.Line("importscope");
				writer.Indent();
				foreach (var import in importScope.Imports)
					writer.Line("import " + FormatPdbObject(import, 0));
				WriteCustomDebugInfos(importScope);
				writer.Unindent();
			}
			// Only Windows PDBs have these. They're the same info as the import scopes of a portable PDB.
			foreach (var @namespace in scope.Namespaces)
				writer.Line("namespace " + DumpText.Quote(@namespace));
			foreach (var local in scope.Variables) {
				writer.Line("local " + local.Index.ToString(CultureInfo.InvariantCulture) + " " + DumpText.Quote(local.Name) + " " + local.Attributes);
				writer.Indent();
				WriteCustomDebugInfos(local);
				writer.Unindent();
			}
			foreach (var constant in scope.Constants) {
				writer.Line("constant " + DumpText.Quote(constant.Name) + " " + ReferenceFormatter.Format(constant.Type) + " " + DumpText.Value(constant.Value));
				writer.Indent();
				WriteCustomDebugInfos(constant);
				writer.Unindent();
			}
			WriteCustomDebugInfos(scope);
			foreach (var childScope in scope.Scopes)
				WritePdbScope(childScope);
			writer.Unindent();
		}

		// There are many kinds of custom debug info and imports, each with its own class and properties, and new
		// ones are added from time to time. They're small data classes, so instead of code for each class, all
		// public properties and fields are written (sorted by name). A new class or property is then compared
		// without changing this code.
		string FormatPdbObject(object? value, int depth) {
			// Some objects reference each other, eg. a scope and its child scopes
			const int maxDepth = 20;
			if (depth > maxDepth)
				return "<too deep>";

			switch (value) {
			case null:
				return "null";
			case string or byte[] or float or double:
				return DumpText.Value(value);
			case Instruction instruction:
				return FormatTarget(instruction);
			case Local local:
				return "local " + local.Index.ToString(CultureInfo.InvariantCulture);
			case Parameter parameter:
				return "parameter " + parameter.Index.ToString(CultureInfo.InvariantCulture);
			case PdbDocument document:
				return "document " + DumpText.Quote(document.Url);
			case Guid or DateTime or decimal:
				return value.GetType().Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
			}

			var type = value.GetType();
			if (type.IsPrimitive || type.IsEnum)
				return type.Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
			if (!IsPdbClass(type)) {
				// A type, member or signature of the module
				if (value is TypeSig or IMDTokenProvider or CallingConventionSig)
					return ReferenceFormatter.Format(value);
				if (value is IEnumerable elements) {
					var texts = new List<string>();
					foreach (var element in elements)
						texts.Add(FormatPdbObject(element, depth + 1));
					return "[" + string.Join(", ", texts) + "]";
				}
				return "unknown value of type " + type.Name;
			}

			var text = new StringBuilder(type.Name).Append(" {");
			foreach (var property in GetComparedProperties(type))
				text.Append(' ').Append(property.Name).Append('=').Append(FormatPdbObject(property.GetValue(value, null), depth + 1)).Append(';');
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(a => a.Name, StringComparer.Ordinal))
				text.Append(' ').Append(field.Name).Append('=').Append(FormatPdbObject(field.GetValue(value), depth + 1)).Append(';');
			return text.Append(" }").ToString();
		}

		static bool IsPdbClass(Type type) => type.Namespace == typeof(PdbCustomDebugInfo).Namespace;

		static IEnumerable<PropertyInfo> GetComparedProperties(Type type) =>
			type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(a => a.CanRead && a.GetIndexParameters().Length == 0 && !IsIgnoredPdbProperty(a.Name))
				.OrderBy(a => a.Name, StringComparer.Ordinal);

		static bool IsIgnoredPdbProperty(string name) {
			// They return another property's value or throw if it's null
			if (name.EndsWith("OrThrow", StringComparison.Ordinal))
				return true;
			switch (name) {
			// Not compared: tokens. The writer can give everything a new token.
			case "MDToken":
			case "Rid":
			// Calculated from other properties, or the same for all instances of a class
			case "HasCustomDebugInfos":
			case "HasCustomDebugInformationTag":
			case "HasImports":
				return true;
			default:
				return false;
			}
		}
	}
}
