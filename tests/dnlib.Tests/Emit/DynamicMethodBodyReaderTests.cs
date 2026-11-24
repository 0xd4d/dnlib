// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using ReflectionOpCodes = System.Reflection.Emit.OpCodes;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// <see cref="DynamicMethodBodyReader"/> reads the code of a <see cref="DynamicMethod"/>: a method that a
	/// running program created with <see cref="ILGenerator"/>. It has no metadata, the reader gets the
	/// code and the objects its tokens stand for from private fields of the runtime's classes.
	/// </summary>
	public class DynamicMethodBodyReaderTests {
		// A module whose core library is the core library of the runtime that runs the tests
		static ModuleDefUser CreateModule() {
			var coreLibrary = new AssemblyRefUser(typeof(object).Assembly.GetName());
			var module = new ModuleDefUser("Test.dll", null, coreLibrary);
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		// The reader knows the private fields of .NET and .NET Framework, Mono's classes are different
		static void SkipOnMono() =>
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "DynamicMethodBodyReader doesn't support Mono");

		static MethodDef Read(Delegate compiledMethod) {
			SkipOnMono();
			var reader = new DynamicMethodBodyReader(CreateModule(), compiledMethod);
			Assert.True(reader.Read());
			return reader.GetMethod();
		}

		static Code[] GetCodes(MethodDef method) => method.Body!.Instructions.Select(a => a.OpCode.Code).ToArray();

		[Fact]
		public void ArgumentsAndArithmetic() {
			var dynamicMethod = new DynamicMethod("Add", typeof(int), new[] { typeof(int), typeof(int) });
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldarg_0);
			il.Emit(ReflectionOpCodes.Ldarg_1);
			il.Emit(ReflectionOpCodes.Add);
			il.Emit(ReflectionOpCodes.Ret);
			var add = (Func<int, int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int, int>));
			Assert.Equal(5, add(2, 3));

			var method = Read(add);

			Assert.Equal(new[] { Code.Ldarg_0, Code.Ldarg_1, Code.Add, Code.Ret }, GetCodes(method));
			Assert.True(method.IsStatic);
			Assert.Equal("System.Int32", method.MethodSig!.RetType!.FullName);
			Assert.Equal(new[] { "System.Int32", "System.Int32" }, method.MethodSig.Params.Select(a => a!.FullName));
			Assert.Same(method.Parameters[0], method.Body!.Instructions[0].GetParameter(method.Parameters));
		}

		public static int StaticField = 7;
		public static int Twice(int value) => value * 2;

		// Every kind of operand that's a token in a normal method: here it's an index into the dynamic
		// method's list of objects
		[Fact]
		public void MethodFieldTypeAndStringOperands() {
			var dynamicMethod = new DynamicMethod("Operands", typeof(string), Type.EmptyTypes, typeof(DynamicMethodBodyReaderTests).Module);
			var il = dynamicMethod.GetILGenerator();
			il.DeclareLocal(typeof(int));
			il.DeclareLocal(typeof(string[]));
			il.Emit(ReflectionOpCodes.Ldsfld, typeof(DynamicMethodBodyReaderTests).GetField(nameof(StaticField))!);
			il.Emit(ReflectionOpCodes.Call, typeof(DynamicMethodBodyReaderTests).GetMethod(nameof(Twice))!);
			il.Emit(ReflectionOpCodes.Stloc_0);
			il.Emit(ReflectionOpCodes.Ldc_I4_1);
			il.Emit(ReflectionOpCodes.Newarr, typeof(string));
			il.Emit(ReflectionOpCodes.Stloc_1);
			il.Emit(ReflectionOpCodes.Ldstr, "text");
			il.Emit(ReflectionOpCodes.Ldloca_S, (byte)0);
			il.Emit(ReflectionOpCodes.Call, typeof(int).GetMethod(nameof(int.ToString), Type.EmptyTypes)!);
			il.Emit(ReflectionOpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!);
			il.Emit(ReflectionOpCodes.Ret);
			var operands = (Func<string>)dynamicMethod.CreateDelegate(typeof(Func<string>));
			Assert.Equal("text14", operands());

			var method = Read(operands);
			var instructions = method.Body!.Instructions;

			Assert.Equal(new[] { "System.Int32", "System.String[]" }, method.Body.Variables.Select(a => a.Type!.FullName));
			var field = Assert.IsAssignableFrom<IField>(instructions[0].Operand);
			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::StaticField", field.FullName);
			var calledMethod = Assert.IsAssignableFrom<IMethod>(instructions[1].Operand);
			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::Twice(System.Int32)", calledMethod.FullName);
			var elementType = Assert.IsAssignableFrom<ITypeDefOrRef>(instructions.Single(a => a.OpCode == OpCodes.Newarr).Operand);
			Assert.Equal("System.String", elementType.FullName);
			Assert.Equal("text", instructions.Single(a => a.OpCode == OpCodes.Ldstr).Operand);
			Assert.Same(method.Body.Variables[0], instructions.Single(a => a.OpCode == OpCodes.Ldloca_S).Operand);
		}

		// The ILGenerator of a DynamicMethod can't create a filter on .NET Framework (NotSupportedException)
#if NET
		static readonly bool hasFilter = true;
#else
		static readonly bool hasFilter = false;
