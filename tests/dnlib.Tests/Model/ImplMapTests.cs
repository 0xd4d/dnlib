// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// An <see cref="ImplMap"/> says which function of which DLL a P/Invoke method calls.
	/// <see cref="ImplMap.IsPinvokeMethod(string, string, bool)"/> checks the DLL and the function: on Windows the
	/// DLL name is case insensitive, spaces at its end are ignored, and so is a ".dll" extension.
	/// </summary>
	public class ImplMapTests {
		static ImplMapUser Create(string dllName, string function) =>
			new ImplMapUser(new ModuleRefUser(new ModuleDefUser("M"), dllName), function, PInvokeAttributes.CallConvWinapi);

		[Theory]
		[InlineData("kernel32.dll", "GetTickCount", "kernel32", "GetTickCount", true, true)]
		[InlineData("kernel32", "GetTickCount", "KERNEL32.DLL", "GetTickCount", true, true)]
		[InlineData("kernel32.dll  ", "GetTickCount", "kernel32", "GetTickCount", true, true)]
		[InlineData("kernel32.dll  ", "GetTickCount", "kernel32", "GetTickCount", false, false)]
		[InlineData("libc.so", "getpid", "libc.so", "getpid", false, true)]
		[InlineData("kernel32", "GetTickCount", "kernel32", "gettickcount", true, false)]
		[InlineData("kernel32", "GetTickCount", "user32", "GetTickCount", true, false)]
		public void TheDllAndTheFunctionAreCompared(string dllName, string function, string askedDll, string askedFunction, bool treatAsWindows, bool expected) =>
			Assert.Equal(expected, Create(dllName, function).IsPinvokeMethod(askedDll, askedFunction, treatAsWindows));

		[Fact]
		public void AnImplMapWithoutAModuleIsNoMethodOfADll() {
			var implMap = new ImplMapUser(null, "GetTickCount", 0);

			Assert.False(implMap.IsPinvokeMethod("kernel32", "GetTickCount", true));
			// The current OS decides how the DLL name is compared
			Assert.True(Create("kernel32", "GetTickCount").IsPinvokeMethod("kernel32", "GetTickCount"));
		}

		// The properties that set a part of the flags only change that part
		[Fact]
		public void ThePartsOfTheFlagsAreSet() {
			var implMap = Create("kernel32", "GetTickCount");
			var module = new ModuleRefUser(new ModuleDefUser("M"), "user32");

			implMap.CharSet = PInvokeAttributes.CharSetUnicode;
			implMap.BestFit = PInvokeAttributes.BestFitDisabled;
			implMap.ThrowOnUnmappableChar = PInvokeAttributes.ThrowOnUnmappableCharEnabled;
			implMap.CallConv = PInvokeAttributes.CallConvFastcall;
			implMap.Module = module;
			implMap.Rid = 3;

			Assert.Equal(PInvokeAttributes.CharSetUnicode, implMap.CharSet);
			Assert.Equal(PInvokeAttributes.BestFitDisabled, implMap.BestFit);
			Assert.Equal(PInvokeAttributes.ThrowOnUnmappableCharEnabled, implMap.ThrowOnUnmappableChar);
			Assert.True(implMap.IsCallConvFastcall);
			Assert.Equal(PInvokeAttributes.CharSetUnicode | PInvokeAttributes.BestFitDisabled | PInvokeAttributes.ThrowOnUnmappableCharEnabled | PInvokeAttributes.CallConvFastcall, implMap.Attributes);
			Assert.Same(module, implMap.Module);
			Assert.Equal(3U, implMap.Rid);
			implMap.Attributes = 0;
			Assert.Equal(PInvokeAttributes.CharSetNotSpec, implMap.CharSet);
		}
	}
}
