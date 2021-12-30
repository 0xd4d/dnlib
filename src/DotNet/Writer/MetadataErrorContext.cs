using System;
using System.Text;

namespace dnlib.DotNet.Writer {
	sealed class MetadataErrorContext {
		public MetadataEvent Event { get; set; }

		public object Source { get; set; }

		public void Append(string errorLevel, ref string message, ref object[] args) {
			int count = 1;
			var stringSource = Source as string;
			var tokenSource = Source as IMDTokenProvider;
			if (tokenSource is not null)
				count += 2;
			int ctxArgIndex = args.Length;

			var newMessage = new StringBuilder(message);
			var newArgs = new object[args.Length + count];
			Array.Copy(args, 0, newArgs, 0, args.Length);

			newMessage.AppendFormat(" {0} occurred after metadata event {{{1}}}", errorLevel, ctxArgIndex);
			newArgs[ctxArgIndex] = Event;

			if (tokenSource is not null) {
				string sourceType = tokenSource switch {
					TypeDef => "type",
					FieldDef => "field",
					MethodDef => "method",
					EventDef => "event",
					PropertyDef => "property",
					_ => throw new InvalidOperationException()
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
