// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
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

		// A parameter gets its name and its attributes from a row of the Param table (a ParamDef), which is
		// optional. Without one, the parameter has no name until a ParamDef is created for it.
		[Fact]
		public void ParameterWithoutAParamDefGetsOneWhenItIsCreated() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Int32, module.CorLibTypes.Int32, module.CorLibTypes.String));
			var second = method.Parameters[2];
			Assert.False(second.HasParamDef);
			Assert.Null(second.ParamDef);
			Assert.Equal("", second.Name);
			Assert.Throws<NullValueException>(() => second.ParamDefOrThrow);

			second.CreateParamDef();
			second.Name = "text";

			var paramDef = Assert.Single(method.ParamDefs);
			Assert.Same(paramDef, second.ParamDef);
			Assert.Same(paramDef, second.ParamDefOrThrow);
			// The sequence number of the 2nd parameter of the signature is 2: number 0 is the return value
			Assert.Equal(2, paramDef.Sequence);
			Assert.Equal("text", paramDef.Name);
			Assert.Equal("text", second.Name);
			// Creating it again doesn't add another one
			second.CreateParamDef();
			Assert.Single(method.ParamDefs);
		}

		[Fact]
		public void ReturnParameterAndThisCanHaveAParamDefToo() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Int32, module.CorLibTypes.Int32));
			var returnParameter = method.Parameters.ReturnParameter;
			var thisParameter = method.Parameters[0];

			returnParameter.CreateParamDef();
			thisParameter.CreateParamDef();

			// The return value is in the Param table with the sequence number 0. 'this' isn't in the table: its
			// ParamDef only exists in memory.
			var paramDef = Assert.Single(method.ParamDefs);
			Assert.Equal(0, paramDef.Sequence);
			Assert.Same(paramDef, returnParameter.ParamDef);
			Assert.True(thisParameter.HasParamDef);
			Assert.DoesNotContain(thisParameter.ParamDef, method.ParamDefs);
		}

		[Fact]
		public void ParameterKnowsItsMethodAndItsPlace() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Int32));
			var parameters = method.Parameters;

			Assert.Same(method, parameters.Method);
			Assert.All(parameters, a => Assert.Same(method, a.Method));
			Assert.True(parameters[0].IsHiddenThisParameter && !parameters[0].IsNormalMethodParameter);
			Assert.True(parameters[1].IsNormalMethodParameter && !parameters[1].IsReturnTypeParameter);
			Assert.True(parameters.ReturnParameter.IsReturnTypeParameter);
			Assert.Equal(Parameter.HIDDEN_THIS_METHOD_SIG_INDEX, parameters[0].MethodSigIndex);
			Assert.Equal(Parameter.RETURN_TYPE_METHOD_SIG_INDEX, parameters.ReturnParameter.MethodSigIndex);
			Assert.Equal(1, parameters.MethodSigIndexBase);
			Assert.Same(module.CorLibTypes.Int32, parameters[1].TypeOrThrow);
		}

		// The list always has the parameters of the method's signature: it can't be changed directly
		[Fact]
		public void ListIsReadOnly() {
			var module = CreateModule(out var type);
			var method = AddMethod(type, MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32, module.CorLibTypes.String));
			IList<Parameter> list = method.Parameters;
			var first = list[0];

			Assert.True(list.IsReadOnly);
			Assert.Throws<NotSupportedException>(() => list.Add(first));
			Assert.Throws<NotSupportedException>(() => list.Insert(0, first));
			Assert.Throws<NotSupportedException>(() => list.Remove(first));
			Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
			Assert.Throws<NotSupportedException>(() => list.Clear());
			Assert.Throws<NotSupportedException>(() => list[0] = first);

			Assert.Equal(2, list.Count);
			Assert.True(list.Contains(first));
			Assert.Equal(1, list.IndexOf(list[1]));
			Assert.Equal(1, method.Parameters.IndexOf(list[1]));
			var copy = new Parameter[3];
			list.CopyTo(copy, 1);
			Assert.Equal(new Parameter?[] { null, list[0], list[1] }, copy);
			Assert.Equal(new[] { list[0], list[1] }, list.ToArray());
		}
	}
}
