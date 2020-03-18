// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
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

			if (type is TypeDef td &&
				td.DeclaringType is null &&
				!((corLibType = self.GetCorLibTypeSig(td.Namespace, td.Name, td.DefinitionAssembly)) is null)) {
				return corLibType;
			}

			if (type is TypeRef tr &&
				!(tr.ResolutionScope is TypeRef) &&
				!((corLibType = self.GetCorLibTypeSig(tr.Namespace, tr.Name, tr.DefinitionAssembly)) is null)) {
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
		public static CorLibTypeSig GetCorLibTypeSig(this ICorLibTypes self, UTF8String @namespace, UTF8String name, IAssembly defAsm) =>
			self.GetCorLibTypeSig(UTF8String.ToSystemStringOrEmpty(@namespace), UTF8String.ToSystemStringOrEmpty(name), defAsm);

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
			if (defAsm is null || !defAsm.IsCorLib())
				return null;
			return name switch {
				"Void" => self.Void,
				"Boolean" => self.Boolean,
				"Char" => self.Char,
				"SByte" => self.SByte,
				"Byte" => self.Byte,
				"Int16" => self.Int16,
				"UInt16" => self.UInt16,
				"Int32" => self.Int32,
				"UInt32" => self.UInt32,
				"Int64" => self.Int64,
				"UInt64" => self.UInt64,
				"Single" => self.Single,
				"Double" => self.Double,
				"String" => self.String,
				"TypedReference" => self.TypedReference,
				"IntPtr" => self.IntPtr,
				"UIntPtr" => self.UIntPtr,
				"Object" => self.Object,
				_ => null,
			};
		}
	}
}
