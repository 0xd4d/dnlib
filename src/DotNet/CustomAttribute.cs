// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Threading;
using dnlib.IO;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A custom attribute
	/// </summary>
	public sealed class CustomAttribute : ICustomAttribute {
		ICustomAttributeType ctor;
		byte[] rawData;
		readonly ThreadSafe.IList<CAArgument> arguments;
		readonly ThreadSafe.IList<CANamedArgument> namedArguments;
		readonly IBinaryReader blobReader;

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
				var cat = ctor;
				return cat == null ? null : cat.DeclaringType;
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
				if (mdCtor != null) {
					var declType = mdCtor.DeclaringType;
					if (declType != null)
						return declType.FullName;
				}

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
		/// Gets the raw custom attribute blob or <c>null</c> if the CA was successfully parsed.
		/// </summary>
		public byte[] RawData {
			get { return rawData; }
		}

		/// <summary>
		/// Gets all constructor arguments
		/// </summary>
		public ThreadSafe.IList<CAArgument> ConstructorArguments {
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
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="rawData">Raw custom attribute blob</param>
		public CustomAttribute(ICustomAttributeType ctor, byte[] rawData)
			: this(ctor, null, null, null) {
			this.rawData = rawData;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		public CustomAttribute(ICustomAttributeType ctor)
			: this(ctor, null, null, null) {
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
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CAArgument> arguments, IEnumerable<CANamedArgument> namedArguments)
			: this(ctor, arguments, namedArguments, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments or <c>null</c> if none</param>
		/// <param name="namedArguments">Named arguments or <c>null</c> if none</param>
		/// <param name="blobReader">A reader that returns the original custom attribute blob data</param>
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CAArgument> arguments, IEnumerable<CANamedArgument> namedArguments, IBinaryReader blobReader) {
			this.ctor = ctor;
			this.arguments = arguments == null ? ThreadSafeListCreator.Create<CAArgument>() : ThreadSafeListCreator.Create<CAArgument>(arguments);
			this.namedArguments = namedArguments == null ? ThreadSafeListCreator.Create<CANamedArgument>() : ThreadSafeListCreator.Create<CANamedArgument>(namedArguments);
			this.blobReader = blobReader;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments. The list is now owned by this instance.</param>
		/// <param name="namedArguments">Named arguments. The list is now owned by this instance.</param>
		/// <param name="blobReader">A reader that returns the original custom attribute blob data</param>
		internal CustomAttribute(ICustomAttributeType ctor, List<CAArgument> arguments, List<CANamedArgument> namedArguments, IBinaryReader blobReader) {
			this.ctor = ctor;
			this.arguments = arguments == null ? ThreadSafeListCreator.Create<CAArgument>() : ThreadSafeListCreator.MakeThreadSafe(arguments);
			this.namedArguments = namedArguments == null ? ThreadSafeListCreator.Create<CANamedArgument>() : ThreadSafeListCreator.MakeThreadSafe(namedArguments);
			this.blobReader = blobReader;
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
			foreach (var namedArg in namedArguments.GetSafeEnumerable()) {
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
			foreach (var namedArg in namedArguments.GetSafeEnumerable()) {
				if (namedArg.IsField == isField && UTF8String.Equals(namedArg.Name, name))
					return namedArg;
			}
			return null;
		}

		/// <summary>
		/// Gets the binary custom attribute data that was used to create this instance.
		/// </summary>
		/// <returns>Blob of this custom attribute</returns>
		public byte[] GetBlob() {
			if (rawData != null)
				return rawData;
			if (blobReader != null) {
#if THREAD_SAFE
				lock (this)
#endif
					return blobReader.ReadAllBytes();
			}
			return new byte[0];
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
			object v = value;
			return string.Format("{0} ({1})", v == null ? "null" : v, type);
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
			object v = Value;
			return string.Format("({0}) {1} {2} = {3} ({4})", isField ? "field" : "property", type, name, v == null ? "null" : v, ArgumentType);
		}
	}
}
