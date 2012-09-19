using System;
using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	class FullNameCreator {
		const string RECURSION_ERROR_RESULT_STRING = "<<<INFRECURSION>>>";
		const string NULLVALUE = "<<<NULL>>>";
		/// <summary>
		/// Max recursion count. If this is reached, we won't continue, and will use a default value.
		/// </summary>
		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;
		StringBuilder sb;
		bool isReflection;
		GenericArguments genericArguments;

		/// <summary>
		/// Returns the namespace of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeRef typeRef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateNamespace(typeRef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The name</returns>
		public static string Name(TypeRef typeRef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateName(typeRef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeRef typeRef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateFullName(typeRef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeRef typeRef) {
			var fnc = new FullNameCreator(true);
			fnc.CreateAssemblyQualifiedName(typeRef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeRef typeRef) {
			return new FullNameCreator().GetDefinitionAssembly(typeRef);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeRef typeRef) {
			return new FullNameCreator().GetOwnerModule(typeRef);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeDef typeDef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateNamespace(typeDef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The name</returns>
		public static string Name(TypeDef typeDef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateName(typeDef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeDef typeDef, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateFullName(typeDef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeDef typeDef) {
			var fnc = new FullNameCreator(true);
			fnc.CreateAssemblyQualifiedName(typeDef);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeDef typeDef) {
			return new FullNameCreator().GetDefinitionAssembly(typeDef);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeDef typeDef) {
			return new FullNameCreator().GetOwnerModule(typeDef);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeSpec typeSpec, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateNamespace(typeSpec);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The name</returns>
		public static string Name(TypeSpec typeSpec, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateName(typeSpec);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeSpec typeSpec, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateFullName(typeSpec);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeSpec typeSpec) {
			var fnc = new FullNameCreator(true);
			fnc.CreateAssemblyQualifiedName(typeSpec);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeSpec typeSpec) {
			return new FullNameCreator().GetDefinitionAssembly(typeSpec);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeSpec typeSpec) {
			return new FullNameCreator().GetOwnerModule(typeSpec);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeSig typeSig, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateNamespace(typeSig);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The name</returns>
		public static string Name(TypeSig typeSig, bool isReflection) {
			var fnc = new FullNameCreator(isReflection);
			fnc.CreateName(typeSig);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeSig typeSig, bool isReflection) {
			return FullName(typeSig, isReflection, null, null);
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="typeGenArgs">Type generic args or <c>null</c> if none</param>
		/// <param name="methodGenArgs">Method generic args or <c>null</c> if none</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeSig typeSig, bool isReflection, IList<TypeSig> typeGenArgs, IList<TypeSig> methodGenArgs) {
			var fnc = new FullNameCreator(isReflection);
			if (fnc.genericArguments == null && (typeGenArgs != null || methodGenArgs != null))
				fnc.genericArguments = new GenericArguments();
			if (typeGenArgs != null)
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			if (methodGenArgs != null)
				fnc.genericArguments.PushMethodArgs(methodGenArgs);
			fnc.CreateFullName(typeSig);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeSig typeSig) {
			var fnc = new FullNameCreator(true);
			fnc.CreateAssemblyQualifiedName(typeSig);
			return fnc.Result;
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeSig typeSig) {
			return new FullNameCreator().GetDefinitionAssembly(typeSig);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeSig typeSig) {
			return new FullNameCreator().GetOwnerModule(typeSig);
		}

		string Result {
			get { return sb.ToString(); }
		}

		FullNameCreator() {
			this.recursionCounter = 0;
		}

		FullNameCreator(bool isReflection) {
			this.isReflection = isReflection;
			this.recursionCounter = 0;
			this.sb = new StringBuilder();
		}

		/// <summary>
		/// Increments <see cref="recursionCounter"/> if it's not too high. <c>ALL</c> instance methods
		/// that can be called recursively must call this method and <see cref="DecrementRecursionCounter"/>
		/// (if this method returns <c>true</c>)
		/// </summary>
		/// <returns><c>true</c> if it was incremented and caller can continue, <c>false</c> if
		/// it was <c>not</c> incremented and the caller must return to its caller.</returns>
		bool IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				return false;
			recursionCounter++;
			return true;
		}

		/// <summary>
		/// Must be called before returning to caller if <see cref="IncrementRecursionCounter"/>
		/// returned <c>true</c>
		/// </summary>
		void DecrementRecursionCounter() {
#if DEBUG
			if (recursionCounter <= 0)
				throw new InvalidOperationException("recursionCounter <= 0");
#endif
			recursionCounter--;
		}

		void CreateFullName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateFullName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateFullName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateFullName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateNamespace(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateNamespace((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateNamespace((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateNamespace((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateAssemblyQualifiedName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateAssemblyQualifiedName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateAssemblyQualifiedName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateAssemblyQualifiedName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateAssemblyQualifiedName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeRef);
			AddAssemblyName(GetDefinitionAssembly(typeRef));

			DecrementRecursionCounter();
		}

		void CreateFullName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			var enclosingTypeRef = typeRef.ResolutionScope as TypeRef;
			if (enclosingTypeRef != null) {
				CreateFullName(enclosingTypeRef);
				AddNestedTypeSeparator();
			}

			if (AddNamespace(typeRef.Namespace))
				sb.Append('.');
			AddName(typeRef.Name);

			DecrementRecursionCounter();
		}

		void CreateNamespace(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddNamespace(typeRef.Namespace);
		}

		void CreateName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddName(typeRef.Name);
		}

		void CreateAssemblyQualifiedName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeDef);
			AddAssemblyName(GetDefinitionAssembly(typeDef));

			DecrementRecursionCounter();
		}

		void CreateFullName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			if (typeDef.IsNested) {
				var enclosingTypeDef = typeDef.EnclosingType;
				if (enclosingTypeDef != null) {
					CreateFullName(enclosingTypeDef);
					AddNestedTypeSeparator();
				}
				else
					sb.Append("<<<ENCLOSINGTYPENULL>>>");
			}

			if (AddNamespace(typeDef.Namespace))
				sb.Append('.');
			AddName(typeDef.Name);

			DecrementRecursionCounter();
		}

		void CreateNamespace(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddNamespace(typeDef.Namespace);
		}

		void CreateName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddName(typeDef.Name);
		}

		void CreateAssemblyQualifiedName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateAssemblyQualifiedName(typeSpec.TypeSig);
		}

		void CreateFullName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateFullName(typeSpec.TypeSig);
		}

		void CreateNamespace(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateNamespace(typeSpec.TypeSig);
		}

		void CreateName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateName(typeSpec.TypeSig);
		}


		void CreateAssemblyQualifiedName(TypeSig typeSig) {
			if (typeSig == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeSig);
			AddAssemblyName(GetDefinitionAssembly(typeSig));

			DecrementRecursionCounter();
		}
		void CreateFullName(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAMESPACE | TYPESIG_NAME);
		}

		void CreateNamespace(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAMESPACE);
		}

		void CreateName(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAME);
		}

		const int TYPESIG_NAMESPACE = 1;
		const int TYPESIG_NAME = 2;
		void CreateTypeSigName(TypeSig typeSig, int flags) {
			if (typeSig == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!IncrementRecursionCounter()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			if (genericArguments != null)
				typeSig = genericArguments.Resolve(typeSig);

			bool createNamespace = (flags & TYPESIG_NAMESPACE) != 0;
			bool createName = (flags & TYPESIG_NAME) != 0;
			int len = sb.Length;
			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				if (createNamespace && createName)
					CreateFullName(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				else if (createNamespace)
					CreateNamespace(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				else if (createName)
					CreateName(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
				CreateTypeSigName(((PtrSig)typeSig).Next, flags);
				if (createName)
					sb.Append('*');
				break;

			case ElementType.ByRef:
				CreateTypeSigName(((ByRefSig)typeSig).Next, flags);
				if (createName)
					sb.Append('&');
				break;

			case ElementType.Array:
				if (createNamespace)
					CreateNamespace(((NonLeafSig)typeSig).Next);
				if (createName) {
					if (len != sb.Length)
						sb.Append('.');	// We printed the namespace so add a dot
					var arraySig = (ArraySig)typeSig;
					sb.Append('[');
					if (arraySig.Rank == 0)
						sb.Append("<RANK0>");	// Not allowed
					else if (arraySig.Rank == 1)
						sb.Append('*');
					else for (int i = 0; i < (int)arraySig.Rank; i++) {
						if (i != 0)
							sb.Append(',');
						if (!isReflection) {
							const int NO_LOWER = int.MinValue;
							const uint NO_SIZE = uint.MaxValue;
							int lower = i >= arraySig.LowerBounds.Count ? NO_LOWER : arraySig.LowerBounds[i];
							uint size = i >= arraySig.Sizes.Count ? NO_SIZE : arraySig.Sizes[i];
							if (lower != NO_LOWER) {
								sb.Append(lower);
								sb.Append("...");
								if (size != NO_SIZE)
									sb.Append(lower + (int)size - 1);
							}
						}
					}
					sb.Append(']');
				}
				break;

			case ElementType.SZArray:
				CreateTypeSigName(((SZArraySig)typeSig).Next, flags);
				if (createName)
					sb.Append("[]");
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				CreateTypeSigName(((ModifierSig)typeSig).Next, flags);
				break;

			case ElementType.Pinned:
				CreateTypeSigName(((PinnedSig)typeSig).Next, flags);
				break;

			case ElementType.ValueArray:
				if (createNamespace)
					CreateNamespace(((NonLeafSig)typeSig).Next);
				if (createName) {
					if (len != sb.Length)
						sb.Append('.');	// We printed the namespace so add a dot
					var valueArraySig = (ValueArraySig)typeSig;
					sb.Append("ValueArray(");
					CreateTypeSigName(valueArraySig.Next, flags);
					sb.Append(',');
					sb.Append(valueArraySig.Size);
					sb.Append(')');
				}
				break;

			case ElementType.Module:
				if (createNamespace)
					CreateNamespace(((NonLeafSig)typeSig).Next);
				if (createName) {
					if (len != sb.Length)
						sb.Append('.');	// We printed the namespace so add a dot
					var moduleSig = (ModuleSig)typeSig;
					sb.Append("Module(");
					CreateTypeSigName(moduleSig.Next, flags);
					sb.Append(',');
					sb.Append(moduleSig.Index);
					sb.Append(')');
				}
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				var typeGenArgs = genericInstSig.GenericArguments;
				if (genericArguments == null)
					genericArguments = new GenericArguments();
				genericArguments.PushTypeArgs(typeGenArgs);
				if (createNamespace)
					CreateNamespace(genericType == null ? null : genericType.TypeDefOrRef);
				if (createName) {
					if (len != sb.Length)
						sb.Append('.');	// We printed the namespace so add a dot
					CreateName(genericType == null ? null : genericType.TypeDefOrRef);
				}
				genericArguments.PopTypeArgs();
				if (createNamespace && createName) {
					if (isReflection) {
						sb.Append('[');
						for (int i = 0; i < typeGenArgs.Count; i++) {
							if (i != 0)
								sb.Append(',');
							var genArg = typeGenArgs[i];
							sb.Append('[');
							CreateFullName(genArg);
							sb.Append(", ");
							var asm = GetDefinitionAssembly(genArg);
							if (asm == null)
								sb.Append(NULLVALUE);
							else
								sb.Append(EscapeAssemblyName(GetAssemblyName(asm)));
							sb.Append(']');
						}
						sb.Append(']');
					}
					else {
						sb.Append('<');
						for (int i = 0; i < typeGenArgs.Count; i++) {
							if (i != 0)
								sb.Append(',');
							CreateFullName(typeGenArgs[i]);
						}
						sb.Append('>');
					}
				}
				break;

			case ElementType.Var:
				if (createName) {
					sb.Append('!');
					sb.Append(((GenericSig)typeSig).Number);
				}
				break;

			case ElementType.MVar:
				if (createName) {
					sb.Append("!!");
					sb.Append(((GenericSig)typeSig).Number);
				}
				break;

			case ElementType.FnPtr:
				if (createName) {
					//TODO: Move printing methods to this class as well so the same recursion counter is used
					sb.Append(Utils.GetMethodString(null, (UTF8String)null, ((FnPtrSig)typeSig).MethodSig));
				}
				break;

			case ElementType.Sentinel:
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				break;
			}

			DecrementRecursionCounter();
		}

		static string GetAssemblyName(IAssembly assembly) {
			var pk = assembly.PublicKeyOrToken;
			if (pk is PublicKey)
				pk = ((PublicKey)pk).Token;
			return Utils.GetAssemblyNameString(new UTF8String(EscapeAssemblyName(assembly.Name)), assembly.Version, assembly.Locale, pk);
		}

		static string EscapeAssemblyName(UTF8String asmSimplName) {
			return EscapeAssemblyName(UTF8String.ToSystemString(asmSimplName));
		}

		static string EscapeAssemblyName(string asmSimplName) {
			var sb = new StringBuilder(asmSimplName.Length);
			foreach (var c in asmSimplName) {
				if (c == ']')
					sb.Append('\\');
				sb.Append(c);
			}
			return sb.ToString();
		}

		void AddNestedTypeSeparator() {
			if (isReflection)
				sb.Append('+');
			else
				sb.Append('/');
		}

		bool AddNamespace(UTF8String @namespace) {
			if (UTF8String.IsNullOrEmpty(@namespace))
				return false;
			AddIdentifier(@namespace.String);
			return true;
		}

		bool AddName(UTF8String name) {
			if (UTF8String.IsNullOrEmpty(name))
				return false;
			AddIdentifier(name.String);
			return true;
		}

		void AddAssemblyName(IAssembly assembly) {
			sb.Append(", ");
			if (assembly == null)
				sb.Append(NULLVALUE);
			else {
				var pkt = assembly.PublicKeyOrToken;
				if (pkt is PublicKey)
					pkt = ((PublicKey)pkt).Token;
				sb.Append(Utils.GetAssemblyNameString(assembly.Name, assembly.Version, assembly.Locale, pkt));
			}
		}

		void AddIdentifier(string id) {
			if (isReflection) {
				// Periods are not escaped by Reflection, even if they're part of a type name.
				foreach (var c in id) {
					switch (c) {
					case ',':
					case '+':
					case '&':
					case '*':
					case '[':
					case ']':
					case '\\':
						sb.Append('\\');
						break;
					}
					sb.Append(c);
				}
			}
			else
				sb.Append(id);
		}

		IAssembly GetDefinitionAssembly(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				return GetDefinitionAssembly((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				return GetDefinitionAssembly((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				return GetDefinitionAssembly((TypeSpec)typeDefOrRef);
			else
				return null;
		}

		ModuleDef GetOwnerModule(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				return GetOwnerModule((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				return GetOwnerModule((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				return GetOwnerModule((TypeSpec)typeDefOrRef);
			else
				return null;
		}

		IAssembly GetDefinitionAssembly(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			if (!IncrementRecursionCounter())
				return null;
			IAssembly result;

			var scope = typeRef.ResolutionScope;
			if (scope == null)
				result = null;	//TODO: Check ownerModule's ExportedType table
			else if (scope is TypeRef)
				result = GetDefinitionAssembly((TypeRef)scope);
			else if (scope is AssemblyRef)
				result = (AssemblyRef)scope;
			else if (scope is ModuleRef) {
				var ownerModule = GetOwnerModule(typeRef);
				result = ownerModule == null ? null : ownerModule.Assembly;
			}
			else if (scope is ModuleDef)
				result = ((ModuleDef)scope).Assembly;
			else
				result = null;	// Should never be reached

			DecrementRecursionCounter();
			return result;
		}

		ModuleDef GetOwnerModule(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			return typeRef.OwnerModule;
		}

		IAssembly GetDefinitionAssembly(TypeDef typeDef) {
			if (typeDef == null)
				return null;
			return typeDef.DefinitionAssembly;
		}

		ModuleDef GetOwnerModule(TypeDef typeDef) {
			if (typeDef == null)
				return null;
			return typeDef.OwnerModule;
		}

		IAssembly GetDefinitionAssembly(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetDefinitionAssembly(typeSpec.TypeSig);
		}

		ModuleDef GetOwnerModule(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetOwnerModule(typeSpec.TypeSig);
		}

		IAssembly GetDefinitionAssembly(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!IncrementRecursionCounter())
				return null;
			IAssembly result;

			if (genericArguments != null)
				typeSig = genericArguments.Resolve(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetDefinitionAssembly(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetDefinitionAssembly(((NonLeafSig)typeSig).Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				if (genericArguments == null)
					genericArguments = new GenericArguments();
				genericArguments.PushTypeArgs(genericInstSig.GenericArguments);
				result = GetDefinitionAssembly(genericType == null ? null : genericType.TypeDefOrRef);
				genericArguments.PopTypeArgs();
				break;

			case ElementType.Var:
			case ElementType.MVar:
				result = null;
				break;

			case ElementType.FnPtr:
			case ElementType.Sentinel:
				result = null;
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			DecrementRecursionCounter();
			return result;
		}

		ModuleDef GetOwnerModule(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!IncrementRecursionCounter())
				return null;
			ModuleDef result;

			if (genericArguments != null)
				typeSig = genericArguments.Resolve(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetOwnerModule(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetOwnerModule(((NonLeafSig)typeSig).Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				if (genericArguments == null)
					genericArguments = new GenericArguments();
				genericArguments.PushTypeArgs(genericInstSig.GenericArguments);
				result = GetOwnerModule(genericType == null ? null : genericType.TypeDefOrRef);
				genericArguments.PopTypeArgs();
				break;

			case ElementType.Var:
			case ElementType.MVar:
				result = null;
				break;

			case ElementType.FnPtr:
			case ElementType.Sentinel:
				result = null;
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			DecrementRecursionCounter();
			return result;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Result;
		}
	}
}
