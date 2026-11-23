// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using dnlib.DotNet;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Describes what a reference refers to: a type, a member, a signature or a scope (assembly, module, file).
	/// The text has names and signatures but no tokens, since the writer can give everything a new token.
	/// </summary>
	static class ReferenceFormatter {
		// Invalid metadata can have references that refer to themselves
		const int MaxDepth = 40;
		const string TooDeep = "<too deep>";

		public static string Format(object? reference) => Format(reference, 0);

		static string Format(object? reference, int depth) {
			if (depth > MaxDepth)
				return TooDeep;
			depth++;
			switch (reference) {
			case null:
				return "null";
			case TypeSig typeSig:
				return FormatTypeSig(typeSig, depth);
			case TypeDef typeDef:
				return "typedef:" + FormatTypeDefName(typeDef);
			case TypeRef typeRef:
				return "typeref:" + DumpText.Quote(typeRef.Namespace) + "." + DumpText.Quote(typeRef.Name) + " in " + Format(typeRef.ResolutionScope, depth);
			case TypeSpec typeSpec:
				return "typespec(" + FormatTypeSig(typeSpec.TypeSig, depth) + ")";
			case FieldDef field:
				return "fielddef:" + FormatTypeDefName(field.DeclaringType) + "::" + DumpText.Quote(field.Name) + " " + Format(field.Signature, depth);
			case MethodDef method:
				return "methoddef:" + FormatTypeDefName(method.DeclaringType) + "::" + DumpText.Quote(method.Name) + " " + Format(method.Signature, depth);
			case MemberRef memberRef:
				return "memberref:" + Format(memberRef.Class, depth) + "::" + DumpText.Quote(memberRef.Name) + " " + Format(memberRef.Signature, depth);
			case MethodSpec methodSpec:
				return "methodspec:" + Format(methodSpec.Method, depth) + " " + Format(methodSpec.Instantiation, depth);
			case StandAloneSig standAloneSig:
				return "standalonesig:" + Format(standAloneSig.Signature, depth);
			case EventDef @event:
				return "eventdef:" + FormatTypeDefName(@event.DeclaringType) + "::" + DumpText.Quote(@event.Name) + " " + Format(@event.EventType, depth);
			case PropertyDef property:
				return "propertydef:" + FormatTypeDefName(property.DeclaringType) + "::" + DumpText.Quote(property.Name) + " " + Format(property.Type, depth);
			case AssemblyRef assemblyRef:
				return FormatAssemblyRef(assemblyRef);
			case AssemblyDef assembly:
				return "assembly:" + DumpText.Quote(assembly.Name);
			case ModuleRef moduleRef:
				return "moduleref:" + DumpText.Quote(moduleRef.Name);
			case ModuleDef module:
				return "module:" + DumpText.Quote(module.Name);
			case ExportedType exportedType:
				return "exportedtype:" + DumpText.Quote(exportedType.TypeNamespace) + "." + DumpText.Quote(exportedType.TypeName) + " in " + Format(exportedType.Implementation, depth);
			case FileDef file:
				return "file:" + DumpText.Quote(file.Name) + " hash=" + DumpText.Hex(file.HashValue) + " metadata=" + file.ContainsMetadata;
			case MethodSig methodSig:
				return FormatMethodSig(methodSig, depth);
			case FieldSig fieldSig:
				return "fieldsig(" + fieldSig.GetCallingConvention() + " " + FormatTypeSig(fieldSig.Type, depth) + ")";
			case PropertySig propertySig:
				return "propertysig(" + propertySig.CallingConvention + " genparams=" + propertySig.GenParamCount.ToString(CultureInfo.InvariantCulture) +
					" " + FormatTypeSig(propertySig.RetType, depth) + " (" + FormatTypeSigs(propertySig.Params, depth) + "))";
			case LocalSig localSig:
				return "localsig(" + localSig.GetCallingConvention() + " " + FormatTypeSigs(localSig.Locals, depth) + ")";
			case GenericInstMethodSig genericInstMethodSig:
				return "genericinstmethodsig(" + genericInstMethodSig.GetCallingConvention() + " <" + FormatTypeSigs(genericInstMethodSig.GenericArguments, depth) + ">)";
			default:
				return "unknown reference of type " + reference.GetType().Name;
			}
		}

		/// <summary>
		/// Returns the namespace and name of a type. A nested type has its declaring types first.
		/// </summary>
		public static string FormatTypeDefName(TypeDef? type) {
			if (type is null)
				return "null";
			var names = new List<string>();
			// Invalid metadata (or code that uses the API) can create a type that's nested in itself
			const int maxNestingLevels = 100;
			for (int i = 0; i < maxNestingLevels && type is not null; i++) {
				names.Add(DumpText.Quote(type.Namespace) + "." + DumpText.Quote(type.Name));
				type = type.DeclaringType;
			}
			names.Reverse();
			return string.Join("/", names);
		}

		static string FormatAssemblyRef(AssemblyRef assemblyRef) {
			// The reader returns an empty array and the API user can use null: they're written the same way
			var publicKeyOrToken = assemblyRef.PublicKeyOrToken?.Data ?? Array2.Empty;
			var hash = assemblyRef.Hash ?? Array2.Empty;
			return "assemblyref:" + DumpText.Quote(assemblyRef.Name) + " version=" + assemblyRef.Version + " culture=" + DumpText.Quote(assemblyRef.Culture) +
				" key=" + DumpText.Hex(publicKeyOrToken) + " flags=" + assemblyRef.Attributes + " hash=" + DumpText.Hex(hash);
		}

		static string FormatMethodSig(MethodSig methodSig, int depth) {
			var sb = new StringBuilder();
			sb.Append("methodsig(").Append(methodSig.CallingConvention);
			sb.Append(" genparams=").Append(methodSig.GenParamCount.ToString(CultureInfo.InvariantCulture));
			sb.Append(' ').Append(FormatTypeSig(methodSig.RetType, depth));
			sb.Append(" (").Append(FormatTypeSigs(methodSig.Params, depth)).Append(')');
			// The parameters after the "..." in the signature of a call to a vararg method
			if (methodSig.ParamsAfterSentinel is not null)
				sb.Append(" sentinel (").Append(FormatTypeSigs(methodSig.ParamsAfterSentinel, depth)).Append(')');
			sb.Append(')');
			return sb.ToString();
		}

		static string FormatTypeSigs(IList<TypeSig?> typeSigs, int depth) {
			var texts = new List<string>(typeSigs.Count);
			foreach (var typeSig in typeSigs)
				texts.Add(FormatTypeSig(typeSig, depth));
			return string.Join(", ", texts);
		}

		static string FormatTypeSig(TypeSig? typeSig, int depth) {
			if (depth > MaxDepth)
				return TooDeep;
			depth++;
			switch (typeSig) {
			case null:
				return "null";
			case CorLibTypeSig corLibTypeSig:
				return corLibTypeSig.ElementType + ":" + Format(corLibTypeSig.TypeDefOrRef, depth);
			case ClassSig classSig:
				return "class " + Format(classSig.TypeDefOrRef, depth);
			case ValueTypeSig valueTypeSig:
				return "valuetype " + Format(valueTypeSig.TypeDefOrRef, depth);
			case GenericVar genericVar:
				return "!" + genericVar.Number.ToString(CultureInfo.InvariantCulture);
			case GenericMVar genericMVar:
				return "!!" + genericMVar.Number.ToString(CultureInfo.InvariantCulture);
			case SentinelSig:
				return "sentinel";
			case FnPtrSig fnPtrSig:
				return "fnptr " + Format(fnPtrSig.Signature, depth);
			case GenericInstSig genericInstSig:
				return FormatTypeSig(genericInstSig.GenericType, depth) + "<" + FormatTypeSigs(genericInstSig.GenericArguments, depth) + ">";
			case PtrSig:
				return FormatTypeSig(typeSig.Next, depth) + "*";
			case ByRefSig:
				return FormatTypeSig(typeSig.Next, depth) + "&";
			case ArraySig arraySig:
				return FormatTypeSig(typeSig.Next, depth) + "[rank=" + arraySig.Rank.ToString(CultureInfo.InvariantCulture) +
					" sizes=" + Join(arraySig.Sizes) + " lowerbounds=" + Join(arraySig.LowerBounds) + "]";
			case SZArraySig:
				return FormatTypeSig(typeSig.Next, depth) + "[]";
			case CModReqdSig modifier:
				return FormatTypeSig(typeSig.Next, depth) + " modreq(" + Format(modifier.Modifier, depth) + ")";
			case CModOptSig modifier:
				return FormatTypeSig(typeSig.Next, depth) + " modopt(" + Format(modifier.Modifier, depth) + ")";
			case PinnedSig:
				return FormatTypeSig(typeSig.Next, depth) + " pinned";
			case ValueArraySig valueArraySig:
				return FormatTypeSig(typeSig.Next, depth) + " valuearray(" + valueArraySig.Size.ToString(CultureInfo.InvariantCulture) + ")";
			case ModuleSig moduleSig:
				return FormatTypeSig(typeSig.Next, depth) + " module(" + moduleSig.Index.ToString(CultureInfo.InvariantCulture) + ")";
			default:
				return "unknown type sig of type " + typeSig.GetType().Name;
			}
		}

		static string Join<T>(IList<T> values) {
			var texts = new List<string>(values.Count);
			foreach (var value in values)
				texts.Add(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
			return "(" + string.Join(",", texts) + ")";
		}

		static class Array2 {
			public static readonly byte[] Empty = new byte[0];
		}
	}
}
