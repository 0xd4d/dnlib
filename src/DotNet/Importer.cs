// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Reflection;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// <see cref="Importer"/> options
	/// </summary>
	[Flags]
	public enum ImporterOptions {
		/// <summary>
		/// Use <see cref="TypeDef"/>s whenever possible if the <see cref="TypeDef"/> is located
		/// in this module.
		/// </summary>
		TryToUseTypeDefs = 1,

		/// <summary>
		/// Use <see cref="MethodDef"/>s whenever possible if the <see cref="MethodDef"/> is located
		/// in this module.
		/// </summary>
		TryToUseMethodDefs = 2,

		/// <summary>
		/// Use <see cref="FieldDef"/>s whenever possible if the <see cref="FieldDef"/> is located
		/// in this module.
		/// </summary>
		TryToUseFieldDefs = 4,

		/// <summary>
		/// Use <see cref="TypeDef"/>s, <see cref="MethodDef"/>s and <see cref="FieldDef"/>s
		/// whenever possible if the definition is located in this module.
		/// </summary>
		/// <seealso cref="TryToUseTypeDefs"/>
		/// <seealso cref="TryToUseMethodDefs"/>
		/// <seealso cref="TryToUseFieldDefs"/>
		TryToUseDefs = TryToUseTypeDefs | TryToUseMethodDefs | TryToUseFieldDefs,

		/// <summary>
		/// Don't set this flag. For internal use only.
		/// </summary>
		FixSignature = int.MinValue,
	}

	/// <summary>
	/// Provides a chance to resolve definition when <see cref="Importer"/> encountered a definition to import.
	/// </summary>
	public class ImportResolver {
		/// <summary>
		/// Resolves the specified TypeDef.
		/// </summary>
		/// <param name="typeDef">The TypeDef.</param>
		/// <returns>The resolved TypeDef, or <c>null</c> if cannot be resolved.</returns>
		public virtual TypeDef Resolve(TypeDef typeDef) {
			return null;
		}

		/// <summary>
		/// Resolves the specified MethodDef.
		/// </summary>
		/// <param name="methodDef">The MethodDef.</param>
		/// <returns>The resolved MethodDef, or <c>null</c> if cannot be resolved.</returns>
		public virtual MethodDef Resolve(MethodDef methodDef) {
			return null;
		}

		/// <summary>
		/// Resolves the specified FieldDef.
		/// </summary>
		/// <param name="fieldDef">The FieldDef.</param>
		/// <returns>The resolved FieldDef, or <c>null</c> if cannot be resolved.</returns>
		public virtual FieldDef Resolve(FieldDef fieldDef) {
			return null;
		}
	}

	/// <summary>
	/// Imports <see cref="Type"/>s, <see cref="ConstructorInfo"/>s, <see cref="MethodInfo"/>s
	/// and <see cref="FieldInfo"/>s as references
	/// </summary>
	public struct Importer {
		readonly ModuleDef module;
		readonly GenericParamContext gpContext;
		RecursionCounter recursionCounter;
		ImporterOptions options;
		ImportResolver resolver;

		/// <summary>
		/// Gets/sets the <see cref="ImporterOptions.TryToUseTypeDefs"/> bit
		/// </summary>
		public bool TryToUseTypeDefs {
			get { return (options & ImporterOptions.TryToUseTypeDefs) != 0; }
			set {
				if (value)
					options |= ImporterOptions.TryToUseTypeDefs;
				else
					options &= ~ImporterOptions.TryToUseTypeDefs;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ImporterOptions.TryToUseMethodDefs"/> bit
		/// </summary>
		public bool TryToUseMethodDefs {
			get { return (options & ImporterOptions.TryToUseMethodDefs) != 0; }
			set {
				if (value)
					options |= ImporterOptions.TryToUseMethodDefs;
				else
					options &= ~ImporterOptions.TryToUseMethodDefs;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ImporterOptions.TryToUseFieldDefs"/> bit
		/// </summary>
		public bool TryToUseFieldDefs {
			get { return (options & ImporterOptions.TryToUseFieldDefs) != 0; }
			set {
				if (value)
					options |= ImporterOptions.TryToUseFieldDefs;
				else
					options &= ~ImporterOptions.TryToUseFieldDefs;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ImporterOptions.FixSignature"/> bit
		/// </summary>
		bool FixSignature {
			get { return (options & ImporterOptions.FixSignature) != 0; }
			set {
				if (value)
					options |= ImporterOptions.FixSignature;
				else
					options &= ~ImporterOptions.FixSignature;
			}
		}

		/// <summary>
		/// Gets or sets the resolver used to resolve definitions.
		/// </summary>
		/// <value>The resolver.</value>
		public ImportResolver Resolver {
			get { return resolver; }
			set { resolver = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		public Importer(ModuleDef module)
			: this(module, 0, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="gpContext">Generic parameter context</param>
		public Importer(ModuleDef module, GenericParamContext gpContext)
			: this(module, 0, gpContext) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="options">Importer options</param>
		public Importer(ModuleDef module, ImporterOptions options)
			: this(module, options, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="options">Importer options</param>
		/// <param name="gpContext">Generic parameter context</param>
		public Importer(ModuleDef module, ImporterOptions options, GenericParamContext gpContext) {
			this.module = module;
			this.recursionCounter = new RecursionCounter();
			this.options = options;
			this.gpContext = gpContext;
			this.resolver = null;
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef Import(Type type) {
			return module.UpdateRowId(ImportAsTypeSig(type).ToTypeDefOrRef());
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef Import(Type type, IList<Type> requiredModifiers, IList<Type> optionalModifiers) {
			return module.UpdateRowId(ImportAsTypeSig(type, requiredModifiers, optionalModifiers).ToTypeDefOrRef());
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public TypeSig ImportAsTypeSig(Type type) {
			return ImportAsTypeSig(type, false);
		}

		TypeSig ImportAsTypeSig(Type type, bool treatAsGenericInst) {
			if (type == null)
				return null;
			switch (treatAsGenericInst ? ElementType.GenericInst : type.GetElementType2()) {
			case ElementType.Void:		return module.CorLibTypes.Void;
			case ElementType.Boolean:	return module.CorLibTypes.Boolean;
			case ElementType.Char:		return module.CorLibTypes.Char;
			case ElementType.I1:		return module.CorLibTypes.SByte;
			case ElementType.U1:		return module.CorLibTypes.Byte;
			case ElementType.I2:		return module.CorLibTypes.Int16;
			case ElementType.U2:		return module.CorLibTypes.UInt16;
			case ElementType.I4:		return module.CorLibTypes.Int32;
			case ElementType.U4:		return module.CorLibTypes.UInt32;
			case ElementType.I8:		return module.CorLibTypes.Int64;
			case ElementType.U8:		return module.CorLibTypes.UInt64;
			case ElementType.R4:		return module.CorLibTypes.Single;
			case ElementType.R8:		return module.CorLibTypes.Double;
			case ElementType.String:	return module.CorLibTypes.String;
			case ElementType.TypedByRef:return module.CorLibTypes.TypedReference;
			case ElementType.U:			return module.CorLibTypes.UIntPtr;
			case ElementType.Object:	return module.CorLibTypes.Object;
			case ElementType.Ptr:		return new PtrSig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.ByRef:		return new ByRefSig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.SZArray:	return new SZArraySig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.ValueType: return new ValueTypeSig(CreateTypeRef(type));
			case ElementType.Class:		return new ClassSig(CreateTypeRef(type));
			case ElementType.Var:		return new GenericVar((uint)type.GenericParameterPosition, gpContext.Type);
			case ElementType.MVar:		return new GenericMVar((uint)type.GenericParameterPosition, gpContext.Method);

			case ElementType.I:
				FixSignature = true;	// FnPtr is mapped to System.IntPtr
				return module.CorLibTypes.IntPtr;

			case ElementType.Array:
				FixSignature = true;	// We don't know sizes and lower bounds
				return new ArraySig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst), (uint)type.GetArrayRank());

			case ElementType.GenericInst:
				var typeGenArgs = type.GetGenericArguments();
				var git = new GenericInstSig(ImportAsTypeSig(type.GetGenericTypeDefinition()) as ClassOrValueTypeSig, (uint)typeGenArgs.Length);
				foreach (var ga in typeGenArgs)
					git.GenericArguments.Add(ImportAsTypeSig(ga));
				return git;

			case ElementType.Sentinel:
			case ElementType.Pinned:
			case ElementType.FnPtr:		// mapped to System.IntPtr
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.End:
			default:
				return null;
			}
		}

		ITypeDefOrRef TryResolve(TypeRef tr) {
			if (!TryToUseTypeDefs || tr == null)
				return tr;
			if (!IsThisModule(tr))
				return tr;
			var td = tr.Resolve();
			if (td == null || td.Module != module)
				return tr;
			return td;
		}

		IMethodDefOrRef TryResolveMethod(IMethodDefOrRef mdr) {
			if (!TryToUseMethodDefs || mdr == null)
				return mdr;

			var mr = mdr as MemberRef;
			if (mr == null)
				return mdr;
			if (!mr.IsMethodRef)
				return mr;

			var declType = GetDeclaringType(mr);
			if (declType == null)
				return mr;
			if (declType.Module != module)
				return mr;
			return (IMethodDefOrRef)declType.ResolveMethod(mr) ?? mr;
		}

		IField TryResolveField(MemberRef mr) {
			if (!TryToUseFieldDefs || mr == null)
				return mr;

			if (!mr.IsFieldRef)
				return mr;

			var declType = GetDeclaringType(mr);
			if (declType == null)
				return mr;
			if (declType.Module != module)
				return mr;
			return (IField)declType.ResolveField(mr) ?? mr;
		}

		TypeDef GetDeclaringType(MemberRef mr) {
			if (mr == null)
				return null;

			var td = mr.Class as TypeDef;
			if (td != null)
				return td;

			td = TryResolve(mr.Class as TypeRef) as TypeDef;
			if (td != null)
				return td;

			var modRef = mr.Class as ModuleRef;
			if (IsThisModule(modRef))
				return module.GlobalType;

			return null;
		}

		bool IsThisModule(TypeRef tr) {
			if (tr == null)
				return false;
			var scopeType = tr.ScopeType.GetNonNestedTypeRefScope() as TypeRef;
			if (scopeType == null)
				return false;

			if (module == scopeType.ResolutionScope)
				return true;

			var modRef = scopeType.ResolutionScope as ModuleRef;
			if (modRef != null)
				return IsThisModule(modRef);

			var asmRef = scopeType.ResolutionScope as AssemblyRef;
			return Equals(module.Assembly, asmRef);
		}

		bool IsThisModule(ModuleRef modRef) {
			return modRef != null &&
				module.Name == modRef.Name &&
				Equals(module.Assembly, modRef.DefinitionAssembly);
		}

		static bool Equals(IAssembly a, IAssembly b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			return Utils.Equals(a.Version, b.Version) &&
				PublicKeyBase.TokenEquals(a.PublicKeyOrToken, b.PublicKeyOrToken) &&
				UTF8String.Equals(a.Name, b.Name) &&
				UTF8String.CaseInsensitiveEquals(a.Culture, b.Culture);
		}

		ITypeDefOrRef CreateTypeRef(Type type) {
			return TryResolve(CreateTypeRef2(type));
		}

		TypeRef CreateTypeRef2(Type type) {
			if (!type.IsNested)
				return module.UpdateRowId(new TypeRefUser(module, type.Namespace ?? string.Empty, type.Name ?? string.Empty, CreateScopeReference(type)));
			return module.UpdateRowId(new TypeRefUser(module, string.Empty, type.Name ?? string.Empty, CreateTypeRef2(type.DeclaringType)));
		}

		IResolutionScope CreateScopeReference(Type type) {
			if (type == null)
				return null;
			var asmName = type.Assembly.GetName();
			var modAsm = module.Assembly;
			if (modAsm != null) {
				if (UTF8String.ToSystemStringOrEmpty(modAsm.Name).Equals(asmName.Name, StringComparison.OrdinalIgnoreCase)) {
					if (UTF8String.ToSystemStringOrEmpty(module.Name).Equals(type.Module.ScopeName, StringComparison.OrdinalIgnoreCase))
						return module;
					return module.UpdateRowId(new ModuleRefUser(module, type.Module.ScopeName));
				}
			}
			var pkt = asmName.GetPublicKeyToken();
			if (pkt == null || pkt.Length == 0)
				pkt = null;

			AssemblyRef assemblyRef = new AssemblyRefUser(asmName.Name, asmName.Version, PublicKeyBase.CreatePublicKeyToken(pkt), asmName.CultureInfo.Name);
			if (assemblyRef.IsCorLib())
				return module.CorLibTypes.AssemblyRef;
			return module.UpdateRowId(assemblyRef);
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public TypeSig ImportAsTypeSig(Type type, IList<Type> requiredModifiers, IList<Type> optionalModifiers) {
			return ImportAsTypeSig(type, requiredModifiers, optionalModifiers, false);
		}

		TypeSig ImportAsTypeSig(Type type, IList<Type> requiredModifiers, IList<Type> optionalModifiers, bool treatAsGenericInst) {
			if (type == null)
				return null;
			if (IsEmpty(requiredModifiers) && IsEmpty(optionalModifiers))
				return ImportAsTypeSig(type, treatAsGenericInst);

			FixSignature = true;	// Order of modifiers is unknown
			var ts = ImportAsTypeSig(type, treatAsGenericInst);

			// We don't know the original order of the modifiers.
			// Assume all required modifiers are closer to the real type.
			// Assume all modifiers should be applied in the same order as in the lists.

			if (requiredModifiers != null) {
				foreach (var modifier in requiredModifiers.GetSafeEnumerable())
					ts = new CModReqdSig(Import(modifier), ts);
			}

			if (optionalModifiers != null) {
				foreach (var modifier in optionalModifiers.GetSafeEnumerable())
					ts = new CModOptSig(Import(modifier), ts);
			}

			return ts;
		}

		static bool IsEmpty<T>(IList<T> list) {
			return list == null || list.Count == 0;
		}

		/// <summary>
		/// Imports a <see cref="MethodBase"/> as a <see cref="IMethod"/>. This will be either
		/// a <see cref="MemberRef"/> or a <see cref="MethodSpec"/>.
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="methodBase"/> is invalid
		/// or if we failed to import the method</returns>
		public IMethod Import(MethodBase methodBase) {
			return Import(methodBase, false);
		}

		/// <summary>
		/// Imports a <see cref="MethodBase"/> as a <see cref="IMethod"/>. This will be either
		/// a <see cref="MemberRef"/> or a <see cref="MethodSpec"/>.
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <param name="forceFixSignature">Always verify method signature to make sure the
		/// returned reference matches the metadata in the source assembly</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="methodBase"/> is invalid
		/// or if we failed to import the method</returns>
		public IMethod Import(MethodBase methodBase, bool forceFixSignature) {
			FixSignature = false;
			return ImportInternal(methodBase, forceFixSignature);
		}

		IMethod ImportInternal(MethodBase methodBase) {
			return ImportInternal(methodBase, false);
		}

		IMethod ImportInternal(MethodBase methodBase, bool forceFixSignature) {
			if (methodBase == null)
				return null;

			if (forceFixSignature) {
				//TODO:
			}

			bool isMethodSpec = methodBase.IsGenericButNotGenericMethodDefinition();
			if (isMethodSpec) {
				IMethodDefOrRef method;
				var origMethod = methodBase.Module.ResolveMethod(methodBase.MetadataToken);
				if (methodBase.DeclaringType.GetElementType2() == ElementType.GenericInst)
					method = module.UpdateRowId(new MemberRefUser(module, methodBase.Name, CreateMethodSig(origMethod), Import(methodBase.DeclaringType)));
				else
					method = ImportInternal(origMethod) as IMethodDefOrRef;

				method = TryResolveMethod(method);

				var gim = CreateGenericInstMethodSig(methodBase);
				var methodSpec = module.UpdateRowId(new MethodSpecUser(method, gim));
				if (FixSignature && !forceFixSignature) {
					//TODO:
				}
				return methodSpec;
			}
			else {
				IMemberRefParent parent;
				if (methodBase.DeclaringType == null) {
					// It's the global type. We can reference it with a ModuleRef token.
					parent = GetModuleParent(methodBase.Module);
				}
				else
					parent = Import(methodBase.DeclaringType);
				if (parent == null)
					return null;

				MethodBase origMethod;
				try {
					// Get the original method def in case the declaring type is a generic
					// type instance and the method uses at least one generic type parameter.
					origMethod = methodBase.Module.ResolveMethod(methodBase.MetadataToken);
				}
				catch (ArgumentException) {
					// Here if eg. the method was created by the runtime (eg. a multi-dimensional
					// array getter/setter method). The method token is in that case 0x06000000,
					// which is invalid.
					origMethod = methodBase;
				}

				var methodSig = CreateMethodSig(origMethod);
				IMethodDefOrRef methodRef = module.UpdateRowId(new MemberRefUser(module, methodBase.Name, methodSig, parent));
				methodRef = TryResolveMethod(methodRef);
				if (FixSignature && !forceFixSignature) {
					//TODO:
				}
				return methodRef;
			}
		}

		MethodSig CreateMethodSig(MethodBase mb) {
			var sig = new MethodSig(GetCallingConvention(mb));

			var mi = mb as MethodInfo;
			if (mi != null)
				sig.RetType = ImportAsTypeSig(mi.ReturnParameter, mb.DeclaringType);
			else
				sig.RetType = module.CorLibTypes.Void;

			foreach (var p in mb.GetParameters())
				sig.Params.Add(ImportAsTypeSig(p, mb.DeclaringType));

			if (mb.IsGenericMethodDefinition)
				sig.GenParamCount = (uint)mb.GetGenericArguments().Length;

			return sig;
		}

		TypeSig ImportAsTypeSig(ParameterInfo p, Type declaringType) {
			return ImportAsTypeSig(p.ParameterType, p.GetRequiredCustomModifiers(), p.GetOptionalCustomModifiers(), declaringType.MustTreatTypeAsGenericInstType(p.ParameterType));
		}

		CallingConvention GetCallingConvention(MethodBase mb) {
			CallingConvention cc = 0;

			var mbcc = mb.CallingConvention;
			if (mb.IsGenericMethodDefinition)
				cc |= CallingConvention.Generic;
			if ((mbcc & CallingConventions.HasThis) != 0)
				cc |= CallingConvention.HasThis;
			if ((mbcc & CallingConventions.ExplicitThis) != 0)
				cc |= CallingConvention.ExplicitThis;

			switch (mbcc & CallingConventions.Any) {
			case CallingConventions.Standard:
				cc |= CallingConvention.Default;
				break;

			case CallingConventions.VarArgs:
				cc |= CallingConvention.VarArg;
				break;

			case CallingConventions.Any:
			default:
				FixSignature = true;
				cc |= CallingConvention.Default;
				break;
			}

			return cc;
		}

		GenericInstMethodSig CreateGenericInstMethodSig(MethodBase mb) {
			var genMethodArgs = mb.GetGenericArguments();
			var gim = new GenericInstMethodSig(CallingConvention.GenericInst, (uint)genMethodArgs.Length);
			foreach (var gma in genMethodArgs)
				gim.GenericArguments.Add(ImportAsTypeSig(gma));
			return gim;
		}

		IMemberRefParent GetModuleParent(Module module2) {
			// If we have no assembly, assume this is a netmodule in the same assembly as module
			var modAsm = module.Assembly;
			bool isSameAssembly = modAsm == null ||
				UTF8String.ToSystemStringOrEmpty(modAsm.Name).Equals(module2.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase);
			if (!isSameAssembly)
				return null;
			return module.UpdateRowId(new ModuleRefUser(module, module.Name));
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public IField Import(FieldInfo fieldInfo) {
			return Import(fieldInfo, false);
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <param name="forceFixSignature">Always verify field signature to make sure the
		/// returned reference matches the metadata in the source assembly</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public IField Import(FieldInfo fieldInfo, bool forceFixSignature) {
			FixSignature = false;
			if (fieldInfo == null)
				return null;

			if (forceFixSignature) {
				//TODO:
			}

			IMemberRefParent parent;
			if (fieldInfo.DeclaringType == null) {
				// It's the global type. We can reference it with a ModuleRef token.
				parent = GetModuleParent(fieldInfo.Module);
			}
			else
				parent = Import(fieldInfo.DeclaringType);
			if (parent == null)
				return null;

			FieldInfo origField;
			try {
				// Get the original field def in case the declaring type is a generic
				// type instance and the field uses a generic type parameter.
				origField = fieldInfo.Module.ResolveField(fieldInfo.MetadataToken);
			}
			catch (ArgumentException) {
				origField = fieldInfo;
			}

			MemberRef fieldRef;
			if (origField.FieldType.ContainsGenericParameters) {
				var origDeclType = origField.DeclaringType;
				var asm = module.Context.AssemblyResolver.Resolve(origDeclType.Module.Assembly.GetName(), module);
				if (asm == null || asm.FullName != origDeclType.Assembly.FullName)
					throw new Exception("Couldn't resolve the correct assembly");
				var mod = asm.FindModule(origDeclType.Module.Name) as ModuleDefMD;
				if (mod == null)
					throw new Exception("Couldn't resolve the correct module");
				var fieldDef = mod.ResolveField((uint)(origField.MetadataToken & 0x00FFFFFF));
				if (fieldDef == null)
					throw new Exception("Couldn't resolve the correct field");

				var fieldSig = new FieldSig(Import(fieldDef.FieldSig.GetFieldType()));
				fieldRef = module.UpdateRowId(new MemberRefUser(module, fieldInfo.Name, fieldSig, parent));
			}
			else {
				var fieldSig = new FieldSig(ImportAsTypeSig(fieldInfo.FieldType));
				fieldRef = module.UpdateRowId(new MemberRefUser(module, fieldInfo.Name, fieldSig, parent));
			}
			var field = TryResolveField(fieldRef);
			if (FixSignature && !forceFixSignature) {
				//TODO:
			}
			return field;
		}

		/// <summary>
		/// Imports a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public IType Import(IType type) {
			if (type == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IType result;
			TypeDef td;
			TypeRef tr;
			TypeSpec ts;
			TypeSig sig;

			if ((td = type as TypeDef) != null)
				result = Import(td);
			else if ((tr = type as TypeRef) != null)
				result = Import(tr);
			else if ((ts = type as TypeSpec) != null)
				result = Import(ts);
			else if ((sig = type as TypeSig) != null)
				result = Import(sig);
			else
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="TypeDef"/> as a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public ITypeDefOrRef Import(TypeDef type) {
			if (type == null)
				return null;
			if (TryToUseTypeDefs && type.Module == module)
				return type;

			if (resolver != null) {
				ITypeDefOrRef result = resolver.Resolve(type);
				if (result != null)
					return result;
			}

			return Import2(type);
		}

		TypeRef Import2(TypeDef type) {
			if (type == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			TypeRef result;

			var declType = type.DeclaringType;
			if (declType != null)
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, Import2(declType)));
			else
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, CreateScopeReference(type.DefinitionAssembly, type.Module)));

			recursionCounter.Decrement();
			return result;
		}

		IResolutionScope CreateScopeReference(IAssembly defAsm, ModuleDef defMod) {
			if (defAsm == null)
				return null;
			if (defAsm.IsCorLib())
				return module.CorLibTypes.AssemblyRef;

			var modAsm = module.Assembly;
			if (defMod != null && defAsm != null && modAsm != null) {
				if (UTF8String.CaseInsensitiveEquals(modAsm.Name, defAsm.Name)) {
					if (UTF8String.CaseInsensitiveEquals(module.Name, defMod.Name))
						return module;
					return module.UpdateRowId(new ModuleRefUser(module, defMod.Name));
				}
			}
			var pkt = PublicKeyBase.ToPublicKeyToken(defAsm.PublicKeyOrToken);
			if (PublicKeyBase.IsNullOrEmpty2(pkt))
				pkt = null;
			return module.UpdateRowId(new AssemblyRefUser(defAsm.Name, defAsm.Version, pkt, defAsm.Culture) { Attributes = defAsm.Attributes & ~AssemblyAttributes.PublicKey });
		}

		/// <summary>
		/// Imports a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public ITypeDefOrRef Import(TypeRef type) {
			return TryResolve(Import2(type));
		}

		TypeRef Import2(TypeRef type) {
			if (type == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			TypeRef result;

			var declaringType = type.DeclaringType;
			if (declaringType != null)
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, Import2(declaringType)));
			else
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, CreateScopeReference(type.DefinitionAssembly, type.Module)));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSpec Import(TypeSpec type) {
			if (type == null)
				return null;
			return module.UpdateRowId(new TypeSpecUser(Import(type.TypeSig)));
		}

		/// <summary>
		/// Imports a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSig Import(TypeSig type) {
			if (type == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			TypeSig result;
			switch (type.ElementType) {
			case ElementType.Void:		result = module.CorLibTypes.Void; break;
			case ElementType.Boolean:	result = module.CorLibTypes.Boolean; break;
			case ElementType.Char:		result = module.CorLibTypes.Char; break;
			case ElementType.I1:		result = module.CorLibTypes.SByte; break;
			case ElementType.U1:		result = module.CorLibTypes.Byte; break;
			case ElementType.I2:		result = module.CorLibTypes.Int16; break;
			case ElementType.U2:		result = module.CorLibTypes.UInt16; break;
			case ElementType.I4:		result = module.CorLibTypes.Int32; break;
			case ElementType.U4:		result = module.CorLibTypes.UInt32; break;
			case ElementType.I8:		result = module.CorLibTypes.Int64; break;
			case ElementType.U8:		result = module.CorLibTypes.UInt64; break;
			case ElementType.R4:		result = module.CorLibTypes.Single; break;
			case ElementType.R8:		result = module.CorLibTypes.Double; break;
			case ElementType.String:	result = module.CorLibTypes.String; break;
			case ElementType.TypedByRef:result = module.CorLibTypes.TypedReference; break;
			case ElementType.I:			result = module.CorLibTypes.IntPtr; break;
			case ElementType.U:			result = module.CorLibTypes.UIntPtr; break;
			case ElementType.Object:	result = module.CorLibTypes.Object; break;
			case ElementType.Ptr:		result = new PtrSig(Import(type.Next)); break;
			case ElementType.ByRef:		result = new ByRefSig(Import(type.Next)); break;
			case ElementType.ValueType: result = CreateClassOrValueType((type as ClassOrValueTypeSig).TypeDefOrRef, true); break;
			case ElementType.Class:		result = CreateClassOrValueType((type as ClassOrValueTypeSig).TypeDefOrRef, false); break;
			case ElementType.Var:		result = new GenericVar((type as GenericVar).Number, gpContext.Type); break;
			case ElementType.ValueArray:result = new ValueArraySig(Import(type.Next), (type as ValueArraySig).Size); break;
			case ElementType.FnPtr:		result = new FnPtrSig(Import((type as FnPtrSig).Signature)); break;
			case ElementType.SZArray:	result = new SZArraySig(Import(type.Next)); break;
			case ElementType.MVar:		result = new GenericMVar((type as GenericMVar).Number, gpContext.Method); break;
			case ElementType.CModReqd:	result = new CModReqdSig(Import((type as ModifierSig).Modifier), Import(type.Next)); break;
			case ElementType.CModOpt:	result = new CModOptSig(Import((type as ModifierSig).Modifier), Import(type.Next)); break;
			case ElementType.Module:	result = new ModuleSig((type as ModuleSig).Index, Import(type.Next)); break;
			case ElementType.Sentinel:	result = new SentinelSig(); break;
			case ElementType.Pinned:	result = new PinnedSig(Import(type.Next)); break;

			case ElementType.Array:
				var arraySig = (ArraySig)type;
				var sizes = new List<uint>(arraySig.Sizes);
				var lbounds = new List<int>(arraySig.LowerBounds);
				result = new ArraySig(Import(type.Next), arraySig.Rank, sizes, lbounds);
				break;

			case ElementType.GenericInst:
				var gis = (GenericInstSig)type;
				var genArgs = new List<TypeSig>(gis.GenericArguments.Count);
				foreach (var ga in gis.GenericArguments.GetSafeEnumerable())
					genArgs.Add(Import(ga));
				result = new GenericInstSig(Import(gis.GenericType) as ClassOrValueTypeSig, genArgs);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}

		ITypeDefOrRef Import(ITypeDefOrRef type) {
			return (ITypeDefOrRef)Import((IType)type);
		}

		TypeSig CreateClassOrValueType(ITypeDefOrRef type, bool isValueType) {
			var corLibType = module.CorLibTypes.GetCorLibTypeSig(type);
			if (corLibType != null)
				return corLibType;

			if (isValueType)
				return new ValueTypeSig(Import(type));
			return new ClassSig(Import(type));
		}

		/// <summary>
		/// Imports a <see cref="CallingConventionSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public CallingConventionSig Import(CallingConventionSig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			CallingConventionSig result;

			var sigType = sig.GetType();
			if (sigType == typeof(MethodSig))
				result = Import((MethodSig)sig);
			else if (sigType == typeof(FieldSig))
				result = Import((FieldSig)sig);
			else if (sigType == typeof(GenericInstMethodSig))
				result = Import((GenericInstMethodSig)sig);
			else if (sigType == typeof(PropertySig))
				result = Import((PropertySig)sig);
			else if (sigType == typeof(LocalSig))
				result = Import((LocalSig)sig);
			else
				result = null;	// Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public FieldSig Import(FieldSig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = new FieldSig(sig.GetCallingConvention(), Import(sig.Type));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public MethodSig Import(MethodSig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			MethodSig result = Import(new MethodSig(sig.GetCallingConvention()), sig);

			recursionCounter.Decrement();
			return result;
		}

		T Import<T>(T sig, T old) where T : MethodBaseSig {
			sig.RetType = Import(old.RetType);
			foreach (var p in old.Params.GetSafeEnumerable())
				sig.Params.Add(Import(p));
			sig.GenParamCount = old.GenParamCount;
			var paramsAfterSentinel = sig.ParamsAfterSentinel;
			if (paramsAfterSentinel != null) {
				foreach (var p in old.ParamsAfterSentinel.GetSafeEnumerable())
					paramsAfterSentinel.Add(Import(p));
			}
			return sig;
		}

		/// <summary>
		/// Imports a <see cref="PropertySig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public PropertySig Import(PropertySig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			PropertySig result = Import(new PropertySig(sig.GetCallingConvention()), sig);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="LocalSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public LocalSig Import(LocalSig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			LocalSig result = new LocalSig(sig.GetCallingConvention(), (uint)sig.Locals.Count);
			foreach (var l in sig.Locals.GetSafeEnumerable())
				result.Locals.Add(Import(l));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="GenericInstMethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public GenericInstMethodSig Import(GenericInstMethodSig sig) {
			if (sig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			GenericInstMethodSig result = new GenericInstMethodSig(sig.GetCallingConvention(), (uint)sig.GenericArguments.Count);
			foreach (var l in sig.GenericArguments.GetSafeEnumerable())
				result.GenericArguments.Add(Import(l));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public IField Import(IField field) {
			if (field == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IField result;
			MemberRef mr;
			FieldDef fd;

			if ((fd = field as FieldDef) != null)
				result = Import(fd);
			else if ((mr = field as MemberRef) != null)
				result = Import(mr);
			else
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public IMethod Import(IMethod method) {
			if (method == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IMethod result;
			MethodDef md;
			MethodSpec ms;
			MemberRef mr;

			if ((md = method as MethodDef) != null)
				result = Import(md);
			else if ((ms = method as MethodSpec) != null)
				result = Import(ms);
			else if ((mr = method as MemberRef) != null)
				result = Import(mr);
			else
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="FieldDef"/> as an <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public IField Import(FieldDef field) {
			if (field == null)
				return null;
			if (TryToUseFieldDefs && field.Module == module)
				return field;
			if (!recursionCounter.Increment())
				return null;

			if (resolver != null) {
				IField resultField = resolver.Resolve(field);
				if (resultField != null)
					return resultField;
			}

			MemberRef result = module.UpdateRowId(new MemberRefUser(module, field.Name));
			result.Signature = Import(field.Signature);
			result.Class = ImportParent(field.DeclaringType);

			recursionCounter.Decrement();
			return result;
		}

		IMemberRefParent ImportParent(TypeDef type) {
			if (type == null)
				return null;
			if (type.IsGlobalModuleType) {
				var om = type.Module;
				return module.UpdateRowId(new ModuleRefUser(module, om == null ? null : om.Name));
			}
			return Import(type);
		}

		/// <summary>
		/// Imports a <see cref="MethodDef"/> as an <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public IMethod Import(MethodDef method) {
			if (method == null)
				return null;
			if (TryToUseMethodDefs && method.Module == module)
				return method;
			if (!recursionCounter.Increment())
				return null;

			if (resolver != null) {
				IMethod resultMethod = resolver.Resolve(method);
				if (resultMethod != null)
					return resultMethod;
			}

			MemberRef result = module.UpdateRowId(new MemberRefUser(module, method.Name));
			result.Signature = Import(method.Signature);
			result.Class = ImportParent(method.DeclaringType);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public MethodSpec Import(MethodSpec method) {
			if (method == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			MethodSpec result = module.UpdateRowId(new MethodSpecUser((IMethodDefOrRef)Import(method.Method)));
			result.Instantiation = Import(method.Instantiation);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="memberRef">The member ref</param>
		/// <returns>The imported member ref or <c>null</c> if <paramref name="memberRef"/> is invalid</returns>
		public MemberRef Import(MemberRef memberRef) {
			if (memberRef == null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			MemberRef result = module.UpdateRowId(new MemberRefUser(module, memberRef.Name));
			result.Signature = Import(memberRef.Signature);
			result.Class = Import(memberRef.Class);
			if (result.Class == null)	// Will be null if memberRef.Class is null or a MethodDef
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		IMemberRefParent Import(IMemberRefParent parent) {
			var tdr = parent as ITypeDefOrRef;
			if (tdr != null) {
				var td = tdr as TypeDef;
				if (td != null && td.IsGlobalModuleType) {
					var om = td.Module;
					return module.UpdateRowId(new ModuleRefUser(module, om == null ? null : om.Name));
				}
				return Import(tdr);
			}

			var modRef = parent as ModuleRef;
			if (modRef != null)
				return module.UpdateRowId(new ModuleRefUser(module, modRef.Name));

			var method = parent as MethodDef;
			if (method != null) {
				var dt = method.DeclaringType;
				return dt == null || dt.Module != module ? null : method;
			}

			return null;
		}
	}
}
