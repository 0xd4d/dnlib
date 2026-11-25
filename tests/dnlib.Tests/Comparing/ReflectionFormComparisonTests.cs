// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// <see cref="SigComparer"/> compares dnlib's types and members with the ones of System.Reflection. These
	/// tests compare references (<see cref="TypeRef"/>, <see cref="ExportedType"/>, <see cref="MemberRef"/>):
	/// <c>Reflection/ReflectionComparisonTests</c> compares the definitions of whole assemblies.
	/// </summary>
	public class ReflectionFormComparisonTests {
		// A module whose core library is the core library of the runtime that runs the tests
		static ModuleDefUser CreateModule() {
			var coreLibrary = new AssemblyRefUser(typeof(object).Assembly.GetName());
			var module = new ModuleDefUser("Test.dll", null, coreLibrary);
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		static readonly ModuleDefUser module = CreateModule();

		static AssemblyRefUser AssemblyOf(Type type) => new AssemblyRefUser(type.Assembly.GetName());

		// References to a type that isn't nested and isn't generic
		static TypeRef CreateTypeRef(Type type) => new TypeRefUser(module, type.Namespace, type.Name, AssemblyOf(type));
		static ExportedType CreateExportedType(Type type) => new ExportedTypeUser(module, 0, type.Namespace, type.Name, TypeAttributes.Public, AssemblyOf(type));

		public static TheoryData<Type> SimpleTypes => new TheoryData<Type> { typeof(Uri), typeof(Version), typeof(List<>), typeof(IDisposable), typeof(DayOfWeek) };

		[Theory, MemberData(nameof(SimpleTypes))]
		public void ReferencesEqualTheReflectionType(Type type) {
			var comparer = new SigComparer();
			var typeRef = CreateTypeRef(type);
			var exported = CreateExportedType(type);
			TypeSig typeSig = type.IsValueType ? new ValueTypeSig(typeRef) : new ClassSig(typeRef);
			var typeSpec = new TypeSpecUser(typeSig);

			Assert.True(comparer.Equals(typeRef, type));
			Assert.True(comparer.Equals(type, typeRef));
			Assert.True(comparer.Equals(exported, type));
			Assert.True(comparer.Equals(type, exported));
			Assert.True(comparer.Equals(typeSig, type));
			Assert.True(comparer.Equals(type, typeSig));
			Assert.True(comparer.Equals(typeSpec, type));
			Assert.True(comparer.Equals(type, typeSpec));
			Assert.True(comparer.Equals((IType)typeRef, type));
			Assert.True(comparer.Equals(type, (ITypeDefOrRef)typeRef));

			int hash = comparer.GetHashCode(type);
			Assert.Equal(hash, comparer.GetHashCode(typeRef));
			Assert.Equal(hash, comparer.GetHashCode(exported));
			Assert.Equal(hash, comparer.GetHashCode(typeSig));
			Assert.Equal(hash, comparer.GetHashCode(typeSpec));
		}

		[Theory, MemberData(nameof(SimpleTypes))]
		public void ReferencesDoNotEqualAnotherReflectionType(Type type) {
			var comparer = new SigComparer();
			var other = typeof(Random);

			Assert.False(comparer.Equals(CreateTypeRef(type), other));
			Assert.False(comparer.Equals(other, CreateExportedType(type)));
			Assert.False(comparer.Equals(new TypeSpecUser(new ClassSig(CreateTypeRef(type))), other));
			// The same name in another namespace or assembly
			Assert.False(comparer.Equals(new TypeRefUser(module, "Other", type.Name, AssemblyOf(type)), type));
			Assert.False(comparer.Equals(new TypeRefUser(module, type.Namespace, type.Name, new AssemblyRefUser("AnotherAssembly")), type));
			Assert.True(new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(new TypeRefUser(module, type.Namespace, type.Name, new AssemblyRefUser("AnotherAssembly")), type));
		}

		[Fact]
		public void NestedTypeReferenceEqualsTheNestedReflectionType() {
			var comparer = new SigComparer();
			var nested = typeof(Environment.SpecialFolder);
			var outerRef = CreateTypeRef(typeof(Environment));
			var nestedRef = new TypeRefUser(module, string.Empty, "SpecialFolder", outerRef);
			var outerExported = CreateExportedType(typeof(Environment));
			var nestedExported = new ExportedTypeUser(module, 0, string.Empty, "SpecialFolder", TypeAttributes.NestedPublic, outerExported);

			Assert.True(comparer.Equals(nestedRef, nested));
			Assert.True(comparer.Equals(nested, nestedExported));
			Assert.Equal(comparer.GetHashCode(nested), comparer.GetHashCode(nestedRef));
			Assert.Equal(comparer.GetHashCode(nested), comparer.GetHashCode(nestedExported));
			Assert.False(comparer.Equals(nestedRef, typeof(Environment)));
			Assert.False(comparer.Equals(outerRef, nested));
			// Nested in another type
			Assert.False(comparer.Equals(new TypeRefUser(module, string.Empty, "SpecialFolder", CreateTypeRef(typeof(Uri))), nested));
		}

		[Fact]
		public void SignaturesEqualConstructedReflectionTypes() {
			var comparer = new SigComparer();
			var int32 = module.CorLibTypes.Int32;
			var listRef = CreateTypeRef(typeof(List<>));

			Assert.True(comparer.Equals(new SZArraySig(int32), typeof(int[])));
			Assert.True(comparer.Equals(new ArraySig(int32, 2), typeof(int[,])));
			Assert.True(comparer.Equals(new PtrSig(int32), typeof(int*)));
			Assert.True(comparer.Equals(new ByRefSig(int32), typeof(int).MakeByRefType()));
			Assert.True(comparer.Equals(new GenericInstSig(new ClassSig(listRef), module.CorLibTypes.String), typeof(List<string>)));
			Assert.True(comparer.Equals(typeof(List<int[]>), new GenericInstSig(new ClassSig(listRef), new SZArraySig(int32))));

			Assert.False(comparer.Equals(new SZArraySig(int32), typeof(long[])));
			Assert.False(comparer.Equals(new SZArraySig(int32), typeof(int[,])));
			Assert.False(comparer.Equals(new ArraySig(int32, 3), typeof(int[,])));
			Assert.False(comparer.Equals(new GenericInstSig(new ClassSig(listRef), int32), typeof(List<string>)));
			Assert.False(comparer.Equals(new GenericInstSig(new ClassSig(listRef), int32), typeof(List<>)));

			Assert.Equal(comparer.GetHashCode(typeof(int[])), comparer.GetHashCode(new SZArraySig(int32)));
			Assert.Equal(comparer.GetHashCode(typeof(List<string>)), comparer.GetHashCode(new GenericInstSig(new ClassSig(listRef), module.CorLibTypes.String)));
		}

		[Fact]
		public void MemberRefEqualsTheReflectionMember() {
			var comparer = new SigComparer();
			var uriRef = CreateTypeRef(typeof(Uri));
			var toString = new MemberRefUser(module, "ToString", MethodSig.CreateInstance(module.CorLibTypes.String), uriRef);
			var isFile = new MemberRefUser(module, "get_IsFile", MethodSig.CreateInstance(module.CorLibTypes.Boolean), uriRef);
			var schemeDelimiter = new MemberRefUser(module, "SchemeDelimiter", new FieldSig(module.CorLibTypes.String), uriRef);
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), uriRef);

			Assert.True(comparer.Equals(toString, typeof(Uri).GetMethod("ToString", Type.EmptyTypes)));
			Assert.True(comparer.Equals(typeof(Uri).GetMethod("get_IsFile"), isFile));
			Assert.True(comparer.Equals((IMethod)isFile, typeof(Uri).GetMethod("get_IsFile")));
			Assert.True(comparer.Equals(constructor, typeof(Uri).GetConstructor(new[] { typeof(string) })));
			Assert.True(comparer.Equals(schemeDelimiter, typeof(Uri).GetField("SchemeDelimiter")));
			Assert.True(comparer.Equals(typeof(Uri).GetField("SchemeDelimiter"), (IField)schemeDelimiter));
			Assert.Equal(comparer.GetHashCode(typeof(Uri).GetMethod("get_IsFile")), comparer.GetHashCode(isFile));
			Assert.Equal(comparer.GetHashCode(typeof(Uri).GetField("SchemeDelimiter")), comparer.GetHashCode(schemeDelimiter));

			// Another name, another signature
			Assert.False(comparer.Equals(isFile, typeof(Uri).GetMethod("get_IsUnc")));
			Assert.False(comparer.Equals(constructor, typeof(Uri).GetConstructor(new[] { typeof(string), typeof(UriKind) })));
			Assert.False(comparer.Equals(schemeDelimiter, typeof(Uri).GetField("UriSchemeFile")));
			// The declaring type is only compared with the option
			var inAnotherType = new MemberRefUser(module, "ToString", MethodSig.CreateInstance(module.CorLibTypes.String), CreateTypeRef(typeof(Version)));
			Assert.True(comparer.Equals(inAnotherType, typeof(Uri).GetMethod("ToString", Type.EmptyTypes)));
			Assert.False(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(inAnotherType, typeof(Uri).GetMethod("ToString", Type.EmptyTypes)));
			Assert.True(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(toString, typeof(Uri).GetMethod("ToString", Type.EmptyTypes)));
		}

		// A method can be a MethodDef, a MemberRef or a MethodSpec (a generic method with type arguments). The
		// methods that take any IMethod or IField find out what they got. The option also compares the types
		// that the members are in.
		[Fact]
		public void EveryFormOfAMemberEqualsTheReflectionMemberOfTheSameType() {
			var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);
			var isFile = typeof(Uri).GetMethod("get_IsFile")!;
			var schemeDelimiter = typeof(Uri).GetField("SchemeDelimiter")!;
			// T Interlocked.Exchange<T>(ref T, T), called with a string
			var exchange = typeof(System.Threading.Interlocked).GetMethods().Single(a => a.Name == "Exchange" && a.IsGenericMethodDefinition).MakeGenericMethod(typeof(string));

			using (var uriModule = ModuleDefMD.Load(typeof(Uri).Module)) {
				var uriDef = uriModule.Find(typeof(Uri).FullName!, isReflectionName: true)!;
				IMethod isFileDef = uriDef.FindMethod("get_IsFile")!;
				IMethod isFileRef = new MemberRefUser(module, "get_IsFile", MethodSig.CreateInstance(module.CorLibTypes.Boolean), CreateTypeRef(typeof(Uri)));
				IMethod isFileRefInAnotherType = new MemberRefUser(module, "get_IsFile", MethodSig.CreateInstance(module.CorLibTypes.Boolean), CreateTypeRef(typeof(Version)));
				IField fieldDef = uriDef.FindField("SchemeDelimiter")!;
				IField fieldRefInAnotherType = new MemberRefUser(module, "SchemeDelimiter", new FieldSig(module.CorLibTypes.String), CreateTypeRef(typeof(Version)));
				var genericParameter = new GenericMVar(0);
				var exchangeRef = new MemberRefUser(module, "Exchange", MethodSig.CreateStaticGeneric(1, genericParameter, new ByRefSig(genericParameter), genericParameter), CreateTypeRef(typeof(System.Threading.Interlocked)));
				IMethod exchangeSpec = new MethodSpecUser(exchangeRef, new GenericInstMethodSig(module.CorLibTypes.String));
				IMethod exchangeSpecWithInt = new MethodSpecUser(exchangeRef, new GenericInstMethodSig(module.CorLibTypes.Int32));

				Assert.True(comparer.Equals(isFileDef, isFile));
				Assert.True(comparer.Equals(isFile, isFileDef));
				Assert.True(comparer.Equals(isFileRef, isFile));
				Assert.True(comparer.Equals(exchangeSpec, exchange));
				Assert.True(comparer.Equals(exchange, exchangeSpec));
				Assert.True(comparer.Equals(fieldDef, schemeDelimiter));
				Assert.True(comparer.Equals(schemeDelimiter, fieldDef));

				Assert.False(comparer.Equals(isFileRefInAnotherType, isFile));
				Assert.False(comparer.Equals(isFileDef, typeof(Uri).GetMethod("get_IsUnc")));
				Assert.False(comparer.Equals(exchangeSpecWithInt, exchange));
				Assert.False(comparer.Equals(exchangeSpec, isFile));
				Assert.False(comparer.Equals(isFileDef, exchange));
				Assert.False(comparer.Equals(fieldRefInAnotherType, schemeDelimiter));
				Assert.False(comparer.Equals(fieldDef, typeof(Uri).GetField("UriSchemeFile")));
			}
		}

		// A generic class of the test assembly: its members' types use the type parameter
		public class Holder<T> {
			public T? Value { get; set; }
			public T? this[int index] => Value;
			public event Action<T>? Changed;
			public void Raise(T value) => Changed?.Invoke(value);
		}

		// Reflection returns the members of Holder<int> with int in their types (Int32 Value), the definitions
		// in the file have the type parameter (!0 Value). They're the same members.
		[Fact]
		public void PropertyAndEventOfAGenericTypeEqualTheReflectionMembersOfAnInstantiation() {
			var comparer = new SigComparer();
			using (var testModule = ModuleDefMD.Load(typeof(Holder<>).Module)) {
				var holder = testModule.Find(typeof(Holder<>).FullName!, isReflectionName: true)!;
				var value = holder.FindProperty("Value")!;
				var indexer = holder.FindProperty("Item")!;
				var changed = holder.FindEvent("Changed")!;

				foreach (var type in new[] { typeof(Holder<>), typeof(Holder<int>), typeof(Holder<string[]>) }) {
					Assert.True(comparer.Equals(value, type.GetProperty("Value")), type.ToString());
					Assert.Equal(comparer.GetHashCode(value), comparer.GetHashCode(type.GetProperty("Value")));
					Assert.True(comparer.Equals(indexer, type.GetProperty("Item")), type.ToString());
					Assert.True(comparer.Equals(changed, type.GetEvent("Changed")), type.ToString());
					Assert.Equal(comparer.GetHashCode(changed), comparer.GetHashCode(type.GetEvent("Changed")));

					// Other members
					Assert.False(comparer.Equals(value, type.GetProperty("Item")), type.ToString());
					Assert.False(comparer.Equals(indexer, type.GetProperty("Value")), type.ToString());
				}
				Assert.False(comparer.Equals(value, typeof(Uri).GetProperty("Host")));
				Assert.False(comparer.Equals(changed, typeof(AppDomain).GetEvent("ProcessExit")));
			}
		}

		[Fact]
		public void NullIsOnlyEqualToNull() {
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(CreateTypeRef(typeof(Uri)), (Type?)null));
			Assert.False(comparer.Equals((Type?)null, CreateExportedType(typeof(Uri))));
			Assert.False(comparer.Equals((TypeRef?)null, typeof(Uri)));
			Assert.False(comparer.Equals((MemberRef?)null, typeof(Uri).GetMethod("get_IsFile")));
		}
	}
}
