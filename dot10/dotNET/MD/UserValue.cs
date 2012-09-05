using System;
using System.Diagnostics;

namespace dot10.dotNET.MD {
	delegate T MFunc<T>();

	/// <summary>
	/// Lazily returns the original value if the user hasn't overwritten the value
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	[DebuggerDisplay("{value}")]
	struct UserValue<TValue> {
		MFunc<TValue> readOriginalValue;
		TValue value;
		bool isUserValue;
		bool isValueInitialized;

		/// <summary>
		/// Set a delegate instance that will return the original value
		/// </summary>
		public MFunc<TValue> ReadOriginalValue {
			set { readOriginalValue = value; }
		}

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		/// <remarks>The getter returns the original value if the value hasn't been initialized.</remarks>
		public TValue Value {
			get {
				if (!isValueInitialized) {
					value = readOriginalValue();
					isValueInitialized = true;
				}
				return value;
			}
			set {
				this.value = value;
				isUserValue = true;
				isValueInitialized = true;
			}
		}

		/// <summary>
		/// Returns true if the value has been initialized
		/// </summary>
		public bool IsValueInitialized {
			get { return isValueInitialized; }
		}

		/// <summary>
		/// Returns true if the value was set by the user
		/// </summary>
		public bool IsUserValue {
			get { return isUserValue; }
		}
	}
}
