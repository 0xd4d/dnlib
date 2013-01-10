/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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

﻿namespace dnlib.DotNet.MD {
	public partial class TablesStream {
		/// <summary>
		/// Reads a raw Module row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRow ReadModuleRow(uint rid) {
			var table = ModuleTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawModuleRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
		}

		/// <summary>
		/// Reads a raw TypeRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeRefRow ReadTypeRefRow(uint rid) {
			var table = TypeRefTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawTypeRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw TypeDef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeDefRow ReadTypeDefRow(uint rid) {
			var table = TypeDefTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawTypeDefRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
		}

		/// <summary>
		/// Reads a raw FieldPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldPtrRow ReadFieldPtrRow(uint rid) {
			var table = FieldPtrTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw Field row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRow ReadFieldRow(uint rid) {
			var table = FieldTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw MethodPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodPtrRow ReadMethodPtrRow(uint rid) {
			var table = MethodPtrTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw Method row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodRow ReadMethodRow(uint rid) {
			var table = MethodTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			if (methodRowReader != null) {
				var row = methodRowReader.ReadRow(rid);
				if (row != null)
					return row;
			}
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[3].Read(reader),
				columns[4].Read(reader),
				columns[5].Read(reader));
		}

		/// <summary>
		/// Reads a raw ParamPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamPtrRow ReadParamPtrRow(uint rid) {
			var table = ParamPtrTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawParamPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw Param row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamRow ReadParamRow(uint rid) {
			var table = ParamTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawParamRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw InterfaceImpl row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawInterfaceImplRow ReadInterfaceImplRow(uint rid) {
			var table = InterfaceImplTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawInterfaceImplRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw MemberRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMemberRefRow ReadMemberRefRow(uint rid) {
			var table = MemberRefTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMemberRefRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw Constant row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawConstantRow ReadConstantRow(uint rid) {
			var table = ConstantTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawConstantRow(reader.ReadByte(),
				reader.ReadByte(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw CustomAttribute row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawCustomAttributeRow ReadCustomAttributeRow(uint rid) {
			var table = CustomAttributeTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawCustomAttributeRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw FieldMarshal row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldMarshalRow ReadFieldMarshalRow(uint rid) {
			var table = FieldMarshalTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldMarshalRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw DeclSecurity row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawDeclSecurityRow ReadDeclSecurityRow(uint rid) {
			var table = DeclSecurityTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawDeclSecurityRow(reader.ReadInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw ClassLayout row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawClassLayoutRow ReadClassLayoutRow(uint rid) {
			var table = ClassLayoutTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawClassLayoutRow(reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw FieldLayout row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldLayoutRow ReadFieldLayoutRow(uint rid) {
			var table = FieldLayoutTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldLayoutRow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw StandAloneSig row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawStandAloneSigRow ReadStandAloneSigRow(uint rid) {
			var table = StandAloneSigTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawStandAloneSigRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw EventMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventMapRow ReadEventMapRow(uint rid) {
			var table = EventMapTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawEventMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw EventPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventPtrRow ReadEventPtrRow(uint rid) {
			var table = EventPtrTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawEventPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw Event row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventRow ReadEventRow(uint rid) {
			var table = EventTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawEventRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw PropertyMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyMapRow ReadPropertyMapRow(uint rid) {
			var table = PropertyMapTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawPropertyMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw PropertyPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyPtrRow ReadPropertyPtrRow(uint rid) {
			var table = PropertyPtrTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawPropertyPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw Property row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyRow ReadPropertyRow(uint rid) {
			var table = PropertyTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawPropertyRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw MethodSemantics row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSemanticsRow ReadMethodSemanticsRow(uint rid) {
			var table = MethodSemanticsTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodSemanticsRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw MethodImpl row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodImplRow ReadMethodImplRow(uint rid) {
			var table = MethodImplTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodImplRow(columns[0].Read(reader),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw ModuleRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRefRow ReadModuleRefRow(uint rid) {
			var table = ModuleRefTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawModuleRefRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw TypeSpec row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeSpecRow ReadTypeSpecRow(uint rid) {
			var table = TypeSpecTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawTypeSpecRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Reads a raw ImplMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawImplMapRow ReadImplMapRow(uint rid) {
			var table = ImplMapTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawImplMapRow(reader.ReadUInt16(),
				columns[1].Read(reader),
				columns[2].Read(reader),
				columns[3].Read(reader));
		}

		/// <summary>
		/// Reads a raw FieldRVA row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRVARow ReadFieldRVARow(uint rid) {
			var table = FieldRVATable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldRVARow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw ENCLog row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCLogRow ReadENCLogRow(uint rid) {
			var table = ENCLogTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawENCLogRow(reader.ReadUInt32(),
				reader.ReadUInt32());
		}

		/// <summary>
		/// Reads a raw ENCMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCMapRow ReadENCMapRow(uint rid) {
			var table = ENCMapTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawENCMapRow(reader.ReadUInt32());
		}

		/// <summary>
		/// Reads a raw Assembly row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRow ReadAssemblyRow(uint rid) {
			var table = AssemblyTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyRow(reader.ReadUInt32(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
		}

		/// <summary>
		/// Reads a raw AssemblyProcessor row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyProcessorRow ReadAssemblyProcessorRow(uint rid) {
			var table = AssemblyProcessorTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyProcessorRow(reader.ReadUInt32());
		}

		/// <summary>
		/// Reads a raw AssemblyOS row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyOSRow ReadAssemblyOSRow(uint rid) {
			var table = AssemblyOSTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32());
		}

		/// <summary>
		/// Reads a raw AssemblyRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefRow ReadAssemblyRefRow(uint rid) {
			var table = AssemblyRefTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyRefRow(reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt16(),
				reader.ReadUInt32(),
				columns[5].Read(reader),
				columns[6].Read(reader),
				columns[7].Read(reader),
				columns[8].Read(reader));
		}

		/// <summary>
		/// Reads a raw AssemblyRefProcessor row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefProcessorRow ReadAssemblyRefProcessorRow(uint rid) {
			var table = AssemblyRefProcessorTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyRefProcessorRow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw AssemblyRefOS row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefOSRow ReadAssemblyRefOSRow(uint rid) {
			var table = AssemblyRefOSTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyRefOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[3].Read(reader));
		}

		/// <summary>
		/// Reads a raw File row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFileRow ReadFileRow(uint rid) {
			var table = FileTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFileRow(reader.ReadUInt32(),
				columns[1].Read(reader),
				columns[2].Read(reader));
		}

		/// <summary>
		/// Reads a raw ExportedType row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawExportedTypeRow ReadExportedTypeRow(uint rid) {
			var table = ExportedTypeTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawExportedTypeRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader),
				columns[4].Read(reader));
		}

		/// <summary>
		/// Reads a raw ManifestResource row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawManifestResourceRow ReadManifestResourceRow(uint rid) {
			var table = ManifestResourceTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawManifestResourceRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				columns[2].Read(reader),
				columns[3].Read(reader));
		}

		/// <summary>
		/// Reads a raw NestedClass row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawNestedClassRow ReadNestedClassRow(uint rid) {
			var table = NestedClassTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawNestedClassRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw GenericParam row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamRow ReadGenericParamRow(uint rid) {
			var table = GenericParamTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
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
		}

		/// <summary>
		/// Reads a raw MethodSpec row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSpecRow ReadMethodSpecRow(uint rid) {
			var table = MethodSpecTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodSpecRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Reads a raw GenericParamConstraint row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or <c>null</c> if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamConstraintRow ReadGenericParamConstraintRow(uint rid) {
			var table = GenericParamConstraintTable;
			if (table == null || table.IsInvalidRID(rid))
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawGenericParamConstraintRow(columns[0].Read(reader),
				columns[1].Read(reader));
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
			if (columnReader != null && columnReader.ReadColumn(table, rid, column, out value))
				return true;
			var reader = table.ImageStream;
			reader.Position = (rid - 1) * table.TableInfo.RowSize + column.Offset;
			value = column.Read(reader);
			return true;
		}
	}
}
