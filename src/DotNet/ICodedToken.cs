// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Pdb;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// The table row can be referenced by a MD token
	/// </summary>
	public interface IMDTokenProvider {
		/// <summary>
		/// Returns the metadata token
		/// </summary>
		MDToken MDToken { get; }

		/// <summary>
		/// Gets/sets the row ID
		/// </summary>
		uint Rid { get; set; }
	}

	/// <summary>
	/// All <c>*MD</c> classes implement this interface.
	/// </summary>
	public interface IMDTokenProviderMD : IMDTokenProvider {
		/// <summary>
		/// Gets the original row ID
		/// </summary>
		uint OrigRid { get; }
	}

	/// <summary>
	/// An assembly. Implemented by <see cref="AssemblyRef"/>, <see cref="AssemblyDef"/> and
	/// <see cref="AssemblyNameInfo"/>.
	/// </summary>
	public interface IAssembly : IFullName {
		/// <summary>
		/// The assembly version
		/// </summary>
		Version Version { get; set; }

		/// <summary>
		/// Assembly flags
		/// </summary>
		AssemblyAttributes Attributes { get; set; }

		/// <summary>
		/// Public key or public key token
		/// </summary>
		PublicKeyBase PublicKeyOrToken { get; }

		/// <summary>
		/// Locale, aka culture
		/// </summary>
		UTF8String Culture { get; set; }

		/// <summary>
		/// Gets the full name of the assembly but use a public key token
		/// </summary>
		string FullNameToken { get; }

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		bool HasPublicKey { get; set; }

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		AssemblyAttributes ProcessorArchitecture { get; set; }

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		AssemblyAttributes ProcessorArchitectureFull { get; set; }

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		bool IsProcessorArchitectureNone { get; }

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		bool IsProcessorArchitectureMSIL { get; }

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		bool IsProcessorArchitectureX86 { get; }

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		bool IsProcessorArchitectureIA64 { get; }

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		bool IsProcessorArchitectureX64 { get; }

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		bool IsProcessorArchitectureARM { get; }

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		bool IsProcessorArchitectureNoPlatform { get; }

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PA_Specified"/> bit
		/// </summary>
		bool IsProcessorArchitectureSpecified { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		bool EnableJITcompileTracking { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		bool DisableJITcompileOptimizer { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		bool IsRetargetable { get; set; }

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		AssemblyAttributes ContentType { get; set; }

		/// <summary>
		/// <c>true</c> if content type is <c>Default</c>
		/// </summary>
		bool IsContentTypeDefault { get; }

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		bool IsContentTypeWindowsRuntime { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Checks whether <paramref name="asm"/> appears to be the core library (eg.
		/// mscorlib, System.Runtime or corefx).
		/// 
		/// If <paramref name="asm"/> is a reference to a private corlib (eg. System.Private.CoreLib),
		/// this method returns false unless <paramref name="asm"/> is an <see cref="AssemblyDef"/>
		/// whose manifest (first) module defines <c>System.Object</c>. This check is performed in
		/// the constructor and the result can be found in <see cref="ModuleDef.IsCoreLibraryModule"/>.
		/// 
		/// Note that this method also returns true if it appears to be a 'public' corlib,
		/// eg. mscorlib, etc, even if it internally possibly references a private corlib.
		/// </summary>
		/// <param name="asm">The assembly</param>
		public static bool IsCorLib(this IAssembly asm) {
			var asmDef = asm as AssemblyDef;
			if (asmDef != null) {
				var manifestModule = asmDef.ManifestModule;
				if (manifestModule != null) {
					var isCorModule = manifestModule.IsCoreLibraryModule;
					if (isCorModule != null)
						return isCorModule.Value;
				}
			}

			string asmName;
			return asm != null &&
				UTF8String.IsNullOrEmpty(asm.Culture) &&
				((asmName = UTF8String.ToSystemStringOrEmpty(asm.Name)).Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
				asmName.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase) ||
				// This name could change but since CoreCLR is used a lot, it's worth supporting
				asmName.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
				asmName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
				asmName.Equals("corefx", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Converts <paramref name="asm"/> to a <see cref="AssemblyRef"/> instance
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns>A new <see cref="AssemblyRef"/> instance</returns>
		public static AssemblyRef ToAssemblyRef(this IAssembly asm) {
			if (asm == null)
				return null;
			// Always create a new one, even if it happens to be an AssemblyRef
			return new AssemblyRefUser(asm.Name, asm.Version, asm.PublicKeyOrToken, asm.Culture) { Attributes = asm.Attributes };
		}

		/// <summary>
		/// Converts <paramref name="type"/> to a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="checkValueType"><c>true</c> if we should try to figure out whether
		/// <paramref name="type"/> is a <see cref="ValueType"/></param>
		/// <returns>A <see cref="TypeSig"/> instance or <c>null</c> if <paramref name="type"/>
		/// is invalid</returns>
		public static TypeSig ToTypeSig(this ITypeDefOrRef type, bool checkValueType = true) {
			if (type == null)
				return null;

			var module = type.Module;
			if (module != null) {
				var corLibType = module.CorLibTypes.GetCorLibTypeSig(type);
				if (corLibType != null)
					return corLibType;
			}

			var td = type as TypeDef;
			if (td != null)
				return CreateClassOrValueType(type, checkValueType ? td.IsValueType : false);

			var tr = type as TypeRef;
			if (tr != null) {
				if (checkValueType)
					td = tr.Resolve();
				return CreateClassOrValueType(type, td == null ? false : td.IsValueType);
			}

			var ts = type as TypeSpec;
			if (ts != null)
				return ts.TypeSig;

			return null;
		}

		static TypeSig CreateClassOrValueType(ITypeDefOrRef type, bool isValueType) {
			if (isValueType)
				return new ValueTypeSig(type);
			return new ClassSig(type);
		}

		/// <summary>
		/// Returns a <see cref="TypeDefOrRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="TypeDefOrRefSig"/> or <c>null</c> if it's not a
		/// <see cref="TypeDefOrRefSig"/></returns>
		public static TypeDefOrRefSig TryGetTypeDefOrRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as TypeDefOrRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassOrValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassOrValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassOrValueTypeSig"/></returns>
		public static ClassOrValueTypeSig TryGetClassOrValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ValueTypeSig"/></returns>
		public static ValueTypeSig TryGetValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassSig"/></returns>
		public static ClassSig TryGetClassSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericSig"/></returns>
		public static GenericSig TryGetGenericSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericVar"/></returns>
		public static GenericVar TryGetGenericVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericMVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericMVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericMVar"/></returns>
		public static GenericMVar TryGetGenericMVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericMVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericInstSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericInstSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericInstSig"/></returns>
		public static GenericInstSig TryGetGenericInstSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericInstSig;
		}

		/// <summary>
		/// Returns a <see cref="PtrSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="PtrSig"/> or <c>null</c> if it's not a
		/// <see cref="PtrSig"/></returns>
		public static PtrSig TryGetPtrSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as PtrSig;
		}

		/// <summary>
		/// Returns a <see cref="ByRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ByRefSig"/> or <c>null</c> if it's not a
		/// <see cref="ByRefSig"/></returns>
		public static ByRefSig TryGetByRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ByRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ArraySig"/> or <c>null</c> if it's not a
		/// <see cref="ArraySig"/></returns>
		public static ArraySig TryGetArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ArraySig;
		}

		/// <summary>
		/// Returns a <see cref="SZArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="SZArraySig"/> or <c>null</c> if it's not a
		/// <see cref="SZArraySig"/></returns>
		public static SZArraySig TryGetSZArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as SZArraySig;
		}

		/// <summary>
		/// Returns the base type of <paramref name="tdr"/>. Throws if we can't resolve
		/// a <see cref="TypeRef"/>.
		/// </summary>
		/// <param name="tdr">The type</param>
		/// <returns>The base type or <c>null</c> if there's no base type</returns>
		public static ITypeDefOrRef GetBaseTypeThrow(this ITypeDefOrRef tdr) {
			return tdr.GetBaseType(true);
		}

		/// <summary>
		/// Returns the base type of <paramref name="tdr"/>
		/// </summary>
		/// <param name="tdr">The type</param>
		/// <param name="throwOnResolveFailure"><c>true</c> if we should throw if we can't
		/// resolve a <see cref="TypeRef"/>. <c>false</c> if we should ignore the error and
		/// just return <c>null</c>.</param>
		/// <returns>The base type or <c>null</c> if there's no base type, or if
		/// <paramref name="throwOnResolveFailure"/> is <c>true</c> and we couldn't resolve
		/// a <see cref="TypeRef"/></returns>
		public static ITypeDefOrRef GetBaseType(this ITypeDefOrRef tdr, bool throwOnResolveFailure = false) {
			var td = tdr as TypeDef;
			if (td != null)
				return td.BaseType;

			var tr = tdr as TypeRef;
			if (tr != null) {
				td = throwOnResolveFailure ? tr.ResolveThrow() : tr.Resolve();
				return td == null ? null : td.BaseType;
			}

			var ts = tdr as TypeSpec;
			if (ts == null)
				return null;

			var git = ts.TypeSig.ToGenericInstSig();
			if (git != null) {
				var genType = git.GenericType;
				tdr = genType == null ? null : genType.TypeDefOrRef;
			}
			else {
				var sig = ts.TypeSig.ToTypeDefOrRefSig();
				tdr = sig == null ? null : sig.TypeDefOrRef;
			}

			td = tdr as TypeDef;
			if (td != null)
				return td.BaseType;

			tr = tdr as TypeRef;
			if (tr != null) {
				td = throwOnResolveFailure ? tr.ResolveThrow() : tr.Resolve();
				return td == null ? null : td.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Gets the scope type, resolves it, and returns the <see cref="TypeDef"/>
		/// </summary>
		/// <param name="tdr">Type</param>
		/// <returns>A <see cref="TypeDef"/> or <c>null</c> if input was <c>null</c> or if we
		/// couldn't resolve the reference.</returns>
		public static TypeDef ResolveTypeDef(this ITypeDefOrRef tdr) {
			var td = tdr as TypeDef;
			if (td != null)
				return td;

			var tr = tdr as TypeRef;
			if (tr != null)
				return tr.Resolve();

			if (tdr == null)
				return null;
			tdr = tdr.ScopeType;

			td = tdr as TypeDef;
			if (td != null)
				return td;

			tr = tdr as TypeRef;
			if (tr != null)
				return tr.Resolve();

			return null;
		}

		/// <summary>
		/// Gets the scope type, resolves it, and returns the <see cref="TypeDef"/>
		/// </summary>
		/// <param name="tdr">Type</param>
		/// <returns>A <see cref="TypeDef"/> instance.</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public static TypeDef ResolveTypeDefThrow(this ITypeDefOrRef tdr) {
			var td = tdr as TypeDef;
			if (td != null)
				return td;

			var tr = tdr as TypeRef;
			if (tr != null)
				return tr.ResolveThrow();

			if (tdr == null)
				throw new TypeResolveException("Can't resolve a null pointer");
			tdr = tdr.ScopeType;

			td = tdr as TypeDef;
			if (td != null)
				return td;

			tr = tdr as TypeRef;
			if (tr != null)
				return tr.ResolveThrow();

			throw new TypeResolveException(string.Format("Could not resolve type: {0} ({1})", tdr, tdr == null ? null : tdr.DefinitionAssembly));
		}

		/// <summary>
		/// Resolves an <see cref="IField"/> to a <see cref="FieldDef"/>. Returns <c>null</c> if it
		/// was not possible to resolve it. See also <see cref="ResolveFieldDefThrow"/>
		/// </summary>
		/// <param name="field">Field to resolve</param>
		/// <returns>The <see cref="FieldDef"/> or <c>null</c> if <paramref name="field"/> is
		/// <c>null</c> or if it wasn't possible to resolve it (the field doesn't exist or its
		/// assembly couldn't be loaded)</returns>
		public static FieldDef ResolveFieldDef(this IField field) {
			var fd = field as FieldDef;
			if (fd != null)
				return fd;

			var mr = field as MemberRef;
			if (mr != null)
				return mr.ResolveField();

			return null;
		}

		/// <summary>
		/// Resolves an <see cref="IField"/> to a <see cref="FieldDef"/> and throws an exception if
		/// it was not possible to resolve it. See also <see cref="ResolveFieldDef"/>
		/// </summary>
		/// <param name="field">Field to resolve</param>
		/// <returns>The <see cref="FieldDef"/></returns>
		public static FieldDef ResolveFieldDefThrow(this IField field) {
			var fd = field as FieldDef;
			if (fd != null)
				return fd;

			var mr = field as MemberRef;
			if (mr != null)
				return mr.ResolveFieldThrow();

			throw new MemberRefResolveException(string.Format("Could not resolve field: {0}", field));
		}

		/// <summary>
		/// Resolves an <see cref="IMethod"/> to a <see cref="MethodDef"/>. Returns <c>null</c> if it
		/// was not possible to resolve it. See also <see cref="ResolveMethodDefThrow"/>. If
		/// <paramref name="method"/> is a <see cref="MethodSpec"/>, then the
		/// <see cref="MethodSpec.Method"/> property is resolved and returned.
		/// </summary>
		/// <param name="method">Method to resolve</param>
		/// <returns>The <see cref="MethodDef"/> or <c>null</c> if <paramref name="method"/> is
		/// <c>null</c> or if it wasn't possible to resolve it (the method doesn't exist or its
		/// assembly couldn't be loaded)</returns>
		public static MethodDef ResolveMethodDef(this IMethod method) {
			var md = method as MethodDef;
			if (md != null)
				return md;

			var mr = method as MemberRef;
			if (mr != null)
				return mr.ResolveMethod();

			var ms = method as MethodSpec;
			if (ms != null) {
				md = ms.Method as MethodDef;
				if (md != null)
					return md;

				mr = ms.Method as MemberRef;
				if (mr != null)
					return mr.ResolveMethod();
			}

			return null;
		}

		/// <summary>
		/// Resolves an <see cref="IMethod"/> to a <see cref="MethodDef"/> and throws an exception
		/// if it was not possible to resolve it. See also <see cref="ResolveMethodDef"/>. If
		/// <paramref name="method"/> is a <see cref="MethodSpec"/>, then the
		/// <see cref="MethodSpec.Method"/> property is resolved and returned.
		/// </summary>
		/// <param name="method">Method to resolve</param>
		/// <returns>The <see cref="MethodDef"/></returns>
		public static MethodDef ResolveMethodDefThrow(this IMethod method) {
			var md = method as MethodDef;
			if (md != null)
				return md;

			var mr = method as MemberRef;
			if (mr != null)
				return mr.ResolveMethodThrow();

			var ms = method as MethodSpec;
			if (ms != null) {
				md = ms.Method as MethodDef;
				if (md != null)
					return md;

				mr = ms.Method as MemberRef;
				if (mr != null)
					return mr.ResolveMethodThrow();
			}

			throw new MemberRefResolveException(string.Format("Could not resolve method: {0}", method));
		}

		/// <summary>
		/// Returns the definition assembly of a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="mr">Member reference</param>
		/// <returns></returns>
		static internal IAssembly GetDefinitionAssembly(this MemberRef mr) {
			if (mr == null)
				return null;
			var parent = mr.Class;

			var tdr = parent as ITypeDefOrRef;
			if (tdr != null)
				return tdr.DefinitionAssembly;

			if (parent is ModuleRef) {
				var mod = mr.Module;
				return mod == null ? null : mod.Assembly;
			}

			var md = parent as MethodDef;
			if (md != null) {
				var declType = md.DeclaringType;
				return declType == null ? null : declType.DefinitionAssembly;
			}

			return null;
		}
	}

	/// <summary>
	/// Implemented by <see cref="MethodDef"/> and <see cref="FileDef"/>, which are the only
	/// valid managed entry point tokens.
	/// </summary>
	public interface IManagedEntryPoint : ICodedToken {
	}

	/// <summary>
	/// Interface to access a module def/ref
	/// </summary>
	public interface IModule : IScope, IFullName {
	}

	/// <summary>
	/// Type of scope
	/// </summary>
	public enum ScopeType {
		/// <summary>
		/// It's an <see cref="dnlib.DotNet.AssemblyRef"/> instance
		/// </summary>
		AssemblyRef,

		/// <summary>
		/// It's a <see cref="dnlib.DotNet.ModuleRef"/> instance
		/// </summary>
		ModuleRef,

		/// <summary>
		/// It's a <see cref="dnlib.DotNet.ModuleDef"/> instance
		/// </summary>
		ModuleDef,
	}

	/// <summary>
	/// Implemented by modules and assemblies
	/// </summary>
	public interface IScope {
		/// <summary>
		/// Gets the scope type
		/// </summary>
		ScopeType ScopeType { get; }

		/// <summary>
		/// Gets the scope name
		/// </summary>
		string ScopeName { get; }
	}

	/// <summary>
	/// Interface to get the full name of a type, field, or method
	/// </summary>
	public interface IFullName {
		/// <summary>
		/// Gets the full name
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// Simple name of implementer
		/// </summary>
		UTF8String Name { get; set; }
	}

	/// <summary>
	/// Implemented by all member refs and types
	/// </summary>
	public interface IOwnerModule {
		/// <summary>
		/// Gets the owner module
		/// </summary>
		ModuleDef Module { get; }
	}

	/// <summary>
	/// Methods to check whether the implementer is a type or a method.
	/// </summary>
	public interface IIsTypeOrMethod {
		/// <summary>
		/// <c>true</c> if it's a type
		/// </summary>
		bool IsType { get; }

		/// <summary>
		/// <c>true</c> if it's a method
		/// </summary>
		bool IsMethod { get; }
	}

	/// <summary>
	/// Implemented by types, fields, methods, properties, events
	/// </summary>
	public interface IMemberRef : ICodedToken, IFullName, IOwnerModule, IIsTypeOrMethod {
		/// <summary>
		/// Gets the declaring type
		/// </summary>
		ITypeDefOrRef DeclaringType { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="FieldDef"/> or a <see cref="MemberRef"/> that's
		/// referencing a field.
		/// </summary>
		bool IsField { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="TypeSpec"/>
		/// </summary>
		bool IsTypeSpec { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="TypeRef"/>
		/// </summary>
		bool IsTypeRef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="TypeDef"/>
		/// </summary>
		bool IsTypeDef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="MethodSpec"/>
		/// </summary>
		bool IsMethodSpec { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="MethodDef"/>
		/// </summary>
		bool IsMethodDef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="MemberRef"/>
		/// </summary>
		bool IsMemberRef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="FieldDef"/>
		/// </summary>
		bool IsFieldDef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="PropertyDef"/>
		/// </summary>
		bool IsPropertyDef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="EventDef"/>
		/// </summary>
		bool IsEventDef { get; }

		/// <summary>
		/// <c>true</c> if it's a <see cref="GenericParam"/>
		/// </summary>
		bool IsGenericParam { get; }
	}

	/// <summary>
	/// All member definitions implement this interface: <see cref="TypeDef"/>,
	/// <see cref="FieldDef"/>, <see cref="MethodDef"/>, <see cref="EventDef"/>,
	/// <see cref="PropertyDef"/>, and <see cref="GenericParam"/>.
	/// </summary>
	public interface IMemberDef : IDnlibDef, IMemberRef {
		/// <summary>
		/// Gets the declaring type
		/// </summary>
		new TypeDef DeclaringType { get; }
	}

	/// <summary>
	/// Implemented by the following classes: <see cref="TypeDef"/>,
	/// <see cref="FieldDef"/>, <see cref="MethodDef"/>, <see cref="EventDef"/>,
	/// <see cref="PropertyDef"/>, <see cref="GenericParam"/>, <see cref="AssemblyDef"/>,
	/// and <see cref="ModuleDef"/>
	/// </summary>
	public interface IDnlibDef : ICodedToken, IFullName, IHasCustomAttribute {
	}

	/// <summary>
	/// Implemented by types and methods
	/// </summary>
	public interface IGenericParameterProvider : ICodedToken, IIsTypeOrMethod {
		/// <summary>
		/// Gets the number of generic parameters / arguments
		/// </summary>
		int NumberOfGenericParameters { get; }
	}

	/// <summary>
	/// Implemented by fields (<see cref="FieldDef"/> and <see cref="MemberRef"/>)
	/// </summary>
	public interface IField : ICodedToken, ITokenOperand, IFullName, IMemberRef {
		/// <summary>
		/// Gets/sets the field signature
		/// </summary>
		FieldSig FieldSig { get; set; }
	}

	/// <summary>
	/// Implemented by methods (<see cref="MethodDef"/>, <see cref="MemberRef"/> and <see cref="MethodSpec"/>)
	/// </summary>
	public interface IMethod : ICodedToken, ITokenOperand, IFullName, IGenericParameterProvider, IMemberRef {
		/// <summary>
		/// Method signature
		/// </summary>
		MethodSig MethodSig { get; set; }
	}

	/// <summary>
	/// Implemented by tables that can be a token in the <c>ldtoken</c> instruction
	/// </summary>
	public interface ITokenOperand : ICodedToken {
	}

	/// <summary>
	/// The table row can be referenced by a coded token
	/// </summary>
	public interface ICodedToken : IMDTokenProvider {
	}

	/// <summary>
	/// TypeDefOrRef coded token interface
	/// </summary>
	public interface ITypeDefOrRef : ICodedToken, IHasCustomAttribute, IMemberRefParent, IType, ITokenOperand, IMemberRef {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeDefOrRefTag { get; }
	}

	/// <summary>
	/// HasConstant coded token interface
	/// </summary>
	public interface IHasConstant : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasConstantTag { get; }

		/// <summary>
		/// Gets/sets the constant value
		/// </summary>
		Constant Constant { get; set; }
	}

	/// <summary>
	/// HasCustomAttribute coded token interface
	/// </summary>
	public interface IHasCustomAttribute : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasCustomAttributeTag { get; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// <c>true</c> if <see cref="CustomAttributes"/> is not empty
		/// </summary>
		bool HasCustomAttributes { get; }
	}

	/// <summary>
	/// HasFieldMarshal coded token interface
	/// </summary>
	public interface IHasFieldMarshal : ICodedToken, IHasCustomAttribute, IHasConstant, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasFieldMarshalTag { get; }

		/// <summary>
		/// Gets/sets the marshal type
		/// </summary>
		MarshalType MarshalType { get; set; }

		/// <summary>
		/// <c>true</c> if <see cref="MarshalType"/> is not <c>null</c>
		/// </summary>
		bool HasMarshalType { get; }
	}

	/// <summary>
	/// HasDeclSecurity coded token interface
	/// </summary>
	public interface IHasDeclSecurity : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasDeclSecurityTag { get; }

		/// <summary>
		/// Gets the permission sets
		/// </summary>
		ThreadSafe.IList<DeclSecurity> DeclSecurities { get; }

		/// <summary>
		/// <c>true</c> if <see cref="DeclSecurities"/> is not empty
		/// </summary>
		bool HasDeclSecurities { get; }
	}

	/// <summary>
	/// MemberRefParent coded token interface
	/// </summary>
	public interface IMemberRefParent : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberRefParentTag { get; }
	}

	/// <summary>
	/// HasSemantic coded token interface
	/// </summary>
	public interface IHasSemantic : ICodedToken, IHasCustomAttribute, IFullName, IMemberRef {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasSemanticTag { get; }
	}

	/// <summary>
	/// MethodDefOrRef coded token interface
	/// </summary>
	public interface IMethodDefOrRef : ICodedToken, IHasCustomAttribute, ICustomAttributeType, IMethod {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MethodDefOrRefTag { get; }
	}

	/// <summary>
	/// MemberForwarded coded token interface
	/// </summary>
	public interface IMemberForwarded : ICodedToken, IHasCustomAttribute, IFullName, IMemberRef {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberForwardedTag { get; }

		/// <summary>
		/// Gets/sets the impl map
		/// </summary>
		ImplMap ImplMap { get; set; }

		/// <summary>
		/// <c>true</c> if <see cref="ImplMap"/> is not <c>null</c>
		/// </summary>
		bool HasImplMap { get; }
	}

	/// <summary>
	/// Implementation coded token interface
	/// </summary>
	public interface IImplementation : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ImplementationTag { get; }
	}

	/// <summary>
	/// CustomAttributeType coded token interface
	/// </summary>
	public interface ICustomAttributeType : ICodedToken, IHasCustomAttribute, IMethod {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int CustomAttributeTypeTag { get; }
	}

	/// <summary>
	/// ResolutionScope coded token interface
	/// </summary>
	public interface IResolutionScope : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ResolutionScopeTag { get; }
	}

	/// <summary>
	/// TypeOrMethodDef coded token interface
	/// </summary>
	public interface ITypeOrMethodDef : ICodedToken, IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IFullName, IMemberRef, IGenericParameterProvider {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeOrMethodDefTag { get; }

		/// <summary>
		/// Gets the generic parameters
		/// </summary>
		ThreadSafe.IList<GenericParam> GenericParameters { get; }

		/// <summary>
		/// <c>true</c> if <see cref="GenericParameters"/> is not empty
		/// </summary>
		bool HasGenericParameters { get; }
	}

	/// <summary>
	/// HasCustomDebugInformation interface
	/// </summary>
	public interface IHasCustomDebugInformation {
		/// <summary>
		/// The custom debug information tag
		/// </summary>
		int HasCustomDebugInformationTag { get; }

		/// <summary>
		/// Gets the custom debug infos
		/// </summary>
		ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos { get; }

		/// <summary>
		/// <c>true</c> if <see cref="CustomDebugInfos"/> is not empty
		/// </summary>
		bool HasCustomDebugInfos { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Converts a <see cref="TypeSig"/> to a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		public static ITypeDefOrRef ToTypeDefOrRef(this TypeSig sig) {
			if (sig == null)
				return null;
			var tdrSig = sig as TypeDefOrRefSig;
			if (tdrSig != null)
				return tdrSig.TypeDefOrRef;
			var module = sig.Module;
			if (module == null)
				return new TypeSpecUser(sig);
			return module.UpdateRowId(new TypeSpecUser(sig));
		}

		/// <summary>
		/// Returns <c>true</c> if it's an integer or a floating point type
		/// </summary>
		/// <param name="tdr">Type</param>
		/// <returns></returns>
		internal static bool IsPrimitive(this IType tdr) {
			if (tdr == null)
				return false;
			if (!tdr.DefinitionAssembly.IsCorLib())
				return false;

			switch (tdr.FullName) {
			case "System.Boolean":
			case "System.Char":
			case "System.SByte":
			case "System.Byte":
			case "System.Int16":
			case "System.UInt16":
			case "System.Int32":
			case "System.UInt32":
			case "System.Int64":
			case "System.UInt64":
			case "System.Single":
			case "System.Double":
			case "System.IntPtr":
			case "System.UIntPtr":
				return true;
			default:
				return false;
			}
		}
	}
}
