// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace dnlib.DotNet {
	/// <summary>
	/// Thrown by the <c>XyzOrThrow</c> properties (eg. <see cref="FieldDef.FieldSigOrThrow"/>) if the value is
	/// <c>null</c> and it's a value that's always present in valid metadata, ie. the metadata is invalid or the
	/// instance hasn't been fully initialized. Optional values (eg. <see cref="MethodDef.BodyOrThrow"/>) throw
	/// a <see cref="NullValueException"/> instead.
	/// The properties without the <c>OrThrow</c> suffix return <c>null</c> instead of throwing.
	/// </summary>
	[Serializable]
	public class InvalidMetadataException : InvalidOperationException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public InvalidMetadataException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public InvalidMetadataException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public InvalidMetadataException(string message, Exception? innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected InvalidMetadataException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Thrown by the <c>XyzOrThrow</c> properties of optional values if there's no value, ie. it's <c>null</c>.
	/// These values can be <c>null</c> in valid metadata: a method doesn't need to have a body
	/// (<see cref="MethodDef.BodyOrThrow"/>), only nested types have a declaring type
	/// (<see cref="IMemberRef.DeclaringTypeOrThrow"/>), a type that hasn't been added to a module has no owner
	/// module (<see cref="IOwnerModule.ModuleOrThrow"/>), etc. Values that are only <c>null</c> if the metadata
	/// is invalid throw an <see cref="InvalidMetadataException"/> instead.
	/// The properties without the <c>OrThrow</c> suffix return <c>null</c> instead of throwing.
	/// </summary>
	[Serializable]
	public class NullValueException : InvalidOperationException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public NullValueException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public NullValueException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public NullValueException(string message, Exception? innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected NullValueException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Used by the <c>XyzOrThrow</c> properties. dnlib itself must not use those properties since it must
	/// be able to read invalid metadata without throwing.
	/// </summary>
	static class OrThrowHelper {
		/// <summary>
		/// Returns <paramref name="value"/> or throws an <see cref="InvalidMetadataException"/> if it's <c>null</c>.
		/// Use it if the value is never <c>null</c> in valid metadata.
		/// </summary>
		public static T NotNull<T>(T? value, string memberName) where T : class {
			if (value is null)
				ThrowIsNull(memberName);
			return value;
		}

		/// <summary>
		/// Returns <paramref name="value"/> or throws a <see cref="NullValueException"/> if it's <c>null</c>.
		/// Use it if the value is optional, ie. it can be <c>null</c> in valid metadata.
		/// </summary>
		public static T NotNullOptional<T>(T? value, string memberName) where T : class {
			if (value is null)
				ThrowOptionalIsNull(memberName);
			return value;
		}

		/// <summary>
		/// Returns <paramref name="list"/> or throws if any of its elements is <c>null</c>. The same list
		/// instance is returned, there's no copy.
		/// </summary>
		public static IList<T> NoNullElements<T>(IList<T?> list, string memberName) where T : class {
			int count = list.Count;
			for (int i = 0; i < count; i++) {
				if (list[i] is null)
					ThrowHasNullElement(memberName, i);
			}
			return list!;
		}

		[DoesNotReturn]
		static void ThrowIsNull(string memberName) =>
			throw new InvalidMetadataException($"{memberName} is null");

		[DoesNotReturn]
		static void ThrowOptionalIsNull(string memberName) =>
			throw new NullValueException($"{memberName} is null");

		[DoesNotReturn]
		static void ThrowHasNullElement(string memberName, int index) =>
			throw new InvalidMetadataException($"{memberName}[{index}] is null");
	}
}
