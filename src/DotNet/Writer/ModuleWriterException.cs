using System;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Thrown when the module writer encounters an unrecoverable error
	/// </summary>
	[Serializable]
	class ModuleWriterException : Exception {
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
	}
}
