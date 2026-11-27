// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.Tests.Reading;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// The types of the runtime's own signatures (<see cref="VarZapSig"/>, <see cref="NativeValueTypeZapSig"/>,
	/// <see cref="CanonZapSig"/>) aren't the normal types when two signatures are compared, but the runtime
	/// loads the same type for them: the generic parameter itself, <c>System.__Canon</c>, and its native view
	/// of a type. A dnlib method with such a signature is the <see cref="System.Reflection.MethodBase"/> that
	/// the runtime loads for it. The tests write the signature into a file and compare the method that dnlib
	/// reads with the method that .NET loads. Only .NET is asked: it's not known what .NET Framework does
	/// with these bytes (set DNLIB_TESTS_ASK_NET_FRAMEWORK=1 to ask it).
	/// </summary>
	public class RuntimeOwnSignatureTypeReflectionTests {
		const string Source = @"
public static class Generic<T> {
	public static void Other<U>(U a) { }
	public static void Method(int a, T b) { }
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint methodRid;
		static readonly string typeParamRid, methodParamRid;

		static RuntimeOwnSignatureTypeReflectionTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Generic`1", isReflectionName: true)!;
				methodRid = type.FindMethod("Method")!.Rid;
				typeParamRid = type.GenericParameters[0].Rid.ToString("X2");
				methodParamRid = type.FindMethod("Other")!.GenericParameters[0].Rid.ToString("X2");
			}
		}

		static bool IsRuntimeAsked => !ClrReaderComparison.IsRuntimeNotAsked &&
			(ClrReaderComparison.IsDotNet || Environment.GetEnvironmentVariable("DNLIB_TESTS_ASK_NET_FRAMEWORK") == "1");

		// The method gets the signature (hex bytes; {T} and {U} are the rids of the generic parameter of the
		// type and of the method Other). It's added to the end of the #Blob heap.
		static byte[] CreateImage(string signature) {
			var bytes = signature.Replace("{T}", typeParamRid).Replace("{U}", methodParamRid)
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var patcher = new MetadataPatcher(baseImage);
			// The method is the last one. It gets no rows of the Param table, see ClrSignatureRuleTests.
			Assert.Equal(methodRid, patcher.GetRowCount(Table.Method));
			patcher.SetCell(Table.Method, methodRid, "Signature", (uint)heapLength);
			patcher.SetCell(Table.Method, methodRid, "ParamList", patcher.GetRowCount(Table.Param) + 1);
			var block = new MetadataBlock(patcher.ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		// Fixed in this commit: the method wasn't equal to the method that the runtime loads, a type of the
		// runtime's own signatures was never equal to a System.Type. The last parameter has the type (the
		// runtime doesn't load the method if it's another parameter).
		[Theory]
		// The native view of a type: "VALUETYPE" and the name of the type in System.Reflection
		[InlineData("00 02 01 08 3D 05", "VALUETYPESystem.Byte")]
		[InlineData("00 02 01 08 3D 0E", "VALUETYPESystem.String")]
		[InlineData("00 02 01 08 3D 3D 08", "VALUETYPEVALUETYPESystem.Int32")]
		[InlineData("00 02 01 08 1D 3D 05", "VALUETYPESystem.Byte[]")]
		[InlineData("00 02 01 08 0F 3D 05", "VALUETYPESystem.Byte*")]
		[InlineData("00 02 01 08 3D 13 00", "VALUETYPET")]
		// System.__Canon
		[InlineData("00 02 01 08 3E", "System.__Canon")]
		[InlineData("00 02 01 08 1D 3E", "System.__Canon[]")]
		[InlineData("00 02 01 08 3D 3E", "VALUETYPESystem.__Canon")]
		// The generic parameter with a rid: the one of the type, and the one of another method
		[InlineData("00 02 01 08 3B {T}", "T")]
		[InlineData("00 02 01 08 1D 3B {T}", "T[]")]
		[InlineData("00 02 01 08 3B {U}", "U")]
		public void MethodIsTheMethodThatTheRuntimeLoads(string signature, string expectedName) {
			var image = CreateImage(signature);
			using (var module = ModuleDefMD.Load(image)) {
				var method = module.ResolveMethod(methodRid)!;
				var lastParam = method.MethodSig!.Params[1]!;
				// Like System.Type.FullName, the full name of a type with a generic parameter is null
				Assert.Equal(expectedName, string.IsNullOrEmpty(lastParam.ReflectionFullName) ? lastParam.ReflectionName : lastParam.ReflectionFullName);
				if (!IsRuntimeAsked)
					return;

				using (var loaded = new LoadedAssembly(image)) {
					var runtimeMethod = loaded.Assembly.ManifestModule.ResolveMethod(method.MDToken.ToInt32())!;
					var runtimeType = runtimeMethod.GetParameters()[1].ParameterType;
					Assert.Equal(expectedName, runtimeType.FullName ?? runtimeType.ToString());
					var comparer = new SigComparer();

					Assert.True(comparer.Equals(lastParam, runtimeType));
					Assert.Equal(comparer.GetHashCode(lastParam), comparer.GetHashCode(runtimeType));
					Assert.True(comparer.Equals(method, runtimeMethod));
					Assert.Equal(comparer.GetHashCode(method), comparer.GetHashCode(runtimeMethod));
					Assert.True(comparer.Equals(method.DeclaringType, runtimeMethod.DeclaringType));
					// It's not another type
					Assert.False(comparer.Equals(module.CorLibTypes.Object, runtimeType));
					Assert.False(comparer.Equals(module.CorLibTypes.Byte, runtimeType));
					Assert.False(comparer.Equals(lastParam, typeof(object)));
					Assert.False(comparer.Equals(lastParam, typeof(byte)));
					Assert.False(comparer.Equals(module.CorLibTypes.Object.TypeDefOrRef, runtimeType));
					comparer.GetHashCode(runtimeType);
				}
			}
		}

		// A generic parameter with a rid is never the type argument: in Generic<int>, the type of the
		// parameter still is T (the type of a parameter with a normal generic parameter is int)
		[Fact]
		public void GenericParameterWithARidIsNotInstantiated() {
			if (!IsRuntimeAsked)
				return;
			var image = CreateImage("00 02 01 08 3B {T}");
			using (var module = ModuleDefMD.Load(image))
			using (var loaded = new LoadedAssembly(image)) {
				var method = module.ResolveMethod(methodRid)!;
				var closedType = loaded.Assembly.GetType("Generic`1", true)!.MakeGenericType(typeof(int));
				var runtimeType = closedType.GetMethod("Method")!.GetParameters()[1].ParameterType;

				Assert.True(runtimeType.IsGenericParameter);
				Assert.True(new SigComparer().Equals(method.MethodSig!.Params[1], runtimeType));
				Assert.False(new SigComparer().Equals(method.MethodSig.Params[1], typeof(int)));
			}
		}

		// The same loaded type is also the normal type of a signature: T is !0, System.__Canon is the class
		[Fact]
		public void LoadedTypeIsAlsoTheNormalType() {
			if (!IsRuntimeAsked)
				return;
			var comparer = new SigComparer();
			var canonImage = CreateImage("00 02 01 08 3E");
			using (var module = ModuleDefMD.Load(canonImage))
			using (var loaded = new LoadedAssembly(canonImage)) {
				var runtimeType = loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!.GetParameters()[1].ParameterType;
				var canonRef = new TypeRefUser(module, "System", "__Canon", module.CorLibTypes.AssemblyRef);

				Assert.True(new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(new ClassSig(canonRef), runtimeType));
				Assert.Equal(comparer.GetHashCode(new ClassSig(canonRef)), comparer.GetHashCode(runtimeType));
				Assert.Equal(comparer.GetHashCode(new CanonZapSig()), comparer.GetHashCode(runtimeType));
				// But the two signatures aren't equal
				Assert.False(comparer.Equals((TypeSig)new CanonZapSig(), new ClassSig(canonRef)));
			}
			var genericParamImage = CreateImage("00 02 01 08 3B {T}");
			using (var module = ModuleDefMD.Load(genericParamImage))
			using (var loaded = new LoadedAssembly(genericParamImage)) {
				var runtimeType = loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!.GetParameters()[1].ParameterType;
				var withRid = module.ResolveMethod(methodRid)!.MethodSig!.Params[1]!;

				Assert.True(comparer.Equals(new GenericVar(0), runtimeType));
				Assert.Equal(comparer.GetHashCode(new GenericVar(0)), comparer.GetHashCode(runtimeType));
				Assert.Equal(comparer.GetHashCode(withRid), comparer.GetHashCode(runtimeType));
				Assert.False(comparer.Equals(withRid, new GenericVar(0)));
				// The same rid in a module that wasn't read from a file: dnlib can't find the generic parameter
				Assert.False(comparer.Equals(new VarZapSig(((VarZapSig)withRid).GenericParamRid, new ModuleDefUser("Other.dll")), runtimeType));
			}
		}

		// The native view of a type is imported from System.Reflection. System.__Canon and a generic parameter
		// are imported as the normal types: a System.Type doesn't know that it was one of these in the file.
		[Fact]
		public void NativeViewIsImportedFromReflection() {
			if (!IsRuntimeAsked)
				return;
			var image = CreateImage("00 02 01 08 1D 3D 05");
			using (var loaded = new LoadedAssembly(image)) {
				var runtimeType = loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!.GetParameters()[1].ParameterType;
				var target = new ModuleDefUser("Target.dll");

				var imported = new Importer(target).ImportAsTypeSig(runtimeType);

				var native = Assert.IsType<NativeValueTypeZapSig>(Assert.IsType<SZArraySig>(imported).Next);
				Assert.Equal(ElementType.U1, native.Next!.ElementType);
				Assert.True(new SigComparer().Equals(imported, runtimeType));
			}
		}
	}
}
