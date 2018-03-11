// dnlib: See LICENSE.txt for more info

using dnlib.IO;

namespace dnlib.DotNet.MD {
	public partial class TablesStream {
		IBinaryReader GetReader_NoLock(MDTable table, uint rid) {
			var reader = table.ImageStream;
			reader.Position = (rid - 1) * table.TableInfo.RowSize;
			return reader;
		}

		/// <summary>
		/// Reads a raw <c>Module</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadModuleRow(uint rid, out RawModuleRow row) {
			var table = ModuleTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawModuleRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeRef</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadTypeRefRow(uint rid, out RawTypeRefRow row) {
			var table = TypeRefTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawTypeRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeDef</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadTypeDefRow(uint rid, out RawTypeDefRow row) {
			var table = TypeDefTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawTypeDefRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldPtr</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFieldPtrRow(uint rid, out RawFieldPtrRow row) {
			var table = FieldPtrTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFieldPtrRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Field</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFieldRow(uint rid, out RawFieldRow row) {
			var table = FieldTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFieldRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodPtr</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodPtrRow(uint rid, out RawMethodPtrRow row) {
			var table = MethodPtrTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodPtrRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Method</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodRow(uint rid, out RawMethodRow row) {
			var table = MethodTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
			var mrr = methodRowReader;
			if (mrr != null && mrr.TryReadRow(rid, out row))
				return true;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ParamPtr</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadParamPtrRow(uint rid, out RawParamPtrRow row) {
			var table = ParamPtrTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawParamPtrRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Param</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadParamRow(uint rid, out RawParamRow row) {
			var table = ParamTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawParamRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>InterfaceImpl</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadInterfaceImplRow(uint rid, out RawInterfaceImplRow row) {
			var table = InterfaceImplTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawInterfaceImplRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MemberRef</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMemberRefRow(uint rid, out RawMemberRefRow row) {
			var table = MemberRefTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMemberRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Constant</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadConstantRow(uint rid, out RawConstantRow row) {
			var table = ConstantTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawConstantRow(reader.ReadByte(),
				reader.ReadByte(),
				columns[2].Read(reader),
				columns[3].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>CustomAttribute</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadCustomAttributeRow(uint rid, out RawCustomAttributeRow row) {
			var table = CustomAttributeTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawCustomAttributeRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldMarshal</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFieldMarshalRow(uint rid, out RawFieldMarshalRow row) {
			var table = FieldMarshalTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFieldMarshalRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>DeclSecurity</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadDeclSecurityRow(uint rid, out RawDeclSecurityRow row) {
			var table = DeclSecurityTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawDeclSecurityRow(reader.ReadInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ClassLayout</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadClassLayoutRow(uint rid, out RawClassLayoutRow row) {
			var table = ClassLayoutTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawClassLayoutRow(reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldLayout</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFieldLayoutRow(uint rid, out RawFieldLayoutRow row) {
			var table = FieldLayoutTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFieldLayoutRow(reader.ReadUInt32(),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>StandAloneSig</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadStandAloneSigRow(uint rid, out RawStandAloneSigRow row) {
			var table = StandAloneSigTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawStandAloneSigRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>EventMap</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadEventMapRow(uint rid, out RawEventMapRow row) {
			var table = EventMapTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawEventMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>EventPtr</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadEventPtrRow(uint rid, out RawEventPtrRow row) {
			var table = EventPtrTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawEventPtrRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Event</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadEventRow(uint rid, out RawEventRow row) {
			var table = EventTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawEventRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>PropertyMap</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadPropertyMapRow(uint rid, out RawPropertyMapRow row) {
			var table = PropertyMapTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawPropertyMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>PropertyPtr</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadPropertyPtrRow(uint rid, out RawPropertyPtrRow row) {
			var table = PropertyPtrTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawPropertyPtrRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Property</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadPropertyRow(uint rid, out RawPropertyRow row) {
			var table = PropertyTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawPropertyRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSemantics</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodSemanticsRow(uint rid, out RawMethodSemanticsRow row) {
			var table = MethodSemanticsTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodSemanticsRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodImpl</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodImplRow(uint rid, out RawMethodImplRow row) {
			var table = MethodImplTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodImplRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ModuleRef</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadModuleRefRow(uint rid, out RawModuleRefRow row) {
			var table = ModuleRefTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawModuleRefRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeSpec</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadTypeSpecRow(uint rid, out RawTypeSpecRow row) {
			var table = TypeSpecTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawTypeSpecRow(columns[0].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ImplMap</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadImplMapRow(uint rid, out RawImplMapRow row) {
			var table = ImplMapTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawImplMapRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldRVA</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFieldRVARow(uint rid, out RawFieldRVARow row) {
			var table = FieldRVATable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFieldRVARow(reader.ReadUInt32(),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ENCLog</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadENCLogRow(uint rid, out RawENCLogRow row) {
			var table = ENCLogTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			row = new RawENCLogRow(reader.ReadUInt32(),
				reader.ReadUInt32());
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ENCMap</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadENCMapRow(uint rid, out RawENCMapRow row) {
			var table = ENCMapTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			row = new RawENCMapRow(reader.ReadUInt32());
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Assembly</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyRow(uint rid, out RawAssemblyRow row) {
			var table = AssemblyTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawAssemblyRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyProcessor</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyProcessorRow(uint rid, out RawAssemblyProcessorRow row) {
			var table = AssemblyProcessorTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			row = new RawAssemblyProcessorRow(reader.ReadUInt32());
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyOS</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyOSRow(uint rid, out RawAssemblyOSRow row) {
			var table = AssemblyOSTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			row = new RawAssemblyOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32());
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRef</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyRefRow(uint rid, out RawAssemblyRefRow row) {
			var table = AssemblyRefTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawAssemblyRefRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[5].Read(reader),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRefProcessor</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyRefProcessorRow(uint rid, out RawAssemblyRefProcessorRow row) {
			var table = AssemblyRefProcessorTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawAssemblyRefProcessorRow(reader.ReadUInt32(),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRefOS</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadAssemblyRefOSRow(uint rid, out RawAssemblyRefOSRow row) {
			var table = AssemblyRefOSTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawAssemblyRefOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[3].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>File</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadFileRow(uint rid, out RawFileRow row) {
			var table = FileTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawFileRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ExportedType</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadExportedTypeRow(uint rid, out RawExportedTypeRow row) {
			var table = ExportedTypeTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawExportedTypeRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ManifestResource</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadManifestResourceRow(uint rid, out RawManifestResourceRow row) {
			var table = ManifestResourceTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawManifestResourceRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>NestedClass</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadNestedClassRow(uint rid, out RawNestedClassRow row) {
			var table = NestedClassTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawNestedClassRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParam</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadGenericParamRow(uint rid, out RawGenericParamRow row) {
			var table = GenericParamTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			if (columns.Count == 4) {
				row = new RawGenericParamRow(reader.ReadUInt16(),
					reader.ReadUInt16(),
					columns[2].Read(reader),
					columns[3].Read(reader));
				return true;
			}
			else {
				row = new RawGenericParamRow(reader.ReadUInt16(),
					reader.ReadUInt16(),
					columns[2].Read(reader),
					columns[3].Read(reader),
					columns[4].Read(reader));
				return true;
			}
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSpec</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodSpecRow(uint rid, out RawMethodSpecRow row) {
			var table = MethodSpecTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodSpecRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParamConstraint</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadGenericParamConstraintRow(uint rid, out RawGenericParamConstraintRow row) {
			var table = GenericParamConstraintTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawGenericParamConstraintRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Document</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadDocumentRow(uint rid, out RawDocumentRow row) {
			var table = DocumentTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawDocumentRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodDebugInformation</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadMethodDebugInformationRow(uint rid, out RawMethodDebugInformationRow row) {
			var table = MethodDebugInformationTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawMethodDebugInformationRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>LocalScope</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadLocalScopeRow(uint rid, out RawLocalScopeRow row) {
			var table = LocalScopeTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawLocalScopeRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				reader.ReadUInt32(),
				reader.ReadUInt32());
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>LocalVariable</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadLocalVariableRow(uint rid, out RawLocalVariableRow row) {
			var table = LocalVariableTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawLocalVariableRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>LocalConstant</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadLocalConstantRow(uint rid, out RawLocalConstantRow row) {
			var table = LocalConstantTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawLocalConstantRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ImportScope</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadImportScopeRow(uint rid, out RawImportScopeRow row) {
			var table = ImportScopeTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawImportScopeRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>StateMachineMethod</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadStateMachineMethodRow(uint rid, out RawStateMachineMethodRow row) {
			var table = StateMachineMethodTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawStateMachineMethodRow(columns[0].Read(reader),
				columns[1].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>CustomDebugInformation</c> row or returns false if the row doesn't exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="row">Row data</param>
		/// <returns></returns>
		public bool TryReadCustomDebugInformationRow(uint rid, out RawCustomDebugInformationRow row) {
			var table = CustomDebugInformationTable;
			if (table.IsInvalidRID(rid)) {
				row = default;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			row = new RawCustomDebugInformationRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="rid">Row ID</param>
		/// <param name="colIndex">Column index in <paramref name="table"/></param>
		/// <param name="value">Result is put here or 0 if we return <c>false</c></param>
		/// <returns><c>true</c> if we could read the column, <c>false</c> otherwise</returns>
		public bool TryReadColumn(MDTable table, uint rid, int colIndex, out uint value) =>
			TryReadColumn(table, rid, table.TableInfo.Columns[colIndex], out value);

		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="rid">Row ID</param>
		/// <param name="column">Column</param>
		/// <param name="value">Result is put here or 0 if we return <c>false</c></param>
		/// <returns><c>true</c> if we could read the column, <c>false</c> otherwise</returns>
		public bool TryReadColumn(MDTable table, uint rid, ColumnInfo column, out uint value) {
			if (table.IsInvalidRID(rid)) {
				value = 0;
				return false;
			}
			var cr = columnReader;
			if (cr != null && cr.ReadColumn(table, rid, column, out value))
				return true;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			reader.Position += column.Offset;
			value = column.Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
			return true;
		}

		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="rid">Row ID</param>
		/// <param name="column">Column</param>
		/// <param name="value">Result is put here or 0 if we return <c>false</c></param>
		/// <returns><c>true</c> if we could read the column, <c>false</c> otherwise</returns>
		internal bool TryReadColumn_NoLock(MDTable table, uint rid, ColumnInfo column, out uint value) {
			if (table.IsInvalidRID(rid)) {
				value = 0;
				return false;
			}
			var cr = columnReader;
			if (cr != null && cr.ReadColumn(table, rid, column, out value))
				return true;
			var reader = GetReader_NoLock(table, rid);
			reader.Position += column.Offset;
			value = column.Read(reader);
			return true;
		}
	}
}
