// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// The four kinds of exception handlers (<c>catch</c>, <c>catch ... when</c> = filter, <c>finally</c>,
	/// and <c>fault</c> which C# only writes for iterators) and the parts that each kind has.
	/// </summary>
	public class ExceptionHandlerKindTests {
		const string Source = @"
using System;
using System.Collections.Generic;
public class Handlers {
	public static int Catch() { try { return Environment.TickCount; } catch (InvalidOperationException) { return 1; } }
	public static int Filter(int a) { try { return Environment.TickCount; } catch (Exception e) when (e.Message.Length == a) { return 1; } }
	public static int Finally() { try { return Environment.TickCount; } finally { Console.WriteLine(); } }
	public static IEnumerable<int> Fault() { try { yield return 1; } finally { Console.WriteLine(); } }
}
";
		static readonly ModuleDefMD module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image);

		static ExceptionHandler GetHandler(string methodName) =>
			Assert.Single(module.Find("Handlers", isReflectionName: true)!.FindMethod(methodName)!.Body!.ExceptionHandlers);

		static void AssertKind(ExceptionHandler handler, ExceptionHandlerType expected) {
			Assert.Equal(expected, handler.HandlerType);
			Assert.Equal(expected == ExceptionHandlerType.Catch, handler.IsCatch);
			Assert.Equal(expected == ExceptionHandlerType.Filter, handler.IsFilter);
			Assert.Equal(expected == ExceptionHandlerType.Finally, handler.IsFinally);
			Assert.Equal(expected == ExceptionHandlerType.Fault, handler.IsFault);
			// Every handler has a protected block and a handler block
			Assert.NotNull(handler.TryStart);
			Assert.NotNull(handler.TryEnd);
			Assert.NotNull(handler.HandlerStart);
			Assert.Same(handler.TryStart, handler.TryStartOrThrow);
			Assert.Same(handler.HandlerStart, handler.HandlerStartOrThrow);
			Assert.True(handler.TryStart.Offset < handler.TryEnd.Offset);
			Assert.True(handler.TryEnd.Offset <= handler.HandlerStart.Offset);
		}

		[Fact]
		public void CatchHasTheTypeOfTheException() {
			var handler = GetHandler("Catch");

			AssertKind(handler, ExceptionHandlerType.Catch);
			Assert.Equal("System.InvalidOperationException", handler.CatchType!.FullName);
			Assert.Same(handler.CatchType, handler.CatchTypeOrThrow);
			Assert.Null(handler.FilterStart);
			Assert.ThrowsAny<InvalidMetadataException>(() => handler.FilterStartOrThrow);
		}

		// The filter is code between the protected block and the handler: it ends with "endfilter"
		[Fact]
		public void FilterHasTheStartOfItsCode() {
			var handler = GetHandler("Filter");

			AssertKind(handler, ExceptionHandlerType.Filter);
			Assert.Null(handler.CatchType);
			Assert.ThrowsAny<InvalidMetadataException>(() => handler.CatchTypeOrThrow);
			Assert.NotNull(handler.FilterStart);
			Assert.Same(handler.FilterStart, handler.FilterStartOrThrow);
			Assert.True(handler.FilterStart.Offset < handler.HandlerStart!.Offset);
			var instructions = module.Find("Handlers", isReflectionName: true)!.FindMethod("Filter")!.Body!.Instructions;
			Assert.Equal(Code.Endfilter, instructions[instructions.IndexOf(handler.HandlerStart) - 1].OpCode.Code);
		}

		[Fact]
		public void FinallyHasNoTypeAndNoFilter() {
			var handler = GetHandler("Finally");

			AssertKind(handler, ExceptionHandlerType.Finally);
			Assert.Null(handler.CatchType);
			Assert.Null(handler.FilterStart);
		}

		// The MoveNext() method of the iterator's class disposes the iterator if its code throws
		[Fact]
		public void FaultOfAnIterator() {
			var iterator = module.GetTypes().Single(a => a.Name.Contains("<Fault>"));
			var handler = iterator.FindMethod("MoveNext")!.Body!.ExceptionHandlers.Single();

			AssertKind(handler, ExceptionHandlerType.Fault);
			Assert.Null(handler.CatchType);
			Assert.Null(handler.FilterStart);
		}

		[Fact]
		public void NewHandlerHasNoParts() {
			var handler = new ExceptionHandler(ExceptionHandlerType.Finally);

			Assert.True(handler.IsFinally);
			Assert.Null(handler.TryStart);
			Assert.Null(handler.HandlerEnd);
			Assert.ThrowsAny<InvalidMetadataException>(() => handler.TryStartOrThrow);
			Assert.ThrowsAny<InvalidMetadataException>(() => handler.HandlerStartOrThrow);
			Assert.Equal(ExceptionHandlerType.Catch, new ExceptionHandler().HandlerType);
			Assert.True(new ExceptionHandler().IsCatch);
		}
	}
}
