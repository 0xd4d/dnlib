// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// The equality comparers that use <see cref="SigComparer"/>, for dictionaries and sets of types, members
	/// and signatures: a definition and a reference to it are the same key
	/// </summary>
	public sealed class EqualityComparerTests : IDisposable {
		const string LibrarySource = @"
using System;

public class Members {
	public int Field;
	public event EventHandler Changed;
	public int Property { get; set; }
	public int Method(string text) { Changed?.Invoke(this, EventArgs.Empty); return 0; }
	public T Generic<T>(T value) { return value; }
}

public class Twin {
	public int Field;
	public event EventHandler Changed;
	public int Property { get; set; }
	public int Method(string text) { Changed?.Invoke(this, EventArgs.Empty); return 0; }
}";

		readonly ModuleDefMD library;
		readonly ModuleDefUser user;
		readonly TypeDef members;
		readonly TypeDef twin;

		public EqualityComparerTests() {
			library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "CompareLibrary" }).Image);
			user = new ModuleDefUser("User.dll");
			new AssemblyDefUser("User").Modules.Add(user);
			members = library.Find("Members", isReflectionName: true)!;
			twin = library.Find("Twin", isReflectionName: true)!;
		}

		public void Dispose() => library.Dispose();

		ICorLibTypes Types => user.CorLibTypes;
		TypeRef CreateTypeRef(string name) => new TypeRefUser(user, string.Empty, name, new AssemblyRefUser("CompareLibrary", library.Assembly!.Version));

		[Fact]
		public void TypeDefAndItsReferenceAreTheSameKey() {
			var typeRef = CreateTypeRef("Members");
			var typeSig = new ClassSig(typeRef);
			var typeSpec = new TypeSpecUser(typeSig);
			var exported = new ExportedTypeUser(user, 0, string.Empty, "Members", TypeAttributes.Public, new AssemblyRefUser("CompareLibrary", library.Assembly!.Version));

			var types = new HashSet<IType>(TypeEqualityComparer.Instance) { members };
			Assert.Contains(typeRef, types);
			Assert.Contains(typeSig, types);
			Assert.Contains(typeSpec, types);
			Assert.Contains(exported, types);
			Assert.DoesNotContain(CreateTypeRef("Twin"), types);
			Assert.DoesNotContain(CreateTypeRef("MEMBERS"), types);
			Assert.Contains(CreateTypeRef("MEMBERS"), new HashSet<IType>(TypeEqualityComparer.CaseInsensitive) { members });

			// The comparer for each kind of type
			Assert.Contains(typeRef, new HashSet<ITypeDefOrRef>(TypeEqualityComparer.Instance) { members });
			Assert.Contains(CreateTypeRef("Members"), new HashSet<TypeRef>(TypeEqualityComparer.Instance) { typeRef });
			Assert.Contains(members, new HashSet<TypeDef>(TypeEqualityComparer.Instance) { members });
			Assert.Contains(new TypeSpecUser(new ClassSig(members)), new HashSet<TypeSpec>(TypeEqualityComparer.Instance) { typeSpec });
			Assert.Contains(new ClassSig(members), new HashSet<TypeSig>(TypeEqualityComparer.Instance) { typeSig });
			Assert.Contains(exported, new HashSet<ExportedType>(TypeEqualityComparer.Instance) { exported });
		}

		// Two definitions in the same module are only equal if they're the same object
		[Fact]
		public void CompareReferenceInSameModuleDoesNotCompareNamesOfDefinitions() {
			Assert.True(TypeEqualityComparer.CompareReferenceInSameModule.Equals(members, members));
			Assert.True(TypeEqualityComparer.CompareReferenceInSameModule.Equals(members, CreateTypeRef("Members")));
			Assert.False(FieldEqualityComparer.CompareReferenceInSameModule.Equals(members.FindField("Field"), twin.FindField("Field")));
			Assert.True(FieldEqualityComparer.DontCompareDeclaringTypes.Equals(members.FindField("Field"), twin.FindField("Field")));
			Assert.False(MethodEqualityComparer.CompareReferenceInSameModule.Equals(members.FindMethod("Method"), twin.FindMethod("Method")));
			Assert.True(MethodEqualityComparer.DontCompareDeclaringTypes.Equals(members.FindMethod("Method"), twin.FindMethod("Method")));
		}

		[Fact]
		public void FieldAndItsReferenceAreTheSameKey() {
			var field = members.FindField("Field")!;
			var reference = new MemberRefUser(user, "Field", new FieldSig(Types.Int32), CreateTypeRef("Members"));
			var twinReference = new MemberRefUser(user, "Field", new FieldSig(Types.Int32), CreateTypeRef("Twin"));
			var upperCase = new MemberRefUser(user, "FIELD", new FieldSig(Types.Int32), CreateTypeRef("Members"));

			var withDeclaringTypes = new HashSet<IField>(FieldEqualityComparer.CompareDeclaringTypes) { field };
			Assert.Contains(reference, withDeclaringTypes);
			Assert.DoesNotContain(twinReference, withDeclaringTypes);
			Assert.DoesNotContain(upperCase, withDeclaringTypes);
			Assert.Contains(twinReference, new HashSet<IField>(FieldEqualityComparer.DontCompareDeclaringTypes) { field });
			Assert.Contains(upperCase, new HashSet<IField>(FieldEqualityComparer.CaseInsensitiveCompareDeclaringTypes) { field });
			Assert.Contains(upperCase, new HashSet<IField>(FieldEqualityComparer.CaseInsensitiveDontCompareDeclaringTypes) { twin.FindField("Field")! });

			Assert.Contains(field, new HashSet<FieldDef>(FieldEqualityComparer.CompareDeclaringTypes) { field });
			Assert.DoesNotContain(twin.FindField("Field")!, new HashSet<FieldDef>(FieldEqualityComparer.CompareDeclaringTypes) { field });
			Assert.Contains(reference, new HashSet<MemberRef>(FieldEqualityComparer.CompareDeclaringTypes) { new MemberRefUser(user, "Field", new FieldSig(Types.Int32), CreateTypeRef("Members")) });
		}

		[Fact]
		public void MethodAndItsReferenceAreTheSameKey() {
			var method = members.FindMethod("Method")!;
			var signature = MethodSig.CreateInstance(Types.Int32, Types.String);
			var reference = new MemberRefUser(user, "Method", signature, CreateTypeRef("Members"));
			var twinReference = new MemberRefUser(user, "Method", signature, CreateTypeRef("Twin"));
			var upperCase = new MemberRefUser(user, "METHOD", signature, CreateTypeRef("Members"));

			var withDeclaringTypes = new HashSet<IMethod>(MethodEqualityComparer.CompareDeclaringTypes) { method };
			Assert.Contains(reference, withDeclaringTypes);
			Assert.DoesNotContain(twinReference, withDeclaringTypes);
			Assert.Contains(twinReference, new HashSet<IMethod>(MethodEqualityComparer.DontCompareDeclaringTypes) { method });
			Assert.Contains(upperCase, new HashSet<IMethod>(MethodEqualityComparer.CaseInsensitiveCompareDeclaringTypes) { method });
			Assert.Contains(upperCase, new HashSet<IMethod>(MethodEqualityComparer.CaseInsensitiveDontCompareDeclaringTypes) { twin.FindMethod("Method")! });

			Assert.Contains(reference, new HashSet<IMethodDefOrRef>(MethodEqualityComparer.CompareDeclaringTypes) { method });
			Assert.Contains(method, new HashSet<MethodDef>(MethodEqualityComparer.CompareDeclaringTypes) { method });
			Assert.Contains(reference, new HashSet<MemberRef>(MethodEqualityComparer.CompareDeclaringTypes) { new MemberRefUser(user, "Method", signature, CreateTypeRef("Members")) });

			var generic = members.FindMethod("Generic")!;
			var withInt = new MethodSpecUser(generic, new GenericInstMethodSig(Types.Int32));
			var specs = new HashSet<MethodSpec>(MethodEqualityComparer.CompareDeclaringTypes) { withInt };
			Assert.Contains(new MethodSpecUser(generic, new GenericInstMethodSig(Types.Int32)), specs);
			Assert.DoesNotContain(new MethodSpecUser(generic, new GenericInstMethodSig(Types.String)), specs);
		}

		[Fact]
		public void PropertiesAndEventsAreComparedWithOrWithoutTheirTypes() {
			var property = members.FindProperty("Property")!;
			var twinProperty = twin.FindProperty("Property")!;
			var changed = members.FindEvent("Changed")!;
			var twinEvent = twin.FindEvent("Changed")!;

			Assert.Contains(twinProperty, new HashSet<PropertyDef>(PropertyEqualityComparer.DontCompareDeclaringTypes) { property });
			Assert.DoesNotContain(twinProperty, new HashSet<PropertyDef>(PropertyEqualityComparer.CompareDeclaringTypes) { property });
			Assert.Contains(property, new HashSet<PropertyDef>(PropertyEqualityComparer.CaseInsensitiveCompareDeclaringTypes) { property });
			Assert.Contains(twinProperty, new HashSet<PropertyDef>(PropertyEqualityComparer.CaseInsensitiveDontCompareDeclaringTypes) { property });
			Assert.DoesNotContain(twinProperty, new HashSet<PropertyDef>(PropertyEqualityComparer.CompareReferenceInSameModule) { property });

			Assert.Contains(twinEvent, new HashSet<EventDef>(EventEqualityComparer.DontCompareDeclaringTypes) { changed });
			Assert.DoesNotContain(twinEvent, new HashSet<EventDef>(EventEqualityComparer.CompareDeclaringTypes) { changed });
			Assert.Contains(changed, new HashSet<EventDef>(EventEqualityComparer.CaseInsensitiveCompareDeclaringTypes) { changed });
			Assert.Contains(twinEvent, new HashSet<EventDef>(EventEqualityComparer.CaseInsensitiveDontCompareDeclaringTypes) { changed });
			Assert.DoesNotContain(twinEvent, new HashSet<EventDef>(EventEqualityComparer.CompareReferenceInSameModule) { changed });
		}

		[Fact]
		public void EqualSignaturesAreTheSameKey() {
			var comparer = SignatureEqualityComparer.Instance;
			var membersSig = new ClassSig(members);
			var membersRefSig = new ClassSig(CreateTypeRef("Members"));

			Assert.Contains(MethodSig.CreateStatic(Types.Void, membersRefSig), new HashSet<MethodSig>(comparer) { MethodSig.CreateStatic(Types.Void, membersSig) });
			Assert.DoesNotContain(MethodSig.CreateInstance(Types.Void, membersRefSig), new HashSet<MethodSig>(comparer) { MethodSig.CreateStatic(Types.Void, membersSig) });
			Assert.Contains(MethodSig.CreateStatic(Types.Void, membersRefSig), new HashSet<MethodBaseSig>(comparer) { MethodSig.CreateStatic(Types.Void, membersSig) });
			Assert.Contains(new FieldSig(membersRefSig), new HashSet<FieldSig>(comparer) { new FieldSig(membersSig) });
			Assert.Contains(PropertySig.CreateInstance(membersRefSig), new HashSet<PropertySig>(comparer) { PropertySig.CreateInstance(membersSig) });
			Assert.Contains(new LocalSig(membersRefSig, Types.Int32), new HashSet<LocalSig>(comparer) { new LocalSig(membersSig, Types.Int32) });
			Assert.DoesNotContain(new LocalSig(membersRefSig), new HashSet<LocalSig>(comparer) { new LocalSig(membersSig, Types.Int32) });
			Assert.Contains(new GenericInstMethodSig(membersRefSig), new HashSet<GenericInstMethodSig>(comparer) { new GenericInstMethodSig(membersSig) });

			// Any signature: its kind is compared too
			var signatures = new HashSet<CallingConventionSig>(comparer) { new FieldSig(membersSig), MethodSig.CreateStatic(membersSig) };
			Assert.Contains(new FieldSig(membersRefSig), signatures);
			Assert.Contains(MethodSig.CreateStatic(membersRefSig), signatures);
			Assert.DoesNotContain(PropertySig.CreateStatic(membersRefSig), signatures);

			var upperCase = new ClassSig(CreateTypeRef("MEMBERS"));
			Assert.DoesNotContain(new FieldSig(upperCase), new HashSet<FieldSig>(comparer) { new FieldSig(membersSig) });
			Assert.Contains(new FieldSig(upperCase), new HashSet<FieldSig>(SignatureEqualityComparer.CaseInsensitive) { new FieldSig(membersSig) });
		}

		// With RawSignatureCompare, two signatures are equal if their bytes in the file would be: a type is its
		// token, not its name. Fixed in 9f1e75ee: tokens of different modules were equal, and so were all types
		// that aren't in a file yet (their row number is 0).
		[Fact]
		public void RawSignatureCompareComparesTokens() {
			var raw = new SigComparer(SigComparerOptions.RawSignatureCompare);
			var normal = new SigComparer();
			var twinWithTheSameName = new TypeDefUser("Members");
			var notInAFile = new TypeDefUser("Members");

			// The same row
			Assert.True(raw.Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(members))));
			// Other rows of the same module
			Assert.False(raw.Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(twin))));
			// A reference and a definition of the same type: equal by name, but not the same token
			Assert.True(normal.Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(CreateTypeRef("Members")))));
			Assert.False(raw.Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(CreateTypeRef("Members")))));
			// Two types without a row number
			Assert.True(normal.Equals(new FieldSig(new ClassSig(twinWithTheSameName)), new FieldSig(new ClassSig(notInAFile))));
			Assert.False(raw.Equals(new FieldSig(new ClassSig(twinWithTheSameName)), new FieldSig(new ClassSig(notInAFile))));
			// The same object is always equal
			Assert.True(raw.Equals(new FieldSig(new ClassSig(notInAFile)), new FieldSig(new ClassSig(notInAFile))));
		}

		[Fact]
		public void RawSignatureCompareDoesNotMixModules() {
			using (var otherLibrary = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "CompareLibrary" }).Image)) {
				var otherMembers = otherLibrary.Find("Members", isReflectionName: true)!;
				Assert.Equal(members.MDToken, otherMembers.MDToken);

				Assert.True(new SigComparer().Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(otherMembers))));
				Assert.False(new SigComparer(SigComparerOptions.RawSignatureCompare).Equals(new FieldSig(new ClassSig(members)), new FieldSig(new ClassSig(otherMembers))));
			}
		}
	}
}
