using System;
using System.Diagnostics;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Param table
	/// </summary>
	[DebuggerDisplay("{Sequence} {Name}")]
	public abstract class ParamDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Param, rid); }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		public int HasFieldMarshalTag {
			get { return 1; }
		}

		/// <summary>
		/// From column Param.Flags
		/// </summary>
		public abstract ParamAttributes Flags { get; set; }

		/// <summary>
		/// From column Param.Sequence
		/// </summary>
		public abstract ushort Sequence { get; set; }

		/// <summary>
		/// From column Param.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <inheritdoc/>
		public abstract FieldMarshal FieldMarshal { get; set; }

		/// <inheritdoc/>
		public abstract Constant Constant { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.In"/> bit
		/// </summary>
		public bool IsIn {
			get { return (Flags & ParamAttributes.In) != 0; }
			set {
				if (value)
					Flags |= ParamAttributes.In;
				else
					Flags &= ~ParamAttributes.In;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Out"/> bit
		/// </summary>
		public bool IsOut {
			get { return (Flags & ParamAttributes.Out) != 0; }
			set {
				if (value)
					Flags |= ParamAttributes.Out;
				else
					Flags &= ~ParamAttributes.Out;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.Optional"/> bit
		/// </summary>
		public bool IsOptional {
			get { return (Flags & ParamAttributes.Optional) != 0; }
			set {
				if (value)
					Flags |= ParamAttributes.Optional;
				else
					Flags &= ~ParamAttributes.Optional;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get { return (Flags & ParamAttributes.HasDefault) != 0; }
			set {
				if (value)
					Flags |= ParamAttributes.HasDefault;
				else
					Flags &= ~ParamAttributes.HasDefault;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="ParamAttributes.HasFieldMarshal"/> bit
		/// </summary>
		public bool HasFieldMarshal {
			get { return (Flags & ParamAttributes.HasFieldMarshal) != 0; }
			set {
				if (value)
					Flags |= ParamAttributes.HasFieldMarshal;
				else
					Flags &= ~ParamAttributes.HasFieldMarshal;
			}
		}
	}

	/// <summary>
	/// A Param row created by the user and not present in the original .NET file
	/// </summary>
	public class ParamDefUser : ParamDef {
		ParamAttributes flags;
		ushort sequence;
		UTF8String name;
		FieldMarshal fieldMarshal;
		Constant constant;

		/// <inheritdoc/>
		public override ParamAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override ushort Sequence {
			get { return sequence; }
			set { sequence = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override FieldMarshal FieldMarshal {
			get { return fieldMarshal; }
			set { fieldMarshal = value; }
		}

		/// <inheritdoc/>
		public override Constant Constant {
			get { return constant; }
			set { constant = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ParamDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ParamDefUser(UTF8String name)
			: this(name, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		public ParamDefUser(UTF8String name, ushort sequence)
			: this(name, sequence, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		/// <param name="flags">Flags</param>
		public ParamDefUser(UTF8String name, ushort sequence, ParamAttributes flags) {
			this.name = name;
			this.sequence = sequence;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ParamDefUser(string name)
			: this(name, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		public ParamDefUser(string name, ushort sequence)
			: this(name, sequence, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		/// <param name="flags">Flags</param>
		public ParamDefUser(string name, ushort sequence, ParamAttributes flags)
			: this(new UTF8String(name), sequence, flags) {
		}
	}

	/// <summary>
	/// Created from a row in the Param table
	/// </summary>
	sealed class ParamDefMD : ParamDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawParamRow rawRow;

		UserValue<ParamAttributes> flags;
		UserValue<ushort> sequence;
		UserValue<UTF8String> name;
		UserValue<FieldMarshal> fieldMarshal;
		UserValue<Constant> constant;

		/// <inheritdoc/>
		public override ParamAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override ushort Sequence {
			get { return sequence.Value; }
			set { sequence.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override FieldMarshal FieldMarshal {
			get { return fieldMarshal.Value; }
			set { fieldMarshal.Value = value; }
		}

		/// <inheritdoc/>
		public override Constant Constant {
			get { return constant.Value; }
			set { constant.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Param</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ParamDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.Param).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Param rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (ParamAttributes)rawRow.Flags;
			};
			sequence.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Sequence;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			fieldMarshal.ReadOriginalValue = () => {
				return readerModule.ResolveFieldMarshal(readerModule.MetaData.GetFieldMarshalRid(Table.Param, rid));
			};
			constant.ReadOriginalValue = () => {
				return readerModule.ResolveConstant(readerModule.MetaData.GetConstantRid(Table.Param, rid));
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadParamRow(rid);
		}
	}
}
