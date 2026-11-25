// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// A type can have several fields or methods with the same name and signature if all but one are "private
	/// scope" (no access flags; such a member can only be used by its token). A reference by name (MemberRef)
	/// then matches more than one of them, and dnlib must find the one the CLR finds.
	///
	/// Every test creates a type with such members and a <c>Get</c> method that uses a MemberRef. The file
	/// is loaded and <c>Get</c> is called: it returns the number of the member the runtime found (1 is the
	/// first one in the type). dnlib must resolve the MemberRef to the same member.
	/// </summary>
	public class DuplicateMemberResolutionTests {
		readonly ModuleDefUser module;
		readonly TypeDefUser type;
		readonly MethodDefUser staticConstructor;

		ICorLibTypes CorLib => module.CorLibTypes;

		public DuplicateMemberResolutionTests() {
			module = new ModuleDefUser("Created.dll") { Kind = ModuleKind.Dll };
			new AssemblyDefUser("Created", new Version(1, 0, 0, 0)).Modules.Add(module);
			type = new TypeDefUser("", "Holder", CorLib.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public };
			module.Types.Add(type);
			staticConstructor = new MethodDefUser(".cctor", MethodSig.CreateStatic(CorLib.Void), MethodImplAttributes.IL,
				MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName) { Body = new CilBody() };
		}

		static void SkipOnMono() =>
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono finds members in another order than the CLR");

		// Adds a static int field named Field. The static constructor stores its number in it.
		void AddField(FieldAttributes attributes, bool threadStatic = false) {
			var field = new FieldDefUser("Field", new FieldSig(CorLib.Int32), attributes | FieldAttributes.Static);
			if (threadStatic) {
				var attributeType = CorLib.GetTypeRef("System", "ThreadStaticAttribute");
				field.CustomAttributes.Add(new CustomAttribute(new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(CorLib.Void), attributeType)));
			}
			type.Fields.Add(field);
			staticConstructor.Body!.Instructions.Add(Instruction.CreateLdcI4(type.Fields.Count));
			staticConstructor.Body.Instructions.Add(OpCodes.Stsfld.ToInstruction(field));
		}

		// Adds 'int Method()' that returns its number
		MethodDefUser AddMethod(TypeDef owner, MethodAttributes attributes) {
			bool isStatic = (attributes & MethodAttributes.Static) != 0;
			var signature = isStatic ? MethodSig.CreateStatic(CorLib.Int32) : MethodSig.CreateInstance(CorLib.Int32);
			var method = new MethodDefUser("Method", signature, MethodImplAttributes.IL, attributes | MethodAttributes.HideBySig) { Body = new CilBody() };
			owner.Methods.Add(method);
			method.Body.Instructions.Add(Instruction.CreateLdcI4(owner.Methods.Count));
			method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			return method;
		}

		MethodDefUser AddConstructor(TypeDef owner, ITypeDefOrRef baseType) {
			var constructor = new MethodDefUser(".ctor", MethodSig.CreateInstance(CorLib.Void), MethodImplAttributes.IL,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName) { Body = new CilBody() };
			constructor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			constructor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(CorLib.Void), baseType)));
			constructor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			owner.Methods.Add(constructor);
			return constructor;
		}

		// Adds 'public static int Get()' with the instructions and a ret, writes the module, calls Get() in the
		// runtime and checks that dnlib resolves the MemberRef in Get() to the member with that number
		void AssertDnlibFindsWhatTheRuntimeFinds(int expectedNumber, params Instruction[] instructions) {
			var get = new MethodDefUser("Get", MethodSig.CreateStatic(CorLib.Int32), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static) { Body = new CilBody() };
			foreach (var instruction in instructions)
				get.Body.Instructions.Add(instruction);
			get.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			type.Methods.Add(get);
			if (staticConstructor.Body!.Instructions.Count != 0) {
				staticConstructor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
				type.Methods.Add(staticConstructor);
			}
			var stream = new MemoryStream();
			module.Write(stream);
			var image = stream.ToArray();

			int numberInTheRuntime;
			// An assembly with a thread static field isn't unloaded, see LoadedAssembly
			bool hasThreadStaticField = type.Fields.Any(a => a.CustomAttributes.Count != 0);
			using (var loaded = new LoadedAssembly(image, canBeUnloaded: !hasThreadStaticField))
				numberInTheRuntime = (int)loaded.Assembly.GetType("Holder", throwOnError: true)!.GetMethod("Get")!.Invoke(null, null)!;

			using (var written = ModuleDefMD.Load(image)) {
				var writtenGet = written.Find("Holder", isReflectionName: true)!.FindMethod("Get")!;
				var reference = writtenGet.Body!.Instructions.Select(a => a.Operand).OfType<MemberRef>().Single(a => a.Name != ".ctor");
				// The MemberRef names a type of its own module
				var member = ((TypeDef)reference.Class!).Resolve(reference);
				int numberInDnlib;
				if (member is FieldDef field)
					numberInDnlib = field.DeclaringType!.Fields.IndexOf(field) + 1;
				else {
					var method = Assert.IsAssignableFrom<MethodDef>(member);
					numberInDnlib = method.DeclaringType!.Methods.IndexOf(method) + 1;
				}
				Assert.Equal(numberInTheRuntime, numberInDnlib);
			}
			// What the test expects to happen, so a change in the runtime or in both is noticed
			Assert.Equal(expectedNumber, numberInTheRuntime);
		}

		Instruction LoadFieldByName() => OpCodes.Ldsfld.ToInstruction(new MemberRefUser(module, "Field", new FieldSig(CorLib.Int32), type));

		[Fact]
		public void FirstOfTwoStaticFieldsIsFound() {
			SkipOnMono();
			AddField(FieldAttributes.PrivateScope);
			AddField(FieldAttributes.Public);

			AssertDnlibFindsWhatTheRuntimeFinds(1, LoadFieldByName());
		}

		[Fact]
		public void FirstOfTwoStaticFieldsIsFoundIfTheSecondIsPrivateScope() {
			SkipOnMono();
			AddField(FieldAttributes.Public);
			AddField(FieldAttributes.PrivateScope);

			AssertDnlibFindsWhatTheRuntimeFinds(1, LoadFieldByName());
		}

		// The runtime has the thread static fields after the other static fields
		[Fact]
		public void ThreadStaticFieldIsFoundAfterTheOtherStaticFields() {
			SkipOnMono();
			AddField(FieldAttributes.PrivateScope, threadStatic: true);
			AddField(FieldAttributes.Public);
			AddField(FieldAttributes.PrivateScope);

			AssertDnlibFindsWhatTheRuntimeFinds(2, LoadFieldByName());
		}

		[Fact]
		public void FirstOfTwoThreadStaticFieldsIsFound() {
			SkipOnMono();
			AddField(FieldAttributes.PrivateScope, threadStatic: true);
			AddField(FieldAttributes.Public, threadStatic: true);

			AssertDnlibFindsWhatTheRuntimeFinds(1, LoadFieldByName());
		}

		Instruction CallStaticMethodByName() => OpCodes.Call.ToInstruction(new MemberRefUser(module, "Method", MethodSig.CreateStatic(CorLib.Int32), type));

		// The runtime looks for methods from the last one to the first one
		[Fact]
		public void LastOfTwoStaticMethodsIsFound() {
			SkipOnMono();
			AddMethod(type, MethodAttributes.PrivateScope | MethodAttributes.Static);
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Static);

			AssertDnlibFindsWhatTheRuntimeFinds(2, CallStaticMethodByName());
		}

		[Fact]
		public void LastOfTwoStaticMethodsIsFoundIfItIsPrivateScope() {
			SkipOnMono();
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Static);
			AddMethod(type, MethodAttributes.PrivateScope | MethodAttributes.Static);

			AssertDnlibFindsWhatTheRuntimeFinds(2, CallStaticMethodByName());
		}

		// The instructions for 'new Holder().Method()', a call that doesn't use the virtual method table
		Instruction[] CallInstanceMethodByName() {
			var constructor = AddConstructor(type, CorLib.Object.TypeDefOrRef!);
			return new[] {
				OpCodes.Newobj.ToInstruction(constructor),
				OpCodes.Call.ToInstruction(new MemberRefUser(module, "Method", MethodSig.CreateInstance(CorLib.Int32), type)),
			};
		}

		[Fact]
		public void LastOfTwoVirtualMethodsIsFound() {
			SkipOnMono();
			AddMethod(type, MethodAttributes.PrivateScope | MethodAttributes.Virtual | MethodAttributes.NewSlot);
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);

			AssertDnlibFindsWhatTheRuntimeFinds(2, CallInstanceMethodByName());
		}

		[Fact]
		public void NonVirtualMethodIsFoundBeforeAVirtualMethod() {
			SkipOnMono();
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
			AddMethod(type, MethodAttributes.PrivateScope);

			AssertDnlibFindsWhatTheRuntimeFinds(2, CallInstanceMethodByName());
		}

		// The type's base type has the virtual method too
		TypeDefUser AddBaseTypeWithTheVirtualMethod() {
			var baseType = new TypeDefUser("", "Base", CorLib.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public };
			module.Types.Add(baseType);
			AddConstructor(baseType, CorLib.Object.TypeDefOrRef!);
			AddMethod(baseType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
			type.BaseType = baseType;
			return baseType;
		}

		Instruction[] CallInstanceMethodOfDerivedTypeByName(TypeDef baseType) {
			var constructor = AddConstructor(type, baseType);
			return new[] {
				OpCodes.Newobj.ToInstruction(constructor),
				OpCodes.Call.ToInstruction(new MemberRefUser(module, "Method", MethodSig.CreateInstance(CorLib.Int32), type)),
			};
		}

		// One method overrides the method of the base type, the other one is a new virtual method
		[Fact]
		public void NewVirtualMethodIsFoundBeforeAnOverride() {
			SkipOnMono();
			var baseType = AddBaseTypeWithTheVirtualMethod();
			AddMethod(type, MethodAttributes.PrivateScope | MethodAttributes.Virtual | MethodAttributes.NewSlot);
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Virtual);

			AssertDnlibFindsWhatTheRuntimeFinds(1, CallInstanceMethodOfDerivedTypeByName(baseType));
		}

		[Fact]
		public void NewVirtualMethodIsFoundBeforeAnOverrideThatIsFirst() {
			SkipOnMono();
			var baseType = AddBaseTypeWithTheVirtualMethod();
			AddMethod(type, MethodAttributes.Public | MethodAttributes.Virtual);
			AddMethod(type, MethodAttributes.PrivateScope | MethodAttributes.Virtual | MethodAttributes.NewSlot);

			AssertDnlibFindsWhatTheRuntimeFinds(2, CallInstanceMethodOfDerivedTypeByName(baseType));
		}
	}
}
