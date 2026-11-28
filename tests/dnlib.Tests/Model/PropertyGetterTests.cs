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

		public static TheoryData<string> ObjectKinds => new TheoryData<string> { "module", "user" };

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
			var objects = (kind == "module" ? PropertySetterTests.GetObjectsOfTheModule(module.Value) : PropertySetterTests.GetUserObjects())
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
