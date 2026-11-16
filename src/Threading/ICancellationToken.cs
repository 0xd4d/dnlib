// dnlib: See LICENSE.txt for more info

#nullable enable

using System;

namespace dnlib.Threading {
	/// <summary>
	/// Cancellation token interface
	/// </summary>
	public interface ICancellationToken {
		/// <summary>
		/// Throws a <see cref="OperationCanceledException"/> if the operation should be canceled
		/// </summary>
		void ThrowIfCancellationRequested();
	}
}
