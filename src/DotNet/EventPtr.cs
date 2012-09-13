using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the EventPtr table
	/// </summary>
	public abstract class EventPtr : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.EventPtr, rid); }
		}

		/// <summary>
		/// From column EventPtr.Event
		/// </summary>
		public abstract EventDef Event { get; set; }
	}

	/// <summary>
	/// An EventPtr row created by the user and not present in the original .NET file
	/// </summary>
	public class EventPtrUser : EventPtr {
		EventDef @event;

		/// <inheritdoc/>
		public override EventDef Event {
			get { return @event; }
			set { @event = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public EventPtrUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="event">Event</param>
		public EventPtrUser(EventDef @event) {
			this.@event = @event;
		}
	}

	/// <summary>
	/// Created from a row in the EventPtr table
	/// </summary>
	sealed class EventPtrMD : EventPtr {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawEventPtrRow rawRow;

		UserValue<EventDef> @event;

		/// <inheritdoc/>
		public override EventDef Event {
			get { return @event.Value; }
			set { @event.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>EventPtr</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public EventPtrMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.EventPtr).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("EventPtr rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			@event.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveEvent(rawRow.Event);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadEventPtrRow(rid);
		}
	}
}
