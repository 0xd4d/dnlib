// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.MD {
	/// <summary>
	/// Version strings found in the meta data header
	/// </summary>
	public static class MDHeaderRuntimeVersion {
		/// <summary>
		/// MS CLR 1.0 version string (.NET 1.0)
		/// </summary>
		public const string MS_CLR_10 = "v1.0.3705";

		/// <summary>
		/// MS CLR 1.0 version string (.NET 1.0). This is an incorrect version that shouldn't be used.
		/// </summary>
		public const string MS_CLR_10_X86RETAIL = "v1.x86ret";

		/// <summary>
		/// MS CLR 1.0 version string (.NET 1.0). This is an incorrect version that shouldn't be used.
		/// </summary>
		public const string MS_CLR_10_RETAIL = "retail";

		/// <summary>
		/// MS CLR 1.0 version string (.NET 1.0). This is an incorrect version that shouldn't be used.
		/// </summary>
		public const string MS_CLR_10_COMPLUS = "COMPLUS";

		/// <summary>
		/// MS CLR 1.1 version string (.NET 1.1)
		/// </summary>
		public const string MS_CLR_11 = "v1.1.4322";

		/// <summary>
		/// MS CLR 2.0 version string (.NET 2.0-3.5)
		/// </summary>
		public const string MS_CLR_20 = "v2.0.50727";

		/// <summary>
		/// MS CLR 4.0 version string (.NET 4.0-4.5)
		/// </summary>
		public const string MS_CLR_40 = "v4.0.30319";

		/// <summary>
		/// MS CLR 1.0 any version
		/// </summary>
		public const string MS_CLR_10_PREFIX = "v1.0";

		/// <summary>
		/// MS CLR 1.0 any version
		/// </summary>
		public const string MS_CLR_10_PREFIX_X86RETAIL = "v1.x86";

		/// <summary>
		/// MS CLR 1.1 any version
		/// </summary>
		public const string MS_CLR_11_PREFIX = "v1.1";

		/// <summary>
		/// MS CLR 2.0 any version
		/// </summary>
		public const string MS_CLR_20_PREFIX = "v2.0";

		/// <summary>
		/// MS CLR 4.0 any version
		/// </summary>
		public const string MS_CLR_40_PREFIX = "v4.0";

		/// <summary>
		/// ECMA 2002 version string
		/// </summary>
		public const string ECMA_2002 = "Standard CLI 2002";

		/// <summary>
		/// ECMA 2005 version string
		/// </summary>
		public const string ECMA_2005 = "Standard CLI 2005";
	}
}
