// dnlib: See LICENSE.txt for more info

using System;
using System.Text;

namespace dnlib.DotNet.Writer {
	sealed class MetadataErrorContext {
		sealed class ErrorSource : IDisposable {
			MetadataErrorContext context;
			readonly ErrorSource originalValue;

			public object Value { get; }

			public ErrorSource(MetadataErrorContext context, object value) {
				this.context = context;
				Value = value;
				originalValue = context.source;
			}

			public void Dispose() {
				if (context is null)
					return;
				context.source = originalValue;
				context = null;
			}
		}

		ErrorSource source;

		public MetadataEvent Event { get; set; }

		public IDisposable SetSource(object source) => this.source = new ErrorSource(this, source);

		public void Append(string errorLevel, ref string message, ref object[] args) {
			int count = 1;
			var stringSource = source?.Value as string;
			var tokenSource = source?.Value as IMDTokenProvider;
			if (tokenSource is not null)
				count += 2;
			int ctxArgIndex = args.Length;

			var newMessage = new StringBuilder(message);
			var newArgs = new object[args.Length + count];
			Array.Copy(args, 0, newArgs, 0, args.Length);

			if (newMessage.Length != 0 && newMessage[newMessage.Length - 1] != '.')
				newMessage.Append('.');
			newMessage.AppendFormat(" {0} occurred after metadata event {{{1}}}", errorLevel, ctxArgIndex);
			newArgs[ctxArgIndex] = Event;

			if (tokenSource is not null) {
				string sourceType = tokenSource switch {
					TypeDef => "type",
					FieldDef => "field",
					MethodDef => "method",
					EventDef => "event",
					PropertyDef => "property",
					_ => "???"
				};
				newMessage.AppendFormat(" during writing {0} '{{{1}}}' (0x{{{2}:X8}})", sourceType, ctxArgIndex + 1, ctxArgIndex + 2);
				newArgs[ctxArgIndex + 1] = tokenSource;
				newArgs[ctxArgIndex + 2] = tokenSource.MDToken.Raw;
			}
			else if (stringSource is not null) {
				newMessage.AppendFormat(" during writing {0}", stringSource);
			}

			message = newMessage.Append('.').ToString();
			args = newArgs;
		}
	}
}
