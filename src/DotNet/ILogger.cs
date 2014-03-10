/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Reflection;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet {
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
		Info,

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

	public static partial class Extensions {
		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		public static void Error(this ILogger logger, object sender, string message) {
			logger.Log(sender, LoggerEvent.Error, "{0}", message);
		}

		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		public static void Error(this ILogger logger, object sender, string message, object arg1) {
			logger.Log(sender, LoggerEvent.Error, message, arg1);
		}

		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		public static void Error(this ILogger logger, object sender, string message, object arg1, object arg2) {
			logger.Log(sender, LoggerEvent.Error, message, arg1, arg2);
		}

		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		public static void Error(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3) {
			logger.Log(sender, LoggerEvent.Error, message, arg1, arg2, arg3);
		}

		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		/// <param name="arg4">Message arg #4</param>
		public static void Error(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3, object arg4) {
			logger.Log(sender, LoggerEvent.Error, message, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Log an error message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="args">Message arguments</param>
		public static void Error(this ILogger logger, object sender, string message, params object[] args) {
			logger.Log(sender, LoggerEvent.Error, message, args);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		public static void Warning(this ILogger logger, object sender, string message) {
			logger.Log(sender, LoggerEvent.Warning, "{0}", message);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		public static void Warning(this ILogger logger, object sender, string message, object arg1) {
			logger.Log(sender, LoggerEvent.Warning, message, arg1);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		public static void Warning(this ILogger logger, object sender, string message, object arg1, object arg2) {
			logger.Log(sender, LoggerEvent.Warning, message, arg1, arg2);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		public static void Warning(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3) {
			logger.Log(sender, LoggerEvent.Warning, message, arg1, arg2, arg3);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		/// <param name="arg4">Message arg #4</param>
		public static void Warning(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3, object arg4) {
			logger.Log(sender, LoggerEvent.Warning, message, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="args">Message arguments</param>
		public static void Warning(this ILogger logger, object sender, string message, params object[] args) {
			logger.Log(sender, LoggerEvent.Warning, message, args);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		public static void Info(this ILogger logger, object sender, string message) {
			logger.Log(sender, LoggerEvent.Info, "{0}", message);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		public static void Info(this ILogger logger, object sender, string message, object arg1) {
			logger.Log(sender, LoggerEvent.Info, message, arg1);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		public static void Info(this ILogger logger, object sender, string message, object arg1, object arg2) {
			logger.Log(sender, LoggerEvent.Info, message, arg1, arg2);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		public static void Info(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3) {
			logger.Log(sender, LoggerEvent.Info, message, arg1, arg2, arg3);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		/// <param name="arg4">Message arg #4</param>
		public static void Info(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3, object arg4) {
			logger.Log(sender, LoggerEvent.Info, message, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="args">Message arguments</param>
		public static void Info(this ILogger logger, object sender, string message, params object[] args) {
			logger.Log(sender, LoggerEvent.Info, message, args);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		public static void Verbose(this ILogger logger, object sender, string message) {
			logger.Log(sender, LoggerEvent.Verbose, "{0}", message);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		public static void Verbose(this ILogger logger, object sender, string message, object arg1) {
			logger.Log(sender, LoggerEvent.Verbose, message, arg1);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		public static void Verbose(this ILogger logger, object sender, string message, object arg1, object arg2) {
			logger.Log(sender, LoggerEvent.Verbose, message, arg1, arg2);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		public static void Verbose(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3) {
			logger.Log(sender, LoggerEvent.Verbose, message, arg1, arg2, arg3);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		/// <param name="arg4">Message arg #4</param>
		public static void Verbose(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3, object arg4) {
			logger.Log(sender, LoggerEvent.Verbose, message, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Log a verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="args">Message arguments</param>
		public static void Verbose(this ILogger logger, object sender, string message, params object[] args) {
			logger.Log(sender, LoggerEvent.Verbose, message, args);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message) {
			logger.Log(sender, LoggerEvent.VeryVerbose, "{0}", message);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message, object arg1) {
			logger.Log(sender, LoggerEvent.VeryVerbose, message, arg1);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message, object arg1, object arg2) {
			logger.Log(sender, LoggerEvent.VeryVerbose, message, arg1, arg2);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3) {
			logger.Log(sender, LoggerEvent.VeryVerbose, message, arg1, arg2, arg3);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="arg1">Message arg #1</param>
		/// <param name="arg2">Message arg #2</param>
		/// <param name="arg3">Message arg #3</param>
		/// <param name="arg4">Message arg #4</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message, object arg1, object arg2, object arg3, object arg4) {
			logger.Log(sender, LoggerEvent.VeryVerbose, message, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Log a very verbose message
		/// </summary>
		/// <param name="logger">this</param>
		/// <param name="sender">Sender or <c>null</c></param>
		/// <param name="message">Message</param>
		/// <param name="args">Message arguments</param>
		public static void VeryVerbose(this ILogger logger, object sender, string message, params object[] args) {
			logger.Log(sender, LoggerEvent.VeryVerbose, message, args);
		}
	}

	/// <summary>
	/// Dummy logger which ignores all messages, but can optionally throw on errors.
	/// </summary>
	public sealed class DummyLogger : ILogger {
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
