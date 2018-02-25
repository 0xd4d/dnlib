// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		/// <param name="row">Row</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, IMDTable table, IRawRow row) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var col in cols) {
				if (col.ColumnSize == ColumnSize.Strings)
					col.Write(writer, stringsHeap.GetOffset(row.Read(col.Index)));
				else
					col.Write(writer, row.Read(col.Index));
			}
		}

		/// <summary>
		/// Writes a metadata table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, IMDTable table) {
			switch (table.Table) {
			case Table.Module:			writer.Write(metadata, (MDTable<RawModuleRow>)table); break;
			case Table.TypeRef:			writer.Write(metadata, (MDTable<RawTypeRefRow>)table); break;
			case Table.TypeDef:			writer.Write(metadata, (MDTable<RawTypeDefRow>)table); break;
			case Table.FieldPtr:		writer.Write(metadata, (MDTable<RawFieldPtrRow>)table); break;
			case Table.Field:			writer.Write(metadata, (MDTable<RawFieldRow>)table); break;
			case Table.MethodPtr:		writer.Write(metadata, (MDTable<RawMethodPtrRow>)table); break;
			case Table.Method:			writer.Write(metadata, (MDTable<RawMethodRow>)table); break;
			case Table.ParamPtr:		writer.Write(metadata, (MDTable<RawParamPtrRow>)table); break;
			case Table.Param:			writer.Write(metadata, (MDTable<RawParamRow>)table); break;
			case Table.InterfaceImpl:	writer.Write(metadata, (MDTable<RawInterfaceImplRow>)table); break;
			case Table.MemberRef:		writer.Write(metadata, (MDTable<RawMemberRefRow>)table); break;
			case Table.Constant:		writer.Write(metadata, (MDTable<RawConstantRow>)table); break;
			case Table.CustomAttribute:	writer.Write(metadata, (MDTable<RawCustomAttributeRow>)table); break;
			case Table.FieldMarshal:	writer.Write(metadata, (MDTable<RawFieldMarshalRow>)table); break;
			case Table.DeclSecurity:	writer.Write(metadata, (MDTable<RawDeclSecurityRow>)table); break;
			case Table.ClassLayout:		writer.Write(metadata, (MDTable<RawClassLayoutRow>)table); break;
			case Table.FieldLayout:		writer.Write(metadata, (MDTable<RawFieldLayoutRow>)table); break;
			case Table.StandAloneSig:	writer.Write(metadata, (MDTable<RawStandAloneSigRow>)table); break;
			case Table.EventMap:		writer.Write(metadata, (MDTable<RawEventMapRow>)table); break;
			case Table.EventPtr:		writer.Write(metadata, (MDTable<RawEventPtrRow>)table); break;
			case Table.Event:			writer.Write(metadata, (MDTable<RawEventRow>)table); break;
			case Table.PropertyMap:		writer.Write(metadata, (MDTable<RawPropertyMapRow>)table); break;
			case Table.PropertyPtr:		writer.Write(metadata, (MDTable<RawPropertyPtrRow>)table); break;
			case Table.Property:		writer.Write(metadata, (MDTable<RawPropertyRow>)table); break;
			case Table.MethodSemantics:	writer.Write(metadata, (MDTable<RawMethodSemanticsRow>)table); break;
			case Table.MethodImpl:		writer.Write(metadata, (MDTable<RawMethodImplRow>)table); break;
			case Table.ModuleRef:		writer.Write(metadata, (MDTable<RawModuleRefRow>)table); break;
			case Table.TypeSpec:		writer.Write(metadata, (MDTable<RawTypeSpecRow>)table); break;
			case Table.ImplMap:			writer.Write(metadata, (MDTable<RawImplMapRow>)table); break;
			case Table.FieldRVA:		writer.Write(metadata, (MDTable<RawFieldRVARow>)table); break;
			case Table.ENCLog:			writer.Write(metadata, (MDTable<RawENCLogRow>)table); break;
			case Table.ENCMap:			writer.Write(metadata, (MDTable<RawENCMapRow>)table); break;
			case Table.Assembly:		writer.Write(metadata, (MDTable<RawAssemblyRow>)table); break;
			case Table.AssemblyProcessor: writer.Write(metadata, (MDTable<RawAssemblyProcessorRow>)table); break;
			case Table.AssemblyOS:		writer.Write(metadata, (MDTable<RawAssemblyOSRow>)table); break;
			case Table.AssemblyRef:		writer.Write(metadata, (MDTable<RawAssemblyRefRow>)table); break;
			case Table.AssemblyRefProcessor: writer.Write(metadata, (MDTable<RawAssemblyRefProcessorRow>)table); break;
			case Table.AssemblyRefOS:	writer.Write(metadata, (MDTable<RawAssemblyRefOSRow>)table); break;
			case Table.File:			writer.Write(metadata, (MDTable<RawFileRow>)table); break;
			case Table.ExportedType:	writer.Write(metadata, (MDTable<RawExportedTypeRow>)table); break;
			case Table.ManifestResource:writer.Write(metadata, (MDTable<RawManifestResourceRow>)table); break;
			case Table.NestedClass:		writer.Write(metadata, (MDTable<RawNestedClassRow>)table); break;
			case Table.GenericParam:	writer.Write(metadata, (MDTable<RawGenericParamRow>)table); break;
			case Table.MethodSpec:		writer.Write(metadata, (MDTable<RawMethodSpecRow>)table); break;
			case Table.GenericParamConstraint: writer.Write(metadata, (MDTable<RawGenericParamConstraintRow>)table); break;
			case Table.Document:		writer.Write(metadata, (MDTable<RawDocumentRow>)table); break;
			case Table.MethodDebugInformation: writer.Write(metadata, (MDTable<RawMethodDebugInformationRow>)table); break;
			case Table.LocalScope:		writer.Write(metadata, (MDTable<RawLocalScopeRow>)table); break;
			case Table.LocalVariable:	writer.Write(metadata, (MDTable<RawLocalVariableRow>)table); break;
			case Table.LocalConstant:	writer.Write(metadata, (MDTable<RawLocalConstantRow>)table); break;
			case Table.ImportScope:		writer.Write(metadata, (MDTable<RawImportScopeRow>)table); break;
			case Table.StateMachineMethod: writer.Write(metadata, (MDTable<RawStateMachineMethodRow>)table); break;
			case Table.CustomDebugInformation: writer.Write(metadata, (MDTable<RawCustomDebugInformationRow>)table); break;

			default:
				Debug.Fail(string.Format("Unknown table: {0}, add a new method overload", table.Table));
				var cols = table.TableInfo.Columns;
				var stringsHeap = metadata.StringsHeap;
				foreach (var row in table.GetRawRows()) {
					foreach (var col in cols) {
						if (col.ColumnSize == ColumnSize.Strings)
							col.Write(writer, stringsHeap.GetOffset(row.Read(col.Index)));
						else
							col.Write(writer, row.Read(col.Index));
					}
				}
				break;
			}
		}

		/// <summary>
		/// Writes a <c>Module</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawModuleRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Generation);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.Mvid);
				cols[3].Write(writer, row.EncId);
				cols[4].Write(writer, row.EncBaseId);
			}
		}

		/// <summary>
		/// Writes a <c>TypeRef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawTypeRefRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				cols[0].Write(writer, row.ResolutionScope);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, stringsHeap.GetOffset(row.Namespace));
			}
		}

		/// <summary>
		/// Writes a <c>TypeDef</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawTypeDefRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, stringsHeap.GetOffset(row.Namespace));
				cols[3].Write(writer, row.Extends);
				cols[4].Write(writer, row.FieldList);
				cols[5].Write(writer, row.MethodList);
			}
		}

		/// <summary>
		/// Writes a <c>FieldPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFieldPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Field);
		}

		/// <summary>
		/// Writes a <c>Field</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFieldRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>MethodPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Method);
		}

		/// <summary>
		/// Writes a <c>Method</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.RVA);
				writer.Write(row.ImplFlags);
				writer.Write(row.Flags);
				cols[3].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[4].Write(writer, row.Signature);
				cols[5].Write(writer, row.ParamList);
			}
		}

		/// <summary>
		/// Writes a <c>ParamPtr</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawParamPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Param);
		}

		/// <summary>
		/// Writes a <c>Param</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawParamRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.Sequence);
				cols[2].Write(writer, stringsHeap.GetOffset(row.Name));
			}
		}

		/// <summary>
		/// Writes a <c>InterfaceImpl</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawInterfaceImplRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMemberRefRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>Constant</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawConstantRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Type);
				writer.Write(row.Padding);
				cols[2].Write(writer, row.Parent);
				cols[3].Write(writer, row.Value);
			}
		}

		/// <summary>
		/// Writes a <c>CustomAttribute</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawCustomAttributeRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFieldMarshalRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawDeclSecurityRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawClassLayoutRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFieldLayoutRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawStandAloneSigRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		/// <summary>
		/// Writes a <c>EventMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawEventMapRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawEventPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Event);
		}

		/// <summary>
		/// Writes a <c>Event</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawEventRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.EventFlags);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.EventType);
			}
		}

		/// <summary>
		/// Writes a <c>PropertyMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawPropertyMapRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawPropertyPtrRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Property);
		}

		/// <summary>
		/// Writes a <c>Property</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawPropertyRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.PropFlags);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.Type);
			}
		}

		/// <summary>
		/// Writes a <c>MethodSemantics</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodSemanticsRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodImplRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawModuleRefRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table)
				cols[0].Write(writer, stringsHeap.GetOffset(row.Name));
		}

		/// <summary>
		/// Writes a <c>TypeSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawTypeSpecRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		/// <summary>
		/// Writes a <c>ImplMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawImplMapRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.MappingFlags);
				cols[1].Write(writer, row.MemberForwarded);
				cols[2].Write(writer, stringsHeap.GetOffset(row.ImportName));
				cols[3].Write(writer, row.ImportScope);
			}
		}

		/// <summary>
		/// Writes a <c>FieldRVA</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFieldRVARow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawENCLogRow> table) {
			foreach (var row in table) {
				writer.Write(row.Token);
				writer.Write(row.FuncCode);
			}
		}

		/// <summary>
		/// Writes a <c>ENCMap</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawENCMapRow> table) {
			foreach (var row in table)
				writer.Write(row.Token);
		}

		/// <summary>
		/// Writes a <c>Assembly</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.HashAlgId);
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[6].Write(writer, row.PublicKey);
				cols[7].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[8].Write(writer, stringsHeap.GetOffset(row.Locale));
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyProcessorRow> table) {
			foreach (var row in table)
				writer.Write(row.Processor);
		}

		/// <summary>
		/// Writes a <c>AssemblyOS</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyOSRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyRefRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[5].Write(writer, row.PublicKeyOrToken);
				cols[6].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[7].Write(writer, stringsHeap.GetOffset(row.Locale));
				cols[8].Write(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>AssemblyRefProcessor</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyRefProcessorRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawAssemblyRefOSRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawFileRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[2].Write(writer, row.HashValue);
			}
		}

		/// <summary>
		/// Writes a <c>ExportedType</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawExportedTypeRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.TypeDefId);
				cols[2].Write(writer, stringsHeap.GetOffset(row.TypeName));
				cols[3].Write(writer, stringsHeap.GetOffset(row.TypeNamespace));
				cols[4].Write(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>ManifestResource</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawManifestResourceRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				writer.Write(row.Offset);
				writer.Write(row.Flags);
				cols[2].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[3].Write(writer, row.Implementation);
			}
		}

		/// <summary>
		/// Writes a <c>NestedClass</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawNestedClassRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawGenericParamRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			if (cols.Count >= 5) {
				foreach (var row in table) {
					writer.Write(row.Number);
					writer.Write(row.Flags);
					cols[2].Write(writer, row.Owner);
					cols[3].Write(writer, stringsHeap.GetOffset(row.Name));
					cols[4].Write(writer, row.Kind);
				}
			}
			else {
				foreach (var row in table) {
					writer.Write(row.Number);
					writer.Write(row.Flags);
					cols[2].Write(writer, row.Owner);
					cols[3].Write(writer, stringsHeap.GetOffset(row.Name));
				}
			}
		}

		/// <summary>
		/// Writes a <c>MethodSpec</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodSpecRow> table) {
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
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawGenericParamConstraintRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Owner);
				cols[1].Write(writer, row.Constraint);
			}
		}

		/// <summary>
		/// Writes a <c>Document</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawDocumentRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Name);
				cols[1].Write(writer, row.HashAlgorithm);
				cols[2].Write(writer, row.Hash);
				cols[3].Write(writer, row.Language);
			}
		}

		/// <summary>
		/// Writes a <c>MethodDebugInformation</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawMethodDebugInformationRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Document);
				cols[1].Write(writer, row.SequencePoints);
			}
		}

		/// <summary>
		/// Writes a <c>LocalScope</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawLocalScopeRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Method);
				cols[1].Write(writer, row.ImportScope);
				cols[2].Write(writer, row.VariableList);
				cols[3].Write(writer, row.ConstantList);
				cols[4].Write(writer, row.StartOffset);
				cols[5].Write(writer, row.Length);
			}
		}

		/// <summary>
		/// Writes a <c>LocalVariable</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawLocalVariableRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				cols[0].Write(writer, row.Attributes);
				cols[1].Write(writer, row.Index);
				cols[2].Write(writer, stringsHeap.GetOffset(row.Name));
			}
		}

		/// <summary>
		/// Writes a <c>LocalConstant</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawLocalConstantRow> table) {
			var cols = table.TableInfo.Columns;
			var stringsHeap = metadata.StringsHeap;
			foreach (var row in table) {
				cols[0].Write(writer, stringsHeap.GetOffset(row.Name));
				cols[1].Write(writer, row.Signature);
			}
		}

		/// <summary>
		/// Writes a <c>ImportScope</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawImportScopeRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.Imports);
			}
		}

		/// <summary>
		/// Writes a <c>StateMachineMethod</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawStateMachineMethodRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.MoveNextMethod);
				cols[1].Write(writer, row.KickoffMethod);
			}
		}

		/// <summary>
		/// Writes a <c>CustomDebugInformation</c> table
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="table">Table</param>
		public static void Write(this BinaryWriter writer, MetaData metadata, MDTable<RawCustomDebugInformationRow> table) {
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.Kind);
				cols[2].Write(writer, row.Value);
			}
		}
	}
}
