// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.PE;
using dnlib.Threading;
using dnlib.W32Resources;

namespace dnlib.DotNet {
	readonly struct ModuleLoader {
		readonly ModuleDef module;
		readonly ICancellationToken cancellationToken;
		readonly Dictionary<object, bool> seen;
		readonly Stack<object> stack;

		ModuleLoader(ModuleDef module, ICancellationToken cancellationToken) {
			const int CAPACITY = 0x4000;
			this.module = module;
			this.cancellationToken = cancellationToken;
			seen = new Dictionary<object, bool>(CAPACITY);
			stack = new Stack<object>(CAPACITY);
		}

		public static void LoadAll(ModuleDef module, ICancellationToken cancellationToken) =>
			new ModuleLoader(module, cancellationToken).Load();

		void Add(UTF8String a) { }
		void Add(Guid? a) { }
		void Add(ushort a) { }
		void Add(AssemblyHashAlgorithm a) { }
		void Add(Version a) { }
		void Add(AssemblyAttributes a) { }
		void Add(PublicKeyBase a) { }
		void Add(RVA a) { }
		void Add(IManagedEntryPoint a) { }
		void Add(string a) { }
		void Add(WinMDStatus a) { }
		void Add(TypeAttributes a) { }
		void Add(FieldAttributes a) { }
		void Add(uint? a) { }
		void Add(byte[] a) { }
		void Add(MethodImplAttributes a) { }
		void Add(MethodAttributes a) { }
		void Add(MethodSemanticsAttributes a) { }
		void Add(ParamAttributes a) { }
		void Add(ElementType a) { }
		void Add(SecurityAction a) { }
		void Add(EventAttributes a) { }
		void Add(PropertyAttributes a) { }
		void Add(PInvokeAttributes a) { }
		void Add(FileAttributes a) { }
		void Add(ManifestResourceAttributes a) { }
		void Add(GenericParamAttributes a) { }
		void Add(NativeType a) { }

		void Load() {
			LoadAllTables();
			Load(module);
			Process();
		}

		void Process() {
			while (stack.Count != 0) {
				if (!(cancellationToken is null))
					cancellationToken.ThrowIfCancellationRequested();
				var o = stack.Pop();
				LoadObj(o);
			}
		}

		void LoadAllTables() {
			var resolver = module as ITokenResolver;
			if (resolver is null)
				return;
			for (Table tbl = 0; tbl <= Table.GenericParamConstraint; tbl++) {
				for (uint rid = 1; ; rid++) {
					var o = resolver.ResolveToken(new MDToken(tbl, rid).Raw, new GenericParamContext());
					if (o is null)
						break;
					Add(o);
					Process();
				}
			}
		}

		void LoadObj(object o) {
			if (o is TypeSig ts) {
				Load(ts);
				return;
			}

			if (o is IMDTokenProvider mdt) {
				Load(mdt);
				return;
			}

			if (o is CustomAttribute ca) {
				Load(ca);
				return;
			}

			if (o is SecurityAttribute sa) {
				Load(sa);
				return;
			}

			if (o is CANamedArgument na) {
				Load(na);
				return;
			}

			if (o is Parameter p) {
				Load(p);
				return;
			}

			if (o is PdbMethod pdbMethod) {
				Load(pdbMethod);
				return;
			}

			if (o is ResourceDirectory rd) {
				Load(rd);
				return;
			}

			if (o is ResourceData rdata) {
				Load(rdata);
				return;
			}

			Debug.Fail("Unknown type");
		}

		void Load(TypeSig ts) {
			if (ts is null)
				return;
			Add(ts.Next);

			switch (ts.ElementType) {
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
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
				Add(((TypeDefOrRefSig)ts).TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
				var vsig = (GenericSig)ts;
				Add(vsig.OwnerType);
				Add(vsig.OwnerMethod);
				break;

			case ElementType.GenericInst:
				var gis = (GenericInstSig)ts;
				Add(gis.GenericType);
				Add(gis.GenericArguments);
				break;

			case ElementType.FnPtr:
				var fpsig = (FnPtrSig)ts;
				Add(fpsig.Signature);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				var cmod = (ModifierSig)ts;
				Add(cmod.Modifier);
				break;

			case ElementType.End:
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.ValueArray:
			case ElementType.SZArray:
			case ElementType.Module:
			case ElementType.Pinned:
			case ElementType.Sentinel:
			case ElementType.R:
			case ElementType.Internal:
			default:
				break;
			}
		}

		void Load(IMDTokenProvider mdt) {
			if (mdt is null)
				return;
			switch (mdt.MDToken.Table) {
			case Table.Module:					Load((ModuleDef)mdt); break;
			case Table.TypeRef:					Load((TypeRef)mdt); break;
			case Table.TypeDef:					Load((TypeDef)mdt); break;
			case Table.Field:					Load((FieldDef)mdt); break;
			case Table.Method:					Load((MethodDef)mdt); break;
			case Table.Param:					Load((ParamDef)mdt); break;
			case Table.InterfaceImpl:			Load((InterfaceImpl)mdt); break;
			case Table.MemberRef:				Load((MemberRef)mdt); break;
			case Table.Constant:				Load((Constant)mdt); break;
			case Table.DeclSecurity:			Load((DeclSecurity)mdt); break;
			case Table.ClassLayout:				Load((ClassLayout)mdt); break;
			case Table.StandAloneSig:			Load((StandAloneSig)mdt); break;
			case Table.Event:					Load((EventDef)mdt); break;
			case Table.Property:				Load((PropertyDef)mdt); break;
			case Table.ModuleRef:				Load((ModuleRef)mdt); break;
			case Table.TypeSpec:				Load((TypeSpec)mdt); break;
			case Table.ImplMap:					Load((ImplMap)mdt); break;
			case Table.Assembly:				Load((AssemblyDef)mdt); break;
			case Table.AssemblyRef:				Load((AssemblyRef)mdt); break;
			case Table.File:					Load((FileDef)mdt); break;
			case Table.ExportedType:			Load((ExportedType)mdt); break;
			case Table.GenericParam:			Load((GenericParam)mdt); break;
			case Table.MethodSpec:				Load((MethodSpec)mdt); break;
			case Table.GenericParamConstraint:	Load((GenericParamConstraint)mdt); break;

			case Table.ManifestResource:
				var rsrc = mdt as Resource;
				if (!(rsrc is null)) {
					Load(rsrc);
					break;
				}

				var mr = mdt as ManifestResource;
				if (!(mr is null)) {
					Load(mr);
					break;
				}

				Debug.Fail("Unknown ManifestResource");
				break;

			case Table.FieldPtr:
			case Table.MethodPtr:
			case Table.ParamPtr:
			case Table.CustomAttribute:
			case Table.FieldMarshal:
			case Table.FieldLayout:
			case Table.EventMap:
			case Table.EventPtr:
			case Table.PropertyMap:
			case Table.PropertyPtr:
			case Table.MethodSemantics:
			case Table.MethodImpl:
			case Table.FieldRVA:
			case Table.ENCLog:
			case Table.ENCMap:
			case Table.AssemblyProcessor:
			case Table.AssemblyOS:
			case Table.AssemblyRefProcessor:
			case Table.AssemblyRefOS:
			case Table.NestedClass:
			case Table.Document:
			case Table.MethodDebugInformation:
			case Table.LocalScope:
			case Table.LocalVariable:
			case Table.LocalConstant:
			case Table.ImportScope:
			case Table.StateMachineMethod:
			case Table.CustomDebugInformation:
				break;

			default:
				Debug.Fail("Unknown type");
				break;
			}
		}

		void Load(ModuleDef obj) {
			if (obj is null || obj != module)
				return;
			Add(obj.Generation);
			Add(obj.Name);
			Add(obj.Mvid);
			Add(obj.EncId);
			Add(obj.EncBaseId);
			Add(obj.CustomAttributes);
			Add(obj.Assembly);
			Add(obj.Types);
			Add(obj.ExportedTypes);
			Add(obj.NativeEntryPoint);
			Add(obj.ManagedEntryPoint);
			Add(obj.Resources);
			Add(obj.VTableFixups);
			Add(obj.Location);
			Add(obj.Win32Resources);
			Add(obj.RuntimeVersion);
			Add(obj.WinMDStatus);
			Add(obj.RuntimeVersionWinMD);
			Add(obj.WinMDVersion);
			Add(obj.PdbState);
		}

		void Load(TypeRef obj) {
			if (obj is null)
				return;
			Add(obj.ResolutionScope);
			Add(obj.Name);
			Add(obj.Namespace);
			Add(obj.CustomAttributes);
		}

		void Load(TypeDef obj) {
			if (obj is null)
				return;
			Add(obj.Module2);
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.Namespace);
			Add(obj.BaseType);
			Add(obj.Fields);
			Add(obj.Methods);
			Add(obj.GenericParameters);
			Add(obj.Interfaces);
			Add(obj.DeclSecurities);
			Add(obj.ClassLayout);
			Add(obj.DeclaringType);
			Add(obj.DeclaringType2);
			Add(obj.NestedTypes);
			Add(obj.Events);
			Add(obj.Properties);
			Add(obj.CustomAttributes);
		}

		void Load(FieldDef obj) {
			if (obj is null)
				return;
			Add(obj.CustomAttributes);
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.Signature);
			Add(obj.FieldOffset);
			Add(obj.MarshalType);
			Add(obj.RVA);
			Add(obj.InitialValue);
			Add(obj.ImplMap);
			Add(obj.Constant);
			Add(obj.DeclaringType);
		}

