// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// A method is a <see cref="MethodDef"/> (its definition) or a <see cref="MemberRef"/> (a reference to
	/// it), and a field is a <see cref="FieldDef"/> or a <see cref="MemberRef"/>. <see cref="SigComparer"/> must
	/// say that a definition and a reference to it are equal, with every method that takes them, in both
	/// directions and with the same hash code.
	/// </summary>
	public sealed class MemberFormComparisonTests : IDisposable {
		const string LibrarySource = @"
using System;

public class Members {
	public int Field;
	public string Other;
	public event EventHandler Changed;
	public int Property { get; set; }
	public int Method(string text) { Changed?.Invoke(this, EventArgs.Empty); return 0; }
	public int Method(int number) { return 0; }
	public T Generic<T>(T value) { return value; }
}

public class Twin {
	public int Field;
	public int Method(string text) { return 0; }
}";

		readonly ModuleDefMD library;
		readonly ModuleDefUser user;
		readonly TypeDef members;
		readonly TypeRef membersRef;

		public MemberFormComparisonTests() {
			library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "CompareLibrary" }).Image);
			user = new ModuleDefUser("User.dll");
			new AssemblyDefUser("User").Modules.Add(user);
			members = library.Find("Members", isReflectionName: true)!;
			membersRef = CreateTypeRef("Members");
		}

		public void Dispose() => library.Dispose();

		TypeRef CreateTypeRef(string name) => new TypeRefUser(user, string.Empty, name, new AssemblyRefUser("CompareLibrary", library.Assembly!.Version));
		ICorLibTypes Types => user.CorLibTypes;

		MemberRef CreateMethodRef(string name, MethodSig signature, TypeRef? declaringType = null) =>
			new MemberRefUser(user, name, signature, declaringType ?? membersRef);

		MemberRef CreateFieldRef(string name, TypeSig type, TypeRef? declaringType = null) =>
			new MemberRefUser(user, name, new FieldSig(type), declaringType ?? membersRef);

		MethodDef MethodWithString => members.FindMethod("Method", MethodSig.CreateInstance(library.CorLibTypes.Int32, library.CorLibTypes.String))!;

		[Fact]
		public void MethodDefEqualsAReferenceToIt() {
			var definition = MethodWithString;
			var reference = CreateMethodRef("Method", MethodSig.CreateInstance(Types.Int32, Types.String));
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(definition, reference));
			Assert.True(comparer.Equals(reference, definition));
			Assert.True(comparer.Equals((IMethod)definition, (IMethod)reference));
			Assert.True(comparer.Equals((IMethodDefOrRef)reference, (IMethodDefOrRef)definition));
			Assert.True(comparer.Equals((IMemberRef)definition, (IMemberRef)reference));
			Assert.True(comparer.Equals(reference, CreateMethodRef("Method", MethodSig.CreateInstance(Types.Int32, Types.String))));
			Assert.Equal(comparer.GetHashCode(definition), comparer.GetHashCode(reference));
			Assert.Equal(comparer.GetHashCode(definition), comparer.GetHashCode((IMethod)reference));
			Assert.Equal(comparer.GetHashCode(definition), comparer.GetHashCode((IMemberRef)reference));
		}

		[Fact]
		public void MethodDefDoesNotEqualAReferenceToAnotherMethod() {
			var definition = MethodWithString;
			var comparer = new SigComparer();

			// Another name, another parameter type, another return type, a static method
			Assert.False(comparer.Equals(definition, CreateMethodRef("Other", MethodSig.CreateInstance(Types.Int32, Types.String))));
			Assert.False(comparer.Equals(definition, CreateMethodRef("Method", MethodSig.CreateInstance(Types.Int32, Types.Int32))));
			Assert.False(comparer.Equals(definition, CreateMethodRef("Method", MethodSig.CreateInstance(Types.String, Types.String))));
			Assert.False(comparer.Equals(definition, CreateMethodRef("Method", MethodSig.CreateStatic(Types.Int32, Types.String))));
			// A field with the method's name
			Assert.False(comparer.Equals((IMemberRef)definition, (IMemberRef)CreateFieldRef("Method", Types.Int32)));
		}

		// Without the option, the method of 'Twin' isn't the method of 'Members'
		[Fact]
		public void DeclaringTypeIsOnlyComparedWithTheOption() {
			var definition = MethodWithString;
			var twin = library.Find("Twin", isReflectionName: true)!.FindMethod("Method")!;
			var twinReference = CreateMethodRef("Method", MethodSig.CreateInstance(Types.Int32, Types.String), CreateTypeRef("Twin"));

			Assert.True(new SigComparer().Equals(definition, twin));
			Assert.True(new SigComparer().Equals(definition, twinReference));
			var compareDeclaringTypes = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);
			Assert.False(compareDeclaringTypes.Equals(definition, twin));
			Assert.False(compareDeclaringTypes.Equals(definition, twinReference));
			Assert.True(compareDeclaringTypes.Equals(twin, twinReference));
			Assert.True(compareDeclaringTypes.Equals(definition, CreateMethodRef("Method", MethodSig.CreateInstance(Types.Int32, Types.String))));
		}

		[Fact]
		public void FieldDefEqualsAReferenceToIt() {
			var definition = members.FindField("Field")!;
			var reference = CreateFieldRef("Field", Types.Int32);
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(definition, reference));
			Assert.True(comparer.Equals(reference, definition));
			Assert.True(comparer.Equals((IField)definition, (IField)reference));
			Assert.True(comparer.Equals((IMemberRef)reference, (IMemberRef)definition));
			Assert.Equal(comparer.GetHashCode(definition), comparer.GetHashCode(reference));
			Assert.Equal(comparer.GetHashCode(definition), comparer.GetHashCode((IField)reference));

			Assert.False(comparer.Equals(definition, CreateFieldRef("Field", Types.String)));
			Assert.False(comparer.Equals(definition, CreateFieldRef("Other", Types.Int32)));
			Assert.False(comparer.Equals(CreateFieldRef("Other", Types.String), definition));
			Assert.True(comparer.Equals(members.FindField("Other"), CreateFieldRef("Other", Types.String)));

			var twinField = library.Find("Twin", isReflectionName: true)!.FindField("Field")!;
			Assert.True(comparer.Equals(definition, twinField));
			Assert.False(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(definition, twinField));
		}

		[Fact]
		public void MethodSpecEqualsAnotherWithTheSameMethodAndTypeArguments() {
			var genericDefinition = members.FindMethod("Generic")!;
			var genericReference = CreateMethodRef("Generic", MethodSig.CreateInstanceGeneric(1, new GenericMVar(0), new GenericMVar(0)));
			var withInt = new MethodSpecUser(genericDefinition, new GenericInstMethodSig(library.CorLibTypes.Int32));
			var referenceWithInt = new MethodSpecUser(genericReference, new GenericInstMethodSig(Types.Int32));
			var referenceWithString = new MethodSpecUser(genericReference, new GenericInstMethodSig(Types.String));
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(genericDefinition, genericReference));
			Assert.True(comparer.Equals(withInt, referenceWithInt));
			Assert.True(comparer.Equals((IMethod)referenceWithInt, (IMethod)withInt));
			Assert.Equal(comparer.GetHashCode(withInt), comparer.GetHashCode(referenceWithInt));
			Assert.False(comparer.Equals(withInt, referenceWithString));
			// The instantiated method isn't the generic method
			Assert.False(comparer.Equals((IMethod)withInt, (IMethod)genericDefinition));
			Assert.False(comparer.Equals((IMethod)genericReference, (IMethod)referenceWithInt));
		}

		[Fact]
		public void PropertiesAndEventsAreComparedByNameAndType() {
			var comparer = new SigComparer();
			var property = members.FindProperty("Property")!;
			var sameProperty = new PropertyDefUser("Property", PropertySig.CreateInstance(Types.Int32));
			var changed = members.FindEvent("Changed")!;
			var sameEvent = new EventDefUser("Changed", Types.GetTypeRef("System", "EventHandler"));

			Assert.True(comparer.Equals(property, sameProperty));
			Assert.Equal(comparer.GetHashCode(property), comparer.GetHashCode(sameProperty));
			Assert.False(comparer.Equals(property, new PropertyDefUser("Property", PropertySig.CreateInstance(Types.String))));
			Assert.False(comparer.Equals(property, new PropertyDefUser("Other", PropertySig.CreateInstance(Types.Int32))));

			Assert.True(comparer.Equals(changed, sameEvent));
			Assert.Equal(comparer.GetHashCode(changed), comparer.GetHashCode(sameEvent));
			Assert.False(comparer.Equals(changed, new EventDefUser("Changed", Types.GetTypeRef("System", "Action"))));
			Assert.False(comparer.Equals(changed, new EventDefUser("Other", Types.GetTypeRef("System", "EventHandler"))));
		}

		[Fact]
		public void NullIsOnlyEqualToNull() {
			var comparer = new SigComparer();

			Assert.True(comparer.Equals((MethodDef?)null, (MemberRef?)null));
			Assert.False(comparer.Equals(MethodWithString, (MemberRef?)null));
			Assert.False(comparer.Equals((MemberRef?)null, MethodWithString));
			Assert.False(comparer.Equals((FieldDef?)null, CreateFieldRef("Field", Types.Int32)));
			Assert.False(comparer.Equals(members.FindProperty("Property"), (PropertyDef?)null));
			Assert.False(comparer.Equals((EventDef?)null, members.FindEvent("Changed")));
		}
	}
}
