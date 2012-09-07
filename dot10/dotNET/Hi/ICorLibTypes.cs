namespace dot10.dotNET.Hi {
	/// <summary>
	/// Access to .NET core library's simple types
	/// </summary>
	public interface ICorLibTypes {
		/// <summary>
		/// Gets a <c>System.Void</c>
		/// </summary>
		ITypeSig Void { get; }

		/// <summary>
		/// Gets a <c>System.Boolean</c>
		/// </summary>
		ITypeSig Boolean { get; }

		/// <summary>
		/// Gets a <c>System.Char</c>
		/// </summary>
		ITypeSig Char { get; }

		/// <summary>
		/// Gets a <c>System.SByte</c>
		/// </summary>
		ITypeSig SByte { get; }

		/// <summary>
		/// Gets a <c>System.Byte</c>
		/// </summary>
		ITypeSig Byte { get; }

		/// <summary>
		/// Gets a <c>System.Int16</c>
		/// </summary>
		ITypeSig Int16 { get; }

		/// <summary>
		/// Gets a <c>System.UInt16</c>
		/// </summary>
		ITypeSig UInt16 { get; }

		/// <summary>
		/// Gets a <c>System.Int32</c>
		/// </summary>
		ITypeSig Int32 { get; }

		/// <summary>
		/// Gets a <c>System.UInt32</c>
		/// </summary>
		ITypeSig UInt32 { get; }

		/// <summary>
		/// Gets a <c>System.Int64</c>
		/// </summary>
		ITypeSig Int64 { get; }

		/// <summary>
		/// Gets a <c>System.UInt64</c>
		/// </summary>
		ITypeSig UInt64 { get; }

		/// <summary>
		/// Gets a <c>System.Single</c>
		/// </summary>
		ITypeSig Single { get; }

		/// <summary>
		/// Gets a <c>System.Double</c>
		/// </summary>
		ITypeSig Double { get; }

		/// <summary>
		/// Gets a <c>System.String</c>
		/// </summary>
		ITypeSig String { get; }

		/// <summary>
		/// Gets a <c>System.TypedReference</c>
		/// </summary>
		ITypeSig TypedReference { get; }

		/// <summary>
		/// Gets a <c>System.IntPtr</c>
		/// </summary>
		ITypeSig IntPtr { get; }

		/// <summary>
		/// Gets a <c>System.UIntPtr</c>
		/// </summary>
		ITypeSig UIntPtr { get; }

		/// <summary>
		/// Gets a <c>System.Object</c>
		/// </summary>
		ITypeSig Object { get; }

		/// <summary>
		/// Gets the assembly reference to the core library
		/// </summary>
		AssemblyRef AssemblyRef { get; }
	}
}
