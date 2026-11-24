// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A field or parameter can say how it's passed to native code (<c>[MarshalAs]</c> in C#): a native type, and
	/// for some native types a few more values that are all optional (the size of an array, the type of its
	/// elements, ...). dnlib has a <see cref="MarshalType"/> class for every native type with such values. The
	/// tests write a field with a marshal type, read it again, and ask the runtime what it reads.
	/// </summary>
	public class MarshalTypeTests {
		// Marshal types with every combination of optional values that can be stored: a value can only be
		// there if all values before it are there. The text is what ToString() returns.
		public static IEnumerable<object[]> MarshalTypes() {
			object[] Row(MarshalType marshalType, string text) => new object[] { marshalType, text };
			yield return Row(new MarshalType(NativeType.Boolean), "Boolean");
			yield return Row(new MarshalType(NativeType.LPWStr), "LPWStr");
			yield return Row(new FixedSysStringMarshalType(), "FixedSysString (<no size>)");
			yield return Row(new FixedSysStringMarshalType(16), "FixedSysString (16)");
			yield return Row(new SafeArrayMarshalType(), "SafeArray (NotInitialized)");
			yield return Row(new SafeArrayMarshalType(VariantType.I4), "SafeArray (I4)");
			yield return Row(new FixedArrayMarshalType(), "FixedArray (0, NotInitialized)");
			yield return Row(new FixedArrayMarshalType(4), "FixedArray (4, NotInitialized)");
			yield return Row(new FixedArrayMarshalType(4, NativeType.I2), "FixedArray (4, I2)");
			yield return Row(new ArrayMarshalType(), "Array (NotInitialized, -1, -1, -1)");
			yield return Row(new ArrayMarshalType(NativeType.I4), "Array (I4, -1, -1, -1)");
			yield return Row(new ArrayMarshalType(NativeType.I4, 2), "Array (I4, 2, -1, -1)");
			yield return Row(new ArrayMarshalType(NativeType.I4, 2, 10), "Array (I4, 2, 10, -1)");
			yield return Row(new ArrayMarshalType(NativeType.I4, 2, 10, 1), "Array (I4, 2, 10, 1)");
			// The marshaler is a class: the runtime loads it, so it must exist
			yield return Row(new CustomMarshalType("guid", "native type", typeof(Uri).AssemblyQualifiedName, null, "cookie"),
				"CustomMarshaler (guid, native type, " + typeof(Uri).AssemblyQualifiedName + ", cookie)");
			yield return Row(new InterfaceMarshalType(NativeType.IUnknown), "IUnknown (-1)");
			yield return Row(new InterfaceMarshalType(NativeType.IDispatch, 3), "IDispatch (3)");
			yield return Row(new InterfaceMarshalType(NativeType.IntF, 1), "IntF (1)");
			// Bytes that are written as they are. These can't be read: a string with a size that ends too early.
			yield return Row(new RawMarshalType(new byte[] { (byte)NativeType.FixedSysString, 0xFF }), "RawBlob");
		}

		[Theory]
		[MemberData(nameof(MarshalTypes))]
		public void MarshalTypeIsWrittenAndReadWithAllItsValues(MarshalType marshalType, string text) {
			Assert.Equal(text, marshalType.ToString());
			var created = new CreatedModule();
			created.AddField("Field", created.Module.CorLibTypes.Object).MarshalType = marshalType;

			var image = created.Write();

			using (var module = ModuleDefMD.Load(image)) {
				var field = CreatedModule.GetType(module).FindField("Field")!;
				Assert.True(field.HasMarshalType);
				var read = field.MarshalType!;
				if (marshalType is RawMarshalType raw) {
					// The reader keeps bytes that it can't read
					Assert.Equal(raw.Data, Assert.IsType<RawMarshalType>(read).Data);
				}
				else if (marshalType is CustomMarshalType) {
					// The name of the marshaler class is read as a type
					var custom = Assert.IsType<CustomMarshalType>(read);
					Assert.Equal("guid", custom.Guid);
					Assert.Equal("native type", custom.NativeTypeName);
					Assert.Equal("cookie", custom.Cookie);
					Assert.Equal("System.Uri", custom.CustomMarshaler!.FullName);
					Assert.Equal(typeof(Uri).Assembly.GetName().Name, custom.CustomMarshaler.DefinitionAssembly!.Name);
				}
				else {
					Assert.Equal(marshalType.GetType(), read.GetType());
					Assert.Equal(text, read.ToString());
				}
			}
		}

		// The runtime's reflection returns the marshalling info of a field as a MarshalAsAttribute. The numbers of
		// dnlib's NativeType are the numbers of UnmanagedType.
		[Theory]
		[MemberData(nameof(MarshalTypes))]
		public void RuntimeReadsTheWrittenNativeType(MarshalType marshalType, string text) {
			Assert.SkipUnless(typeof(object).Assembly.GetName().Name == "System.Private.CoreLib", "Mono and .NET Framework read the marshalling info in other ways");
			Assert.SkipWhen(marshalType is RawMarshalType, "The bytes of this test aren't valid marshalling info");
			Assert.NotEmpty(text);
			var created = new CreatedModule();
			created.AddField("Field", created.Module.CorLibTypes.Object).MarshalType = marshalType;

			using (var loaded = new LoadedAssembly(created.Write())) {
				var field = loaded.Assembly.GetType("Ns.Type")!.GetField("Field")!;
				MarshalAsAttribute attribute;
				try {
					attribute = (MarshalAsAttribute)field.GetCustomAttributes(typeof(MarshalAsAttribute), false).Single();
				}
				catch (BadImageFormatException) {
					// The runtime needs the size of a fixed size string: dnlib reads more than the runtime does
					Assert.False(((FixedSysStringMarshalType)marshalType).IsSizeValid);
					return;
				}

				Assert.Equal((int)marshalType.NativeType, (int)attribute.Value);
				switch (marshalType) {
				case FixedSysStringMarshalType fixedString when fixedString.IsSizeValid:
					Assert.Equal(fixedString.Size, attribute.SizeConst);
					break;
				case FixedArrayMarshalType fixedArray when fixedArray.IsSizeValid:
					Assert.Equal(fixedArray.Size, attribute.SizeConst);
					if (fixedArray.IsElementTypeValid)
						Assert.Equal((int)fixedArray.ElementType, (int)attribute.ArraySubType);
					break;
				case ArrayMarshalType array when array.IsElementTypeValid:
					Assert.Equal((int)array.ElementType, (int)attribute.ArraySubType);
					if (array.IsParamNumberValid)
						Assert.Equal(array.ParamNumber, attribute.SizeParamIndex);
					if (array.IsSizeValid)
						Assert.Equal(array.Size, attribute.SizeConst);
					break;
				case CustomMarshalType custom:
					Assert.Equal(custom.CustomMarshalerTypeName, attribute.MarshalType);
					Assert.Equal(custom.Cookie, attribute.MarshalCookie);
					break;
					// The values of a safe array and of an interface are only read by the runtime on Windows (COM)
				}
			}
		}
	}
}
