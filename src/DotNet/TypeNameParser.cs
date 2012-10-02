using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Thrown by <see cref="TypeNameParser"/> when it fails to parse a type name
	/// </summary>
	[Serializable]
	public class TypeNameParserException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeNameParserException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public TypeNameParserException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public TypeNameParserException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Helps <see cref="TypeNameParser"/> create types
	/// </summary>
	public interface ITypeNameParserHelper {
		/// <summary>
		/// Finds a <see cref="TypeRef"/>'s <see cref="AssemblyRef"/> when the original assembly
		/// info is missing from the full type name.
		/// </summary>
		/// <param name="nonNestedTypeRef">A non-nested <see cref="TypeRef"/></param>
		/// <returns><paramref name="nonNestedTypeRef"/>'s <see cref="AssemblyRef"/> or <c>null</c></returns>
		AssemblyRef FindAssemblyRef(TypeRef nonNestedTypeRef);

		/// <summary>
		/// Checks whether <paramref name="typeRef"/> is a value type
		/// </summary>
		/// <param name="typeRef">The type</param>
		/// <returns><c>true</c> if <paramref name="typeRef"/> is a value type, <c>false</c> if
		/// <paramref name="typeRef"/> is a reference type.</returns>
		bool IsValueType(TypeRef typeRef);
	}

	/// <summary>
	/// Parses a type name and creates an <see cref="IType"/>
	/// </summary>
	public abstract class TypeNameParser : IDisposable {
		/// <summary>Owner module</summary>
		protected ModuleDef ownerModule;
		/// <summary>Text reader</summary>
		protected StringReader reader;
		ITypeNameParserHelper typeNameParserHelper;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="IType"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="IType"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		public static IType ParseReflection(ModuleDef ownerModule, string typeFullName, ITypeNameParserHelper typeNameParserHelper) {
			using (var parser = new ReflectionTypeNameParser(ownerModule, typeFullName, typeNameParserHelper))
				return parser.Parse();
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="IType"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="IType"/> instance or <c>null</c> if parsing failed</returns>
		public static IType ParseReflectionNoThrow(ModuleDef ownerModule, string typeFullName, ITypeNameParserHelper typeNameParserHelper) {
			try {
				return ParseReflection(ownerModule, typeFullName, typeNameParserHelper);
			}
			catch (TypeNameParserException) {
				return null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		protected TypeNameParser(ModuleDef ownerModule, string typeFullName, ITypeNameParserHelper typeNameParserHelper) {
			this.ownerModule = ownerModule;
			this.reader = new StringReader(typeFullName);
			this.typeNameParserHelper = typeNameParserHelper;
		}

		/// <summary>
		/// Parses a type name and creates a <see cref="IType"/>
		/// </summary>
		/// <returns>A new <see cref="IType"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		internal abstract IType Parse();

		/// <summary>
		/// Increment recursion counter
		/// </summary>
		/// <exception cref="TypeNameParserException">If this method has been called too many times</exception>
		protected void RecursionIncrement() {
			if (!recursionCounter.Increment())
				throw new TypeNameParserException("Stack overflow");
		}

		/// <summary>
		/// Decrement recursion counter
		/// </summary>
		protected void RecursionDecrement() {
			recursionCounter.Decrement();
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (reader != null)
				reader.Dispose();
			reader = null;
		}

		internal abstract class TSpec {
			public readonly ElementType etype;

			protected TSpec(ElementType etype) {
				this.etype = etype;
			}
		}

		internal class SZArraySpec : TSpec {
			public static readonly SZArraySpec Instance = new SZArraySpec();
			SZArraySpec()
				: base(ElementType.SZArray) {
			}
		}

		internal class ArraySpec : TSpec {
			public uint rank;
			public readonly IList<uint> sizes = new List<uint>();
			public readonly IList<int> lowerBounds = new List<int>();

			public ArraySpec()
				: base(ElementType.Array) {
			}
		}

		internal class GenericInstSpec : TSpec {
			public readonly List<TypeSig> args = new List<TypeSig>();

			public GenericInstSpec()
				: base(ElementType.GenericInst) {
			}
		}

		internal class ByRefSpec : TSpec {
			public static readonly ByRefSpec Instance = new ByRefSpec();
			ByRefSpec()
				: base(ElementType.ByRef) {
			}
		}

		internal class PtrSpec : TSpec {
			public static readonly PtrSpec Instance = new PtrSpec();
			PtrSpec()
				: base(ElementType.Ptr) {
			}
		}

		internal GenericSig ReadGenericSig() {
			Verify(reader.Read() == '!', "Expected '!'");
			if (reader.Peek() == '!') {
				reader.Read();
				return new GenericMVar(ReadUInt32());
			}
			return new GenericVar(ReadUInt32());
		}

		internal TypeSig CreateTypeSig(IList<TSpec> tspecs, TypeSig currentSig) {
			if (tspecs.Count == 0)
				return currentSig;
			foreach (var tspec in tspecs) {
				switch (tspec.etype) {
				case ElementType.SZArray:
					currentSig = new SZArraySig(currentSig);
					break;

				case ElementType.Array:
					var arraySpec = (ArraySpec)tspec;
					currentSig = new ArraySig(currentSig, arraySpec.rank, arraySpec.sizes, arraySpec.lowerBounds);
					break;

				case ElementType.GenericInst:
					var ginstSpec = (GenericInstSpec)tspec;
					currentSig = new GenericInstSig(currentSig as ClassOrValueTypeSig, ginstSpec.args);
					break;

				case ElementType.ByRef:
					currentSig = new ByRefSig(currentSig);
					break;

				case ElementType.Ptr:
					currentSig = new PtrSig(currentSig);
					break;

				default:
					Verify(false, "Unknown TSpec");
					break;
				}
			}
			return currentSig;
		}

		/// <summary>
		/// Gets the top-most (non-nested) <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">Input</param>
		/// <returns>The non-nested <see cref="TypeRef"/> or <c>null</c></returns>
		protected static TypeRef GetNonNestedTypeRef(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.ResolutionScope as TypeRef;
				if (next == null)
					return typeRef;
				typeRef = next;
			}
			return null;	// Should never happen
		}

		/// <summary>
		/// Reads a <see cref="TypeRef"/> including any possible nested <see cref="TypeRef"/>s.
		/// </summary>
		/// <param name="nestedChar">Character separating nested types</param>
		/// <returns>A new <see cref="TypeRef"/> instance, which could be nested.</returns>
		protected TypeRef ReadTypeRefAndNestedNoAssembly(char nestedChar) {
			var typeRef = ReadTypeRefNoAssembly();
			while (true) {
				SkipWhite();
				if (reader.Peek() != nestedChar)
					break;
				reader.Read();
				var newTypeRef = ReadTypeRefNoAssembly();
				newTypeRef.ResolutionScope = typeRef;
				typeRef = newTypeRef;
			}
			return typeRef;
		}

		/// <summary>
		/// Reads a namespace and name and creates a TypeRef. Does not read any nested types.
		/// </summary>
		/// <returns>A new <see cref="TypeRef"/> instance</returns>
		protected TypeRef ReadTypeRefNoAssembly() {
			string ns, name;
			GetNamespaceAndName(ReadId(), out ns, out name);
			return new TypeRefUser(ownerModule, ns, name);
		}

		static void GetNamespaceAndName(string fullName, out string ns, out string name) {
			int index = fullName.LastIndexOf('.');
			if (index < 0) {
				ns = string.Empty;
				name = fullName;
			}
			else {
				ns = fullName.Substring(0, index);
				name = fullName.Substring(index + 1);
			}
		}

		internal TypeSig ToTypeSig(IType type) {
			var tsig = type as TypeSig;
			if (tsig != null)
				return tsig;
			var td = type as TypeDef;
			if (td != null)
				return ToTypeSig(td, td.IsValueType);
			var tr = type as TypeRef;
			if (tr != null)
				return ToTypeSig(tr, IsValueType(tr));
			var ts = type as TypeSpec;
			if (ts != null)
				return ts.TypeSig;
			Verify(false, "Unknown type");
			return null;
		}

		static TypeSig ToTypeSig(ITypeDefOrRef type, bool isValueType) {
			return isValueType ? (TypeSig)new ValueTypeSig(type) : new ClassSig(type);
		}

		internal AssemblyRef FindAssemblyRef(TypeRef nonNestedTypeRef) {
			AssemblyRef asmRef = null;
			if (nonNestedTypeRef != null && typeNameParserHelper != null)
				asmRef = typeNameParserHelper.FindAssemblyRef(nonNestedTypeRef);
			if (asmRef != null)
				return asmRef;
			if (ownerModule != null && ownerModule.Assembly != null)
				return ownerModule.Assembly.ToAssemblyRef();
			return new AssemblyRefUser("<<<UNKNOWN>>>");
		}

		internal bool IsValueType(TypeRef typeRef) {
			if (typeRef == null)
				return false;
			if (typeNameParserHelper == null)
				return false;	// Assume it's a reference type
			return typeNameParserHelper.IsValueType(typeRef);
		}

		internal static void Verify(bool b, string msg) {
			if (!b)
				throw new TypeNameParserException(msg);
		}

		internal void SkipWhite() {
			while (true) {
				int next = reader.Peek();
				if (next == -1)
					break;
				if (!char.IsWhiteSpace((char)next))
					break;
				reader.Read();
			}
		}

		internal uint ReadUInt32() {
			SkipWhite();
			bool readInt = false;
			uint val = 0;
			while (true) {
				int c = reader.Peek();
				if (c == -1 || !(c >= '0' && c <= '9'))
					break;
				reader.Read();
				uint newVal = val * 10 + (uint)(c - '0');
				Verify(newVal >= val, "Integer overflow");
				val = newVal;
				readInt = true;
			}
			Verify(readInt, "Expected an integer");
			return val;
		}

		internal int ReadInt32() {
			SkipWhite();

			bool isSigned = false;
			if (reader.Peek() == '-') {
				isSigned = true;
				reader.Read();
			}

			uint val = ReadUInt32();
			if (isSigned) {
				Verify(val <= (uint)int.MaxValue + 1, "Integer overflow");
				return -(int)val;
			}
			else {
				Verify(val <= (uint)int.MaxValue, "Integer overflow");
				return (int)val;
			}
		}

		internal string ReadId() {
			SkipWhite();
			var sb = new StringBuilder();
			int c;
			while ((c = GetIdChar()) != -1)
				sb.Append((char)c);
			Verify(sb.Length > 0, "Expected an id");
			return sb.ToString();
		}

		/// <summary>
		/// Gets the next ID char or <c>-1</c> if no more ID chars
		/// </summary>
		internal abstract int GetIdChar();
	}

	/// <summary>
	/// Parses reflection type names. Grammar http://msdn.microsoft.com/en-us/library/yfsftwz6.aspx
	/// </summary>
	sealed class ReflectionTypeNameParser : TypeNameParser {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		public ReflectionTypeNameParser(ModuleDef ownerModule, string typeFullName, ITypeNameParserHelper typeNameParserHelper)
			: base(ownerModule, typeFullName, typeNameParserHelper) {
		}

		/// <inheritdoc/>
		internal override IType Parse() {
			try {
				var type = ReadType();
				SkipWhite();
				Verify(reader.Peek() == -1, "Extra input after type name");
				return type;
			}
			catch (TypeNameParserException) {
				throw;
			}
			catch (Exception ex) {
				throw new TypeNameParserException("Could not parse type name", ex);
			}
		}

		IType ReadType() {
			RecursionIncrement();
			IType result;

			SkipWhite();
			if (reader.Peek() == '!') {
				var currentSig = ReadGenericSig();
				var tspecs = ReadTSpecs();
				ReadOptionalAssemblyRef();
				result = CreateTypeSig(tspecs, currentSig);
			}
			else {
				TypeRef typeRef = ReadTypeRefAndNestedNoAssembly('+');
				var tspecs = ReadTSpecs();
				var nonNestedTypeRef = GetNonNestedTypeRef(typeRef);
				nonNestedTypeRef.ResolutionScope = ReadOptionalAssemblyRef() ?? FindAssemblyRef(nonNestedTypeRef);
				if (tspecs.Count == 0)
					result = typeRef;
				else
					result = CreateTypeSig(tspecs, ToTypeSig(typeRef));
			}

			RecursionDecrement();
			return result;
		}

		AssemblyRef ReadOptionalAssemblyRef() {
			SkipWhite();
			if (reader.Peek() == ',') {
				reader.Read();
				return ReadAssemblyRef();
			}
			return null;
		}

		IList<TSpec> ReadTSpecs() {
			var tspecs = new List<TSpec>();
			while (true) {
				SkipWhite();
				switch (reader.Peek()) {
				case '[':	// SZArray, Array, or GenericInst
					reader.Read();
					SkipWhite();
					if (reader.Peek() == '[') {
						// Generic args

						var ginstSpec = new GenericInstSpec();
						while (true) {
							SkipWhite();
							if (reader.Peek() != '[')
								break;
							reader.Read();
							ginstSpec.args.Add(ToTypeSig(ReadType()));
							SkipWhite();
							Verify(reader.Read() == ']', "Expected ']'");
							SkipWhite();
							if (reader.Peek() != ',')
								break;
							reader.Read();
						}

						Verify(reader.Read() == ']', "Expected ']'");
						tspecs.Add(ginstSpec);
					}
					else if (reader.Peek() == ']') {
						// SZ array
						Verify(reader.Read() == ']', "Expected ']'");
						tspecs.Add(SZArraySpec.Instance);
					}
					else {
						// Array

						var arraySpec = new ArraySpec();
						arraySpec.rank = 0;
						while (true) {
							SkipWhite();
							int c = reader.Peek();
							if (c == '*')
								reader.Read();
							else if (c == ',' || c == ']') {
							}
							else if (c == '-' || char.IsDigit((char)c)) {
								int lower = ReadInt32();
								uint? size;
								SkipWhite();
								Verify(reader.Read() == '.', "Expected '.'");
								Verify(reader.Read() == '.', "Expected '.'");
								if (reader.Peek() == '.') {
									reader.Read();
									size = null;
								}
								else {
									SkipWhite();
									if (reader.Peek() == '-') {
										int upper = ReadInt32();
										Verify(upper >= lower, "upper < lower");
										size = (uint)(upper - lower + 1);
										Verify(size.Value != 0 && size.Value <= 0x1FFFFFFF, "Invalid size");
									}
									else {
										uint upper = ReadUInt32();
										long lsize = (long)upper - (long)lower + 1;
										Verify(lsize > 0 && lsize <= 0x1FFFFFFF, "Invalid size");
										size = (uint)lsize;
									}
								}
								if (arraySpec.lowerBounds.Count == arraySpec.rank)
									arraySpec.lowerBounds.Add(lower);
								if (size.HasValue && arraySpec.sizes.Count == arraySpec.rank)
									arraySpec.sizes.Add(size.Value);
							}
							else
								Verify(false, "Unknown char");

							arraySpec.rank++;
							SkipWhite();
							if (reader.Peek() != ',')
								break;
							reader.Read();
						}

						Verify(reader.Read() == ']', "Expected ']'");
						tspecs.Add(arraySpec);
					}
					break;

				case '&':	// ByRef
					reader.Read();
					tspecs.Add(ByRefSpec.Instance);
					break;

				case '*':	// Ptr
					reader.Read();
					tspecs.Add(PtrSpec.Instance);
					break;

				default:
					goto done;
				}
			}
done:
			return tspecs;
		}

		AssemblyRef ReadAssemblyRef() {
			var asmRef = new AssemblyRefUser();

			asmRef.Name = new UTF8String(ReadId());
			SkipWhite();
			if (reader.Peek() != ',')
				return asmRef;
			reader.Read();

			while (true) {
				SkipWhite();
				int c = reader.Peek();
				if (c == -1 || c == ']')
					break;
				if (c == ',') {
					reader.Read();
					continue;
				}

				string key = ReadId();
				SkipWhite();
				if (reader.Peek() != '=')
					continue;
				reader.Read();
				string value = ReadId();

				switch (key.ToLowerInvariant()) {
				case "version":
					asmRef.Version = Utils.ParseVersion(value);
					break;

				case "publickey":
					if (value == "null")
						asmRef.PublicKeyOrToken = null;
					else
						asmRef.PublicKeyOrToken = PublicKeyBase.CreatePublicKey(Utils.ParseBytes(value));
					break;

				case "publickeytoken":
					if (value == "null")
						asmRef.PublicKeyOrToken = null;
					else
						asmRef.PublicKeyOrToken = PublicKeyBase.CreatePublicKeyToken(Utils.ParseBytes(value));
					break;

				case "culture":
					if (value.ToLowerInvariant() == "neutral")
						asmRef.Locale = UTF8String.Empty;
					else
						asmRef.Locale = new UTF8String(value);
					break;
				}
			}

			return asmRef;
		}

		internal override int GetIdChar() {
			int c = reader.Peek();
			if (c == -1 || char.IsWhiteSpace((char)c))
				return -1;
			switch (c) {
			case '\\':
				reader.Read();
				return reader.Read();

			case ',':
			case '+':
			case '&':
			case '*':
			case '[':
			case ']':
			case '=':
				return -1;

			default:
				return reader.Read();
			}
		}
	}
}
