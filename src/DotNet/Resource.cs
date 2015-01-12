// dnlib: See LICENSE.txt for more info

﻿using System;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;
using dnlib.Threading;

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
	public abstract class Resource : IDisposable, IMDTokenProvider {
		uint rid;
		uint? offset;
		UTF8String name;
		ManifestResourceAttributes flags;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ManifestResource, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// Gets/sets the offset of the resource
		/// </summary>
		public uint? Offset {
			get { return offset; }
			set { offset = value; }
		}

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets/sets the flags
		/// </summary>
		public ManifestResourceAttributes Attributes {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// Gets the type of resource
		/// </summary>
		public abstract ResourceType ResourceType { get; }

		/// <summary>
		/// Gets/sets the visibility
		/// </summary>
		public ManifestResourceAttributes Visibility {
			get { return flags & ManifestResourceAttributes.VisibilityMask; }
			set { flags = (flags & ~ManifestResourceAttributes.VisibilityMask) | (value & ManifestResourceAttributes.VisibilityMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (flags & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate {
			get { return (flags & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="flags">flags</param>
		protected Resource(UTF8String name, ManifestResourceAttributes flags) {
			this.name = name;
			this.flags = flags;
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
		}
	}

	/// <summary>
	/// A resource that is embedded in a .NET module. This is the most common type of resource.
	/// </summary>
	public sealed class EmbeddedResource : Resource {
		IImageStream dataStream;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override ResourceType ResourceType {
			get { return ResourceType.Embedded; }
		}

		/// <summary>
		/// Gets/sets the resource data. It's never <c>null</c>.
		/// </summary>
		public IImageStream Data {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return dataStream;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (value == dataStream)
					return;
				if (dataStream != null)
					dataStream.Dispose();
				dataStream = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="data">Resource data</param>
		public EmbeddedResource(UTF8String name, byte[] data)
			: this(name, data, ManifestResourceAttributes.Private) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="data">Resource data</param>
		/// <param name="flags">Resource flags</param>
		public EmbeddedResource(UTF8String name, byte[] data, ManifestResourceAttributes flags)
			: this(name, new MemoryImageStream(0, data, 0, data.Length), flags) {
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="dataStream">Resource data</param>
		public EmbeddedResource(UTF8String name, IImageStream dataStream)
			: this(name, dataStream, ManifestResourceAttributes.Private) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="dataStream">Resource data</param>
		/// <param name="flags">Resource flags</param>
		public EmbeddedResource(UTF8String name, IImageStream dataStream, ManifestResourceAttributes flags)
			: base(name, flags) {
			if (dataStream == null)
				throw new ArgumentNullException("dataStream");
			this.dataStream = dataStream;
		}

		/// <summary>
		/// Creates a new resource stream that can access the same data as the original
		/// Stream. Note that the data is shared between these streams.
		/// </summary>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public IImageStream GetClonedResourceStream() {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return dataStream.Clone();
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
		}

		/// <summary>
		/// Gets the resource data as a <see cref="Stream"/>
		/// </summary>
		/// <returns>A stream</returns>
		public Stream GetResourceStream() {
			return GetClonedResourceStream().CreateStream(true);
		}

		/// <summary>
		/// Gets the resource data as a byte array
		/// </summary>
		/// <returns>The resource data</returns>
		public byte[] GetResourceData() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return dataStream.ReadAllBytes();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (!disposing)
				return;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (dataStream != null)
				dataStream.Dispose();
			dataStream = null;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
			base.Dispose(disposing);
		}

		/// <inheritdoc/>
		public override string ToString() {
			var ds = dataStream;
			return string.Format("{0} - size: {1}", UTF8String.ToSystemStringOrEmpty(Name), ds == null ? 0 : ds.Length);
		}
	}

	/// <summary>
	/// A reference to a resource in another assembly
	/// </summary>
	public sealed class AssemblyLinkedResource : Resource {
		AssemblyRef asmRef;

		/// <inheritdoc/>
		public override ResourceType ResourceType {
			get { return ResourceType.AssemblyLinked; }
		}

		/// <summary>
		/// Gets/sets the assembly reference
		/// </summary>
		public AssemblyRef Assembly {
			get { return asmRef; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				asmRef = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="asmRef">Assembly reference</param>
		/// <param name="flags">Resource flags</param>
		public AssemblyLinkedResource(UTF8String name, AssemblyRef asmRef, ManifestResourceAttributes flags)
			: base(name, flags) {
			if (asmRef == null)
				throw new ArgumentNullException("asmRef");
			this.asmRef = asmRef;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} - assembly: {1}", UTF8String.ToSystemStringOrEmpty(Name), asmRef.FullName);
		}
	}

	/// <summary>
	/// A resource that is stored in a file on disk
	/// </summary>
	public sealed class LinkedResource : Resource {
		FileDef file;

		/// <inheritdoc/>
		public override ResourceType ResourceType {
			get { return ResourceType.Linked; }
		}

		/// <summary>
		/// Gets/sets the file
		/// </summary>
		public FileDef File {
			get { return file; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				file = value;
			}
		}

		/// <summary>
		/// Gets/sets the hash
		/// </summary>
		public byte[] Hash {
			get { return file.HashValue; }
			set { file.HashValue = value; }
		}

		/// <summary>
		/// Gets/sets the file name
		/// </summary>
		public UTF8String FileName {
			get { return file == null ? UTF8String.Empty : file.Name; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="file">The file</param>
		/// <param name="flags">Resource flags</param>
		public LinkedResource(UTF8String name, FileDef file, ManifestResourceAttributes flags)
			: base(name, flags) {
			this.file = file;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} - file: {1}", UTF8String.ToSystemStringOrEmpty(Name), UTF8String.ToSystemStringOrEmpty(FileName));
		}
	}
}
