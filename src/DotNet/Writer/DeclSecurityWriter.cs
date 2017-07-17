// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes <c>DeclSecurity</c> blobs
	/// </summary>
	public struct DeclSecurityWriter : ICustomAttributeWriterHelper {
		readonly ModuleDef module;
		readonly IWriterError helper;
		readonly BinaryWriterContext context;

		/// <summary>
		/// Creates a <c>DeclSecurity</c> blob from <paramref name="secAttrs"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="secAttrs">List of <see cref="SecurityAttribute"/>s</param>
		/// <param name="helper">Helps this class</param>
		/// <returns>A <c>DeclSecurity</c> blob</returns>
		public static byte[] Write(ModuleDef module, IList<SecurityAttribute> secAttrs, IWriterError helper) {
			return new DeclSecurityWriter(module, helper, null).Write(secAttrs);
		}

		internal static byte[] Write(ModuleDef module, IList<SecurityAttribute> secAttrs, IWriterError helper, BinaryWriterContext context) {
			return new DeclSecurityWriter(module, helper, context).Write(secAttrs);
		}

		DeclSecurityWriter(ModuleDef module, IWriterError helper, BinaryWriterContext context) {
			this.module = module;
			this.helper = helper;
			this.context = context;
		}

		byte[] Write(IList<SecurityAttribute> secAttrs) {
			if (secAttrs == null)
				secAttrs = new SecurityAttribute[0];

			var xml = DeclSecurity.GetNet1xXmlStringInternal(secAttrs);
			if (xml != null)
				return WriteFormat1(xml);
			return WriteFormat2(secAttrs);
		}

		byte[] WriteFormat1(string xml) {
			return Encoding.Unicode.GetBytes(xml);
		}

		byte[] WriteFormat2(IList<SecurityAttribute> secAttrs) {
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream)) {
				writer.Write((byte)'.');
				WriteCompressedUInt32(writer, (uint)secAttrs.Count);

				foreach (var sa in secAttrs) {
					if (sa == null) {
						helper.Error("SecurityAttribute is null");
						Write(writer, UTF8String.Empty);
						WriteCompressedUInt32(writer, 1);
						WriteCompressedUInt32(writer, 0);
						continue;
					}
					var attrType = sa.AttributeType;
					string fqn;
					if (attrType == null) {
						helper.Error("SecurityAttribute attribute type is null");
						fqn = string.Empty;
					}
					else
						fqn = attrType.AssemblyQualifiedName;
					Write(writer, fqn);

					var namedArgsBlob = context == null ?
						CustomAttributeWriter.Write(this, sa.NamedArguments) :
						CustomAttributeWriter.Write(this, sa.NamedArguments, context);
					if (namedArgsBlob.Length > 0x1FFFFFFF) {
						helper.Error("Named arguments blob size doesn't fit in 29 bits");
						namedArgsBlob = new byte[0];
					}
					WriteCompressedUInt32(writer, (uint)namedArgsBlob.Length);
					writer.Write(namedArgsBlob);
				}

				return stream.ToArray();
			}
		}

		uint WriteCompressedUInt32(BinaryWriter writer, uint value) {
			return writer.WriteCompressedUInt32(helper, value);
		}

		void Write(BinaryWriter writer, UTF8String s) {
			writer.Write(helper, s);
		}

		void IWriterError.Error(string message) {
			helper.Error(message);
		}

		bool IFullNameCreatorHelper.MustUseAssemblyName(IType type) {
			return FullNameCreator.MustUseAssemblyName(module, type);
		}
	}
}
