// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public abstract class MethodSpec : IHasCustomAttribute, IHasCustomDebugInformation, IMethod, IContainsGenericParameter {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.MethodSpec, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 21;

		/// <summary>
		/// From column MethodSpec.Method
		/// </summary>
		public IMethodDefOrRef Method {
			get => method;
			set => method = value;
		}
		/// <summary/>
		protected IMethodDefOrRef method;

		/// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		public CallingConventionSig Instantiation {
			get => instantiation;
			set => instantiation = value;
		}
		/// <summary/>
		protected CallingConventionSig instantiation;

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
		public int HasCustomDebugInformationTag => 21;

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
		MethodSig IMethod.MethodSig {
			get => method?.MethodSig;
			set {
				var m = method;
				if (!(m is null))
					m.MethodSig = value;
			}
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get {
				var m = method;
				return m is null ? UTF8String.Empty : m.Name;
			}
			set {
				var m = method;
				if (!(m is null))
					m.Name = value;
			}
		}

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType => method?.DeclaringType;

		/// <summary>
		/// Gets/sets the generic instance method sig
		/// </summary>
		public GenericInstMethodSig GenericInstMethodSig {
			get => instantiation as GenericInstMethodSig;
			set => instantiation = value;
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters => GenericInstMethodSig?.GenericArguments.Count ?? 0;

		/// <inheritdoc/>
		public ModuleDef Module => method?.Module;

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var methodGenArgs = GenericInstMethodSig?.GenericArguments;
				var m = method;
				if (m is MethodDef methodDef)
					return FullNameFactory.MethodFullName(methodDef.DeclaringType?.FullName, methodDef.Name, methodDef.MethodSig, null, methodGenArgs, null, null);

				if (m is MemberRef memberRef) {
					var methodSig = memberRef.MethodSig;
					if (!(methodSig is null)) {
						var gis = (memberRef.Class as TypeSpec)?.TypeSig as GenericInstSig;
						var typeGenArgs = gis?.GenericArguments;
						return FullNameFactory.MethodFullName(memberRef.GetDeclaringTypeFullName(), memberRef.Name, methodSig, typeGenArgs, methodGenArgs, null, null);
					}
				}

				return string.Empty;
			}
		}

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => true;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => true;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;
		bool IContainsGenericParameter.ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

		/// <inheritdoc/>
		public override string ToString() => FullName;
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
			instantiation = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MethodSpec table
	/// </summary>
	sealed class MethodSpecMD : MethodSpec, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly GenericParamContext gpContext;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.MethodSpec, origRid);
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
		/// <param name="readerModule">The module which contains this <c>MethodSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodSpecMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MethodSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"MethodSpec rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			bool b = readerModule.TablesStream.TryReadMethodSpecRow(origRid, out var row);
			Debug.Assert(b);
			method = readerModule.ResolveMethodDefOrRef(row.Method, gpContext);
			instantiation = readerModule.ReadSignature(row.Instantiation, gpContext);
		}
	}
}
