// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeSpec table
	/// </summary>
	public abstract class TypeSpec : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IHasCustomDebugInformation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.TypeSpec, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag => 2;

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 13;

		/// <inheritdoc/>
		public int MemberRefParentTag => 4;

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var ts = TypeSig;
				return ts is null ? 0 : ((IGenericParameterProvider)ts).NumberOfGenericParameters;
			}
		}

		/// <inheritdoc/>
		UTF8String IFullName.Name {
			get {
				var mr = ScopeType;
				return mr is null ? UTF8String.Empty : mr.Name;
			}
			set {
				var mr = ScopeType;
				if (!(mr is null))
					mr.Name = value;
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType {
			get {
				var sig = TypeSig.RemovePinnedAndModifiers();

				if (sig is GenericInstSig gis)
					sig = gis.GenericType;

				if (sig is TypeDefOrRefSig tdr) {
					if (tdr.IsTypeDef || tdr.IsTypeRef)
						return tdr.TypeDefOrRef.DeclaringType;
					return null;	// If it's another TypeSpec, just stop. Don't want possible inf recursion.
				}

				return null;
			}
		}

		bool IIsTypeOrMethod.IsType => true;
		bool IIsTypeOrMethod.IsMethod => false;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => true;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var sig = TypeSig;
				return !(sig is null) && sig.IsValueType;
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive {
			get {
				var sig = TypeSig;
				return !(sig is null) && sig.IsPrimitive;
			}
		}

		/// <inheritdoc/>
		public string TypeName => FullNameFactory.Name(this, false, null);

		/// <inheritdoc/>
		public string ReflectionName => FullNameFactory.Name(this, true, null);

		/// <inheritdoc/>
		string IType.Namespace => FullNameFactory.Namespace(this, false, null);

		/// <inheritdoc/>
		public string ReflectionNamespace => FullNameFactory.Namespace(this, true, null);

		/// <inheritdoc/>
		public string FullName => FullNameFactory.FullName(this, false, null, null);

		/// <inheritdoc/>
		public string ReflectionFullName => FullNameFactory.FullName(this, true, null, null);

		/// <inheritdoc/>
		public string AssemblyQualifiedName => FullNameFactory.AssemblyQualifiedName(this, null, null);

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly => FullNameFactory.DefinitionAssembly(this);

		/// <inheritdoc/>
		public IScope Scope => FullNameFactory.Scope(this);

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType => FullNameFactory.ScopeType(this);

		/// <inheritdoc/>
		public bool ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

		/// <inheritdoc/>
		public ModuleDef Module => FullNameFactory.OwnerModule(this);

		/// <summary>
		/// From column TypeSpec.Signature
		/// </summary>
		public TypeSig TypeSig {
			get {
				if (!typeSigAndExtraData_isInitialized)
					InitializeTypeSigAndExtraData();
				return typeSig;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				typeSig = value;
				if (!typeSigAndExtraData_isInitialized)
					GetTypeSigAndExtraData_NoLock(out extraData);
				typeSigAndExtraData_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary>
		/// Gets/sets the extra data that was found after the signature
		/// </summary>
		public byte[] ExtraData {
			get {
				if (!typeSigAndExtraData_isInitialized)
					InitializeTypeSigAndExtraData();
				return extraData;
			}
			set {
				if (!typeSigAndExtraData_isInitialized)
					InitializeTypeSigAndExtraData();
				extraData = value;
			}
		}
		/// <summary/>
		protected TypeSig typeSig;
		/// <summary/>
		protected byte[] extraData;
		/// <summary/>
		protected bool typeSigAndExtraData_isInitialized;

		void InitializeTypeSigAndExtraData() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (typeSigAndExtraData_isInitialized)
				return;
			typeSig = GetTypeSigAndExtraData_NoLock(out extraData);
			typeSigAndExtraData_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="typeSig"/></summary>
		protected virtual TypeSig GetTypeSigAndExtraData_NoLock(out byte[] extraData) {
			extraData = null;
			return null;
		}

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes is null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() =>
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;


		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 13;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos is null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() =>
			Interlocked.CompareExchange(ref customDebugInfos, new List<PdbCustomDebugInfo>(), null);
		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A TypeSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeSpecUser : TypeSpec {
		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeSpecUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">A type sig</param>
		public TypeSpecUser(TypeSig typeSig) {
			this.typeSig = typeSig;
			extraData = null;
			typeSigAndExtraData_isInitialized = true;
		}
	}

	/// <summary>
	/// Created from a row in the TypeSpec table
	/// </summary>
	sealed class TypeSpecMD : TypeSpec, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly GenericParamContext gpContext;
		readonly uint signatureOffset;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override TypeSig GetTypeSigAndExtraData_NoLock(out byte[] extraData) {
			var sig = readerModule.ReadTypeSignature(signatureOffset, gpContext, out extraData);
			if (!(sig is null))
				sig.Rid = origRid;
			return sig;
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.TypeSpec, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), gpContext, list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeSpecMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.TypeSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"TypeSpec rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			bool b = readerModule.TablesStream.TryReadTypeSpecRow(origRid, out var row);
			Debug.Assert(b);
			signatureOffset = row.Signature;
		}
	}
}
