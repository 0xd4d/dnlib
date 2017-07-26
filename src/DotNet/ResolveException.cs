// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Runtime.Serialization;

namespace dnlib.DotNet {
	/// <summary>
	/// Resolve exception base class
	/// </summary>
	[Serializable]
	public class ResolveException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public ResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public ResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected ResolveException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Thrown if an assembly couldn't be resolved
	/// </summary>
	[Serializable]
	public class AssemblyResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public AssemblyResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public AssemblyResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected AssemblyResolveException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Thrown if a type couldn't be resolved
	/// </summary>
	[Serializable]
	public class TypeResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public TypeResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public TypeResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected TypeResolveException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Thrown if a method/field couldn't be resolved
	/// </summary>
	[Serializable]
	public class MemberRefResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public MemberRefResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public MemberRefResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public MemberRefResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected MemberRefResolveException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}
}