		void Load(MethodDef obj) {
			if (obj is null)
				return;
			Add(obj.RVA);
			Add(obj.ImplAttributes);
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.Signature);
			Add(obj.ParamDefs);
			Add(obj.GenericParameters);
			Add(obj.DeclSecurities);
			Add(obj.ImplMap);
			Add(obj.MethodBody);
			Add(obj.CustomAttributes);
			Add(obj.Overrides);
			Add(obj.DeclaringType);
			Add(obj.Parameters);
			Add(obj.SemanticsAttributes);
		}

		void Load(ParamDef obj) {
			if (obj is null)
				return;
			Add(obj.DeclaringMethod);
			Add(obj.Attributes);
			Add(obj.Sequence);
			Add(obj.Name);
			Add(obj.MarshalType);
			Add(obj.Constant);
			Add(obj.CustomAttributes);
		}

		void Load(InterfaceImpl obj) {
			if (obj is null)
				return;
			Add(obj.Interface);
			Add(obj.CustomAttributes);
		}

		void Load(MemberRef obj) {
			if (obj is null)
				return;
			Add(obj.Class);
			Add(obj.Name);
			Add(obj.Signature);
			Add(obj.CustomAttributes);
		}

		void Load(Constant obj) {
			if (obj is null)
				return;
			Add(obj.Type);
			var o = obj.Value;
		}

		void Load(DeclSecurity obj) {
			if (obj is null)
				return;
			Add(obj.Action);
			Add(obj.SecurityAttributes);
			Add(obj.CustomAttributes);
			obj.GetBlob();
		}

		void Load(ClassLayout obj) {
			if (obj is null)
				return;
			Add(obj.PackingSize);
			Add(obj.ClassSize);
		}

		void Load(StandAloneSig obj) {
			if (obj is null)
				return;
			Add(obj.Signature);
			Add(obj.CustomAttributes);
		}

		void Load(EventDef obj) {
			if (obj is null)
				return;
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.EventType);
			Add(obj.CustomAttributes);
			Add(obj.AddMethod);
			Add(obj.InvokeMethod);
			Add(obj.RemoveMethod);
			Add(obj.OtherMethods);
			Add(obj.DeclaringType);
		}

		void Load(PropertyDef obj) {
			if (obj is null)
				return;
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.Type);
			Add(obj.Constant);
			Add(obj.CustomAttributes);
			Add(obj.GetMethods);
			Add(obj.SetMethods);
			Add(obj.OtherMethods);
			Add(obj.DeclaringType);
		}

		void Load(ModuleRef obj) {
			if (obj is null)
				return;
			Add(obj.Name);
			Add(obj.CustomAttributes);
		}

		void Load(TypeSpec obj) {
			if (obj is null)
				return;
			Add(obj.TypeSig);
			Add(obj.ExtraData);
			Add(obj.CustomAttributes);
		}

		void Load(ImplMap obj) {
			if (obj is null)
				return;
			Add(obj.Attributes);
			Add(obj.Name);
			Add(obj.Module);
		}

		void Load(AssemblyDef obj) {
			if (obj is null)
				return;
			if (obj.ManifestModule != module)
				return;
			Add(obj.HashAlgorithm);
			Add(obj.Version);
			Add(obj.Attributes);
			Add(obj.PublicKey);
			Add(obj.Name);
			Add(obj.Culture);
			Add(obj.DeclSecurities);
			Add(obj.Modules);
			Add(obj.CustomAttributes);
		}

		void Load(AssemblyRef obj) {
			if (obj is null)
				return;
			Add(obj.Version);
			Add(obj.Attributes);
			Add(obj.PublicKeyOrToken);
			Add(obj.Name);
			Add(obj.Culture);
			Add(obj.Hash);
			Add(obj.CustomAttributes);
		}

		void Load(FileDef obj) {
			if (obj is null)
				return;
			Add(obj.Flags);
			Add(obj.Name);
			Add(obj.HashValue);
			Add(obj.CustomAttributes);
		}

		void Load(ExportedType obj) {
			if (obj is null)
				return;
			Add(obj.CustomAttributes);
			Add(obj.Attributes);
			Add(obj.TypeDefId);
			Add(obj.TypeName);
			Add(obj.TypeNamespace);
			Add(obj.Implementation);
		}

		void Load(Resource obj) {
			if (obj is null)
				return;

			Add(obj.Offset);
			Add(obj.Name);
			Add(obj.Attributes);

			switch (obj.ResourceType) {
			case ResourceType.Embedded:
				break;

			case ResourceType.AssemblyLinked:
				var ar = (AssemblyLinkedResource)obj;
				Add(ar.Assembly);
				break;

			case ResourceType.Linked:
				var lr = (LinkedResource)obj;
				Add(lr.File);
				Add(lr.Hash);
				break;

			default:
				Debug.Fail("Unknown resource");
				break;
			}
		}

		void Load(ManifestResource obj) {
			if (obj is null)
				return;
			Add(obj.Offset);
			Add(obj.Flags);
			Add(obj.Name);
			Add(obj.Implementation);
			Add(obj.CustomAttributes);
		}

		void Load(GenericParam obj) {
			if (obj is null)
				return;
			Add(obj.Owner);
			Add(obj.Number);
			Add(obj.Flags);
			Add(obj.Name);
			Add(obj.Kind);
			Add(obj.GenericParamConstraints);
			Add(obj.CustomAttributes);
		}

		void Load(MethodSpec obj) {
			if (obj is null)
				return;
			Add(obj.Method);
			Add(obj.Instantiation);
			Add(obj.CustomAttributes);
		}

		void Load(GenericParamConstraint obj) {
			if (obj is null)
				return;
			Add(obj.Owner);
			Add(obj.Constraint);
			Add(obj.CustomAttributes);
		}

		void Load(CANamedArgument obj) {
			if (obj is null)
				return;
			Add(obj.Type);
			Add(obj.Name);
			Load(obj.Argument);
		}

		void Load(Parameter obj) {
			if (obj is null)
				return;
			Add(obj.Type);
		}

		void Load(SecurityAttribute obj) {
			if (obj is null)
				return;
			Add(obj.AttributeType);
			Add(obj.NamedArguments);
		}

		void Load(CustomAttribute obj) {
			if (obj is null)
				return;
			Add(obj.Constructor);
			Add(obj.RawData);
			Add(obj.ConstructorArguments);
			Add(obj.NamedArguments);
		}

		void Load(MethodOverride obj) {
			Add(obj.MethodBody);
			Add(obj.MethodDeclaration);
		}

		void AddCAValue(object obj) {
			if (obj is CAArgument) {
				Load((CAArgument)obj);
				return;
			}

			if (obj is IList<CAArgument> list) {
				Add(list);
				return;
			}

			if (obj is IMDTokenProvider md) {
				Add(md);
				return;
			}
		}

		void Load(CAArgument obj) {
			Add(obj.Type);
			AddCAValue(obj.Value);
		}

		void Load(PdbMethod obj) { }

		void Load(ResourceDirectory obj) {
			if (obj is null)
				return;
			Add(obj.Directories);
			Add(obj.Data);
		}

		void Load(ResourceData obj) { }

		void AddToStack<T>(T t) where T : class {
			if (t is null)
				return;
			if (seen.ContainsKey(t))
				return;
			seen[t] = true;
			stack.Push(t);
		}

		void Add(CustomAttribute obj) => AddToStack(obj);
		void Add(SecurityAttribute obj) => AddToStack(obj);
		void Add(CANamedArgument obj) => AddToStack(obj);
		void Add(Parameter obj) => AddToStack(obj);
		void Add(IMDTokenProvider o) => AddToStack(o);
		void Add(PdbMethod pdbMethod) { }
		void Add(TypeSig ts) => AddToStack(ts);
		void Add(ResourceDirectory rd) => AddToStack(rd);
		void Add(ResourceData rd) => AddToStack(rd);

		void Add<T>(IList<T> list) where T : IMDTokenProvider {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<TypeSig> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<CustomAttribute> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<SecurityAttribute> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<MethodOverride> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Load(item);
		}

		void Add(IList<CAArgument> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Load(item);
		}

		void Add(IList<CANamedArgument> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(ParameterList list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<Instruction> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<ExceptionHandler> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<Local> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<ResourceDirectory> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(IList<ResourceData> list) {
			if (list is null)
				return;
			foreach (var item in list)
				Add(item);
		}

		void Add(VTableFixups vtf) {
			if (vtf is null)
				return;
			foreach (var fixup in vtf) {
				foreach (var method in fixup)
					Add(method);
			}
		}

		void Add(Win32Resources vtf) {
			if (vtf is null)
				return;
			Add(vtf.Root);
		}

		void Add(CallingConventionSig sig) {
			if (sig is MethodBaseSig msig) {
				Add(msig);
				return;
			}

			if (sig is FieldSig fsig) {
				Add(fsig);
				return;
			}

			if (sig is LocalSig lsig) {
				Add(lsig);
				return;
			}

			if (sig is GenericInstMethodSig gsig) {
				Add(gsig);
				return;
			}

			Debug.Assert(sig is null);
		}

		void Add(MethodBaseSig msig) {
			if (msig is null)
				return;
			Add(msig.ExtraData);
			Add(msig.RetType);
			Add(msig.Params);
			Add(msig.ParamsAfterSentinel);
		}

		void Add(FieldSig fsig) {
			if (fsig is null)
				return;
			Add(fsig.ExtraData);
			Add(fsig.Type);
		}

		void Add(LocalSig lsig) {
			if (lsig is null)
				return;
			Add(lsig.ExtraData);
			Add(lsig.Locals);
		}

		void Add(GenericInstMethodSig gsig) {
			if (gsig is null)
				return;
			Add(gsig.ExtraData);
			Add(gsig.GenericArguments);
		}

		void Add(MarshalType mt) {
			if (mt is null)
				return;
			Add(mt.NativeType);
		}

		void Add(MethodBody mb) {
			if (mb is CilBody cilBody) {
				Add(cilBody);
				return;
			}

			if (mb is NativeMethodBody nb) {
				Add(nb);
				return;
			}

			Debug.Assert(mb is null, "Unknown method body");
		}

		void Add(NativeMethodBody body) {
			if (body is null)
				return;
			Add(body.RVA);
		}

		void Add(CilBody body) {
			if (body is null)
				return;
			Add(body.Instructions);
			Add(body.ExceptionHandlers);
			Add(body.Variables);
			Add(body.PdbMethod);
		}

		void Add(Instruction instr) {
			if (instr is null)
				return;

			if (instr.Operand is IMDTokenProvider mdt) {
				Add(mdt);
				return;
			}

			if (instr.Operand is Parameter p) {
				Add(p);
				return;
			}

			if (instr.Operand is Local l) {
				Add(l);
				return;
			}

			if (instr.Operand is CallingConventionSig csig) {
				Add(csig);
				return;
			}
		}

		void Add(ExceptionHandler eh) {
			if (eh is null)
				return;
			Add(eh.CatchType);
		}

		void Add(Local local) {
			if (local is null)
				return;
			Add(local.Type);
		}

		void Add(PdbState state) {
			if (state is null)
				return;
			Add(state.UserEntryPoint);
		}
	}
}
