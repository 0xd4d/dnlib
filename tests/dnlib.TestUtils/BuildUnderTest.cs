// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;

namespace dnlib.TestUtils {
	/// <summary>
	/// The same tests are run against every build of dnlib (net10.0, netstandard2.0, net45, net35, each with
	/// and without <c>THREAD_SAFE</c>). This class knows which build the test run was told to use, so a test
	/// can check that it's really the one that got loaded.
	/// </summary>
	public static class BuildUnderTest {
		/// <summary>
		/// Target framework of the dnlib build that should be tested, eg. <c>net35</c>
		/// </summary>
		public static string ExpectedTargetFramework => GetMetadata("DnlibExpectedTfm");

		/// <summary>
		/// <c>true</c> if the dnlib build that should be tested is thread safe
		/// </summary>
		public static bool ExpectedThreadSafe => GetMetadata("DnlibExpectedThreadSafe") == "true";

		/// <summary>
		/// Target framework of the dnlib assembly that is loaded, eg. <c>net35</c>
		/// </summary>
		public static string ActualTargetFramework {
			get {
				var dnlibAssembly = typeof(Settings).Assembly;
				foreach (var data in dnlibAssembly.GetCustomAttributesData()) {
					if (data.Constructor.DeclaringType?.FullName != "System.Runtime.Versioning.TargetFrameworkAttribute")
						continue;
					return ToShortName((string)data.ConstructorArguments[0].Value!);
				}
				// The attribute doesn't exist in .NET Framework 3.5 and earlier
				return "net35";
			}
		}

		static string ToShortName(string frameworkName) => frameworkName switch {
			".NETCoreApp,Version=v10.0" => "net10.0",
			".NETStandard,Version=v2.0" => "netstandard2.0",
			".NETFramework,Version=v4.5" => "net45",
			_ => frameworkName,
		};

		internal static string GetMetadata(string key) {
			foreach (var attr in typeof(BuildUnderTest).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()) {
				if (attr.Key == key)
					return attr.Value ?? string.Empty;
			}
			throw new InvalidOperationException($"dnlib.TestUtils.csproj doesn't set the assembly metadata '{key}'");
		}
	}
}
