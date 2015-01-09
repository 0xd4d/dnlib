// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;

﻿namespace dnlib.DotNet.MD {
	public partial class TablesStream {
		IBinaryReader GetReader_NoLock(MDTable table, uint rid) {
			IBinaryReader reader;
			if (hotTableStream != null) {
				reader = hotTableStream.GetTableReader(table, rid);
				if (reader != null)
					return reader;
			}
			reader = table.ImageStream;
			reader.Position = (rid - 1) * table.TableInfo.RowSize;
			return reader;
		}

		/// <summary>
		/// Reads a raw <c>Module</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRow ReadModuleRow(uint rid) {
			var table = ModuleTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawModuleRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Module</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="generation"></param>
		/// <param name="name"></param>
		/// <param name="mvid"></param>
		/// <param name="encId"></param>
		/// <returns></returns>
		internal uint ReadModuleRow(uint rid, out ushort generation, out uint name, out uint mvid, out uint encId) {
			var table = ModuleTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			generation = reader.ReadUInt16();
			name = columns[1].Read(reader);
			mvid = columns[2].Read(reader);
			encId = columns[3].Read(reader);
			return columns[4].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeRefRow ReadTypeRefRow(uint rid) {
			var table = TypeRefTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawTypeRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="resolutionScope"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadTypeRefRow(uint rid, out uint resolutionScope, out uint name) {
			var table = TypeRefTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			resolutionScope = columns[0].Read(reader);
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeDef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeDefRow ReadTypeDefRow(uint rid) {
			var table = TypeDefTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawTypeDefRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeDef</c> row. Doesn't read field/method rid list.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="flags"></param>
		/// <param name="name"></param>
		/// <param name="namespace"></param>
		/// <returns></returns>
		internal uint ReadTypeDefRow(uint rid, out int flags, out uint name, out uint @namespace) {
			var table = TypeDefTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			flags = reader.ReadInt32();
			name = columns[1].Read(reader);
			@namespace = columns[2].Read(reader);
			return columns[3].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldPtrRow ReadFieldPtrRow(uint rid) {
			var table = FieldPtrTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFieldPtrRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Field</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRow ReadFieldRow(uint rid) {
			var table = FieldTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFieldRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Field</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="flags"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadFieldRow(uint rid, out int flags, out uint name) {
			var table = FieldTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			flags = reader.ReadUInt16();
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodPtrRow ReadMethodPtrRow(uint rid) {
			var table = MethodPtrTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMethodPtrRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Method</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodRow ReadMethodRow(uint rid) {
			var table = MethodTable;
			if (table.IsInvalidRID(rid))
				return null;
			var mrr = methodRowReader;
			if (mrr != null) {
				var row = mrr.ReadRow(rid);
				if (row != null)
					return row;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMethodRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Method</c> row but not ParamList
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="rva"></param>
		/// <param name="implFlags"></param>
		/// <param name="flags"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadMethodRow(uint rid, out RVA rva, out int implFlags, out int flags, out uint name) {
			var table = MethodTable;
			var mrr = methodRowReader;
			if (mrr != null) {
				var row = mrr.ReadRow(rid);
				if (row != null) {
					rva = (RVA)row.RVA;
					implFlags = row.ImplFlags;
					flags = row.Flags;
					name = row.Name;
					return row.Signature;
				}
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			rva = (RVA)reader.ReadUInt32();
			implFlags = reader.ReadUInt16();
			flags = reader.ReadUInt16();
			name = columns[3].Read(reader);
			return columns[4].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ParamPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamPtrRow ReadParamPtrRow(uint rid) {
			var table = ParamPtrTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawParamPtrRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Param</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamRow ReadParamRow(uint rid) {
			var table = ParamTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawParamRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Param</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="flags"></param>
		/// <param name="sequence"></param>
		/// <returns></returns>
		internal uint ReadParamRow(uint rid, out int flags, out ushort sequence) {
			var table = ParamTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			flags = reader.ReadUInt16();
			sequence = reader.ReadUInt16();
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>InterfaceImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawInterfaceImplRow ReadInterfaceImplRow(uint rid) {
			var table = InterfaceImplTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawInterfaceImplRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>InterfaceImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The <c>Interface</c> field</returns>
		internal uint ReadInterfaceImplRow2(uint rid) {
			var table = InterfaceImplTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			reader.Position += columns[0].Size;
			return columns[1].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MemberRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMemberRefRow ReadMemberRefRow(uint rid) {
			var table = MemberRefTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMemberRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MemberRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="class"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadMemberRefRow(uint rid, out uint @class, out uint name) {
			var table = MemberRefTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			@class = columns[0].Read(reader);
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Constant</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawConstantRow ReadConstantRow(uint rid) {
			var table = ConstantTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawConstantRow(reader.ReadByte(),
				reader.ReadByte(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Constant</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal uint ReadConstantRow(uint rid, out ElementType type) {
			var table = ConstantTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			type = (ElementType)reader.ReadByte();
			reader.Position += 1 + columns[1].Size;
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>CustomAttribute</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawCustomAttributeRow ReadCustomAttributeRow(uint rid) {
			var table = CustomAttributeTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawCustomAttributeRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldMarshal</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldMarshalRow ReadFieldMarshalRow(uint rid) {
			var table = FieldMarshalTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFieldMarshalRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>DeclSecurity</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawDeclSecurityRow ReadDeclSecurityRow(uint rid) {
			var table = DeclSecurityTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawDeclSecurityRow(reader.ReadInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>DeclSecurity</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="action"></param>
		/// <returns></returns>
		internal uint ReadDeclSecurityRow(uint rid, out SecurityAction action) {
			var table = DeclSecurityTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			action = (SecurityAction)reader.ReadInt16();
			reader.Position += columns[1].Size;
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ClassLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawClassLayoutRow ReadClassLayoutRow(uint rid) {
			var table = ClassLayoutTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawClassLayoutRow(reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ClassLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="packingSize"></param>
		/// <returns></returns>
		internal uint ReadClassLayoutRow(uint rid, out ushort packingSize) {
			var table = ClassLayoutTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			packingSize = reader.ReadUInt16();
			return reader.ReadUInt32();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldLayoutRow ReadFieldLayoutRow(uint rid) {
			var table = FieldLayoutTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFieldLayoutRow(reader.ReadUInt32(),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint? ReadFieldLayoutRow2(uint rid) {
			var table = FieldLayoutTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return reader.ReadUInt32();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>StandAloneSig</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawStandAloneSigRow ReadStandAloneSigRow(uint rid) {
			var table = StandAloneSigTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawStandAloneSigRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>StandAloneSig</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint ReadStandAloneSigRow2(uint rid) {
			var table = StandAloneSigTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return columns[0].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>EventMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventMapRow ReadEventMapRow(uint rid) {
			var table = EventMapTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawEventMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>EventPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventPtrRow ReadEventPtrRow(uint rid) {
			var table = EventPtrTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawEventPtrRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Event</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventRow ReadEventRow(uint rid) {
			var table = EventTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawEventRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Event</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="eventFlags"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadEventRow(uint rid, out int eventFlags, out uint name) {
			var table = EventTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			eventFlags = reader.ReadUInt16();
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>PropertyMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyMapRow ReadPropertyMapRow(uint rid) {
			var table = PropertyMapTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawPropertyMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>PropertyPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyPtrRow ReadPropertyPtrRow(uint rid) {
			var table = PropertyPtrTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawPropertyPtrRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Property</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyRow ReadPropertyRow(uint rid) {
			var table = PropertyTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawPropertyRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Property</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="propFlags"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadPropertyRow(uint rid, out int propFlags, out uint name) {
			var table = PropertyTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			propFlags = reader.ReadUInt16();
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSemantics</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSemanticsRow ReadMethodSemanticsRow(uint rid) {
			var table = MethodSemanticsTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMethodSemanticsRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSemantics</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="semantic"></param>
		/// <returns></returns>
		internal uint ReadMethodSemanticsRow(uint rid, out MethodSemanticsAttributes semantic) {
			var table = MethodSemanticsTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			semantic = (MethodSemanticsAttributes)reader.ReadUInt16();
			return columns[1].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodImplRow ReadMethodImplRow(uint rid) {
			var table = MethodImplTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMethodImplRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="methodBody"></param>
		/// <returns></returns>
		internal uint ReadMethodImplRow(uint rid, out uint methodBody) {
			var table = MethodImplTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			reader.Position += columns[0].Size;
			methodBody = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ModuleRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRefRow ReadModuleRefRow(uint rid) {
			var table = ModuleRefTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawModuleRefRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ModuleRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint ReadModuleRefRow2(uint rid) {
			var table = ModuleRefTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return columns[0].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeSpecRow ReadTypeSpecRow(uint rid) {
			var table = TypeSpecTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawTypeSpecRow(columns[0].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>TypeSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint ReadTypeSpecRow2(uint rid) {
			var table = TypeSpecTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return columns[0].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ImplMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawImplMapRow ReadImplMapRow(uint rid) {
			var table = ImplMapTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawImplMapRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ImplMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="attributes"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadImplMapRow(uint rid, out int attributes, out uint name) {
			var table = ImplMapTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			attributes = reader.ReadUInt16();
			reader.Position += columns[1].Size;
			name = columns[2].Read(reader);
			return columns[3].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldRVA</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRVARow ReadFieldRVARow(uint rid) {
			var table = FieldRVATable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFieldRVARow(reader.ReadUInt32(),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>FieldRVA</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="rva"></param>
		/// <returns></returns>
		internal bool ReadFieldRVARow(uint rid, out RVA rva) {
			var table = FieldRVATable;
			if (table.IsInvalidRID(rid)) {
				rva = 0;
				return false;
			}
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			rva = (RVA)reader.ReadUInt32();
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ENCLog</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCLogRow ReadENCLogRow(uint rid) {
			var table = ENCLogTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			return new RawENCLogRow(reader.ReadUInt32(),
				reader.ReadUInt32());
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ENCMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCMapRow ReadENCMapRow(uint rid) {
			var table = ENCMapTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			return new RawENCMapRow(reader.ReadUInt32());
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Assembly</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRow ReadAssemblyRow(uint rid) {
			var table = AssemblyTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawAssemblyRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>Assembly</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="hashAlgId"></param>
		/// <param name="version"></param>
		/// <param name="attributes"></param>
		/// <param name="publicKey"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadAssemblyRow(uint rid, out AssemblyHashAlgorithm hashAlgId, out Version version, out int attributes, out uint publicKey, out uint name) {
			var table = AssemblyTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			hashAlgId = (AssemblyHashAlgorithm)reader.ReadUInt32();
			version = new Version(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
			attributes = reader.ReadInt32();
			publicKey = columns[6].Read(reader);
			name = columns[7].Read(reader);
			return columns[8].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyProcessor</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyProcessorRow ReadAssemblyProcessorRow(uint rid) {
			var table = AssemblyProcessorTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			return new RawAssemblyProcessorRow(reader.ReadUInt32());
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyOS</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyOSRow ReadAssemblyOSRow(uint rid) {
			var table = AssemblyOSTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			return new RawAssemblyOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32());
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefRow ReadAssemblyRefRow(uint rid) {
			var table = AssemblyRefTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawAssemblyRefRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[5].Read(reader),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="version"></param>
		/// <param name="attributes"></param>
		/// <param name="publicKeyOrToken"></param>
		/// <param name="name"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		internal uint ReadAssemblyRefRow(uint rid, out Version version, out int attributes, out uint publicKeyOrToken, out uint name, out uint culture) {
			var table = AssemblyRefTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			version = new Version(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
			attributes = reader.ReadInt32();
			publicKeyOrToken = columns[5].Read(reader);
			name = columns[6].Read(reader);
			culture = columns[7].Read(reader);
			return columns[8].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRefProcessor</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefProcessorRow ReadAssemblyRefProcessorRow(uint rid) {
			var table = AssemblyRefProcessorTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawAssemblyRefProcessorRow(reader.ReadUInt32(),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>AssemblyRefOS</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefOSRow ReadAssemblyRefOSRow(uint rid) {
			var table = AssemblyRefOSTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawAssemblyRefOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[3].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>File</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFileRow ReadFileRow(uint rid) {
			var table = FileTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawFileRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>File</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="attributes"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadFileRow(uint rid, out int attributes, out uint name) {
			var table = FileTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			attributes = reader.ReadInt32();
			name = columns[1].Read(reader);
			return columns[2].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ExportedType</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawExportedTypeRow ReadExportedTypeRow(uint rid) {
			var table = ExportedTypeTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawExportedTypeRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ExportedType</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="attributes"></param>
		/// <param name="typeDefId"></param>
		/// <param name="name"></param>
		/// <param name="namespace"></param>
		/// <returns></returns>
		internal uint ReadExportedTypeRow(uint rid, out int attributes, out uint typeDefId, out uint name, out uint @namespace) {
			var table = ExportedTypeTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			attributes = reader.ReadInt32();
			typeDefId = reader.ReadUInt32();
			name = columns[2].Read(reader);
			@namespace = columns[3].Read(reader);
			return columns[4].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ManifestResource</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawManifestResourceRow ReadManifestResourceRow(uint rid) {
			var table = ManifestResourceTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawManifestResourceRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>ManifestResource</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="offset"></param>
		/// <param name="attributes"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadManifestResourceRow(uint rid, out uint offset, out int attributes, out uint name) {
			var table = ManifestResourceTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			offset = reader.ReadUInt32();
			attributes = reader.ReadInt32();
			name = columns[2].Read(reader);
			return columns[3].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>NestedClass</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawNestedClassRow ReadNestedClassRow(uint rid) {
			var table = NestedClassTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawNestedClassRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>NestedClass</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint ReadNestedClassRow2(uint rid) {
			var table = NestedClassTable;
			if (table.IsInvalidRID(rid))
				return 0;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			reader.Position += columns[0].Size;
			return columns[1].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParam</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamRow ReadGenericParamRow(uint rid) {
			var table = GenericParamTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			if (columns.Count == 4) {
				return new RawGenericParamRow(reader.ReadUInt16(),
					reader.ReadUInt16(),
					columns[2].Read(reader),
					columns[3].Read(reader));
			}
			return new RawGenericParamRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParam</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="number"></param>
		/// <param name="attributes"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal uint ReadGenericParamRow(uint rid, out ushort number, out int attributes, out uint name) {
			var table = GenericParamTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			number = reader.ReadUInt16();
			attributes = reader.ReadUInt16();
			reader.Position += columns[2].Size;
			name = columns[3].Read(reader);
			if (columns.Count == 4)
				return 0;
			return columns[4].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSpecRow ReadMethodSpecRow(uint rid) {
			var table = MethodSpecTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawMethodSpecRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>MethodSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="method"></param>
		/// <returns></returns>
		internal uint ReadMethodSpecRow(uint rid, out uint method) {
			var table = MethodSpecTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			method = columns[0].Read(reader);
			return columns[1].Read(reader);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParamConstraint</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamConstraintRow ReadGenericParamConstraintRow(uint rid) {
			var table = GenericParamConstraintTable;
			if (table.IsInvalidRID(rid))
				return null;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			return new RawGenericParamConstraintRow(columns[0].Read(reader),
				columns[1].Read(reader));
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads a raw <c>GenericParamConstraint</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns></returns>
		internal uint ReadGenericParamConstraintRow2(uint rid) {
			var table = GenericParamConstraintTable;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(table, rid);
			var columns = table.TableInfo.Columns;
			reader.Position += columns[0].Size;
			return columns[1].Read(reader);
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
		public bool ReadColumn(MDTable table, uint rid, int colIndex, out uint value) {
			return ReadColumn(table, rid, table.TableInfo.Columns[colIndex], out value);
		}

		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="rid">Row ID</param>
		/// <param name="column">Column</param>
		/// <param name="value">Result is put here or 0 if we return <c>false</c></param>
		/// <returns><c>true</c> if we could read the column, <c>false</c> otherwise</returns>
		public bool ReadColumn(MDTable table, uint rid, ColumnInfo column, out uint value) {
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
		internal bool ReadColumn_NoLock(MDTable table, uint rid, ColumnInfo column, out uint value) {
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