#endif

		// try { throw } filter { ... } handler { ... } / try { } catch (Exception) { } / try { } finally { }
		// Without a filter, the first handler is catch (InvalidOperationException).
		static Func<int, int> CreateMethodWithExceptionHandlers() {
			var dynamicMethod = new DynamicMethod("Handlers", typeof(int), new[] { typeof(int) });
			var il = dynamicMethod.GetILGenerator();
			var result = il.DeclareLocal(typeof(int));

			il.BeginExceptionBlock();
			il.BeginExceptionBlock();
			il.BeginExceptionBlock();
			il.Emit(ReflectionOpCodes.Ldarg_0);
			il.Emit(ReflectionOpCodes.Ldc_I4_1);
			var noThrow = il.DefineLabel();
			il.Emit(ReflectionOpCodes.Bne_Un_S, noThrow);
			il.Emit(ReflectionOpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes)!);
			il.Emit(ReflectionOpCodes.Throw);
			il.MarkLabel(noThrow);
			if (hasFilter) {
				// The filter handles the exception if the argument is 1 (it always is when there's an exception)
				il.BeginExceptFilterBlock();
				il.Emit(ReflectionOpCodes.Pop);
				il.Emit(ReflectionOpCodes.Ldarg_0);
				il.Emit(ReflectionOpCodes.Ldc_I4_1);
				il.Emit(ReflectionOpCodes.Ceq);
				il.BeginCatchBlock(null);
			}
			else
				il.BeginCatchBlock(typeof(InvalidOperationException));
			il.Emit(ReflectionOpCodes.Pop);
			il.Emit(ReflectionOpCodes.Ldc_I4, 100);
			il.Emit(ReflectionOpCodes.Stloc, result);
			il.EndExceptionBlock();
			il.BeginCatchBlock(typeof(Exception));
			il.Emit(ReflectionOpCodes.Pop);
			il.EndExceptionBlock();
			il.BeginFinallyBlock();
			il.Emit(ReflectionOpCodes.Ldloc, result);
			il.Emit(ReflectionOpCodes.Ldc_I4_1);
			il.Emit(ReflectionOpCodes.Add);
			il.Emit(ReflectionOpCodes.Stloc, result);
			il.EndExceptionBlock();
			il.Emit(ReflectionOpCodes.Ldloc, result);
			il.Emit(ReflectionOpCodes.Ret);
			return (Func<int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int>));
		}

		[Fact]
		public void ExceptionHandlers() {
			var handlers = CreateMethodWithExceptionHandlers();
			Assert.Equal(1, handlers(0));
			Assert.Equal(101, handlers(1));

			var method = Read(handlers);
			var body = method.Body!;

			var firstHandlerType = hasFilter ? ExceptionHandlerType.Filter : ExceptionHandlerType.Catch;
			Assert.Equal(new[] { firstHandlerType, ExceptionHandlerType.Catch, ExceptionHandlerType.Finally }, body.ExceptionHandlers.Select(a => a.HandlerType));
			var filter = body.ExceptionHandlers[0];
			var catchHandler = body.ExceptionHandlers[1];
			var finallyHandler = body.ExceptionHandlers[2];
			Assert.Equal("System.Exception", catchHandler.CatchType!.FullName);
			if (hasFilter) {
				// Fixed in c58fd04b: the filter's start wasn't read. The filter is between the try block and its handler.
				Assert.NotNull(filter.FilterStart);
				Assert.Same(filter.TryEnd, filter.FilterStart);
				Assert.Equal(Code.Pop, filter.FilterStart.OpCode.Code);
				Assert.Equal(Code.Endfilter, body.Instructions[body.Instructions.IndexOf(filter.HandlerStart!) - 1].OpCode.Code);
			}
			else
				Assert.Equal("System.InvalidOperationException", filter.CatchType!.FullName);
			// The 3 try blocks start at the same instruction, and each one contains the one before it
			Assert.Same(body.Instructions[0], filter.TryStart);
			Assert.Same(filter.TryStart, catchHandler.TryStart);
			Assert.Same(filter.TryStart, finallyHandler.TryStart);
			Assert.True(filter.HandlerEnd!.Offset <= catchHandler.TryEnd!.Offset);
			Assert.True(catchHandler.HandlerEnd!.Offset <= finallyHandler.TryEnd!.Offset);
		}

		// The method that was read can be added to a module, written to a file and run
		[Fact]
		public void MethodThatWasReadRunsInAnAssembly() {
			SkipOnMono();
			var module = CreateModule();
			var reader = new DynamicMethodBodyReader(module, CreateMethodWithExceptionHandlers());
			Assert.True(reader.Read());
			var method = reader.GetMethod();
			method.Name = "Handlers";
			method.Attributes = MethodAttributes.Public | MethodAttributes.Static;
			var type = new TypeDefUser("Ns", "Type", module.CorLibTypes.Object.TypeDefOrRef);
			type.Attributes = TypeAttributes.Public;
			module.Types.Add(type);
			type.Methods.Add(method);
			var stream = new System.IO.MemoryStream();
			module.Write(stream);

			using (var loaded = new dnlib.TestUtils.LoadedAssembly(stream.ToArray())) {
				var runtimeMethod = loaded.Assembly.GetType("Ns.Type")!.GetMethod("Handlers")!;
				Assert.Equal(1, runtimeMethod.Invoke(null, new object[] { 0 }));
				Assert.Equal(101, runtimeMethod.Invoke(null, new object[] { 1 }));
			}
		}
	}
}
