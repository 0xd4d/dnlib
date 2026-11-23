// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="MethodDef.Parameters"/> has the parameters as the method's code sees them: an instance method
	/// has the hidden <c>this</c> parameter first, then the parameters of the signature.
	/// </summary>
	public class ParameterListTests {
		static MethodDefUser AddMethod(TypeDef type, MethodSig signature) {
			var method = new MethodDefUser("M", signature);
			type.Methods.Add(method);
			return method;
		}

		static ModuleDefUser CreateModule(out TypeDefUser type) {
			var module = new ModuleDefUser("Module.dll");
			type = new TypeDefUser("Ns", "Type", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			return module;
		}

		[Fact]
		public void StaticMethodHasOnlyTheSignaturesParameters() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32, module.CorLibTypes.String));

			Assert.Equal(2, method.Parameters.Count);
			Assert.Equal("System.Int32", method.Parameters[0].Type?.FullName);
			Assert.Equal("System.String", method.Parameters[1].Type?.FullName);
			Assert.False(method.Parameters[0].IsHiddenThisParameter);
			Assert.Equal(0, method.Parameters[0].MethodSigIndex);
			Assert.Equal(1, method.Parameters[1].MethodSigIndex);
		}

		[Fact]
		public void InstanceMethodHasTheHiddenThisParameterFirst() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Int32));

			Assert.Equal(2, method.Parameters.Count);
			Assert.True(method.Parameters[0].IsHiddenThisParameter);
			Assert.Equal("Ns.Type", method.Parameters[0].Type?.FullName);
			Assert.IsType<ClassSig>(method.Parameters[0].Type);
			Assert.Equal("System.Int32", method.Parameters[1].Type?.FullName);
			Assert.Equal(0, method.Parameters[1].MethodSigIndex);
			Assert.Equal(1, method.Parameters[1].Index);
		}

		[Fact]
		public void ThisOfAValueTypeMethodIsAReference() {
			var module = CreateModule(out var type);
			type.BaseType = module.CorLibTypes.GetTypeRef("System", "ValueType");
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void));

			var thisType = Assert.IsType<ByRefSig>(method.Parameters[0].Type);
			Assert.IsType<ValueTypeSig>(thisType.Next);
		}

		[Fact]
		public void ThisOfAGenericTypeMethodIsTheTypeWithItsOwnGenericParameters() {
			var module = CreateModule(out var type);
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void));

			var thisType = Assert.IsType<GenericInstSig>(method.Parameters[0].Type);
			var argument = Assert.IsType<GenericVar>(Assert.Single(thisType.GenericArguments));
			Assert.Equal(0U, argument.Number);
		}

		[Fact]
		public void ReturnParameterHasTheReturnType() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateStatic(module.CorLibTypes.Double));

			Assert.True(method.Parameters.ReturnParameter.IsReturnTypeParameter);
			Assert.Equal("System.Double", method.Parameters.ReturnParameter.Type?.FullName);
		}

		[Fact]
		public void UpdateParameterTypesReadsTheChangedSignature() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32));

			method.MethodSig!.Params.Add(module.CorLibTypes.String);
			method.MethodSig.RetType = module.CorLibTypes.Boolean;
			method.Parameters.UpdateParameterTypes();

			Assert.Equal(2, method.Parameters.Count);
			Assert.Equal("System.String", method.Parameters[1].Type?.FullName);
			Assert.Equal("System.Boolean", method.Parameters.ReturnParameter.Type?.FullName);
		}

		// Fixed in 3c0ecd82: the type of 'this' stayed what it was when the method was added to the type
		[Fact]
		public void UpdateParameterTypesAlsoUpdatesTheHiddenThisParameter() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void));
			Assert.IsType<ClassSig>(method.Parameters[0].Type);

			// The type becomes generic, so 'this' is now Type<T>
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			method.Parameters.UpdateParameterTypes();

			Assert.IsType<GenericInstSig>(method.Parameters[0].Type);
		}

		[Fact]
		public void MovingAMethodToAnotherTypeChangesTheTypeOfThis() {
			var module = CreateModule(out var type);
			var other = new TypeDefUser("Ns", "Other", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(other);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void));

			method.DeclaringType = other;

			Assert.Equal("Ns.Other", method.Parameters[0].Type?.FullName);
		}
	}
}
