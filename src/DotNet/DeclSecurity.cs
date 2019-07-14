// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the DeclSecurity table
	/// </summary>
	[DebuggerDisplay("{Action} Count={SecurityAttributes.Count}")]
	public abstract class DeclSecurity : IHasCustomAttribute, IHasCustomDebugInformation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.DeclSecurity, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 8;

		/// <summary>
		/// From column DeclSecurity.Action
		/// </summary>
		public SecurityAction Action {
			get => action;
			set => action = value;
		}
		/// <summary/>
		protected SecurityAction action;

		/// <summary>
		/// From column DeclSecurity.PermissionSet
		/// </summary>
		public IList<SecurityAttribute> SecurityAttributes {
			get {
				if (securityAttributes is null)
					InitializeSecurityAttributes();
				return securityAttributes;
			}
		}
		/// <summary/>
		protected IList<SecurityAttribute> securityAttributes;
		/// <summary>Initializes <see cref="securityAttributes"/></summary>
		protected virtual void InitializeSecurityAttributes() =>
			Interlocked.CompareExchange(ref securityAttributes, new List<SecurityAttribute>(), null);

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
		public int HasCustomDebugInformationTag => 8;

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
		/// <c>true</c> if <see cref="SecurityAttributes"/> is not empty
		/// </summary>
		public bool HasSecurityAttributes => SecurityAttributes.Count > 0;

		/// <summary>
		/// Gets the blob data or <c>null</c> if there's none
		/// </summary>
		/// <returns>Blob data or <c>null</c></returns>
		public abstract byte[] GetBlob();

		/// <summary>
		/// Returns the .NET 1.x XML string or null if it's not a .NET 1.x format
		/// </summary>
		/// <returns></returns>
		public string GetNet1xXmlString() => GetNet1xXmlStringInternal(SecurityAttributes);

		internal static string GetNet1xXmlStringInternal(IList<SecurityAttribute> secAttrs) {
			if (secAttrs is null || secAttrs.Count != 1)
				return null;
			var sa = secAttrs[0];
			if (sa is null || sa.TypeFullName != "System.Security.Permissions.PermissionSetAttribute")
				return null;
			if (sa.NamedArguments.Count != 1)
				return null;
			var na = sa.NamedArguments[0];
			if (na is null || !na.IsProperty || na.Name != "XML")
				return null;
			if (na.ArgumentType.GetElementType() != ElementType.String)
				return null;
			var arg = na.Argument;
			if (arg.Type.GetElementType() != ElementType.String)
				return null;
			var utf8 = arg.Value as UTF8String;
			if (!(utf8 is null))
				return utf8;
			if (arg.Value is string s)
				return s;
			return null;
		}
	}

	/// <summary>
	/// A DeclSecurity row created by the user and not present in the original .NET file
	/// </summary>
	public class DeclSecurityUser : DeclSecurity {
		/// <summary>
		/// Default constructor
		/// </summary>
		public DeclSecurityUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action">The security action</param>
		/// <param name="securityAttrs">The security attributes (now owned by this)</param>
		public DeclSecurityUser(SecurityAction action, IList<SecurityAttribute> securityAttrs) {
			this.action = action;
			securityAttributes = securityAttrs;
		}

		/// <inheritdoc/>
		public override byte[] GetBlob() => null;
	}

	/// <summary>
	/// Created from a row in the DeclSecurity table
	/// </summary>
	sealed class DeclSecurityMD : DeclSecurity, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly uint permissionSet;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeSecurityAttributes() {
			var gpContext = new GenericParamContext();
			var tmp = DeclSecurityReader.Read(readerModule, permissionSet, gpContext);
			Interlocked.CompareExchange(ref securityAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.DeclSecurity, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			var gpContext = new GenericParamContext();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), gpContext, list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>DeclSecurity</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public DeclSecurityMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.DeclSecurityTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"DeclSecurity rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadDeclSecurityRow(origRid, out var row);
			Debug.Assert(b);
			permissionSet = row.PermissionSet;
			action = (SecurityAction)row.Action;
		}

		/// <inheritdoc/>
		public override byte[] GetBlob() => readerModule.BlobStream.Read(permissionSet);
	}
}
