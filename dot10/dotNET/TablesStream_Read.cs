namespace dot10.dotNET {
	public partial class TablesStream {
		/// <summary>
		/// Read a raw Module row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRow ReadModuleRow(uint rid) {
			var table = GetTable(Table.Module);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw TypeRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeRefRow ReadTypeRefRow(uint rid) {
			var table = GetTable(Table.TypeRef);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw TypeDef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeDefRow ReadTypeDefRow(uint rid) {
			var table = GetTable(Table.TypeDef);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw FieldPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldPtrRow ReadFieldPtrRow(uint rid) {
			var table = GetTable(Table.FieldPtr);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw Field row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRow ReadFieldRow(uint rid) {
			var table = GetTable(Table.Field);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw MethodPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodPtrRow ReadMethodPtrRow(uint rid) {
			var table = GetTable(Table.MethodPtr);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw Method row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodRow ReadMethodRow(uint rid) {
			var table = GetTable(Table.Method);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
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
		/// Read a raw ParamPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamPtrRow ReadParamPtrRow(uint rid) {
			var table = GetTable(Table.ParamPtr);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawParamPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw Param row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawParamRow ReadParamRow(uint rid) {
			var table = GetTable(Table.Param);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw InterfaceImpl row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawInterfaceImplRow ReadInterfaceImplRow(uint rid) {
			var table = GetTable(Table.InterfaceImpl);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawInterfaceImplRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw MemberRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMemberRefRow ReadMemberRefRow(uint rid) {
			var table = GetTable(Table.MemberRef);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw Constant row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawConstantRow ReadConstantRow(uint rid) {
			var table = GetTable(Table.Constant);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawConstantRow(reader.ReadByte(),
				reader.ReadByte(),
				columns[2].Read(reader),
				columns[3].Read(reader));
		}

		/// <summary>
		/// Read a raw CustomAttribute row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawCustomAttributeRow ReadCustomAttributeRow(uint rid) {
			var table = GetTable(Table.CustomAttribute);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw FieldMarshal row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldMarshalRow ReadFieldMarshalRow(uint rid) {
			var table = GetTable(Table.FieldMarshal);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldMarshalRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw DeclSecurity row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawDeclSecurityRow ReadDeclSecurityRow(uint rid) {
			var table = GetTable(Table.DeclSecurity);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw ClassLayout row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawClassLayoutRow ReadClassLayoutRow(uint rid) {
			var table = GetTable(Table.ClassLayout);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw FieldLayout row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldLayoutRow ReadFieldLayoutRow(uint rid) {
			var table = GetTable(Table.FieldLayout);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldLayoutRow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw StandAloneSig row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawStandAloneSigRow ReadStandAloneSigRow(uint rid) {
			var table = GetTable(Table.StandAloneSig);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawStandAloneSigRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw EventMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventMapRow ReadEventMapRow(uint rid) {
			var table = GetTable(Table.EventMap);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawEventMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw EventPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventPtrRow ReadEventPtrRow(uint rid) {
			var table = GetTable(Table.EventPtr);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawEventPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw Event row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawEventRow ReadEventRow(uint rid) {
			var table = GetTable(Table.Event);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw PropertyMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyMapRow ReadPropertyMapRow(uint rid) {
			var table = GetTable(Table.PropertyMap);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawPropertyMapRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw PropertyPtr row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyPtrRow ReadPropertyPtrRow(uint rid) {
			var table = GetTable(Table.PropertyPtr);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawPropertyPtrRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw Property row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawPropertyRow ReadPropertyRow(uint rid) {
			var table = GetTable(Table.Property);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw MethodSemantics row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSemanticsRow ReadMethodSemanticsRow(uint rid) {
			var table = GetTable(Table.MethodSemantics);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw MethodImpl row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodImplRow ReadMethodImplRow(uint rid) {
			var table = GetTable(Table.MethodImpl);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw ModuleRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawModuleRefRow ReadModuleRefRow(uint rid) {
			var table = GetTable(Table.ModuleRef);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawModuleRefRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw TypeSpec row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawTypeSpecRow ReadTypeSpecRow(uint rid) {
			var table = GetTable(Table.TypeSpec);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawTypeSpecRow(columns[0].Read(reader));
		}

		/// <summary>
		/// Read a raw ImplMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawImplMapRow ReadImplMapRow(uint rid) {
			var table = GetTable(Table.ImplMap);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw FieldRVA row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFieldRVARow ReadFieldRVARow(uint rid) {
			var table = GetTable(Table.FieldRVA);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawFieldRVARow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw ENCLog row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCLogRow ReadENCLogRow(uint rid) {
			var table = GetTable(Table.ENCLog);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawENCLogRow(reader.ReadUInt32(),
				reader.ReadUInt32());
		}

		/// <summary>
		/// Read a raw ENCMap row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawENCMapRow ReadENCMapRow(uint rid) {
			var table = GetTable(Table.ENCMap);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawENCMapRow(reader.ReadUInt32());
		}

		/// <summary>
		/// Read a raw Assembly row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRow ReadAssemblyRow(uint rid) {
			var table = GetTable(Table.Assembly);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw AssemblyProcessor row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyProcessorRow ReadAssemblyProcessorRow(uint rid) {
			var table = GetTable(Table.AssemblyProcessor);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyProcessorRow(reader.ReadUInt32());
		}

		/// <summary>
		/// Read a raw AssemblyOS row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyOSRow ReadAssemblyOSRow(uint rid) {
			var table = GetTable(Table.AssemblyOS);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyOSRow(reader.ReadUInt32(),
				reader.ReadUInt32(),
				reader.ReadUInt32());
		}

		/// <summary>
		/// Read a raw AssemblyRef row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefRow ReadAssemblyRefRow(uint rid) {
			var table = GetTable(Table.AssemblyRef);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw AssemblyRefProcessor row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefProcessorRow ReadAssemblyRefProcessorRow(uint rid) {
			var table = GetTable(Table.AssemblyRefProcessor);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawAssemblyRefProcessorRow(reader.ReadUInt32(),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw AssemblyRefOS row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawAssemblyRefOSRow ReadAssemblyRefOSRow(uint rid) {
			var table = GetTable(Table.AssemblyRefOS);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw File row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawFileRow ReadFileRow(uint rid) {
			var table = GetTable(Table.File);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw ExportedType row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawExportedTypeRow ReadExportedTypeRow(uint rid) {
			var table = GetTable(Table.ExportedType);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw ManifestResource row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawManifestResourceRow ReadManifestResourceRow(uint rid) {
			var table = GetTable(Table.ManifestResource);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw NestedClass row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawNestedClassRow ReadNestedClassRow(uint rid) {
			var table = GetTable(Table.NestedClass);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawNestedClassRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw GenericParam row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamRow ReadGenericParamRow(uint rid) {
			var table = GetTable(Table.GenericParam);
			if (table == null || rid == 0 || rid > table.Rows)
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
		/// Read a raw MethodSpec row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawMethodSpecRow ReadMethodSpecRow(uint rid) {
			var table = GetTable(Table.MethodSpec);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawMethodSpecRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}

		/// <summary>
		/// Read a raw GenericParamConstraint row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The row or null if table doesn't exist or if <paramref name="rid"/> is invalid</returns>
		public RawGenericParamConstraintRow ReadGenericParamConstraintRow(uint rid) {
			var table = GetTable(Table.GenericParamConstraint);
			if (table == null || rid == 0 || rid > table.Rows)
				return null;
			var reader = table.ImageStream;
			var tableInfo = table.TableInfo;
			var columns = tableInfo.Columns;
			reader.Position = (rid - 1) * tableInfo.RowSize;
			return new RawGenericParamConstraintRow(columns[0].Read(reader),
				columns[1].Read(reader));
		}
	}
}
