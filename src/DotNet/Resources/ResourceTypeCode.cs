// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Type of resource
	/// </summary>
	public enum ResourceTypeCode {
		/// <summary>
		/// null
		/// </summary>
		Null		= 0,

		/// <summary>
		/// <see cref="string"/>
		/// </summary>
		String		= 1,

		/// <summary>
		/// <see cref="bool"/>
		/// </summary>
		Boolean		= 2,

		/// <summary>
		/// <see cref="char"/>
		/// </summary>
		Char		= 3,

		/// <summary>
		/// <see cref="byte"/>
		/// </summary>
		Byte		= 4,

		/// <summary>
		/// <see cref="sbyte"/>
		/// </summary>
		SByte		= 5,

		/// <summary>
		/// <see cref="short"/>
		/// </summary>
		Int16		= 6,

		/// <summary>
		/// <see cref="ushort"/>
		/// </summary>
		UInt16		= 7,

		/// <summary>
		/// <see cref="int"/>
		/// </summary>
		Int32		= 8,

		/// <summary>
		/// <see cref="uint"/>
		/// </summary>
		UInt32		= 9,

		/// <summary>
		/// <see cref="long"/>
		/// </summary>
		Int64		= 0x0A,

		/// <summary>
		/// <see cref="ulong"/>
		/// </summary>
		UInt64		= 0x0B,

		/// <summary>
		/// <see cref="float"/>
		/// </summary>
		Single		= 0x0C,

		/// <summary>
		/// <see cref="double"/>
		/// </summary>
		Double		= 0x0D,

		/// <summary>
		/// <see cref="decimal"/>
		/// </summary>
		Decimal		= 0x0E,

		/// <summary>
		/// <see cref="DateTime"/>
		/// </summary>
		DateTime	= 0x0F,

		/// <summary>
		/// <see cref="TimeSpan"/>
		/// </summary>
		TimeSpan	= 0x10,

		/// <summary>
		/// <see cref="byte"/> array
		/// </summary>
		ByteArray	= 0x20,

		/// <summary>
		/// <see cref="Stream"/>
		/// </summary>
		Stream		= 0x21,

		/// <summary>
		/// Start of user types
		/// </summary>
		UserTypes	= 0x40,
	}
}
