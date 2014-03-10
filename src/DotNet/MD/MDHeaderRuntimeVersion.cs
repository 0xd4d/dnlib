/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
