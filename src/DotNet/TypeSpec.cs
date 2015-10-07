// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeSpec table
	/// </summary>
	public abstract class TypeSpec : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeSpec, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 13; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var ts = TypeSig;
				return ts == null ? 0 : ((IGenericParameterProvider)ts).NumberOfGenericParameters;
			}
		}

		/// <inheritdoc/>
		UTF8String IFullName.Name {
			get {
				var mr = ScopeType;
				return mr == null ? UTF8String.Empty : mr.Name;
			}
			set {
				var mr = ScopeType;
				if (mr != null)
					mr.Name = value;
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType {
			get {
				var sig = TypeSig.RemovePinnedAndModifiers();

				var gis = sig as GenericInstSig;
				if (gis != null)
					sig = gis.GenericType;

				var tdr = sig as TypeDefOrRefSig;
				if (tdr != null) {
					if (tdr.IsTypeDef || tdr.IsTypeRef)
						return tdr.TypeDefOrRef.DeclaringType;
					return null;	// If it's another TypeSpec, just stop. Don't want possible inf recursion.
				}

				return null;
			}
		}

		bool IIsTypeOrMethod.IsType {
			get { return true; }
		}

		bool IIsTypeOrMethod.IsMethod {
			get { return false; }
		}

		bool IMemberRef.IsField {
			get { return false; }
		}

		bool IMemberRef.IsTypeSpec {
			get { return true; }
		}

		bool IMemberRef.IsTypeRef {
			get { return false; }
		}

		bool IMemberRef.IsTypeDef {
			get { return false; }
		}

		bool IMemberRef.IsMethodSpec {
			get { return false; }
		}

		bool IMemberRef.IsMethodDef {
			get { return false; }
		}

		bool IMemberRef.IsMemberRef {
			get { return false; }
		}

		bool IMemberRef.IsFieldDef {
			get { return false; }
		}

		bool IMemberRef.IsPropertyDef {
			get { return false; }
		}

		bool IMemberRef.IsEventDef {
			get { return false; }
		}

		bool IMemberRef.IsGenericParam {
			get { return false; }
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var sig = TypeSig;
				return sig != null && sig.IsValueType;
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive {
			get {
				var sig = TypeSig;
				return sig != null && sig.IsPrimitive;
			}
		}

		/// <inheritdoc/>
		public string TypeName {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		string IType.Namespace {
			get { return FullNameCreator.Namespace(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true); }
		}

		/// <inheritdoc/>
		public string AssemblyQualifiedName {
			get { return FullNameCreator.AssemblyQualifiedName(this); }
		}

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly {
			get { return FullNameCreator.DefinitionAssembly(this); }
		}

		/// <inheritdoc/>
		public IScope Scope {
			get { return FullNameCreator.Scope(this); }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType {
			get { return FullNameCreator.ScopeType(this); }
		}

		/// <inheritdoc/>
		public bool ContainsGenericParameter {
			get { return TypeHelper.ContainsGenericParameter(this); }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return FullNameCreator.OwnerModule(this); }
		}

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
				if (customAttributes == null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() {
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
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
			this.extraData = null;
			this.typeSigAndExtraData_isInitialized = true;
		}
	}

	/// <summary>
	/// Created from a row in the TypeSpec table
	/// </summary>
	sealed class TypeSpecMD : TypeSpec, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly GenericParamContext gpContext;
		readonly uint origRid;
		readonly uint signatureOffset;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override TypeSig GetTypeSigAndExtraData_NoLock(out byte[] extraData) {
			var sig = readerModule.ReadTypeSignature(signatureOffset, gpContext, out extraData);
			if (sig != null)
				sig.Rid = origRid;
			return sig;
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.TypeSpec, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
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
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.TypeSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("TypeSpec rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			this.signatureOffset = readerModule.TablesStream.ReadTypeSpecRow2(origRid);
		}
	}
}
