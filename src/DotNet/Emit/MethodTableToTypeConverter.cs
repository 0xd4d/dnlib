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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SR = System.Reflection;
using System.IO;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Converts a type address to a type. The address can be found in RuntimeTypeHandle.Value
	/// and it's the same address you use with the WinDbg SOS command !dumpmt.
	/// </summary>
	/// <remarks>It currently doesn't support all possible type addresses if it's .NET 2.0.
	/// Only addresses of normal types can then be used, eg. <c>SomeType</c> but not
	/// <c>SomeType*</c> or <c>SomeType[]</c>, etc.</remarks>
	static class MethodTableToTypeConverter {
		const string METHOD_NAME = "m";
		static readonly MethodInfo setMethodBodyMethodInfo = typeof(MethodBuilder).GetMethod("SetMethodBody", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo localSignatureFieldInfo = typeof(ILGenerator).GetField("m_localSignature", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo sigDoneFieldInfo = typeof(SignatureHelper).GetField("m_sigDone", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo currSigFieldInfo = typeof(SignatureHelper).GetField("m_currSig", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo signatureFieldInfo = typeof(SignatureHelper).GetField("m_signature", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static Dictionary<IntPtr, Type> addrToType = new Dictionary<IntPtr, Type>();
		static Dictionary<object, bool> alreadyInitialized;
		static ModuleBuilder moduleBuilder;
		static int numNewTypes;

		static MethodTableToTypeConverter() {
			var asmb = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynAsm"), AssemblyBuilderAccess.Run);
			moduleBuilder = asmb.DefineDynamicModule("DynMod");
		}

		/// <summary>
		/// Converts <paramref name="address"/> to a <see cref="Type"/>. Only addresses of simple
		/// types (eg. <c>SomeType</c> but not <c>SomeType*</c> or <c>SomeType[]</c>, etc) are
		/// supported if .NET 2.0 is used.
		/// </summary>
		/// <param name="address">Address of type</param>
		/// <returns>The <see cref="Type"/> or <c>null</c></returns>
		public static Type Convert(IntPtr address) {
			Type type;
			if (addrToType.TryGetValue(address, out type))
				return type;

			type = GetTypeUsingTypeBuilder(address);
			if (type == null) {
				// Always call this method since additional assemblies could've been loaded
				InitializeAllSimpleTypeAddresses();
				addrToType.TryGetValue(address, out type);
			}

			addrToType[address] = type;

			return type;
		}

		static void InitializeAllSimpleTypeAddresses() {
			if (alreadyInitialized == null)
				alreadyInitialized = new Dictionary<object, bool>();
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
				if (alreadyInitialized.ContainsKey(asm))
					continue;
				foreach (var mod in asm.GetModules()) {
					if (alreadyInitialized.ContainsKey(mod))
						continue;

					// The reason mod.GetTypes() isn't used is that if it's an obfuscated
					// assembly with invalid types or if it's a ModuleBuilder with a type
					// that hasn't been finished yet, an exception is thrown.
					for (int i = 2; ; i++) {
						Type type;
						try {
							type = mod.ResolveType(0x02000000 + i);
						}
						catch (TypeLoadException) {
							// Invalid type or a type that hasn't been finished building yet.
							continue;
						}
						catch (ArgumentException) {
							// No more types left
							break;
						}
						addrToType[type.TypeHandle.Value] = type;
					}

					var modType = mod.GetType();
					if (modType != typeof(ModuleBuilder) && modType.ToString() != "System.Reflection.Emit.InternalModuleBuilder")
						alreadyInitialized[mod] = true;
				}
				var asmType = asm.GetType();
				if (asmType != typeof(AssemblyBuilder) && asmType.ToString() != "System.Reflection.Emit.InternalAssemblyBuilder")
					alreadyInitialized[asm] = true;
			}
		}

		static Type GetTypeUsingTypeBuilder(IntPtr address) {
			if (moduleBuilder == null)
				return null;

			var tb = moduleBuilder.DefineType(GetNextTypeName());
			var mb = tb.DefineMethod(METHOD_NAME, SR.MethodAttributes.Static, typeof(void), new Type[0]);

			try {
				if (setMethodBodyMethodInfo != null)
					return GetTypeNET45(tb, mb, address);
				else
					return GetTypeNET20(tb, mb, address);
			}
			catch {
				moduleBuilder = null;
				return null;
			}
		}

		// .NET 4.5 and later have the documented SetMethodBody() method.
		static Type GetTypeNET45(TypeBuilder tb, MethodBuilder mb, IntPtr address) {
			byte[] code = new byte[1] { 0x2A };
			int maxStack = 8;
			byte[] locals = GetLocalSignature(address);
			setMethodBodyMethodInfo.Invoke(mb, new object[5] { code, maxStack, locals, null, null });

			var createdMethod = tb.CreateType().GetMethod(METHOD_NAME, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			return createdMethod.GetMethodBody().LocalVariables[0].LocalType;
		}

		// .NET 2.0 - 4.5. This code works with .NET 4.0+ but will throw an exception
		// if .NET 2.0 is used ("operation could destabilize the runtime")
		static Type GetTypeNET20(TypeBuilder tb, MethodBuilder mb, IntPtr address) {
			var ilg = mb.GetILGenerator();
			ilg.Emit(SR.Emit.OpCodes.Ret);

			// We need at least one local to make sure the SignatureHelper from ILGenerator is used.
			ilg.DeclareLocal(typeof(int));

			var locals = GetLocalSignature(address);
			var sigHelper = (SignatureHelper)localSignatureFieldInfo.GetValue(ilg);
			sigDoneFieldInfo.SetValue(sigHelper, true);
			currSigFieldInfo.SetValue(sigHelper, locals.Length);
			signatureFieldInfo.SetValue(sigHelper, locals);

			var createdMethod = tb.CreateType().GetMethod(METHOD_NAME, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			return createdMethod.GetMethodBody().LocalVariables[0].LocalType;
		}

		static string GetNextTypeName() {
			return string.Format("Type{0}", numNewTypes++);
		}

		static byte[] GetLocalSignature(IntPtr mtAddr) {
			ulong mtValue = (ulong)mtAddr.ToInt64();
			if (IntPtr.Size == 4) {
				return new byte[] {
					0x07,
					0x01,
					(byte)ElementType.Internal,
					(byte)mtValue,
					(byte)(mtValue >> 8),
					(byte)(mtValue >> 16),
					(byte)(mtValue >> 24),
				};
			}
			else {
				return new byte[] {
					0x07,
					0x01,
					(byte)ElementType.Internal,
					(byte)mtValue,
					(byte)(mtValue >> 8),
					(byte)(mtValue >> 16),
					(byte)(mtValue >> 24),
					(byte)(mtValue >> 32),
					(byte)(mtValue >> 40),
					(byte)(mtValue >> 48),
					(byte)(mtValue >> 56),
				};
			}
		}
	}
}
