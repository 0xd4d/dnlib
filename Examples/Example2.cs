using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.Examples {
	public class Example2 {
		// This will open the current assembly, add a new class and method to it,
		// and then save the assembly to disk.
		public static void Run() {
			// Open the current module
			ModuleDefMD mod = ModuleDefMD.Load(typeof(Example2).Module);

			// Create a new public class that derives from System.Object
			TypeDef type1 = new TypeDefUser("My.Namespace", "MyType",
								mod.CorLibTypes.Object.TypeDefOrRef);
			type1.Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout |
								TypeAttributes.Class | TypeAttributes.AnsiClass;
			// Make sure to add it to the module or any other type in the module. This is
			// not a nested type, so add it to mod.Types.
			mod.Types.Add(type1);

			// Create a public static System.Int32 field called MyField
			FieldDef field1 = new FieldDefUser("MyField",
							new FieldSig(mod.CorLibTypes.Int32),
							FieldAttributes.Public | FieldAttributes.Static);
			// Add it to the type we created earlier
			type1.Fields.Add(field1);

			// Add a static method that adds both inputs and the static field
			// and returns the result
			MethodImplAttributes methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			MethodAttributes methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
			MethodDef meth1 = new MethodDefUser("MyMethod",
						MethodSig.CreateStatic(mod.CorLibTypes.Int32, mod.CorLibTypes.Int32, mod.CorLibTypes.Int32),
						methImplFlags, methFlags);
			type1.Methods.Add(meth1);

			// Create the CIL method body
			CilBody body = new CilBody();
			meth1.Body = body;
			// Name the 1st and 2nd args a and b, respectively
			meth1.ParamDefs.Add(new ParamDefUser("a", 1));
			meth1.ParamDefs.Add(new ParamDefUser("b", 2));

			// Create a local. We don't really need it but let's add one anyway
			Local local1 = new Local(mod.CorLibTypes.Int32);
			body.Variables.Add(local1);

			// Add the instructions, and use the useless local
			body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
			body.Instructions.Add(OpCodes.Add.ToInstruction());
			body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(field1));
			body.Instructions.Add(OpCodes.Add.ToInstruction());
			body.Instructions.Add(OpCodes.Stloc.ToInstruction(local1));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(local1));
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			// Save the assembly to a file on disk
			mod.Write(@"C:\saved-assembly.dll");
		}
	}
}
