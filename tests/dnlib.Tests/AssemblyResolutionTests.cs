using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.Tests;

[TestClass]
public sealed class AssemblyResolutionTests
{
    private static Stream? GetManifestStreamSource() =>
        typeof(AssemblyResolutionTests).Assembly.GetManifestResourceStream(typeof(AssemblyResolutionTests), "Test_Resource.txt");

    [TestMethod]
    public void ResolveSystemRuntimeMethods()
    {
        var moduleDef = LoadTestModuleDef();
        var thisTypeDef = moduleDef.Find(typeof(AssemblyResolutionTests).FullName, false);
        var refMethod = thisTypeDef.FindMethod(nameof(GetManifestStreamSource));
        var instructions = refMethod.Body.Instructions;
        for (var i = 0; i < instructions.Count; i++) {
            var instruction = instructions[i];
            if (instruction.OpCode != OpCodes.Callvirt ||
                !(instruction.Operand is IMethodDefOrRef targetMethodDefOrRef) ||
                !UTF8String.Equals(targetMethodDefOrRef.Name, "GetManifestResourceStream") ||
                !UTF8String.Equals(targetMethodDefOrRef.DeclaringType.FullName, "System.Reflection.Assembly")) continue;

            var targetMethodDef = targetMethodDefOrRef.ResolveMethodDefThrow();
            Assert.AreEqual("System.IO.Stream", targetMethodDef.ReturnType.FullName);
            Assert.AreEqual(3, targetMethodDef.Parameters.Count);
        }
    }
    internal static ModuleDefMD LoadTestModuleDef() {
        var asmResolver = new AssemblyResolver();
        asmResolver.DefaultModuleContext = new ModuleContext(asmResolver);
        var options = new ModuleCreationOptions(asmResolver.DefaultModuleContext);

        var thisModule = ModuleDefMD.Load(typeof(AssemblyResolutionTests).Module, options);
        asmResolver.AddToCache(thisModule);

        return thisModule;
    }
}
