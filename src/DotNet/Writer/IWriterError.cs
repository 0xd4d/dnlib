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

	/// <summary>
	/// Gets notified of errors. The default handler should normally throw since the written data
	/// will probably be invalid. Any error can be ignored.
	/// </summary>
	public interface IWriterError2 : IWriterError {
		/// <summary>
		/// Called when an error is detected (eg. a null pointer or other invalid value). The error
		/// can be ignored but the written data won't be valid.
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="args">Optional message arguments</param>
		void Error(string message, params object[] args);
	}

	static partial class Extensions {
		/// <summary>
		/// Called when an error is detected (eg. a null pointer or other invalid value). The error
		/// can be ignored but the written data won't be valid.
		/// </summary>
		/// <param name="helper">The instance of <see cref="IWriterError"/></param>
		/// <param name="message">Error message</param>
		/// <param name="args">Optional message arguments</param>
		internal static void Error2(this IWriterError helper, string message, params object[] args) {
			if (helper is IWriterError2 helper2)
				helper2.Error(message, args);
			else
				helper.Error(string.Format(message, args));
		}
	}
}
