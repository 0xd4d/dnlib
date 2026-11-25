// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <c>IsCorLib()</c> tells whether an assembly is the runtime's core library. dnlib needs to know it to
	/// recognize types like <c>System.Type</c> and <c>System.Object</c>, eg. when it reads custom attributes.
	/// </summary>
	public class IsCorLibTests {
		static AssemblyRefUser CreateReference(string name, string culture = "") =>
			new AssemblyRefUser(name, new Version(4, 0, 0, 0), null, culture);

		[Theory]
		[InlineData("mscorlib")]
		[InlineData("System.Private.CoreLib")]
		[InlineData("System.Runtime")]
		[InlineData("netstandard")]
		[InlineData("corefx")]
		// Assembly names aren't case sensitive
		[InlineData("MSCORLIB")]
		[InlineData("system.runtime")]
		public void CoreLibraryNames(string name) =>
			Assert.True(CreateReference(name).IsCorLib());

		[Theory]
		[InlineData("System")]
		[InlineData("System.Core")]
		[InlineData("mscorlib2")]
		[InlineData("")]
		public void OtherNames(string name) =>
			Assert.False(CreateReference(name).IsCorLib());

		// Fixed in 86d3b9f3: the CLR finds its core library by name only, so the culture doesn't matter
		[Theory]
		[InlineData("mscorlib")]
		[InlineData("System.Private.CoreLib")]
		public void RuntimeCoreLibraryWithACultureIsStillTheCoreLibrary(string name) =>
			Assert.True(CreateReference(name, "fr").IsCorLib());

		// The CLR can't find these if the reference has a culture
		[Theory]
		[InlineData("System.Runtime")]
		[InlineData("netstandard")]
		[InlineData("corefx")]
		public void FacadeWithACultureIsNotTheCoreLibrary(string name) =>
			Assert.False(CreateReference(name, "fr").IsCorLib());

		[Fact]
		public void NullIsNotTheCoreLibrary() =>
			Assert.False(((IAssembly?)null).IsCorLib());
	}
}
