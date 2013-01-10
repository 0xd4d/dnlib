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

﻿using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// A custom attribute
	/// </summary>
	public sealed class CustomAttribute {
		ICustomAttributeType ctor;
		byte[] rawData;
		List<CAArgument> arguments;
		List<CANamedArgument> namedArguments;

		/// <summary>
		/// Gets/sets the custom attribute constructor
		/// </summary>
		public ICustomAttributeType Constructor {
			get { return ctor; }
			set { ctor = value; }
		}

		/// <summary>
		/// Gets the attribute type
		/// </summary>
		public ITypeDefOrRef AttributeType {
			get {
				var methodRef = ctor as IMethod;
				return methodRef == null ? null : methodRef.DeclaringType;
			}
		}

		/// <summary>
		/// Gets the full name of the attribute type
		/// </summary>
		public string TypeFullName {
			get {
				var mrCtor = ctor as MemberRef;
				if (mrCtor != null)
					return mrCtor.GetDeclaringTypeFullName() ?? string.Empty;

				var mdCtor = ctor as MethodDef;
				if (mdCtor != null && mdCtor.DeclaringType != null)
					return mdCtor.DeclaringType.FullName;

				return string.Empty;
			}
		}

		/// <summary>
		/// <c>true</c> if the raw custom attribute blob hasn't been parsed
		/// </summary>
		public bool IsRawBlob {
			get { return rawData != null; }
		}

		/// <summary>
		/// Gets the raw custom attribute blob or <c>null</c>
		/// </summary>
		public byte[] RawData {
			get { return rawData; }
		}

		/// <summary>
		/// Gets all constructor arguments
		/// </summary>
		public List<CAArgument> ConstructorArguments {
			get { return arguments; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ConstructorArguments"/> is not empty
		/// </summary>
		public bool HasConstructorArguments {
			get { return arguments.Count > 0; }
		}

		/// <summary>
		/// Gets all named arguments (field and property values)
		/// </summary>
		public List<CANamedArgument> NamedArguments {
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
				foreach (var namedArg in namedArguments) {
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
				foreach (var namedArg in namedArguments) {
					if (namedArg.IsProperty)
						yield return namedArg;
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="rawData">Raw custom attribute blob</param>
		public CustomAttribute(ICustomAttributeType ctor, byte[] rawData)
			: this(ctor, null, null) {
			this.rawData = rawData;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		public CustomAttribute(ICustomAttributeType ctor)
			: this(ctor, null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments or <c>null</c> if none</param>
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CAArgument> arguments)
			: this(ctor, arguments, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="namedArguments">Named arguments or <c>null</c> if none</param>
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CANamedArgument> namedArguments)
			: this(ctor, null, namedArguments) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments or <c>null</c> if none</param>
		/// <param name="namedArguments">Named arguments or <c>null</c> if none</param>
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CAArgument> arguments, IEnumerable<CANamedArgument> namedArguments) {
			this.ctor = ctor;
			this.arguments = arguments == null ? new List<CAArgument>() : new List<CAArgument>(arguments);
			this.namedArguments = namedArguments == null ? new List<CANamedArgument>() : new List<CANamedArgument>(namedArguments);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments. The list is now owned by this instance.</param>
		/// <param name="namedArguments">Named arguments. The list is now owned by this instance.</param>
		internal CustomAttribute(ICustomAttributeType ctor, List<CAArgument> arguments, List<CANamedArgument> namedArguments) {
			this.ctor = ctor;
			this.arguments = arguments == null ? new List<CAArgument>() : arguments;
			this.namedArguments = namedArguments == null ? new List<CANamedArgument>() : namedArguments;
		}

		/// <summary>
		/// Gets the field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetField(string name) {
			return GetNamedArgument(name, true);
		}

		/// <summary>
		/// Gets the field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetField(UTF8String name) {
			return GetNamedArgument(name, true);
		}

		/// <summary>
		/// Gets the property named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetProperty(string name) {
			return GetNamedArgument(name, false);
		}

		/// <summary>
		/// Gets the property named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetProperty(UTF8String name) {
			return GetNamedArgument(name, false);
		}

		/// <summary>
		/// Gets the property/field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property/field</param>
		/// <param name="isField"><c>true</c> if it's a field, <c>false</c> if it's a property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetNamedArgument(string name, bool isField) {
			foreach (var namedArg in namedArguments) {
				if (namedArg.IsField == isField && UTF8String.ToSystemStringOrEmpty(namedArg.Name) == name)
					return namedArg;
			}
			return null;
		}

		/// <summary>
		/// Gets the property/field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property/field</param>
		/// <param name="isField"><c>true</c> if it's a field, <c>false</c> if it's a property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetNamedArgument(UTF8String name, bool isField) {
			foreach (var namedArg in namedArguments) {
				if (namedArg.IsField == isField && UTF8String.Equals(namedArg.Name, name))
					return namedArg;
			}
			return null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return TypeFullName;
		}
	}

	/// <summary>
	/// A custom attribute constructor argument
	/// </summary>
	public struct CAArgument {
		TypeSig type;
		object value;

		/// <summary>
		/// Gets/sets the argument type
		/// </summary>
		public TypeSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		/// Gets/sets the argument value
		/// </summary>
		public object Value {
			get { return value; }
			set { this.value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Argument type</param>
		public CAArgument(TypeSig type) {
			this.type = type;
			this.value = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Argument type</param>
		/// <param name="value">Argument value</param>
		public CAArgument(TypeSig type, object value) {
			this.type = type;
			this.value = value;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} ({1})", value, type);
		}
	}

	/// <summary>
	/// A custom attribute field/property argument
	/// </summary>
	public sealed class CANamedArgument {
		bool isField;
		TypeSig type;
		UTF8String name;
		CAArgument argument;

		/// <summary>
		/// <c>true</c> if it's a field
		/// </summary>
		public bool IsField {
			get { return isField; }
			set { isField = value; }
		}

		/// <summary>
		/// <c>true</c> if it's a property
		/// </summary>
		public bool IsProperty {
			get { return !isField; }
			set { isField = !value; }
		}

		/// <summary>
		/// Gets/sets the field/property type
		/// </summary>
		public TypeSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		/// Gets/sets the property/field name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets/sets the argument
		/// </summary>
		public CAArgument Argument {
			get { return argument; }
			set { argument = value; }
		}

		/// <summary>
		/// Gets/sets the argument type
		/// </summary>
		public TypeSig ArgumentType {
			get { return argument.Type; }
			set { argument.Type = value; }
		}

		/// <summary>
		/// Gets/sets the argument value
		/// </summary>
		public object Value {
			get { return argument.Value; }
			set { argument.Value = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public CANamedArgument() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isField"><c>true</c> if field, <c>false</c> if property</param>
		public CANamedArgument(bool isField) {
			this.isField = isField;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isField"><c>true</c> if field, <c>false</c> if property</param>
		/// <param name="type">Field/property type</param>
		public CANamedArgument(bool isField, TypeSig type) {
			this.isField = isField;
			this.type = type;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isField"><c>true</c> if field, <c>false</c> if property</param>
		/// <param name="type">Field/property type</param>
		/// <param name="name">Name of field/property</param>
		public CANamedArgument(bool isField, TypeSig type, UTF8String name) {
			this.isField = isField;
			this.type = type;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isField"><c>true</c> if field, <c>false</c> if property</param>
		/// <param name="type">Field/property type</param>
		/// <param name="name">Name of field/property</param>
		/// <param name="argument">Field/property argument</param>
		public CANamedArgument(bool isField, TypeSig type, UTF8String name, CAArgument argument) {
			this.isField = isField;
			this.type = type;
			this.name = name;
			this.argument = argument;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("({0}) {1} {2} = {3} ({4})", isField ? "field" : "property", type, name, Value, ArgumentType);
		}
	}
}
