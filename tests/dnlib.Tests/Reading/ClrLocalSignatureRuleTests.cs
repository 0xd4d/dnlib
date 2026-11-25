// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The local variables of a method are a signature: the byte 7, the number of variables and their types.
	/// These tests write signatures that a compiler doesn't write (another number of types than the number
	/// says, pinned in unusual places, types that a variable can't have) and read the variables with dnlib
	/// and with the runtime that runs the tests (MethodBody.LocalVariables). If the runtime reads the
	/// variables, dnlib must read the same ones.
	/// </summary>
	public class ClrLocalSignatureRuleTests {
		const string Source = @"
public static class Marker {
	// The addresses of the variables are used: the compiler can't remove them
	public static string Method() {
		int number;
		string text;
		Fill(out number, out text);
		return text + number;
	}
	static void Fill(out int number, out string text) {
		number = 1;
		text = null;
	}
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint methodRid, signatureRid;
		static readonly string objectToken;

		static ClrLocalSignatureRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var method = module.Find("Marker", isReflectionName: true)!.FindMethod("Method")!;
				methodRid = method.Rid;
				Assert.Equal(2, method.Body!.Variables.Count);
				signatureRid = new MDToken(method.Body.LocalVarSigTok).Rid;
				// The token of a type in a signature: the row and (in the 2 low bits) the table
				uint token = module.GetTypeRefs().Single(a => a.FullName == "System.Object").Rid << 2 | 1;
				Assert.True(token < 0x80);
				objectToken = token.ToString("X2");
			}
		}

		// The bytes as hex numbers. {Object} is the token of System.Object. The signature is added to the
		// end of the #Blob heap; the method then has these variables.
		static byte[] CreateImage(string signature) {
			var bytes = signature.Replace("{Object}", objectToken).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(new MetadataPatcher(baseImage).SetCell(Table.StandAloneSig, signatureRid, "Signature", (uint)heapLength).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		// "List`1[[System.Int32, mscorlib, Version=...]]" is "List`1[[System.Int32]]"
		static string WithoutAssemblyNames(string typeName) => Regex.Replace(typeName, @", [\w.]+, Version=[\d.]+, Culture=neutral, PublicKeyToken=\w+", "");

		// The types of the variables, "pinned" before a pinned one
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var body = module.ResolveMethod(methodRid)!.Body;
				if (body is null)
					return "no body";
				if (body.Variables.Count == 0)
					return "none";
				return string.Join(", ", body.Variables.Select(a => {
					// For the CLR a variable is pinned if 'pinned' is the first thing after the modifiers
					var type = a.Type.RemoveModifiers();
					bool isPinned = type is PinnedSig;
					if (type is PinnedSig pinned)
						type = pinned.Next;
					return (isPinned ? "pinned " : "") + (type is null ? "no type" : WithoutAssemblyNames(type.ReflectionFullName));
				}));
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var method = loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!;
				var variables = method.GetMethodBody()!.LocalVariables;
				if (variables.Count == 0)
					return "none";
				return string.Join(", ", variables.OrderBy(a => a.LocalIndex).Select(a => (a.IsPinned ? "pinned " : "") + WithoutAssemblyNames(a.LocalType.FullName ?? a.LocalType.ToString())));
			}
		}

		[Theory]
		[InlineData("07 02 08 0E", "System.Int32, System.String", "System.Int32, System.String")]
		// The number of variables
		[InlineData("07 00", "none", "none")]
		[InlineData("07 01 08 0E", "System.Int32", "System.Int32")]
		[InlineData("07 03 08 0E", "none", "throws BadImageFormatException")]
		[InlineData("07 80 02 08 0E", "System.Int32, System.String", "System.Int32, System.String")]
		[InlineData("07 02 08", "none", "throws BadImageFormatException")]
		[InlineData("07", "none", "throws VerificationException")]
		[InlineData("", "none", "throws VerificationException")]
		// The first byte
		[InlineData("06 02 08 0E", "none", "throws VerificationException")]
		[InlineData("00 02 08 0E", "none", "throws VerificationException")]
		[InlineData("27 02 08 0E", "System.Int32, System.String", "throws VerificationException")]
		[InlineData("87 02 08 0E", "System.Int32, System.String", "throws VerificationException")]
		// Pinned variables (45) and references (10)
		[InlineData("07 02 45 08 0E", "pinned System.Int32, System.String", "pinned System.Int32, System.String")]
		[InlineData("07 02 08 45 0E", "System.Int32, pinned System.String", "System.Int32, pinned System.String")]
		[InlineData("07 02 45 10 08 0E", "pinned System.Int32&, System.String", "pinned System.Int32&, System.String")]
		[InlineData("07 02 10 45 08 0E", "System.Int32&, System.String", "System.Int32&, System.String")]
		[InlineData("07 02 45 45 08 0E", "pinned System.Int32, System.String", "pinned System.Int32, System.String")]
		[InlineData("07 02 10 08 0E", "System.Int32&, System.String", "System.Int32&, System.String")]
		[InlineData("07 02 10 10 08 0E", "System.Int32&&, System.String", "throws TypeLoadException")]
		[InlineData("07 02 1D 45 08 0E", "System.Int32[], System.String", "System.Int32[], System.String")]
		// Modifiers (20 and 1F with the token of a type)
		[InlineData("07 02 20 {Object} 08 0E", "System.Int32, System.String", "System.Int32, System.String")]
		[InlineData("07 02 1F {Object} 08 0E", "System.Int32, System.String", "System.Int32, System.String")]
		[InlineData("07 02 45 20 {Object} 08 0E", "pinned System.Int32, System.String", "pinned System.Int32, System.String")]
		[InlineData("07 02 20 {Object} 45 08 0E", "pinned System.Int32, System.String", "pinned System.Int32, System.String")]
		// Types that a variable can't have, and bytes that aren't a type
		[InlineData("07 02 01 0E", "System.Void, System.String", "throws VerificationException")]
		[InlineData("07 02 16 0E", "System.TypedReference, System.String", "System.TypedReference, System.String")]
		[InlineData("07 02 10 16 0E", "System.TypedReference&, System.String", "System.TypedReference&, System.String")]
		[InlineData("07 02 41 08 0E", "System.Int32, System.String", "throws VerificationException")]
		[InlineData("07 02 00 0E", "no type, System.String", "throws VerificationException")]
		[InlineData("07 02 FF 0E", "no type, System.String", "throws BadImageFormatException")]
		[InlineData("07 02 13 00 0E", ", System.String", "throws BadImageFormatException")]
		public void LocalVariables(string signature, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(signature);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
