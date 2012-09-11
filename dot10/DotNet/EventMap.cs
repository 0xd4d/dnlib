using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the EventMap table
	/// </summary>
	public abstract class EventMap : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.EventMap, rid); }
		}

		/// <summary>
		/// From column EventMap.Parent
		/// </summary>
		public abstract TypeDef Parent { get; set; }

		/// <summary>
		/// From column EventMap.EventList
		/// </summary>
		public abstract IList<EventDef> Events { get; }
	}

	/// <summary>
	/// A EventMap row created by the user and not present in the original .NET file
	/// </summary>
	public class EventMapUser : EventMap {
		TypeDef parent;
		IList<EventDef> events;

		/// <inheritdoc/>
		public override TypeDef Parent {
			get { return parent; }
			set { parent = value; }
		}

		/// <inheritdoc/>
		public override IList<EventDef> Events {
			get { return events; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public EventMapUser() {
			this.events = new List<EventDef>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="event1">Event #1</param>
		public EventMapUser(TypeDef parent, EventDef event1) {
			this.parent = parent;
			this.events = new List<EventDef> { event1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="event1">Event #1</param>
		/// <param name="event2">Event #2</param>
		public EventMapUser(TypeDef parent, EventDef event1, EventDef event2) {
			this.parent = parent;
			this.events = new List<EventDef> { event1, event2 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="event1">Event #1</param>
		/// <param name="event2">Event #2</param>
		/// <param name="event3">Event #3</param>
		public EventMapUser(TypeDef parent, EventDef event1, EventDef event2, EventDef event3) {
			this.parent = parent;
			this.events = new List<EventDef> { event1, event2, event3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="events">Events</param>
		public EventMapUser(TypeDef parent, params EventDef[] events) {
			this.parent = parent;
			this.events = new List<EventDef>(events);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="events">Events</param>
		public EventMapUser(TypeDef parent, IEnumerable<EventDef> events) {
			this.parent = parent;
			this.events = new List<EventDef>(events);
		}
	}

	/// <summary>
	/// Created from a row in the EventMap table
	/// </summary>
	sealed class EventMapMD : EventMap {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawEventMapRow rawRow;

		UserValue<TypeDef> parent;
		LazyList<EventDef> events;

		/// <inheritdoc/>
		public override TypeDef Parent {
			get { return parent.Value; }
			set { parent.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<EventDef> Events {
			get {
				if (events == null) {
					var range = readerModule.MetaData.GetEventRange(rid);
					events = new LazyList<EventDef>((int)range.Length, range, (range2, index) => readerModule.ResolveEvent(((RidRange)range2)[index]));
				}
				return events;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>EventMap</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public EventMapMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.EventMap).Rows < rid)
				throw new BadImageFormatException(string.Format("EventMap rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			parent.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDef(rawRow.Parent);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadEventMapRow(rid);
		}
	}
}
