// dnlib: See LICENSE.txt for more info

#nullable enable

// The compiler finds these attributes by name, so they can be internal. Nullable warnings are only
// enabled in the net10.0 build, but the attributes must also be present in the other builds since
// they're part of the public API's nullable annotations.
#if !NET
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
	sealed class AllowNullAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
	sealed class DisallowNullAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	sealed class MaybeNullAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
	sealed class NotNullAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class MaybeNullWhenAttribute : Attribute {
		public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}

	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class NotNullWhenAttribute : Attribute {
		public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	sealed class NotNullIfNotNullAttribute : Attribute {
		public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
		public string ParameterName { get; }
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	sealed class DoesNotReturnAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class DoesNotReturnIfAttribute : Attribute {
		public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
		public bool ParameterValue { get; }
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	sealed class MemberNotNullAttribute : Attribute {
		public MemberNotNullAttribute(string member) => Members = new[] { member };
		public MemberNotNullAttribute(params string[] members) => Members = members;
		public string[] Members { get; }
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	sealed class MemberNotNullWhenAttribute : Attribute {
		public MemberNotNullWhenAttribute(bool returnValue, string member) {
			ReturnValue = returnValue;
			Members = new[] { member };
		}
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members) {
			ReturnValue = returnValue;
			Members = members;
		}
		public bool ReturnValue { get; }
		public string[] Members { get; }
	}
}
#endif
