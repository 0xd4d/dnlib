// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// Reads <c>DeclSecurity</c> blobs
	/// </summary>
	public struct DeclSecurityReader {
		DataReader reader;
		readonly ModuleDef module;
		readonly GenericParamContext gpContext;

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="sig"><c>#Blob</c> offset of <c>DeclSecurity</c> signature</param>
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDefMD module, uint sig) => Read(module, module.BlobStream.CreateReader(sig), new GenericParamContext());

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="sig"><c>#Blob</c> offset of <c>DeclSecurity</c> signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDefMD module, uint sig, GenericParamContext gpContext) => Read(module, module.BlobStream.CreateReader(sig), gpContext);

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="blob"><c>DeclSecurity</c> blob</param>
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDef module, byte[] blob) => Read(module, ByteArrayDataReaderFactory.CreateReader(blob), new GenericParamContext());

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="blob"><c>DeclSecurity</c> blob</param>
		/// <param name="gpContext">Generic parameter context</param>/// 
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDef module, byte[] blob, GenericParamContext gpContext) => Read(module, ByteArrayDataReaderFactory.CreateReader(blob), gpContext);

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="signature"><c>DeclSecurity</c> stream that will be owned by us</param>
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDef module, DataReader signature) => Read(module, signature, new GenericParamContext());

		/// <summary>
		/// Reads a <c>DeclSecurity</c> blob
		/// </summary>
		/// <param name="module">Module that will own the returned list</param>
		/// <param name="signature"><c>DeclSecurity</c> stream that will be owned by us</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A list of <see cref="SecurityAttribute"/>s</returns>
		public static IList<SecurityAttribute> Read(ModuleDef module, DataReader signature, GenericParamContext gpContext) {
			var reader = new DeclSecurityReader(module, signature, gpContext);
			return reader.Read();
		}

		DeclSecurityReader(ModuleDef module, DataReader reader, GenericParamContext gpContext) {
			this.reader = reader;
			this.module = module;
			this.gpContext = gpContext;
		}

		IList<SecurityAttribute> Read() {
			try {
				if (reader.Position >= reader.Length)
					return new List<SecurityAttribute>();

				if (reader.ReadByte() == '.')
					return ReadBinaryFormat();
				reader.Position--;
				return ReadXmlFormat();
			}
			catch {
				return new List<SecurityAttribute>();
			}
		}

		/// <summary>
		/// Reads the new (.NET 2.0+) DeclSecurity blob format
		/// </summary>
		/// <returns></returns>
		IList<SecurityAttribute> ReadBinaryFormat() {
			int numAttrs = (int)reader.ReadCompressedUInt32();
			var list = new List<SecurityAttribute>(numAttrs);

			for (int i = 0; i < numAttrs; i++) {
				var name = ReadUTF8String();
				// Use CA search rules. Some tools don't write the fully qualified name.
				var attrRef = TypeNameParser.ParseReflection(module, UTF8String.ToSystemStringOrEmpty(name), new CAAssemblyRefFinder(module), gpContext);
				/*int blobLength = (int)*/reader.ReadCompressedUInt32();
				int numNamedArgs = (int)reader.ReadCompressedUInt32();
				var namedArgs = CustomAttributeReader.ReadNamedArguments(module, ref reader, numNamedArgs, gpContext);
				if (namedArgs is null)
					throw new ApplicationException("Could not read named arguments");
				list.Add(new SecurityAttribute(attrRef, namedArgs));
			}

			return list;
		}

		/// <summary>
		/// Reads the old (.NET 1.x) DeclSecurity blob format
		/// </summary>
		/// <returns></returns>
		IList<SecurityAttribute> ReadXmlFormat() {
			var xml = reader.ReadUtf16String((int)reader.Length / 2);
			var sa = SecurityAttribute.CreateFromXml(module, xml);
			return new List<SecurityAttribute> { sa };
		}

		UTF8String ReadUTF8String() {
			uint len = reader.ReadCompressedUInt32();
			return len == 0 ? UTF8String.Empty : new UTF8String(reader.ReadBytes((int)len));
		}
	}
}
