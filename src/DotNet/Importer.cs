// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
		/// Use already existing <see cref="AssemblyRef"/>s whenever possible
		/// </summary>
		TryToUseExistingAssemblyRefs = 8,
	}

	/// <summary>
	/// Re-maps entities that were renamed in the target module
	/// </summary>
	public abstract class ImportMapper {
		/// <summary>
		/// Matches source <see cref="ITypeDefOrRef"/> to the one that is already present in the target module under a different name.
		/// </summary>
		/// <param name="source"><see cref="ITypeDefOrRef"/> referenced by the entity that is being imported.</param>
		/// <returns>matching <see cref="ITypeDefOrRef"/> or <c>null</c> if there's no match.</returns>
		public virtual ITypeDefOrRef? Map(ITypeDefOrRef source) => null;

		/// <summary>
		/// Matches source <see cref="FieldDef"/> to the one that is already present in the target module under a different name.
		/// </summary>
		/// <param name="source"><see cref="FieldDef"/> referenced by the entity that is being imported.</param>
		/// <returns>matching <see cref="IField"/> or <c>null</c> if there's no match.</returns>
		public virtual IField? Map(FieldDef source) => null;

		/// <summary>
		/// Matches source <see cref="MethodDef"/> to the one that is already present in the target module under a different name.
		/// </summary>
		/// <param name="source"><see cref="MethodDef"/> referenced by the entity that is being imported.</param>
		/// <returns>matching <see cref="IMethod"/> or <c>null</c> if there's no match.</returns>
		public virtual IMethod? Map(MethodDef source) => null;

		/// <summary>
		/// Matches source <see cref="MemberRef"/> to the one that is already present in the target module under a different name.
		/// </summary>
		/// <param name="source"><see cref="MemberRef"/> referenced by the entity that is being imported.</param>
		/// <returns>matching <see cref="MemberRef"/> or <c>null</c> if there's no match.</returns>
		public virtual MemberRef? Map(MemberRef source) => null;

		/// <summary>
		/// Overrides default behavior of <see cref="Importer.ImportOrNull(Type)"/>
		/// May be used to use reference assemblies for <see cref="Type"/> resolution, for example.
		/// </summary>
		/// <param name="source"><see cref="Type"/> to create <see cref="ITypeDefOrRef"/> for. <paramref name="source"/> is non-generic type or generic type without generic arguments.</param>
		/// <returns>A <see cref="TypeDef"/> or a <see cref="TypeRef"/> (not a <see cref="TypeSpec"/>), or null to use default <see cref="Importer"/>'s type resolution</returns>
		public virtual ITypeDefOrRef? Map(Type source) => null;
	}

	/// <summary>
	/// Imports <see cref="Type"/>s, <see cref="ConstructorInfo"/>s, <see cref="MethodInfo"/>s
	/// and <see cref="FieldInfo"/>s as references
	/// </summary>
	public struct Importer {
		readonly ModuleDef module;
		internal readonly GenericParamContext gpContext;
		readonly ImportMapper? mapper;
		RecursionCounter recursionCounter;
		readonly ImporterOptions options;
		// true if a generic type definition should be imported as a generic instance type
		// with its own generic parameters as generic arguments, see ImportAsOperand()
		bool treatGenericTypeDefsAsGenericInsts;

		bool TryToUseTypeDefs => (options & ImporterOptions.TryToUseTypeDefs) != 0;
		bool TryToUseMethodDefs => (options & ImporterOptions.TryToUseMethodDefs) != 0;
		bool TryToUseFieldDefs => (options & ImporterOptions.TryToUseFieldDefs) != 0;
		bool TryToUseExistingAssemblyRefs => (options & ImporterOptions.TryToUseExistingAssemblyRefs) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		public Importer(ModuleDef module)
			: this(module, 0, new GenericParamContext(), null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="gpContext">Generic parameter context</param>
		public Importer(ModuleDef module, GenericParamContext gpContext)
			: this(module, 0, gpContext, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="options">Importer options</param>
		public Importer(ModuleDef module, ImporterOptions options)
			: this(module, options, new GenericParamContext(), null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="options">Importer options</param>
		/// <param name="gpContext">Generic parameter context</param>
		public Importer(ModuleDef module, ImporterOptions options, GenericParamContext gpContext)
			: this(module, options, gpContext, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module that will own all references</param>
		/// <param name="options">Importer options</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="mapper">Mapper for renamed entities</param>
		public Importer(ModuleDef module, ImporterOptions options, GenericParamContext gpContext, ImportMapper? mapper) {
			this.module = module;
			recursionCounter = new RecursionCounter();
			this.options = options;
			this.gpContext = gpContext;
			this.mapper = mapper;
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef? ImportOrNull(Type? type) => module.UpdateRowId(ImportAsTypeSigOrNull(type).ToTypeDefOrRef());

#pragma warning disable RS0030 // Obsolete alias of the throwing Import(Type)
		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>. See also <see cref="Import(Type)"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns></returns>
		[Obsolete("Use '" + nameof(Import) + "(Type)' instead.")]
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? ImportDeclaringType(Type? type) => Import(type);
#pragma warning restore RS0030

		/// <summary>
		/// Imports a <see cref="Type"/> used as an instruction operand (eg. <c>newarr</c>, <c>castclass</c>, catch type),
		/// see <see cref="ImportAsOperandOrNull(MethodBase)"/>.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef? ImportAsOperandOrNull(Type? type) {
			bool old = treatGenericTypeDefsAsGenericInsts;
			treatGenericTypeDefsAsGenericInsts = true;
			try {
				return ImportOrNull(type);
			}
			finally {
				treatGenericTypeDefsAsGenericInsts = old;
			}
		}

		/// <summary>
		/// Imports a <see cref="MethodBase"/> used as an instruction operand (eg. <c>call</c>, <c>newobj</c>, <c>ldftn</c>).
		/// <para>
		/// Reflection returns the same <see cref="Type"/> for a generic type definition (eg. <c>My&lt;&gt;</c>) and
		/// for the generic type instantiated with its own generic parameters (eg. <c>My&lt;T&gt;</c> inside <c>My&lt;T&gt;</c>).
		/// <see cref="ImportOrNull(MethodBase)"/> imports it as the generic type definition, but an instruction operand can't
		/// reference a member of an uninstantiated generic type, so this method imports all generic type definitions
		/// as generic instance types instantiated with their own generic parameters, eg. <c>My`1&lt;!0&gt;</c>.
		/// </para>
		/// <para>
		/// <c>ldtoken</c> is the only instruction that can reference both of them (eg. <c>typeof(My&lt;&gt;)</c> and
		/// <c>typeof(My&lt;T&gt;)</c>). This method returns the generic instance type which is the most common one. Use
		/// <see cref="ImportOrNull(MethodBase)"/> if it's the generic type definition.
		/// </para>
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="methodBase"/> is invalid
		/// or if we failed to import the method</returns>
		public IMethod? ImportAsOperandOrNull(MethodBase? methodBase) {
			bool old = treatGenericTypeDefsAsGenericInsts;
			treatGenericTypeDefsAsGenericInsts = true;
			try {
				return ImportOrNull(methodBase);
			}
			finally {
				treatGenericTypeDefsAsGenericInsts = old;
			}
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> used as an instruction operand (eg. <c>ldfld</c>, <c>stsfld</c>),
		/// see <see cref="ImportAsOperandOrNull(MethodBase)"/>.
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public IField? ImportAsOperandOrNull(FieldInfo? fieldInfo) {
			bool old = treatGenericTypeDefsAsGenericInsts;
			treatGenericTypeDefsAsGenericInsts = true;
			try {
				return ImportOrNull(fieldInfo);
			}
			finally {
				treatGenericTypeDefsAsGenericInsts = old;
			}
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef? ImportOrNull(Type? type, IList<Type>? requiredModifiers, IList<Type>? optionalModifiers) =>
			module.UpdateRowId(ImportAsTypeSigOrNull(type, requiredModifiers, optionalModifiers).ToTypeDefOrRef());

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public TypeSig? ImportAsTypeSigOrNull(Type? type) => ImportAsTypeSigOrNull(type, null, null);

		/// <param name="type">The type</param>
		/// <param name="declaringType">Declaring type of the member or null</param>
		/// <param name="treatAsGenericInst">See <see cref="ReflectionExtensions.MustTreatTypeAsGenericInstType(Type, Type)"/></param>
		/// <param name="modifiedType">The .NET 8+ modified type of <paramref name="type"/> or null. It's only used
		/// to get function pointer calling conventions and modifiers.</param>
		TypeSig? ImportAsTypeSigOrNull(Type? type, Type? declaringType, bool? treatAsGenericInst = null, Type? modifiedType = null) {
			if (type is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			TypeSig? result;
			bool treatAsGenericInst2 = treatAsGenericInst ??
				((treatGenericTypeDefsAsGenericInsts && type.IsGenericTypeDefinition) || declaringType.MustTreatTypeAsGenericInstType(type));
			switch (treatAsGenericInst2 ? ElementType.GenericInst : type.GetElementType2()) {
			case ElementType.Void: result = module.CorLibTypes.Void; break;
			case ElementType.Boolean: result = module.CorLibTypes.Boolean; break;
			case ElementType.Char: result = module.CorLibTypes.Char; break;
			case ElementType.I1: result = module.CorLibTypes.SByte; break;
			case ElementType.U1: result = module.CorLibTypes.Byte; break;
			case ElementType.I2: result = module.CorLibTypes.Int16; break;
			case ElementType.U2: result = module.CorLibTypes.UInt16; break;
			case ElementType.I4: result = module.CorLibTypes.Int32; break;
			case ElementType.U4: result = module.CorLibTypes.UInt32; break;
			case ElementType.I8: result = module.CorLibTypes.Int64; break;
			case ElementType.U8: result = module.CorLibTypes.UInt64; break;
			case ElementType.R4: result = module.CorLibTypes.Single; break;
			case ElementType.R8: result = module.CorLibTypes.Double; break;
			case ElementType.String: result = module.CorLibTypes.String; break;
			case ElementType.TypedByRef: result = module.CorLibTypes.TypedReference; break;
			case ElementType.U: result = module.CorLibTypes.UIntPtr; break;
			case ElementType.Object: result = module.CorLibTypes.Object; break;
			case ElementType.Ptr: result = new PtrSig(ImportAsTypeSigOrNull(type.GetElementType(), declaringType, null, modifiedType?.GetElementType())); break;
			case ElementType.ByRef: result = new ByRefSig(ImportAsTypeSigOrNull(type.GetElementType(), declaringType, null, modifiedType?.GetElementType())); break;
			case ElementType.SZArray: result = new SZArraySig(ImportAsTypeSigOrNull(type.GetElementType(), declaringType, null, modifiedType?.GetElementType())); break;
			case ElementType.FnPtr: result = ImportFnPtr(type, declaringType, modifiedType); break;
			case ElementType.ValueType: result = new ValueTypeSig(CreateTypeDefOrRef(type)); break;
			case ElementType.Class: result = new ClassSig(CreateTypeDefOrRef(type)); break;
			case ElementType.Var: result = new GenericVar((uint)type.GenericParameterPosition, gpContext.Type); break;
			case ElementType.MVar: result = new GenericMVar((uint)type.GenericParameterPosition, gpContext.Method); break;

			case ElementType.I:
				// Could be a FnPtr, older runtimes (before .NET 8) map them to System.IntPtr
				result = module.CorLibTypes.IntPtr;
				break;

			case ElementType.Array:
				// We don't know sizes and lower bounds. Assume it's `0..`
				var lowerBounds = new int[type.GetArrayRank()];
				var sizes = Array2.Empty<uint>();
				result = new ArraySig(ImportAsTypeSigOrNull(type.GetElementType(), declaringType, null, modifiedType?.GetElementType()), (uint)type.GetArrayRank(), sizes, lowerBounds);
				break;

			case ElementType.GenericInst:
				var typeGenArgs = type.GetGenericArguments();
				var git = new GenericInstSig(ImportAsTypeSigOrNull(type.GetGenericTypeDefinition(), null, false) as ClassOrValueTypeSig, (uint)typeGenArgs.Length);
				foreach (var ga in typeGenArgs)
					git.GenericArguments.Add(ImportAsTypeSigOrNull(ga, declaringType));
				result = git;
				break;

			case ElementType.Sentinel:
			case ElementType.Pinned:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.Internal:
			case ElementType.CModInternal:
			case ElementType.Module:
			case ElementType.End:
			default:
				result = null;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}

		FnPtrSig ImportFnPtr(Type type, Type? declaringType, Type? modifiedType) {
			// Only modified types (.NET 8+) have calling conventions and modifiers
			if (modifiedType is not null && !modifiedType.IsFunctionPointer2())
				modifiedType = null;
			var retType = type.GetFunctionPointerReturnType2();
			var paramTypes = type.GetFunctionPointerParameterTypes2();
			var modRetType = modifiedType?.GetFunctionPointerReturnType2();
			var modParamTypes = modifiedType?.GetFunctionPointerParameterTypes2();
			if (modParamTypes is not null && modParamTypes.Length != paramTypes.Length) {
				modifiedType = null;
				modRetType = null;
				modParamTypes = null;
			}
			// Reflection doesn't have the other calling convention bits (HasThis, ExplicitThis, Generic),
			// can't tell Default from VarArg and needs a modified type to get the unmanaged calling convention.
			var cc = ReflectionExtensions.GetFunctionPointerCallingConvention(type, modifiedType, out _);
			var sig = new MethodSig(cc ?? (type.IsUnmanagedFunctionPointer2() ? CallingConvention.Unmanaged : CallingConvention.Default));
			sig.RetType = ImportFnPtrPart(retType, modRetType, declaringType);
			for (int i = 0; i < paramTypes.Length; i++)
				sig.Params.Add(ImportFnPtrPart(paramTypes[i], modParamTypes?[i], declaringType));
			return new FnPtrSig(sig);
		}

		TypeSig? ImportFnPtrPart(Type? type, Type? modifiedType, Type? declaringType) {
			if (modifiedType is null)
				return ImportAsTypeSigOrNull(type, declaringType);
			var ts = ImportAsTypeSigOrNull(type, declaringType, null, modifiedType);
			var requiredModifiers = modifiedType.GetRequiredCustomModifiers2();
			var optionalModifiers = modifiedType.GetOptionalCustomModifiers2();
			// Order of modifiers is unknown, use the same order as ImportAsTypeSigOrNull(Type, IList<Type>, IList<Type>, Type, Type)
			foreach (var modifier in requiredModifiers)
				ts = new CModReqdSig(ImportOrNull(modifier), ts);
			foreach (var modifier in optionalModifiers)
				ts = new CModOptSig(ImportOrNull(modifier), ts);
			return ts;
		}

		ITypeDefOrRef? TryResolve(TypeRef? tr) {
			if (!TryToUseTypeDefs || tr is null)
				return tr;
			if (!IsThisModule(tr))
				return tr;
			var td = tr.Resolve();
			if (td is null || td.Module != module)
				return tr;
			return td;
		}

		IMethodDefOrRef? TryResolveMethod(IMethodDefOrRef? mdr) {
			if (!TryToUseMethodDefs || mdr is null)
				return mdr;

			var mr = mdr as MemberRef;
			if (mr is null)
				return mdr;
			if (!mr.IsMethodRef)
				return mr;

			var declType = GetDeclaringType(mr);
			if (declType is null)
				return mr;
			if (declType.Module != module)
				return mr;
			return (IMethodDefOrRef?)declType.ResolveMethod(mr) ?? mr;
		}

		IField? TryResolveField(MemberRef? mr) {
			if (!TryToUseFieldDefs || mr is null)
				return mr;

			if (!mr.IsFieldRef)
				return mr;

			var declType = GetDeclaringType(mr);
			if (declType is null)
				return mr;
			if (declType.Module != module)
				return mr;
			return (IField?)declType.ResolveField(mr) ?? mr;
		}

		TypeDef? GetDeclaringType(MemberRef? mr) {
			if (mr is null)
				return null;

			if (mr.Class is TypeDef td)
				return td;

			if (TryResolve(mr.Class as TypeRef) is TypeDef resolvedTypeDef)
				return resolvedTypeDef;

			var modRef = mr.Class as ModuleRef;
			if (IsThisModule(modRef))
				return module.GlobalType;

			return null;
		}

		bool IsThisModule(TypeRef? tr) {
			if (tr is null)
				return false;
			var scopeType = tr.GetNonNestedTypeRefScope() as TypeRef;
			if (scopeType is null)
				return false;

			if (module == scopeType.ResolutionScope)
				return true;

			if (scopeType.ResolutionScope is ModuleRef modRef)
				return IsThisModule(modRef);

			var asmRef = scopeType.ResolutionScope as AssemblyRef;
			return Equals(module.Assembly, asmRef);
		}

		bool IsThisModule(ModuleRef? modRef) =>
			modRef is not null &&
			module.Name == modRef.Name &&
			Equals(module.Assembly, modRef.DefinitionAssembly);

		static bool Equals(IAssembly? a, IAssembly? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			return Utils.Equals(a.Version, b.Version) &&
				PublicKeyBase.TokenEquals(a.PublicKeyOrToken, b.PublicKeyOrToken) &&
				UTF8String.Equals(a.Name, b.Name) &&
				UTF8String.CaseInsensitiveEquals(a.Culture, b.Culture);
		}

		ITypeDefOrRef? CreateTypeDefOrRef(Type type) {
			var tdr = mapper?.Map(type);
			if (tdr is TypeSpec)
				throw new InvalidOperationException($"{nameof(ImportMapper)}.{nameof(ImportMapper.Map)}({nameof(Type)}) must return a {nameof(TypeDef)} or a {nameof(TypeRef)}, not a {nameof(TypeSpec)}: {type}");
			if (tdr is TypeDef td)
				return td;
			if (tdr is TypeRef tr)
				return TryResolve(tr);

			if (TryToUseTypeDefs && IsThisModule(type.Module) && module.ResolveToken(type.MetadataToken) is TypeDef def)
				return def;

			return TryResolve(CreateTypeRef(type));
		}

		TypeRef? CreateTypeRef(Type type) {
			if (!type.IsNested)
				return module.UpdateRowId(new TypeRefUser(module, type.Namespace ?? string.Empty, ReflectionExtensions.Unescape(type.Name) ?? string.Empty, CreateScopeReference(type)));
			if (!recursionCounter.Increment())
				return null;
			type.GetTypeNamespaceAndName_TypeDefOrRef(out var @namespace, out var name);
			var result = module.UpdateRowId(new TypeRefUser(module, @namespace ?? string.Empty, name ?? string.Empty, CreateTypeRef(type.DeclaringType!)));
			recursionCounter.Decrement();
			return result;
		}

		IResolutionScope? CreateScopeReference(Type? type) {
			if (type is null)
				return null;
			var asmName = type.Assembly.GetName();
			var modAsm = module.Assembly;
			if (modAsm is not null) {
				if (UTF8String.ToSystemStringOrEmpty(modAsm.Name).Equals(asmName.Name, StringComparison.OrdinalIgnoreCase)) {
					if (UTF8String.ToSystemStringOrEmpty(module.Name).Equals(type.Module.ScopeName, StringComparison.OrdinalIgnoreCase))
						return module;
					return module.UpdateRowId(new ModuleRefUser(module, type.Module.ScopeName));
				}
			}
			var pkt = asmName.GetPublicKeyToken();
			if (pkt is null || pkt.Length == 0)
				pkt = null;
			if (TryToUseExistingAssemblyRefs && module.GetAssemblyRef(asmName.Name!) is AssemblyRef asmRef)
				return asmRef;
			return module.UpdateRowId(new AssemblyRefUser(asmName.Name!, asmName.Version!, PublicKeyBase.CreatePublicKeyToken(pkt), asmName.CultureInfo?.Name ?? string.Empty) {
				ContentType = AssemblyNameInfo.GetContentType(asmName),
			});
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public TypeSig? ImportAsTypeSigOrNull(Type? type, IList<Type>? requiredModifiers, IList<Type>? optionalModifiers) =>
			ImportAsTypeSigOrNull(type, requiredModifiers, optionalModifiers, null);

		TypeSig? ImportAsTypeSigOrNull(Type? type, IList<Type>? requiredModifiers, IList<Type>? optionalModifiers, Type? declaringType, Type? modifiedType = null) {
			if (type is null)
				return null;
			if (IsEmpty(requiredModifiers) && IsEmpty(optionalModifiers))
				return ImportAsTypeSigOrNull(type, declaringType, null, modifiedType);

			var ts = ImportAsTypeSigOrNull(type, declaringType, null, modifiedType);

			// We don't know the original order of the modifiers.
			// Assume all required modifiers are closer to the real type.
			// Assume all modifiers should be applied in the same order as in the lists.

			if (requiredModifiers is not null) {
				foreach (var modifier in requiredModifiers)
					ts = new CModReqdSig(ImportOrNull(modifier), ts);
			}

			if (optionalModifiers is not null) {
				foreach (var modifier in optionalModifiers)
					ts = new CModOptSig(ImportOrNull(modifier), ts);
			}

			return ts;
		}

		static bool IsEmpty<T>(IList<T>? list) => list is null || list.Count == 0;

		/// <summary>
		/// Imports a <see cref="MethodBase"/> as a <see cref="IMethod"/>. This will be either
		/// a <see cref="MemberRef"/> or a <see cref="MethodSpec"/>.
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="methodBase"/> is invalid
		/// or if we failed to import the method</returns>
		public IMethod? ImportOrNull(MethodBase? methodBase) {
			if (methodBase is null)
				return null;

			if (TryToUseMethodDefs && IsThisModule(methodBase.Module) &&
				!methodBase.IsGenericMethod && (methodBase.DeclaringType is null || !methodBase.DeclaringType.IsGenericType) &&
				module.ResolveToken(methodBase.MetadataToken) is MethodDef md) {
				// In same module and method and declaring type are both non-generic, directly resolve method definition.
				// Obfuscator may rename many methods into same name then TryResolveMethod will return inconsistent method.
				return md;
			}

			bool isMethodSpec = methodBase.IsGenericButNotGenericMethodDefinition();
			if (isMethodSpec) {
				IMethodDefOrRef? method;
				var origMethod = methodBase.ResolveMethodDefinition();
				// DeclaringType is null if it's a generic method in the global type (<Module>)
				if (methodBase.DeclaringType is not null && methodBase.DeclaringType.GetElementType2() == ElementType.GenericInst)
					method = module.UpdateRowId(new MemberRefUser(module, methodBase.Name, CreateMethodSig(origMethod), ImportOrNull(methodBase.DeclaringType)));
				else
					method = ImportOrNull(origMethod) as IMethodDefOrRef;

				method = TryResolveMethod(method);
				if (method is null)
					return null;
				var gim = CreateGenericInstMethodSig(methodBase);
				return module.UpdateRowId(new MethodSpecUser(method, gim));
			}
			else {
				IMemberRefParent? parent;
				if (methodBase.DeclaringType is null) {
					// It's the global type. We can reference it with a ModuleRef token.
					parent = GetModuleParent(methodBase.Module);
				}
				else
					parent = ImportOrNull(methodBase.DeclaringType);
				if (parent is null)
					return null;

				MethodBase? origMethod;
				// Methods created by the runtime (eg. multi-dimensional array methods) have token 0x06000000.
				// Check it first since ResolveMethod() would throw, and exceptions are slow.
				if (HasNilRid(methodBase.MetadataToken))
					origMethod = methodBase;
				else {
					try {
						// Get the original method def in case the declaring type is a generic
						// type instance and the method uses at least one generic type parameter.
						origMethod = methodBase.ResolveMethodDefinition();
					}
					catch (ArgumentException) {
						origMethod = methodBase;
					}
				}

				var methodSig = CreateMethodSig(origMethod);
				IMethodDefOrRef? methodRef = module.UpdateRowId(new MemberRefUser(module, methodBase.Name, methodSig, parent));
				return TryResolveMethod(methodRef);
			}
		}

		static bool HasNilRid(int token) => (token & 0x00FFFFFF) == 0;

		bool IsThisModule(Module module2) => UTF8String.ToSystemStringOrEmpty(module.Name).Equals(module2.ScopeName, StringComparison.OrdinalIgnoreCase) && IsThisAssembly(module2);

		MethodSig? CreateMethodSig(MethodBase mb) {
			var sig = new MethodSig(GetCallingConvention(mb));

			if (mb is MethodInfo mi)
				sig.RetType = ImportAsTypeSigOrNull(mi.ReturnParameter, mb.DeclaringType);
			else
				sig.RetType = module.CorLibTypes.Void;

			foreach (var p in mb.GetParameters())
				sig.Params.Add(ImportAsTypeSigOrNull(p, mb.DeclaringType));

			if (mb.IsGenericMethodDefinition)
				sig.GenParamCount = (uint)mb.GetGenericArguments().Length;

			return sig;
		}

		TypeSig? ImportAsTypeSigOrNull(ParameterInfo p, Type? declaringType) {
			var type = p.ParameterType;
			var modifiedType = type.ContainsFunctionPointer() ? p.GetModifiedParameterType2() : null;
			return ImportAsTypeSigOrNull(type, p.GetRequiredCustomModifiers(), p.GetOptionalCustomModifiers(), declaringType, modifiedType);
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
				cc |= CallingConvention.Default;
				break;
			}

			return cc;
		}

		GenericInstMethodSig? CreateGenericInstMethodSig(MethodBase mb) {
			var genMethodArgs = mb.GetGenericArguments();
			var gim = new GenericInstMethodSig(CallingConvention.GenericInst, (uint)genMethodArgs.Length);
			foreach (var gma in genMethodArgs)
				gim.GenericArguments.Add(ImportAsTypeSigOrNull(gma));
			return gim;
		}

		IMemberRefParent? GetModuleParent(Module module2) {
			if (!IsThisAssembly(module2))
				return null;
			return module.UpdateRowId(new ModuleRefUser(module, module2.ScopeName));
		}

		bool IsThisAssembly(Module module2) {
			// If we have no assembly, assume this is a netmodule in the same assembly as module
			var modAsm = module.Assembly;
			return modAsm is null || UTF8String.ToSystemStringOrEmpty(modAsm.Name).Equals(module2.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public IField? ImportOrNull(FieldInfo? fieldInfo) {
			if (fieldInfo is null)
				return null;

			if (TryToUseFieldDefs && IsThisModule(fieldInfo.Module) &&
				(fieldInfo.DeclaringType is null || !fieldInfo.DeclaringType.IsGenericType) &&
				module.ResolveToken(fieldInfo.MetadataToken) is FieldDef fd) {
				// In same module and declaring type is non-generic, directly resolve field definition.
				// Obfuscator may rename many fields into same name then TryResolveField will return inconsistent field.
				return fd;
			}

			IMemberRefParent? parent;
			if (fieldInfo.DeclaringType is null) {
				// It's the global type. We can reference it with a ModuleRef token.
				parent = GetModuleParent(fieldInfo.Module);
			}
			else
				parent = ImportOrNull(fieldInfo.DeclaringType);
			if (parent is null)
				return null;

			FieldInfo? origField;
			// Check it first since ResolveField() would throw, and exceptions are slow
			if (HasNilRid(fieldInfo.MetadataToken))
				origField = fieldInfo;
			else {
				try {
					// Get the original field def in case the declaring type is a generic
					// type instance and the field uses a generic type parameter.
					origField = fieldInfo.ResolveFieldDefinition();
				}
				catch (ArgumentException) {
					origField = fieldInfo;
				}
			}

			var fieldType = origField.FieldType;
			var modifiedFieldType = fieldType.ContainsFunctionPointer() ? origField.GetModifiedFieldType2() : null;
			var fieldSig = new FieldSig(ImportAsTypeSigOrNull(fieldType,
				origField.GetRequiredCustomModifiers(), origField.GetOptionalCustomModifiers(), origField.DeclaringType, modifiedFieldType));
			var fieldRef = module.UpdateRowId(new MemberRefUser(module, fieldInfo.Name, fieldSig, parent));

			return TryResolveField(fieldRef);
		}

		/// <summary>
		/// Imports a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public IType? ImportOrNull(IType? type) {
			if (type is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IType? result;
			TypeDef? td;
			TypeRef? tr;
			TypeSpec? ts;
			TypeSig? sig;

			if ((td = type as TypeDef) is not null)
				result = ImportOrNull(td);
			else if ((tr = type as TypeRef) is not null)
				result = ImportOrNull(tr);
			else if ((ts = type as TypeSpec) is not null)
				result = ImportOrNull(ts);
			else if ((sig = type as TypeSig) is not null)
				result = ImportOrNull(sig);
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
		public ITypeDefOrRef? ImportOrNull(TypeDef? type) {
			if (type is null)
				return null;
			if (TryToUseTypeDefs && type.Module == module)
				return type;
			var mapped = mapper?.Map(type);
			if (mapped is not null)
				return mapped;
			return Import2(type);
		}

		TypeRef? Import2(TypeDef? type) {
			if (type is null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			TypeRef? result;

			var declType = type.DeclaringType;
			if (declType is not null)
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, Import2(declType)));
			else
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, CreateScopeReference(type.DefinitionAssembly, type.Module?.Name)));

			recursionCounter.Decrement();
			return result;
		}

		/// <param name="defAsm">The assembly that has the type</param>
		/// <param name="defModName">Name of the module that has the type or <c>null</c> if it's the manifest module
		/// (the type is referenced by an <see cref="AssemblyRef"/>)</param>
		IResolutionScope? CreateScopeReference(IAssembly? defAsm, UTF8String? defModName) {
			if (defAsm is null)
				return null;
			var modAsm = module.Assembly;
			if (modAsm is not null && UTF8String.CaseInsensitiveEquals(modAsm.Name, defAsm.Name)) {
				defModName ??= modAsm.ManifestModule?.Name;
				if (defModName is not null) {
					if (UTF8String.CaseInsensitiveEquals(module.Name, defModName))
						return module;
					return module.UpdateRowId(new ModuleRefUser(module, defModName));
				}
			}
			var pkt = PublicKeyBase.ToPublicKeyToken(defAsm.PublicKeyOrToken);
			if (PublicKeyBase.IsNullOrEmpty2(pkt))
				pkt = null;
			if (TryToUseExistingAssemblyRefs && module.GetAssemblyRef(defAsm.Name) is AssemblyRef asmRef)
				return asmRef;
			return module.UpdateRowId(new AssemblyRefUser(defAsm.Name, defAsm.Version!, pkt, defAsm.Culture) { Attributes = defAsm.Attributes & ~AssemblyAttributes.PublicKey });
		}

		/// <summary>
		/// Imports a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public ITypeDefOrRef? ImportOrNull(TypeRef? type) {
			if (type is null)
				return null;
			var mapped = mapper?.Map(type);
			if (mapped is not null)
				return mapped;

			return TryResolve(Import2(type));
		}

		TypeRef? Import2(TypeRef? type) {
			if (type is null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			TypeRef? result;

			var declaringType = type.DeclaringType;
			if (declaringType is not null)
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, Import2(declaringType)));
			else
				result = module.UpdateRowId(new TypeRefUser(module, type.Namespace, type.Name, CreateScopeReference(type)));

			recursionCounter.Decrement();
			return result;
		}

		// type is a non-nested TypeRef. Its Module is the module that has the TypeRef, not the type.
		IResolutionScope? CreateScopeReference(TypeRef type) =>
			type.ResolutionScope switch {
				// The type is in another module of the TypeRef's assembly
				ModuleRef modRef => CreateScopeReference(type.DefinitionAssembly, modRef.Name),
				// The type is in the manifest module of the referenced assembly
				AssemblyRef asmRef => CreateScopeReference(asmRef, null),
				ModuleDef mod => CreateScopeReference(mod.Assembly, mod.Name),
				_ => CreateScopeReference(type.DefinitionAssembly, type.Module?.Name),
			};

		/// <summary>
		/// Imports a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSpec? ImportOrNull(TypeSpec? type) {
			if (type is null)
				return null;
			var sig = ImportOrNull(type.TypeSig);
			if (sig is null)
				return null;
			return module.UpdateRowId(new TypeSpecUser(sig));
		}

		/// <summary>
		/// Imports a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSig? ImportOrNull(TypeSig? type) {
			if (type is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			TypeSig? result;
			switch (type.ElementType) {
			case ElementType.Void: result = module.CorLibTypes.Void; break;
			case ElementType.Boolean: result = module.CorLibTypes.Boolean; break;
			case ElementType.Char: result = module.CorLibTypes.Char; break;
			case ElementType.I1: result = module.CorLibTypes.SByte; break;
			case ElementType.U1: result = module.CorLibTypes.Byte; break;
			case ElementType.I2: result = module.CorLibTypes.Int16; break;
			case ElementType.U2: result = module.CorLibTypes.UInt16; break;
			case ElementType.I4: result = module.CorLibTypes.Int32; break;
			case ElementType.U4: result = module.CorLibTypes.UInt32; break;
			case ElementType.I8: result = module.CorLibTypes.Int64; break;
			case ElementType.U8: result = module.CorLibTypes.UInt64; break;
			case ElementType.R4: result = module.CorLibTypes.Single; break;
			case ElementType.R8: result = module.CorLibTypes.Double; break;
			case ElementType.String: result = module.CorLibTypes.String; break;
			case ElementType.TypedByRef: result = module.CorLibTypes.TypedReference; break;
			case ElementType.I: result = module.CorLibTypes.IntPtr; break;
			case ElementType.U: result = module.CorLibTypes.UIntPtr; break;
			case ElementType.Object: result = module.CorLibTypes.Object; break;
			case ElementType.Ptr: result = new PtrSig(ImportOrNull(type.Next)); break;
			case ElementType.ByRef: result = new ByRefSig(ImportOrNull(type.Next)); break;
			case ElementType.ValueType: result = CreateClassOrValueType((type as ClassOrValueTypeSig)!.TypeDefOrRef, true); break;
			case ElementType.Class: result = CreateClassOrValueType((type as ClassOrValueTypeSig)!.TypeDefOrRef, false); break;
			case ElementType.Var: result = new GenericVar((type as GenericVar)!.Number, gpContext.Type); break;
			case ElementType.ValueArray: result = new ValueArraySig(ImportOrNull(type.Next), (type as ValueArraySig)!.Size); break;
			case ElementType.FnPtr: result = new FnPtrSig(ImportOrNull((type as FnPtrSig)!.Signature)); break;
			case ElementType.SZArray: result = new SZArraySig(ImportOrNull(type.Next)); break;
			case ElementType.MVar: result = new GenericMVar((type as GenericMVar)!.Number, gpContext.Method); break;
			case ElementType.CModReqd: result = new CModReqdSig(ImportOrNull((type as ModifierSig)!.Modifier), ImportOrNull(type.Next)); break;
			case ElementType.CModOpt: result = new CModOptSig(ImportOrNull((type as ModifierSig)!.Modifier), ImportOrNull(type.Next)); break;
			case ElementType.Module: result = new ModuleSig((type as ModuleSig)!.Index, ImportOrNull(type.Next)); break;
			case ElementType.Sentinel: result = new SentinelSig(); break;
			case ElementType.Pinned: result = new PinnedSig(ImportOrNull(type.Next)); break;

			case ElementType.Array:
				var arraySig = (ArraySig)type;
				var sizes = new List<uint>(arraySig.Sizes);
				var lbounds = new List<int>(arraySig.LowerBounds);
				result = new ArraySig(ImportOrNull(type.Next), arraySig.Rank, sizes, lbounds);
				break;

			case ElementType.GenericInst:
				var gis = (GenericInstSig)type;
				var genArgs = new List<TypeSig?>(gis.GenericArguments.Count);
				foreach (var ga in gis.GenericArguments)
					genArgs.Add(ImportOrNull(ga));
				result = new GenericInstSig(ImportOrNull(gis.GenericType) as ClassOrValueTypeSig, genArgs);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			case ElementType.CModInternal:
			default:
				result = null;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public ITypeDefOrRef? ImportOrNull(ITypeDefOrRef? type) => (ITypeDefOrRef?)ImportOrNull((IType?)type);

		TypeSig? CreateClassOrValueType(ITypeDefOrRef? type, bool isValueType) {
			var corLibType = module.CorLibTypes.GetCorLibTypeSig(type);
			if (corLibType is not null)
				return corLibType;

			if (isValueType)
				return new ValueTypeSig(ImportOrNull(type));
			return new ClassSig(ImportOrNull(type));
		}

		/// <summary>
		/// Imports a <see cref="CallingConventionSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public CallingConventionSig? ImportOrNull(CallingConventionSig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			CallingConventionSig? result;

			var sigType = sig.GetType();
			if (sigType == typeof(MethodSig))
				result = ImportOrNull((MethodSig)sig);
			else if (sigType == typeof(FieldSig))
				result = ImportOrNull((FieldSig)sig);
			else if (sigType == typeof(GenericInstMethodSig))
				result = ImportOrNull((GenericInstMethodSig)sig);
			else if (sigType == typeof(PropertySig))
				result = ImportOrNull((PropertySig)sig);
			else if (sigType == typeof(LocalSig))
				result = ImportOrNull((LocalSig)sig);
			else
				result = null;  // Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public FieldSig? ImportOrNull(FieldSig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = new FieldSig(sig.GetCallingConvention(), ImportOrNull(sig.Type));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public MethodSig? ImportOrNull(MethodSig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = ImportOrNull(new MethodSig(sig.GetCallingConvention()), sig);

			recursionCounter.Decrement();
			return result;
		}

		T ImportOrNull<T>(T sig, T old) where T : MethodBaseSig {
			sig.RetType = ImportOrNull(old.RetType);
			foreach (var p in old.Params)
				sig.Params.Add(ImportOrNull(p));
			sig.GenParamCount = old.GenParamCount;
			var oldParamsAfterSentinel = old.ParamsAfterSentinel;
			if (oldParamsAfterSentinel is not null) {
				var paramsAfterSentinel = new List<TypeSig?>(oldParamsAfterSentinel.Count);
				foreach (var p in oldParamsAfterSentinel)
					paramsAfterSentinel.Add(ImportOrNull(p));
				sig.ParamsAfterSentinel = paramsAfterSentinel;
			}
			return sig;
		}

		/// <summary>
		/// Imports a <see cref="PropertySig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public PropertySig? ImportOrNull(PropertySig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = ImportOrNull(new PropertySig(sig.GetCallingConvention()), sig);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="LocalSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public LocalSig? ImportOrNull(LocalSig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = new LocalSig(sig.GetCallingConvention(), (uint)sig.Locals.Count);
			foreach (var l in sig.Locals)
				result.Locals.Add(ImportOrNull(l));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="GenericInstMethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported sig or <c>null</c> if input is invalid</returns>
		public GenericInstMethodSig? ImportOrNull(GenericInstMethodSig? sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			var result = new GenericInstMethodSig(sig.GetCallingConvention(), (uint)sig.GenericArguments.Count);
			foreach (var l in sig.GenericArguments)
				result.GenericArguments.Add(ImportOrNull(l));

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public IField? ImportOrNull(IField? field) {
			if (field is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IField? result;
			MemberRef? mr;
			FieldDef? fd;

			if ((fd = field as FieldDef) is not null)
				result = ImportOrNull(fd);
			else if ((mr = field as MemberRef) is not null)
				result = ImportOrNull(mr);
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
		public IMethod? ImportOrNull(IMethod? method) {
			if (method is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			IMethod? result;
			MethodDef? md;
			MethodSpec? ms;
			MemberRef? mr;

			if ((md = method as MethodDef) is not null)
				result = ImportOrNull(md);
			else if ((ms = method as MethodSpec) is not null)
				result = ImportOrNull(ms);
			else if ((mr = method as MemberRef) is not null)
				result = ImportOrNull(mr);
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
		public IField? ImportOrNull(FieldDef? field) {
			if (field is null)
				return null;
			if (TryToUseFieldDefs && field.Module == module)
				return field;
			if (!recursionCounter.Increment())
				return null;
			var mapped = mapper?.Map(field);
			if (mapped is not null) {
				recursionCounter.Decrement();
				return mapped;
			}

			MemberRef result = module.UpdateRowId(new MemberRefUser(module, field.Name));
			result.Signature = ImportOrNull(field.Signature);
			result.Class = ImportParent(field.DeclaringType);

			recursionCounter.Decrement();
			return result;
		}

		IMemberRefParent? ImportParent(TypeDef? type) {
			if (type is null)
				return null;
			if (type.IsGlobalModuleType)
				return module.UpdateRowId(new ModuleRefUser(module, type.Module?.Name));
			return ImportOrNull(type);
		}

		/// <summary>
		/// Imports a <see cref="MethodDef"/> as an <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public IMethod? ImportOrNull(MethodDef? method) {
			if (method is null)
				return null;
			if (TryToUseMethodDefs && method.Module == module)
				return method;
			if (!recursionCounter.Increment())
				return null;
			var mapped = mapper?.Map(method);
			if (mapped is not null) {
				recursionCounter.Decrement();
				return mapped;
			}

			MemberRef result = module.UpdateRowId(new MemberRefUser(module, method.Name));
			result.Signature = ImportOrNull(method.Signature);
			result.Class = ImportParent(method.DeclaringType);

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public MethodSpec? ImportOrNull(MethodSpec? method) {
			if (method is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			// An ImportMapper can return any IMethod, eg. a MethodSpec, which can't be the generic method of a MethodSpec
			MethodSpec? result = null;
			if (ImportOrNull(method.Method) is IMethodDefOrRef genericMethod) {
				result = module.UpdateRowId(new MethodSpecUser(genericMethod));
				result.Instantiation = ImportOrNull(method.Instantiation);
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Imports a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="memberRef">The member ref</param>
		/// <returns>The imported member ref or <c>null</c> if <paramref name="memberRef"/> is invalid</returns>
		public MemberRef? ImportOrNull(MemberRef? memberRef) {
			if (memberRef is null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			var mapped = mapper?.Map(memberRef);
			if (mapped is not null) {
				recursionCounter.Decrement();
				return mapped;
			}

			MemberRef? result = module.UpdateRowId(new MemberRefUser(module, memberRef.Name));
			result.Signature = ImportOrNull(memberRef.Signature);
			result.Class = ImportOrNull(memberRef.Class);
			if (result.Class is null)   // Will be null if memberRef.Class is null or a MethodDef
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		IMemberRefParent? ImportOrNull(IMemberRefParent? parent) {
			if (parent is ITypeDefOrRef tdr) {
				if (tdr is TypeDef td && td.IsGlobalModuleType)
					return module.UpdateRowId(new ModuleRefUser(module, td.Module?.Name));
				return ImportOrNull(tdr);
			}

			if (parent is ModuleRef modRef)
				return module.UpdateRowId(new ModuleRefUser(module, modRef.Name));

			if (parent is MethodDef method) {
				var dt = method.DeclaringType;
				return dt is null || dt.Module != module ? null : method;
			}

			return null;
		}

		// The methods below never return null if the input isn't null: they throw if the import fails.
		// dnlib itself must call the OrNull methods.

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? Import(Type? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? Import(Type? type, IList<Type>? requiredModifiers, IList<Type>? optionalModifiers) =>
			type is null ? null : ImportOrNull(type, requiredModifiers, optionalModifiers) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportAsTypeSigOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public TypeSig? ImportAsTypeSig(Type? type) =>
			type is null ? null : ImportAsTypeSigOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportAsTypeSigOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public TypeSig? ImportAsTypeSig(Type? type, IList<Type>? requiredModifiers, IList<Type>? optionalModifiers) =>
			type is null ? null : ImportAsTypeSigOrNull(type, requiredModifiers, optionalModifiers) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="MethodBase"/> as a <see cref="IMethod"/>. This will be either
		/// a <see cref="MemberRef"/> or a <see cref="MethodSpec"/>.
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method. It's <c>null</c> only if <paramref name="methodBase"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(methodBase))]
		public IMethod? Import(MethodBase? methodBase) =>
			methodBase is null ? null : ImportOrNull(methodBase) ?? throw new ImportException($"Could not import: {methodBase}");

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field. It's <c>null</c> only if <paramref name="fieldInfo"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(fieldInfo))]
		public IField? Import(FieldInfo? fieldInfo) =>
			fieldInfo is null ? null : ImportOrNull(fieldInfo) ?? throw new ImportException($"Could not import: {fieldInfo}");

		/// <summary>
		/// Imports a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public IType? Import(IType? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="TypeDef"/> as a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? Import(TypeDef? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? Import(TypeRef? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public TypeSpec? Import(TypeSpec? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public TypeSig? Import(TypeSig? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

#pragma warning disable RS0030 // Doc references to the throwing Import(MethodBase)/ImportAsOperand(MethodBase)
		/// <summary>
		/// Imports a <see cref="Type"/> used as an instruction operand (eg. <c>newarr</c>, <c>castclass</c>, catch type),
		/// see <see cref="ImportAsOperand(MethodBase)"/>.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportAsOperandOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? ImportAsOperand(Type? type) =>
			type is null ? null : ImportAsOperandOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="MethodBase"/> used as an instruction operand (eg. <c>call</c>, <c>newobj</c>, <c>ldftn</c>).
		/// <para>
		/// Reflection returns the same <see cref="Type"/> for a generic type definition (eg. <c>My&lt;&gt;</c>) and
		/// for the generic type instantiated with its own generic parameters (eg. <c>My&lt;T&gt;</c> inside <c>My&lt;T&gt;</c>).
		/// <see cref="Import(MethodBase)"/> imports it as the generic type definition, but an instruction operand can't
		/// reference a member of an uninstantiated generic type, so this method imports all generic type definitions
		/// as generic instance types instantiated with their own generic parameters, eg. <c>My`1&lt;!0&gt;</c>.
		/// </para>
		/// <para>
		/// <c>ldtoken</c> is the only instruction that can reference both of them (eg. <c>typeof(My&lt;&gt;)</c> and
		/// <c>typeof(My&lt;T&gt;)</c>). This method returns the generic instance type which is the most common one. Use
		/// <see cref="Import(MethodBase)"/> if it's the generic type definition.
		/// </para>
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method. It's <c>null</c> only if <paramref name="methodBase"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportAsOperandOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(methodBase))]
		public IMethod? ImportAsOperand(MethodBase? methodBase) =>
			methodBase is null ? null : ImportAsOperandOrNull(methodBase) ?? throw new ImportException($"Could not import: {methodBase}");

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> used as an instruction operand (eg. <c>ldfld</c>, <c>stsfld</c>),
		/// see <see cref="ImportAsOperand(MethodBase)"/>.
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field. It's <c>null</c> only if <paramref name="fieldInfo"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportAsOperandOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(fieldInfo))]
		public IField? ImportAsOperand(FieldInfo? fieldInfo) =>
			fieldInfo is null ? null : ImportAsOperandOrNull(fieldInfo) ?? throw new ImportException($"Could not import: {fieldInfo}");
#pragma warning restore RS0030

		/// <summary>
		/// Imports a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type. It's <c>null</c> only if <paramref name="type"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(type))]
		public ITypeDefOrRef? Import(ITypeDefOrRef? type) =>
			type is null ? null : ImportOrNull(type) ?? throw new ImportException($"Could not import: {type}");

		/// <summary>
		/// Imports a <see cref="CallingConventionSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public CallingConventionSig? Import(CallingConventionSig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public FieldSig? Import(FieldSig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="MethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public MethodSig? Import(MethodSig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="PropertySig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public PropertySig? Import(PropertySig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="LocalSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public LocalSig? Import(LocalSig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="GenericInstMethodSig"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		/// <returns>The imported signature. It's <c>null</c> only if <paramref name="sig"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(sig))]
		public GenericInstMethodSig? Import(GenericInstMethodSig? sig) =>
			sig is null ? null : ImportOrNull(sig) ?? throw new ImportException($"Could not import: {sig}");

		/// <summary>
		/// Imports a <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported field. It's <c>null</c> only if <paramref name="field"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(field))]
		public IField? Import(IField? field) =>
			field is null ? null : ImportOrNull(field) ?? throw new ImportException($"Could not import: {field}");

		/// <summary>
		/// Imports a <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method. It's <c>null</c> only if <paramref name="method"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(method))]
		public IMethod? Import(IMethod? method) =>
			method is null ? null : ImportOrNull(method) ?? throw new ImportException($"Could not import: {method}");

		/// <summary>
		/// Imports a <see cref="FieldDef"/> as an <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported field. It's <c>null</c> only if <paramref name="field"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(field))]
		public IField? Import(FieldDef? field) =>
			field is null ? null : ImportOrNull(field) ?? throw new ImportException($"Could not import: {field}");

		/// <summary>
		/// Imports a <see cref="MethodDef"/> as an <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method. It's <c>null</c> only if <paramref name="method"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(method))]
		public IMethod? Import(MethodDef? method) =>
			method is null ? null : ImportOrNull(method) ?? throw new ImportException($"Could not import: {method}");

		/// <summary>
		/// Imports a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method. It's <c>null</c> only if <paramref name="method"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(method))]
		public MethodSpec? Import(MethodSpec? method) =>
			method is null ? null : ImportOrNull(method) ?? throw new ImportException($"Could not import: {method}");

		/// <summary>
		/// Imports a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="memberRef">The member ref</param>
		/// <returns>The imported member reference. It's <c>null</c> only if <paramref name="memberRef"/> is <c>null</c>.</returns>
		/// <exception cref="ImportException">It couldn't be imported. <c>ImportOrNull()</c> returns <c>null</c> instead of throwing.</exception>
		[return: NotNullIfNotNull(nameof(memberRef))]
		public MemberRef? Import(MemberRef? memberRef) =>
			memberRef is null ? null : ImportOrNull(memberRef) ?? throw new ImportException($"Could not import: {memberRef}");
	}

	/// <summary>
	/// Thrown by the <see cref="Importer"/>'s <c>Import()</c> methods if something couldn't be imported.
	/// The <c>ImportOrNull()</c> methods return <c>null</c> instead of throwing.
	/// </summary>
	[Serializable]
	public class ImportException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ImportException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public ImportException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public ImportException(string message, Exception? innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected ImportException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) {
		}
	}
}
