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

using System.Collections.Generic;
using System.Text;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A <c>DeclSecurity</c> security attribute
	/// </summary>
	public sealed class SecurityAttribute : ICustomAttribute {
		ITypeDefOrRef attrType;
		readonly ThreadSafe.IList<CANamedArgument> namedArguments;

		/// <summary>
		/// Gets/sets the attribute type
		/// </summary>
		public ITypeDefOrRef AttributeType {
			get { return attrType; }
			set { attrType = value; }
		}

		/// <summary>
		/// Gets the full name of the attribute type
		/// </summary>
		public string TypeFullName {
			get {
				var at = attrType;
				return at == null ? string.Empty : at.FullName;
			}
		}

		/// <summary>
		/// Gets all named arguments (field and property values)
		/// </summary>
		public ThreadSafe.IList<CANamedArgument> NamedArguments {
			get { return namedArguments; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="NamedArguments"/> is not empty
		/// </summary>
		public bool HasNamedArguments {
			get { return namedArguments.Count > 0; }
		}

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are field arguments
		/// </summary>
		public IEnumerable<CANamedArgument> Fields {
			get {
				foreach (var namedArg in namedArguments.GetSafeEnumerable()) {
					if (namedArg.IsField)
						yield return namedArg;
				}
			}
		}

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are property arguments
		/// </summary>
		public IEnumerable<CANamedArgument> Properties {
			get {
				foreach (var namedArg in namedArguments.GetSafeEnumerable()) {
					if (namedArg.IsProperty)
						yield return namedArg;
				}
			}
		}

		/// <summary>
		/// Creates a <see cref="SecurityAttribute"/> from an XML string.
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="xml">XML</param>
		/// <returns>A new <see cref="SecurityAttribute"/> instance</returns>
		public static SecurityAttribute CreateFromXml(ModuleDef module, string xml) {
			var attrType = module.CorLibTypes.GetTypeRef("System.Security.Permissions", "PermissionSetAttribute");
			var utf8Xml = new UTF8String(xml);
			var namedArg = new CANamedArgument(false, module.CorLibTypes.String, "XML", new CAArgument(module.CorLibTypes.String, utf8Xml));
			var list = ThreadSafeListCreator.Create<CANamedArgument>(namedArg);
			return new SecurityAttribute(attrType, list);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public SecurityAttribute()
			: this(null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="attrType">Attribute type</param>
		public SecurityAttribute(ITypeDefOrRef attrType)
			: this(attrType, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="attrType">Attribute type</param>
		/// <param name="namedArguments">Named arguments that will be owned by this instance</param>
		public SecurityAttribute(ITypeDefOrRef attrType, IList<CANamedArgument> namedArguments) {
			this.attrType = attrType;
			this.namedArguments = ThreadSafeListCreator.MakeThreadSafe(namedArguments ?? new List<CANamedArgument>());
		}

		/// <inheritdoc/>
		public override string ToString() {
			return TypeFullName;
		}
	}
}
