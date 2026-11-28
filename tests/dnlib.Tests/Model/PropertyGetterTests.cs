// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Every property of the object model can be read, also the ones that only an interface has: reading a property
	/// never throws, except an <c>OrThrow</c> property whose value is missing (eg. a reference that can't be resolved)
	/// and a property that a kind of object doesn't support. The objects are the ones of
	/// <see cref="PropertySetterTests"/> without
	/// the parts of the writer: most of their values only exist while the writer writes.
	/// </summary>
	public class PropertyGetterTests {
		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image));

		public static TheoryData<string> ObjectKinds => new TheoryData<string> { "module", "user", "debug info" };

		// The method bodies, signatures, custom attributes and the debug info of a module with a PDB
		internal static IEnumerable<object> GetDebugInfoObjects(ModuleDefMD module) {
			yield return module.PdbState!;
			foreach (var document in module.PdbState!.Documents)
				yield return document;
			foreach (var info in module.CustomDebugInfos)
				yield return info;
			foreach (var type in module.GetTypes()) {
				foreach (var ca in type.CustomAttributes) {
					yield return ca;
					foreach (var argument in ca.ConstructorArguments)
						yield return argument;
					foreach (var named in ca.NamedArguments)
						yield return named;
				}
				foreach (var method in type.Methods) {
					yield return method.MethodSig!;
					foreach (var parameter in method.Parameters)
						yield return parameter;
					if (method.MethodSig?.RetType is TypeSig retType)
						yield return retType;
					foreach (var info in method.CustomDebugInfos)
						yield return info;
					var body = method.Body;
					if (body is null)
						continue;
					yield return body;
					foreach (var instruction in body.Instructions) {
						yield return instruction;
						if (instruction.SequencePoint is not null)
							yield return instruction.SequencePoint;
					}
					foreach (var handler in body.ExceptionHandlers)
						yield return handler;
					foreach (var local in body.Variables)
						yield return local;
					if (body.PdbMethod?.Scope is dnlib.DotNet.Pdb.PdbScope scope) {
						yield return body.PdbMethod;
						foreach (var o in GetScopeObjects(scope))
							yield return o;
					}
				}
			}
		}

		static IEnumerable<object> GetScopeObjects(dnlib.DotNet.Pdb.PdbScope scope) {
			yield return scope;
			foreach (var local in scope.Variables)
				yield return local;
			foreach (var constant in scope.Constants)
				yield return constant;
			if (scope.ImportScope is not null) {
				yield return scope.ImportScope;
				foreach (var import in scope.ImportScope.Imports)
					yield return import;
			}
			foreach (var child in scope.Scopes) {
				foreach (var o in GetScopeObjects(child))
					yield return o;
			}
		}

		// The public properties and the properties of the interfaces that the type implements explicitly
		static IEnumerable<MethodInfo> GetGetters(Type type) {
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (property.GetIndexParameters().Length == 0 && property.GetMethod is { IsPublic: true } getter)
					yield return getter;
			}
			foreach (var interfaceType in type.GetInterfaces()) {
				if (interfaceType.Assembly != typeof(ModuleDef).Assembly)
					continue;
				var map = type.GetInterfaceMap(interfaceType);
				for (int i = 0; i < map.TargetMethods.Length; i++) {
					var target = map.TargetMethods[i];
					if (target.IsPrivate && target.GetParameters().Length == 0 && map.InterfaceMethods[i].Name.StartsWith("get_", StringComparison.Ordinal))
						yield return target;
				}
			}
		}

		static bool IsAllowedException(MethodInfo getter, Exception exception) =>
			getter.Name.EndsWith("OrThrow", StringComparison.Ordinal) || exception is NotSupportedException;

		[Theory, MemberData(nameof(ObjectKinds))]
		public void EveryPropertyCanBeRead(string kind) {
			using var debugInfoModule = kind == "debug info" ? dnlib.Tests.Pdb.DebugInfoSource.Load() : null;
			var objects = (kind == "module" ? PropertySetterTests.GetObjectsOfTheModule(module.Value) :
				kind == "user" ? PropertySetterTests.GetUserObjects() : GetDebugInfoObjects(debugInfoModule!))
				.Where(a => a.GetType().Namespace != "dnlib.DotNet.Writer").ToList();
			int getters = 0;
			foreach (var o in objects) {
				foreach (var getter in GetGetters(o.GetType())) {
					try {
						getter.Invoke(o, null);
					}
					catch (TargetInvocationException ex) when (IsAllowedException(getter, ex.InnerException!)) {
					}
					getters++;
				}
				Assert.NotNull(o.ToString());
			}
			Assert.True(getters > 500, getters.ToString());
		}
	}
}
