// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// Every <c>Equals()</c> and <c>GetHashCode()</c> overload of <see cref="SigComparer"/> accepts null: null
	/// equals null and nothing else, and its hash code is 0. The overloads are found with reflection, so a new
	/// overload is tested too.
	///
	/// The exception: the declaring type of a global method or field is null (a <see cref="Type"/> and a
	/// <see cref="TypeDef"/>), so a null type is the global type, &lt;Module&gt;.
	/// </summary>
	public class SigComparerNullTests {
		static readonly ModuleDefUser module = CreateModule();

		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);
			return module;
		}

		// The reflection members that are the samples of System.Reflection's kinds of members
		static class Members {
			public static int Field = 1;
			public static int Property => 0;
			public static event Action? Event;
			public static void Method(int value) => Event?.Invoke();
		}

		// An object of each kind that the overloads take. The first one that a parameter type accepts is used.
		static readonly object[] samples = CreateSamples();

		static object[] CreateSamples() {
			var types = module.CorLibTypes;
			var type = new TypeDefUser("Ns", "Type", types.Object.TypeDefOrRef);
			module.Types.Add(type);
			var method = new MethodDefUser("Method", MethodSig.CreateStatic(types.Void, types.Int32), MethodAttributes.Public | MethodAttributes.Static);
			type.Methods.Add(method);
			var field = new FieldDefUser("Field", new FieldSig(types.Int32), FieldAttributes.Public | FieldAttributes.Static);
			type.Fields.Add(field);
			var property = new PropertyDefUser("Property", PropertySig.CreateStatic(types.Int32));
			type.Properties.Add(property);
			var eventDef = new EventDefUser("Event", types.GetTypeRef("System", "Action"));
			type.Events.Add(eventDef);
			var typeRef = new TypeRefUser(module, "Ns", "Type", module);
			var methodRef = new MemberRefUser(module, "Method", MethodSig.CreateStatic(types.Void, types.Int32), typeRef);
			var thisType = typeof(Members);
			return new object[] {
				type, typeRef, new TypeSpecUser(new SZArraySig(types.Int32)), new ClassSig(typeRef),
				new ExportedTypeUser(module, 0, "Ns", "Exported", TypeAttributes.Public, new AssemblyRefUser("Other")),
				method, methodRef, field, property, eventDef,
				new MethodSpecUser(methodRef, new GenericInstMethodSig(types.Int32)),
				MethodSig.CreateStatic(types.Void), new FieldSig(types.Int32), PropertySig.CreateStatic(types.Int32),
				new LocalSig(types.Int32), new GenericInstMethodSig(types.Int32),
				new List<TypeSig?> { types.Int32 },
				new AssemblyRefUser("Other"), module, new ModuleRefUser(module, "Other.dll"),
				thisType, thisType.GetMethod(nameof(Members.Method))!, thisType.GetField(nameof(Members.Field))!,
				thisType.GetProperty(nameof(Members.Property))!, thisType.GetEvent(nameof(Members.Event))!, thisType.Module, thisType.Assembly,
			};
		}

		static object? GetSample(Type parameterType) => samples.FirstOrDefault(a => parameterType.IsInstanceOfType(a));

		static IEnumerable<MethodInfo> GetOverloads(string name, int parameterCount) =>
			typeof(SigComparer).GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(a => a.Name == name && a.GetParameters().Length == parameterCount && a.GetParameters().All(p => !p.ParameterType.IsValueType))
				.Where(a => a.DeclaringType == typeof(SigComparer));

		public static TheoryData<string> EqualsOverloads {
			get {
				var data = new TheoryData<string>();
				foreach (var method in GetOverloads(nameof(SigComparer.Equals), 2))
					data.Add(string.Join(", ", method.GetParameters().Select(a => a.ParameterType.FullName)));
				return data;
			}
		}

		public static TheoryData<string> GetHashCodeOverloads {
			get {
				var data = new TheoryData<string>();
				foreach (var method in GetOverloads(nameof(SigComparer.GetHashCode), 1))
					data.Add(method.GetParameters()[0].ParameterType.FullName!);
				return data;
			}
		}

		static MethodInfo FindOverload(string name, string parameters) =>
			GetOverloads(name, name == nameof(SigComparer.Equals) ? 2 : 1).Single(a => string.Join(", ", a.GetParameters().Select(p => p.ParameterType.FullName)) == parameters);

		[Fact]
		public void TheOverloadsAreFound() {
			Assert.True(EqualsOverloads.Count() > 40);
			Assert.True(GetHashCodeOverloads.Count() > 20);
		}

		[Theory, MemberData(nameof(EqualsOverloads))]
		public void NullOnlyEqualsNull(string parameters) {
			var method = FindOverload(nameof(SigComparer.Equals), parameters);
			var parameterTypes = method.GetParameters().Select(a => a.ParameterType).ToArray();
			var a = GetSample(parameterTypes[0]);
			var b = GetSample(parameterTypes[1]);
			Assert.True(a is not null && b is not null, $"No sample for {parameters}");
			object comparer = new SigComparer();

			// The other side of a comparison with a System.Type is a dnlib type: a null Type is the global type,
			// and nothing equals a null dnlib type
			bool withReflectionType = parameterTypes.Contains(typeof(Type));
			Assert.Equal(!withReflectionType, (bool)method.Invoke(comparer, new object?[] { null, null })!);
			Assert.False((bool)method.Invoke(comparer, new object?[] { null, b })!);
			Assert.False((bool)method.Invoke(comparer, new object?[] { a, null })!);
		}

		[Theory, MemberData(nameof(GetHashCodeOverloads))]
		public void TheHashCodeOfNullIsZero(string parameter) {
			var method = FindOverload(nameof(SigComparer.GetHashCode), parameter);
			object comparer = new SigComparer();

			int expected = parameter == typeof(TypeDef).FullName || parameter == typeof(Type).FullName ? GlobalTypeHashCode : 0;
			Assert.Equal(expected, (int)method.Invoke(comparer, new object?[] { null })!);
		}

		static int GlobalTypeHashCode => new SigComparer().GetHashCode(new ModuleDefUser("Other.dll").GlobalType);

		// A null type is the global type, so it equals the global type of a module
		[Fact]
		public void ANullTypeIsTheGlobalType() {
			var globalType = new ModuleDefUser("Other.dll").GlobalType!;
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(globalType, (Type?)null));
			Assert.Equal(comparer.GetHashCode(globalType), comparer.GetHashCode((TypeDef?)null));
			Assert.Equal(comparer.GetHashCode(globalType), comparer.GetHashCode((Type?)null));
		}
	}
}
