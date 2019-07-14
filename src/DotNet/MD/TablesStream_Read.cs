// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.DotNet.MD {
	public partial class TablesStream {
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawModuleRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader),
				table.Column4.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawTypeRefRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawTypeDefRow(
				reader.Unsafe_ReadUInt32(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader),
				table.Column4.Unsafe_Read24(ref reader),
				table.Column5.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFieldPtrRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFieldRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodPtrRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			if (!(mrr is null) && mrr.TryReadRow(rid, out row))
				return true;
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				table.Column3.Unsafe_Read24(ref reader),
				table.Column4.Unsafe_Read24(ref reader),
				table.Column5.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawParamPtrRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawParamRow(
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawInterfaceImplRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMemberRefRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawConstantRow(
				reader.Unsafe_ReadByte(),
				reader.Unsafe_ReadByte(),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawCustomAttributeRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFieldMarshalRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawDeclSecurityRow(
				(short)reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawClassLayoutRow(
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt32(),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFieldLayoutRow(
				reader.Unsafe_ReadUInt32(),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawStandAloneSigRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawEventMapRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawEventPtrRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawEventRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawPropertyMapRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawPropertyPtrRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawPropertyRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodSemanticsRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodImplRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawModuleRefRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawTypeSpecRow(table.Column0.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawImplMapRow(
				reader.Unsafe_ReadUInt16(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFieldRVARow(
				reader.Unsafe_ReadUInt32(),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawENCLogRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32());
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawENCMapRow(reader.Unsafe_ReadUInt32());
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt32(),
				table.Column6.Unsafe_Read24(ref reader),
				table.Column7.Unsafe_Read24(ref reader),
				table.Column8.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyProcessorRow(reader.Unsafe_ReadUInt32());
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyOSRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32());
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyRefRow(
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt32(),
				table.Column5.Unsafe_Read24(ref reader),
				table.Column6.Unsafe_Read24(ref reader),
				table.Column7.Unsafe_Read24(ref reader),
				table.Column8.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyRefProcessorRow(
				reader.Unsafe_ReadUInt32(),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawAssemblyRefOSRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32(),
				table.Column3.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawFileRow(
				reader.Unsafe_ReadUInt32(),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawExportedTypeRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32(),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader),
				table.Column4.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawManifestResourceRow(
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32(),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawNestedClassRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			if (table.Column4 is null) {
				row = new RawGenericParamRow(
					reader.Unsafe_ReadUInt16(),
					reader.Unsafe_ReadUInt16(),
					table.Column2.Unsafe_Read24(ref reader),
					table.Column3.Unsafe_Read24(ref reader));
				return true;
			}
			else {
				row = new RawGenericParamRow(
					reader.Unsafe_ReadUInt16(),
					reader.Unsafe_ReadUInt16(),
					table.Column2.Unsafe_Read24(ref reader),
					table.Column3.Unsafe_Read24(ref reader),
					table.Column4.Unsafe_Read24(ref reader));
				return true;
			}
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodSpecRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawGenericParamConstraintRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawDocumentRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawMethodDebugInformationRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawLocalScopeRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader),
				table.Column3.Unsafe_Read24(ref reader),
				reader.Unsafe_ReadUInt32(),
				reader.Unsafe_ReadUInt32());
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawLocalVariableRow(
				reader.Unsafe_ReadUInt16(),
				reader.Unsafe_ReadUInt16(),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawLocalConstantRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawImportScopeRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawStateMachineMethodRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader));
			return true;
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
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize;
			row = new RawCustomDebugInformationRow(
				table.Column0.Unsafe_Read24(ref reader),
				table.Column1.Unsafe_Read24(ref reader),
				table.Column2.Unsafe_Read24(ref reader));
			return true;
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
			if (!(cr is null) && cr.ReadColumn(table, rid, column, out value))
				return true;
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize + (uint)column.Offset;
			value = column.Read(ref reader);
			return true;
		}

		internal bool TryReadColumn24(MDTable table, uint rid, int colIndex, out uint value) =>
			TryReadColumn24(table, rid, table.TableInfo.Columns[colIndex], out value);

		internal bool TryReadColumn24(MDTable table, uint rid, ColumnInfo column, out uint value) {
			Debug.Assert(column.Size == 2 || column.Size == 4);
			if (table.IsInvalidRID(rid)) {
				value = 0;
				return false;
			}
			var cr = columnReader;
			if (!(cr is null) && cr.ReadColumn(table, rid, column, out value))
				return true;
			var reader = table.DataReader;
			reader.Position = (rid - 1) * (uint)table.TableInfo.RowSize + (uint)column.Offset;
			value = column.Size == 2 ? reader.Unsafe_ReadUInt16() : reader.Unsafe_ReadUInt32();
			return true;
		}
	}
}
