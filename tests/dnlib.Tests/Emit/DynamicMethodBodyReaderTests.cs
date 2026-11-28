// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using ReflectionOpCodes = System.Reflection.Emit.OpCodes;
using dnlib.TestUtils;

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

		public class GenericHolder<T> {
			public static T Value = default!;
			public static T Echo(T value) => value;
		}

		// The locals of a dynamic method aren't stored with tokens: a class is stored as the address of the
		// runtime's own data about the type, which the reader converts back to the type
		[Fact]
		public void LocalsOfClassAndStructTypes() {
			var dynamicMethod = new DynamicMethod("Locals", typeof(void), Type.EmptyTypes);
			var il = dynamicMethod.GetILGenerator();
			il.DeclareLocal(typeof(Guid));
			il.DeclareLocal(typeof(System.Text.StringBuilder));
			il.DeclareLocal(typeof(System.Collections.Generic.List<string>));
			il.DeclareLocal(typeof(DynamicMethodBodyReaderTests));
			il.DeclareLocal(typeof(GenericHolder<int>[]));
			il.Emit(ReflectionOpCodes.Ret);
			var locals = (Action)dynamicMethod.CreateDelegate(typeof(Action));
			locals();

			var method = Read(locals);

			Assert.Equal(new[] {
				"System.Guid",
				"System.Text.StringBuilder",
				"System.Collections.Generic.List`1<System.String>",
				"dnlib.Tests.Emit.DynamicMethodBodyReaderTests",
				"dnlib.Tests.Emit.DynamicMethodBodyReaderTests/GenericHolder`1<System.Int32>[]",
			}, method.Body!.Variables.Select(a => a.Type!.FullName));
			Assert.True(method.Body.Variables[0].Type!.IsValueType);
			Assert.False(method.Body.Variables[1].Type!.IsValueType);
		}

		// A member of an instantiated generic type is stored with its type
		[Fact]
		public void MembersOfAGenericType() {
			var dynamicMethod = new DynamicMethod("Generic", typeof(int), Type.EmptyTypes, typeof(DynamicMethodBodyReaderTests).Module);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldc_I4_3);
			il.Emit(ReflectionOpCodes.Stsfld, typeof(GenericHolder<int>).GetField("Value")!);
			il.Emit(ReflectionOpCodes.Ldsfld, typeof(GenericHolder<int>).GetField("Value")!);
			il.Emit(ReflectionOpCodes.Call, typeof(GenericHolder<int>).GetMethod("Echo")!);
			il.Emit(ReflectionOpCodes.Ret);
			var generic = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			Assert.Equal(3, generic());

			var instructions = Read(generic).Body!.Instructions;

			const string holder = "dnlib.Tests.Emit.DynamicMethodBodyReaderTests/GenericHolder`1<System.Int32>";
			var field = Assert.IsAssignableFrom<MemberRef>(instructions[1].Operand);
			Assert.Equal(holder, field.DeclaringType!.FullName);
			Assert.Equal("Value", field.Name);
			// The signature is the one of the definition: the field's type is the generic parameter T
			Assert.Equal(ElementType.Var, field.FieldSig!.Type!.ElementType);
			var calledMethod = Assert.IsAssignableFrom<MemberRef>(instructions[3].Operand);
			Assert.Equal(holder, calledMethod.DeclaringType!.FullName);
			Assert.Equal("Echo", calledMethod.Name);
			Assert.Equal(ElementType.Var, calledMethod.MethodSig!.RetType!.ElementType);
		}

		// ldtoken of a type, a method and a field, and a call of an instantiated generic method
		[Fact]
		public void TokenOperandsAndAGenericMethod() {
			var dynamicMethod = new DynamicMethod("Tokens", typeof(int[]), Type.EmptyTypes, typeof(DynamicMethodBodyReaderTests).Module);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldtoken, typeof(Guid));
			il.Emit(ReflectionOpCodes.Pop);
			il.Emit(ReflectionOpCodes.Ldtoken, typeof(DynamicMethodBodyReaderTests).GetMethod(nameof(Twice))!);
			il.Emit(ReflectionOpCodes.Pop);
			il.Emit(ReflectionOpCodes.Ldtoken, typeof(DynamicMethodBodyReaderTests).GetField(nameof(StaticField))!);
			il.Emit(ReflectionOpCodes.Pop);
			il.Emit(ReflectionOpCodes.Call, typeof(Array).GetMethod(nameof(Array.Empty))!.MakeGenericMethod(typeof(int)));
			il.Emit(ReflectionOpCodes.Ret);
			var tokens = (Func<int[]>)dynamicMethod.CreateDelegate(typeof(Func<int[]>));
			Assert.Empty(tokens());

			var instructions = Read(tokens).Body!.Instructions;

			Assert.Equal("System.Guid", Assert.IsAssignableFrom<ITypeDefOrRef>(instructions[0].Operand).FullName);
			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::Twice(System.Int32)", Assert.IsAssignableFrom<IMethod>(instructions[2].Operand).FullName);
			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::StaticField", Assert.IsAssignableFrom<IField>(instructions[4].Operand).FullName);
			var emptyArray = Assert.IsAssignableFrom<MethodSpec>(instructions[6].Operand);
			Assert.Equal("System.Array", emptyArray.DeclaringType!.FullName);
			Assert.Equal("System.Int32", Assert.Single(emptyArray.GenericInstMethodSig!.GenericArguments)!.FullName);
		}

		// An indirect call has the signature of the called method as its operand
		[Fact]
		public void IndirectCallHasASignature() {
			var dynamicMethod = new DynamicMethod("Indirect", typeof(int), Type.EmptyTypes, typeof(DynamicMethodBodyReaderTests).Module);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldc_I4, 21);
			il.Emit(ReflectionOpCodes.Ldftn, typeof(DynamicMethodBodyReaderTests).GetMethod(nameof(Twice))!);
			il.EmitCalli(ReflectionOpCodes.Calli, System.Reflection.CallingConventions.Standard, typeof(int), new[] { typeof(int) }, null);
			il.Emit(ReflectionOpCodes.Ret);
			var indirect = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			Assert.Equal(42, indirect());

			var instructions = Read(indirect).Body!.Instructions;

			Assert.Equal(Code.Calli, instructions[2].OpCode.Code);
			var signature = Assert.IsType<MethodSig>(instructions[2].Operand);
			Assert.Equal("System.Int32", signature.RetType!.FullName);
			Assert.Equal("System.Int32", Assert.Single(signature.Params)!.FullName);
			Assert.False(signature.HasThis);
		}

		// The code of a method that's created with DynamicILInfo is a byte array, and so are its exception
		// handlers, in the format of a method body in a file: "small" (12 bytes per handler) or "fat" (24).
		//
		//   IL_0000: nop           try {
		//   IL_0001: leave.s IL_0006
		//   IL_0003: pop           } catch (Exception) {
		//   IL_0004: leave.s IL_0006   }
		//   IL_0006: ldc.i4.5
		//   IL_0007: ret
		static Func<int> CreateMethodFromBytes(bool fatExceptionHandlers) {
			var dynamicMethod = new DynamicMethod("FromBytes", typeof(int), Type.EmptyTypes);
			var info = dynamicMethod.GetDynamicILInfo();
			info.SetCode(new byte[] { 0x00, 0xDE, 0x03, 0x26, 0xDE, 0x00, 0x1B, 0x2A }, 1);
			info.SetLocalSignature(SignatureHelper.GetLocalVarSigHelper().GetSignature());
			var exceptionToken = LittleEndian.GetBytes(info.GetTokenFor(typeof(Exception).TypeHandle));
			byte[] handlers;
			if (fatExceptionHandlers) {
				handlers = new byte[] {
					0x41, 28, 0, 0,                 // fat exception handlers, 4 + 24 bytes
					0, 0, 0, 0,                     // catch
					0, 0, 0, 0, 3, 0, 0, 0,         // try: offset 0, 3 bytes
					3, 0, 0, 0, 3, 0, 0, 0,         // handler: offset 3, 3 bytes
				}.Concat(exceptionToken).ToArray();
			}
			else {
				handlers = new byte[] {
					0x01, 16, 0, 0,                 // exception handlers, 4 + 12 bytes
					0, 0,                           // catch
					0, 0, 3,                        // try: offset 0, 3 bytes
					3, 0, 3,                        // handler: offset 3, 3 bytes
				}.Concat(exceptionToken).ToArray();
			}
			info.SetExceptions(handlers);
			return (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ExceptionHandlersOfAMethodThatWasCreatedFromBytes(bool fatExceptionHandlers) {
			// Mono can't create such a method
			SkipOnMono();
			var fromBytes = CreateMethodFromBytes(fatExceptionHandlers);
			Assert.Equal(5, fromBytes());

			var body = Read(fromBytes).Body!;

			Assert.Equal(new[] { Code.Nop, Code.Leave_S, Code.Pop, Code.Leave_S, Code.Ldc_I4_5, Code.Ret }, body.Instructions.Select(a => a.OpCode.Code));
			var handler = Assert.Single(body.ExceptionHandlers);
			Assert.Equal(ExceptionHandlerType.Catch, handler.HandlerType);
			Assert.Equal("System.Exception", handler.CatchType!.FullName);
			Assert.Same(body.Instructions[0], handler.TryStart);
			Assert.Same(body.Instructions[2], handler.TryEnd);
			Assert.Same(body.Instructions[2], handler.HandlerStart);
			Assert.Same(body.Instructions[4], handler.HandlerEnd);
		}

		// Fixed in e19fba56: the reader gets the code from private fields of two runtime classes, one for methods
		// created with ILGenerator and one for DynamicILInfo. Both have fields with the same names (eg. m_code),
		// and the reader kept the field of the class it saw first: reading a method of the other kind threw.
		[Fact]
		public void BothKindsOfDynamicMethodsAreReadInAnyOrder() {
			// Mono can't create a method with DynamicILInfo
			SkipOnMono();
			var dynamicMethod = new DynamicMethod("Seven", typeof(int), Type.EmptyTypes);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldc_I4_7);
			il.Emit(ReflectionOpCodes.Ret);
			var fromILGenerator = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			var fromBytes = CreateMethodFromBytes(fatExceptionHandlers: false);

			for (int i = 0; i < 2; i++) {
				Assert.Equal(new[] { Code.Ldc_I4_7, Code.Ret }, GetCodes(Read(fromILGenerator)));
				Assert.Equal(new[] { Code.Nop, Code.Leave_S, Code.Pop, Code.Leave_S, Code.Ldc_I4_5, Code.Ret }, GetCodes(Read(fromBytes)));
			}
		}

		// The same method read with the option that lets the runtime find the declaring type of a member handle
		[Fact]
		public void MembersAreReadWithTheOptionThatGuessesTheirDeclaringType() {
			SkipOnMono();
			var dynamicMethod = new DynamicMethod("Guess", typeof(int), Type.EmptyTypes, typeof(DynamicMethodBodyReaderTests).Module);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldsfld, typeof(DynamicMethodBodyReaderTests).GetField(nameof(StaticField))!);
			il.Emit(ReflectionOpCodes.Call, typeof(DynamicMethodBodyReaderTests).GetMethod(nameof(Twice))!);
			il.Emit(ReflectionOpCodes.Ret);
			var guess = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			var module = CreateModule();

			var reader = new DynamicMethodBodyReader(module, guess, new Importer(module, ImporterOptions.TryToUseDefs), DynamicMethodBodyReaderOptions.GuessDeclaringType);
			Assert.True(reader.Read());
			var instructions = reader.GetMethod().Body!.Instructions;

			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::StaticField", Assert.IsAssignableFrom<IField>(instructions[0].Operand).FullName);
			Assert.Equal("System.Int32 dnlib.Tests.Emit.DynamicMethodBodyReaderTests::Twice(System.Int32)", Assert.IsAssignableFrom<IMethod>(instructions[1].Operand).FullName);
		}

		// The constructor with an importer, and RestoreMethod() that puts the code into a method of a module
		[Fact]
		public void TheBodyIsRestoredIntoAMethod() {
			SkipOnMono();
			var dynamicMethod = new DynamicMethod("Seven", typeof(int), Type.EmptyTypes);
			var il = dynamicMethod.GetILGenerator();
			il.Emit(ReflectionOpCodes.Ldc_I4_7);
			il.Emit(ReflectionOpCodes.Ret);
			var seven = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			var module = CreateModule();
			// The instructions, locals and exception handlers are put into the body that the method has
			var method = new MethodDefUser("Seven", MethodSig.CreateStatic(module.CorLibTypes.Int32)) { Body = new CilBody() };

			var reader = new DynamicMethodBodyReader(module, seven, new Importer(module));
			Assert.True(reader.Read());
			reader.RestoreMethod(method);

			Assert.Equal(new[] { Code.Ldc_I4_7, Code.Ret }, GetCodes(method));
			Assert.True(method.Body!.InitLocals);
			Assert.True(method.Body.MaxStack >= 1);
		}

		// The reader only reads a dynamic method or a delegate of one
		[Fact]
		public void OnlyADynamicMethodIsRead() {
			SkipOnMono();
			var module = CreateModule();

			Assert.Throws<ArgumentNullException>(() => new DynamicMethodBodyReader(module, null!));
			Assert.ThrowsAny<Exception>(() => new DynamicMethodBodyReader(module, "not a method"));
			Assert.ThrowsAny<Exception>(() => new DynamicMethodBodyReader(module, (Func<int>)(() => 1)));
		}

		// A dynamic method that calls another dynamic method: the other method has no token, the reader can't read it
		[Fact]
		public void ACallOfAnotherDynamicMethodIsNotRead() {
			SkipOnMono();
			var callee = new DynamicMethod("Callee", typeof(int), Type.EmptyTypes);
			var calleeIL = callee.GetILGenerator();
			calleeIL.Emit(ReflectionOpCodes.Ldc_I4_1);
			calleeIL.Emit(ReflectionOpCodes.Ret);
			var caller = new DynamicMethod("Caller", typeof(int), Type.EmptyTypes);
			var il = caller.GetILGenerator();
			il.Emit(ReflectionOpCodes.Call, callee);
			il.Emit(ReflectionOpCodes.Ret);
			var callerDelegate = (Func<int>)caller.CreateDelegate(typeof(Func<int>));
			Assert.Equal(1, callerDelegate());

			var reader = new DynamicMethodBodyReader(CreateModule(), callerDelegate);

			Assert.ThrowsAny<Exception>(() => reader.Read());
		}

		// Exception handlers with a filter and a finally block, in the bytes of a method created with DynamicILInfo
		//
		//   IL_0000: nop           try {
		//   IL_0001: leave.s IL_000A
		//   IL_0003: pop           } filter {
		//   IL_0004: ldc.i4.1
		//   IL_0005: endfilter     } {
		//   IL_0007: pop
		//   IL_0008: leave.s IL_000A }
		//   IL_000A: nop           try {
		//   IL_000B: leave.s IL_000E
		//   IL_000D: endfinally    } finally { }
		//   IL_000E: ldc.i4.5
		//   IL_000F: ret
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void FilterAndFinallyHandlersOfAMethodThatWasCreatedFromBytes(bool fatExceptionHandlers) {
			SkipOnMono();
			var dynamicMethod = new DynamicMethod("FilterAndFinally", typeof(int), Type.EmptyTypes);
			var info = dynamicMethod.GetDynamicILInfo();
			info.SetCode(new byte[] { 0x00, 0xDE, 0x07, 0x26, 0x17, 0xFE, 0x11, 0x26, 0xDE, 0x00, 0x00, 0xDE, 0x01, 0xDC, 0x1B, 0x2A }, 2);
			info.SetLocalSignature(SignatureHelper.GetLocalVarSigHelper().GetSignature());
			byte[] handlers;
			if (fatExceptionHandlers) {
				handlers = new byte[] {
					0x41, 52, 0, 0,                 // fat exception handlers, 4 + 2 * 24 bytes
					1, 0, 0, 0,                     // filter
					0, 0, 0, 0, 3, 0, 0, 0,         // try: offset 0, 3 bytes
					7, 0, 0, 0, 3, 0, 0, 0,         // handler: offset 7, 3 bytes
					3, 0, 0, 0,                     // filter: offset 3
					2, 0, 0, 0,                     // finally
					10, 0, 0, 0, 3, 0, 0, 0,        // try: offset 10, 3 bytes
					13, 0, 0, 0, 1, 0, 0, 0,        // handler: offset 13, 1 byte
					0, 0, 0, 0,
				};
			}
			else {
				handlers = new byte[] {
					0x01, 28, 0, 0,                 // exception handlers, 4 + 2 * 12 bytes
					1, 0,                           // filter
					0, 0, 3,                        // try: offset 0, 3 bytes
					7, 0, 3,                        // handler: offset 7, 3 bytes
					3, 0, 0, 0,                     // filter: offset 3
					2, 0,                           // finally
					10, 0, 3,                       // try: offset 10, 3 bytes
					13, 0, 1,                       // handler: offset 13, 1 byte
					0, 0, 0, 0,
				};
			}
			info.SetExceptions(handlers);
			var fromBytes = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>));
			Assert.Equal(5, fromBytes());

			var body = Read(fromBytes).Body!;

			Assert.Equal(2, body.ExceptionHandlers.Count);
			var filter = body.ExceptionHandlers[0];
			Assert.Equal(ExceptionHandlerType.Filter, filter.HandlerType);
			Assert.Same(body.Instructions[2], filter.FilterStart);
			Assert.Same(body.Instructions[5], filter.HandlerStart);
			var finallyHandler = body.ExceptionHandlers[1];
			Assert.Equal(ExceptionHandlerType.Finally, finallyHandler.HandlerType);
			Assert.Equal(Code.Endfinally, finallyHandler.HandlerStart!.OpCode.Code);
		}
	}
}
