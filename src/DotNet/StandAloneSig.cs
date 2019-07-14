// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the StandAloneSig table
	/// </summary>
	public abstract class StandAloneSig : IHasCustomAttribute, IHasCustomDebugInformation, IContainsGenericParameter {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.StandAloneSig, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 11;

		/// <summary>
		/// From column StandAloneSig.Signature
		/// </summary>
		public CallingConventionSig Signature {
			get => signature;
			set => signature = value;
		}
		/// <summary/>
		protected CallingConventionSig signature;

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
		public int HasCustomDebugInformationTag => 11;

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

		/// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get => signature as MethodSig;
			set => signature = value;
		}

		/// <summary>
		/// Gets/sets the locals sig
		/// </summary>
		public LocalSig LocalSig {
			get => signature as LocalSig;
			set => signature = value;
		}

		/// <inheritdoc/>
		public bool ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);
	}

	/// <summary>
	/// A StandAloneSig row created by the user and not present in the original .NET file
	/// </summary>
	public class StandAloneSigUser : StandAloneSig {
		/// <summary>
		/// Default constructor
		/// </summary>
		public StandAloneSigUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localSig">A locals sig</param>
		public StandAloneSigUser(LocalSig localSig) => signature = localSig;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodSig">A method sig</param>
		public StandAloneSigUser(MethodSig methodSig) => signature = methodSig;
	}

	/// <summary>
	/// Created from a row in the StandAloneSig table
	/// </summary>
	sealed class StandAloneSigMD : StandAloneSig, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly GenericParamContext gpContext;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.StandAloneSig, origRid);
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
		/// <param name="readerModule">The module which contains this <c>StandAloneSig</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public StandAloneSigMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.StandAloneSigTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"StandAloneSig rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			bool b = readerModule.TablesStream.TryReadStandAloneSigRow(origRid, out var row);
			Debug.Assert(b);
			signature = readerModule.ReadSignature(row.Signature, gpContext);
		}
	}
}
