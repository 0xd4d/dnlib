// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Globalization;
using dnlib.DotNet;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Writes everything dnlib knows about a module as text: the module and assembly properties, resources,
	/// types, members, signatures, method bodies, custom attributes etc.
	///
	/// A module that's written to a file and read again must have the same dump as before. So the dump has
	/// nothing that the writer is allowed to change: no tokens, RVAs or file offsets. Things are identified
	/// by name and signature, see <see cref="ReferenceFormatter"/>.
	///
	/// A few more things can change when a module is written. They're not in the dump, and each place that
	/// leaves something out has a comment that starts with "Not compared".
	/// </summary>
	public sealed partial class ModuleDumper {
		readonly ModuleDumpOptions options;
		readonly DumpWriter writer = new DumpWriter();

		ModuleDumper(ModuleDumpOptions options) => this.options = options;

		/// <summary>
		/// Dumps everything that doesn't belong to a type: the module, the assembly, the resources and the
		/// exported types
		/// </summary>
		public static string DumpModuleHeader(ModuleDef module, ModuleDumpOptions? options = null) {
			var dumper = new ModuleDumper(options ?? new ModuleDumpOptions());
			dumper.WriteModuleHeader(module);
			return dumper.writer.ToString();
		}

		/// <summary>
		/// Dumps a type and its members. Its nested types are only listed, they're dumped with their own call.
		/// </summary>
		public static string DumpType(TypeDef type, ModuleDumpOptions? options = null) {
			var dumper = new ModuleDumper(options ?? new ModuleDumpOptions());
			dumper.WriteType(type);
			return dumper.writer.ToString();
		}

		/// <summary>
		/// Dumps the whole module: <see cref="DumpModuleHeader"/> followed by <see cref="DumpType"/> of all types,
		/// including nested types. Only use it with small modules, the text is big.
		/// </summary>
		public static string Dump(ModuleDef module, ModuleDumpOptions? options = null) {
			var text = new System.Text.StringBuilder(DumpModuleHeader(module, options));
			foreach (var type in module.GetTypes())
				text.Append(DumpType(type, options));
			return text.ToString();
		}

		void WriteModuleHeader(ModuleDef module) {
			writer.Line("module " + DumpText.Quote(module.Name));
			writer.Indent();
			writer.Line("mvid=" + Format(module.Mvid) + " encid=" + Format(module.EncId) + " encbaseid=" + Format(module.EncBaseId) + " generation=" + module.Generation.ToString(CultureInfo.InvariantCulture));
			writer.Line("kind=" + module.Kind + " machine=" + module.Machine + " characteristics=" + module.Characteristics + " dllcharacteristics=" + module.DllCharacteristics);
			writer.Line("runtimeversion=" + DumpText.Quote(module.RuntimeVersion) + " cor20flags=" + module.Cor20HeaderFlags +
				" cor20runtimeversion=" + Format(module.Cor20HeaderRuntimeVersion) + " tablesversion=" + Format(module.TablesHeaderVersion));
			writer.Line("entrypoint=" + ReferenceFormatter.Format(module.ManagedEntryPoint));
			WriteCustomAttributes(module);
			writer.Unindent();

			var assembly = module.Assembly;
			if (assembly is not null)
				WriteAssembly(assembly);

			foreach (var resource in module.Resources)
				WriteResource(resource);

			WriteExportedTypes(module);
		}

		void WriteAssembly(AssemblyDef assembly) {
			// Not compared: a public key that's null and one that's empty. They're written the same way.
			var publicKey = assembly.PublicKey?.Data ?? emptyBytes;
			writer.Line("assembly " + DumpText.Quote(assembly.Name));
			writer.Indent();
			writer.Line("version=" + assembly.Version + " culture=" + DumpText.Quote(assembly.Culture) + " publickey=" + DumpText.Hex(publicKey) +
				" flags=" + assembly.Attributes + " hashalgorithm=" + assembly.HashAlgorithm + " modules=" + assembly.Modules.Count.ToString(CultureInfo.InvariantCulture));
			WriteCustomAttributes(assembly);
			WriteDeclSecurities(assembly);
			writer.Unindent();
		}

		void WriteResource(Resource resource) {
			var text = "resource " + DumpText.Quote(resource.Name) + " " + resource.Attributes + " " + resource.ResourceType + " ";
			switch (resource) {
			case EmbeddedResource embedded:
				text += DumpText.Hex(embedded.CreateReader().ToArray());
				break;
			case AssemblyLinkedResource assemblyLinked:
				text += ReferenceFormatter.Format(assemblyLinked.Assembly);
				break;
			case LinkedResource linked:
				text += ReferenceFormatter.Format(linked.File);
				break;
			}
			writer.Line(text);
			writer.Indent();
			WriteCustomAttributes(resource);
			writer.Unindent();
		}

		void WriteExportedTypes(ModuleDef module) {
			// Not compared: the order of the exported types. The writer sorts them so that a nested type is
			// after its declaring type.
			var dumps = new List<string>();
			foreach (var exportedType in module.ExportedTypes) {
				var dumper = new ModuleDumper(options);
				dumper.writer.Line("exportedtype " + DumpText.Quote(exportedType.TypeNamespace) + "." + DumpText.Quote(exportedType.TypeName) +
					" " + exportedType.Attributes + " typedefid=" + exportedType.TypeDefId.ToString(CultureInfo.InvariantCulture) +
					" implementation=" + ReferenceFormatter.Format(exportedType.Implementation));
				dumper.writer.Indent();
				dumper.WriteCustomAttributes(exportedType);
				dumps.Add(dumper.writer.ToString());
			}
			dumps.Sort(StringComparer.Ordinal);
			foreach (var dump in dumps) {
				foreach (var line in dump.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
					writer.Line(line);
			}
		}

		void WriteType(TypeDef type) {
			var layout = type.HasClassLayout ?
				"packingsize=" + type.PackingSize.ToString(CultureInfo.InvariantCulture) + " classsize=" + type.ClassSize.ToString(CultureInfo.InvariantCulture) :
				"none";
			writer.Line("type " + ReferenceFormatter.FormatTypeDefName(type));
			writer.Indent();
			writer.Line("flags=" + type.Attributes + " layout=" + layout);
			writer.Line("base=" + ReferenceFormatter.Format(type.BaseType));
			foreach (var interfaceImpl in type.Interfaces) {
				writer.Line("interface " + ReferenceFormatter.Format(interfaceImpl.Interface));
				writer.Indent();
				WriteCustomAttributes(interfaceImpl);
				writer.Unindent();
			}
			WriteGenericParameters(type.GenericParameters);
			WriteCustomAttributes(type);
			WriteDeclSecurities(type);
			foreach (var nestedType in type.NestedTypes)
				writer.Line("nestedtype " + DumpText.Quote(nestedType.Namespace) + "." + DumpText.Quote(nestedType.Name));
			foreach (var field in type.Fields)
				WriteField(field);
			foreach (var method in type.Methods)
				WriteMethod(method);
			foreach (var @event in type.Events)
				WriteEvent(@event);
			foreach (var property in type.Properties)
				WriteProperty(property);
			writer.Unindent();
		}

		void WriteGenericParameters(IList<GenericParam> genericParameters) {
			foreach (var genericParameter in genericParameters) {
				writer.Line("genericparam " + genericParameter.Number.ToString(CultureInfo.InvariantCulture) + " " + DumpText.Quote(genericParameter.Name) +
					" " + genericParameter.Flags + " kind=" + ReferenceFormatter.Format(genericParameter.Kind));
				writer.Indent();
				foreach (var constraint in genericParameter.GenericParamConstraints) {
					writer.Line("constraint " + ReferenceFormatter.Format(constraint.Constraint));
					writer.Indent();
					WriteCustomAttributes(constraint);
					writer.Unindent();
				}
				WriteCustomAttributes(genericParameter);
				writer.Unindent();
			}
		}

		void WriteField(FieldDef field) {
			var text = "field " + DumpText.Quote(field.Name) + " " + field.Attributes + " offset=" + Format(field.FieldOffset) + " " + ReferenceFormatter.Format(field.Signature);
			if (field.HasFieldRVA || field.InitialValue is not null)
				text += " initialvalue=" + DumpText.Hex(field.InitialValue);
			writer.Line(text);
			writer.Indent();
			WriteConstant(field.Constant);
			WriteMarshalType(field.MarshalType);
			WriteImplMap(field.ImplMap);
			WriteCustomAttributes(field);
			writer.Unindent();
		}

		void WriteMethod(MethodDef method) {
			writer.Line("method " + DumpText.Quote(method.Name) + " " + ReferenceFormatter.Format(method.Signature));
			writer.Indent();
			writer.Line("flags=" + method.Attributes + " implflags=" + method.ImplAttributes + " semantics=" + method.SemanticsAttributes);
			foreach (var paramDef in method.ParamDefs) {
				writer.Line("param " + paramDef.Sequence.ToString(CultureInfo.InvariantCulture) + " " + DumpText.Quote(paramDef.Name) + " " + paramDef.Attributes);
				writer.Indent();
				WriteConstant(paramDef.Constant);
				WriteMarshalType(paramDef.MarshalType);
				WriteCustomAttributes(paramDef);
				writer.Unindent();
			}
			WriteGenericParameters(method.GenericParameters);
			WriteImplMap(method.ImplMap);
			foreach (var methodOverride in method.Overrides)
				writer.Line("override body=" + ReferenceFormatter.Format(methodOverride.MethodBody) + " declaration=" + ReferenceFormatter.Format(methodOverride.MethodDeclaration));
			WriteCustomAttributes(method);
			WriteDeclSecurities(method);

			var exportInfo = method.ExportInfo;
			if (exportInfo is not null && options.MethodExportInfo)
				writer.Line("export " + DumpText.Quote(exportInfo.Name) + " ordinal=" + Format(exportInfo.Ordinal) + " " + exportInfo.Options);
			if (method.NativeBody is not null)
				writer.Line("nativebody");
			if (method.Body is not null)
				WriteMethodBody(method.Body);
			writer.Unindent();
		}

		void WriteEvent(EventDef @event) {
			writer.Line("event " + DumpText.Quote(@event.Name) + " " + @event.Attributes + " " + ReferenceFormatter.Format(@event.EventType));
			writer.Indent();
			WriteAccessor("add", @event.AddMethod);
			WriteAccessor("invoke", @event.InvokeMethod);
			WriteAccessor("remove", @event.RemoveMethod);
			foreach (var method in @event.OtherMethods)
				WriteAccessor("other", method);
			WriteCustomAttributes(@event);
			writer.Unindent();
		}

		void WriteProperty(PropertyDef property) {
			writer.Line("property " + DumpText.Quote(property.Name) + " " + property.Attributes + " " + ReferenceFormatter.Format(property.Type));
			writer.Indent();
			foreach (var method in property.GetMethods)
				WriteAccessor("get", method);
			foreach (var method in property.SetMethods)
				WriteAccessor("set", method);
			foreach (var method in property.OtherMethods)
				WriteAccessor("other", method);
			WriteConstant(property.Constant);
			WriteCustomAttributes(property);
			writer.Unindent();
		}

		void WriteAccessor(string kind, MethodDef? method) {
			if (method is not null)
				writer.Line(kind + " " + ReferenceFormatter.Format(method));
		}

		void WriteImplMap(ImplMap? implMap) {
			if (implMap is not null)
				writer.Line("implmap " + DumpText.Quote(implMap.Name) + " " + implMap.Attributes + " module=" + DumpText.Quote(implMap.Module?.Name));
		}

		void WriteConstant(Constant? constant) {
			if (constant is not null)
				writer.Line("constant " + constant.Type + " " + DumpText.Value(constant.Value));
		}

		static readonly byte[] emptyBytes = new byte[0];

		static string Format<T>(T? value) where T : struct =>
			value is null ? "null" : Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;
	}
}
