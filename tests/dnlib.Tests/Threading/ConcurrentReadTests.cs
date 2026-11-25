// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Threading {
	/// <summary>
	/// The dnlib build on NuGet is thread safe (<c>THREAD_SAFE</c>): several threads can use the same module.
	/// dnlib reads most things when they're first used, so the interesting moment is the first use: many
	/// threads then try to initialize the same lists and properties at the same time.
	///
	/// These tests let several threads read everything of a module that was just opened. No thread may throw or
	/// block forever, and every thread must see the same module as a single thread does.
	/// They don't run with the build that isn't thread safe.
	/// </summary>
	public class ConcurrentReadTests {
		const int Threads = 8;
		static readonly ModuleDumpOptions dumpOptions = new ModuleDumpOptions { MaxStack = true, Pdb = true };

		static void SkipUnlessThreadSafe() =>
			Assert.SkipUnless(Settings.IsThreadSafe, "This dnlib build isn't thread safe");

		// Dumps the module on several threads at once and returns each thread's dump
		static List<string> DumpOnManyThreads(ModuleDef module, TimeSpan timeout) {
			var dumps = new string?[Threads];
			var exceptions = new Exception?[Threads];
			var threads = new List<Thread>();
			// Makes all threads start reading at the same moment
			using (var start = new Barrier(Threads)) {
				for (int i = 0; i < Threads; i++) {
					int index = i;
					var thread = new Thread(() => {
						try {
							start.SignalAndWait();
							dumps[index] = ModuleDumper.Dump(module, dumpOptions);
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

			var result = new List<string>();
			for (int i = 0; i < Threads; i++) {
				if (exceptions[i] is Exception exception)
					throw new InvalidOperationException($"Thread {i} threw {exception.GetType().Name}: {exception.Message}", exception);
				result.Add(dumps[i]!);
			}
			return result;
		}

		[Fact]
		public void AllThreadsReadTheSameFixtureModule() {
			SkipUnlessThreadSafe();
			var features = Fixtures.Fixtures.Features;
			string expected;
			using (var module = RoundTrip.LoadWithPdb(features.Image, features.Pdb!))
				expected = ModuleDumper.Dump(module, dumpOptions);

			// A new module every time: the threads only race the first time something is read
			for (int iteration = 0; iteration < 50; iteration++) {
				using (var module = RoundTrip.LoadWithPdb(features.Image, features.Pdb!)) {
					foreach (var dump in DumpOnManyThreads(module, TimeSpan.FromMinutes(1)))
						Assert.Equal(expected, dump);
				}
			}
		}

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void AllThreadsReadTheSameCoreLibrary() {
			SkipUnlessThreadSafe();
			var image = File.ReadAllBytes(typeof(object).Assembly.Location);
			string expected;
			using (var module = ModuleDefMD.Load(image))
				expected = ModuleDumper.Dump(module, dumpOptions);

			for (int iteration = 0; iteration < 3; iteration++) {
				using (var module = ModuleDefMD.Load(image)) {
					foreach (var dump in DumpOnManyThreads(module, TimeSpan.FromMinutes(10)))
						Assert.True(expected == dump, "A thread read something else than a single thread does");
				}
			}
		}
	}
}
