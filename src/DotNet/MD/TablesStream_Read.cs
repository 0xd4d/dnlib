/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using dnlib.IO;

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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>TypeRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeRefRow ReadTypeRefRow(uint rid) {
			var table = TypeRefTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>TypeDef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeDefRow ReadTypeDefRow(uint rid) {
			var table = TypeDefTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>FieldPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldPtrRow ReadFieldPtrRow(uint rid) {
			var table = FieldPtrTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>MethodPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodPtrRow ReadMethodPtrRow(uint rid) {
			var table = MethodPtrTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ParamPtr</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamPtrRow ReadParamPtrRow(uint rid) {
			var table = ParamPtrTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>InterfaceImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawInterfaceImplRow ReadInterfaceImplRow(uint rid) {
			var table = InterfaceImplTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>MemberRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMemberRefRow ReadMemberRefRow(uint rid) {
			var table = MemberRefTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>Constant</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawConstantRow ReadConstantRow(uint rid) {
			var table = ConstantTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>CustomAttribute</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawCustomAttributeRow ReadCustomAttributeRow(uint rid) {
			var table = CustomAttributeTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ClassLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawClassLayoutRow ReadClassLayoutRow(uint rid) {
			var table = ClassLayoutTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>FieldLayout</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldLayoutRow ReadFieldLayoutRow(uint rid) {
			var table = FieldLayoutTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>StandAloneSig</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawStandAloneSigRow ReadStandAloneSigRow(uint rid) {
			var table = StandAloneSigTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>EventMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventMapRow ReadEventMapRow(uint rid) {
			var table = EventMapTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>PropertyMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyMapRow ReadPropertyMapRow(uint rid) {
			var table = PropertyMapTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>MethodSemantics</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSemanticsRow ReadMethodSemanticsRow(uint rid) {
			var table = MethodSemanticsTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>MethodImpl</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodImplRow ReadMethodImplRow(uint rid) {
			var table = MethodImplTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ModuleRef</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRefRow ReadModuleRefRow(uint rid) {
			var table = ModuleRefTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>TypeSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeSpecRow ReadTypeSpecRow(uint rid) {
			var table = TypeSpecTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ImplMap</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawImplMapRow ReadImplMapRow(uint rid) {
			var table = ImplMapTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>FieldRVA</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRVARow ReadFieldRVARow(uint rid) {
			var table = FieldRVATable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ENCLog</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCLogRow ReadENCLogRow(uint rid) {
			var table = ENCLogTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>AssemblyProcessor</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyProcessorRow ReadAssemblyProcessorRow(uint rid) {
			var table = AssemblyProcessorTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>AssemblyRefProcessor</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefProcessorRow ReadAssemblyRefProcessorRow(uint rid) {
			var table = AssemblyRefProcessorTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ExportedType</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawExportedTypeRow ReadExportedTypeRow(uint rid) {
			var table = ExportedTypeTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>ManifestResource</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawManifestResourceRow ReadManifestResourceRow(uint rid) {
			var table = ManifestResourceTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>NestedClass</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawNestedClassRow ReadNestedClassRow(uint rid) {
			var table = NestedClassTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>GenericParam</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamRow ReadGenericParamRow(uint rid) {
			var table = GenericParamTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>MethodSpec</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSpecRow ReadMethodSpecRow(uint rid) {
			var table = MethodSpecTable;
			if (table == null || table.IsInvalidRID(rid))
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
		/// Reads a raw <c>GenericParamConstraint</c> row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamConstraintRow ReadGenericParamConstraintRow(uint rid) {
			var table = GenericParamConstraintTable;
			if (table == null || table.IsInvalidRID(rid))
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
			if (table == null || table.IsInvalidRID(rid)) {
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
	}
}
