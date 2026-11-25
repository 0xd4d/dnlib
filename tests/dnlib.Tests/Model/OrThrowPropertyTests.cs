// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Many properties can be <c>null</c> if the metadata is invalid or if an object isn't initialized, eg.
	/// <see cref="FieldDef.FieldSig"/>. Each has a twin whose name ends with <c>OrThrow</c> that never returns
	/// <c>null</c>, eg. <see cref="FieldDef.FieldSigOrThrow"/>:
	/// <list type="bullet">
	/// <item>if the property isn't <c>null</c>, the twin returns the same object</item>
	/// <item>if it's <c>null</c>, the twin throws <see cref="InvalidMetadataException"/> (the value is required
	/// in valid metadata) or <see cref="NullValueException"/> (the value is optional)</item>
	/// <item>if the property is a list that can have <c>null</c> elements, the twin returns the same list if it
	/// has none and throws if it has one</item>
	/// </list>
	/// The test checks this for every such pair of every object it can find.
	/// </summary>
	public class OrThrowPropertyTests {
		const string Suffix = "OrThrow";

		static bool IsOrThrow(PropertyInfo property) => property.Name.EndsWith(Suffix, StringComparison.Ordinal);

		// Objects whose properties have values: everything in a valid module with debug info
		static List<object> GetObjectsOfValidModule() {
			var features = Fixtures.Fixtures.Features;
			var module = RoundTrip.LoadWithPdb(features.Image, features.Pdb!);
			var objects = ObjectGraph.GetReachableObjects(new object[] { module }, property => !IsOrThrow(property));
			// MethodOverride is a struct, which ObjectGraph doesn't return
			foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
				foreach (var methodOverride in method.Overrides)
					objects.Add(methodOverride);
			}
			return objects;
		}

		// Objects whose properties are null: new objects that were created with the API and not initialized
		static List<object> GetUninitializedObjects() {
			var module = new ModuleDefUser();
			var objects = new List<object> {
				module,
				new AssemblyDefUser(),
				new TypeDefUser("Type"),
				new TypeRefUser(null, "TypeRef"),
				new TypeSpecUser(),
				new FieldDefUser(),
				new MethodDefUser(),
				new PropertyDefUser(),
				new EventDefUser(),
				new MemberRefUser(module),
				new MethodSpecUser(),
				new StandAloneSigUser(),
				new InterfaceImplUser(),
				new GenericParamUser(),
				new GenericParamConstraintUser(),
				new ExportedTypeUser(module),
				new MethodOverride(),
				new CustomAttribute(null),
				new MethodSig(),
				new FieldSig(),
				new PropertySig(),
				new LocalSig(),
				new GenericInstMethodSig(),
				new GenericInstSig(),
				new PtrSig(null),
				new CModReqdSig(null, null),
				new ClassSig(null),
				new CilBody(),
				new ExceptionHandler(),
				new Local(null),
				new dnlib.DotNet.Pdb.PdbMethod(),
				new dnlib.DotNet.Pdb.PdbScope(),
				new ExportedTypeUser(null),
				new MemberRefUser(null),
				new ModuleRefUser(null),
				new ImplMapUser(),
				new FnPtrSig(null),
				new Parameter(0),
			};
			// A new module has the global type, <Module>
			var moduleWithoutTypes = new ModuleDefUser();
			moduleWithoutTypes.Types.Clear();
			objects.Add(moduleWithoutTypes);
			// A signature with a null in its lists
			var methodSig = new MethodSig { RetType = null };
			methodSig.Params.Add(null);
			methodSig.ParamsAfterSentinel = new List<TypeSig?> { null };
			objects.Add(methodSig);
			var localSig = new LocalSig();
			localSig.Locals.Add(null);
			objects.Add(localSig);
			var genericInstSig = new GenericInstSig();
			genericInstSig.GenericArguments.Add(null);
			objects.Add(genericInstSig);
			var genericInstMethodSig = new GenericInstMethodSig();
			genericInstMethodSig.GenericArguments.Add(null);
			objects.Add(genericInstMethodSig);
			return objects;
		}

		[Fact]
		public void EveryOrThrowPropertyAgreesWithItsTwin() {
			var objects = GetObjectsOfValidModule();
			objects.AddRange(GetUninitializedObjects());

			var returnedTheValue = new HashSet<string>();
			var threw = new HashSet<string>();
			foreach (var obj in objects) {
				foreach (var orThrowProperty in ObjectGraph.GetReadableProperties(obj.GetType()).Where(IsOrThrow)) {
					var twinName = orThrowProperty.Name.Substring(0, orThrowProperty.Name.Length - Suffix.Length);
					var name = orThrowProperty.DeclaringType!.Name + "." + orThrowProperty.Name;
					// The twin is a property, or a public field in a few old classes (eg. ExceptionHandler.TryStart)
					var twinProperty = obj.GetType().GetProperty(twinName, BindingFlags.Public | BindingFlags.Instance);
					var twinField = obj.GetType().GetField(twinName, BindingFlags.Public | BindingFlags.Instance);
					Assert.True(twinProperty is not null || twinField is not null, $"{name} has no property or field called {twinName}");
					var value = twinProperty is not null ? twinProperty.GetValue(obj, null) : twinField!.GetValue(obj);
					var twinType = twinProperty is not null ? twinProperty.PropertyType : twinField!.FieldType;
					if (value is not null && !HasNullElement(value)) {
						Assert.True(ReferenceEquals(value, orThrowProperty.GetValue(obj, null)), $"{name} didn't return the value of {twinName}");
						returnedTheValue.Add(name);
					}
					else if (value is null && IsListOfSignatures(twinType)) {
						// The OrThrow twin of a list only promises that no element is null. A list that's
						// optional (MethodSig.ParamsAfterSentinel) is still null if there's no list.
						Assert.True(orThrowProperty.GetValue(obj, null) is null, $"{name} didn't return null");
					}
					else {
						Exception? exception = null;
						try {
							orThrowProperty.GetValue(obj, null);
						}
						catch (TargetInvocationException ex) {
							exception = ex.InnerException;
						}
						Assert.True(exception is InvalidMetadataException or NullValueException,
							$"{name} must throw InvalidMetadataException or NullValueException, but it " + (exception is null ? "returned" : "threw " + exception.GetType().Name));
						threw.Add(name);
					}
				}
			}

			// Every OrThrow property of the object model must have been tested both ways, so a new one that
			// the objects above don't reach is noticed
			var all = GetAllOrThrowProperties();
			Assert.True(all.IsSubsetOf(returnedTheValue), "Not tested with a value: " + string.Join(", ", all.Except(returnedTheValue).OrderBy(a => a)));
			Assert.True(all.IsSubsetOf(threw), "Not tested without a value: " + string.Join(", ", all.Except(threw).OrderBy(a => a)));
		}

		// A list of signatures with a null in it, eg. MethodSig.Params
		static bool HasNullElement(object value) =>
			value is IEnumerable<TypeSig?> list && list.Contains(null);

		static bool IsListOfSignatures(Type type) => typeof(IEnumerable<TypeSig?>).IsAssignableFrom(type);

		static HashSet<string> GetAllOrThrowProperties() {
			var names = new HashSet<string>();
			foreach (var type in typeof(ModuleDef).Assembly.GetTypes()) {
				if (!type.IsPublic && !type.IsNestedPublic)
					continue;
				foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
					if (IsOrThrow(property) && !type.IsInterface)
						names.Add(type.Name + "." + property.Name);
				}
			}
			return names;
		}
	}
}
