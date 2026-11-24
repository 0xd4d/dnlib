// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Threading {
	/// <summary>
	/// Several threads use the same modules at the same time for more than reading: they resolve references
	/// (which fills the caches of the assembly resolver and of the modules), import and compare, and each
	/// thread changes its own type of a shared module. No thread may throw or block forever, and the result
	/// must be what one thread gets. These tests don't run with the dnlib build that isn't thread safe.
	/// </summary>
	public class ConcurrentUseTests {
		const int Threads = 8;
		static readonly TimeSpan timeout = TimeSpan.FromMinutes(2);

		static void SkipUnlessThreadSafe() =>
			Assert.SkipUnless(Settings.IsThreadSafe, "This dnlib build isn't thread safe");

		// Runs the action on several threads that start at the same moment. The action gets the thread's index.
		static void RunOnManyThreads(Action<int> action) {
			var exceptions = new Exception?[Threads];
			var threads = new List<Thread>();
			using (var start = new Barrier(Threads)) {
				for (int i = 0; i < Threads; i++) {
					int index = i;
					var thread = new Thread(() => {
						try {
							start.SignalAndWait();
							action(index);
						}
						catch (Exception ex) {
							exceptions[index] = ex;
						}
					});
					thread.IsBackground = true;
					thread.Start();
					threads.Add(thread);
				}
				foreach (var thread in threads)
					Assert.True(thread.Join(timeout), "A thread didn't finish: the threads probably wait for each other (deadlock)");
			}
			for (int i = 0; i < Threads; i++) {
				if (exceptions[i] is Exception exception)
					throw new InvalidOperationException($"Thread {i} threw {exception.GetType().Name}: {exception.Message}", exception);
			}
		}

		const string LibrarySource = @"
namespace Library {
	public class Shape {
		public int Sides;
		public virtual string Name() { return ""shape""; }
		public static T Echo<T>(T value) { return value; }
	}
	public class Square : Shape {
		public override string Name() { return ""square""; }
	}
}";

		const string UserSource = @"
using Library;

public static class User {
	public static string Use() {
		Shape shape = new Square();
		shape.Sides = Shape.Echo(4);
		return shape.Name() + shape.Sides + Shape.Echo(""text"");
	}
}";

		// The references of a module, as text: what each one resolves to, and what it's imported as
		static string ResolveImportAndCompare(ModuleDefMD user, ModuleDef importTarget) {
			var importer = new Importer(importTarget);
			var comparer = new SigComparer();
			var lines = new List<string>();
			foreach (var typeRef in user.GetTypeRefs().OrderBy(a => a.FullName, StringComparer.Ordinal)) {
				var typeDef = typeRef.Resolve();
				if (typeDef is null)
					continue;
				Assert.True(comparer.Equals(typeRef, typeDef));
				lines.Add($"{typeRef.FullName} -> {typeDef.FullName} in {typeDef.Module!.Name}, imported as {importer.Import(typeDef).FullName}");
			}
			foreach (var memberRef in user.GetMemberRefs().OrderBy(a => a.FullName, StringComparer.Ordinal)) {
				IMemberForwarded? definition = memberRef.IsMethodRef ? memberRef.ResolveMethod() : (IMemberForwarded?)memberRef.ResolveField();
				if (definition is null)
					continue;
				lines.Add($"{memberRef.FullName} -> {definition.FullName}, imported as {importer.Import(memberRef).FullName}");
			}
			return string.Join("\n", lines);
		}

		[Fact]
		public void ThreadsResolveImportAndCompareTheSameReferences() {
			SkipUnlessThreadSafe();
			var libraryImage = CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "ThreadLibrary" }).Image;
			var userOptions = new CSharpCompilerOptions { AssemblyName = "ThreadUser" };
			userOptions.References.Add(libraryImage);
			var userImage = CSharpCompiler.Compile(UserSource, userOptions).Image;

			string? expected = null;
			// New modules and a new resolver every time: the threads race when the caches are empty
			for (int iteration = 0; iteration < 30; iteration++) {
				var resolver = new AssemblyResolver { UseGAC = false };
				var context = new ModuleContext(resolver);
				resolver.DefaultModuleContext = context;
				context.Resolver = new Resolver(resolver);
				using (var library = ModuleDefMD.Load(libraryImage, context))
				using (var user = ModuleDefMD.Load(userImage, context)) {
					resolver.AddToCache(library);
					var results = new string[Threads];

					RunOnManyThreads(index => {
						var importTarget = new ModuleDefUser("Target" + index + ".dll");
						new AssemblyDefUser("Target" + index).Modules.Add(importTarget);
						results[index] = ResolveImportAndCompare(user, importTarget);
					});

					expected ??= results[0];
					Assert.Contains("Library.Shape -> Library.Shape in ThreadLibrary.dll", expected);
					Assert.Contains("Library.Shape::Name()", expected);
					Assert.All(results, a => Assert.Equal(expected, a));
				}
			}
		}

		// Every thread changes another type of the same module
		[Fact]
		public void ThreadsEditDifferentTypesOfAModule() {
			SkipUnlessThreadSafe();
			const int membersPerType = 50;
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var types = new TypeDef[Threads];
				for (int i = 0; i < Threads; i++) {
					types[i] = new TypeDefUser("Edited", "Type" + i, module.CorLibTypes.Object.TypeDefOrRef);
					module.Types.Add(types[i]);
				}

				RunOnManyThreads(index => {
					var type = types[index];
					for (int i = 0; i < membersPerType; i++) {
						type.Fields.Add(new FieldDefUser("Field" + i, new FieldSig(module.CorLibTypes.Int32), FieldAttributes.Public | FieldAttributes.Static));
						var method = new MethodDefUser("Method" + i, MethodSig.CreateStatic(module.CorLibTypes.Int32), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
						method.Body = new CilBody();
						method.Body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(type.Fields[i]));
						method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
						type.Methods.Add(method);
						var nested = new TypeDefUser("Nested" + i, module.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.NestedPublic };
						type.NestedTypes.Add(nested);
					}
					type.Name = "Renamed" + index;
				});

				var stream = new MemoryStream();
				module.Write(stream);
				using (var written = ModuleDefMD.Load(stream.ToArray())) {
					for (int i = 0; i < Threads; i++) {
						var type = written.Find("Edited.Renamed" + i, isReflectionName: true);
						Assert.NotNull(type);
						Assert.Equal(membersPerType, type.Fields.Count);
						Assert.Equal(membersPerType, type.Methods.Count);
						Assert.Equal(membersPerType, type.NestedTypes.Count);
						Assert.Same(type.Fields[membersPerType - 1], type.Methods[membersPerType - 1].Body!.Instructions[0].Operand);
					}
				}
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(stream.ToArray()));
			}
		}

		// Threads read a module for the first time while another thread writes it
		[Fact]
		public void ModuleIsWrittenWhileOtherThreadsReadIt() {
			SkipUnlessThreadSafe();
			var image = Fixtures.Fixtures.Features.Image;
			byte[] expected;
			using (var module = ModuleDefMD.Load(image)) {
				var stream = new MemoryStream();
				module.Write(stream);
				expected = stream.ToArray();
			}

			for (int iteration = 0; iteration < 20; iteration++) {
				using (var module = ModuleDefMD.Load(image)) {
					byte[]? written = null;

					RunOnManyThreads(index => {
						// One thread writes, the others read everything
						if (index == 0) {
							var stream = new MemoryStream();
							module.Write(stream);
							written = stream.ToArray();
						}
						else {
							foreach (var type in module.GetTypes()) {
								foreach (var method in type.Methods)
									GC.KeepAlive(method.Body?.Instructions.Count);
								GC.KeepAlive(type.CustomAttributes.Count + type.Fields.Count + type.Properties.Count + type.Events.Count);
							}
						}
					});

					Assert.Equal(expected, written);
				}
			}
		}
	}
}
