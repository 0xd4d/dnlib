using System;
using dnlib.DotNet;

namespace dnlib.Examples {
	// This example will open mscorlib.dll and then print out all types
	// in the assembly, including the number of methods, fields, properties
	// and events each type has.
	public class Example1 {
		public static void Run() {
			// Load mscorlib.dll
			string filename = typeof(void).Module.FullyQualifiedName;
			ModuleDefMD mod = ModuleDefMD.Load(filename);

			int totalNumTypes = 0;
			// mod.Types only returns non-nested types.
			// mod.GetTypes() returns all types, including nested types.
			foreach (TypeDef type in mod.GetTypes()) {
				totalNumTypes++;
				Console.WriteLine();
				Console.WriteLine("Type: {0}", type.FullName);
				if (type.BaseType != null)
					Console.WriteLine("  Base type: {0}", type.BaseType.FullName);

				Console.WriteLine("  Methods: {0}", type.Methods.Count);
				Console.WriteLine("  Fields: {0}", type.Fields.Count);
				Console.WriteLine("  Properties: {0}", type.Properties.Count);
				Console.WriteLine("  Events: {0}", type.Events.Count);
				Console.WriteLine("  Nested types: {0}", type.NestedTypes.Count);

				if (type.Interfaces.Count > 0) {
					Console.WriteLine("  Interfaces:");
					foreach (InterfaceImpl iface in type.Interfaces)
						Console.WriteLine("    {0}", iface.Interface.FullName);
				}
			}
			Console.WriteLine();
			Console.WriteLine("Total number of types: {0}", totalNumTypes);
		}
	}
}
