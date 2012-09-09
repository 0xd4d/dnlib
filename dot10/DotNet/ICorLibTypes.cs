namespace dot10.DotNet {
	/// <summary>
	/// Access to .NET core library's simple types
	/// </summary>
	public interface ICorLibTypes {
		/// <summary>
		/// Gets a <c>System.Void</c>
		/// </summary>
		CorLibTypeSig Void { get; }

		/// <summary>
		/// Gets a <c>System.Boolean</c>
		/// </summary>
		CorLibTypeSig Boolean { get; }

		/// <summary>
		/// Gets a <c>System.Char</c>
		/// </summary>
		CorLibTypeSig Char { get; }

		/// <summary>
		/// Gets a <c>System.SByte</c>
		/// </summary>
		CorLibTypeSig SByte { get; }

		/// <summary>
		/// Gets a <c>System.Byte</c>
		/// </summary>
		CorLibTypeSig Byte { get; }

		/// <summary>
		/// Gets a <c>System.Int16</c>
		/// </summary>
		CorLibTypeSig Int16 { get; }

		/// <summary>
		/// Gets a <c>System.UInt16</c>
		/// </summary>
		CorLibTypeSig UInt16 { get; }

		/// <summary>
		/// Gets a <c>System.Int32</c>
		/// </summary>
		CorLibTypeSig Int32 { get; }

		/// <summary>
		/// Gets a <c>System.UInt32</c>
		/// </summary>
		CorLibTypeSig UInt32 { get; }

		/// <summary>
		/// Gets a <c>System.Int64</c>
		/// </summary>
		CorLibTypeSig Int64 { get; }

		/// <summary>
		/// Gets a <c>System.UInt64</c>
		/// </summary>
		CorLibTypeSig UInt64 { get; }

		/// <summary>
		/// Gets a <c>System.Single</c>
		/// </summary>
		CorLibTypeSig Single { get; }

		/// <summary>
		/// Gets a <c>System.Double</c>
		/// </summary>
		CorLibTypeSig Double { get; }

		/// <summary>
		/// Gets a <c>System.String</c>
		/// </summary>
		CorLibTypeSig String { get; }

		/// <summary>
		/// Gets a <c>System.TypedReference</c>
		/// </summary>
		CorLibTypeSig TypedReference { get; }

		/// <summary>
		/// Gets a <c>System.IntPtr</c>
		/// </summary>
		CorLibTypeSig IntPtr { get; }

		/// <summary>
		/// Gets a <c>System.UIntPtr</c>
		/// </summary>
		CorLibTypeSig UIntPtr { get; }

		/// <summary>
		/// Gets a <c>System.Object</c>
		/// </summary>
		CorLibTypeSig Object { get; }

		/// <summary>
		/// Gets the assembly reference to the core library
		/// </summary>
		AssemblyRef AssemblyRef { get; }
	}
}
