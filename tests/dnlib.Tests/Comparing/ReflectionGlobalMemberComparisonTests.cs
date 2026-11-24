// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// <see cref="SigComparer"/> and the methods and fields of System.Reflection that aren't in a class: global
	/// methods and fields are members of the <c>&lt;Module&gt;</c> type in the metadata, but Reflection returns
	/// them from <see cref="Module.GetMethods()"/> and their <see cref="MemberInfo.DeclaringType"/> is null (the
	/// CLR; on Mono it's the type <c>&lt;Module&gt;</c>). A
	/// <see cref="MemberRef"/> can reference them with a <see cref="ModuleRef"/> as its parent. C# has no
	/// global members, so the module is created with dnlib and loaded.
	/// </summary>
	public class ReflectionGlobalMemberComparisonTests {
		const string ModuleName = "GlobalMembers.dll";

		// static int GlobalMethod(int), static int GlobalField, and a class Ns.C with the same members
		static byte[] CreateImage() {
			// Mono doesn't load a module without an MVID
			var mvid = new Guid("3a0b3ad4-3c08-4c0d-9d57-0c3a7c1d2e41");
			var module = new ModuleDefUser(ModuleName, mvid, new AssemblyRefUser(typeof(object).Assembly.GetName())) { Kind = ModuleKind.Dll };
			new AssemblyDefUser("GlobalMembers", new Version(1, 0, 0, 0)).Modules.Add(module);
			var type = new TypeDefUser("Ns", "C", module.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed };
			module.Types.Add(type);
			foreach (var declaringType in new[] { module.GlobalType!, type }) {
				var method = new MethodDefUser("GlobalMethod", MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
				method.Body = new CilBody();
				method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
				method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
				declaringType.Methods.Add(method);
				declaringType.Fields.Add(new FieldDefUser("GlobalField", new FieldSig(module.CorLibTypes.Int32), FieldAttributes.Public | FieldAttributes.Static));
			}
			var stream = new MemoryStream();
			module.Write(stream);
			return stream.ToArray();
		}

		static readonly byte[] image = CreateImage();
		const SigComparerOptions CompareDeclaringTypes = SigComparerOptions.CompareMethodFieldDeclaringType;

		[Fact]
		public void GlobalDefinitionsEqualTheReflectionMembersOfTheModule() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "On Mono, the declaring type of a global member is the type <Module>, not null");

			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				var reflectionModule = loaded.Assembly.ManifestModule;
				var globalMethod = reflectionModule.GetMethod("GlobalMethod")!;
				var globalField = reflectionModule.GetField("GlobalField")!;
				var methodInClass = loaded.Assembly.GetType("Ns.C")!.GetMethod("GlobalMethod")!;
				var fieldInClass = loaded.Assembly.GetType("Ns.C")!.GetField("GlobalField")!;
				Assert.Null(globalMethod.DeclaringType);
				Assert.Equal(123, globalMethod.Invoke(null, new object[] { 123 }));
				var comparer = new SigComparer(CompareDeclaringTypes);
				var methodDef = module.GlobalType!.FindMethod("GlobalMethod")!;
				var fieldDef = module.GlobalType!.FindField("GlobalField")!;
				var type = module.Find("Ns.C", isReflectionName: true)!;

				Assert.True(comparer.Equals(methodDef, globalMethod));
				Assert.True(comparer.Equals(globalMethod, methodDef));
				Assert.True(comparer.Equals(fieldDef, globalField));
				Assert.True(comparer.Equals(globalField, fieldDef));
				Assert.Equal(comparer.GetHashCode(globalMethod), comparer.GetHashCode(methodDef));
				Assert.Equal(comparer.GetHashCode(globalField), comparer.GetHashCode(fieldDef));

				// The members of the class have the same names and signatures: only the declaring type is different
				Assert.False(comparer.Equals(methodDef, methodInClass));
				Assert.False(comparer.Equals(type.FindMethod("GlobalMethod"), globalMethod));
				Assert.False(comparer.Equals(fieldDef, fieldInClass));
				Assert.False(comparer.Equals(type.FindField("GlobalField"), globalField));
				Assert.True(new SigComparer().Equals(methodDef, methodInClass));
				Assert.True(new SigComparer().Equals(fieldDef, fieldInClass));
			}
		}

		// The parent of a reference to a global member is a ModuleRef with the name of the module
		[Fact]
		public void ReferenceWithAModuleRefParentEqualsTheGlobalReflectionMember() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "On Mono, the declaring type of a global member is the type <Module>, not null");

			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				var reflectionModule = loaded.Assembly.ManifestModule;
				var globalMethod = reflectionModule.GetMethod("GlobalMethod")!;
				var globalField = reflectionModule.GetField("GlobalField")!;
				var methodInClass = loaded.Assembly.GetType("Ns.C")!.GetMethod("GlobalMethod")!;
				var comparer = new SigComparer(CompareDeclaringTypes);
				var methodSig = MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32);
				var fieldSig = new FieldSig(module.CorLibTypes.Int32);
				var thisModule = new ModuleRefUser(module, ModuleName);
				var methodRef = new MemberRefUser(module, "GlobalMethod", methodSig, thisModule);
				var fieldRef = new MemberRefUser(module, "GlobalField", fieldSig, thisModule);

				Assert.True(comparer.Equals(methodRef, globalMethod));
				Assert.True(comparer.Equals(globalMethod, (IMethod)methodRef));
				Assert.True(comparer.Equals(fieldRef, globalField));
				Assert.True(comparer.Equals(globalField, (IField)fieldRef));

				// A method of a class isn't a method of the module
				Assert.False(comparer.Equals(methodRef, methodInClass));
				// Another module of the assembly, and the reference to the class
				var otherModule = new ModuleRefUser(module, "Other.netmodule");
				Assert.False(comparer.Equals(new MemberRefUser(module, "GlobalMethod", methodSig, otherModule), globalMethod));
				Assert.False(comparer.Equals(new MemberRefUser(module, "GlobalField", fieldSig, otherModule), globalField));
				var classRef = new TypeRefUser(module, "Ns", "C", module);
				Assert.False(comparer.Equals(new MemberRefUser(module, "GlobalMethod", methodSig, classRef), globalMethod));
				Assert.True(comparer.Equals(new MemberRefUser(module, "GlobalMethod", methodSig, classRef), methodInClass));
				// Without the option, only the name and the signature are compared
				Assert.True(new SigComparer().Equals(new MemberRefUser(module, "GlobalMethod", methodSig, otherModule), globalMethod));
				Assert.False(new SigComparer().Equals(new MemberRefUser(module, "Other", methodSig, thisModule), globalMethod));
			}
		}

		// The parent can also be the method's definition (that's what a call of a vararg method has): the
		// member is in the type of that method
		[Fact]
		public void ReferenceWithAMethodDefParentIsInTheTypeOfThatMethod() {
			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				var globalMethod = loaded.Assembly.ManifestModule.GetMethod("GlobalMethod")!;
				var methodInClass = loaded.Assembly.GetType("Ns.C")!.GetMethod("GlobalMethod")!;
				var comparer = new SigComparer(CompareDeclaringTypes);
				var methodDefInClass = module.Find("Ns.C", isReflectionName: true)!.FindMethod("GlobalMethod")!;
				var methodRef = new MemberRefUser(module, "GlobalMethod", methodDefInClass.MethodSig, methodDefInClass);

				Assert.True(comparer.Equals(methodRef, methodInClass));
				Assert.True(comparer.Equals(methodInClass, methodRef));
				Assert.False(comparer.Equals(methodRef, globalMethod));
				Assert.False(comparer.Equals(methodRef, typeof(Math).GetMethod("Abs", new[] { typeof(int) })));
			}
		}

		// The global type itself: Reflection has no Type for it that a member returns
		[Fact]
		public void GlobalTypeIsNotAReflectionType() {
			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				var comparer = new SigComparer();
				var reflectionClass = loaded.Assembly.GetType("Ns.C")!;

				Assert.False(comparer.Equals(module.GlobalType, reflectionClass));
				Assert.True(comparer.Equals(module.Find("Ns.C", isReflectionName: true), reflectionClass));
				Assert.DoesNotContain(loaded.Assembly.GetTypes(), a => a.Name == "<Module>");
			}
		}
	}
}
