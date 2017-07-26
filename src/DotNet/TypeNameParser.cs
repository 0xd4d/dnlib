// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace dnlib.DotNet {
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected TypeNameParserException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Helps <see cref="TypeNameParser"/> create types
	/// </summary>
	public interface IAssemblyRefFinder {
		/// <summary>
		/// Finds a <see cref="TypeRef"/>'s <see cref="AssemblyRef"/> when the original assembly
		/// info is missing from the full type name.
		/// </summary>
		/// <param name="nonNestedTypeRef">A non-nested <see cref="TypeRef"/></param>
		/// <returns><paramref name="nonNestedTypeRef"/>'s <see cref="AssemblyRef"/> or <c>null</c></returns>
		AssemblyRef FindAssemblyRef(TypeRef nonNestedTypeRef);
	}

	/// <summary>
	/// Parses a type name and creates an <see cref="IType"/>
	/// </summary>
	public abstract class TypeNameParser : IDisposable {
		/// <summary>Owner module</summary>
		protected ModuleDef ownerModule;
		readonly GenericParamContext gpContext;
		StringReader reader;
		readonly IAssemblyRefFinder typeNameParserHelper;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="ITypeDefOrRef"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="ITypeDefOrRef"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		public static ITypeDefOrRef ParseReflectionThrow(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper) {
			return ParseReflectionThrow(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext());
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="ITypeDefOrRef"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="ITypeDefOrRef"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		public static ITypeDefOrRef ParseReflectionThrow(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext) {
			using (var parser = new ReflectionTypeNameParser(ownerModule, typeFullName, typeNameParserHelper, gpContext))
				return parser.Parse();
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="ITypeDefOrRef"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="ITypeDefOrRef"/> instance or <c>null</c> if parsing failed</returns>
		public static ITypeDefOrRef ParseReflection(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper) {
			return ParseReflection(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext());
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="ITypeDefOrRef"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="ITypeDefOrRef"/> instance or <c>null</c> if parsing failed</returns>
		public static ITypeDefOrRef ParseReflection(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext) {
			try {
				return ParseReflectionThrow(ownerModule, typeFullName, typeNameParserHelper, gpContext);
			}
			catch (TypeNameParserException) {
				return null;
			}
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="TypeSig"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="TypeSig"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		public static TypeSig ParseAsTypeSigReflectionThrow(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper) {
			return ParseAsTypeSigReflectionThrow(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext());
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="TypeSig"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		public static TypeSig ParseAsTypeSigReflectionThrow(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext) {
			using (var parser = new ReflectionTypeNameParser(ownerModule, typeFullName, typeNameParserHelper, gpContext))
				return parser.ParseAsTypeSig();
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="TypeSig"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if parsing failed</returns>
		public static TypeSig ParseAsTypeSigReflection(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper) {
			return ParseAsTypeSigReflection(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext());
		}

		/// <summary>
		/// Parses a Reflection type name and creates a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="TypeSig"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if parsing failed</returns>
		public static TypeSig ParseAsTypeSigReflection(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext) {
			try {
				return ParseAsTypeSigReflectionThrow(ownerModule, typeFullName, typeNameParserHelper, gpContext);
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
		protected TypeNameParser(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper)
			: this(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		protected TypeNameParser(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext) {
			this.ownerModule = ownerModule;
			this.reader = new StringReader(typeFullName ?? string.Empty);
			this.typeNameParserHelper = typeNameParserHelper;
			this.gpContext = gpContext;
		}

		/// <summary>
		/// Parses a type name and creates a <see cref="IType"/>
		/// </summary>
		/// <returns>A new <see cref="IType"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		internal ITypeDefOrRef Parse() {
			return ownerModule.UpdateRowId(ParseAsTypeSig().ToTypeDefOrRef());
		}

		/// <summary>
		/// Parses a type name and creates a <see cref="TypeSig"/>
		/// </summary>
		/// <returns>A new <see cref="TypeSig"/> instance</returns>
		/// <exception cref="TypeNameParserException">If parsing failed</exception>
		internal abstract TypeSig ParseAsTypeSig();

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

		internal sealed class SZArraySpec : TSpec {
			public static readonly SZArraySpec Instance = new SZArraySpec();
			SZArraySpec()
				: base(ElementType.SZArray) {
			}
		}

		internal sealed class ArraySpec : TSpec {
			public uint rank;
			public readonly IList<uint> sizes = new List<uint>();
			public readonly IList<int> lowerBounds = new List<int>();

			public ArraySpec()
				: base(ElementType.Array) {
			}
		}

		internal sealed class GenericInstSpec : TSpec {
			public readonly List<TypeSig> args = new List<TypeSig>();

			public GenericInstSpec()
				: base(ElementType.GenericInst) {
			}
		}

		internal sealed class ByRefSpec : TSpec {
			public static readonly ByRefSpec Instance = new ByRefSpec();
			ByRefSpec()
				: base(ElementType.ByRef) {
			}
		}

		internal sealed class PtrSpec : TSpec {
			public static readonly PtrSpec Instance = new PtrSpec();
			PtrSpec()
				: base(ElementType.Ptr) {
			}
		}

		internal GenericSig ReadGenericSig() {
			Verify(ReadChar() == '!', "Expected '!'");
			if (PeekChar() == '!') {
				ReadChar();
				return new GenericMVar(ReadUInt32(), gpContext.Method);
			}
			return new GenericVar(ReadUInt32(), gpContext.Type);
		}

		internal TypeSig CreateTypeSig(IList<TSpec> tspecs, TypeSig currentSig) {
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
		/// Reads a <see cref="TypeRef"/> including any possible nested <see cref="TypeRef"/>s.
		/// </summary>
		/// <param name="nestedChar">Character separating nested types</param>
		/// <returns>A new <see cref="TypeRef"/> instance, which could be nested.</returns>
		protected TypeRef ReadTypeRefAndNestedNoAssembly(char nestedChar) {
			var typeRef = ReadTypeRefNoAssembly();
			while (true) {
				SkipWhite();
				if (PeekChar() != nestedChar)
					break;
				ReadChar();
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
			// White space is important here. Any white space before the comma/EOF must be
			// parsed as part of the name.
			GetNamespaceAndName(ReadId(false), out ns, out name);
			return ownerModule.UpdateRowId(new TypeRefUser(ownerModule, ns, name));
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

		internal TypeSig ToTypeSig(ITypeDefOrRef type) {
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
			var ownerAsm = ownerModule.Assembly;
			if (ownerAsm != null)
				return ownerModule.UpdateRowId(ownerAsm.ToAssemblyRef());
			return AssemblyRef.CurrentAssembly;
		}

		internal bool IsValueType(TypeRef typeRef) {
			return typeRef != null && typeRef.IsValueType;
		}

		internal static void Verify(bool b, string msg) {
			if (!b)
				throw new TypeNameParserException(msg);
		}

		internal void SkipWhite() {
			while (true) {
				int next = PeekChar();
				if (next == -1)
					break;
				if (!char.IsWhiteSpace((char)next))
					break;
				ReadChar();
			}
		}

		internal uint ReadUInt32() {
			SkipWhite();
			bool readInt = false;
			uint val = 0;
			while (true) {
				int c = PeekChar();
				if (c == -1 || !(c >= '0' && c <= '9'))
					break;
				ReadChar();
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
			if (PeekChar() == '-') {
				isSigned = true;
				ReadChar();
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
			return ReadId(true);
		}

		internal string ReadId(bool ignoreWhiteSpace) {
			SkipWhite();
			var sb = new StringBuilder();
			int c;
			while ((c = GetIdChar(ignoreWhiteSpace)) != -1)
				sb.Append((char)c);
			Verify(sb.Length > 0, "Expected an id");
			return sb.ToString();
		}

		/// <summary>
		/// Peeks the next char. -1 if no more chars.
		/// </summary>
		protected int PeekChar() {
			return reader.Peek();
		}

		/// <summary>
		/// Gets the next char or -1 if no more chars
		/// </summary>
		protected int ReadChar() {
			return reader.Read();
		}

		/// <summary>
		/// Gets the next ID char or <c>-1</c> if no more ID chars
		/// </summary>
		/// <param name="ignoreWhiteSpace"><c>true</c> if white space should be ignored</param>
		internal abstract int GetIdChar(bool ignoreWhiteSpace);
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
		public ReflectionTypeNameParser(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper)
			: base(ownerModule, typeFullName, typeNameParserHelper, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Module that will own the returned <see cref="IType"/> or <c>null</c></param>
		/// <param name="typeFullName">Full name of type</param>
		/// <param name="typeNameParserHelper">Helper class</param>
		/// <param name="gpContext">Generic parameter context</param>
		public ReflectionTypeNameParser(ModuleDef ownerModule, string typeFullName, IAssemblyRefFinder typeNameParserHelper, GenericParamContext gpContext)
			: base(ownerModule, typeFullName, typeNameParserHelper, gpContext) {
		}

		/// <summary>
		/// Parses an assembly name
		/// </summary>
		/// <param name="asmFullName">Full assembly name</param>
		/// <returns>A new <see cref="AssemblyRef"/> instance or <c>null</c> if parsing failed</returns>
		public static AssemblyRef ParseAssemblyRef(string asmFullName) {
			return ParseAssemblyRef(asmFullName, new GenericParamContext());
		}

		/// <summary>
		/// Parses an assembly name
		/// </summary>
		/// <param name="asmFullName">Full assembly name</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="AssemblyRef"/> instance or <c>null</c> if parsing failed</returns>
		public static AssemblyRef ParseAssemblyRef(string asmFullName, GenericParamContext gpContext) {
			try {
				using (var parser = new ReflectionTypeNameParser(null, asmFullName, null, gpContext))
					return parser.ReadAssemblyRef();
			}
			catch {
				return null;
			}
		}

		/// <inheritdoc/>
		internal override TypeSig ParseAsTypeSig() {
			try {
				var type = ReadType(true);
				SkipWhite();
				Verify(PeekChar() == -1, "Extra input after type name");
				return type;
			}
			catch (TypeNameParserException) {
				throw;
			}
			catch (Exception ex) {
				throw new TypeNameParserException("Could not parse type name", ex);
			}
		}

		TypeSig ReadType(bool readAssemblyReference) {
			RecursionIncrement();
			TypeSig result;

			SkipWhite();
			if (PeekChar() == '!') {
				var currentSig = ReadGenericSig();
				var tspecs = ReadTSpecs();
				ReadOptionalAssemblyRef();
				result = CreateTypeSig(tspecs, currentSig);
			}
			else {
				TypeRef typeRef = ReadTypeRefAndNestedNoAssembly('+');
				var tspecs = ReadTSpecs();
				var nonNestedTypeRef = TypeRef.GetNonNestedTypeRef(typeRef);
				AssemblyRef asmRef;
				if (readAssemblyReference)
					asmRef = ReadOptionalAssemblyRef() ?? FindAssemblyRef(nonNestedTypeRef);
				else
					asmRef = FindAssemblyRef(nonNestedTypeRef);
				nonNestedTypeRef.ResolutionScope = asmRef;

				// Make sure the CorLib types are used whenever possible
				result = null;
				if (typeRef == nonNestedTypeRef) {
					var corLibSig = ownerModule.CorLibTypes.GetCorLibTypeSig(typeRef.Namespace, typeRef.Name, typeRef.DefinitionAssembly);
					if (corLibSig != null)
						result = corLibSig;
				}
				if (result == null) {
					var typeDef = Resolve(asmRef, typeRef);
					result = ToTypeSig(typeDef != null ? (ITypeDefOrRef)typeDef : typeRef);
				}

				if (tspecs.Count != 0)
					result = CreateTypeSig(tspecs, result);
			}

			RecursionDecrement();
			return result;
		}

		TypeDef Resolve(AssemblyRef asmRef, TypeRef typeRef) {
			var asm = ownerModule.Assembly;
			if (asm == null)
				return null;
			if (!(AssemblyNameComparer.CompareAll.Equals(asmRef, asm) && asmRef.IsRetargetable == asm.IsRetargetable))
				return null;
			var td = typeRef.Resolve();
			return td != null && td.Module == ownerModule ? td : null;
		}

		AssemblyRef ReadOptionalAssemblyRef() {
			SkipWhite();
			if (PeekChar() == ',') {
				ReadChar();
				return ReadAssemblyRef();
			}
			return null;
		}

		IList<TSpec> ReadTSpecs() {
			var tspecs = new List<TSpec>();
			while (true) {
				SkipWhite();
				switch (PeekChar()) {
				case '[':	// SZArray, Array, or GenericInst
					ReadChar();
					SkipWhite();
					var peeked = PeekChar();
					if (peeked == ']') {
						// SZ array
						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(SZArraySpec.Instance);
					}
					else if (peeked == '*' || peeked == ',' || peeked == '-' || char.IsDigit((char)peeked)) {
						// Array

						var arraySpec = new ArraySpec();
						arraySpec.rank = 0;
						while (true) {
							SkipWhite();
							int c = PeekChar();
							if (c == '*')
								ReadChar();
							else if (c == ',' || c == ']') {
							}
							else if (c == '-' || char.IsDigit((char)c)) {
								int lower = ReadInt32();
								uint? size;
								SkipWhite();
								Verify(ReadChar() == '.', "Expected '.'");
								Verify(ReadChar() == '.', "Expected '.'");
								if (PeekChar() == '.') {
									ReadChar();
									size = null;
								}
								else {
									SkipWhite();
									if (PeekChar() == '-') {
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
							if (PeekChar() != ',')
								break;
							ReadChar();
						}

						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(arraySpec);
					}
					else {
						// Generic args

						var ginstSpec = new GenericInstSpec();
						while (true) {
							SkipWhite();
							peeked = PeekChar();
							bool needSeperators = peeked == '[';
							if (peeked == ']')
								break;
							Verify(!needSeperators || ReadChar() == '[', "Expected '['");
							ginstSpec.args.Add(ReadType(needSeperators));
							SkipWhite();
							Verify(!needSeperators || ReadChar() == ']', "Expected ']'");
							SkipWhite();
							if (PeekChar() != ',')
								break;
							ReadChar();
						}

						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(ginstSpec);
					}
					break;

				case '&':	// ByRef
					ReadChar();
					tspecs.Add(ByRefSpec.Instance);
					break;

				case '*':	// Ptr
					ReadChar();
					tspecs.Add(PtrSpec.Instance);
					break;

				default:
					return tspecs;
				}
			}
		}

		AssemblyRef ReadAssemblyRef() {
			var asmRef = new AssemblyRefUser();
			if (ownerModule != null)
				ownerModule.UpdateRowId(asmRef);

			asmRef.Name = ReadAssemblyNameId();
			SkipWhite();
			if (PeekChar() != ',')
				return asmRef;
			ReadChar();

			while (true) {
				SkipWhite();
				int c = PeekChar();
				if (c == -1 || c == ']')
					break;
				if (c == ',') {
					ReadChar();
					continue;
				}

				string key = ReadId();
				SkipWhite();
				if (PeekChar() != '=')
					continue;
				ReadChar();
				string value = ReadId();

				switch (key.ToUpperInvariant()) {
				case "VERSION":
					asmRef.Version = Utils.ParseVersion(value);
					break;

				case "CONTENTTYPE":
					if (value.Equals("WindowsRuntime", StringComparison.OrdinalIgnoreCase))
						asmRef.ContentType = AssemblyAttributes.ContentType_WindowsRuntime;
					else
						asmRef.ContentType = AssemblyAttributes.ContentType_Default;
					break;

				case "RETARGETABLE":
					if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase))
						asmRef.IsRetargetable = true;
					else
						asmRef.IsRetargetable = false;
					break;

				case "PUBLICKEY":
					if (value.Equals("null", StringComparison.OrdinalIgnoreCase) ||
						value.Equals("neutral", StringComparison.OrdinalIgnoreCase))
						asmRef.PublicKeyOrToken = new PublicKey();
					else
						asmRef.PublicKeyOrToken = PublicKeyBase.CreatePublicKey(Utils.ParseBytes(value));
					break;

				case "PUBLICKEYTOKEN":
					if (value.Equals("null", StringComparison.OrdinalIgnoreCase) ||
						value.Equals("neutral", StringComparison.OrdinalIgnoreCase))
						asmRef.PublicKeyOrToken = new PublicKeyToken();
					else
						asmRef.PublicKeyOrToken = PublicKeyBase.CreatePublicKeyToken(Utils.ParseBytes(value));
					break;

				case "CULTURE":
				case "LANGUAGE":
					if (value.Equals("neutral", StringComparison.OrdinalIgnoreCase))
						asmRef.Culture = UTF8String.Empty;
					else
						asmRef.Culture = value;
					break;
				}
			}

			return asmRef;
		}

		string ReadAssemblyNameId() {
			SkipWhite();
			var sb = new StringBuilder();
			int c;
			while ((c = GetAsmNameChar()) != -1)
				sb.Append((char)c);
			var name = sb.ToString().Trim();
			Verify(name.Length > 0, "Expected an assembly name");
			return name;
		}

		int GetAsmNameChar() {
			int c = PeekChar();
			if (c == -1)
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ']':
			case ',':
				return -1;

			default:
				return ReadChar();
			}
		}

		internal override int GetIdChar(bool ignoreWhiteSpace) {
			int c = PeekChar();
			if (c == -1)
				return -1;
			if (ignoreWhiteSpace && char.IsWhiteSpace((char)c))
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ',':
			case '+':
			case '&':
			case '*':
			case '[':
			case ']':
			case '=':
				return -1;

			default:
				return ReadChar();
			}
		}
	}
}
