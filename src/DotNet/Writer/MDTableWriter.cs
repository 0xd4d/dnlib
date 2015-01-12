// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes <see cref="MDTable{T}"/>s
	/// </summary>
	public static class MDTableWriter {
		/// <summary>
		/// Writes a raw row
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		/// <param name="row">Row</param>
		public static void Write(this BinaryWriter writer, IMDTable table, IRawRow row) {
			if (table.Table == Table.Constant) {
				var cols = table.TableInfo.Columns;
				var row2 = (RawConstantRow)row;
				writer.Write(row2.Type);
				writer.Write(row2.Padding);
				cols[1].Write(writer, row2.Parent);
				cols[2].Write(writer, row2.Value);
			}
			else {
				var cols = table.TableInfo.Columns;
				foreach (var col in cols)
					col.Write(writer, row.Read(col.Index));
			}
		}

		/// <summary>
		/// Writes a metadata table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, IMDTable table) {
			switch (table.Table) {
			case Table.Module:			writer.Write((MDTable<RawModuleRow>)table); break;
			case Table.TypeRef:			writer.Write((MDTable<RawTypeRefRow>)table); break;
			case Table.TypeDef:			writer.Write((MDTable<RawTypeDefRow>)table); break;
			case Table.FieldPtr:		writer.Write((MDTable<RawFieldPtrRow>)table); break;
			case Table.Field:			writer.Write((MDTable<RawFieldRow>)table); break;
			case Table.MethodPtr:		writer.Write((MDTable<RawMethodPtrRow>)table); break;
			case Table.Method:			writer.Write((MDTable<RawMethodRow>)table); break;
			case Table.ParamPtr:		writer.Write((MDTable<RawParamPtrRow>)table); break;
			case Table.Param:			writer.Write((MDTable<RawParamRow>)table); break;
			case Table.InterfaceImpl:	writer.Write((MDTable<RawInterfaceImplRow>)table); break;
			case Table.MemberRef:		writer.Write((MDTable<RawMemberRefRow>)table); break;
			case Table.Constant:		writer.Write((MDTable<RawConstantRow>)table); break;
			case Table.CustomAttribute:	writer.Write((MDTable<RawCustomAttributeRow>)table); break;
			case Table.FieldMarshal:	writer.Write((MDTable<RawFieldMarshalRow>)table); break;
			case Table.DeclSecurity:	writer.Write((MDTable<RawDeclSecurityRow>)table); break;
			case Table.ClassLayout:		writer.Write((MDTable<RawClassLayoutRow>)table); break;
			case Table.FieldLayout:		writer.Write((MDTable<RawFieldLayoutRow>)table); break;
			case Table.StandAloneSig:	writer.Write((MDTable<RawStandAloneSigRow>)table); break;
			case Table.EventMap:		writer.Write((MDTable<RawEventMapRow>)table); break;
			case Table.EventPtr:		writer.Write((MDTable<RawEventPtrRow>)table); break;
			case Table.Event:			writer.Write((MDTable<RawEventRow>)table); break;
			case Table.PropertyMap:		writer.Write((MDTable<RawPropertyMapRow>)table); break;
			case Table.PropertyPtr:		writer.Write((MDTable<RawPropertyPtrRow>)table); break;
			case Table.Property:		writer.Write((MDTable<RawPropertyRow>)table); break;
			case Table.MethodSemantics:	writer.Write((MDTable<RawMethodSemanticsRow>)table); break;
			case Table.MethodImpl:		writer.Write((MDTable<RawMethodImplRow>)table); break;
			case Table.ModuleRef:		writer.Write((MDTable<RawModuleRefRow>)table); break;
			case Table.TypeSpec:		writer.Write((MDTable<RawTypeSpecRow>)table); break;
			case Table.ImplMap:			writer.Write((MDTable<RawImplMapRow>)table); break;
			case Table.FieldRVA:		writer.Write((MDTable<RawFieldRVARow>)table); break;
			case Table.ENCLog:			writer.Write((MDTable<RawENCLogRow>)table); break;
			case Table.ENCMap:			writer.Write((MDTable<RawENCMapRow>)table); break;
			case Table.Assembly:		writer.Write((MDTable<RawAssemblyRow>)table); break;
			case Table.AssemblyProcessor: writer.Write((MDTable<RawAssemblyProcessorRow>)table); break;
			case Table.AssemblyOS:		writer.Write((MDTable<RawAssemblyOSRow>)table); break;
			case Table.AssemblyRef:		writer.Write((MDTable<RawAssemblyRefRow>)table); break;
			case Table.AssemblyRefProcessor: writer.Write((MDTable<RawAssemblyRefProcessorRow>)table); break;
			case Table.AssemblyRefOS:	writer.Write((MDTable<RawAssemblyRefOSRow>)table); break;
			case Table.File:			writer.Write((MDTable<RawFileRow>)table); break;
			case Table.ExportedType:	writer.Write((MDTable<RawExportedTypeRow>)table); break;
			case Table.ManifestResource:writer.Write((MDTable<RawManifestResourceRow>)table); break;
			case Table.NestedClass:		writer.Write((MDTable<RawNestedClassRow>)table); break;
			case Table.GenericParam:	writer.Write((MDTable<RawGenericParamRow>)table); break;
			case Table.MethodSpec:		writer.Write((MDTable<RawMethodSpecRow>)table); break;
			case Table.GenericParamConstraint: writer.Write((MDTable<RawGenericParamConstraintRow>)table); break;

			default:
				var cols = table.TableInfo.Columns;
				foreach (var row in table.GetRawRows()) {
					foreach (var col in cols)
						col.Write(writer, row.Read(col.Index));
				}
				break;
			}
		}

		/// <summary>
		/// Writes a <c>Module</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawModuleRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Generation);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Mvid);
				cols[3].Write(writer, row.EncId);
				cols[4].Write(writer, row.EncBaseId);
			}
		}

		/// <summary>
		/// Writes a <c>TypeRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawTypeRefRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.ResolutionScope);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Namespace);
			}
		}

		/// <summary>
		/// Writes a <c>TypeDef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawTypeDefRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Namespace);
				cols[3].Write(writer, row.Extends);
				cols[4].Write(writer, row.FieldList);
				cols[5].Write(writer, row.MethodList);
			}
		}

		/// <summary>
		/// Writes a <c>FieldPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFieldPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Field);
		}

		/// <summary>
		/// Writes a <c>Field</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFieldRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>MethodPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMethodPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Method);
		}

		/// <summary>
		/// Writes a <c>Method</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMethodRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.RVA);
				writer.Write(row.ImplFlags);
				writer.Write(row.Flags);
				cols[3].Write(writer, row.Name);
				cols[4].Write(writer, row.Signature);
				cols[5].Write(writer, row.ParamList);
			}
		}

		/// <summary>
		/// Writes a <c>ParamPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawParamPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Param);
		}

		/// <summary>
		/// Writes a <c>Param</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawParamRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.Sequence);
				cols[2].Write(writer, row.Name);
			}
		}

		/// <summary>
		/// Writes a <c>InterfaceImpl</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawInterfaceImplRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.Interface);
			}
		}

		/// <summary>
		/// Writes a <c>MemberRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMemberRefRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>Constant</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawConstantRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Type);
				writer.Write(row.Padding);
				cols[1].Write(writer, row.Parent);
				cols[2].Write(writer, row.Value);
			}
		}

		/// <summary>
		/// Writes a <c>CustomAttribute</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawCustomAttributeRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.Type);
				cols[2].Write(writer, row.Value);
			}
		}

		/// <summary>
		/// Writes a <c>FieldMarshal</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFieldMarshalRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.NativeType);
			}
		}

		/// <summary>
		/// Writes a <c>DeclSecurity</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawDeclSecurityRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Action);
				cols[1].Write(writer, row.Parent);
				cols[2].Write(writer, row.PermissionSet);
			}
		}

		/// <summary>
		/// Writes a <c>ClassLayout</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawClassLayoutRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.PackingSize);
				writer.Write(row.ClassSize);
				cols[2].Write(writer, row.Parent);
			}
		}

		/// <summary>
		/// Writes a <c>FieldLayout</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFieldLayoutRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.OffSet);
				cols[1].Write(writer, row.Field);
			}
		}

		/// <summary>
		/// Writes a <c>StandAloneSig</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawStandAloneSigRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		/// <summary>
		/// Writes a <c>EventMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawEventMapRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.EventList);
			}
		}

		/// <summary>
		/// Writes a <c>EventPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawEventPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Event);
		}

		/// <summary>
		/// Writes a <c>Event</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawEventRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.EventFlags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.EventType);
			}
		}

		/// <summary>
		/// Writes a <c>PropertyMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawPropertyMapRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.PropertyList);
			}
		}

		/// <summary>
		/// Writes a <c>PropertyPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawPropertyPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Property);
		}

		/// <summary>
		/// Writes a <c>Property</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawPropertyRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.PropFlags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Type);
			}
		}

		/// <summary>
		/// Writes a <c>MethodSemantics</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMethodSemanticsRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Semantic);
				cols[1].Write(writer, row.Method);
				cols[2].Write(writer, row.Association);
			}
		}

		/// <summary>
		/// Writes a <c>MethodImpl</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMethodImplRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.MethodBody);
				cols[2].Write(writer, row.MethodDeclaration);
			}
		}

		/// <summary>
		/// Writes a <c>ModuleRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawModuleRefRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Name);
		}

		/// <summary>
		/// Writes a <c>TypeSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawTypeSpecRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		/// <summary>
		/// Writes a <c>ImplMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawImplMapRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.MappingFlags);
				cols[1].Write(writer, row.MemberForwarded);
				cols[2].Write(writer, row.ImportName);
				cols[3].Write(writer, row.ImportScope);
			}
		}

		/// <summary>
		/// Writes a <c>FieldRVA</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFieldRVARow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.RVA);
				cols[1].Write(writer, row.Field);
			}
		}

		/// <summary>
		/// Writes a <c>ENCLog</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawENCLogRow> table) {
			foreach (var row in table) {
				writer.Write(row.Token);
				writer.Write(row.FuncCode);
			}
		}

		/// <summary>
		/// Writes a <c>ENCMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawENCMapRow> table) {
			foreach (var row in table)
				writer.Write(row.Token);
		}

		/// <summary>
		/// Writes a <c>Assembly</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.HashAlgId);
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[6].Write(writer, row.PublicKey);
				cols[7].Write(writer, row.Name);
				cols[8].Write(writer, row.Locale);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyProcessorRow> table) {
			foreach (var row in table)
				writer.Write(row.Processor);
		}

		/// <summary>
		/// Writes a <c>AssemblyOS</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyOSRow> table) {
			foreach (var row in table) {
				writer.Write(row.OSPlatformId);
				writer.Write(row.OSMajorVersion);
				writer.Write(row.OSMinorVersion);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyRefRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[5].Write(writer, row.PublicKeyOrToken);
				cols[6].Write(writer, row.Name);
				cols[7].Write(writer, row.Locale);
				cols[8].Write(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRefProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyRefProcessorRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Processor);
				cols[1].Write(writer, row.AssemblyRef);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRefOS</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawAssemblyRefOSRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.OSPlatformId);
				writer.Write(row.OSMajorVersion);
				writer.Write(row.OSMinorVersion);
				cols[3].Write(writer, row.AssemblyRef);
			}
		}

		/// <summary>
		/// Writes a <c>File</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawFileRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>ExportedType</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawExportedTypeRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.TypeDefId);
				cols[2].Write(writer, row.TypeName);
				cols[3].Write(writer, row.TypeNamespace);
				cols[4].Write(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>ManifestResource</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawManifestResourceRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Offset);
				writer.Write(row.Flags);
				cols[2].Write(writer, row.Name);
				cols[3].Write(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>NestedClass</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawNestedClassRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.NestedClass);
				cols[1].Write(writer, row.EnclosingClass);
			}
		}

		/// <summary>
		/// Writes a <c>GenericParam</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawGenericParamRow> table) {
			var cols = table.TableInfo.Columns;
			bool useKindColumn = cols.Count >= 5;
			foreach (var row in table) {
				writer.Write(row.Number);
				writer.Write(row.Flags);
				cols[2].Write(writer, row.Owner);
				cols[3].Write(writer, row.Name);
				if (useKindColumn)
					cols[4].Write(writer, row.Kind);
			}
		}

		/// <summary>
		/// Writes a <c>MethodSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawMethodSpecRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Method);
				cols[1].Write(writer, row.Instantiation);
			}
		}

		/// <summary>
		/// Writes a <c>GenericParamConstraint</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MDTable<RawGenericParamConstraintRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Owner);
				cols[1].Write(writer, row.Constraint);
			}
		}
	}
}
