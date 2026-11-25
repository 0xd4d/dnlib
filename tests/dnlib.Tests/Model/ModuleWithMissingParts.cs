// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;

namespace dnlib.Tests.Model {
	/// <summary>
	/// A module that was created with the API and that has members without a signature, a custom attribute
	/// without a constructor, instructions without an operand, ...: for the tests of code that walks over
	/// everything in a module and must skip what isn't there.
	/// </summary>
	static class ModuleWithMissingParts {
		public static ModuleDefUser Create() {
			var module = new ModuleDefUser("Test.dll");
			var assembly = new AssemblyDefUser("Test");
			assembly.Modules.Add(module);
			var types = module.CorLibTypes;
			var type = new TypeDefUser("Ns", "Type");
			module.Types.Add(type);

			type.Fields.Add(new FieldDefUser("NoSignature"));
			type.Fields.Add(new FieldDefUser("NoType", new FieldSig(null)));
			var method = new MethodDefUser("NoSignature");
			type.Methods.Add(method);
			var methodWithBody = new MethodDefUser("NoOperands", new MethodSig(dnlib.DotNet.CallingConvention.Default, 0, null, new TypeSig?[] { null, types.Int32 }));
			methodWithBody.Body = new dnlib.DotNet.Emit.CilBody();
			methodWithBody.Body.Instructions.Add(new dnlib.DotNet.Emit.Instruction(dnlib.DotNet.Emit.OpCodes.Ldtoken, null));
			methodWithBody.Body.Instructions.Add(new dnlib.DotNet.Emit.Instruction(dnlib.DotNet.Emit.OpCodes.Call, null));
			methodWithBody.Body.Variables.Add(new dnlib.DotNet.Emit.Local(null));
			methodWithBody.Body.ExceptionHandlers.Add(new dnlib.DotNet.Emit.ExceptionHandler(dnlib.DotNet.Emit.ExceptionHandlerType.Catch));
			type.Methods.Add(methodWithBody);
			type.Methods.Add(new MethodDefUser("Native") { MethodBody = new dnlib.DotNet.Emit.NativeMethodBody((dnlib.PE.RVA)0x1000) });
			type.Properties.Add(new PropertyDefUser("NoSignature"));
			type.Events.Add(new EventDefUser("NoType"));
			type.Interfaces.Add(new InterfaceImplUser(null));
			type.GenericParameters.Add(new GenericParamUser(0));
			type.GenericParameters[0].GenericParamConstraints.Add(new GenericParamConstraintUser(null));
			type.DeclSecurities.Add(new DeclSecurityUser(SecurityAction.Demand, new System.Collections.Generic.List<SecurityAttribute> { new SecurityAttribute(null) }));

			// Custom attributes: no constructor, and arguments that have no type or whose value is an argument
			type.CustomAttributes.Add(new CustomAttribute(null));
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(types.Void, types.Object, types.Object), new TypeRefUser(module, "Ns", "Attr"));
			var attribute = new CustomAttribute(constructor);
			attribute.ConstructorArguments.Add(new CAArgument(types.Object, new CAArgument(types.Int32, 1)));
			attribute.ConstructorArguments.Add(new CAArgument(null, null));
			attribute.ConstructorArguments.Add(new CAArgument(new SZArraySig(types.Object), new System.Collections.Generic.List<CAArgument> { new CAArgument(types.Object, null) }));
			attribute.NamedArguments.Add(new CANamedArgument(true, null, null, new CAArgument(null, null)));
			type.CustomAttributes.Add(attribute);

			module.Resources.Add(new EmbeddedResource("Embedded", new byte[] { 1, 2, 3 }));
			module.Resources.Add(new LinkedResource("Linked", new FileDefUser("File.bin", dnlib.DotNet.FileAttributes.ContainsNoMetadata, null), ManifestResourceAttributes.Public));
			module.Resources.Add(new AssemblyLinkedResource("InAssembly", new AssemblyRefUser("Other"), ManifestResourceAttributes.Public));
			module.ExportedTypes.Add(new ExportedTypeUser(module, 0, "Ns", "Exported", TypeAttributes.Public, null));
			module.ExportedTypes.Add(new ExportedTypeUser(module, 0, "Ns", "InFile", TypeAttributes.Public, new FileDefUser("Other.netmodule", dnlib.DotNet.FileAttributes.ContainsMetadata, null)));

			return module;
		}
	}
}
