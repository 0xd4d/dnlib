using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

/*

This example shows how to create an assembly from scratch and create two
instances by calling their constructors. One default constructor and another
one taking two arguments.


ILSpy output of created file:

using System;
namespace Ctor.Test
{
   internal class Main
   {
       public static void Main()
       {
           new Main();
           new Main(12345, null);
       }
       public Main()
       {
           Console.WriteLine("Default .ctor called", null);
       }
       public Main(int count, string name)
       {
           Console.WriteLine(".ctor(Int32) called with arg {0}", count);
       }
   }
}


Output of program:

C:\>ctor-test.exe
Default .ctor called
.ctor(Int32) called with arg 12345

*/

namespace dnlib.Examples {
	public class Example4 {
		public static void Run() {
			// This is the file that will be created
			string newFileName = @"C:\ctor-test.exe";

			// Create the module
			var mod = new ModuleDefUser("ctor-test", null,
				new AssemblyRefUser(new AssemblyNameInfo(typeof(int).Assembly.GetName().FullName)));
			// It's a console app
			mod.Kind = ModuleKind.Console;
			// Create the assembly and add the created module to it
			new AssemblyDefUser("ctor-test", new Version(1, 2, 3, 4)).Modules.Add(mod);

			CilBody body;
			// Create the Ctor.Test.Main type
			var main = new TypeDefUser("Ctor.Test", "Main", mod.CorLibTypes.Object.TypeDefOrRef);
			// Add it to the module
			mod.Types.Add(main);
			// Create the static 'void Main()' method
			var entryPoint = new MethodDefUser("Main", MethodSig.CreateStatic(mod.CorLibTypes.Void),
							MethodImplAttributes.IL | MethodImplAttributes.Managed,
							MethodAttributes.Public | MethodAttributes.Static);
			// Set entry point to entryPoint and add it as a Ctor.Test.Main method
			mod.EntryPoint = entryPoint;
			main.Methods.Add(entryPoint);

			// Create System.Console type reference
			var systemConsole = mod.CorLibTypes.GetTypeRef("System", "Console");
			// Create 'void System.Console.WriteLine(string,object)' method reference
			var writeLine2 = new MemberRefUser(mod, "WriteLine",
							MethodSig.CreateStatic(mod.CorLibTypes.Void, mod.CorLibTypes.String,
												mod.CorLibTypes.Object),
							systemConsole);
			// Create System.Object::.ctor method reference. This is the default constructor
			var objectCtor = new MemberRefUser(mod, ".ctor",
							MethodSig.CreateInstance(mod.CorLibTypes.Void),
							mod.CorLibTypes.Object.TypeDefOrRef);

			// Create first Ctor.Test.Main constructor: Main()
			var ctor0 = new MethodDefUser(".ctor", MethodSig.CreateInstance(mod.CorLibTypes.Void),
							MethodImplAttributes.IL | MethodImplAttributes.Managed,
							MethodAttributes.Public |
							MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			// Add the method to Main
			main.Methods.Add(ctor0);
			// Create method body and add a few instruction
			ctor0.Body = body = new CilBody();
			body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			body.Instructions.Add(OpCodes.Call.ToInstruction(objectCtor));
			body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Default .ctor called"));
			body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
			body.Instructions.Add(OpCodes.Call.ToInstruction(writeLine2));
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			// Create second Ctor.Test.Main constructor: Main(int,string)
			var ctor1 = new MethodDefUser(".ctor", MethodSig.CreateInstance(mod.CorLibTypes.Void, mod.CorLibTypes.Int32, mod.CorLibTypes.String),
							MethodImplAttributes.IL | MethodImplAttributes.Managed,
							MethodAttributes.Public |
							MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			// Add the method to Main
			main.Methods.Add(ctor1);
			// Create names for the arguments. This is optional. Since this is an instance method
			// (it's a constructor), the first arg is the 'this' pointer. The normal arguments
			// begin at index 1.
			ctor1.Parameters[1].CreateParamDef();
			ctor1.Parameters[1].ParamDef.Name = "count";
			ctor1.Parameters[2].CreateParamDef();
			ctor1.Parameters[2].ParamDef.Name = "name";
			// Create method body and add a few instruction
			ctor1.Body = body = new CilBody();
			body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			body.Instructions.Add(OpCodes.Call.ToInstruction(objectCtor));
			body.Instructions.Add(OpCodes.Ldstr.ToInstruction(".ctor(Int32) called with arg {0}"));
			body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
			body.Instructions.Add(OpCodes.Box.ToInstruction(mod.CorLibTypes.Int32));
			body.Instructions.Add(OpCodes.Call.ToInstruction(writeLine2));
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			// Create the entry point method body and add instructions to allocate a new Main()
			// object and call the two created ctors.
			entryPoint.Body = body = new CilBody();
			body.Instructions.Add(OpCodes.Newobj.ToInstruction(ctor0));
			body.Instructions.Add(OpCodes.Pop.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(12345));
			body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
			body.Instructions.Add(OpCodes.Newobj.ToInstruction(ctor1));
			body.Instructions.Add(OpCodes.Pop.ToInstruction());
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			// Save the assembly
			mod.Write(newFileName);
		}
	}
}
