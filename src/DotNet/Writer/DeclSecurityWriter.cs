// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes <c>DeclSecurity</c> blobs
	/// </summary>
	public readonly struct DeclSecurityWriter : ICustomAttributeWriterHelper {
		readonly ModuleDef module;
		readonly IWriterError helper;
		readonly DataWriterContext context;
		readonly bool optimizeCustomAttributeSerializedTypeNames;

		/// <summary>
		/// Creates a <c>DeclSecurity</c> blob from <paramref name="secAttrs"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="secAttrs">List of <see cref="SecurityAttribute"/>s</param>
		/// <param name="helper">Helps this class</param>
		/// <returns>A <c>DeclSecurity</c> blob</returns>
		public static byte[] Write(ModuleDef module, IList<SecurityAttribute> secAttrs, IWriterError helper) =>
			Write(module, secAttrs, helper, false);

		/// <summary>
		/// Creates a <c>DeclSecurity</c> blob from <paramref name="secAttrs"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="secAttrs">List of <see cref="SecurityAttribute"/>s</param>
		/// <param name="helper">Helps this class</param>
		/// <param name="optimizeCustomAttributeSerializedTypeNames">Optimize serialized type strings in custom attributes.
		/// For more info, see <see cref="MetadataFlags.OptimizeCustomAttributeSerializedTypeNames"/></param>
		/// <returns>A <c>DeclSecurity</c> blob</returns>
		public static byte[] Write(ModuleDef module, IList<SecurityAttribute> secAttrs, IWriterError helper, bool optimizeCustomAttributeSerializedTypeNames) =>
			new DeclSecurityWriter(module, helper, optimizeCustomAttributeSerializedTypeNames, null).Write(secAttrs);

		internal static byte[] Write(ModuleDef module, IList<SecurityAttribute> secAttrs, IWriterError helper, bool optimizeCustomAttributeSerializedTypeNames, DataWriterContext context) =>
			new DeclSecurityWriter(module, helper, optimizeCustomAttributeSerializedTypeNames, context).Write(secAttrs);

		DeclSecurityWriter(ModuleDef module, IWriterError helper, bool optimizeCustomAttributeSerializedTypeNames, DataWriterContext context) {
			this.module = module;
			this.helper = helper;
			this.context = context;
			this.optimizeCustomAttributeSerializedTypeNames = optimizeCustomAttributeSerializedTypeNames;
		}

		byte[] Write(IList<SecurityAttribute> secAttrs) {
			if (secAttrs is null)
				secAttrs = Array2.Empty<SecurityAttribute>();

			var xml = DeclSecurity.GetNet1xXmlStringInternal(secAttrs);
			if (xml is not null)
				return WriteFormat1(xml);
			return WriteFormat2(secAttrs);
		}

		byte[] WriteFormat1(string xml) => Encoding.Unicode.GetBytes(xml);

		byte[] WriteFormat2(IList<SecurityAttribute> secAttrs) {
			var stream = new MemoryStream();
			var writer = new DataWriter(stream);
			writer.WriteByte((byte)'.');
			WriteCompressedUInt32(writer, (uint)secAttrs.Count);

			int count = secAttrs.Count;
			for (int i = 0; i < count; i++) {
				var sa = secAttrs[i];
				if (sa is null) {
					helper.Error("SecurityAttribute is null");
					Write(writer, UTF8String.Empty);
					WriteCompressedUInt32(writer, 1);
					WriteCompressedUInt32(writer, 0);
					continue;
				}
				var attrType = sa.AttributeType;
				string fqn;
				if (attrType is null) {
					helper.Error("SecurityAttribute attribute type is null");
					fqn = string.Empty;
				}
				else
					fqn = attrType.AssemblyQualifiedName;
				Write(writer, fqn);

				var namedArgsBlob = context is null ?
					CustomAttributeWriter.Write(this, sa.NamedArguments) :
					CustomAttributeWriter.Write(this, sa.NamedArguments, context);
				if (namedArgsBlob.Length > 0x1FFFFFFF) {
					helper.Error("Named arguments blob size doesn't fit in 29 bits");
					namedArgsBlob = Array2.Empty<byte>();
				}
				WriteCompressedUInt32(writer, (uint)namedArgsBlob.Length);
				writer.WriteBytes(namedArgsBlob);
			}

			return stream.ToArray();
		}

		uint WriteCompressedUInt32(DataWriter writer, uint value) => writer.WriteCompressedUInt32(helper, value);
		void Write(DataWriter writer, UTF8String s) => writer.Write(helper, s);
		void IWriterError.Error(string message) => helper.Error(message);
		bool IFullNameFactoryHelper.MustUseAssemblyName(IType type) =>
			FullNameFactory.MustUseAssemblyName(module, type, optimizeCustomAttributeSerializedTypeNames);
	}
}
