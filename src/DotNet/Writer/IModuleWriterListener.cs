// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Gets notified of various events when writing a module
	/// </summary>
	public interface IModuleWriterListener {
		/// <summary>
		/// Called by <see cref="ModuleWriterBase"/> and its sub classes.
		/// </summary>
		/// <param name="writer">The module writer</param>
		/// <param name="evt">Type of writer event</param>
		void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt);
	}

	/// <summary>
	/// A <see cref="IModuleWriterListener"/> which does nothing
	/// </summary>
	public sealed class DummyModuleWriterListener : IModuleWriterListener {
		/// <summary>
		/// An instance of this dummy listener
		/// </summary>
		public static readonly DummyModuleWriterListener Instance = new DummyModuleWriterListener();

		/// <inheritdoc/>
		public void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt) {
		}
	}
}
