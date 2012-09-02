using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Module table
	/// </summary>
	public class ModuleDef : IHasCustomAttribute, IResolutionScope, IDisposable {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Module.Generation
		/// </summary>
		protected ushort generation;

		/// <summary>
		/// From column Module.Name
		/// </summary>
		protected string name;

		/// <summary>
		/// From column Module.Mvid
		/// </summary>
		protected Guid? mvid;

		/// <summary>
		/// From column Module.EncId
		/// </summary>
		protected Guid? encId;

		/// <summary>
		/// From column Module.EncBaseId
		/// </summary>
		protected Guid? encBaseId;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Module, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 7; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 0; }
		}

		/// <summary>
		/// Creates a <see cref="ModuleDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDef"/> instance</returns>
		public static ModuleDef Load(string fileName) {
			return MDModuleDef.Load(fileName);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDef"/> instance</returns>
		public static ModuleDef Load(byte[] data) {
			return MDModuleDef.Load(data);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDef"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDef"/> instance</returns>
		public static ModuleDef Load(IntPtr addr) {
			return MDModuleDef.Load(addr);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDef"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="ModuleDef"/> instance that now owns <paramref name="dnFile"/></returns>
		public static ModuleDef Load(DotNetFile dnFile) {
			return MDModuleDef.Load(dnFile);
		}


		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing">true if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
		}
	}
}
