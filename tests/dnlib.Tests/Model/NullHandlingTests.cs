// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// What dnlib does with <c>null</c> where its API says what happens: a name is never null (null becomes an
	/// empty name), null can't be added to the lists of a type or module, and lookups with null find nothing.
	/// </summary>
	public class NullHandlingTests {
		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		// Fixed in 1cb6aaed and d2d3399e: the names that are stored in the #Strings heap. The file format has
		// no null name: null and an empty name are the same offset.
		[Fact]
		public void NameSetToNullIsAnEmptyName() {
			var module = CreateModule();

			var type = new TypeDefUser("Ns", "Name");
			type.Name = null;
			type.Namespace = null;
			Assert.Same(UTF8String.Empty, type.Name);
			Assert.Same(UTF8String.Empty, type.Namespace);

			var method = new MethodDefUser("Name");
			method.Name = null;
			Assert.Same(UTF8String.Empty, method.Name);

			var field = new FieldDefUser("Name");
			field.Name = null;
			Assert.Same(UTF8String.Empty, field.Name);

			var property = new PropertyDefUser("Name");
			property.Name = null;
			Assert.Same(UTF8String.Empty, property.Name);

			var eventDef = new EventDefUser("Name");
			eventDef.Name = null;
			Assert.Same(UTF8String.Empty, eventDef.Name);

			var typeRef = new TypeRefUser(module, "Ns", "Name");
			typeRef.Name = null;
			typeRef.Namespace = null;
			Assert.Same(UTF8String.Empty, typeRef.Name);
			Assert.Same(UTF8String.Empty, typeRef.Namespace);

			var assemblyRef = new AssemblyRefUser("Name");
			assemblyRef.Name = null;
			assemblyRef.Culture = null;
			Assert.Same(UTF8String.Empty, assemblyRef.Name);
			Assert.Same(UTF8String.Empty, assemblyRef.Culture);

			module.Name = null;
			module.Assembly!.Name = null;
			module.Assembly.Culture = null;
			Assert.Same(UTF8String.Empty, module.Name);
			Assert.Same(UTF8String.Empty, module.Assembly.Name);
			Assert.Same(UTF8String.Empty, module.Assembly.Culture);
		}

		[Fact]
		public void ObjectCreatedWithANullNameHasAnEmptyName() {
			var module = CreateModule();

			Assert.Same(UTF8String.Empty, new TypeDefUser((UTF8String?)null, (UTF8String?)null).Name);
			Assert.Same(UTF8String.Empty, new TypeDefUser((UTF8String?)null, (UTF8String?)null).Namespace);
			Assert.Same(UTF8String.Empty, new MethodDefUser(null).Name);
			Assert.Same(UTF8String.Empty, new FieldDefUser(null).Name);
			Assert.Same(UTF8String.Empty, new PropertyDefUser(null).Name);
			Assert.Same(UTF8String.Empty, new EventDefUser(null).Name);
			Assert.Same(UTF8String.Empty, new TypeRefUser(module, null, null).Name);
			Assert.Same(UTF8String.Empty, new ModuleRefUser(module, null).Name);
			Assert.Same(UTF8String.Empty, new MemberRefUser(module, null).Name);
			Assert.Same(UTF8String.Empty, new ParamDefUser(null).Name);
			Assert.Same(UTF8String.Empty, new GenericParamUser(0, GenericParamAttributes.NonVariant, null).Name);
			Assert.Same(UTF8String.Empty, new ModuleDefUser(null).Name);
		}

		[Fact]
		public void MembersWithEmptyNamesAreWrittenAndRead() {
			var created = new CreatedModule();
			created.AddField(null!, created.Module.CorLibTypes.Int32);
			created.AddMethod(null!, OpCodes.Ret.ToInstruction());

			using (var module = created.WriteAndRead()) {
				var type = CreatedModule.GetType(module);
				Assert.Equal(UTF8String.Empty, Assert.Single(type.Fields).Name);
				Assert.Equal(UTF8String.Empty, Assert.Single(type.Methods).Name);
			}
		}

		// Fixed in 0a1c72ee: some lists accepted null and failed later, most threw NullReferenceException
		[Fact]
		public void NullCanNotBeAddedToTheListsOfAModuleOrType() {
			var module = CreateModule();
			var type = new TypeDefUser("Ns", "Name");
			module.Types.Add(type);

			Assert.Throws<ArgumentNullException>(() => module.Types.Add(null!));
			Assert.Throws<ArgumentNullException>(() => module.Types.Insert(0, null!));
			Assert.Throws<ArgumentNullException>(() => module.Types[0] = null!);
			Assert.Throws<ArgumentNullException>(() => type.Methods.Add(null!));
			Assert.Throws<ArgumentNullException>(() => type.Fields.Add(null!));
			Assert.Throws<ArgumentNullException>(() => type.Properties.Add(null!));
			Assert.Throws<ArgumentNullException>(() => type.Events.Add(null!));
			Assert.Throws<ArgumentNullException>(() => type.NestedTypes.Add(null!));
			Assert.Throws<ArgumentNullException>(() => type.GenericParameters.Add(null!));
			Assert.Throws<ArgumentNullException>(() => module.Assembly!.Modules.Add(null!));
			Assert.Same(type, Assert.Single(module.Types, a => !a.IsGlobalModuleType));
		}

		// Fixed in 041fcb9f: the opcode was null
		[Fact]
		public void NewInstructionIsANop() {
			var instruction = new Instruction();

			Assert.Same(OpCodes.Nop, instruction.OpCode);
			Assert.Null(instruction.Operand);
			Assert.Equal(1, instruction.GetSize());
		}

		// Fixed in 3b678fdf
		[Fact]
		public void LocationAndRuntimeVersionAreNeverNull() {
			var module = CreateModule();
			Assert.Equal(string.Empty, module.Location);
			Assert.Equal(MDHeaderRuntimeVersion.MS_CLR_20, module.RuntimeVersion);

			module.Location = null;
			module.RuntimeVersion = null;

			Assert.Equal(string.Empty, module.Location);
			Assert.Equal(MDHeaderRuntimeVersion.MS_CLR_20, module.RuntimeVersion);
		}

		// Fixed in e8f2ffda: these threw NullReferenceException
		[Fact]
		public void LookupsWithNullFindNothing() {
			var module = CreateModule();
			module.Resources.Add(new EmbeddedResource("Resource", new byte[] { 1 }));

			Assert.Equal(-1, module.Resources.IndexOf((UTF8String?)null));
			Assert.Equal(-1, module.Resources.IndexOfEmbeddedResource(null));
			Assert.Equal(-1, module.Resources.IndexOfAssemblyLinkedResource(null));
			Assert.Equal(-1, module.Resources.IndexOfLinkedResource(null));
			Assert.Equal(0, module.Resources.IndexOfEmbeddedResource("Resource"));
			Assert.Equal(0, AssemblyNameComparer.CompareAll.GetHashCode(null));
			Assert.Equal(0, UTF8StringEqualityComparer.Instance.GetHashCode(null));
			Assert.Null(module.Find((string)null!, isReflectionName: true));
			Assert.Null(module.Find((ITypeDefOrRef?)null));
		}

		// Fixed in d2a4efaa: Create() always adds a new string to the heap (Add() reuses an existing one), and
		// threw NullReferenceException for null. Null is an empty string.
		[Fact]
		public void HeapsStoreNullLikeAnEmptyString() {
			var stringsHeap = new StringsHeap();
			Assert.Equal(0U, stringsHeap.Add(null));
			Assert.Equal(0U, stringsHeap.Add(UTF8String.Empty));
			uint offsetOfNull = stringsHeap.Create(null);
			uint offsetOfEmpty = stringsHeap.Create(UTF8String.Empty);
			// A new string each time, and each one is 1 byte: the zero at its end
			Assert.Equal(1U, offsetOfNull);
			Assert.Equal(2U, offsetOfEmpty);

			var userStringsHeap = new USHeap();
			// An empty user string is 2 bytes: its length (1) and the byte that every user string ends with
			uint firstOffset = userStringsHeap.Create(null);
			Assert.Equal(firstOffset + 2, userStringsHeap.Create(string.Empty));
		}
	}
}
