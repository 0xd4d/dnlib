// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="ModuleDef.Find(string, bool)"/> looks at every type until it finds the name. With
	/// <see cref="ModuleDef.EnableTypeDefFindCache"/>, the module remembers the names of the types it has seen
	/// so the next search is fast. The cache doesn't notice new, removed or renamed types: the caller must call
	/// <see cref="ModuleDef.ResetTypeDefFindCache"/>.
	/// </summary>
	public sealed class TypeFindCacheTests : IDisposable {
		readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		public void Dispose() => module.Dispose();

		[Theory, InlineData(false), InlineData(true)]
		public void EveryTypeIsFoundByBothKindsOfNames(bool useCache) {
			module.EnableTypeDefFindCache = useCache;
			Assert.Equal(useCache, module.EnableTypeDefFindCache);
			var types = module.GetTypes().ToList();
			Assert.Contains(types, a => a.IsNested);

			// Twice: the second time, a cache has all the names
			for (int i = 0; i < 2; i++) {
				foreach (var type in types) {
					// A nested type is "Outer/Inner", and "Outer+Inner" as a reflection name
					Assert.Same(type, module.Find(type.FullName, isReflectionName: false));
					Assert.Same(type, module.Find(type.ReflectionFullName, isReflectionName: true));
				}
				Assert.Null(module.Find("No.Such.Type", isReflectionName: false));
				Assert.Null(module.Find("No.Such.Type", isReflectionName: true));
			}
		}

		[Fact]
		public void CacheMustBeResetAfterATypeIsRenamedOrAdded() {
			module.EnableTypeDefFindCache = true;
			var type = module.Find("Features.Program", isReflectionName: true)!;
			Assert.Null(module.Find("Features.Renamed", isReflectionName: true));

			type.Name = "Renamed";
			var added = new TypeDefUser("Features", "Added", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(added);
			module.ResetTypeDefFindCache();

			Assert.True(module.EnableTypeDefFindCache);
			Assert.Same(type, module.Find("Features.Renamed", isReflectionName: true));
			Assert.Same(type, module.Find("Features.Renamed", isReflectionName: false));
			Assert.Null(module.Find("Features.Program", isReflectionName: true));
			Assert.Same(added, module.Find("Features.Added", isReflectionName: true));
		}

		[Fact]
		public void WithoutTheCacheChangesAreSeenAtOnce() {
			var type = module.Find("Features.Program", isReflectionName: true)!;

			type.Name = "Renamed";

			Assert.Same(type, module.Find("Features.Renamed", isReflectionName: true));
			Assert.Null(module.Find("Features.Program", isReflectionName: true));
		}

		// Turning the cache off and on forgets the names
		[Fact]
		public void DisablingTheCacheClearsIt() {
			module.EnableTypeDefFindCache = true;
			var type = module.Find("Features.Program", isReflectionName: true)!;
			type.Name = "Renamed";

			module.EnableTypeDefFindCache = false;
			module.EnableTypeDefFindCache = true;

			Assert.Null(module.Find("Features.Program", isReflectionName: true));
			Assert.Same(type, module.Find("Features.Renamed", isReflectionName: true));
		}
	}
}
