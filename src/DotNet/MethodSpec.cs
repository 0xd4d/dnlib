using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public abstract class MethodSpec : IHasCustomAttribute, IMethod {
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
		public abstract IMethodDefOrRef Method { get; set; }

		/// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		public abstract CallingConventionSig Instantiation { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		MethodSig IMethod.MethodSig {
			get { return Method == null ? null : Method.MethodSig; }
			set { if (Method != null) Method.MethodSig = value; }
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get { return Method == null ? null : Method.Name; }
			set { if (Method != null)Method.Name = value; }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get { return Method == null ? null : Method.DeclaringType; }
		}

		/// <summary>
		/// Gets/sets the generic instance method sig
		/// </summary>
		public GenericInstMethodSig GenericInstMethodSig {
			get { return Instantiation as GenericInstMethodSig; }
			set { Instantiation = value; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsMethod {
			get { return true; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsType {
			get { return false; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = GenericInstMethodSig;
				return sig == null ? 0 : sig.GenericArguments.Count;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var methodGenArgs = GenericInstMethodSig == null ? null : GenericInstMethodSig.GenericArguments;
				var methodDef = Method as MethodDef;
				if (methodDef != null) {
					var declaringType = methodDef.DeclaringType;
					return FullNameCreator.MethodFullName(declaringType == null ? null : declaringType.FullName, methodDef.Name, methodDef.MethodSig, null, methodGenArgs);
				}

				var memberRef = Method as MemberRef;
				if (memberRef != null && memberRef.IsMethodRef)
					return FullNameCreator.MethodFullName(memberRef.GetDeclaringTypeFullName(), memberRef.Name, memberRef.MethodSig, null, methodGenArgs);

				return string.Empty;
			}
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
		IMethodDefOrRef method;
		CallingConventionSig instantiation;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override IMethodDefOrRef Method {
			get { return method; }
			set { method = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Instantiation {
			get { return instantiation; }
			set { instantiation = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

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
	sealed class MethodSpecMD : MethodSpec {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodSpecRow rawRow;

		UserValue<IMethodDefOrRef> method;
		UserValue<CallingConventionSig> instantiation;
		CustomAttributeCollection customAttributeCollection;

		/// <inheritdoc/>
		public override IMethodDefOrRef Method {
			get { return method.Value; }
			set { method.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Instantiation {
			get { return instantiation.Value; }
			set { instantiation.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MethodSpec, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodSpecMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.MethodSpec).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("MethodSpec rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			method.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveMethodDefOrRef(rawRow.Method);
			};
			instantiation.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Instantiation);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodSpecRow(rid);
		}
	}
}
