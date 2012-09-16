using System;

namespace dot10.DotNet.Emit {
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
	}
}
