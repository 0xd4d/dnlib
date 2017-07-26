// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Runtime.Serialization;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Thrown when invalid data is detected while parsing a .NET method
	/// </summary>
	[Serializable]
	public class InvalidMethodException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public InvalidMethodException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Error message</param>
		public InvalidMethodException(string msg)
			: base(msg) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Error message</param>
		/// <param name="innerException">The inner exception or <c>null</c> if none</param>
		public InvalidMethodException(string msg, Exception innerException)
			: base(msg, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected InvalidMethodException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}
}
