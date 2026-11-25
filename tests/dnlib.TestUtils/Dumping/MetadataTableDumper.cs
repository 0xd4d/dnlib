// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Writes the rows of the metadata tables of a PE file as text. It reads the file with
	/// System.Reflection.Metadata and not with dnlib, so the tests can check what dnlib wrote without using
	/// dnlib's reader, and compare it with what a compiler wrote.
	/// </summary>
	/// <remarks>
	/// Offsets in the heaps aren't written, the strings, GUIDs and bytes at those offsets are. Two files with
	/// the same rows in the same order have the same text, even if their heaps are in another order.
	///
	/// Not in the text: where method bodies and field data are in the file (RVAs), the method bodies, and the
	/// row numbers of the AssemblyRef and ModuleRef tables (dnlib has no option to preserve them).
	/// </remarks>
	public sealed class MetadataTableDumper {
		readonly MetadataReader reader;
		readonly StringBuilder text = new StringBuilder();

		MetadataTableDumper(MetadataReader reader) => this.reader = reader;

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <param name="image">A PE file with .NET metadata</param>
		public static string Dump(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image)))
				return new MetadataTableDumper(peReader.GetMetadataReader()).Dump();
		}

		// Eg. "TypeReference 12", or "nil" if it doesn't reference a row
		// A reference to an AssemblyRef or ModuleRef row is the name of the assembly or module: the writer can't
		// keep the order of these rows.
		string Row(Handle handle) {
			if (handle.IsNil)
				return "nil";
			if (handle.Kind == HandleKind.AssemblyReference)
				return "AssemblyReference " + Text(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name);
			if (handle.Kind == HandleKind.ModuleReference)
				return "ModuleReference " + Text(reader.GetModuleReference((ModuleReferenceHandle)handle).Name);
			return $"{handle.Kind} {MetadataTokens.GetToken(reader, handle) & 0x00FFFFFF}";
		}
		string Text(StringHandle handle) => handle.IsNil ? "nil" : "\"" + reader.GetString(handle) + "\"";
		string Blob(BlobHandle handle) => handle.IsNil ? "nil" : Hex(reader.GetBlobBytes(handle));
		string Guid(GuidHandle handle) => handle.IsNil ? "nil" : reader.GetGuid(handle).ToString();
		static string Hex(byte[] bytes) => bytes.Length == 0 ? "empty" : BitConverter.ToString(bytes);

		void Line(string table, Handle handle, string values) => text.AppendLine($"{table} {MetadataTokens.GetToken(reader, handle) & 0x00FFFFFF}: {values}");

		string Dump() {
			var module = reader.GetModuleDefinition();
			text.AppendLine($"Module: {Text(module.Name)}, mvid {Guid(module.Mvid)}, generation {module.Generation}");
			if (reader.IsAssembly) {
				var assembly = reader.GetAssemblyDefinition();
				text.AppendLine($"Assembly: {Text(assembly.Name)}, {assembly.Version}, culture {Text(assembly.Culture)}, {assembly.Flags}, {assembly.HashAlgorithm}, public key {Blob(assembly.PublicKey)}");
			}
			// Sorted by name, see Row()
			var assemblyReferences = new System.Collections.Generic.List<string>();
			foreach (var handle in reader.AssemblyReferences) {
				var row = reader.GetAssemblyReference(handle);
				assemblyReferences.Add($"AssemblyRef: {Text(row.Name)}, {row.Version}, culture {Text(row.Culture)}, {row.Flags}, public key or token {Blob(row.PublicKeyOrToken)}, hash {Blob(row.HashValue)}");
			}
			assemblyReferences.Sort(StringComparer.Ordinal);
			foreach (var line in assemblyReferences)
				text.AppendLine(line);
			var moduleReferences = new System.Collections.Generic.List<string>();
			for (int rid = 1; rid <= reader.GetTableRowCount(TableIndex.ModuleRef); rid++)
				moduleReferences.Add("ModuleRef: " + Text(reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(rid)).Name));
			moduleReferences.Sort(StringComparer.Ordinal);
			foreach (var line in moduleReferences)
				text.AppendLine(line);
			foreach (var handle in reader.AssemblyFiles) {
				var row = reader.GetAssemblyFile(handle);
				Line("File", handle, $"{Text(row.Name)}, has metadata {row.ContainsMetadata}, hash {Blob(row.HashValue)}");
			}
			foreach (var handle in reader.TypeReferences) {
				var row = reader.GetTypeReference(handle);
				Line("TypeRef", handle, $"{Text(row.Namespace)} {Text(row.Name)}, scope {Row(row.ResolutionScope)}");
			}
			DumpTypeDefinitions();
			foreach (var handle in reader.FieldDefinitions) {
				var row = reader.GetFieldDefinition(handle);
				Line("Field", handle, $"{Text(row.Name)}, {row.Attributes}, signature {Blob(row.Signature)}, offset {row.GetOffset()}, has data {row.GetRelativeVirtualAddress() != 0}, marshal {Blob(row.GetMarshallingDescriptor())}, constant {Row(row.GetDefaultValue())}");
			}
			DumpMethodDefinitions();
			foreach (var handle in reader.MemberReferences) {
				var row = reader.GetMemberReference(handle);
				Line("MemberRef", handle, $"{Text(row.Name)}, class {Row(row.Parent)}, signature {Blob(row.Signature)}");
			}
			for (int rid = 1; rid <= reader.GetTableRowCount(TableIndex.Constant); rid++) {
				var handle = MetadataTokens.ConstantHandle(rid);
				var row = reader.GetConstant(handle);
				Line("Constant", handle, $"{row.TypeCode}, parent {Row(row.Parent)}, value {Blob(row.Value)}");
			}
			foreach (var handle in reader.CustomAttributes) {
				var row = reader.GetCustomAttribute(handle);
				Line("CustomAttribute", handle, $"parent {Row(row.Parent)}, constructor {Row(row.Constructor)}, value {Blob(row.Value)}");
			}
			foreach (var handle in reader.DeclarativeSecurityAttributes) {
				var row = reader.GetDeclarativeSecurityAttribute(handle);
				Line("DeclSecurity", handle, $"{row.Action}, parent {Row(row.Parent)}, permission set {Blob(row.PermissionSet)}");
			}
			for (int rid = 1; rid <= reader.GetTableRowCount(TableIndex.StandAloneSig); rid++) {
				var handle = MetadataTokens.StandaloneSignatureHandle(rid);
				Line("StandAloneSig", handle, Blob(reader.GetStandaloneSignature(handle).Signature));
			}
			for (int rid = 1; rid <= reader.GetTableRowCount(TableIndex.TypeSpec); rid++) {
				var handle = MetadataTokens.TypeSpecificationHandle(rid);
				Line("TypeSpec", handle, Blob(reader.GetTypeSpecification(handle).Signature));
			}
			for (int rid = 1; rid <= reader.GetTableRowCount(TableIndex.MethodSpec); rid++) {
				var handle = MetadataTokens.MethodSpecificationHandle(rid);
				var row = reader.GetMethodSpecification(handle);
				Line("MethodSpec", handle, $"method {Row(row.Method)}, instantiation {Blob(row.Signature)}");
			}
			foreach (var handle in reader.EventDefinitions) {
				var row = reader.GetEventDefinition(handle);
				var accessors = row.GetAccessors();
				Line("Event", handle, $"{Text(row.Name)}, {row.Attributes}, type {Row(row.Type)}, add {Row(accessors.Adder)}, remove {Row(accessors.Remover)}, raise {Row(accessors.Raiser)}, others {accessors.Others.Length}");
			}
			foreach (var handle in reader.PropertyDefinitions) {
				var row = reader.GetPropertyDefinition(handle);
				var accessors = row.GetAccessors();
				Line("Property", handle, $"{Text(row.Name)}, {row.Attributes}, signature {Blob(row.Signature)}, get {Row(accessors.Getter)}, set {Row(accessors.Setter)}, others {accessors.Others.Length}, constant {Row(row.GetDefaultValue())}");
			}
			foreach (var handle in reader.ExportedTypes) {
				var row = reader.GetExportedType(handle);
				Line("ExportedType", handle, $"{Text(row.Namespace)} {Text(row.Name)}, {row.Attributes}, implementation {Row(row.Implementation)}");
			}
			foreach (var handle in reader.ManifestResources) {
				var row = reader.GetManifestResource(handle);
				Line("ManifestResource", handle, $"{Text(row.Name)}, {row.Attributes}, implementation {Row(row.Implementation)}");
			}
			return text.ToString();
		}

		void DumpTypeDefinitions() {
			foreach (var handle in reader.TypeDefinitions) {
				var row = reader.GetTypeDefinition(handle);
				var layout = row.GetLayout();
				Line("TypeDef", handle, $"{Text(row.Namespace)} {Text(row.Name)}, {row.Attributes}, extends {Row(row.BaseType)}, declaring type {Row(row.GetDeclaringType())}, packing {layout.PackingSize}, size {layout.Size}");
				foreach (var field in row.GetFields())
					text.AppendLine($"  {Row(field)}");
				foreach (var method in row.GetMethods())
					text.AppendLine($"  {Row(method)}");
				foreach (var property in row.GetProperties())
					text.AppendLine($"  {Row(property)}");
				foreach (var @event in row.GetEvents())
					text.AppendLine($"  {Row(@event)}");
				foreach (var interfaceHandle in row.GetInterfaceImplementations())
					text.AppendLine($"  implements {Row(reader.GetInterfaceImplementation(interfaceHandle).Interface)} ({Row(interfaceHandle)})");
				foreach (var implHandle in row.GetMethodImplementations()) {
					var impl = reader.GetMethodImplementation(implHandle);
					text.AppendLine($"  method impl: body {Row(impl.MethodBody)}, declaration {Row(impl.MethodDeclaration)}");
				}
				DumpGenericParameters(row.GetGenericParameters());
			}
		}

		void DumpMethodDefinitions() {
			foreach (var handle in reader.MethodDefinitions) {
				var row = reader.GetMethodDefinition(handle);
				Line("Method", handle, $"{Text(row.Name)}, {row.Attributes}, {row.ImplAttributes}, signature {Blob(row.Signature)}, has body {row.RelativeVirtualAddress != 0}");
				foreach (var parameterHandle in row.GetParameters()) {
					var parameter = reader.GetParameter(parameterHandle);
					text.AppendLine($"  {Row(parameterHandle)}: {Text(parameter.Name)}, sequence {parameter.SequenceNumber}, {parameter.Attributes}, marshal {Blob(parameter.GetMarshallingDescriptor())}, constant {Row(parameter.GetDefaultValue())}");
				}
				var import = row.GetImport();
				if (!import.Module.IsNil)
					text.AppendLine($"  pinvoke: {Text(import.Name)}, {import.Attributes}, module {Row(import.Module)}");
				DumpGenericParameters(row.GetGenericParameters());
			}
		}

		void DumpGenericParameters(GenericParameterHandleCollection parameters) {
			foreach (var handle in parameters) {
				var parameter = reader.GetGenericParameter(handle);
				text.AppendLine($"  {Row(handle)}: {Text(parameter.Name)}, index {parameter.Index}, {parameter.Attributes}");
				foreach (var constraintHandle in parameter.GetConstraints())
					text.AppendLine($"    constraint {Row(reader.GetGenericParameterConstraint(constraintHandle).Type)} ({Row(constraintHandle)})");
			}
		}
	}
}
