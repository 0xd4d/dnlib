// dnlib: See LICENSE.txt for more info

// The trim/AOT analyzers only run on the net10.0 build, but the trimmer also reads these
// attributes (by name) from the netstandard2.0 build when an app references it.
#if !NET
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
	sealed class RequiresUnreferencedCodeAttribute : Attribute {
		public RequiresUnreferencedCodeAttribute(string message) => Message = message;
		public string Message { get; }
		public string Url { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
	sealed class RequiresDynamicCodeAttribute : Attribute {
		public RequiresDynamicCodeAttribute(string message) => Message = message;
		public string Message { get; }
		public string Url { get; set; }
	}

	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
	sealed class RequiresAssemblyFilesAttribute : Attribute {
		public RequiresAssemblyFilesAttribute() { }
		public RequiresAssemblyFilesAttribute(string message) => Message = message;
		public string Message { get; }
		public string Url { get; set; }
	}

	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	sealed class UnconditionalSuppressMessageAttribute : Attribute {
		public UnconditionalSuppressMessageAttribute(string category, string checkId) {
			Category = category;
			CheckId = checkId;
		}
		public string Category { get; }
		public string CheckId { get; }
		public string Scope { get; set; }
		public string Target { get; set; }
		public string MessageId { get; set; }
		public string Justification { get; set; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
		AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
		AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
	sealed class DynamicallyAccessedMembersAttribute : Attribute {
		public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) => MemberTypes = memberTypes;
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
	}

	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	sealed class DynamicDependencyAttribute : Attribute {
		public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type) {
			MemberTypes = memberTypes;
			Type = type;
		}
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
		public Type Type { get; }
	}

	[Flags]
	enum DynamicallyAccessedMemberTypes {
		None = 0,
		PublicParameterlessConstructor = 0x0001,
		PublicConstructors = 0x0002 | PublicParameterlessConstructor,
		NonPublicConstructors = 0x0004,
		PublicMethods = 0x0008,
		NonPublicMethods = 0x0010,
		PublicFields = 0x0020,
		NonPublicFields = 0x0040,
		PublicNestedTypes = 0x0080,
		NonPublicNestedTypes = 0x0100,
		PublicProperties = 0x0200,
		NonPublicProperties = 0x0400,
		PublicEvents = 0x0800,
		NonPublicEvents = 0x1000,
		Interfaces = 0x2000,
		All = ~None,
	}
}
#endif
