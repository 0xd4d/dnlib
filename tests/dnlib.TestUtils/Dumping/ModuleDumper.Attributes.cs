// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;

namespace dnlib.TestUtils.Dumping {
	// Custom attributes, security attributes and marshal types
	public sealed partial class ModuleDumper {
		// An argument can be an array of arguments, and invalid data could nest them deeply
		const int MaxArgumentDepth = 40;

		void WriteCustomAttributes(IHasCustomAttribute owner) {
			foreach (var customAttribute in owner.CustomAttributes) {
				// dnlib keeps the bytes of a custom attribute it can't decode
				if (customAttribute.IsRawBlob) {
					writer.Line("customattribute " + ReferenceFormatter.Format(customAttribute.Constructor) + " raw=" + DumpText.Hex(customAttribute.RawData));
					continue;
				}
				writer.Line("customattribute " + ReferenceFormatter.Format(customAttribute.Constructor));
				writer.Indent();
				foreach (var argument in customAttribute.ConstructorArguments)
					writer.Line("argument " + FormatArgument(argument, 0));
				WriteNamedArguments(customAttribute.NamedArguments);
				writer.Unindent();
			}
		}

		void WriteNamedArguments(IList<CANamedArgument> namedArguments) {
			foreach (var namedArgument in namedArguments) {
				writer.Line((namedArgument.IsField ? "field " : "property ") + DumpText.Quote(namedArgument.Name) + " " +
					ReferenceFormatter.Format(namedArgument.Type) + " = " + FormatArgument(namedArgument.Argument, 0));
			}
		}

		static string FormatArgument(CAArgument argument, int depth) =>
			"(" + ReferenceFormatter.Format(argument.Type) + ")" + FormatArgumentValue(argument.Value, depth + 1);

		static string FormatArgumentValue(object? value, int depth) {
			if (depth > MaxArgumentDepth)
				return "<too deep>";
			switch (value) {
			// A boxed value, ie. the parameter's type is System.Object
			case CAArgument argument:
				return FormatArgument(argument, depth);
			// An array
			case IList<CAArgument> elements:
				var text = new StringBuilder("{");
				foreach (var element in elements)
					text.Append(FormatArgument(element, depth)).Append(", ");
				return text.Append('}').ToString();
			// A System.Type value, eg. typeof(string)
			case TypeSig type:
				return ReferenceFormatter.Format(type);
			default:
				return DumpText.Value(value);
			}
		}

		void WriteDeclSecurities(IHasDeclSecurity owner) {
			foreach (var declSecurity in owner.DeclSecurities) {
				writer.Line("declsecurity " + declSecurity.Action);
				writer.Indent();
				foreach (var securityAttribute in declSecurity.SecurityAttributes) {
					writer.Line("securityattribute " + ReferenceFormatter.Format(securityAttribute.AttributeType));
					writer.Indent();
					WriteNamedArguments(securityAttribute.NamedArguments);
					writer.Unindent();
				}
				WriteCustomAttributes(declSecurity);
				writer.Unindent();
			}
		}

		void WriteMarshalType(MarshalType? marshalType) {
			if (marshalType is null)
				return;
			// Every kind of marshal type has its own class with its own properties (eg. FixedArrayMarshalType
			// has Size and ElementType). All public properties are written, so a new property is compared too.
			var text = new StringBuilder("marshal " + marshalType.GetType().Name + " " + marshalType.NativeType);
			var properties = marshalType.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(a => a.Name, StringComparer.Ordinal);
			foreach (var property in properties) {
				if (property.Name == nameof(MarshalType.NativeType) || property.GetIndexParameters().Length != 0)
					continue;
				// Not compared: CustomMarshalType.CustomMarshalerTypeName. It's the type name as it's in the
				// file. The writer creates the name again from CustomMarshalType.CustomMarshaler, which is compared.
				if (property.Name == nameof(CustomMarshalType.CustomMarshalerTypeName))
					continue;
				var value = property.GetValue(marshalType, null);
				var valueText = value is ITypeDefOrRef or TypeSig ? ReferenceFormatter.Format(value) : DumpText.Value(value);
				text.Append(' ').Append(property.Name).Append('=').Append(valueText);
			}
			writer.Line(text.ToString());
		}
	}
}
