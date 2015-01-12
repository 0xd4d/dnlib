// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Access to .NET core library's simple types
	/// </summary>
	public interface ICorLibTypes {
		/// <summary>
		/// Gets a <c>System.Void</c>
		/// </summary>
		CorLibTypeSig Void { get; }

		/// <summary>
		/// Gets a <c>System.Boolean</c>
		/// </summary>
		CorLibTypeSig Boolean { get; }

		/// <summary>
		/// Gets a <c>System.Char</c>
		/// </summary>
		CorLibTypeSig Char { get; }

		/// <summary>
		/// Gets a <c>System.SByte</c>
		/// </summary>
		CorLibTypeSig SByte { get; }

		/// <summary>
		/// Gets a <c>System.Byte</c>
		/// </summary>
		CorLibTypeSig Byte { get; }

		/// <summary>
		/// Gets a <c>System.Int16</c>
		/// </summary>
		CorLibTypeSig Int16 { get; }

		/// <summary>
		/// Gets a <c>System.UInt16</c>
		/// </summary>
		CorLibTypeSig UInt16 { get; }

		/// <summary>
		/// Gets a <c>System.Int32</c>
		/// </summary>
		CorLibTypeSig Int32 { get; }

		/// <summary>
		/// Gets a <c>System.UInt32</c>
		/// </summary>
		CorLibTypeSig UInt32 { get; }

		/// <summary>
		/// Gets a <c>System.Int64</c>
		/// </summary>
		CorLibTypeSig Int64 { get; }

		/// <summary>
		/// Gets a <c>System.UInt64</c>
		/// </summary>
		CorLibTypeSig UInt64 { get; }

		/// <summary>
		/// Gets a <c>System.Single</c>
		/// </summary>
		CorLibTypeSig Single { get; }

		/// <summary>
		/// Gets a <c>System.Double</c>
		/// </summary>
		CorLibTypeSig Double { get; }

		/// <summary>
		/// Gets a <c>System.String</c>
		/// </summary>
		CorLibTypeSig String { get; }

		/// <summary>
		/// Gets a <c>System.TypedReference</c>
		/// </summary>
		CorLibTypeSig TypedReference { get; }

		/// <summary>
		/// Gets a <c>System.IntPtr</c>
		/// </summary>
		CorLibTypeSig IntPtr { get; }

		/// <summary>
		/// Gets a <c>System.UIntPtr</c>
		/// </summary>
		CorLibTypeSig UIntPtr { get; }

		/// <summary>
		/// Gets a <c>System.Object</c>
		/// </summary>
		CorLibTypeSig Object { get; }

		/// <summary>
		/// Gets the assembly reference to the core library
		/// </summary>
		AssemblyRef AssemblyRef { get; }

		/// <summary>
		/// Gets a <see cref="TypeRef"/> that references a type in the core library assembly
		/// </summary>
		/// <param name="namespace">Namespace of type (eg. "System")</param>
		/// <param name="name">Name of type</param>
		/// <returns>A <see cref="TypeRef"/> instance. This instance may be a cached instance.</returns>
		TypeRef GetTypeRef(string @namespace, string name);
	}

	public static partial class Extensions {
		/// <summary>
		/// Gets a <see cref="CorLibTypeSig"/> if <paramref name="type"/> matches a primitive type.
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CorLibTypeSig"/> or <c>null</c> if it didn't match any primitive type</returns>
		public static CorLibTypeSig GetCorLibTypeSig(this ICorLibTypes self, ITypeDefOrRef type) {
			CorLibTypeSig corLibType;

			TypeDef td;
			if ((td = type as TypeDef) != null &&
				td.DeclaringType == null &&
				(corLibType = self.GetCorLibTypeSig(td.Namespace, td.Name, td.DefinitionAssembly)) != null) {
				return corLibType;
			}

			TypeRef tr;
			if ((tr = type as TypeRef) != null &&
				!(tr.ResolutionScope is TypeRef) &&
				(corLibType = self.GetCorLibTypeSig(tr.Namespace, tr.Name, tr.DefinitionAssembly)) != null) {
				return corLibType;
			}

			return null;
		}

		/// <summary>
		/// Gets a <see cref="CorLibTypeSig"/> if <paramref name="namespace"/> and
		/// <paramref name="name"/> matches a primitive type.
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		/// <param name="defAsm">Definition assembly</param>
		/// <returns>A <see cref="CorLibTypeSig"/> or <c>null</c> if it didn't match any primitive type</returns>
		public static CorLibTypeSig GetCorLibTypeSig(this ICorLibTypes self, UTF8String @namespace, UTF8String name, IAssembly defAsm) {
			return self.GetCorLibTypeSig(UTF8String.ToSystemStringOrEmpty(@namespace), UTF8String.ToSystemStringOrEmpty(name), defAsm);
		}

		/// <summary>
		/// Gets a <see cref="CorLibTypeSig"/> if <paramref name="namespace"/> and
		/// <paramref name="name"/> matches a primitive type.
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		/// <param name="defAsm">Definition assembly</param>
		/// <returns>A <see cref="CorLibTypeSig"/> or <c>null</c> if it didn't match any primitive type</returns>
		public static CorLibTypeSig GetCorLibTypeSig(this ICorLibTypes self, string @namespace, string name, IAssembly defAsm) {
			if (@namespace != "System")
				return null;
			if (defAsm == null || !defAsm.IsCorLib())
				return null;
			switch (name) {
			case "Void":	return self.Void;
			case "Boolean":	return self.Boolean;
			case "Char":	return self.Char;
			case "SByte":	return self.SByte;
			case "Byte":	return self.Byte;
			case "Int16":	return self.Int16;
			case "UInt16":	return self.UInt16;
			case "Int32":	return self.Int32;
			case "UInt32":	return self.UInt32;
			case "Int64":	return self.Int64;
			case "UInt64":	return self.UInt64;
			case "Single":	return self.Single;
			case "Double":	return self.Double;
			case "String":	return self.String;
			case "TypedReference": return self.TypedReference;
			case "IntPtr":	return self.IntPtr;
			case "UIntPtr":	return self.UIntPtr;
			case "Object":	return self.Object;
			}
			return null;
		}
	}
}
