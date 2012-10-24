using System;
using System.Collections.Generic;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Initializes .NET table row sizes
	/// </summary>
	sealed class DotNetTableSizes {
		bool bigStrings;
		bool bigGuid;
		bool bigBlob;
		IList<uint> rowCounts;
		TableInfo[] tableInfos;

		/// <summary>
		/// Initializes the table sizes
		/// </summary>
		/// <param name="bigStrings"><c>true</c> if #Strings size >= 0x10000</param>
		/// <param name="bigGuid"><c>true</c> if #GUID size >= 0x10000</param>
		/// <param name="bigBlob"><c>true</c> if #Blob size >= 0x10000</param>
		/// <param name="rowCounts">Count of rows in each table</param>
		public void InitializeSizes(bool bigStrings, bool bigGuid, bool bigBlob, IList<uint> rowCounts) {
			this.bigStrings = bigStrings;
			this.bigGuid = bigGuid;
			this.bigBlob = bigBlob;
			this.rowCounts = rowCounts;
			foreach (var tableInfo in tableInfos) {
				int colOffset = 0;
				foreach (var colInfo in tableInfo.Columns) {
					colInfo.Offset = colOffset;
					var colSize = GetSize(colInfo.ColumnSize);
					colInfo.Size = colSize;
					colOffset += colSize + (colSize & 1);
				}
				tableInfo.RowSize = colOffset;
			}
		}

		int GetSize(ColumnSize columnSize) {
			if (ColumnSize.Module <= columnSize && columnSize <= ColumnSize.GenericParamConstraint) {
				int table = (int)(columnSize - ColumnSize.Module);
				return rowCounts[table] > 0xFFFF ? 4 : 2;
			}
			else if (ColumnSize.TypeDefOrRef <= columnSize && columnSize <= ColumnSize.TypeOrMethodDef) {
				CodedToken info;
				switch (columnSize) {
				case ColumnSize.TypeDefOrRef:		info = CodedToken.TypeDefOrRef; break;
				case ColumnSize.HasConstant:		info = CodedToken.HasConstant; break;
				case ColumnSize.HasCustomAttribute:	info = CodedToken.HasCustomAttribute; break;
				case ColumnSize.HasFieldMarshal:	info = CodedToken.HasFieldMarshal; break;
				case ColumnSize.HasDeclSecurity:	info = CodedToken.HasDeclSecurity; break;
				case ColumnSize.MemberRefParent:	info = CodedToken.MemberRefParent; break;
				case ColumnSize.HasSemantic:		info = CodedToken.HasSemantic; break;
				case ColumnSize.MethodDefOrRef:		info = CodedToken.MethodDefOrRef; break;
				case ColumnSize.MemberForwarded:	info = CodedToken.MemberForwarded; break;
				case ColumnSize.Implementation:		info = CodedToken.Implementation; break;
				case ColumnSize.CustomAttributeType:info = CodedToken.CustomAttributeType; break;
				case ColumnSize.ResolutionScope:	info = CodedToken.ResolutionScope; break;
				case ColumnSize.TypeOrMethodDef:	info = CodedToken.TypeOrMethodDef; break;
				default: throw new InvalidOperationException(string.Format("Invalid ColumnSize: {0}", columnSize));
				}
				uint maxRows = 0;
				foreach (var tableType in info.TableTypes) {
					var tableRows = rowCounts[(int)tableType];
					if (tableRows > maxRows)
						maxRows = tableRows;
				}
				ulong finalRows = (ulong)maxRows << info.Bits;
				return finalRows > 0xFFFF ? 4 : 2;
			}
			else {
				switch (columnSize) {
				case ColumnSize.Byte:	return 1;
				case ColumnSize.Int16:	return 2;
				case ColumnSize.UInt16:	return 2;
				case ColumnSize.Int32:	return 4;
				case ColumnSize.UInt32:	return 4;
				case ColumnSize.Strings:return bigStrings ? 4 : 2;
				case ColumnSize.GUID:	return bigGuid ? 4 : 2;
				case ColumnSize.Blob:	return bigBlob ? 4 : 2;
				}
			}
			throw new InvalidOperationException(string.Format("Invalid ColumnSize: {0}", columnSize));
		}

		/// <summary>
		/// Creates the table infos
		/// </summary>
		/// <param name="majorVersion">Major table version</param>
		/// <param name="minorVersion">Minor table version</param>
		/// <param name="maxPresentTables">Initialized to max present tables (eg. 42 or 45)</param>
		/// <returns>All table infos (not completely initialized)</returns>
		public TableInfo[] CreateTables(byte majorVersion, byte minorVersion, out int maxPresentTables) {
			// v1.0 doesn't support generics. 1.1 supports generics but the GenericParam
			// table is different from the 2.0 GenericParam table.
			maxPresentTables = (majorVersion == 1 && minorVersion == 0) ? (int)Table.NestedClass + 1 : (int)Table.GenericParamConstraint + 1;

			var tableInfos = new TableInfo[(int)Table.GenericParamConstraint + 1];

			tableInfos[(int)Table.Module] = new TableInfo(Table.Module, "Module", new ColumnInfo[] {
				new ColumnInfo("Generation", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Mvid", ColumnSize.GUID),
				new ColumnInfo("EncId", ColumnSize.GUID),
				new ColumnInfo("EncBaseId", ColumnSize.GUID),
			});
			tableInfos[(int)Table.TypeRef] = new TableInfo(Table.TypeRef, "TypeRef", new ColumnInfo[] {
				new ColumnInfo("ResolutionScope", ColumnSize.ResolutionScope),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Namespace", ColumnSize.Strings),
			});
			tableInfos[(int)Table.TypeDef] = new TableInfo(Table.TypeDef, "TypeDef", new ColumnInfo[] {
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Namespace", ColumnSize.Strings),
				new ColumnInfo("Extends", ColumnSize.TypeDefOrRef),
				new ColumnInfo("FieldList", ColumnSize.Field),
				new ColumnInfo("MethodList", ColumnSize.Method),
			});
			tableInfos[(int)Table.FieldPtr] = new TableInfo(Table.FieldPtr, "FieldPtr", new ColumnInfo[] {
				new ColumnInfo("Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.Field] = new TableInfo(Table.Field, "Field", new ColumnInfo[] {
				new ColumnInfo("Flags", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.MethodPtr] = new TableInfo(Table.MethodPtr, "MethodPtr", new ColumnInfo[] {
				new ColumnInfo("Method", ColumnSize.Method),
			});
			tableInfos[(int)Table.Method] = new TableInfo(Table.Method, "Method", new ColumnInfo[] {
				new ColumnInfo("RVA", ColumnSize.UInt32),
				new ColumnInfo("ImplFlags", ColumnSize.UInt16),
				new ColumnInfo("Flags", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Signature", ColumnSize.Blob),
				new ColumnInfo("ParamList", ColumnSize.Param),
			});
			tableInfos[(int)Table.ParamPtr] = new TableInfo(Table.ParamPtr, "ParamPtr", new ColumnInfo[] {
				new ColumnInfo("Param", ColumnSize.Param),
			});
			tableInfos[(int)Table.Param] = new TableInfo(Table.Param, "Param", new ColumnInfo[] {
				new ColumnInfo("Flags", ColumnSize.UInt16),
				new ColumnInfo("Sequence", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
			});
			tableInfos[(int)Table.InterfaceImpl] = new TableInfo(Table.InterfaceImpl, "InterfaceImpl", new ColumnInfo[] {
				new ColumnInfo("Class", ColumnSize.TypeDef),
				new ColumnInfo("Interface", ColumnSize.TypeDefOrRef),
			});
			tableInfos[(int)Table.MemberRef] = new TableInfo(Table.MemberRef, "MemberRef", new ColumnInfo[] {
				new ColumnInfo("Class", ColumnSize.MemberRefParent),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.Constant] = new TableInfo(Table.Constant, "Constant", new ColumnInfo[] {
				new ColumnInfo("Type", ColumnSize.Byte),
				new ColumnInfo("Parent", ColumnSize.HasConstant),
				new ColumnInfo("Value", ColumnSize.Blob),
			});
			tableInfos[(int)Table.CustomAttribute] = new TableInfo(Table.CustomAttribute, "CustomAttribute", new ColumnInfo[] {
				new ColumnInfo("Parent", ColumnSize.HasCustomAttribute),
				new ColumnInfo("Type", ColumnSize.CustomAttributeType),
				new ColumnInfo("Value", ColumnSize.Blob),
			});
			tableInfos[(int)Table.FieldMarshal] = new TableInfo(Table.FieldMarshal, "FieldMarshal", new ColumnInfo[] {
				new ColumnInfo("Parent", ColumnSize.HasFieldMarshal),
				new ColumnInfo("NativeType", ColumnSize.Blob),
			});
			tableInfos[(int)Table.DeclSecurity] = new TableInfo(Table.DeclSecurity, "DeclSecurity", new ColumnInfo[] {
				new ColumnInfo("Action", ColumnSize.Int16),
				new ColumnInfo("Parent", ColumnSize.HasDeclSecurity),
				new ColumnInfo("PermissionSet", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ClassLayout] = new TableInfo(Table.ClassLayout, "ClassLayout", new ColumnInfo[] {
				new ColumnInfo("PackingSize", ColumnSize.UInt16),
				new ColumnInfo("ClassSize", ColumnSize.UInt32),
				new ColumnInfo("Parent", ColumnSize.TypeDef),
			});
			tableInfos[(int)Table.FieldLayout] = new TableInfo(Table.FieldLayout, "FieldLayout", new ColumnInfo[] {
				new ColumnInfo("OffSet", ColumnSize.UInt32),
				new ColumnInfo("Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.StandAloneSig] = new TableInfo(Table.StandAloneSig, "StandAloneSig", new ColumnInfo[] {
				new ColumnInfo("Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.EventMap] = new TableInfo(Table.EventMap, "EventMap", new ColumnInfo[] {
				new ColumnInfo("Parent", ColumnSize.TypeDef),
				new ColumnInfo("EventList", ColumnSize.Event),
			});
			tableInfos[(int)Table.EventPtr] = new TableInfo(Table.EventPtr, "EventPtr", new ColumnInfo[] {
				new ColumnInfo("Event", ColumnSize.Event),
			});
			tableInfos[(int)Table.Event] = new TableInfo(Table.Event, "Event", new ColumnInfo[] {
				new ColumnInfo("EventFlags", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("EventType", ColumnSize.TypeDefOrRef),
			});
			tableInfos[(int)Table.PropertyMap] = new TableInfo(Table.PropertyMap, "PropertyMap", new ColumnInfo[] {
				new ColumnInfo("Parent", ColumnSize.TypeDef),
				new ColumnInfo("PropertyList", ColumnSize.Property),
			});
			tableInfos[(int)Table.PropertyPtr] = new TableInfo(Table.PropertyPtr, "PropertyPtr", new ColumnInfo[] {
				new ColumnInfo("Property", ColumnSize.Property),
			});
			tableInfos[(int)Table.Property] = new TableInfo(Table.Property, "Property", new ColumnInfo[] {
				new ColumnInfo("PropFlags", ColumnSize.UInt16),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Type", ColumnSize.Blob),
			});
			tableInfos[(int)Table.MethodSemantics] = new TableInfo(Table.MethodSemantics, "MethodSemantics", new ColumnInfo[] {
				new ColumnInfo("Semantic", ColumnSize.UInt16),
				new ColumnInfo("Method", ColumnSize.Method),
				new ColumnInfo("Association", ColumnSize.HasSemantic),
			});
			tableInfos[(int)Table.MethodImpl] = new TableInfo(Table.MethodImpl, "MethodImpl", new ColumnInfo[] {
				new ColumnInfo("Class", ColumnSize.TypeDef),
				new ColumnInfo("MethodBody", ColumnSize.MethodDefOrRef),
				new ColumnInfo("MethodDeclaration", ColumnSize.MethodDefOrRef),
			});
			tableInfos[(int)Table.ModuleRef] = new TableInfo(Table.ModuleRef, "ModuleRef", new ColumnInfo[] {
				new ColumnInfo("Name", ColumnSize.Strings),
			});
			tableInfos[(int)Table.TypeSpec] = new TableInfo(Table.TypeSpec, "TypeSpec", new ColumnInfo[] {
				new ColumnInfo("Signature", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ImplMap] = new TableInfo(Table.ImplMap, "ImplMap", new ColumnInfo[] {
				new ColumnInfo("MappingFlags", ColumnSize.UInt16),
				new ColumnInfo("MemberForwarded", ColumnSize.MemberForwarded),
				new ColumnInfo("ImportName", ColumnSize.Strings),
				new ColumnInfo("ImportScope", ColumnSize.ModuleRef),
			});
			tableInfos[(int)Table.FieldRVA] = new TableInfo(Table.FieldRVA, "FieldRVA", new ColumnInfo[] {
				new ColumnInfo("RVA", ColumnSize.UInt32),
				new ColumnInfo("Field", ColumnSize.Field),
			});
			tableInfos[(int)Table.ENCLog] = new TableInfo(Table.ENCLog, "ENCLog", new ColumnInfo[] {
				new ColumnInfo("Token", ColumnSize.UInt32),
				new ColumnInfo("FuncCode", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.ENCMap] = new TableInfo(Table.ENCMap, "ENCMap", new ColumnInfo[] {
				new ColumnInfo("Token", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.Assembly] = new TableInfo(Table.Assembly, "Assembly", new ColumnInfo[] {
				new ColumnInfo("HashAlgId", ColumnSize.UInt32),
				new ColumnInfo("MajorVersion", ColumnSize.UInt16),
				new ColumnInfo("MinorVersion", ColumnSize.UInt16),
				new ColumnInfo("BuildNumber", ColumnSize.UInt16),
				new ColumnInfo("RevisionNumber", ColumnSize.UInt16),
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("PublicKey", ColumnSize.Blob),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Locale", ColumnSize.Strings),
			});
			tableInfos[(int)Table.AssemblyProcessor] = new TableInfo(Table.AssemblyProcessor, "AssemblyProcessor", new ColumnInfo[] {
				new ColumnInfo("Processor", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.AssemblyOS] = new TableInfo(Table.AssemblyOS, "AssemblyOS", new ColumnInfo[] {
				new ColumnInfo("OSPlatformId", ColumnSize.UInt32),
				new ColumnInfo("OSMajorVersion", ColumnSize.UInt32),
				new ColumnInfo("OSMinorVersion", ColumnSize.UInt32),
			});
			tableInfos[(int)Table.AssemblyRef] = new TableInfo(Table.AssemblyRef, "AssemblyRef", new ColumnInfo[] {
				new ColumnInfo("MajorVersion", ColumnSize.UInt16),
				new ColumnInfo("MinorVersion", ColumnSize.UInt16),
				new ColumnInfo("BuildNumber", ColumnSize.UInt16),
				new ColumnInfo("RevisionNumber", ColumnSize.UInt16),
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("PublicKeyOrToken", ColumnSize.Blob),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Locale", ColumnSize.Strings),
				new ColumnInfo("HashValue", ColumnSize.Blob),
			});
			tableInfos[(int)Table.AssemblyRefProcessor] = new TableInfo(Table.AssemblyRefProcessor, "AssemblyRefProcessor", new ColumnInfo[] {
				new ColumnInfo("Processor", ColumnSize.UInt32),
				new ColumnInfo("AssemblyRef", ColumnSize.AssemblyRef),
			});
			tableInfos[(int)Table.AssemblyRefOS] = new TableInfo(Table.AssemblyRefOS, "AssemblyRefOS", new ColumnInfo[] {
				new ColumnInfo("OSPlatformId", ColumnSize.UInt32),
				new ColumnInfo("OSMajorVersion", ColumnSize.UInt32),
				new ColumnInfo("OSMinorVersion", ColumnSize.UInt32),
				new ColumnInfo("AssemblyRef", ColumnSize.AssemblyRef),
			});
			tableInfos[(int)Table.File] = new TableInfo(Table.File, "File", new ColumnInfo[] {
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("HashValue", ColumnSize.Blob),
			});
			tableInfos[(int)Table.ExportedType] = new TableInfo(Table.ExportedType, "ExportedType", new ColumnInfo[] {
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("TypeDefId", ColumnSize.UInt32),
				new ColumnInfo("TypeName", ColumnSize.Strings),
				new ColumnInfo("TypeNamespace", ColumnSize.Strings),
				new ColumnInfo("Implementation", ColumnSize.Implementation),
			});
			tableInfos[(int)Table.ManifestResource] = new TableInfo(Table.ManifestResource, "ManifestResource", new ColumnInfo[] {
				new ColumnInfo("Offset", ColumnSize.UInt32),
				new ColumnInfo("Flags", ColumnSize.UInt32),
				new ColumnInfo("Name", ColumnSize.Strings),
				new ColumnInfo("Implementation", ColumnSize.Implementation),
			});
			tableInfos[(int)Table.NestedClass] = new TableInfo(Table.NestedClass, "NestedClass", new ColumnInfo[] {
				new ColumnInfo("NestedClass", ColumnSize.TypeDef),
				new ColumnInfo("EnclosingClass", ColumnSize.TypeDef),
			});
			if (majorVersion == 1 && minorVersion == 1) {
				tableInfos[(int)Table.GenericParam] = new TableInfo(Table.GenericParam, "GenericParam", new ColumnInfo[] {
					new ColumnInfo("Number", ColumnSize.UInt16),
					new ColumnInfo("Flags", ColumnSize.UInt16),
					new ColumnInfo("Owner", ColumnSize.TypeOrMethodDef),
					new ColumnInfo("Name", ColumnSize.Strings),
					new ColumnInfo("Kind", ColumnSize.TypeDefOrRef),
				});
			}
			else {
				tableInfos[(int)Table.GenericParam] = new TableInfo(Table.GenericParam, "GenericParam", new ColumnInfo[] {
					new ColumnInfo("Number", ColumnSize.UInt16),
					new ColumnInfo("Flags", ColumnSize.UInt16),
					new ColumnInfo("Owner", ColumnSize.TypeOrMethodDef),
					new ColumnInfo("Name", ColumnSize.Strings),
				});
			}
			tableInfos[(int)Table.MethodSpec] = new TableInfo(Table.MethodSpec, "MethodSpec", new ColumnInfo[] {
				new ColumnInfo("Method", ColumnSize.MethodDefOrRef),
				new ColumnInfo("Instantiation", ColumnSize.Blob),
			});
			tableInfos[(int)Table.GenericParamConstraint] = new TableInfo(Table.GenericParamConstraint, "GenericParamConstraint", new ColumnInfo[] {
				new ColumnInfo("Owner", ColumnSize.GenericParam),
				new ColumnInfo("Constraint", ColumnSize.TypeDefOrRef),
			});
			return this.tableInfos = tableInfos;
		}
	}
}
