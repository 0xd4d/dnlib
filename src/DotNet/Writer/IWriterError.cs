// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Gets notified of errors. The default handler should normally throw since the written data
	/// will probably be invalid. Any error can be ignored.
	/// </summary>
	public interface IWriterError {
		/// <summary>
		/// Called when an error is detected (eg. a null pointer or other invalid value). The error
		/// can be ignored but the written data won't be valid.
		/// </summary>
		/// <param name="message">Error message</param>
		void Error(string message);
	}
}
