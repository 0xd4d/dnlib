using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSemantics table
	/// </summary>
	public abstract class MethodSemantics : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodSemantics, rid); }
		}

		/// <summary>
		/// From column MethodSemantics.Field
		/// </summary>
		public abstract MethodSemanticsAttributes Semantic { get; set; }

		/// <summary>
		/// From column MethodSemantics.Field
		/// </summary>
		public abstract MethodDef Method { get; set; }

		/// <summary>
		/// From column MethodSemantics.Association
		/// </summary>
		public abstract IHasSemantic Association { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Setter"/> bit
		/// </summary>
		public bool IsSetter {
			get { return (Semantic & MethodSemanticsAttributes.Setter) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.Setter;
				else
					Semantic &= ~MethodSemanticsAttributes.Setter;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Getter"/> bit
		/// </summary>
		public bool IsGetter {
			get { return (Semantic & MethodSemanticsAttributes.Getter) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.Getter;
				else
					Semantic &= ~MethodSemanticsAttributes.Getter;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Other"/> bit
		/// </summary>
		public bool IsOther {
			get { return (Semantic & MethodSemanticsAttributes.Other) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.Other;
				else
					Semantic &= ~MethodSemanticsAttributes.Other;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.AddOn"/> bit
		/// </summary>
		public bool IsAddOn {
			get { return (Semantic & MethodSemanticsAttributes.AddOn) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.AddOn;
				else
					Semantic &= ~MethodSemanticsAttributes.AddOn;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.RemoveOn"/> bit
		/// </summary>
		public bool IsRemoveOn {
			get { return (Semantic & MethodSemanticsAttributes.RemoveOn) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.RemoveOn;
				else
					Semantic &= ~MethodSemanticsAttributes.RemoveOn;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Fire"/> bit
		/// </summary>
		public bool IsFire {
			get { return (Semantic & MethodSemanticsAttributes.Fire) != 0; }
			set {
				if (value)
					Semantic |= MethodSemanticsAttributes.Fire;
				else
					Semantic &= ~MethodSemanticsAttributes.Fire;
			}
		}
	}

	/// <summary>
	/// A MethodSemantics row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodSemanticsUser : MethodSemantics {
		MethodSemanticsAttributes semantic;
		MethodDef method;
		IHasSemantic association;

		/// <inheritdoc/>
		public override MethodSemanticsAttributes Semantic {
			get { return semantic; }
			set { semantic = value; }
		}

		/// <inheritdoc/>
		public override MethodDef Method {
			get { return method; }
			set { method = value; }
		}

		/// <inheritdoc/>
		public override IHasSemantic Association {
			get { return association; }
			set { association = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodSemanticsUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="association">Association</param>
		/// <param name="semantic">Semantic</param>
		public MethodSemanticsUser(MethodDef method, IHasSemantic association, MethodSemanticsAttributes semantic) {
			this.semantic = semantic;
			this.method = method;
			this.association = association;
		}
	}

	/// <summary>
	/// Created from a row in the MethodSemantics table
	/// </summary>
	sealed class MethodSemanticsMD : MethodSemantics {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodSemanticsRow rawRow;

		UserValue<MethodSemanticsAttributes> semantic;
		UserValue<MethodDef> method;
		UserValue<IHasSemantic> association;

		/// <inheritdoc/>
		public override MethodSemanticsAttributes Semantic {
			get { return semantic.Value; }
			set { semantic.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodDef Method {
			get { return method.Value; }
			set { method.Value = value; }
		}

		/// <inheritdoc/>
		public override IHasSemantic Association {
			get { return association.Value; }
			set { association.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodSemantics</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public MethodSemanticsMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.MethodSemantics).Rows < rid)
				throw new BadImageFormatException(string.Format("MethodSemantics rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			semantic.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodSemanticsAttributes)rawRow.Semantic;
			};
			method.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveMethod(rawRow.Method);
			};
			association.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveHasSemantic(rawRow.Association);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodSemanticsRow(rid);
		}
	}
}
