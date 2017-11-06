// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the File table
	/// </summary>
	public abstract class FileDef : IHasCustomAttribute, IImplementation, IHasCustomDebugInformation, IManagedEntryPoint {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.File, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 16; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 0; }
		}

		/// <summary>
		/// From column File.Flags
		/// </summary>
		public FileAttributes Flags {
			get { return (FileAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column File.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column File.HashValue
		/// </summary>
		public byte[] HashValue {
			get { return hashValue; }
			set { hashValue = value; }
		}
		/// <summary/>
		protected byte[] hashValue;

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
		public int HasCustomDebugInformationTag {
			get { return 16; }
		}

		/// <inheritdoc/>
		public bool HasCustomDebugInfos {
			get { return CustomDebugInfos.Count > 0; }
		}

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos == null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() {
			Interlocked.CompareExchange(ref customDebugInfos, ThreadSafeListCreator.Create<PdbCustomDebugInfo>(), null);
		}

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, FileAttributes flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="FileAttributes.ContainsMetaData"/> bit
		/// </summary>
		public bool ContainsMetaData {
			get { return ((FileAttributes)attributes & FileAttributes.ContainsNoMetaData) == 0; }
			set { ModifyAttributes(!value, FileAttributes.ContainsNoMetaData); }
		}

		/// <summary>
		/// Gets/sets the <see cref="FileAttributes.ContainsNoMetaData"/> bit
		/// </summary>
		public bool ContainsNoMetaData {
			get { return ((FileAttributes)attributes & FileAttributes.ContainsNoMetaData) != 0; }
			set { ModifyAttributes(value, FileAttributes.ContainsNoMetaData); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(name); }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A File row created by the user and not present in the original .NET file
	/// </summary>
	public class FileDefUser : FileDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public FileDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of file</param>
		/// <param name="flags">Flags</param>
		/// <param name="hashValue">File hash</param>
		public FileDefUser(UTF8String name, FileAttributes flags, byte[] hashValue) {
			this.name = name;
			this.attributes = (int)flags;
			this.hashValue = hashValue;
		}
	}

	/// <summary>
	/// Created from a row in the File table
	/// </summary>
	sealed class FileDefMD : FileDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.File, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>File</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FileDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.FileTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("File rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint name;
			uint hashValue = readerModule.TablesStream.ReadFileRow(origRid, out this.attributes, out name);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.hashValue = readerModule.BlobStream.Read(hashValue);
		}
	}
}
