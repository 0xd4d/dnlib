// dnlib: See LICENSE.txt for more info

// See coreclr/src/vm/siginfo.cpp

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace dnlib.DotNet {
	/// <summary>
	/// <c>System.Runtime.InteropServices.TypeIdentifierAttribute</c> helper code used by <see cref="SigComparer"/>
	/// </summary>
	static class TIAHelper {
		readonly struct Info : IEquatable<Info> {
			public readonly UTF8String Scope;
			public readonly UTF8String Identifier;

			public Info(UTF8String scope, UTF8String identifier) {
				Scope = scope;
				Identifier = identifier;
			}

			public bool Equals(Info other) => stricmp(Scope, other.Scope) && UTF8String.Equals(Identifier, other.Identifier);

			// The CLR compares lengths so null and empty strings are equal
			static bool stricmp(UTF8String a, UTF8String b) {
				var da = a?.Data ?? Array2.Empty<byte>();
				var db = b?.Data ?? Array2.Empty<byte>();
				if (da == db)
					return true;
				if (da.Length != db.Length)
					return false;
				for (int i = 0; i < da.Length; i++) {
					byte ba = da[i], bb = db[i];
					if ((byte)'A' <= ba && ba <= (byte)'Z')
						ba = (byte)(ba - 'A' + 'a');
					if ((byte)'A' <= bb && bb <= (byte)'Z')
						bb = (byte)(bb - 'A' + 'a');
					if (ba != bb)
						return false;
				}
				return true;
			}
		}

		static Info? GetInfo(TypeDef td) {
			if (td is null)
				return null;
			if (td.IsWindowsRuntime)
				return null;

			UTF8String scope = null, identifier = null;
			var tia = td.CustomAttributes.Find("System.Runtime.InteropServices.TypeIdentifierAttribute");
			if (tia is not null) {
				if (tia.ConstructorArguments.Count >= 2) {
					if (tia.ConstructorArguments[0].Type.GetElementType() != ElementType.String)
						return null;
					if (tia.ConstructorArguments[1].Type.GetElementType() != ElementType.String)
						return null;
					scope = tia.ConstructorArguments[0].Value as UTF8String ?? tia.ConstructorArguments[0].Value as string;
					identifier = tia.ConstructorArguments[1].Value as UTF8String ?? tia.ConstructorArguments[1].Value as string;
				}
			}
			else {
				if (!IsTypeLibAssembly(td.Module?.Assembly))
					return null;
			}

			if (UTF8String.IsNull(identifier)) {
				CustomAttribute gca;
				if (td.IsInterface && td.IsImport)
					gca = td.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
				else {
					var asm = td.Module?.Assembly;
					if (asm is null)
						return null;
					gca = asm.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
				}
				if (gca is null)
					return null;
				if (gca.ConstructorArguments.Count < 1)
					return null;
				if (gca.ConstructorArguments[0].Type.GetElementType() != ElementType.String)
					return null;
				scope = gca.ConstructorArguments[0].Value as UTF8String ?? gca.ConstructorArguments[0].Value as string;
				var ns = td.Namespace;
				var name = td.Name;
				if (UTF8String.IsNullOrEmpty(ns))
					identifier = name;
				else if (UTF8String.IsNullOrEmpty(name))
					identifier = new UTF8String(Concat(ns.Data, (byte)'.', Array2.Empty<byte>()));
				else
					identifier = new UTF8String(Concat(ns.Data, (byte)'.', name.Data));
			}
			return new Info(scope, identifier);
		}

		static byte[] Concat(byte[] a, byte b, byte[] c) {
			var data = new byte[a.Length + 1 + c.Length];
			for (int i = 0; i < a.Length; i++)
				data[i] = a[i];
			data[a.Length] = b;
			for (int i = 0, j = a.Length + 1; i < c.Length; i++, j++)
				data[j] = c[i];
			return data;
		}

		internal static bool IsTypeDefEquivalent(TypeDef td) => GetInfo(td) is not null && CheckEquivalent(td);

		// See coreclr's IsTypeDefEquivalent()
		static bool CheckEquivalent(TypeDef td) {
			Debug.Assert(td is not null);

			for (int i = 0; td is not null && i < 1000; i++) {
				if (td.IsWindowsRuntime)
					return false;

				// Types with a TypeIdentifierAttribute are always marked as equivalent
				if (td.CustomAttributes.IsDefined("System.Runtime.InteropServices.TypeIdentifierAttribute"))
					return true;

				// Else the assembly must be a type library
				if (!IsTypeLibAssembly(td.Module?.Assembly))
					return false;

				// It must be a COMImport/COMEvent interface, enum, struct, or delegate
				bool f;
				if (td.IsInterface)
					f = td.IsImport || td.CustomAttributes.IsDefined("System.Runtime.InteropServices.ComEventInterfaceAttribute");
				else
					f = td.IsValueType || ExtendsMulticastDelegate(td);
				if (!f)
					return false;

				// Not generic
				if (td.GenericParameters.Count > 0)
					return false;

				// Externally visible
				if (!IsExternallyVisible(td))
					return false;

				// If it's nested, the enclosing type must also be equivalent
				td = td.DeclaringType;
				if (td is null)
					return true;
			}

			return false;
		}

		static bool IsTypeLibAssembly(AssemblyDef asm) =>
			asm is not null &&
			(asm.CustomAttributes.IsDefined("System.Runtime.InteropServices.ImportedFromTypeLibAttribute") ||
			asm.CustomAttributes.IsDefined("System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute"));

		// See coreclr's IsTypeDefExternallyVisible()
		static bool IsExternallyVisible(TypeDef td) {
			for (int i = 0; td is not null && i < 1000; i++) {
				if (td.IsPublic)
					return true;
				if (!td.IsNestedPublic)
					return false;
				td = td.DeclaringType;
			}
			return false;
		}

		// The CLR only checks the base type (unlike TypeDef.IsDelegate it doesn't reject abstract types)
		static bool ExtendsMulticastDelegate(TypeDef td) {
			if (td.IsInterface)
				return false;
			var baseType = td.BaseType;
			return baseType is not null &&
				baseType.Name == "MulticastDelegate" &&
				baseType.Namespace == "System" &&
				baseType.DefinitionAssembly.IsCorLib();
		}

		/// <summary>
		/// Linked list of type pairs that are currently being compared for equivalence. Prevents
		/// infinite recursion, eg. a delegate that takes itself as a parameter.
		/// See coreclr's <c>TokenPairList</c>.
		/// </summary>
		internal sealed class TypeDefPairList {
			readonly TypeDef td1;
			readonly TypeDef td2;
			readonly TypeDefPairList next;

			public TypeDefPairList(TypeDef td1, TypeDef td2, TypeDefPairList next) {
				this.td1 = td1;
				this.td2 = td2;
				this.next = next;
			}

			public static bool Exists(TypeDefPairList list, TypeDef td1, TypeDef td2) {
				for (; list is not null; list = list.next) {
					if (list.td1 == td1 && list.td2 == td2)
						return true;
					if (list.td1 == td2 && list.td2 == td1)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Type pairs that have been proven equivalent during the outermost <see cref="Equivalent"/> call
		/// (the one that is called when no other pair is being compared).
		/// Prevents exponential time when the same types are compared many times. Only <c>true</c> results
		/// are cached: a result can depend on pairs that are still being compared (<see cref="TypeDefPairList"/>)
		/// but if one of those pairs isn't equivalent, the outermost call also returns <c>false</c> and the
		/// cache is discarded.
		/// </summary>
		internal sealed class TypeDefPairSet {
			readonly HashSet<TypeDefPair> set = new HashSet<TypeDefPair>();

			public bool Contains(TypeDef td1, TypeDef td2) =>
				set.Contains(new TypeDefPair(td1, td2)) || set.Contains(new TypeDefPair(td2, td1));

			public void Add(TypeDef td1, TypeDef td2) => set.Add(new TypeDefPair(td1, td2));

			readonly struct TypeDefPair : IEquatable<TypeDefPair> {
				readonly TypeDef td1;
				readonly TypeDef td2;
				public TypeDefPair(TypeDef td1, TypeDef td2) {
					this.td1 = td1;
					this.td2 = td2;
				}
				public bool Equals(TypeDefPair other) => td1 == other.td1 && td2 == other.td2;
				public override bool Equals(object obj) => obj is TypeDefPair other && Equals(other);
				public override int GetHashCode() => RuntimeHelpers.GetHashCode(td1) * 31 + RuntimeHelpers.GetHashCode(td2);
			}
		}

		// See coreclr's CompareTypeDefsForEquivalence(). The caller must check whether we're in a scope
		// where type equivalence isn't allowed (eg. inside a function pointer or a generic instance type).
		internal static bool Equivalent(ref SigComparer comparer, TypeDef td1, TypeDef td2) {
			if (td1 is null || td2 is null)
				return false;
			if (TypeDefPairList.Exists(comparer.typeEquivalenceVisited, td1, td2)) {
				// We're already comparing these types
				return true;
			}
			if (comparer.typeEquivalenceCache is not null && comparer.typeEquivalenceCache.Contains(td1, td2))
				return true;

			// 1. both types must opt into type equivalence
			var info1 = GetInfo(td1);
			if (info1 is null)
				return false;
			var info2 = GetInfo(td2);
			if (info2 is null)
				return false;
			if (!CheckEquivalent(td1) || !CheckEquivalent(td2))
				return false;

			// 2. same type identity
			if (!info1.Value.Equals(info2.Value))
				return false;

			// 2a. same name and namespace (case sensitive)
			if (!EqualsNullIsEmpty(td1.Name, td2.Name) || !EqualsNullIsEmpty(td1.Namespace, td2.Namespace))
				return false;

			// 2b. not nested or equivalent enclosing types
			var declType1 = td1.DeclaringType;
			var declType2 = td2.DeclaringType;
			if ((declType1 is null) != (declType2 is null))
				return false;
			if (declType1 is not null && !Equivalent(ref comparer, declType1, declType2))
				return false;

			// If no other pair is being compared, this is the outermost compare. It owns the cache (created on first Add)
			// and discards it when it returns.
			var oldVisited = comparer.typeEquivalenceVisited;
			bool ownsCache = oldVisited is null;
			comparer.typeEquivalenceVisited = new TypeDefPairList(td1, td2, oldVisited);
			bool result = EquivalentKind(ref comparer, td1, td2);
			comparer.typeEquivalenceVisited = oldVisited;
			if (ownsCache)
				comparer.typeEquivalenceCache = null;
			else if (result)
				(comparer.typeEquivalenceCache ??= new TypeDefPairSet()).Add(td1, td2);
			return result;
		}

		static bool EqualsNullIsEmpty(UTF8String a, UTF8String b) =>
			UTF8String.Equals(UTF8String.IsNull(a) ? UTF8String.Empty : a, UTF8String.IsNull(b) ? UTF8String.Empty : b);

		// 3. type is an interface, struct, enum, or delegate
		static bool EquivalentKind(ref SigComparer comparer, TypeDef td1, TypeDef td2) {
			if (td1.IsInterface)
				return td2.IsInterface;
			if (td1.IsEnum)
				return td2.IsEnum && CompareStructures(ref comparer, td1, td2, true);
			if (td1.IsValueType)
				return td2.IsValueType && !td2.IsEnum && CompareStructures(ref comparer, td1, td2, false);
			if (ExtendsMulticastDelegate(td1))
				return ExtendsMulticastDelegate(td2) && CompareDelegates(ref comparer, td1, td2);
			return false;
		}

		// See coreclr's CompareDelegatesForEquivalence()
		static bool CompareDelegates(ref SigComparer comparer, TypeDef td1, TypeDef td2) {
			var sig1 = td1.FindMethod(InvokeString)?.MethodSig;
			var sig2 = td2.FindMethod(InvokeString)?.MethodSig;
			if (sig1 is null || sig2 is null)
				return false;
			return comparer.EqualsForTypeEquivalence(sig1, sig2);
		}
		static readonly UTF8String InvokeString = new UTF8String("Invoke");

		// See coreclr's CompareStructuresForEquivalence()
		static bool CompareStructures(ref SigComparer comparer, TypeDef td1, TypeDef td2, bool enumMode) {
			// The types can't have any methods
			if (td1.Methods.Count != 0 || td2.Methods.Count != 0)
				return false;

			// Compare the instance fields. All fields must be public instance fields, except for literal fields in enums.
			var fields1 = td1.Fields;
			var fields2 = td2.Fields;
			int index1 = 0, index2 = 0;
			while (true) {
				if (!GetNextInstanceField(fields1, ref index1, enumMode, out var fd1))
					return false;
				if (!GetNextInstanceField(fields2, ref index2, enumMode, out var fd2))
					return false;
				if (fd1 is null && fd2 is null)
					break;
				if (fd1 is null || fd2 is null)
					return false;
				var fieldSig1 = fd1.FieldSig;
				var fieldSig2 = fd2.FieldSig;
				if (fieldSig1 is null || fieldSig2 is null)
					return false;
				if (!comparer.EqualsForTypeEquivalence(fieldSig1, fieldSig2))
					return false;
			}

			if (!enumMode) {
				if (!CompareTypeLayout(td1, td2))
					return false;
			}

			return true;
		}

		// Returns false if the type can't be equivalent, else true and field is the next public instance field or null if there are no more fields
		static bool GetNextInstanceField(IList<FieldDef> fields, ref int index, bool enumMode, out FieldDef field) {
			field = null;
			while (index < fields.Count) {
				var fd = fields[index++];
				if (fd is null)
					return false;
				if (fd.IsPublic && !fd.IsStatic) {
					field = fd;
					return true;
				}
				// Literals are ignored in enums
				if (!enumMode || !fd.IsLiteral)
					return false;
			}
			return true;
		}

		// See coreclr's CompareTypeLayout()
		static bool CompareTypeLayout(TypeDef td1, TypeDef td2) {
			// Both must have sequential or explicit layout
			bool isExplicitLayout;
			if (td1.IsSequentialLayout) {
				if (!td2.IsSequentialLayout)
					return false;
				isExplicitLayout = false;
			}
			else if (td1.IsExplicitLayout) {
				if (!td2.IsExplicitLayout)
					return false;
				isExplicitLayout = true;
			}
			else
				return false;

			// Same charset
			if (td1.StringFormat != td2.StringFormat)
				return false;

			// Same packing and explicit size. A missing ClassLayout row is the same as 0.
			var cl1 = td1.ClassLayout;
			var cl2 = td2.ClassLayout;
			if ((cl1?.PackingSize ?? 0) != (cl2?.PackingSize ?? 0))
				return false;
			if ((cl1?.ClassSize ?? 0) != (cl2?.ClassSize ?? 0))
				return false;

			// Same offsets and same field marshal
			var fields1 = td1.Fields;
			var fields2 = td2.Fields;
			for (int i = 0; i < fields1.Count; i++) {
				if (i >= fields2.Count)
					return false;
				var fd1 = fields1[i];
				var fd2 = fields2[i];
				if (fd1 is null || fd2 is null)
					return false;

				if (isExplicitLayout) {
					// The CLR doesn't check for a missing FieldLayout row (GetFieldOffset() returns S_FALSE and the
					// offset is uninitialized) so it's undefined. Treat it as not equivalent.
					var offset1 = fd1.FieldOffset;
					var offset2 = fd2.FieldOffset;
					if (offset1 is null || offset2 is null || offset1.Value != offset2.Value)
						return false;
				}

				// The CLR checks the HasFieldMarshal flags and compares the blobs. The flag isn't
				// updated when MarshalType is changed (it's updated when the module is written) so use
				// MarshalType instead.
				var marshalType1 = fd1.MarshalType;
				var marshalType2 = fd2.MarshalType;
				if ((marshalType1 is null) != (marshalType2 is null))
					return false;
				if (marshalType1 is not null) {
					var blob1 = GetMarshalBlob(fd1.Module, marshalType1);
					var blob2 = GetMarshalBlob(fd2.Module, marshalType2);
					if (blob1 is null || blob2 is null || !Equals(blob1, blob2))
						return false;
				}
			}

			return true;
		}

		static byte[] GetMarshalBlob(ModuleDef module, MarshalType marshalType) {
			// The CLR compares the raw blobs. dnlib compares the blobs created from the (possibly modified)
			// MarshalTypes. Errors are ignored: invalid marshal types are still written deterministically.
			return Writer.MarshalBlobWriter.Write(module, marshalType, IgnoreWriterErrors.Instance);
		}

		sealed class IgnoreWriterErrors : Writer.IWriterError2 {
			public static readonly IgnoreWriterErrors Instance = new IgnoreWriterErrors();
			public void Error(string message) { }
			public void Error(string message, params object[] args) { }
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
	}
}
