// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="MemberFinder"/> starts at a module and collects everything it can reach: the definitions,
	/// and the references and signatures they use (base types, member types, method bodies, custom attributes)
	/// </summary>
	public class MemberFinderTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);
		static readonly MemberFinder finder = new MemberFinder().FindAll(module);

		static void AssertSameItems<T>(IEnumerable<T> expected, Dictionary<T, bool> actual) where T : class {
			var expectedSet = new HashSet<T>(expected);
			Assert.NotEmpty(expectedSet);
			var missing = expectedSet.Where(a => !actual.ContainsKey(a)).ToList();
			Assert.True(missing.Count == 0, $"Not found: {string.Join(", ", missing.Take(5))}");
			var extra = actual.Keys.Where(a => !expectedSet.Contains(a)).ToList();
			Assert.True(extra.Count == 0, $"Found but not in the module: {string.Join(", ", extra.Take(5))}");
		}

		static IEnumerable<TypeDef> AllTypes => module.GetTypes();

		[Fact]
		public void AllDefinitionsAreFound() {
			AssertSameItems(AllTypes, finder.TypeDefs);
			AssertSameItems(AllTypes.SelectMany(a => a.Methods), finder.MethodDefs);
			AssertSameItems(AllTypes.SelectMany(a => a.Fields), finder.FieldDefs);
			AssertSameItems(AllTypes.SelectMany(a => a.Properties), finder.PropertyDefs);
			AssertSameItems(AllTypes.SelectMany(a => a.Events), finder.EventDefs);
			AssertSameItems(AllTypes.SelectMany(a => a.GenericParameters).Concat(AllTypes.SelectMany(a => a.Methods).SelectMany(a => a.GenericParameters)), finder.GenericParams);
		}

		[Fact]
		public void ExportedTypesAreFound() => AssertSameItems(module.ExportedTypes, finder.ExportedTypes);

		static IEnumerable<object> GetOperands() =>
			AllTypes.SelectMany(a => a.Methods).Where(a => a.Body is not null).SelectMany(a => a.Body!.Instructions).Select(a => a.Operand).OfType<object>();

		[Fact]
		public void ReferencesInMethodBodiesAreFound() {
			var memberRefs = GetOperands().OfType<MemberRef>().ToList();
			var methodSpecs = GetOperands().OfType<MethodSpec>().ToList();
			var typeRefs = GetOperands().OfType<TypeRef>().ToList();
			var typeSpecs = GetOperands().OfType<TypeSpec>().ToList();
			Assert.NotEmpty(memberRefs);
			Assert.NotEmpty(methodSpecs);
			Assert.NotEmpty(typeRefs);
			Assert.NotEmpty(typeSpecs);

			Assert.All(memberRefs, a => Assert.True(finder.MemberRefs.ContainsKey(a), a.FullName));
			Assert.All(methodSpecs, a => Assert.True(finder.MethodSpecs.ContainsKey(a), a.FullName));
			Assert.All(typeRefs, a => Assert.True(finder.TypeRefs.ContainsKey(a), a.FullName));
			Assert.All(typeSpecs, a => Assert.True(finder.TypeSpecs.ContainsKey(a), a.FullName));
		}

		[Fact]
		public void BaseTypesInterfacesAndSignatureTypesAreFound() {
			foreach (var type in AllTypes) {
				if (type.BaseType is TypeRef baseType)
					Assert.True(finder.TypeRefs.ContainsKey(baseType), baseType.FullName);
				foreach (var interfaceType in type.Interfaces.Select(a => a.Interface).OfType<TypeRef>())
					Assert.True(finder.TypeRefs.ContainsKey(interfaceType), interfaceType.FullName);
				foreach (var field in type.Fields)
					Assert.True(finder.TypeSigs.ContainsKey(field.FieldSig!.Type!), field.FullName);
				foreach (var method in type.Methods)
					Assert.True(finder.TypeSigs.ContainsKey(method.MethodSig!.RetType!), method.FullName);
			}
		}

		[Fact]
		public void CustomAttributesAndTheirConstructorsAreFound() {
			var attributes = AllTypes.SelectMany(a => a.CustomAttributes)
				.Concat(AllTypes.SelectMany(a => a.Methods).SelectMany(a => a.CustomAttributes))
				.Concat(module.CustomAttributes)
				.Concat(module.Assembly!.CustomAttributes)
				.ToList();
			Assert.NotEmpty(attributes);

			foreach (var attribute in attributes) {
				Assert.True(finder.CustomAttributes.ContainsKey(attribute), attribute.TypeFullName);
				if (attribute.Constructor is MemberRef constructor)
					Assert.True(finder.MemberRefs.ContainsKey(constructor), constructor.FullName);
			}
		}

		[Fact]
		public void NothingIsFoundInAnEmptyModule() {
			var emptyModule = new ModuleDefUser("Empty.dll");

			var emptyFinder = new MemberFinder().FindAll(emptyModule);

			// Only the <Module> type that every module has
			Assert.Same(emptyModule.GlobalType, Assert.Single(emptyFinder.TypeDefs.Keys));
			Assert.Empty(emptyFinder.MethodDefs);
			Assert.Empty(emptyFinder.MemberRefs);
			Assert.Empty(emptyFinder.TypeRefs);
		}

		// A type that's only used inside another signature must be found too
		[Fact]
		public void TypeRefsInsideFunctionPointersAndModifiersAreFound() {
			var created = new dnlib.Tests.Writer.CreatedModule();
			var testModule = created.Module;
			var library = new AssemblyRefUser("Library", new System.Version(1, 0, 0, 0));
			var inFunctionPointer = new TypeRefUser(testModule, "Library", "InFunctionPointer", library);
			var inModifier = new TypeRefUser(testModule, "Library", "InModifier", library);
			var inModifierOfGenericParameter = new TypeRefUser(testModule, "Library", "InModifierOfGenericParameter", library);
			created.Type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			created.AddField("FunctionPointer", new FnPtrSig(MethodSig.CreateStatic(testModule.CorLibTypes.Void, new ClassSig(inFunctionPointer))));
			created.AddField("Modifier", new CModOptSig(inModifier, testModule.CorLibTypes.Int32));
			created.AddField("ModifierOfGenericParameter", new CModOptSig(inModifierOfGenericParameter, new GenericVar(0, created.Type)));

			var found = new MemberFinder().FindAll(testModule);
			Assert.Contains(inFunctionPointer, found.TypeRefs.Keys);
			Assert.Contains(inModifier, found.TypeRefs.Keys);
			Assert.Contains(inModifierOfGenericParameter, found.TypeRefs.Keys);

			using (var read = created.WriteAndRead()) {
				var names = read.GetTypeRefs().Select(a => a.Name.String).ToList();
				Assert.Contains("InFunctionPointer", names);
				Assert.Contains("InModifier", names);
				Assert.Contains("InModifierOfGenericParameter", names);
			}
		}
	}
}
