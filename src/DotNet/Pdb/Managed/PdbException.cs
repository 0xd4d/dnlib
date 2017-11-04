// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Runtime.Serialization;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// Exception that is thrown when <see cref="PdbReader"/> encounters an error.
	/// </summary>
	[Serializable]
	sealed class PdbException : Exception {
		/// <summary>
		/// Constructor
		/// </summary>
		public PdbException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public PdbException(string message)
			: base("Failed to read PDB: " + message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="innerException">Inner exception</param>
		public PdbException(Exception innerException)
			: base("Failed to read PDB: " + innerException.Message, innerException) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public PdbException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}
}
