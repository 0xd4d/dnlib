// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Dss;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The native Windows PDB reader and writer ask for some metadata through COM interfaces (IMetaDataImport and
	/// IMetaDataEmit): the names and flags of types and methods, the declaring type of a nested type and the
	/// signature of locals. dnlib's implementations (<see cref="ReaderMetaDataImport"/> for a module that was read,
	/// <see cref="MDEmitter"/> for a module that's written) are plain .NET code, so they're tested on every OS.
	/// </summary>
	public unsafe class ComMetadataTests {
		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image));

		// A name is copied with a zero char at its end, but only as many chars as fit
		static string GetName(Action<IntPtr, uint, IntPtr> getName, uint bufferLength, out uint requiredLength) {
			var buffer = new ushort[Math.Max(bufferLength, 1)];
			uint length = 0;
			fixed (ushort* p = buffer)
				getName((IntPtr)p, bufferLength, (IntPtr)(&length));
			requiredLength = length;
			int end = Array.IndexOf(buffer, (ushort)0);
			return new string(buffer.Take(end < 0 ? buffer.Length : end).Select(a => (char)a).ToArray());
		}

		[Fact]
		public void TheReaderReturnsTheNamesOfTypeRefsAndTypeDefs() {
			var md = module.Value;
			using (var import = new ReaderMetaDataImport(md.Metadata)) {
				var typeRef = md.GetTypeRefs().First(a => a.FullName == "System.Object");
				uint resolutionScope = 0;
				import.GetTypeRefProps(typeRef.MDToken.Raw, &resolutionScope, null, 0, null);
				Assert.True(md.TablesStream.TryReadTypeRefRow(typeRef.Rid, out var row));
				Assert.Equal(row.ResolutionScope, resolutionScope);
				Assert.Equal("System.Object", GetName((p, n, l) => import.GetTypeRefProps(typeRef.MDToken.Raw, null, (ushort*)p, n, (uint*)l), 100, out uint length));
				Assert.Equal(13U, length);
				// A buffer that's too small gets the start of the name
				Assert.Equal("Syst", GetName((p, n, l) => import.GetTypeRefProps(typeRef.MDToken.Raw, null, (ushort*)p, n, (uint*)l), 5, out length));
				Assert.Equal(4U, length);
				// Without a buffer, the length of the whole name is returned
				uint totalLength = 0;
				import.GetTypeRefProps(typeRef.MDToken.Raw, null, null, 0, &totalLength);
				Assert.Equal(13U, totalLength);

				var type = md.Find("Features.Code", isReflectionName: true)!;
				uint flags = 0, extends = 0;
				import.GetTypeDefProps(type.MDToken.Raw, null, 0, null, &flags, &extends);
				Assert.Equal((uint)type.Attributes, flags);
				Assert.NotEqual(0U, extends);
				Assert.Equal("Features.Code", GetName((p, n, l) => import.GetTypeDefProps(type.MDToken.Raw, (ushort*)p, n, (uint*)l, null, null), 100, out _));

				// A token of another table or a row that doesn't exist
				Assert.Throws<ArgumentException>(() => import.GetTypeRefProps(type.MDToken.Raw, null, null, 0, null));
				Assert.Throws<ArgumentException>(() => import.GetTypeDefProps(typeRef.MDToken.Raw, null, 0, null, null, null));
				Assert.Throws<ArgumentException>(() => import.GetTypeRefProps(0x01FFFFFF, null, null, 0, null));
				Assert.Throws<ArgumentException>(() => import.GetTypeDefProps(0x02FFFFFF, null, 0, null, null, null));
			}
		}

		// The signature of the locals of a method is a pointer to the bytes in the #Blob heap
		[Fact]
		public void TheReaderReturnsTheSignatureOfAStandAloneSig() {
			var md = module.Value;
			Assert.True(md.TablesStream.TryReadStandAloneSigRow(1, out var row));
			var expected = md.BlobStream.Read(row.Signature)!;
			using (var import = new ReaderMetaDataImport(md.Metadata)) {
				byte* signature = null;
				uint size = 0;
				import.GetSigFromToken(new MDToken(Table.StandAloneSig, 1).Raw, &signature, &size);

				Assert.Equal((uint)expected.Length, size);
				var bytes = new byte[size];
				System.Runtime.InteropServices.Marshal.Copy((IntPtr)signature, bytes, 0, bytes.Length);
				Assert.Equal(expected, bytes);
				Assert.Throws<ArgumentException>(() => import.GetSigFromToken(new MDToken(Table.TypeDef, 1).Raw, null, null));
				Assert.Throws<ArgumentException>(() => import.GetSigFromToken(new MDToken(Table.StandAloneSig, 0xFFFFFF).Raw, null, null));
			}
		}

		// While a module is written, the emitter knows the new tokens of the types and methods
		[Fact]
		public void TheEmitterReturnsTheNamesAndTokensOfTheWrittenModule() {
			var md = module.Value;
			var options = new ModuleWriterOptions(md);
			bool asked = false;
			options.WriterEvent += (sender, e) => {
				if (e.Event != ModuleWriterEvent.End)
					return;
				var metadata = e.Writer.Metadata;
				var emitter = new MDEmitter(metadata);
				var nested = md.GetTypes().First(a => a.DeclaringType is not null);
				var method = nested.DeclaringType!.Methods.First();
				uint nestedToken = new MDToken(Table.TypeDef, metadata.GetRid(nested)).Raw;
				uint declaringToken = new MDToken(Table.TypeDef, metadata.GetRid(nested.DeclaringType)).Raw;
				uint methodToken = new MDToken(Table.Method, metadata.GetRid(method)).Raw;

				uint enclosing = 0;
				emitter.GetNestedClassProps(nestedToken, &enclosing);
				Assert.Equal(declaringToken, enclosing);
				emitter.GetNestedClassProps(declaringToken, &enclosing);
				Assert.Equal(nested.DeclaringType.DeclaringType is null ? 0U : 1U, enclosing == 0 ? 0U : 1U);

				uint flags = 0;
				emitter.GetTypeDefProps(declaringToken, null, 0, null, &flags, null);
				Assert.Equal((uint)nested.DeclaringType.Attributes, flags);
				Assert.Equal(nested.DeclaringType.Name.String, GetName((p, n, l) => emitter.GetTypeDefProps(declaringToken, (ushort*)p, n, (uint*)l, null, null), 100, out _).Split('.').Last());

				uint declaringType = 0, attributes = 0, rva = 0, implFlags = 0, sigSize = 1;
				IntPtr sig;
				emitter.GetMethodProps(methodToken, &declaringType, null, 0, null, &attributes, &sig, &sigSize, &rva, &implFlags);
				Assert.Equal(declaringToken, declaringType);
				Assert.Equal((uint)method.Attributes, attributes);
				Assert.Equal((uint)method.ImplAttributes, implFlags);
				Assert.Equal(0U, sigSize);
				Assert.Equal(method.Name.String, GetName((p, n, l) => emitter.GetMethodProps(methodToken, null, (ushort*)p, n, (uint*)l, null, null, null, null, null), 100, out uint nameLength));
				Assert.Equal((uint)method.Name.String.Length + 1, nameLength);

				Assert.Throws<ArgumentException>(() => emitter.GetMethodProps(declaringToken, null, null, 0, null, null, null, null, null, null));
				Assert.Throws<ArgumentException>(() => emitter.GetTypeDefProps(methodToken, null, 0, null, null, null));
				Assert.Throws<ArgumentException>(() => emitter.GetNestedClassProps(methodToken, null));
				((IMetaDataEmit)emitter).GetTokenFromSig(IntPtr.Zero, 0, out uint sigToken);
				Assert.Equal(0x11000000U, sigToken);
				asked = true;
			};

			md.Write(new MemoryStream(), options);

			Assert.True(asked);
		}
	}
}
