// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The type of a property is in its signature (the byte 8 or 0x28, the number of parameters, the type
	/// and the types of the parameters), the type of an event is a token. These tests write signatures and
	/// tokens that a compiler doesn't write and read the type with dnlib and with the runtime that runs the
	/// tests. If the runtime has a type, dnlib must have the same one.
	/// </summary>
	public class ClrPropertyAndEventTypeRuleTests {
		const string Source = @"
public class Marker {
	public int Property { get; set; }
	public event System.Action Event { add { } remove { } }
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly string objectToken;
		static readonly uint actionToken, markerToken;

		static ClrPropertyAndEventTypeRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				// The token of a type in a signature or in the EventType column: the row and (in the 2 low
				// bits) the table (0: TypeDef, 1: TypeRef, 2: TypeSpec)
				uint token = module.GetTypeRefs().Single(a => a.FullName == "System.Object").Rid << 2 | 1;
				Assert.True(token < 0x80);
				objectToken = token.ToString("X2");
				actionToken = module.GetTypeRefs().Single(a => a.FullName == "System.Action").Rid << 2 | 1;
				markerToken = module.Find("Marker", isReflectionName: true)!.Rid << 2;
			}
		}

		// The bytes as hex numbers. {Object} is the token of System.Object. The signature is added to the
		// end of the #Blob heap; the property then has that signature.
		static byte[] CreateImageWithPropertySignature(string signature) {
			var bytes = signature.Replace("{Object}", objectToken).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(new MetadataPatcher(baseImage).SetCell(Table.Property, 1, "Type", (uint)heapLength).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		[Theory]
		[InlineData("28 00 08", "System.Int32", "System.Int32")]
		// The first byte: 8 = a property, 0x20 = it has a this parameter. Fixed in this commit: the CLR reads
		// the signature of a property as a property signature whatever the first byte says (if it's not 6,
		// a field signature). dnlib read eg. a method signature and the property had no PropertySig.
		[InlineData("08 00 08", "System.Int32", "System.Int32")]
		[InlineData("06 00 08", "no signature", "throws BadImageFormatException")]
		[InlineData("00 00 08", "System.Int32", "System.Int32")]
		[InlineData("20 00 08", "System.Int32", "System.Int32")]
		[InlineData("07 00 08", "System.Int32", "System.Int32")]
		[InlineData("48 00 08", "System.Int32", "System.Int32")]
		[InlineData("88 00 08", "System.Int32", "System.Int32")]
		[InlineData("2B 00 08", "System.Int32", "System.Int32")]
		// The number of parameters
		[InlineData("28 01 08 0E", "System.Int32", "System.Int32")]
		[InlineData("28 01 08", "no signature", "throws BadImageFormatException")]
		[InlineData("28 00 08 0E", "System.Int32", "System.Int32")]
		[InlineData("28 80 00 08", "System.Int32", "System.Int32")]
		[InlineData("28 00", "no signature", "throws BadImageFormatException")]
		[InlineData("28", "no signature", "throws BadImageFormatException")]
		[InlineData("", "no signature", "throws BadImageFormatException")]
		// The type
		[InlineData("28 00 0E", "System.String", "System.String")]
		[InlineData("28 00 01", "System.Void", "System.Void")]
		[InlineData("28 00 10 08", "System.Int32&", "System.Int32&")]
		[InlineData("28 00 16", "System.TypedReference", "System.TypedReference")]
		[InlineData("28 00 1D 08", "System.Int32[]", "System.Int32[]")]
		[InlineData("28 00 20 {Object} 08", "System.Int32", "System.Int32")]
		[InlineData("28 00 45 08", "System.Int32", "System.Int32")]
		[InlineData("28 00 41 08", "System.Int32", "System.Int32")]
		[InlineData("28 00 00", "no type", "throws BadImageFormatException")]
		public void TypeOfAProperty(string signature, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithPropertySignature(signature);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var sig = module.Find("Marker", isReflectionName: true)!.Properties.Single().PropertySig;
				dnlib = sig is null ? "no signature" : sig.RetType?.ReflectionFullName ?? "no type";
			}
			string Runtime() {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.GetType("Marker", throwOnError: true)!.GetProperties(Declared).Single().PropertyType.FullName!;
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, Runtime);
		}

		// The EventType column of the event's row
		static byte[] CreateImageWithEventType(string eventType) {
			uint token;
			switch (eventType) {
			case "System.Action": token = actionToken; break;
			case "Marker": token = markerToken; break;
			case "System.Object": token = Convert.ToUInt32(objectToken, 16); break;
			case "0": token = 0; break;
			case "table 3": token = (actionToken & ~3U) | 3; break;
			case "type row that doesn't exist": token = 1000 << 2; break;
			case "type reference row that doesn't exist": token = 1000 << 2 | 1; break;
			case "type specification row that doesn't exist": token = 1 << 2 | 2; break;
			default: throw new ArgumentException(eventType);
			}
			return new MetadataPatcher(baseImage).SetCell(Table.Event, 1, "EventType", token).ToArray();
		}

		// The runtime isn't asked: System.Reflection doesn't use this column, its type of an event is the
		// type of the parameter of the add method
		[Theory]
		[InlineData("System.Action", "System.Action")]
		[InlineData("Marker", "Marker")]
		[InlineData("System.Object", "System.Object")]
		[InlineData("0", "no type")]
		[InlineData("table 3", "no type")]
		[InlineData("type row that doesn't exist", "no type")]
		[InlineData("type reference row that doesn't exist", "no type")]
		[InlineData("type specification row that doesn't exist", "no type")]
		public void TypeOfAnEvent(string eventType, string expectedDnlib) {
			var image = CreateImageWithEventType(eventType);

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(expectedDnlib, module.Find("Marker", isReflectionName: true)!.Events.Single().EventType?.ReflectionFullName ?? "no type");
		}

		// The first byte of the signature is the same after the file is written
		[Fact]
		public void PropertySignatureWithAnotherFirstByteSurvivesAWrite() {
			using (var module = ModuleDefMD.Load(CreateImageWithPropertySignature("20 00 08"))) {
				var property = module.Find("Marker", isReflectionName: true)!.Properties.Single();
				Assert.Equal("System.Int32", property.PropertySig!.RetType!.FullName);
				Assert.Equal(CallingConvention.HasThis, property.PropertySig.GetCallingConvention());

				var stream = new System.IO.MemoryStream();
				module.Write(stream);
				using (var written = ModuleDefMD.Load(stream.ToArray())) {
					var writtenProperty = written.Find("Marker", isReflectionName: true)!.Properties.Single();
					Assert.Equal(CallingConvention.HasThis, writtenProperty.PropertySig!.GetCallingConvention());
					Assert.Equal("System.Int32", writtenProperty.PropertySig.RetType!.FullName);
				}
			}
		}
	}
}
