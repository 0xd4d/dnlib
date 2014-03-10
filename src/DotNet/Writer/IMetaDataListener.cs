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

﻿namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Gets notified of various events when writing the metadata
	/// </summary>
	public interface IMetaDataListener {
		/// <summary>
		/// Called by <see cref="MetaData"/>
		/// </summary>
		/// <param name="metaData">The metadata</param>
		/// <param name="evt">Type of metadata event</param>
		void OnMetaDataEvent(MetaData metaData, MetaDataEvent evt);
	}

	/// <summary>
	/// A <see cref="IMetaDataListener"/> which does nothing
	/// </summary>
	public sealed class DummyMetaDataListener : IMetaDataListener {
		/// <summary>
		/// An instance of this dummy listener
		/// </summary>
		public static readonly DummyMetaDataListener Instance = new DummyMetaDataListener();

		/// <inheritdoc/>
		public void OnMetaDataEvent(MetaData metaData, MetaDataEvent evt) {
		}
	}

	/// <summary>
	/// All <see cref="MetaData"/> events
	/// </summary>
	public enum MetaDataEvent {
		/// <summary>
		/// Creating the tables has just begun
		/// </summary>
		BeginCreateTables,

		/// <summary>
		/// The <c>rid</c>s of types, fields, methods, events, properties and parameters are
		/// now known.
		/// </summary>
		MemberDefRidsAllocated,

		/// <summary>
		/// The tables and rows of all types, fields, methods, events, properties and parameters
		/// have been initialized. Method body RVAs are still not known, and no method has been
		/// written yet.
		/// </summary>
		MemberDefsInitialized,

		/// <summary>
		/// Most of the tables that should be sorted have been sorted. The <c>CustomAttribute</c>
		/// table is still unsorted since it's not been created yet.
		/// </summary>
		MostTablesSorted,

		/// <summary>
		/// Custom attributes of all types, fields, methods, events, properties and parameters
		/// have now been written.
		/// </summary>
		MemberDefCustomAttributesWritten,

		/// <summary>
		/// All method bodies are about to be written
		/// </summary>
		BeginWriteMethodBodies,

		/// <summary>
		/// All method bodies have been written. Their RVAs are still not known.
		/// </summary>
		EndWriteMethodBodies,

		/// <summary>
		/// All resources are about to be added to the .NET resources table
		/// </summary>
		BeginAddResources,

		/// <summary>
		/// All resources have been added to the .NET resources table
		/// </summary>
		EndAddResources,

		/// <summary>
		/// All tables are now sorted, including the <c>CustomAttribute</c> table.
		/// </summary>
		OnAllTablesSorted,

		/// <summary>
		/// All tables have been created and all rows populated. The only columns that haven't
		/// been initialized yet are the ones that are RVAs.
		/// </summary>
		EndCreateTables,
	}
}
