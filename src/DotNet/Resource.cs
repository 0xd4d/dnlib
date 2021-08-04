// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Threading;
using dnlib.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet {
	/// <summary>
	/// Type of resource
	/// </summary>
	public enum ResourceType {
		/// <summary>
		/// It's a <see cref="EmbeddedResource"/>
		/// </summary>
		Embedded,

		/// <summary>
		/// It's a <see cref="AssemblyLinkedResource"/>
		/// </summary>
		AssemblyLinked,

		/// <summary>
		/// It's a <see cref="LinkedResource"/>
		/// </summary>
		Linked,
	}

	/// <summary>
	/// Resource base class
	/// </summary>
	public abstract class Resource : IMDTokenProvider, IHasCustomAttribute, IHasCustomDebugInformation {
		private protected uint rid;
		private protected uint? offset;
		UTF8String name;
		ManifestResourceAttributes flags;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ManifestResource, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <summary>
		/// Gets/sets the offset of the resource
		/// </summary>
		public uint? Offset {
			get => offset;
			set => offset = value;
		}

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Gets/sets the flags
		/// </summary>
		public ManifestResourceAttributes Attributes {
			get => flags;
			set => flags = value;
		}

		/// <summary>
		/// Gets the type of resource
		/// </summary>
		public abstract ResourceType ResourceType { get; }

		/// <summary>
		/// Gets/sets the visibility
		/// </summary>
		public ManifestResourceAttributes Visibility {
			get => flags & ManifestResourceAttributes.VisibilityMask;
			set => flags = (flags & ~ManifestResourceAttributes.VisibilityMask) | (value & ManifestResourceAttributes.VisibilityMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic => (flags & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public;

		/// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate => (flags & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private;

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 18;

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
		public int HasCustomDebugInformationTag => 18;

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
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="flags">flags</param>
		protected Resource(UTF8String name, ManifestResourceAttributes flags) {
			this.name = name;
			this.flags = flags;
		}
	}

	/// <summary>
	/// A resource that is embedded in a .NET module. This is the most common type of resource.
	/// </summary>
	public class EmbeddedResource : Resource {
		readonly DataReaderFactory dataReaderFactory;
		readonly uint resourceStartOffset;
		readonly uint resourceLength;

		/// <summary>
		/// Gets the length of the data
		/// </summary>
		public uint Length => resourceLength;

		/// <inheritdoc/>
		public override ResourceType ResourceType => ResourceType.Embedded;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="data">Resource data</param>
		/// <param name="flags">Resource flags</param>
		public EmbeddedResource(UTF8String name, byte[] data, ManifestResourceAttributes flags = ManifestResourceAttributes.Private)
			: this(name, ByteArrayDataReaderFactory.Create(data, filename: null), 0, (uint)data.Length, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="dataReaderFactory">Data reader factory</param>
		/// <param name="offset">Offset of resource data</param>
		/// <param name="length">Length of resource data</param>
		/// <param name="flags">Resource flags</param>
		public EmbeddedResource(UTF8String name, DataReaderFactory dataReaderFactory, uint offset, uint length, ManifestResourceAttributes flags = ManifestResourceAttributes.Private)
			: base(name, flags) {
			this.dataReaderFactory = dataReaderFactory ?? throw new ArgumentNullException(nameof(dataReaderFactory));
			resourceStartOffset = offset;
			resourceLength = length;
		}

		/// <summary>
		/// Gets a data reader that can access the resource
		/// </summary>
		/// <returns></returns>
		public DataReader CreateReader() => dataReaderFactory.CreateReader(resourceStartOffset, resourceLength);

		/// <inheritdoc/>
		public override string ToString() => $"{UTF8String.ToSystemStringOrEmpty(Name)} - size: {(resourceLength)}";
	}

	sealed class EmbeddedResourceMD : EmbeddedResource, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.ManifestResource, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		public EmbeddedResourceMD(ModuleDefMD readerModule, ManifestResource mr, byte[] data)
			: this(readerModule, mr, ByteArrayDataReaderFactory.Create(data, filename: null), 0, (uint)data.Length) {
		}

		public EmbeddedResourceMD(ModuleDefMD readerModule, ManifestResource mr, DataReaderFactory dataReaderFactory, uint offset, uint length)
			: base(mr.Name, dataReaderFactory, offset, length, mr.Flags) {
			this.readerModule = readerModule;
			origRid = rid = mr.Rid;
			this.offset = mr.Offset;
		}
	}

	/// <summary>
	/// A reference to a resource in another assembly
	/// </summary>
	public class AssemblyLinkedResource : Resource {
		AssemblyRef asmRef;

		/// <inheritdoc/>
		public override ResourceType ResourceType => ResourceType.AssemblyLinked;

		/// <summary>
		/// Gets/sets the assembly reference
		/// </summary>
		public AssemblyRef Assembly {
			get => asmRef;
			set => asmRef = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="asmRef">Assembly reference</param>
		/// <param name="flags">Resource flags</param>
		public AssemblyLinkedResource(UTF8String name, AssemblyRef asmRef, ManifestResourceAttributes flags)
			: base(name, flags) => this.asmRef = asmRef ?? throw new ArgumentNullException(nameof(asmRef));

		/// <inheritdoc/>
		public override string ToString() => $"{UTF8String.ToSystemStringOrEmpty(Name)} - assembly: {asmRef.FullName}";
	}

	sealed class AssemblyLinkedResourceMD : AssemblyLinkedResource, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.ManifestResource, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		public AssemblyLinkedResourceMD(ModuleDefMD readerModule, ManifestResource mr, AssemblyRef asmRef) : base(mr.Name, asmRef, mr.Flags) {
			this.readerModule = readerModule;
			origRid = rid = mr.Rid;
			offset = mr.Offset;
		}
	}

	/// <summary>
	/// A resource that is stored in a file on disk
	/// </summary>
	public class LinkedResource : Resource {
		FileDef file;

		/// <inheritdoc/>
		public override ResourceType ResourceType => ResourceType.Linked;

		/// <summary>
		/// Gets/sets the file
		/// </summary>
		public FileDef File {
			get => file;
			set => file = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets/sets the hash
		/// </summary>
		public byte[] Hash {
			get => file.HashValue;
			set => file.HashValue = value;
		}

		/// <summary>
		/// Gets/sets the file name
		/// </summary>
		public UTF8String FileName => file is null ? UTF8String.Empty : file.Name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="file">The file</param>
		/// <param name="flags">Resource flags</param>
		public LinkedResource(UTF8String name, FileDef file, ManifestResourceAttributes flags)
			: base(name, flags) => this.file = file;

		/// <inheritdoc/>
		public override string ToString() => $"{UTF8String.ToSystemStringOrEmpty(Name)} - file: {UTF8String.ToSystemStringOrEmpty(FileName)}";
	}

	sealed class LinkedResourceMD : LinkedResource, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.ManifestResource, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		public LinkedResourceMD(ModuleDefMD readerModule, ManifestResource mr, FileDef file) : base(mr.Name, file, mr.Flags) {
			this.readerModule = readerModule;
			origRid = rid = mr.Rid;
			offset = mr.Offset;
		}
	}
}
