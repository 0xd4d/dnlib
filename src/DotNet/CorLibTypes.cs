// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
	/// <summary>
	/// Default implementation of <see cref="ICorLibTypes"/>
	/// </summary>
	public sealed class CorLibTypes : ICorLibTypes {
		readonly ModuleDef module;
		CorLibTypeSig typeVoid;
		CorLibTypeSig typeBoolean;
		CorLibTypeSig typeChar;
		CorLibTypeSig typeSByte;
		CorLibTypeSig typeByte;
		CorLibTypeSig typeInt16;
		CorLibTypeSig typeUInt16;
		CorLibTypeSig typeInt32;
		CorLibTypeSig typeUInt32;
		CorLibTypeSig typeInt64;
		CorLibTypeSig typeUInt64;
		CorLibTypeSig typeSingle;
		CorLibTypeSig typeDouble;
		CorLibTypeSig typeString;
		CorLibTypeSig typeTypedReference;
		CorLibTypeSig typeIntPtr;
		CorLibTypeSig typeUIntPtr;
		CorLibTypeSig typeObject;
		readonly AssemblyRef corLibAssemblyRef;

		/// <inheritdoc/>
		public CorLibTypeSig Void => typeVoid;

		/// <inheritdoc/>
		public CorLibTypeSig Boolean => typeBoolean;

		/// <inheritdoc/>
		public CorLibTypeSig Char => typeChar;

		/// <inheritdoc/>
		public CorLibTypeSig SByte => typeSByte;

		/// <inheritdoc/>
		public CorLibTypeSig Byte => typeByte;

		/// <inheritdoc/>
		public CorLibTypeSig Int16 => typeInt16;

		/// <inheritdoc/>
		public CorLibTypeSig UInt16 => typeUInt16;

		/// <inheritdoc/>
		public CorLibTypeSig Int32 => typeInt32;

		/// <inheritdoc/>
		public CorLibTypeSig UInt32 => typeUInt32;

		/// <inheritdoc/>
		public CorLibTypeSig Int64 => typeInt64;

		/// <inheritdoc/>
		public CorLibTypeSig UInt64 => typeUInt64;

		/// <inheritdoc/>
		public CorLibTypeSig Single => typeSingle;

		/// <inheritdoc/>
		public CorLibTypeSig Double => typeDouble;

		/// <inheritdoc/>
		public CorLibTypeSig String => typeString;

		/// <inheritdoc/>
		public CorLibTypeSig TypedReference => typeTypedReference;

		/// <inheritdoc/>
		public CorLibTypeSig IntPtr => typeIntPtr;

		/// <inheritdoc/>
		public CorLibTypeSig UIntPtr => typeUIntPtr;

		/// <inheritdoc/>
		public CorLibTypeSig Object => typeObject;

		/// <inheritdoc/>
		public AssemblyRef AssemblyRef => corLibAssemblyRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		public CorLibTypes(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		/// <param name="corLibAssemblyRef">Corlib assembly reference or <c>null</c> if a default
		/// assembly reference should be created</param>
		public CorLibTypes(ModuleDef module, AssemblyRef? corLibAssemblyRef) {
			this.module = module;
			this.corLibAssemblyRef = corLibAssemblyRef ?? CreateCorLibAssemblyRef();
			bool isCorLib = module.Assembly.IsCorLib();
			typeVoid	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Void"),		ElementType.Void);
			typeBoolean	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Boolean"),	ElementType.Boolean);
			typeChar	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Char"),		ElementType.Char);
			typeSByte	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "SByte"),		ElementType.I1);
			typeByte	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Byte"),		ElementType.U1);
			typeInt16	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int16"),		ElementType.I2);
			typeUInt16	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt16"),	ElementType.U2);
			typeInt32	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int32"),		ElementType.I4);
			typeUInt32	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt32"),	ElementType.U4);
			typeInt64	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int64"),		ElementType.I8);
			typeUInt64	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt64"),	ElementType.U8);
			typeSingle	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Single"),	ElementType.R4);
			typeDouble	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Double"),	ElementType.R8);
			typeString	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "String"),	ElementType.String);
			typeTypedReference = new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "TypedReference"), ElementType.TypedByRef);
			typeIntPtr	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "IntPtr"),	ElementType.I);
			typeUIntPtr	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UIntPtr"),	ElementType.U);
			typeObject	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Object"),	ElementType.Object);
		}

		AssemblyRef CreateCorLibAssemblyRef() => module.UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR20());

		ITypeDefOrRef CreateCorLibTypeRef(bool isCorLib, string name) {
			var tr = new TypeRefUser(module, "System", name, corLibAssemblyRef);
			if (isCorLib) {
				var td = module.Find(tr);
				if (td is not null)
					return td;
			}
			return module.UpdateRowId(tr);
		}

		/// <inheritdoc/>
		public TypeRef GetTypeRef(string @namespace, string name) => module.UpdateRowId(new TypeRefUser(module, @namespace, name, corLibAssemblyRef));
	}
}
