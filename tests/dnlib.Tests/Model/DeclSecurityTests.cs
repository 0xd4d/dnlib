// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Security attributes (<c>[SecurityPermission(SecurityAction.Demand, ...)]</c>) aren't custom attributes
	/// in the metadata: all attributes of a type, method or assembly with the same action are one row of
	/// the DeclSecurity table, with a blob that has the attributes' types and properties. .NET Framework 1.x
	/// wrote the blob as the XML text of a permission set (UTF-16); dnlib reads that as one
	/// <c>PermissionSetAttribute</c> with the property <c>XML</c> and writes it as XML again.
	/// </summary>
	public class DeclSecurityTests {
		const string Source = @"
using System.Security.Permissions;
#pragma warning disable SYSLIB0003, CS0618 // obsolete
[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
public class WithDemand {
	[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true, SkipVerification = true)]
	[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.Execution)]
	[SecurityPermission(SecurityAction.LinkDemand, Unrestricted = true)]
	public void Method() { }
}
public class WithoutSecurity {
	public void Method() { }
}
";
		static readonly byte[] image = CSharpCompiler.Compile(Source).Image;

		const string PermissionSetXml = "<PermissionSet class=\"System.Security.PermissionSet\" version=\"1\" Unrestricted=\"true\"/>";

		[Fact]
		public void AttributesWithTheSameActionAreOneDeclSecurity() {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("WithDemand", isReflectionName: true)!;
				var method = type.FindMethod("Method")!;

				var demand = Assert.Single(type.DeclSecurities);
				Assert.Equal(SecurityAction.Demand, demand.Action);
				var attribute = Assert.Single(demand.SecurityAttributes);
				Assert.Equal("System.Security.Permissions.SecurityPermissionAttribute", attribute.TypeFullName);
				var property = Assert.Single(attribute.NamedArguments);
				Assert.True(property.IsProperty);
				Assert.Equal("UnmanagedCode", property.Name);
				Assert.Equal(true, property.Value);
				Assert.True(type.HasSecurity);
				Assert.True(type.HasDeclSecurities);
				Assert.True(demand.HasSecurityAttributes);

				Assert.Equal(2, method.DeclSecurities.Count);
				var assert = method.DeclSecurities.Single(a => a.Action == SecurityAction.Assert);
				Assert.Equal(2, assert.SecurityAttributes.Count);
				Assert.Equal(new[] { "UnmanagedCode", "SkipVerification" }, assert.SecurityAttributes[0].NamedArguments.Select(a => a.Name!.String).ToArray());
				Assert.Equal("Flags", assert.SecurityAttributes[1].NamedArguments.Single().Name);
				// An enum value is its number: Execution = 8
				Assert.Equal(8, assert.SecurityAttributes[1].NamedArguments.Single().Value);
				var linkDemand = method.DeclSecurities.Single(a => a.Action == SecurityAction.LinkDemand);
				Assert.Equal("Unrestricted", linkDemand.SecurityAttributes.Single().NamedArguments.Single().Name);

				var withoutSecurity = module.Find("WithoutSecurity", isReflectionName: true)!;
				Assert.Empty(withoutSecurity.DeclSecurities);
				Assert.False(withoutSecurity.HasDeclSecurities);
				Assert.False(withoutSecurity.HasSecurity);
				Assert.False(withoutSecurity.FindMethod("Method")!.HasDeclSecurities);
			}
		}

		// The compiler adds [assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
		// if unsafe code is allowed
		[Fact]
		public void AssemblyCanHaveSecurityAttributes() {
			using (var module = ModuleDefMD.Load(image)) {
				var declSecurity = Assert.Single(module.Assembly!.DeclSecurities);

				Assert.Equal(SecurityAction.RequestMinimum, declSecurity.Action);
				var attribute = Assert.Single(declSecurity.SecurityAttributes);
				Assert.Equal("System.Security.Permissions.SecurityPermissionAttribute", attribute.TypeFullName);
				Assert.Equal("SkipVerification", Assert.Single(attribute.NamedArguments).Name);
				Assert.True(module.Assembly.HasDeclSecurities);
			}
		}

		// The rows as System.Reflection.Metadata reads them
		[Fact]
		public void RowsHaveTheActionParentAndBlobOfTheFile() {
			using (var module = ModuleDefMD.Load(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var reader = peReader.GetMetadataReader();
				// The three in the source, and the one that the compiler adds to an assembly that allows unsafe code
				Assert.Equal(4, reader.DeclarativeSecurityAttributes.Count);
				Assert.Equal(4U, module.TablesStream.DeclSecurityTable.Rows);
				uint rid = 0;
				foreach (var handle in reader.DeclarativeSecurityAttributes) {
					var expected = reader.GetDeclarativeSecurityAttribute(handle);
					var declSecurity = module.ResolveDeclSecurity(++rid)!;

					Assert.Equal((int)expected.Action, (int)declSecurity.Action);
					Assert.Equal(reader.GetBlobBytes(expected.PermissionSet), declSecurity.GetBlob());
					// The binary format starts with a '.'
					Assert.Equal((byte)'.', declSecurity.GetBlob()![0]);
					Assert.Null(declSecurity.GetNet1xXmlString());
					var parent = (IHasDeclSecurity)module.ResolveToken((uint)MetadataTokens.GetToken(expected.Parent))!;
					Assert.Contains(declSecurity, parent.DeclSecurities);
				}
			}
		}

		[Fact]
		public void ModuleWithSecurityAttributesSurvivesAWrite() {
			RoundTrip.CheckWithNewMetadata(image);
			RoundTrip.CheckWithPreservedMetadata(image);
		}

		static DeclSecurity AddXmlDeclSecurity(ModuleDef module, TypeDef type) {
			var declSecurity = new DeclSecurityUser(SecurityAction.Demand, new List<SecurityAttribute> { SecurityAttribute.CreateFromXml(module, PermissionSetXml) });
			type.DeclSecurities.Add(declSecurity);
			return declSecurity;
		}

		[Fact]
		public void PermissionSetXmlIsWrittenAsUtf16Text() {
			byte[] written;
			using (var module = ModuleDefMD.Load(image)) {
				var declSecurity = AddXmlDeclSecurity(module, module.Find("WithoutSecurity", isReflectionName: true)!);
				Assert.Equal(PermissionSetXml, declSecurity.GetNet1xXmlString());
				Assert.Null(declSecurity.GetBlob());
				var stream = new MemoryStream();
				module.Write(stream);
				written = stream.ToArray();
			}

			using (var module = ModuleDefMD.Load(written))
			using (var peReader = new PEReader(new MemoryStream(written))) {
				var declSecurity = Assert.Single(module.Find("WithoutSecurity", isReflectionName: true)!.DeclSecurities);
				Assert.Equal(SecurityAction.Demand, declSecurity.Action);
				Assert.Equal(Encoding.Unicode.GetBytes(PermissionSetXml), declSecurity.GetBlob());
				Assert.Equal(PermissionSetXml, declSecurity.GetNet1xXmlString());

				// It's read as a PermissionSetAttribute of the core library with the text as its XML property
				var attribute = Assert.Single(declSecurity.SecurityAttributes);
				Assert.Equal("System.Security.Permissions.PermissionSetAttribute", attribute.TypeFullName);
				Assert.Same(module.CorLibTypes.AssemblyRef, attribute.AttributeType!.DefinitionAssembly);
				var property = Assert.Single(attribute.NamedArguments);
				Assert.True(property.IsProperty);
				Assert.Equal("XML", property.Name);
				Assert.Equal(ElementType.String, property.Type!.ElementType);
				Assert.Equal(PermissionSetXml, (UTF8String)property.Value!);

				var reader = peReader.GetMetadataReader();
				uint parentToken = module.Find("WithoutSecurity", isReflectionName: true)!.MDToken.Raw;
				var row = reader.DeclarativeSecurityAttributes.Select(reader.GetDeclarativeSecurityAttribute).Single(a => (uint)MetadataTokens.GetToken(a.Parent) == parentToken);
				Assert.Equal(PermissionSetXml, Encoding.Unicode.GetString(reader.GetBlobBytes(row.PermissionSet)));
			}
			RoundTrip.CheckWithNewMetadata(written);
		}

		// Only one PermissionSetAttribute with only the XML property is the old format
		[Fact]
		public void OtherAttributesAreNotPermissionSetXml() {
			var module = new ModuleDefUser("Test.dll");
			var types = module.CorLibTypes;
			var xmlAttribute = SecurityAttribute.CreateFromXml(module, PermissionSetXml);
			var name = new CANamedArgument(false, types.String, "Name", new CAArgument(types.String, new UTF8String("FullTrust")));
			var xmlField = new CANamedArgument(true, types.String, "XML", new CAArgument(types.String, new UTF8String(PermissionSetXml)));
			var permissionSetAttribute = types.GetTypeRef("System.Security.Permissions", "PermissionSetAttribute");
			var otherAttribute = types.GetTypeRef("System.Security.Permissions", "SecurityPermissionAttribute");

			string? GetXml(params SecurityAttribute[] attributes) => new DeclSecurityUser(SecurityAction.Demand, attributes.ToList()).GetNet1xXmlString();

			Assert.Equal(PermissionSetXml, GetXml(xmlAttribute));
			Assert.Null(GetXml());
			Assert.Null(GetXml(xmlAttribute, xmlAttribute));
			Assert.Null(GetXml(new SecurityAttribute(permissionSetAttribute, new List<CANamedArgument> { name })));
			Assert.Null(GetXml(new SecurityAttribute(permissionSetAttribute, new List<CANamedArgument> { xmlField })));
			Assert.Null(GetXml(new SecurityAttribute(permissionSetAttribute, new List<CANamedArgument> { xmlAttribute.NamedArguments[0], name })));
			Assert.Null(GetXml(new SecurityAttribute(otherAttribute, new List<CANamedArgument> { xmlAttribute.NamedArguments[0] })));
			Assert.Null(GetXml(new SecurityAttribute(permissionSetAttribute)));
			// The value can be a string too
			var withString = new CANamedArgument(false, types.String, "XML", new CAArgument(types.String, PermissionSetXml));
			Assert.Equal(PermissionSetXml, GetXml(new SecurityAttribute(permissionSetAttribute, new List<CANamedArgument> { withString })));
			var withInt32 = new CANamedArgument(false, types.String, "XML", new CAArgument(types.String, 123));
			Assert.Null(GetXml(new SecurityAttribute(permissionSetAttribute, new List<CANamedArgument> { withInt32 })));
		}

		// A blob that doesn't start with '.' is UTF-16 text. A binary blob that can't be read has no attributes.
		[Fact]
		public void BlobsThatAreNotValid() {
			var module = new ModuleDefUser("Test.dll");

			Assert.Empty(DeclSecurityReader.Read(module, new byte[0]));
			Assert.Equal("AB", DeclSecurity.GetNet1xXmlStringInternal(DeclSecurityReader.Read(module, Encoding.Unicode.GetBytes("AB"))));
			// Half a character at the end isn't read
			Assert.Equal("A", DeclSecurity.GetNet1xXmlStringInternal(DeclSecurityReader.Read(module, new byte[] { (byte)'A', 0, (byte)'B' })));
			// '.', then the number of attributes: there are none in the blob
			Assert.Empty(DeclSecurityReader.Read(module, new byte[] { (byte)'.', 5 }));
			Assert.Empty(DeclSecurityReader.Read(module, new byte[] { (byte)'.' }));
			Assert.Empty(DeclSecurityReader.Read(module, new byte[] { (byte)'.', 0 }));
			// One attribute with a name and nothing else
			Assert.Empty(DeclSecurityReader.Read(module, new byte[] { (byte)'.', 1, 1, (byte)'A' }));
		}
	}
}
