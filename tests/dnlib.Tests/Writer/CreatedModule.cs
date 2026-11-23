// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A small module that's created with the API, for tests that write something specific and read it again:
	/// one class <c>Ns.Type</c> that a test adds its fields and methods to.
	/// </summary>
	sealed class CreatedModule {
		public ModuleDefUser Module { get; }
		public TypeDefUser Type { get; }

		public CreatedModule() {
			Module = new ModuleDefUser("Created.dll") { Kind = ModuleKind.Dll };
			new AssemblyDefUser("Created", new System.Version(1, 0, 0, 0)).Modules.Add(Module);
			Type = new TypeDefUser("Ns", "Type", Module.CorLibTypes.Object.TypeDefOrRef);
			Type.Attributes = TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed;
			Module.Types.Add(Type);
		}

		/// <summary>
		/// Adds a <c>static void</c> method without parameters that has a body with the instructions
		/// </summary>
		public MethodDefUser AddMethod(string name, params Instruction[] instructions) {
			var method = new MethodDefUser(name, MethodSig.CreateStatic(Module.CorLibTypes.Void), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			var body = new CilBody();
			foreach (var instruction in instructions)
				body.Instructions.Add(instruction);
			method.Body = body;
			Type.Methods.Add(method);
			return method;
		}

		/// <summary>
		/// Adds a <c>static</c> field
		/// </summary>
		public FieldDefUser AddField(string name, TypeSig type) {
			var field = new FieldDefUser(name, new FieldSig(type), FieldAttributes.Public | FieldAttributes.Static);
			Type.Fields.Add(field);
			return field;
		}

		/// <summary>
		/// Writes the module with the default options. Throws <see cref="ModuleWriterException"/> if the
		/// writer reports an error.
		/// </summary>
		public byte[] Write() {
			var stream = new MemoryStream();
			Module.Write(stream);
			return stream.ToArray();
		}

		/// <summary>
		/// Writes the module and reads it again
		/// </summary>
		public ModuleDefMD WriteAndRead() => ModuleDefMD.Load(Write());

		/// <summary>
		/// Returns the type <c>Ns.Type</c> of a module that was returned by <see cref="WriteAndRead"/>
		/// </summary>
		public static TypeDef GetType(ModuleDef module) => module.Find("Ns.Type", isReflectionName: true)!;
	}
}
