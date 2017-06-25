// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Emit;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Custom debug info kind
	/// </summary>
	/// <remarks>See <c>CustomDebugInfoKind</c> in Roslyn source code</remarks>
	public enum PdbCustomDebugInfoKind {
		/// <summary>
		/// <see cref="PdbUsingGroupsCustomDebugInfo"/>
		/// </summary>
		UsingGroups,

		/// <summary>
		/// <see cref="PdbForwardMethodInfoCustomDebugInfo"/>
		/// </summary>
		ForwardMethodInfo,

		/// <summary>
		/// <see cref="PdbForwardModuleInfoCustomDebugInfo"/>
		/// </summary>
		ForwardModuleInfo,

		/// <summary>
		/// <see cref="PdbStateMachineHoistedLocalScopesCustomDebugInfo"/>
		/// </summary>
		StateMachineHoistedLocalScopes,

		/// <summary>
		/// <see cref="PdbStateMachineTypeNameCustomDebugInfo"/>
		/// </summary>
		StateMachineTypeName,

		/// <summary>
		/// <see cref="PdbDynamicLocalsCustomDebugInfo"/>
		/// </summary>
		DynamicLocals,

		/// <summary>
		/// <see cref="PdbEditAndContinueLocalSlotMapCustomDebugInfo"/>
		/// </summary>
		EditAndContinueLocalSlotMap,

		/// <summary>
		/// <see cref="PdbEditAndContinueLambdaMapCustomDebugInfo"/>
		/// </summary>
		EditAndContinueLambdaMap,

		/// <summary>
		/// <see cref="PdbTupleElementNamesCustomDebugInfo"/>
		/// </summary>
		TupleElementNames,
	}

	/// <summary>
	/// Base class of custom debug info added to the PDB file by the compiler
	/// </summary>
	public abstract class PdbCustomDebugInfo {
		/// <summary>
		/// Gets the custom debug info kind
		/// </summary>
		public abstract PdbCustomDebugInfoKind Kind { get; }
	}

	/// <summary>
	/// Unknown custom debug info. If you see an instance of this class, you're using an old dnlib version or
	/// dnlib hasn't been updated to support this new custom debug info kind.
	/// </summary>
	public sealed class PdbUnknownCustomDebugInfo : PdbCustomDebugInfo {
		readonly PdbCustomDebugInfoKind kind;
		readonly byte[] data;

		/// <summary>
		/// Gets the custom debug info kind
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return kind; }
		}

		/// <summary>
		/// Gets the data
		/// </summary>
		public byte[] Data {
			get { return data; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Custom debug info kind</param>
		/// <param name="data">Raw custom debug info data</param>
		public PdbUnknownCustomDebugInfo(PdbCustomDebugInfoKind kind, byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");
			this.kind = kind;
			this.data = data;
		}
	}

	/// <summary>
	/// Contains sizes of using groups
	/// </summary>
	public sealed class PdbUsingGroupsCustomDebugInfo : PdbCustomDebugInfo {
		readonly ThreadSafe.IList<ushort> usingCounts;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.UsingGroups"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.UsingGroups; }
		}

		/// <summary>
		/// Gets the using counts
		/// </summary>
		public ThreadSafe.IList<ushort> UsingCounts {
			get { return usingCounts; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbUsingGroupsCustomDebugInfo() {
			usingCounts = ThreadSafeListCreator.Create<ushort>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="UsingCounts"/></param>
		public PdbUsingGroupsCustomDebugInfo(int capacity) {
			usingCounts = ThreadSafeListCreator.Create<ushort>(capacity);
		}
	}

	/// <summary>
	/// Contains a reference to another method that contains the import strings
	/// </summary>
	public sealed class PdbForwardMethodInfoCustomDebugInfo : PdbCustomDebugInfo {
		IMethodDefOrRef method;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.ForwardMethodInfo"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.ForwardMethodInfo; }
		}

		/// <summary>
		/// Gets/sets the referenced method
		/// </summary>
		public IMethodDefOrRef Method {
			get { return method; }
			set { method = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbForwardMethodInfoCustomDebugInfo() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The referenced method</param>
		public PdbForwardMethodInfoCustomDebugInfo(IMethodDefOrRef method) {
			this.method = method;
		}
	}

	/// <summary>
	/// Contains a reference to another method that contains the per-module debug info (assembly reference aliases)
	/// </summary>
	public sealed class PdbForwardModuleInfoCustomDebugInfo : PdbCustomDebugInfo {
		IMethodDefOrRef method;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.ForwardModuleInfo"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.ForwardModuleInfo; }
		}

		/// <summary>
		/// Gets/sets the referenced method
		/// </summary>
		public IMethodDefOrRef Method {
			get { return method; }
			set { method = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbForwardModuleInfoCustomDebugInfo() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The referenced method</param>
		public PdbForwardModuleInfoCustomDebugInfo(IMethodDefOrRef method) {
			this.method = method;
		}
	}

	/// <summary>
	/// State machine hosted local scope info
	/// </summary>
	public struct StateMachineHoistedLocalScope {
		/// <summary>
		/// true if it's a syntesized local (<see cref="Start"/> and <see cref="End"/> are both null)
		/// </summary>
		public bool IsSynthesizedLocal {
			get { return Start == null && End == null; }
		}

		/// <summary>
		/// The instruction of the first operation in the scope. Can be null if it's a synthesized local
		/// </summary>
		public Instruction Start;

		/// <summary>
		/// The instruction of the first operation outside of the scope or null if it ends at the last instruction in the body.
		/// Can also be null if it's a synthesized local (in which case <see cref="Start"/> is also null, see <see cref="IsSynthesizedLocal"/>)
		/// </summary>
		public Instruction End;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start of the scope</param>
		/// <param name="end">First instruction after the end of the scope</param>
		public StateMachineHoistedLocalScope(Instruction start, Instruction end) {
			Start = start;
			End = end;
		}
	}

	/// <summary>
	/// Contains local scopes for state machine hoisted local variables.
	/// </summary>
	public sealed class PdbStateMachineHoistedLocalScopesCustomDebugInfo : PdbCustomDebugInfo {
		readonly ThreadSafe.IList<StateMachineHoistedLocalScope> scopes;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes; }
		}

		/// <summary>
		/// Gets the scopes
		/// </summary>
		public ThreadSafe.IList<StateMachineHoistedLocalScope> Scopes {
			get { return scopes; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbStateMachineHoistedLocalScopesCustomDebugInfo() {
			scopes = ThreadSafeListCreator.Create<StateMachineHoistedLocalScope>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="Scopes"/></param>
		public PdbStateMachineHoistedLocalScopesCustomDebugInfo(int capacity) {
			scopes = ThreadSafeListCreator.Create<StateMachineHoistedLocalScope>(capacity);
		}
	}

	/// <summary>
	/// Contains the state machine type
	/// </summary>
	public sealed class PdbStateMachineTypeNameCustomDebugInfo : PdbCustomDebugInfo {
		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.StateMachineTypeName"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.StateMachineTypeName; }
		}

		/// <summary>
		/// Gets/sets the state machine type
		/// </summary>
		public TypeDef Type { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbStateMachineTypeNameCustomDebugInfo() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">State machine type</param>
		public PdbStateMachineTypeNameCustomDebugInfo(TypeDef type) {
			Type = type;
		}
	}

	/// <summary>
	/// Contains dynamic flags for local variables and constants
	/// </summary>
	public sealed class PdbDynamicLocalsCustomDebugInfo : PdbCustomDebugInfo {
		readonly ThreadSafe.IList<PdbDynamicLocal> locals;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.DynamicLocals"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.DynamicLocals; }
		}

		/// <summary>
		/// Gets the dynamic locals
		/// </summary>
		public ThreadSafe.IList<PdbDynamicLocal> Locals {
			get { return locals; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbDynamicLocalsCustomDebugInfo() {
			locals = ThreadSafeListCreator.Create<PdbDynamicLocal>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="Locals"/></param>
		public PdbDynamicLocalsCustomDebugInfo(int capacity) {
			locals = ThreadSafeListCreator.Create<PdbDynamicLocal>(capacity);
		}
	}

	/// <summary>
	/// Dynamic local info
	/// </summary>
	public sealed class PdbDynamicLocal {
		readonly ThreadSafe.IList<byte> flags;
		string name;
		Local local;

		/// <summary>
		/// Gets the dynamic flags
		/// </summary>
		public ThreadSafe.IList<byte> Flags {
			get { return flags; }
		}

		/// <summary>
		/// Gets/sets the name of the local. The name must have at most 64 characters and no char can be NUL (0x0000).
		/// If null is written, <see cref="dnlib.DotNet.Emit.Local.Name"/> is returned instead.
		/// </summary>
		public string Name {
			get {
				var n = name;
				if (n != null)
					return n;
				var l = local;
				return l == null ? null : l.Name;
			}
			set { name = value; }
		}

		/// <summary>
		/// true if it's a constant and not a variable (<see cref="Local"/> is null)
		/// </summary>
		public bool IsConstant {
			get { return Local == null; }
		}

		/// <summary>
		/// true if it's a variable (<see cref="Local"/> is not null)
		/// </summary>
		public bool IsVariable {
			get { return Local != null; }
		}

		/// <summary>
		/// Gets/sets the local. Could be null if there's no local (it's a 'const' local).
		/// </summary>
		public Local Local {
			get { return local; }
			set { local = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbDynamicLocal() {
			flags = ThreadSafeListCreator.Create<byte>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="Flags"/></param>
		public PdbDynamicLocal(int capacity) {
			flags = ThreadSafeListCreator.Create<byte>(capacity);
		}
	}

	/// <summary>
	/// Contains the EnC local variable slot map
	/// </summary>
	public sealed class PdbEditAndContinueLocalSlotMapCustomDebugInfo : PdbCustomDebugInfo {
		readonly byte[] data;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap; }
		}

		/// <summary>
		/// Gets the data. Spec: https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLocalSlotMap
		/// </summary>
		public byte[] Data {
			get { return data; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Raw custom debug info data</param>
		public PdbEditAndContinueLocalSlotMapCustomDebugInfo(byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");
			this.data = data;
		}
	}

	/// <summary>
	/// Contains the EnC lambda map
	/// </summary>
	public sealed class PdbEditAndContinueLambdaMapCustomDebugInfo : PdbCustomDebugInfo {
		readonly byte[] data;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.EditAndContinueLambdaMap"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.EditAndContinueLambdaMap; }
		}

		/// <summary>
		/// Gets the data. Spec: https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLambdaAndClosureMap
		/// </summary>
		public byte[] Data {
			get { return data; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Raw custom debug info data</param>
		public PdbEditAndContinueLambdaMapCustomDebugInfo(byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");
			this.data = data;
		}
	}

	/// <summary>
	/// Contains tuple element names for local variables and constants
	/// </summary>
	public sealed class PdbTupleElementNamesCustomDebugInfo : PdbCustomDebugInfo {
		readonly ThreadSafe.IList<PdbTupleElementNames> names;

		/// <summary>
		/// Returns <see cref="PdbCustomDebugInfoKind.TupleElementNames"/>
		/// </summary>
		public override PdbCustomDebugInfoKind Kind {
			get { return PdbCustomDebugInfoKind.TupleElementNames; }
		}

		/// <summary>
		/// Gets the tuple element names
		/// </summary>
		public ThreadSafe.IList<PdbTupleElementNames> Names {
			get { return names; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbTupleElementNamesCustomDebugInfo() {
			names = ThreadSafeListCreator.Create<PdbTupleElementNames>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="Names"/></param>
		public PdbTupleElementNamesCustomDebugInfo(int capacity) {
			names = ThreadSafeListCreator.Create<PdbTupleElementNames>(capacity);
		}
	}

	/// <summary>
	/// Tuple element name info
	/// </summary>
	public sealed class PdbTupleElementNames {
		readonly ThreadSafe.IList<string> tupleElementNames;
		string name;
		Local local;
		Instruction scopeStart, scopeEnd;

		/// <summary>
		/// Gets/sets the name of the local. If null is written, <see cref="dnlib.DotNet.Emit.Local.Name"/> is returned instead.
		/// </summary>
		public string Name {
			get {
				var n = name;
				if (n != null)
					return n;
				var l = local;
				return l == null ? null : l.Name;
			}
			set { name = value; }
		}

		/// <summary>
		/// Gets/sets the local. It's null if it's a constant, and non-null if it's a variable
		/// </summary>
		public Local Local {
			get { return local; }
			set { local = value; }
		}

		/// <summary>
		/// true if it's a constant. Constants have a scope (<see cref="ScopeStart"/> and <see cref="ScopeEnd"/>)
		/// </summary>
		public bool IsConstant {
			get { return local == null; }
		}

		/// <summary>
		/// true if it's a variable. Variables don't have a scope (<see cref="ScopeStart"/> and <see cref="ScopeEnd"/>)
		/// </summary>
		public bool IsVariable {
			get { return local != null; }
		}

		/// <summary>
		/// Gets/sets the start of the scope or null. Only constants have a scope.
		/// </summary>
		public Instruction ScopeStart {
			get { return scopeStart; }
			set { scopeStart = value; }
		}

		/// <summary>
		/// Gets/sets the end of the scope or null if it has no scope or if the scope ends at the end of the body. Only constants have a scope.
		/// </summary>
		public Instruction ScopeEnd {
			get { return scopeEnd; }
			set { scopeEnd = value; }
		}

		/// <summary>
		/// Gets the tuple element names
		/// </summary>
		public ThreadSafe.IList<string> TupleElementNames {
			get { return tupleElementNames; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbTupleElementNames() {
			tupleElementNames = ThreadSafeListCreator.Create<string>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity of <see cref="TupleElementNames"/></param>
		public PdbTupleElementNames(int capacity) {
			tupleElementNames = ThreadSafeListCreator.Create<string>(capacity);
		}
	}
}
