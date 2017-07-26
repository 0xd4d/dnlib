// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Runtime.Serialization;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Thrown when the module writer encounters an unrecoverable error
	/// </summary>
	[Serializable]
	public class ModuleWriterException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleWriterException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		public ModuleWriterException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="innerException">Other exception</param>
		public ModuleWriterException(string message, Exception innerException)
			: base(message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected ModuleWriterException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}
}
