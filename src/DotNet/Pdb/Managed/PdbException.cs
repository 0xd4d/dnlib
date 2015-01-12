// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// Exception that is thrown when <see cref="PdbReader"/> encounters an error.
	/// </summary>
	[Serializable]
	public sealed class PdbException : Exception {
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
	}
}