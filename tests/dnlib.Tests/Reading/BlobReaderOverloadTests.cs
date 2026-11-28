// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The readers of blobs (<see cref="MarshalBlobReader"/>, <see cref="DeclSecurityReader"/>,
	/// <see cref="CustomAttributeReader"/>) have overloads that read the blob at an offset of the module's
	/// #Blob heap, from a byte array or from a <see cref="DataReader"/>, with or without a generic parameter
	/// context. They all read the same.
	/// </summary>
	public class BlobReaderOverloadTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		[Fact]
		public void EveryMarshalBlobOverloadReadsTheSameType() {
			Assert.True(module.TablesStream.TryReadFieldMarshalRow(1, out var row));
			uint offset = row.NativeType;
			var bytes = module.BlobStream.Read(offset)!;
			var gpContext = new GenericParamContext();

			var types = new[] {
				MarshalBlobReader.Read(module, offset),
				MarshalBlobReader.Read(module, offset, gpContext),
				MarshalBlobReader.Read(module, bytes),
				MarshalBlobReader.Read(module, bytes, gpContext),
				MarshalBlobReader.Read(module, ByteArrayDataReaderFactory.CreateReader(bytes)),
			};

			Assert.All(types, a => Assert.Equal(types[0].ToString(), a.ToString()));
			Assert.All(types, a => Assert.Equal(types[0].NativeType, a.NativeType));
		}

		[Fact]
		public void EveryDeclSecurityOverloadReadsTheSameAttributes() {
			Assert.True(module.TablesStream.DeclSecurityTable.Rows > 0, "The fixture has no security declaration");
			Assert.True(module.TablesStream.TryReadDeclSecurityRow(1, out var row));
			uint offset = row.PermissionSet;
			var bytes = module.BlobStream.Read(offset)!;
			var gpContext = new GenericParamContext();

			var lists = new[] {
				DeclSecurityReader.Read(module, offset),
				DeclSecurityReader.Read(module, offset, gpContext),
				DeclSecurityReader.Read(module, bytes),
				DeclSecurityReader.Read(module, bytes, gpContext),
				DeclSecurityReader.Read(module, ByteArrayDataReaderFactory.CreateReader(bytes)),
			};

			Assert.All(lists, a => Assert.Equal(lists[0].Select(b => b.TypeFullName), a.Select(b => b.TypeFullName)));
			Assert.NotEmpty(lists[0]);
		}

		[Fact]
		public void TheCustomAttributeAtAnOffsetIsRead() {
			Assert.True(module.TablesStream.TryReadCustomAttributeRow(1, out var row));
			var ctor = module.ResolveCustomAttributeType(row.Type);

			var attribute = CustomAttributeReader.Read(module, ctor, row.Value);

			Assert.Same(ctor, attribute.Constructor);
			Assert.Equal(module.ReadCustomAttribute(1)!.TypeFullName, attribute.TypeFullName);
		}

		// A context with a type or a method
		[Fact]
		public void AGenericParamContextHasATypeOrAMethod() {
			var type = module.GetTypes().First(a => a.HasGenericParameters);

			Assert.True(new GenericParamContext().IsEmpty);
			var context = GenericParamContext.Create(type);
			Assert.False(context.IsEmpty);
			Assert.Same(type, context.Type);
			Assert.Null(context.Method);
		}
	}
}
