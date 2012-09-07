using System;
using System.Collections.Generic;
using dot10.PE;
using dot10.dotNET.MD;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// A high-level representation of a row in the Method table
	/// </summary>
	public abstract class MethodDef : IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IMethodDefOrRef, IMemberForwarded, ICustomAttributeType, ITypeOrMethodDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Method, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 1; }
		}

		/// <summary>
		/// From column Method.RVA
		/// </summary>
		public abstract RVA RVA { get; set; }

		/// <summary>
		/// From column Method.ImplFlags
		/// </summary>
		public abstract MethodImplAttributes ImplFlags { get; set; }

		/// <summary>
		/// From column Method.Flags
		/// </summary>
		public abstract MethodAttributes Flags { get; set; }

		/// <summary>
		/// From column Method.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Method.Signature
		/// </summary>
		public abstract ISignature Signature { get; set; }

		/// <summary>
		/// From column Method.ParamList
		/// </summary>
		public abstract IList<ParamDef> ParamList { get; }

		/// <summary>
		/// Gets/sets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get { return Signature as MethodSig; }
			set { Signature = value; }
		}
	}

	/// <summary>
	/// A Method row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodDefUser : MethodDef {
		RVA rva;
		MethodImplAttributes implFlags;
		MethodAttributes flags;
		UTF8String name;
		ISignature signature;
		IList<ParamDef> parameters = new List<ParamDef>();

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplFlags {
			get { return implFlags; }
			set { implFlags = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override ISignature Signature {
			get { return signature; }
			set { signature = value; }
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamList {
			get { return parameters; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		public MethodDefUser(UTF8String name)
			: this(name, null, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig)
			: this(name, methodSig, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodAttributes flags)
			: this(name, methodSig, 0, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags)
			: this(name, methodSig, implFlags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags, MethodAttributes flags) {
			this.name = name;
			this.signature = methodSig;
			this.implFlags = implFlags;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		public MethodDefUser(string name)
			: this(name, null, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		public MethodDefUser(string name, MethodSig methodSig)
			: this(name, methodSig, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodAttributes flags)
			: this(name, methodSig, 0, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodImplAttributes implFlags)
			: this(name, methodSig, implFlags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodImplAttributes implFlags, MethodAttributes flags)
			: this(new UTF8String(name), methodSig, implFlags, flags) {
		}
	}

	/// <summary>
	/// Created from a row in the Method table
	/// </summary>
	sealed class MethodDefMD : MethodDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodRow rawRow;

		UserValue<RVA> rva;
		UserValue<MethodImplAttributes> implFlags;
		UserValue<MethodAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<ISignature> signature;
		LazyList<ParamDef> parameters;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva.Value; }
			set { rva.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplFlags {
			get { return implFlags.Value; }
			set { implFlags.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override ISignature Signature {
			get { return signature.Value; }
			set { signature.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamList {
			get {
				if (parameters == null) {
					uint startRid;
					uint num = readerModule.MetaData.GetParamRange(rid, out startRid);
					parameters = new LazyList<ParamDef>((int)num, startRid, rid2 => readerModule.ResolveParam(readerModule.MetaData.ToParamRid(rid2)));
				}
				return parameters;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Method</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public MethodDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Method).Rows < rid)
				throw new BadImageFormatException(string.Format("Method rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			rva.ReadOriginalValue = () => {
				InitializeRawRow();
				return new RVA(rawRow.RVA);
			};
			implFlags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodImplAttributes)rawRow.ImplFlags;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			signature.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Signature);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodRow(rid);
		}
	}
}
