// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Api {
	/// <summary>
	/// dnlib's public exception types have the usual constructors: no arguments, a message, a message and an inner
	/// exception, and the serialization constructor that gets the values that <see cref="Exception.GetObjectData"/>
	/// stored.
	/// </summary>
	public class ExceptionTypeTests {
		public static TheoryData<string> ExceptionTypes {
			get {
				var data = new TheoryData<string>();
				foreach (var type in GetExceptionTypes())
					data.Add(type.FullName!);
				return data;
			}
		}

		static IEnumerable<Type> GetExceptionTypes() =>
			typeof(ModuleDef).Assembly.GetExportedTypes().Where(a => typeof(Exception).IsAssignableFrom(a) && !a.IsAbstract).OrderBy(a => a.FullName, StringComparer.Ordinal);

		static Type GetType(string fullName) => typeof(ModuleDef).Assembly.GetType(fullName, throwOnError: true)!;

		[Fact]
		public void TheKnownExceptionTypesAreFound() {
			var names = GetExceptionTypes().Select(a => a.Name).ToArray();

			Assert.Contains(nameof(ResolveException), names);
			Assert.Contains(nameof(AssemblyResolveException), names);
			Assert.Contains(nameof(InvalidMetadataException), names);
			Assert.Contains(nameof(dnlib.DotNet.Emit.InvalidMethodException), names);
			Assert.Contains(nameof(dnlib.DotNet.Writer.ModuleWriterException), names);
		}

		[Theory, MemberData(nameof(ExceptionTypes))]
		public void TheConstructorWithoutArgumentsHasADefaultMessage(string fullName) {
			var ctor = GetType(fullName).GetConstructor(Type.EmptyTypes);
			if (ctor is null)
				return;

			var exception = (Exception)ctor.Invoke(null);

			Assert.False(string.IsNullOrEmpty(exception.Message));
			Assert.Null(exception.InnerException);
		}

		[Theory, MemberData(nameof(ExceptionTypes))]
		public void TheMessageIsStored(string fullName) {
			var ctor = GetType(fullName).GetConstructor(new[] { typeof(string) });
			if (ctor is null)
				return;

			var exception = (Exception)ctor.Invoke(new object[] { "the message" });

			Assert.Equal("the message", exception.Message);
		}

		[Theory, MemberData(nameof(ExceptionTypes))]
		public void TheMessageAndTheInnerExceptionAreStored(string fullName) {
			var ctor = GetType(fullName).GetConstructor(new[] { typeof(string), typeof(Exception) });
			if (ctor is null)
				return;
			var inner = new InvalidOperationException("inner");

			var exception = (Exception)ctor.Invoke(new object[] { "the message", inner });

			Assert.Equal("the message", exception.Message);
			Assert.Same(inner, exception.InnerException);
		}

		// The serialization constructor is called by a formatter (eg. BinaryFormatter, which .NET no longer has).
		// It's called here with the values that GetObjectData() stores.
		[Theory, MemberData(nameof(ExceptionTypes))]
		public void TheSerializationConstructorReadsWhatGetObjectDataStored(string fullName) {
			var type = GetType(fullName);
			var serializationCtor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
				new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
			var messageCtor = type.GetConstructor(new[] { typeof(string) });
			if (serializationCtor is null || messageCtor is null)
				return;
			var original = (Exception)messageCtor.Invoke(new object[] { "the message" });
#pragma warning disable SYSLIB0050, SYSLIB0051 // Obsolete serialization APIs
			var info = new SerializationInfo(type, new FormatterConverter());
			var context = new StreamingContext(StreamingContextStates.All);
			original.GetObjectData(info, context);
#pragma warning restore SYSLIB0050, SYSLIB0051

			var copy = (Exception)serializationCtor.Invoke(new object[] { info, context });

			Assert.Equal("the message", copy.Message);
		}
	}
}
