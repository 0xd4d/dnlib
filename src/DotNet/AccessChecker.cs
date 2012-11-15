using System;
using System.Collections.Generic;

namespace dot10.DotNet {
	/// <summary>
	/// Checks whether a type has access to some other target type, method or field
	/// according to the target's visibility.
	/// </summary>
	public struct AccessChecker {
		TypeDef userType;
		List<TypeDef> userTypeEnclosingTypes;
		bool enclosingTypesInitialized;
		Dictionary<ITypeDefOrRef, bool> baseTypes;
		bool baseTypesInitialized;

		[Flags]
		enum CheckTypeAccess {
			/// <summary>
			/// Can't access the type
			/// </summary>
			None = 0,

			/// <summary>
			/// Normal access to the type and its members. I.e., type must be public or
			/// internal to access the type. Type + member must be public or internal to
			/// access the member.
			/// </summary>
			Normal = 1,

			/// <summary>
			/// Full access to the type, even if the type is private. If clear, the type
			/// must be public or internal.
			/// </summary>
			FullTypeAccess = 2,

			/// <summary>
			/// Full access to the type's members (types, fields, methods), even if the
			/// members are private. If clear, the members must be public or internal.
			/// </summary>
			FullMemberAccess = 4,

			/// <summary>
			/// Full access to the type and its members
			/// </summary>
			Full = Normal | FullTypeAccess | FullMemberAccess,
		}

