using System;
using System.Reflection;
using dot10.DotNet.Writer;

namespace dot10.DotNet {
	/// <summary>
	/// <see cref="ILogger"/> events
	/// </summary>
	public enum LoggerEvent {
		/// <summary>
		/// An error was detected. An exception should normally be thrown but the error
		/// can be ignored.
		/// </summary>
		Error,

		/// <summary>
		/// Just a warning and can be ignored.
		/// </summary>
		Warning,

		/// <summary>
		/// A normal message
		/// </summary>
		Message,

		/// <summary>
		/// A verbose message
		/// </summary>
		Verbose,

		/// <summary>
		/// A very verbose message
		/// </summary>
		VeryVerbose,
	}

	/// <summary>
	/// Simple logger
	/// </summary>
	public interface ILogger {
		/// <summary>
		/// Log something
		/// </summary>
		/// <param name="sender">Caller or <c>null</c></param>
		/// <param name="loggerEvent">Logger event</param>
		/// <param name="format">Format</param>
		/// <param name="args">Arguments</param>
		void Log(object sender, LoggerEvent loggerEvent, string format, params object[] args);

		/// <summary>
		/// <c>true</c> if this event is ignored. If the event is ignored, the caller can
		/// choose not to call <see cref="Log"/>. This is useful if it can take time to
		/// prepare the message.
		/// </summary>
		/// <param name="loggerEvent">The logger event</param>
		bool IgnoresEvent(LoggerEvent loggerEvent);
	}

	/// <summary>
	/// Dummy logger which ignores all messages, but can optionally throw on errors.
	/// </summary>
	public class DummyLogger : ILogger {
		ConstructorInfo ctor;

		/// <summary>
		/// It ignores everything and doesn't throw anything.
		/// </summary>
		public static readonly DummyLogger NoThrowInstance = new DummyLogger();

		/// <summary>
		/// Throws a <see cref="ModuleWriterException"/> on errors, but ignores anything else.
		/// </summary>
		public static readonly DummyLogger ThrowModuleWriterExceptionOnErrorInstance = new DummyLogger(typeof(ModuleWriterException));

		DummyLogger() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exceptionToThrow">If non-<c>null</c>, this exception type is thrown on
		/// errors. It must have a public constructor that takes a <see cref="string"/> as the only
		/// argument.</param>
		public DummyLogger(Type exceptionToThrow) {
			if (exceptionToThrow != null) {
				if (!exceptionToThrow.IsSubclassOf(typeof(Exception)))
					throw new ArgumentException(string.Format("Not a System.Exception sub class: {0}", exceptionToThrow.GetType()));
				ctor = exceptionToThrow.GetConstructor(new Type[] { typeof(string) });
				if (ctor == null)
					throw new ArgumentException(string.Format("Exception type {0} doesn't have a public constructor that takes a string as the only argument", exceptionToThrow.GetType()));
			}
		}

		/// <inheritdoc/>
		public void Log(object sender, LoggerEvent loggerEvent, string format, params object[] args) {
			if (loggerEvent == LoggerEvent.Error && ctor != null)
				throw (Exception)ctor.Invoke(new object[] { string.Format(format, args) });
		}

		/// <inheritdoc/>
		public bool IgnoresEvent(LoggerEvent loggerEvent) {
			if (ctor == null)
				return true;
			return loggerEvent != LoggerEvent.Error;
		}
	}
}
