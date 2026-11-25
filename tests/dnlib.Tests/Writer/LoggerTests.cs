// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The writers and some readers report problems to an <see cref="ILogger"/>. It has one method,
	/// <see cref="ILogger.Log"/>; the extension methods <c>Error()</c>, <c>Warning()</c>, <c>Info()</c>,
	/// <c>Verbose()</c> and <c>VeryVerbose()</c> call it with the kind of message. <see cref="DummyLogger"/> is
	/// the logger that ignores everything or throws on the first error.
	/// </summary>
	public class LoggerTests {
		sealed class RecordingLogger : ILogger {
			public readonly List<string> Messages = new List<string>();
			public object? LastSender;

			public void Log(object? sender, LoggerEvent loggerEvent, string format, params object?[] args) {
				LastSender = sender;
				Messages.Add(loggerEvent + ": " + string.Format(format, args));
			}

			public bool IgnoresEvent(LoggerEvent loggerEvent) => false;
		}

		// Every kind of message has six methods: with 0 to 4 arguments and with an array of arguments. They're
		// found with reflection (30 methods).
		[Fact]
		public void EveryExtensionMethodLogsItsKindOfMessageWithItsArguments() {
			var kinds = Enum.GetNames(typeof(LoggerEvent));
			var methods = typeof(dnlib.DotNet.Extensions).GetMethods()
				.Where(a => kinds.Contains(a.Name) && a.GetParameters().Length >= 3 && a.GetParameters()[0].ParameterType == typeof(ILogger)).ToList();
			Assert.Equal(kinds.Length * 6, methods.Count);
			var sender = new object();

			foreach (var method in methods) {
				var parameters = method.GetParameters();
				bool hasArray = parameters[parameters.Length - 1].ParameterType == typeof(object[]);
				// The values of the message's arguments: "arg0", "arg1", ...
				int argumentCount = hasArray ? 6 : parameters.Length - 3;
				var values = Enumerable.Range(0, argumentCount).Select(a => (object?)("arg" + a)).ToArray();
				var format = "message" + string.Concat(Enumerable.Range(0, argumentCount).Select(a => " {" + a + "}"));
				var arguments = new List<object?> { null, sender, format };
				if (hasArray)
					arguments.Add(values);
				else
					arguments.AddRange(values);
				var logger = new RecordingLogger();
				arguments[0] = logger;

				method.Invoke(null, arguments.ToArray());

				var expected = method.Name + ": message" + string.Concat(values.Select(a => " " + a));
				Assert.Equal(new[] { expected }, logger.Messages);
				Assert.Same(sender, logger.LastSender);
			}
		}

		[Fact]
		public void LoggerThatDoesNotThrowIgnoresEverything() {
			var logger = DummyLogger.NoThrowInstance;

			foreach (LoggerEvent kind in Enum.GetValues(typeof(LoggerEvent))) {
				logger.Log(this, kind, "message {0}", 1);
				Assert.True(logger.IgnoresEvent(kind));
			}
		}

		[Fact]
		public void LoggerThatThrowsOnlyThrowsForAnError() {
			var logger = DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;

			var exception = Assert.Throws<ModuleWriterException>(() => logger.Error(this, "Value {0} and {1}", 1, "two"));
			Assert.Equal("Value 1 and two", exception.Message);
			logger.Warning(this, "message");
			logger.Info(this, "message");
			Assert.False(logger.IgnoresEvent(LoggerEvent.Error));
			Assert.True(logger.IgnoresEvent(LoggerEvent.Warning));
			Assert.True(logger.IgnoresEvent(LoggerEvent.VeryVerbose));
		}

		[Fact]
		public void LoggerCanThrowAnyExceptionThatIsCreatedFromAMessage() {
			var logger = new DummyLogger(typeof(InvalidOperationException));

			var exception = Assert.Throws<InvalidOperationException>(() => logger.Error(this, "Problem {0}", 5));
			Assert.Equal("Problem 5", exception.Message);
			// No exception type: nothing is thrown
			new DummyLogger(null).Error(this, "ignored");
		}

		sealed class ExceptionWithoutAMessageConstructor : Exception {
		}

		[Fact]
		public void TypeThatCanNotBeThrownIsRejected() {
			var notAnException = Assert.Throws<ArgumentException>(() => new DummyLogger(typeof(string)));
			Assert.Contains("System.String", notAnException.Message);
			var noConstructor = Assert.Throws<ArgumentException>(() => new DummyLogger(typeof(ExceptionWithoutAMessageConstructor)));
			Assert.Contains(nameof(ExceptionWithoutAMessageConstructor), noConstructor.Message);
		}
	}
}
