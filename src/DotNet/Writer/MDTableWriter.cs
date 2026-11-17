// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes <see cref="MDTable{T}"/>s
	/// </summary>
	public static class MDTableWriter {
		/// <summary>
		/// Writes a <c>Module</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawModuleRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var columns3 = columns[3];
			var columns4 = columns[4];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.Generation);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.Mvid);
				columns3.Write24(writer, row.EncId);
				columns4.Write24(writer, row.EncBaseId);
			}
		}

		/// <summary>
		/// Writes a <c>TypeRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawTypeRefRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.ResolutionScope);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, stringsHeap.GetOffset(row.Namespace));
			}
		}

		/// <summary>
		/// Writes a <c>TypeDef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawTypeDefRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var columns3 = columns[3];
			var columns4 = columns[4];
			var columns5 = columns[5];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Flags);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, stringsHeap.GetOffset(row.Namespace));
				columns3.Write24(writer, row.Extends);
				columns4.Write24(writer, row.FieldList);
				columns5.Write24(writer, row.MethodList);
			}
		}

		/// <summary>
		/// Writes a <c>FieldPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFieldPtrRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Field);
			}
		}

		/// <summary>
		/// Writes a <c>Field</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFieldRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.Flags);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>MethodPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodPtrRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Method);
			}
		}

		/// <summary>
		/// Writes a <c>Method</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodRow> table) {
			var columns = table.TableInfo.Columns;
			var columns3 = columns[3];
			var columns4 = columns[4];
			var columns5 = columns[5];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.RVA);
				writer.WriteUInt16(row.ImplFlags);
				writer.WriteUInt16(row.Flags);
				columns3.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns4.Write24(writer, row.Signature);
				columns5.Write24(writer, row.ParamList);
			}
		}

		/// <summary>
		/// Writes a <c>ParamPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawParamPtrRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Param);
			}
		}

		/// <summary>
		/// Writes a <c>Param</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawParamRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.Flags);
				writer.WriteUInt16(row.Sequence);
				columns2.Write24(writer, stringsHeap.GetOffset(row.Name));
			}
		}

		/// <summary>
		/// Writes a <c>InterfaceImpl</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawInterfaceImplRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Class);
				columns1.Write24(writer, row.Interface);
			}
		}

		/// <summary>
		/// Writes a <c>MemberRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMemberRefRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Class);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>Constant</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawConstantRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var columns3 = columns[3];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteByte(row.Type);
				writer.WriteByte(row.Padding);
				columns2.Write24(writer, row.Parent);
				columns3.Write24(writer, row.Value);
			}
		}

		/// <summary>
		/// Writes a <c>CustomAttribute</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawCustomAttributeRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.Type);
				columns2.Write24(writer, row.Value);
			}
		}

		/// <summary>
		/// Writes a <c>FieldMarshal</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFieldMarshalRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.NativeType);
			}
		}

		/// <summary>
		/// Writes a <c>DeclSecurity</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawDeclSecurityRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteInt16(row.Action);
				columns1.Write24(writer, row.Parent);
				columns2.Write24(writer, row.PermissionSet);
			}
		}

		/// <summary>
		/// Writes a <c>ClassLayout</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawClassLayoutRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.PackingSize);
				writer.WriteUInt32(row.ClassSize);
				columns2.Write24(writer, row.Parent);
			}
		}

		/// <summary>
		/// Writes a <c>FieldLayout</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFieldLayoutRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.OffSet);
				columns1.Write24(writer, row.Field);
			}
		}

		/// <summary>
		/// Writes a <c>StandAloneSig</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawStandAloneSigRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>EventMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawEventMapRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.EventList);
			}
		}

		/// <summary>
		/// Writes a <c>EventPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawEventPtrRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Event);
			}
		}

		/// <summary>
		/// Writes a <c>Event</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawEventRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.EventFlags);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.EventType);
			}
		}

		/// <summary>
		/// Writes a <c>PropertyMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawPropertyMapRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.PropertyList);
			}
		}

		/// <summary>
		/// Writes a <c>PropertyPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawPropertyPtrRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Property);
			}
		}

		/// <summary>
		/// Writes a <c>Property</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawPropertyRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.PropFlags);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.Type);
			}
		}

		/// <summary>
		/// Writes a <c>MethodSemantics</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodSemanticsRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.Semantic);
				columns1.Write24(writer, row.Method);
				columns2.Write24(writer, row.Association);
			}
		}

		/// <summary>
		/// Writes a <c>MethodImpl</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodImplRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Class);
				columns1.Write24(writer, row.MethodBody);
				columns2.Write24(writer, row.MethodDeclaration);
			}
		}

		/// <summary>
		/// Writes a <c>ModuleRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawModuleRefRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, stringsHeap.GetOffset(row.Name));
			}
		}

		/// <summary>
		/// Writes a <c>TypeSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawTypeSpecRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>ImplMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawImplMapRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var columns3 = columns[3];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.MappingFlags);
				columns1.Write24(writer, row.MemberForwarded);
				columns2.Write24(writer, stringsHeap.GetOffset(row.ImportName));
				columns3.Write24(writer, row.ImportScope);
			}
		}

		/// <summary>
		/// Writes a <c>FieldRVA</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFieldRVARow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.RVA);
				columns1.Write24(writer, row.Field);
			}
		}

		/// <summary>
		/// Writes a <c>ENCLog</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawENCLogRow> table) {
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Token);
				writer.WriteUInt32(row.FuncCode);
			}
		}

		/// <summary>
		/// Writes a <c>ENCMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawENCMapRow> table) {
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Token);
			}
		}

		/// <summary>
		/// Writes a <c>Assembly</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyRow> table) {
			var columns = table.TableInfo.Columns;
			var columns6 = columns[6];
			var columns7 = columns[7];
			var columns8 = columns[8];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.HashAlgId);
				writer.WriteUInt16(row.MajorVersion);
				writer.WriteUInt16(row.MinorVersion);
				writer.WriteUInt16(row.BuildNumber);
				writer.WriteUInt16(row.RevisionNumber);
				writer.WriteUInt32(row.Flags);
				columns6.Write24(writer, row.PublicKey);
				columns7.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns8.Write24(writer, stringsHeap.GetOffset(row.Locale));
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyProcessorRow> table) {
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Processor);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyOS</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyOSRow> table) {
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.OSPlatformId);
				writer.WriteUInt32(row.OSMajorVersion);
				writer.WriteUInt32(row.OSMinorVersion);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyRefRow> table) {
			var columns = table.TableInfo.Columns;
			var columns5 = columns[5];
			var columns6 = columns[6];
			var columns7 = columns[7];
			var columns8 = columns[8];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.MajorVersion);
				writer.WriteUInt16(row.MinorVersion);
				writer.WriteUInt16(row.BuildNumber);
				writer.WriteUInt16(row.RevisionNumber);
				writer.WriteUInt32(row.Flags);
				columns5.Write24(writer, row.PublicKeyOrToken);
				columns6.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns7.Write24(writer, stringsHeap.GetOffset(row.Locale));
				columns8.Write24(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRefProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyRefProcessorRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Processor);
				columns1.Write24(writer, row.AssemblyRef);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRefOS</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawAssemblyRefOSRow> table) {
			var columns = table.TableInfo.Columns;
			var columns3 = columns[3];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.OSPlatformId);
				writer.WriteUInt32(row.OSMajorVersion);
				writer.WriteUInt32(row.OSMinorVersion);
				columns3.Write24(writer, row.AssemblyRef);
			}
		}

		/// <summary>
		/// Writes a <c>File</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawFileRow> table) {
			var columns = table.TableInfo.Columns;
			var columns1 = columns[1];
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Flags);
				columns1.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns2.Write24(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>ExportedType</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawExportedTypeRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var columns3 = columns[3];
			var columns4 = columns[4];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Flags);
				writer.WriteUInt32(row.TypeDefId);
				columns2.Write24(writer, stringsHeap.GetOffset(row.TypeName));
				columns3.Write24(writer, stringsHeap.GetOffset(row.TypeNamespace));
				columns4.Write24(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>ManifestResource</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawManifestResourceRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var columns3 = columns[3];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt32(row.Offset);
				writer.WriteUInt32(row.Flags);
				columns2.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns3.Write24(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>NestedClass</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawNestedClassRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.NestedClass);
				columns1.Write24(writer, row.EnclosingClass);
			}
		}

		/// <summary>
		/// Writes a <c>GenericParam</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawGenericParamRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var columns3 = columns[3];
			var stringsHeap = metadata.StringsHeap;
			if (columns.Length >= 5) {
				var columns4 = columns[4];
				for (int i = 0; i < table.Rows; i++) {
					var row = table[(uint)i + 1];
					writer.WriteUInt16(row.Number);
					writer.WriteUInt16(row.Flags);
					columns2.Write24(writer, row.Owner);
					columns3.Write24(writer, stringsHeap.GetOffset(row.Name));
					columns4.Write24(writer, row.Kind);
				}
			}
			else {
				for (int i = 0; i < table.Rows; i++) {
					var row = table[(uint)i + 1];
					writer.WriteUInt16(row.Number);
					writer.WriteUInt16(row.Flags);
					columns2.Write24(writer, row.Owner);
					columns3.Write24(writer, stringsHeap.GetOffset(row.Name));
				}
			}
		}

		/// <summary>
		/// Writes a <c>MethodSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodSpecRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Method);
				columns1.Write24(writer, row.Instantiation);
			}
		}

		/// <summary>
		/// Writes a <c>GenericParamConstraint</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawGenericParamConstraintRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Owner);
				columns1.Write24(writer, row.Constraint);
			}
		}

		/// <summary>
		/// Writes a <c>Document</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawDocumentRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			var columns3 = columns[3];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Name);
				columns1.Write24(writer, row.HashAlgorithm);
				columns2.Write24(writer, row.Hash);
				columns3.Write24(writer, row.Language);
			}
		}

		/// <summary>
		/// Writes a <c>MethodDebugInformation</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawMethodDebugInformationRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Document);
				columns1.Write24(writer, row.SequencePoints);
			}
		}

		/// <summary>
		/// Writes a <c>LocalScope</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawLocalScopeRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			var columns3 = columns[3];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Method);
				columns1.Write24(writer, row.ImportScope);
				columns2.Write24(writer, row.VariableList);
				columns3.Write24(writer, row.ConstantList);
				writer.WriteUInt32(row.StartOffset);
				writer.WriteUInt32(row.Length);
			}
		}

		/// <summary>
		/// Writes a <c>LocalVariable</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawLocalVariableRow> table) {
			var columns = table.TableInfo.Columns;
			var columns2 = columns[2];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				writer.WriteUInt16(row.Attributes);
				writer.WriteUInt16(row.Index);
				columns2.Write24(writer, stringsHeap.GetOffset(row.Name));
			}
		}

		/// <summary>
		/// Writes a <c>LocalConstant</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawLocalConstantRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var stringsHeap = metadata.StringsHeap;
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, stringsHeap.GetOffset(row.Name));
				columns1.Write24(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>ImportScope</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawImportScopeRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.Imports);
			}
		}

		/// <summary>
		/// Writes a <c>StateMachineMethod</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawStateMachineMethodRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.MoveNextMethod);
				columns1.Write24(writer, row.KickoffMethod);
			}
		}

		/// <summary>
		/// Writes a <c>CustomDebugInformation</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this DataWriter writer, Metadata metadata, MDTable<RawCustomDebugInformationRow> table) {
			var columns = table.TableInfo.Columns;
			var columns0 = columns[0];
			var columns1 = columns[1];
			var columns2 = columns[2];
			for (int i = 0; i < table.Rows; i++) {
				var row = table[(uint)i + 1];
				columns0.Write24(writer, row.Parent);
				columns1.Write24(writer, row.Kind);
				columns2.Write24(writer, row.Value);
			}
		}
	}
}
