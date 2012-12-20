using System;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.Examples {
	// This example creates a new assembly and saves it to disk.
	// This is what it will look like if you decompile it:
	//
	// using System;
	// namespace My.Namespace
	// {
	//     internal class Startup
	//     {
	//         private static int Main(string[] args)
	//         {
	//             Console.WriteLine("Hello World!");
	//             return 0;
	//         }
	//     }
	// }
	public class Example3 {
		public static void Run() {
			// Create a new module. The string passed in is the name of the module,
			// not the file name.
			ModuleDef mod = new ModuleDefUser("MyModule.exe");
			// It's a console application
			mod.Kind = ModuleKind.Console;

			// Add the module to an assembly
			AssemblyDef asm = new AssemblyDefUser("MyAssembly", new Version(1, 2, 3, 4), null, null);
			asm.Modules.Add(mod);

			// Add a .NET resource
			byte[] resourceData = Encoding.UTF8.GetBytes("Hello, world!");
			mod.Resources.Add(new EmbeddedResource("My.Resource", resourceData,
							ManifestResourceAttributes.Private));

			// Add the startup type. It derives from System.Object.
			TypeDef startUpType = new TypeDefUser("My.Namespace", "Startup", mod.CorLibTypes.Object.TypeDefOrRef);
			startUpType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout |
									TypeAttributes.Class | TypeAttributes.AnsiClass;
			// Add the type to the module
			mod.Types.Add(startUpType);

			// Create the entry point method
			MethodDef entryPoint = new MethodDefUser("Main",
				MethodSig.CreateStatic(mod.CorLibTypes.Int32, new SZArraySig(mod.CorLibTypes.String)));
			entryPoint.Attributes = MethodAttributes.Private | MethodAttributes.Static |
							MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
			entryPoint.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			// Name the 1st argument (argument 0 is the return type)
			entryPoint.ParamDefs.Add(new ParamDefUser("args", 1));
			// Add the method to the startup type
			startUpType.Methods.Add(entryPoint);
			// Set module entry point
			mod.EntryPoint = entryPoint;

			// Create a TypeRef to System.Console
			TypeRef consoleRef = new TypeRefUser(mod, "System", "Console", mod.CorLibTypes.AssemblyRef);
			// Create a method ref to 'System.Void System.Console::WriteLine(System.String)'
			MemberRef consoleWrite1 = new MemberRefUser(mod, "WriteLine",
						MethodSig.CreateStatic(mod.CorLibTypes.Void, mod.CorLibTypes.String),
						consoleRef);

			// Add a CIL method body to the entry point method
			CilBody epBody = new CilBody();
			entryPoint.Body = epBody;
			epBody.Instructions.Add(OpCodes.Ldstr.ToInstruction("Hello World!"));
			epBody.Instructions.Add(OpCodes.Call.ToInstruction(consoleWrite1));
			epBody.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
			epBody.Instructions.Add(OpCodes.Ret.ToInstruction());

			// Save the assembly to a file on disk
			mod.Write(@"C:\saved-assembly.exe");
		}
	}
}
