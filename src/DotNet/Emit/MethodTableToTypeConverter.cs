// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SR = System.Reflection;
using dnlib.Threading;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Converts a type address to a <see cref="Type"/>. The address can be found in
	/// <c>RuntimeTypeHandle.Value</c> and it's the same address you use with the WinDbg SOS command
	/// !dumpmt.
	/// </summary>
	static class MethodTableToTypeConverter {
		const string METHOD_NAME = "m";
		static readonly MethodInfo setMethodBodyMethodInfo = typeof(MethodBuilder).GetMethod("SetMethodBody", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo localSignatureFieldInfo = typeof(ILGenerator).GetField("m_localSignature", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo sigDoneFieldInfo = typeof(SignatureHelper).GetField("m_sigDone", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo currSigFieldInfo = typeof(SignatureHelper).GetField("m_currSig", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo signatureFieldInfo = typeof(SignatureHelper).GetField("m_signature", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo ptrFieldInfo = typeof(RuntimeTypeHandle).GetField("m_ptr", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		static readonly Dictionary<IntPtr, Type> addrToType = new Dictionary<IntPtr, Type>();
		static ModuleBuilder moduleBuilder;
		static int numNewTypes;
#if THREAD_SAFE
		static readonly Lock theLock = Lock.Create();
#endif

		static MethodTableToTypeConverter() {
			if (ptrFieldInfo == null) {
				var asmb = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynAsm"), AssemblyBuilderAccess.Run);
				moduleBuilder = asmb.DefineDynamicModule("DynMod");
			}
		}

		/// <summary>
		/// Converts <paramref name="address"/> to a <see cref="Type"/>.
		/// </summary>
		/// <param name="address">Address of type</param>
		/// <returns>The <see cref="Type"/> or <c>null</c></returns>
		public static Type Convert(IntPtr address) {
			Type type;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (addrToType.TryGetValue(address, out type))
				return type;

			type = GetTypeNET20(address) ?? GetTypeUsingTypeBuilder(address);
			addrToType[address] = type;
			return type;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
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
					return GetTypeNET40(tb, mb, address);
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

		// This code works with .NET 4.0+ but will throw an exception if .NET 2.0 is used
		// ("operation could destabilize the runtime")
		static Type GetTypeNET40(TypeBuilder tb, MethodBuilder mb, IntPtr address) {
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

		// .NET 2.0 - 3.5
		static Type GetTypeNET20(IntPtr address) {
			if (ptrFieldInfo == null)
				return null;
			object th = new RuntimeTypeHandle();
			ptrFieldInfo.SetValue(th, address);
			return Type.GetTypeFromHandle((RuntimeTypeHandle)th);
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