		/// <summary>
		/// Gets/sets the user type
		/// </summary>
		public TypeDef UserType {
			get { return userType; }
			set {
				if (userType == value)
					return;
				userType = value;
				enclosingTypesInitialized = false;
				baseTypesInitialized = false;
				if (userTypeEnclosingTypes != null)
					userTypeEnclosingTypes.Clear();
				if (baseTypes != null)
					baseTypes.Clear();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userType">The type accessing the target type, field or method</param>
		public AccessChecker(TypeDef userType) {
			this.userType = userType;
			this.userTypeEnclosingTypes = null;
			this.baseTypes = null;
			this.enclosingTypesInitialized = false;
			this.baseTypesInitialized = false;
		}

		/// <summary>
		/// Checks whether it has access to a method or a field
		/// </summary>
		/// <param name="op">Operand</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(object op) {
			var md = op as MethodDef;
			if (md != null)
				return CanAccess(md);

			var mr = op as MemberRef;
			if (mr != null)
				return CanAccess(mr);

			var fd = op as FieldDef;
			if (fd != null)
				return CanAccess(fd);

			var ms = op as MethodSpec;
			if (ms != null)
				return CanAccess(ms);

			var tr = op as TypeRef;
			if (tr != null)
				return CanAccess(tr.Resolve());

			var td = op as TypeDef;
			if (td != null)
				return CanAccess(td);

			var ts = op as TypeSpec;
			if (ts != null)
				return CanAccess(ts);

			return null;
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="tr">The type</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(TypeRef tr) {
			if (tr == null)
				return null;
			return CanAccess(tr.Resolve());
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="td">The type</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(TypeDef td) {
			var access = GetTypeAccess(td);
			if (access == null)
				return null;
			return (access.Value & CheckTypeAccess.Normal) != 0;
		}

		CheckTypeAccess? GetTypeAccess(TypeDef td) {
			if (td == null)
				return null;
			if (userType == td)
				return CheckTypeAccess.Full;

			// If we're not a nested type, td can't be our enclosing type
			if (userType.DeclaringType == null)
				return GetTypeAccess2(td);

			// Can't be an enclosing type if they're not in the same module
			if (userType.OwnerModule != td.OwnerModule)
				return GetTypeAccess2(td);

			var tdEncTypes = GetEnclosingTypes(td, true);
			var ourEncTypes = InitializeOurEnclosingTypes();
			int maxChecks = Math.Min(tdEncTypes.Count, ourEncTypes.Count);
			int commonIndex;
			for (commonIndex = 0; commonIndex < maxChecks; commonIndex++) {
				if (tdEncTypes[commonIndex] != ourEncTypes[commonIndex])
					break;
			}

			// If td encloses us, then we have access to td and all its members even if
			// they're private.
			if (commonIndex == tdEncTypes.Count)
				return CheckTypeAccess.Full;

			// If there are no common enclosing types, only check the visibility.
			if (commonIndex == 0)
				return GetTypeAccess2(td);

			// If td's declaring type encloses this, then we have full access to td even if
			// it's private, but only normal access to its members.
			if (commonIndex + 1 == tdEncTypes.Count)
				return CheckTypeAccess.Normal | CheckTypeAccess.FullTypeAccess;

			// Normal visibility checks starting from type after common enclosing type
			for (int i = commonIndex; i < tdEncTypes.Count; i++) {
				var type = tdEncTypes[i];
				if (!IsVisible(type))
					return CheckTypeAccess.None;
			}
			return CheckTypeAccess.Normal;
		}

		CheckTypeAccess GetTypeAccess2(TypeDef td) {
			while (td != null) {
				if (!IsVisible(td))
					return CheckTypeAccess.None;
				td = td.DeclaringType;
			}
			return CheckTypeAccess.Normal;
		}

		/// <summary>
		/// Checks whether <paramref name="td"/> is visible to us without checking whether they
		/// have any common enclosing types.
		/// </summary>
		/// <param name="td">Type</param>
		bool IsVisible(TypeDef td) {
			if (td == null)
				return false;
			if (td == userType)
				return true;

			switch (td.Visibility) {
			case TypeAttributes.NotPublic:
				return IsSameAssemblyOrFriendAssembly(td.OwnerModule);

			case TypeAttributes.Public:
				return true;

			case TypeAttributes.NestedPublic:
				return true;

			case TypeAttributes.NestedPrivate:
				return false;

			case TypeAttributes.NestedFamily:
				return IsSubClassOf(td);

			case TypeAttributes.NestedAssembly:
				return IsSameAssemblyOrFriendAssembly(td.OwnerModule);

			case TypeAttributes.NestedFamANDAssem:
				return IsSubClassOf(td) &&
					IsSameAssemblyOrFriendAssembly(td.OwnerModule);

			case TypeAttributes.NestedFamORAssem:
				return IsSubClassOf(td) ||
					IsSameAssemblyOrFriendAssembly(td.OwnerModule);

			default:
				return false;
			}
		}

		bool IsSameAssemblyOrFriendAssembly(ModuleDef module) {
			if (module == null)
				return false;
			var userModule = userType.OwnerModule;
			if (userModule == null)
				return false;
			if (userModule == module)
				return true;
			if (IsSameAssembly(userModule.Assembly, module.Assembly))
				return true;
			var userAsm = userModule.Assembly;
			if (userAsm != null && userAsm.IsFriendAssemblyOf(module.Assembly))
				return true;

			return false;
		}

		static bool IsSameAssembly(IAssembly asm1, IAssembly asm2) {
			if (asm1 == null || asm2 == null)
				return false;
			if (asm1 == asm2)
				return true;
			return new AssemblyNameComparer(AssemblyNameComparerFlags.All).Equals(new AssemblyNameInfo(asm1), new AssemblyNameInfo(asm2));
		}

		/// <summary>
		/// Checks whether <see cref="userType"/> is a sub class of <paramref name="td"/>
		/// </summary>
		/// <param name="td">Type</param>
		bool IsSubClassOf(TypeDef td) {
			if (td == null)
				return false;
			InitializeBaseTypes();
			return baseTypes.ContainsKey(td);
		}

		void InitializeBaseTypes() {
			if (baseTypesInitialized)
				return;
			if (baseTypes == null)
				baseTypes = new Dictionary<ITypeDefOrRef, bool>(TypeEqualityComparer.Instance);
			baseTypesInitialized = true;

			ITypeDefOrRef baseType = userType;
			while (baseType != null) {
				baseTypes[baseType] = true;
				baseType = baseType.GetBaseType();
			}
		}

		List<TypeDef> InitializeOurEnclosingTypes() {
			if (!enclosingTypesInitialized) {
				userTypeEnclosingTypes = GetEnclosingTypes(userType, false);
				enclosingTypesInitialized = true;
			}
			return userTypeEnclosingTypes;
		}

		/// <summary>
		/// Returns a list of all enclosing types, in order of non-enclosed to most enclosed type
		/// </summary>
		/// <param name="td">Type</param>
		/// <param name="includeInput"><c>true</c> if <paramref name="td"/> should be included</param>
		/// <returns>A list of all enclosing types</returns>
		static List<TypeDef> GetEnclosingTypes(TypeDef td, bool includeInput) {
			var list = new List<TypeDef>();
			if (includeInput && td != null)
				list.Add(td);
			while (td != null) {
				var dt = td.DeclaringType;
				if (dt == null)
					break;
				if (list.Contains(dt))
					break;
				list.Add(dt);
				td = dt;
			}
			list.Reverse();
			return list;
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="FieldDef"/>
		/// </summary>
		/// <param name="fd">The field</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(FieldDef fd) {
			if (fd == null)
				return null;
			var access = GetTypeAccess(fd.DeclaringType);
			if (access == null)
				return null;
			var acc = access.Value;
			if ((acc & CheckTypeAccess.Normal) == 0)
				return false;
			if ((acc & CheckTypeAccess.FullMemberAccess) != 0)
				return true;

			return IsVisible(fd);
		}

		bool IsVisible(FieldDef fd) {
			if (fd == null)
				return false;
			var fdDeclaringType = fd.DeclaringType;
			if (fdDeclaringType == null)
				return false;
			if (userType == fdDeclaringType)
				return true;

			switch (fd.Access) {
			case FieldAttributes.PrivateScope:
				// Private scope aka compiler controlled fields/methods can only be accessed
				// by a Field/Method token. This means they must be in the same module.
				return userType.OwnerModule == fdDeclaringType.OwnerModule;

			case FieldAttributes.Private:
				return false;

			case FieldAttributes.FamANDAssem:
				return IsSubClassOf(fdDeclaringType) &&
					IsSameAssemblyOrFriendAssembly(fdDeclaringType.OwnerModule);

			case FieldAttributes.Assembly:
				return IsSameAssemblyOrFriendAssembly(fdDeclaringType.OwnerModule);

			case FieldAttributes.Family:
				return IsSubClassOf(fdDeclaringType);

			case FieldAttributes.FamORAssem:
				return IsSubClassOf(fdDeclaringType) ||
					IsSameAssemblyOrFriendAssembly(fdDeclaringType.OwnerModule);

			case FieldAttributes.Public:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="MethodDef"/>
		/// </summary>
		/// <param name="md">The method</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(MethodDef md) {
			if (md == null)
				return null;
			var access = GetTypeAccess(md.DeclaringType);
			if (access == null)
				return null;
			var acc = access.Value;
			if ((acc & CheckTypeAccess.Normal) == 0)
				return false;
			if ((acc & CheckTypeAccess.FullMemberAccess) != 0)
				return true;

			return IsVisible(md);
		}

		bool IsVisible(MethodDef md) {
			if (md == null)
				return false;
			var mdDeclaringType = md.DeclaringType;
			if (mdDeclaringType == null)
				return false;
			if (userType == mdDeclaringType)
				return true;

			switch (md.Access) {
			case MethodAttributes.PrivateScope:
				// Private scope aka compiler controlled fields/methods can only be accessed
				// by a Field/Method token. This means they must be in the same module.
				return userType.OwnerModule == mdDeclaringType.OwnerModule;

			case MethodAttributes.Private:
				return false;

			case MethodAttributes.FamANDAssem:
				return IsSubClassOf(mdDeclaringType) &&
					IsSameAssemblyOrFriendAssembly(mdDeclaringType.OwnerModule);

			case MethodAttributes.Assembly:
				return IsSameAssemblyOrFriendAssembly(mdDeclaringType.OwnerModule);

			case MethodAttributes.Family:
				return IsSubClassOf(mdDeclaringType);

			case MethodAttributes.FamORAssem:
				return IsSubClassOf(mdDeclaringType) ||
					IsSameAssemblyOrFriendAssembly(mdDeclaringType.OwnerModule);

			case MethodAttributes.Public:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="mr">The member reference</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(MemberRef mr) {
			if (mr == null)
				return null;

			var ownerModule = mr.OwnerModule;
			if (ownerModule == null)
				return null;

			var parent = mr.Class;

			var td = parent as TypeDef;
			if (td != null)
				return CanAccess(td, mr);

			var tr = parent as TypeRef;
			if (tr != null)
				return CanAccess(tr.Resolve(), mr);

			var ts = parent as TypeSpec;
			if (ts != null)
				return CanAccess(ts.ResolveTypeDef(), mr);

			var md = parent as MethodDef;
			if (md != null)
				return CanAccess(md, mr);

			var mod = parent as ModuleRef;
			if (mod != null)
				return CanAccess(mod, mr);

			return null;
		}

		bool? CanAccess(TypeDef td, MemberRef mr) {
			if (mr == null || td == null)
				return null;

			if (mr.MethodSig != null) {
				var md = td.FindMethodCheckBaseType(mr.Name, mr.MethodSig);
				// If method isn't found, assume it's accessible. It could be a method
				// that is created by the CLR (eg. an array's Get()/Set()/Address() or .ctors)
				if (md == null)
					return true;
				return CanAccess(md);
			}

			if (mr.FieldSig != null)
				return CanAccess(td.FindFieldCheckBaseType(mr.Name, mr.FieldSig));

			return null;
		}

		bool? CanAccess(MethodDef md, MemberRef mr) {
			if (mr == null || md == null)
				return null;
			return CanAccess(md);
		}

		bool? CanAccess(ModuleRef mod, MemberRef mr) {
			if (mr == null || mod == null || mod.OwnerModule == null)
				return null;

			var userModule = userType.OwnerModule;
			if (userModule == null)
				return null;
			if (!IsSameAssembly(userModule.Assembly, mod.OwnerModule.Assembly))
				return false;
			if (userModule.Assembly == null)
				return false;
			var otherMod = userModule.Assembly.FindModule(mod.Name);
			if (otherMod == null)
				return false;
			return CanAccess(otherMod.GlobalType, mr);
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="ts">The type spec</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(TypeSpec ts) {
			return CanAccess(ts.ResolveTypeDef());
		}

		/// <summary>
		/// Checks whether it has access to a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="ms">The method spec</param>
		/// <returns><c>true</c> if it has access to it, <c>false</c> if not, and <c>null</c>
		/// if we can't determine it (eg. we couldn't resolve a type or input was <c>null</c>)</returns>
		public bool? CanAccess(MethodSpec ms) {
			if (ms == null)
				return null;

			var mdr = ms.Method;

			var md = mdr as MethodDef;
			if (md != null)
				return CanAccess(md);

			var mr = mdr as MemberRef;
			if (mr != null)
				return CanAccess(mr);

			return null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0}", userType);
		}
	}
}
