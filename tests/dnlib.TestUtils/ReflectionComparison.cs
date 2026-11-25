// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Reflection;
using dnlib.DotNet;

namespace dnlib.TestUtils {
	/// <summary>
	/// Thrown by <see cref="ReflectionComparison"/> when dnlib and reflection don't agree
	/// </summary>
	public sealed class ReflectionDifferenceException : Exception {
		public ReflectionDifferenceException(string message) : base(message) { }
	}

	/// <summary>
	/// Checks <see cref="SigComparer"/> and <see cref="Importer"/> against System.Reflection. The same assembly
	/// is loaded by the runtime and by dnlib, and for every type, method, field, property and event:
	/// <list type="bullet">
	/// <item>dnlib's definition equals the reflection object, and both have the same hash code</item>
	/// <item>dnlib's definition doesn't equal the reflection objects of the next members of the same type</item>
	/// <item>the reflection object can be imported into another module, and what was imported equals the
	/// reflection object and has the same hash code</item>
	/// </list>
	/// A definition and its reflection object are the ones with the same metadata token.
	/// </summary>
	public sealed class ReflectionComparison {
		const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		// A member is also compared with this many of the members that follow it, which must not be equal
		const int OtherMembersToCompare = 3;

		readonly ModuleDefMD module;
		readonly SigComparerOptions options;
		readonly SigComparer comparer;
		readonly Importer importer;

		/// <summary>
		/// Number of members (and types) that were checked
		/// </summary>
		public int CheckedMembers { get; private set; }

		ReflectionComparison(ModuleDefMD module, SigComparerOptions options) {
			this.module = module;
			this.options = options;
			comparer = new SigComparer(options);
			// Import into an empty module, so everything becomes a reference to the assembly
			var targetModule = new ModuleDefUser("ImportTarget.dll");
			new AssemblyDefUser("ImportTarget").Modules.Add(targetModule);
			importer = new Importer(targetModule);
		}

		/// <summary>
		/// Checks all types and members of an assembly. Throws a <see cref="ReflectionDifferenceException"/>
		/// that names the first member dnlib and reflection don't agree on.
		/// </summary>
		/// <param name="assembly">The assembly, loaded by the runtime</param>
		/// <param name="module">The same assembly's main module, loaded by dnlib</param>
		/// <param name="options">Options for the <see cref="SigComparer"/></param>
		/// <returns>Number of types and members that were checked</returns>
		public static int Check(Assembly assembly, ModuleDefMD module, SigComparerOptions options) {
			var comparison = new ReflectionComparison(module, options);
			foreach (var type in GetTypes(assembly))
				comparison.CheckType(type);
			return comparison.CheckedMembers;
		}

		static IEnumerable<Type> GetTypes(Assembly assembly) {
			try {
				return assembly.ManifestModule.GetTypes();
			}
			catch (ReflectionTypeLoadException ex) {
				// Some types can't be loaded on this OS or runtime. The other types are in the array.
				var types = new List<Type>();
				foreach (var type in ex.Types) {
					if (type is not null)
						types.Add(type);
				}
				return types;
			}
		}

		void CheckType(Type type) {
			var typeDef = Resolve<TypeDef>(type);
			Require(comparer.Equals(typeDef, type), type, "the TypeDef doesn't equal the Type");
			Require(comparer.GetHashCode(typeDef) == comparer.GetHashCode(type), type, "the TypeDef and the Type have different hash codes");

			var imported = importer.ImportOrNull(type);
			Require(imported is not null, type, "the Type can't be imported");
			Require(comparer.Equals(imported, type), type, "the imported type doesn't equal the Type");
			Require(comparer.GetHashCode(imported) == comparer.GetHashCode(type), type, "the imported type and the Type have different hash codes");
			CheckedMembers++;

			var methods = new List<MethodBase>();
			methods.AddRange(type.GetConstructors(DeclaredMembers));
			methods.AddRange(type.GetMethods(DeclaredMembers));
			CheckMembers(methods, CheckMethod, (def, other) => comparer.Equals((MethodDef)def, other));

			CheckMembers(type.GetFields(DeclaredMembers), CheckField, (def, other) => comparer.Equals((FieldDef)def, other));
			CheckMembers(type.GetProperties(DeclaredMembers), CheckProperty, (def, other) => comparer.Equals((PropertyDef)def, other));
			CheckMembers(type.GetEvents(DeclaredMembers), CheckEvent, (def, other) => comparer.Equals((EventDef)def, other));
		}

