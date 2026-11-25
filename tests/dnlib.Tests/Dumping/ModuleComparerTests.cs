// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Dumping {
	/// <summary>
	/// Many tests rely on <see cref="ModuleComparer"/> to notice that a module changed. These tests change one
	/// thing in a module and check that the comparer reports it, so a comparer that compares nothing (or not
	/// enough) can't go unnoticed.
	/// </summary>
	public class ModuleComparerTests {
		static ModuleDefMD LoadFeatures() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		// Loads the module twice, changes one of them and returns what the comparer reports
		static string GetDifference(Action<ModuleDefMD> change, ModuleDumpOptions? options = null) {
			using (var original = LoadFeatures())
			using (var changed = LoadFeatures()) {
				change(changed);
				var exception = Assert.Throws<ModuleDifferenceException>(() => ModuleComparer.AssertEqual(original, changed, options));
				return exception.Message;
			}
		}

		static TypeDef FindType(ModuleDef module, string name) =>
			module.Find(name, isReflectionName: true) ?? throw new ArgumentException($"Type {name} doesn't exist");

		static MethodDef FindMethod(ModuleDef module, string type, string name) =>
			FindType(module, type).FindMethod(name) ?? throw new ArgumentException($"Method {type}.{name} doesn't exist");

		[Fact]
		public void SameModuleLoadedTwiceIsEqual() {
			using (var first = LoadFeatures())
			using (var second = LoadFeatures())
				ModuleComparer.AssertEqual(first, second);
		}

		[Fact]
		public void MessageShowsWhereTheDifferenceIs() {
			var message = GetDifference(module => FindMethod(module, "Features.Code", "Lambdas").Body!.Instructions[0].OpCode = OpCodes.Break);

			Assert.Contains("Features.Code", message);
			Assert.Contains("method \"Lambdas\"", message);
			Assert.Contains("actual:   break", message);
		}

		[Fact]
		public void ModulePropertyIsCompared() =>
			Assert.Contains("module header", GetDifference(module => module.Mvid = Guid.NewGuid()));

		[Fact]
		public void AssemblyPropertyIsCompared() =>
			Assert.Contains("module header", GetDifference(module => module.Assembly!.Version = new Version(9, 8, 7, 6)));

		[Fact]
		public void AssemblyCustomAttributeIsCompared() =>
			Assert.Contains("module header", GetDifference(module => module.Assembly!.CustomAttributes.RemoveAt(0)));

		[Fact]
		public void EntryPointIsCompared() =>
			Assert.Contains("entrypoint", GetDifference(module => module.EntryPoint = null));

		[Fact]
		public void RemovedTypeIsReported() =>
			Assert.Contains("Missing type", GetDifference(module => module.Types.RemoveAt(module.Types.Count - 1)));

		[Fact]
		public void AddedTypeIsReported() =>
			Assert.Contains("Extra type: NewType", GetDifference(module => module.Types.Add(new TypeDefUser("NewType"))));

		[Fact]
		public void TypeNameIsCompared() =>
			GetDifference(module => FindType(module, "Features.Square").Name = "Circle");

		[Fact]
		public void TypeFlagsAreCompared() =>
			Assert.Contains("flags=", GetDifference(module => FindType(module, "Features.Square").Attributes ^= TypeAttributes.Sealed));

		[Fact]
		public void BaseTypeIsCompared() =>
			Assert.Contains("base=", GetDifference(module => FindType(module, "Features.Square").BaseType = module.CorLibTypes.Object.TypeDefOrRef));

		[Fact]
		public void InterfaceIsCompared() =>
			Assert.Contains("interface", GetDifference(module => FindType(module, "Features.Shape").Interfaces.Clear()));

		[Fact]
		public void ClassLayoutIsCompared() =>
			Assert.Contains("layout=", GetDifference(module => FindType(module, "Features.Union").ClassSize = 32));

		[Fact]
		public void GenericParameterConstraintIsCompared() =>
			Assert.Contains("constraint", GetDifference(module => FindType(module, "Features.Outer`1").GenericParameters[0].GenericParamConstraints.Clear()));

		[Fact]
		public void FieldTypeIsCompared() =>
			Assert.Contains("field \"sides\"", GetDifference(module => FindType(module, "Features.Shape").FindField("sides")!.FieldType = module.CorLibTypes.Int64));

		[Fact]
		public void FieldOffsetIsCompared() =>
			Assert.Contains("offset=", GetDifference(module => FindType(module, "Features.Union").FindField("High")!.FieldOffset = 5));

		[Fact]
		public void FieldConstantIsCompared() =>
			Assert.Contains("constant", GetDifference(module => FindType(module, "Features.Shape").FindField("MaxSides")!.Constant!.Value = 5));

		[Fact]
		public void FieldInitialValueIsCompared() =>
			Assert.Contains("initialvalue=", GetDifference(module => {
				var field = module.GetTypes().SelectMany(type => type.Fields).First(a => a.InitialValue is not null);
				field.InitialValue![0] ^= 0xFF;
			}));

		[Fact]
		public void FieldMarshalTypeIsCompared() =>
			Assert.Contains("marshal", GetDifference(module => ((FixedSysStringMarshalType)FindType(module, "Features.NativeData").FindField("Text")!.MarshalType!).Size = 17));

		[Fact]
		public void MethodSignatureIsCompared() =>
			Assert.Contains("method \"Add\"", GetDifference(module => FindMethod(module, "Features.Native", "Add").MethodSig!.Params[0] = module.CorLibTypes.UInt32));

		[Fact]
		public void MethodFlagsAreCompared() =>
			Assert.Contains("implflags=", GetDifference(module => FindMethod(module, "Features.Native", "Add").ImplAttributes |= MethodImplAttributes.NoInlining));

		[Fact]
		public void ParameterNameIsCompared() =>
			Assert.Contains("param 1", GetDifference(module => FindMethod(module, "Features.Native", "Add").ParamDefs[0].Name = "renamed"));

		[Fact]
		public void PInvokeInfoIsCompared() =>
			Assert.Contains("implmap", GetDifference(module => FindMethod(module, "Features.Native", "GetTicks").ImplMap!.Name = "Other"));

		[Fact]
		public void MethodOverrideIsCompared() =>
			Assert.Contains("override", GetDifference(module => FindType(module, "Features.Shape").Methods.First(a => a.HasOverrides).Overrides.Clear()));

		[Fact]
		public void InstructionOperandIsCompared() =>
			Assert.Contains("ldstr", GetDifference(module => {
				var instruction = FindMethod(module, "Features.Code", "Switch").Body!.Instructions.First(a => a.OpCode.Code == Code.Ldstr);
				instruction.Operand = "changed";
			}));

		[Fact]
		public void BranchTargetIsCompared() =>
			GetDifference(module => {
				var instructions = FindMethod(module, "Features.Code", "Exceptions").Body!.Instructions;
				var branch = instructions.First(a => a.Operand is Instruction);
				branch.Operand = instructions[instructions.Count - 1] == branch.Operand ? instructions[0] : instructions[instructions.Count - 1];
			});

		[Fact]
		public void LocalTypeIsCompared() =>
			Assert.Contains("local 0", GetDifference(module => FindMethod(module, "Features.Code", "Locals").Body!.Variables[0].Type = module.CorLibTypes.Object));

		[Fact]
		public void InitLocalsIsCompared() =>
			Assert.Contains("initlocals=", GetDifference(module => {
				var body = FindMethod(module, "Features.Code", "Locals").Body!;
				body.InitLocals = !body.InitLocals;
			}));

		[Fact]
		public void ExceptionHandlerIsCompared() =>
			Assert.Contains("exceptionhandler", GetDifference(module => {
				var handler = FindMethod(module, "Features.Code", "Exceptions").Body!.ExceptionHandlers[0];
				handler.TryEnd = handler.TryStart;
			}));

		[Fact]
		public void MaxStackIsOnlyComparedIfTheOptionIsSet() {
			Action<ModuleDefMD> change = module => FindMethod(module, "Features.Code", "Locals").Body!.MaxStack = 100;

			Assert.Contains("maxstack=100", GetDifference(change, new ModuleDumpOptions { MaxStack = true }));
			using (var original = LoadFeatures())
			using (var changed = LoadFeatures()) {
				change(changed);
				ModuleComparer.AssertEqual(original, changed);
			}
		}

		[Fact]
		public void CustomAttributeArgumentIsCompared() =>
			Assert.Contains("argument", GetDifference(module => {
				var attribute = FindType(module, "Features.Shape").CustomAttributes.First(a => a.ConstructorArguments.Count == 12);
				attribute.ConstructorArguments[6] = new CAArgument(module.CorLibTypes.Int32, 12345);
			}));

		[Fact]
		public void CustomAttributeNamedArgumentIsCompared() =>
			Assert.Contains("property \"Number\"", GetDifference(module => {
				var attribute = FindType(module, "Features.Shape").CustomAttributes.First(a => a.NamedArguments.Count != 0);
				var number = attribute.NamedArguments.First(a => a.Name == "Number");
				number.Argument = new CAArgument(module.CorLibTypes.Int32, 12345);
			}));

		[Fact]
		public void PropertyAccessorIsCompared() =>
			Assert.Contains("property \"Name\"", GetDifference(module => FindType(module, "Features.Shape").FindProperty("Name")!.SetMethod = null));

		[Fact]
		public void EventAccessorIsCompared() =>
			Assert.Contains("event \"Changed\"", GetDifference(module => FindType(module, "Features.Shape").FindEvent("Changed")!.RemoveMethod = null));
	}
}
