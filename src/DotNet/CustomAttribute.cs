// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// A custom attribute
	/// </summary>
	public sealed class CustomAttribute : ICustomAttribute {
		ICustomAttributeType ctor;
		byte[] rawData;
		readonly IList<CAArgument> arguments;
		readonly IList<CANamedArgument> namedArguments;
		uint caBlobOffset;

		/// <summary>
		/// Gets/sets the custom attribute constructor
		/// </summary>
		public ICustomAttributeType Constructor {
			get => ctor;
			set => ctor = value;
		}

		/// <summary>
		/// Gets the attribute type
		/// </summary>
		public ITypeDefOrRef AttributeType => ctor?.DeclaringType;

		/// <summary>
		/// Gets the full name of the attribute type
		/// </summary>
		public string TypeFullName {
			get {
				if (ctor is MemberRef mrCtor)
					return mrCtor.GetDeclaringTypeFullName() ?? string.Empty;

				if (ctor is MethodDef mdCtor) {
					var declType = mdCtor.DeclaringType;
					if (!(declType is null))
						return declType.FullName;
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// <c>true</c> if the raw custom attribute blob hasn't been parsed
		/// </summary>
		public bool IsRawBlob => !(rawData is null);

		/// <summary>
		/// Gets the raw custom attribute blob or <c>null</c> if the CA was successfully parsed.
		/// </summary>
		public byte[] RawData => rawData;

		/// <summary>
		/// Gets all constructor arguments
		/// </summary>
		public IList<CAArgument> ConstructorArguments => arguments;

		/// <summary>
		/// <c>true</c> if <see cref="ConstructorArguments"/> is not empty
		/// </summary>
		public bool HasConstructorArguments => arguments.Count > 0;

		/// <summary>
		/// Gets all named arguments (field and property values)
		/// </summary>
		public IList<CANamedArgument> NamedArguments => namedArguments;

		/// <summary>
		/// <c>true</c> if <see cref="NamedArguments"/> is not empty
		/// </summary>
		public bool HasNamedArguments => namedArguments.Count > 0;

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are field arguments
		/// </summary>
		public IEnumerable<CANamedArgument> Fields {
			get {
				var namedArguments = this.namedArguments;
				int count = namedArguments.Count;
				for (int i = 0; i < count; i++) {
					var namedArg = namedArguments[i];
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
				var namedArguments = this.namedArguments;
				int count = namedArguments.Count;
				for (int i = 0; i < count; i++) {
					var namedArg = namedArguments[i];
					if (namedArg.IsProperty)
						yield return namedArg;
				}
			}
		}

		/// <summary>
		/// Gets the #Blob offset or 0 if unknown
		/// </summary>
		public uint BlobOffset => caBlobOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="rawData">Raw custom attribute blob</param>
		public CustomAttribute(ICustomAttributeType ctor, byte[] rawData)
			: this(ctor, null, null, 0) => this.rawData = rawData;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		public CustomAttribute(ICustomAttributeType ctor)
			: this(ctor, null, null, 0) {
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
			: this(ctor, arguments, namedArguments, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments or <c>null</c> if none</param>
		/// <param name="namedArguments">Named arguments or <c>null</c> if none</param>
		/// <param name="caBlobOffset">Original custom attribute #Blob offset or 0</param>
		public CustomAttribute(ICustomAttributeType ctor, IEnumerable<CAArgument> arguments, IEnumerable<CANamedArgument> namedArguments, uint caBlobOffset) {
			this.ctor = ctor;
			this.arguments = arguments is null ? new List<CAArgument>() : new List<CAArgument>(arguments);
			this.namedArguments = namedArguments is null ? new List<CANamedArgument>() : new List<CANamedArgument>(namedArguments);
			this.caBlobOffset = caBlobOffset;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ctor">Custom attribute constructor</param>
		/// <param name="arguments">Constructor arguments. The list is now owned by this instance.</param>
		/// <param name="namedArguments">Named arguments. The list is now owned by this instance.</param>
		/// <param name="caBlobOffset">Original custom attribute #Blob offset or 0</param>
		internal CustomAttribute(ICustomAttributeType ctor, List<CAArgument> arguments, List<CANamedArgument> namedArguments, uint caBlobOffset) {
			this.ctor = ctor;
			this.arguments = arguments ?? new List<CAArgument>();
			this.namedArguments = namedArguments ?? new List<CANamedArgument>();
			this.caBlobOffset = caBlobOffset;
		}

		/// <summary>
		/// Gets the field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetField(string name) => GetNamedArgument(name, true);

		/// <summary>
		/// Gets the field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetField(UTF8String name) => GetNamedArgument(name, true);

		/// <summary>
		/// Gets the property named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetProperty(string name) => GetNamedArgument(name, false);

		/// <summary>
		/// Gets the property named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetProperty(UTF8String name) => GetNamedArgument(name, false);

		/// <summary>
		/// Gets the property/field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Name of property/field</param>
		/// <param name="isField"><c>true</c> if it's a field, <c>false</c> if it's a property</param>
		/// <returns>A <see cref="CANamedArgument"/> instance or <c>null</c> if not found</returns>
		public CANamedArgument GetNamedArgument(string name, bool isField) {
			var namedArguments = this.namedArguments;
			int count = namedArguments.Count;
			for (int i = 0; i < count; i++) {
				var namedArg = namedArguments[i];
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
			var namedArguments = this.namedArguments;
			int count = namedArguments.Count;
			for (int i = 0; i < count; i++) {
				var namedArg = namedArguments[i];
				if (namedArg.IsField == isField && UTF8String.Equals(namedArg.Name, name))
					return namedArg;
			}
			return null;
		}

		/// <inheritdoc/>
		public override string ToString() => TypeFullName;
	}

	/// <summary>
	/// A custom attribute constructor argument
	/// </summary>
	public struct CAArgument : ICloneable {
		TypeSig type;
		object value;

		/// <summary>
		/// Gets/sets the argument type
		/// </summary>
		public TypeSig Type {
			readonly get => type;
			set => type = value;
		}

		/// <summary>
		/// Gets/sets the argument value
		/// </summary>
		public object Value {
			readonly get => value;
			set => this.value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Argument type</param>
		public CAArgument(TypeSig type) {
			this.type = type;
			value = null;
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

		readonly object ICloneable.Clone() => Clone();

		/// <summary>
		/// Clones this instance and any <see cref="CAArgument"/>s and <see cref="CANamedArgument"/>s
		/// referenced from this instance.
		/// </summary>
		/// <returns></returns>
		public readonly CAArgument Clone() {
			var value = this.value;
			if (value is CAArgument)
				value = ((CAArgument)value).Clone();
			else if (value is IList<CAArgument> args) {
				var newArgs = new List<CAArgument>(args.Count);
				int count = args.Count;
				for (int i = 0; i < count; i++) {
					var arg = args[i];
					newArgs.Add(arg.Clone());
				}
				value = newArgs;
			}
			return new CAArgument(type, value);
		}

		/// <inheritdoc/>
		public override readonly string ToString() => $"{value ?? "null"} ({type})";
	}

	/// <summary>
	/// A custom attribute field/property argument
	/// </summary>
	public sealed class CANamedArgument : ICloneable {
		bool isField;
		TypeSig type;
		UTF8String name;
		CAArgument argument;

		/// <summary>
		/// <c>true</c> if it's a field
		/// </summary>
		public bool IsField {
			get => isField;
			set => isField = value;
		}

		/// <summary>
		/// <c>true</c> if it's a property
		/// </summary>
		public bool IsProperty {
			get => !isField;
			set => isField = !value;
		}

		/// <summary>
		/// Gets/sets the field/property type
		/// </summary>
		public TypeSig Type {
			get => type;
			set => type = value;
		}

		/// <summary>
		/// Gets/sets the property/field name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Gets/sets the argument
		/// </summary>
		public CAArgument Argument {
			get => argument;
			set => argument = value;
		}

		/// <summary>
		/// Gets/sets the argument type
		/// </summary>
		public TypeSig ArgumentType {
			get => argument.Type;
			set => argument.Type = value;
		}

		/// <summary>
		/// Gets/sets the argument value
		/// </summary>
		public object Value {
			get => argument.Value;
			set => argument.Value = value;
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
		public CANamedArgument(bool isField) => this.isField = isField;

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

		object ICloneable.Clone() => Clone();

		/// <summary>
		/// Clones this instance and any <see cref="CAArgument"/>s referenced from this instance.
		/// </summary>
		/// <returns></returns>
		public CANamedArgument Clone() => new CANamedArgument(isField, type, name, argument.Clone());

		/// <inheritdoc/>
		public override string ToString() => $"({(isField ? "field" : "property")}) {type} {name} = {Value ?? "null"} ({ArgumentType})";
	}
}
