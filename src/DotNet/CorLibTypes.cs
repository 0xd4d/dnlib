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

namespace dnlib.DotNet {
	sealed class CorLibTypes : ICorLibTypes {
		readonly ModuleDef module;
		CorLibTypeSig typeVoid;
		CorLibTypeSig typeBoolean;
		CorLibTypeSig typeChar;
		CorLibTypeSig typeSByte;
		CorLibTypeSig typeByte;
		CorLibTypeSig typeInt16;
		CorLibTypeSig typeUInt16;
		CorLibTypeSig typeInt32;
		CorLibTypeSig typeUInt32;
		CorLibTypeSig typeInt64;
		CorLibTypeSig typeUInt64;
		CorLibTypeSig typeSingle;
		CorLibTypeSig typeDouble;
		CorLibTypeSig typeString;
		CorLibTypeSig typeTypedReference;
		CorLibTypeSig typeIntPtr;
		CorLibTypeSig typeUIntPtr;
		CorLibTypeSig typeObject;
		readonly AssemblyRef corLibAssemblyRef;

		/// <inheritdoc/>
		public CorLibTypeSig Void {
			get { return typeVoid; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Boolean {
			get { return typeBoolean; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Char {
			get { return typeChar; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig SByte {
			get { return typeSByte; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Byte {
			get { return typeByte; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int16 {
			get { return typeInt16; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt16 {
			get { return typeUInt16; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int32 {
			get { return typeInt32; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt32 {
			get { return typeUInt32; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Int64 {
			get { return typeInt64; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UInt64 {
			get { return typeUInt64; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Single {
			get { return typeSingle; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Double {
			get { return typeDouble; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig String {
			get { return typeString; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig TypedReference {
			get { return typeTypedReference; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig IntPtr {
			get { return typeIntPtr; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig UIntPtr {
			get { return typeUIntPtr; }
		}

		/// <inheritdoc/>
		public CorLibTypeSig Object {
			get { return typeObject; }
		}

		/// <inheritdoc/>
		public AssemblyRef AssemblyRef {
			get { return corLibAssemblyRef; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		public CorLibTypes(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		/// <param name="corLibAssemblyRef">Corlib assembly reference or <c>null</c> if a default
		/// assembly reference should be created</param>
		public CorLibTypes(ModuleDef module, AssemblyRef corLibAssemblyRef) {
			this.module = module;
			this.corLibAssemblyRef = corLibAssemblyRef ?? CreateCorLibAssemblyRef();
			Initialize();
		}

		AssemblyRef CreateCorLibAssemblyRef() {
			return module.UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR20());
		}

		void Initialize() {
			typeVoid	= new CorLibTypeSig(CreateCorLibTypeRef("Void"),	ElementType.Void);
			typeBoolean	= new CorLibTypeSig(CreateCorLibTypeRef("Boolean"),	ElementType.Boolean);
			typeChar	= new CorLibTypeSig(CreateCorLibTypeRef("Char"),	ElementType.Char);
			typeSByte	= new CorLibTypeSig(CreateCorLibTypeRef("SByte"),	ElementType.I1);
			typeByte	= new CorLibTypeSig(CreateCorLibTypeRef("Byte"),	ElementType.U1);
			typeInt16	= new CorLibTypeSig(CreateCorLibTypeRef("Int16"),	ElementType.I2);
			typeUInt16	= new CorLibTypeSig(CreateCorLibTypeRef("UInt16"),	ElementType.U2);
			typeInt32	= new CorLibTypeSig(CreateCorLibTypeRef("Int32"),	ElementType.I4);
			typeUInt32	= new CorLibTypeSig(CreateCorLibTypeRef("UInt32"),	ElementType.U4);
			typeInt64	= new CorLibTypeSig(CreateCorLibTypeRef("Int64"),	ElementType.I8);
			typeUInt64	= new CorLibTypeSig(CreateCorLibTypeRef("UInt64"),	ElementType.U8);
			typeSingle	= new CorLibTypeSig(CreateCorLibTypeRef("Single"),	ElementType.R4);
			typeDouble	= new CorLibTypeSig(CreateCorLibTypeRef("Double"),	ElementType.R8);
			typeString	= new CorLibTypeSig(CreateCorLibTypeRef("String"),	ElementType.String);
			typeTypedReference = new CorLibTypeSig(CreateCorLibTypeRef("TypedReference"), ElementType.TypedByRef);
			typeIntPtr	= new CorLibTypeSig(CreateCorLibTypeRef("IntPtr"),	ElementType.I);
			typeUIntPtr	= new CorLibTypeSig(CreateCorLibTypeRef("UIntPtr"),	ElementType.U);
			typeObject	= new CorLibTypeSig(CreateCorLibTypeRef("Object"),	ElementType.Object);
		}

		TypeRef CreateCorLibTypeRef(string name) {
			return module.UpdateRowId(new TypeRefUser(module, "System", name, corLibAssemblyRef));
		}

		/// <inheritdoc/>
		public TypeRef GetTypeRef(string @namespace, string name) {
			return module.UpdateRowId(new TypeRefUser(module, @namespace, name, corLibAssemblyRef));
		}
	}
}