		// check: checks a member and returns its definition
		// equalsOther: compares a definition with the reflection object of another member
		void CheckMembers<TMember>(IList<TMember> members, Func<TMember, object> check, Func<object, TMember, bool> equalsOther) where TMember : MemberInfo {
			for (int i = 0; i < members.Count; i++) {
				var definition = check(members[i]);
				for (int j = i + 1; j < members.Count && j <= i + OtherMembersToCompare; j++) {
					if (CanBeEqual(members[i], members[j]))
						continue;
					Require(!equalsOther(definition, members[j]), members[i], "its definition equals another member: " + members[j]);
				}
				CheckedMembers++;
			}
		}

		// Two members of a type always differ in name or signature. But if names are compared without case,
		// members whose names only differ in case can be equal (eg. the fields 'G' and 'g').
		bool CanBeEqual(MemberInfo a, MemberInfo b) =>
			(options & SigComparerOptions.CaseInsensitiveAll) != 0 && string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

		object CheckMethod(MethodBase method) {
			var methodDef = Resolve<MethodDef>(method);
			if (!ReflectionHasFunctionPointerTypes && UsesFunctionPointer(methodDef.MethodSig))
				return methodDef;
			Require(comparer.Equals(methodDef, method), method, "the MethodDef doesn't equal the MethodBase");
			Require(comparer.GetHashCode(methodDef) == comparer.GetHashCode(method), method, "the MethodDef and the MethodBase have different hash codes");

			var imported = importer.ImportOrNull(method);
			Require(imported is not null, method, "the MethodBase can't be imported");
			Require(comparer.Equals(imported, method), method, "the imported method doesn't equal the MethodBase");
			return methodDef;
		}

		object CheckField(FieldInfo field) {
			var fieldDef = Resolve<FieldDef>(field);
			if (!ReflectionHasFunctionPointerTypes && UsesFunctionPointer(fieldDef.FieldType))
				return fieldDef;
			Require(comparer.Equals(fieldDef, field), field, "the FieldDef doesn't equal the FieldInfo");
			Require(comparer.GetHashCode(fieldDef) == comparer.GetHashCode(field), field, "the FieldDef and the FieldInfo have different hash codes");

			var imported = importer.ImportOrNull(field);
			Require(imported is not null, field, "the FieldInfo can't be imported");
			Require(comparer.Equals(imported, field), field, "the imported field doesn't equal the FieldInfo");
			return fieldDef;
		}

		object CheckProperty(PropertyInfo property) {
			var propertyDef = Resolve<PropertyDef>(property);
			Require(comparer.Equals(propertyDef, property), property, "the PropertyDef doesn't equal the PropertyInfo");
			Require(comparer.GetHashCode(propertyDef) == comparer.GetHashCode(property), property, "the PropertyDef and the PropertyInfo have different hash codes");
			return propertyDef;
		}

		object CheckEvent(EventInfo @event) {
			var eventDef = Resolve<EventDef>(@event);
			Require(comparer.Equals(eventDef, @event), @event, "the EventDef doesn't equal the EventInfo");
			Require(comparer.GetHashCode(eventDef) == comparer.GetHashCode(@event), @event, "the EventDef and the EventInfo have different hash codes");
			return eventDef;
		}

		// Reflection has function pointer types since .NET 8. Before that, the type of a function pointer
		// parameter or field is IntPtr (.NET Framework) or a fake class (Mono), so there's nothing to compare
		// such a signature with.
#if NET
		const bool ReflectionHasFunctionPointerTypes = true;
#else
		const bool ReflectionHasFunctionPointerTypes = false;
#endif

		static bool UsesFunctionPointer(MethodSig? signature) {
			if (signature is null)
				return false;
			if (UsesFunctionPointer(signature.RetType))
				return true;
			foreach (var parameter in signature.Params) {
				if (UsesFunctionPointer(parameter))
					return true;
			}
			return false;
		}

		static bool UsesFunctionPointer(TypeSig? type) {
			// Arrays, pointers, modifiers etc have the type they're made of in Next
			for (; type is not null; type = type.Next) {
				if (type is FnPtrSig)
					return true;
				if (type is GenericInstSig genericInst) {
					foreach (var argument in genericInst.GenericArguments) {
						if (UsesFunctionPointer(argument))
							return true;
					}
				}
			}
			return false;
		}

		T Resolve<T>(MemberInfo member) where T : class {
			var definition = module.ResolveToken(member.MetadataToken) as T;
			if (definition is null)
				throw new ReflectionDifferenceException($"{Describe(member)}: dnlib has no {typeof(T).Name} with token 0x{member.MetadataToken:X8}");
			return definition;
		}

		static void Require(bool condition, MemberInfo member, string problem) {
			if (!condition)
				throw new ReflectionDifferenceException(Describe(member) + ": " + problem);
		}

		static string Describe(MemberInfo member) {
			var declaringType = member.DeclaringType;
			return declaringType is null ? member.ToString()! : declaringType + " :: " + member;
		}
	}
}
