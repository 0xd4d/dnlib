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
		public CorLibTypeSig Void {
			get { return typeVoid; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Boolean {
			get { return typeBoolean; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Char {
			get { return typeChar; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig SByte {
			get { return typeSByte; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Byte {
			get { return typeByte; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int16 {
			get { return typeInt16; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt16 {
			get { return typeUInt16; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int32 {
			get { return typeInt32; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt32 {
			get { return typeUInt32; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int64 {
			get { return typeInt64; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt64 {
			get { return typeUInt64; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Single {
			get { return typeSingle; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Double {
			get { return typeDouble; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig String {
			get { return typeString; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig TypedReference {
			get { return typeTypedReference; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig IntPtr {
			get { return typeIntPtr; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UIntPtr {
			get { return typeUIntPtr; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Object {
			get { return typeObject; }
		}

		/// <inheritdoc/>
		public AssemblyRef AssemblyRef {
			get { return corLibAssemblyRef; }
		}

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
		public CorLibTypes(ModuleDef module, AssemblyRef corLibAssemblyRef) {
			this.module = module;
			this.corLibAssemblyRef = corLibAssemblyRef ?? CreateCorLibAssemblyRef();
			Initialize();
		}

		AssemblyRef CreateCorLibAssemblyRef() {
			return module.UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR20());
		}

		void Initialize() {
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

		ITypeDefOrRef CreateCorLibTypeRef(bool isCorLib, string name) {
			var tr = new TypeRefUser(module, "System", name, corLibAssemblyRef);
			if (isCorLib) {
				var td = module.Find(tr);
				if (td != null)
					return td;
			}
			return module.UpdateRowId(tr);
		}

		/// <inheritdoc/>
		public TypeRef GetTypeRef(string @namespace, string name) {
			return module.UpdateRowId(new TypeRefUser(module, @namespace, name, corLibAssemblyRef));
		}
	}
}
