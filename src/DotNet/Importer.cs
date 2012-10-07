using System;
using System.Collections.Generic;
using System.Reflection;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Imports <see cref="Type"/>s, <see cref="ConstructorInfo"/>s, <see cref="MethodInfo"/>s
	/// and <see cref="FieldInfo"/>s as references
	/// </summary>
	public struct Importer {
		ModuleDef ownerModule;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">The module that will own all references</param>
		public Importer(ModuleDef ownerModule) {
			this.ownerModule = ownerModule;
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef Import(Type type) {
			return ImportAsTypeSig(type).ToTypeDefOrRef();
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="requiredModifiers">A list of all required modifiers or <c>null</c></param>
		/// <param name="optionalModifiers">A list of all optional modifiers or <c>null</c></param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef Import(Type type, IList<Type> requiredModifiers, IList<Type> optionalModifiers) {
			return ImportAsTypeSig(type, requiredModifiers, optionalModifiers).ToTypeDefOrRef();
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
			case ElementType.Void:		return ownerModule.CorLibTypes.Void;
			case ElementType.Boolean:	return ownerModule.CorLibTypes.Boolean;
			case ElementType.Char:		return ownerModule.CorLibTypes.Char;
			case ElementType.I1:		return ownerModule.CorLibTypes.SByte;
			case ElementType.U1:		return ownerModule.CorLibTypes.Byte;
			case ElementType.I2:		return ownerModule.CorLibTypes.Int16;
			case ElementType.U2:		return ownerModule.CorLibTypes.UInt16;
			case ElementType.I4:		return ownerModule.CorLibTypes.Int32;
			case ElementType.U4:		return ownerModule.CorLibTypes.UInt32;
			case ElementType.I8:		return ownerModule.CorLibTypes.Int64;
			case ElementType.U8:		return ownerModule.CorLibTypes.UInt64;
			case ElementType.R4:		return ownerModule.CorLibTypes.Single;
			case ElementType.R8:		return ownerModule.CorLibTypes.Double;
			case ElementType.String:	return ownerModule.CorLibTypes.String;
			case ElementType.TypedByRef:return ownerModule.CorLibTypes.TypedReference;
			case ElementType.I:			return ownerModule.CorLibTypes.IntPtr;
			case ElementType.U:			return ownerModule.CorLibTypes.UIntPtr;
			case ElementType.Object:	return ownerModule.CorLibTypes.Object;
			case ElementType.Ptr:		return new PtrSig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.ByRef:		return new ByRefSig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.SZArray:	return new SZArraySig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst));
			case ElementType.Array:		return new ArraySig(ImportAsTypeSig(type.GetElementType(), treatAsGenericInst), (uint)type.GetArrayRank());
			case ElementType.ValueType: return new ValueTypeSig(CreateTypeRef(type));
			case ElementType.Class:		return new ClassSig(CreateTypeRef(type));
			case ElementType.Var:		return new GenericVar((uint)type.GenericParameterPosition);
			case ElementType.MVar:		return new GenericMVar((uint)type.GenericParameterPosition);

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

		TypeRef CreateTypeRef(Type type) {
			if (!type.IsNested)
				return new TypeRefUser(ownerModule, type.Namespace ?? string.Empty, type.Name ?? string.Empty, CreateScopeReference(type));
			return new TypeRefUser(ownerModule, string.Empty, type.Name ?? string.Empty, CreateTypeRef(type.DeclaringType));
		}

		IResolutionScope CreateScopeReference(Type type) {
			if (type == null)
				return null;
			if (ownerModule.Assembly != null) {
				if (UTF8String.ToSystemStringOrEmpty(ownerModule.Assembly.Name).Equals(type.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase)) {
					if (UTF8String.ToSystemStringOrEmpty(ownerModule.Name).Equals(type.Module.ScopeName, StringComparison.OrdinalIgnoreCase))
						return ownerModule;
					return new ModuleRefUser(ownerModule, type.Module.ScopeName);
				}
			}
			var asmName = type.Assembly.GetName();
			var pkt = asmName.GetPublicKeyToken();
			if (pkt == null || pkt.Length == 0)
				pkt = null;
			return new AssemblyRefUser(asmName.Name, asmName.Version, PublicKeyBase.CreatePublicKeyToken(pkt), asmName.CultureInfo.Name);
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

			var ts = ImportAsTypeSig(type, treatAsGenericInst);

			// We don't know the original order of the modifiers.
			// Assume all required modifiers are closer to the real type.
			// Assume all modifiers should be applied in the same order as in the lists.

			if (requiredModifiers != null) {
				foreach (var modifier in requiredModifiers)
					ts = new CModReqdSig(Import(modifier), ts);
			}

			if (optionalModifiers != null) {
				foreach (var modifier in optionalModifiers)
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
			if (methodBase == null)
				return null;

			bool isMethodSpec = methodBase.IsGenericButNotGenericMethodDefinition();
			if (isMethodSpec) {
				var method = Import(methodBase.Module.ResolveMethod(methodBase.MetadataToken)) as IMethodDefOrRef;
				var gim = CreateGenericInstMethodSig(methodBase);
				return new MethodSpecUser(method, gim);
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

				MethodBase origMethod = null;
				try {
					// Get the original method def in case the declaring type is a generic
					// type instance and the method uses at least one generic type parameter.
					origMethod = methodBase.Module.ResolveMethod(methodBase.MetadataToken);
				}
				catch (ArgumentException) {
					// Here if eg. the method is created by the runtime (eg. a multi-dimensional
					// array getter/setter method). The method token is in that case 0x06000000,
					// which is invalid.
					origMethod = methodBase;
				}

				var methodSig = CreateMethodSig(origMethod);
				return new MemberRefUser(ownerModule, methodBase.Name, methodSig, parent);
			}
		}

		MethodSig CreateMethodSig(MethodBase mb) {
			var sig = new MethodSig(GetCallingConvention(mb));

			var mi = mb as MethodInfo;
			if (mi != null)
				sig.RetType = ImportAsTypeSig(mi.ReturnParameter, mb.DeclaringType);
			else
				sig.RetType = ownerModule.CorLibTypes.Void;

			foreach (var p in mb.GetParameters())
				sig.Params.Add(ImportAsTypeSig(p, mb.DeclaringType));

			if (mb.IsGenericMethodDefinition)
				sig.GenParamCount = (uint)mb.GetGenericArguments().Length;

			return sig;
		}

		TypeSig ImportAsTypeSig(ParameterInfo p, Type declaringType) {
			return ImportAsTypeSig(p.ParameterType, p.GetRequiredCustomModifiers(), p.GetOptionalCustomModifiers(), declaringType.MustTreatTypeAsGenericInstType(p.ParameterType));
		}

		static CallingConvention GetCallingConvention(MethodBase mb) {
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

		GenericInstMethodSig CreateGenericInstMethodSig(MethodBase mb) {
			var genMethodArgs = mb.GetGenericArguments();
			var gim = new GenericInstMethodSig(CallingConvention.GenericInst, (uint)genMethodArgs.Length);
			foreach (var gma in genMethodArgs)
				gim.GenericArguments.Add(ImportAsTypeSig(gma));
			return gim;
		}

		IMemberRefParent GetModuleParent(Module module) {
			// If we have no assembly, assume this is a netmodule in the same assembly as module
			bool isSameAssembly = ownerModule.Assembly == null ||
				UTF8String.ToSystemStringOrEmpty(ownerModule.Assembly.Name).Equals(module.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase);
			if (!isSameAssembly)
				return null;
			return new ModuleRefUser(ownerModule, ownerModule.Name);
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public MemberRef Import(FieldInfo fieldInfo) {
			if (fieldInfo == null)
				return null;

			IMemberRefParent parent;
			if (fieldInfo.DeclaringType == null) {
				// It's the global type. We can reference it with a ModuleRef token.
				parent = GetModuleParent(fieldInfo.Module);
			}
			else
				parent = Import(fieldInfo.DeclaringType);
			if (parent == null)
				return null;

			FieldInfo origField = null;
			try {
				// Get the original field def in case the declaring type is a generic
				// type instance and the field uses at least one generic type parameter.
				origField = fieldInfo.Module.ResolveField(fieldInfo.MetadataToken);
			}
			catch (ArgumentException) {
				origField = fieldInfo;
			}

			var fieldSig = new FieldSig(ImportAsTypeSig(origField.FieldType));
			return new MemberRefUser(ownerModule, fieldInfo.Name, fieldSig, parent);
		}
	}
}
