// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public abstract class MethodSpec : IHasCustomAttribute, IMethod, IContainsGenericParameter {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodSpec, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 21; }
		}

		/// <summary>
		/// From column MethodSpec.Method
		/// </summary>
		public IMethodDefOrRef Method {
			get { return method; }
			set { method = value; }
		}
		/// <summary/>
		protected IMethodDefOrRef method;

		/// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		public CallingConventionSig Instantiation {
			get { return instantiation; }
			set { instantiation = value; }
		}
		/// <summary/>
		protected CallingConventionSig instantiation;

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
		MethodSig IMethod.MethodSig {
			get {
				var m = method;
				return m == null ? null : m.MethodSig;
			}
			set {
				var m = method;
				if (m != null)
					m.MethodSig = value;
			}
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get {
				var m = method;
				return m == null ? UTF8String.Empty : m.Name;
			}
			set {
				var m = method;
				if (m != null)
					m.Name = value;
			}
		}

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get {
				var m = method;
				return m == null ? null : m.DeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the generic instance method sig
		/// </summary>
		public GenericInstMethodSig GenericInstMethodSig {
			get { return instantiation as GenericInstMethodSig; }
			set { instantiation = value; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = GenericInstMethodSig;
				return sig == null ? 0 : sig.GenericArguments.Count;
			}
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var m = method;
				return m == null ? null : m.Module;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var gims = GenericInstMethodSig;
				var methodGenArgs = gims == null ? null : gims.GenericArguments;
				var m = method;
				var methodDef = m as MethodDef;
				if (methodDef != null) {
					var declaringType = methodDef.DeclaringType;
					return FullNameCreator.MethodFullName(declaringType == null ? null : declaringType.FullName, methodDef.Name, methodDef.MethodSig, null, methodGenArgs);
				}

				var memberRef = m as MemberRef;
				if (memberRef != null) {
					var methodSig = memberRef.MethodSig;
					if (methodSig != null) {
						var tsOwner = memberRef.Class as TypeSpec;
						var gis = tsOwner == null ? null : tsOwner.TypeSig as GenericInstSig;
						var typeGenArgs = gis == null ? null : gis.GenericArguments;
						return FullNameCreator.MethodFullName(memberRef.GetDeclaringTypeFullName(), memberRef.Name, methodSig, typeGenArgs, methodGenArgs);
					}
				}

				return string.Empty;
			}
		}

		bool IIsTypeOrMethod.IsType {
			get { return false; }
		}

		bool IIsTypeOrMethod.IsMethod {
			get { return true; }
		}

		bool IMemberRef.IsField {
			get { return false; }
		}

		bool IMemberRef.IsTypeSpec {
			get { return false; }
		}

		bool IMemberRef.IsTypeRef {
			get { return false; }
		}

		bool IMemberRef.IsTypeDef {
			get { return false; }
		}

		bool IMemberRef.IsMethodSpec {
			get { return true; }
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

		bool IContainsGenericParameter.ContainsGenericParameter {
			get { return TypeHelper.ContainsGenericParameter(this); }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MethodSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodSpecUser : MethodSpec {
		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodSpecUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		public MethodSpecUser(IMethodDefOrRef method)
			: this(method, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		/// <param name="sig">The instantiated method sig</param>
		public MethodSpecUser(IMethodDefOrRef method, GenericInstMethodSig sig) {
			this.method = method;
			this.instantiation = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MethodSpec table
	/// </summary>
	sealed class MethodSpecMD : MethodSpec, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MethodSpec, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodSpecMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MethodSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("MethodSpec rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint method;
			uint instantiation = readerModule.TablesStream.ReadMethodSpecRow(origRid, out method);
			this.method = readerModule.ResolveMethodDefOrRef(method, gpContext);
			this.instantiation = readerModule.ReadSignature(instantiation, gpContext);
		}
	}
}
