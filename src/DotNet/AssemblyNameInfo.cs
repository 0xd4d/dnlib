/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Stores assembly name information
	/// </summary>
	public sealed class AssemblyNameInfo {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyAttributes flags;
		PublicKeyBase publicKeyOrToken;
		UTF8String name;
		UTF8String locale;

		/// <summary>
		/// Gets/sets the <see cref="AssemblyHashAlgorithm"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId; }
			set { hashAlgId = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="Version"/> or <c>null</c> if none specified
		/// </summary>
		public Version Version {
			get { return version; }
			set { version = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes"/>
		/// </summary>
		public AssemblyAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// Gets/sets the public key or token
		/// </summary>
		public PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken; }
			set { publicKeyOrToken = value; }
		}

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets/sets the locale or <c>null</c> if none specified
		/// </summary>
		public UTF8String Locale {
			get { return locale; }
			set { locale = value; }
		}

		/// <summary>
		/// Gets the full name of the assembly
		/// </summary>
		public string FullName {
			get { return Utils.GetAssemblyNameString(name, version, locale, publicKeyOrToken); }
		}

		/// <summary>
		/// Gets the full name of the assembly but use a public key token
		/// </summary>
		public string FullNameToken {
			get {
				var pk = publicKeyOrToken;
				if (pk is PublicKey)
					pk = (pk as PublicKey).Token;
				return Utils.GetAssemblyNameString(name, version, locale, pk);
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyNameInfo() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmFullName">An assembly name</param>
		public AssemblyNameInfo(string asmFullName)
			: this(ReflectionTypeNameParser.ParseAssemblyRef(asmFullName)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asm">The assembly</param>
		public AssemblyNameInfo(IAssembly asm) {
			if (asm == null)
				return;
			var asmDef = asm as AssemblyDef;
			this.hashAlgId = asmDef == null ? 0 : asmDef.HashAlgorithm;
			this.version = asm.Version ?? new Version(0, 0, 0, 0);
			this.flags = asm.Attributes;
			this.publicKeyOrToken = asm.PublicKeyOrToken;
			this.name = UTF8String.IsNullOrEmpty(asm.Name) ? UTF8String.Empty : asm.Name;
			this.locale = UTF8String.IsNullOrEmpty(asm.Culture) ? UTF8String.Empty : asm.Culture;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		public AssemblyNameInfo(AssemblyName asmName) {
			if (asmName == null)
				return;
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.flags = (AssemblyAttributes)asmName.Flags;
			this.publicKeyOrToken = (PublicKeyBase)PublicKeyBase.CreatePublicKey(asmName.GetPublicKey()) ??
							PublicKeyBase.CreatePublicKeyToken(asmName.GetPublicKeyToken());
			this.name = asmName.Name ?? string.Empty;
			this.locale = asmName.CultureInfo != null && asmName.CultureInfo.Name != null ? asmName.CultureInfo.Name : string.Empty;
		}

		/// <inhertidoc/>
		public override string ToString() {
			return FullName;
		}
	}
}
