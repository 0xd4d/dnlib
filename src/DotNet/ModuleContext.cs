// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet {
	/// <summary>
	/// <see cref="ModuleDef"/> context
	/// </summary>
	public class ModuleContext {
		IAssemblyResolver? assemblyResolver;
		IResolver? resolver;
		readonly OpCode?[]?[] experimentalOpCodes = new OpCode?[12][];

		/// <summary>
		/// Gets/sets the assembly resolver. This is never <c>null</c>.
		/// </summary>
		[AllowNull]
		public IAssemblyResolver AssemblyResolver {
			get {
				if (assemblyResolver is null)
					Interlocked.CompareExchange(ref assemblyResolver, NullResolver.Instance, null);
				return assemblyResolver;
			}
			set => assemblyResolver = value;
		}

		/// <summary>
		/// Gets/sets the resolver. This is never <c>null</c>.
		/// </summary>
		[AllowNull]
		public IResolver Resolver {
			get {
				if (resolver is null)
					Interlocked.CompareExchange(ref resolver, NullResolver.Instance, null);
				return resolver;
			}
			set => resolver = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleContext() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">Assembly resolver</param>
		/// <exception cref="System.ArgumentNullException">If <paramref name="assemblyResolver"/> is <c>null</c></exception>
		public ModuleContext(IAssemblyResolver assemblyResolver)
			: this(assemblyResolver, new Resolver(assemblyResolver)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resolver">Type/method/field resolver or <c>null</c></param>
		public ModuleContext(IResolver? resolver)
			: this(null, resolver) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">Assembly resolver or <c>null</c></param>
		/// <param name="resolver">Type/method/field resolver or <c>null</c></param>
		public ModuleContext(IAssemblyResolver? assemblyResolver, IResolver? resolver) {
			this.assemblyResolver = assemblyResolver;
			this.resolver = resolver;
			if (resolver is null && assemblyResolver is not null)
				this.resolver = new Resolver(assemblyResolver);
		}

		/// <summary>
		/// Registers an experimental CIL opcode. It must be a 2-byte opcode
		/// where the first byte lies within the range <c>0xF0..0xFB</c>.
		/// </summary>
		public void RegisterExperimentalOpCode(OpCode opCode) {
			byte high = (byte)((ushort)opCode.Value >> 8);
			byte low = (byte)opCode.Value;
			if (!IsExperimental(high))
				throw new ArgumentOutOfRangeException(nameof(opCode), "The first byte of an experimental opcode must be 0xF0..0xFB");
			OpCode?[] array = experimentalOpCodes[high - 0xF0] ??= new OpCode?[256];

			array[low] = opCode;
		}

		/// <summary>
		/// Clears an experimental CIL opcode.
		/// </summary>
		public void ClearExperimentalOpCode(byte high, byte low) {
			if (!IsExperimental(high))
				return;
			OpCode?[]? array = experimentalOpCodes[high - 0xF0];

			if (array != null)
				array[low] = null;
		}

		/// <summary>
		/// Attempts to get an experimental CIL opcode.
		/// </summary>
		/// <returns>The opcode or <c>null</c> if it wasn't registered or if <paramref name="high"/> isn't
		/// within the range <c>0xF0..0xFB</c></returns>
		public OpCode? GetExperimentalOpCode(byte high, byte low) {
			if (!IsExperimental(high))
				return null;
			return experimentalOpCodes[high - 0xF0]?[low];
		}

		static bool IsExperimental(byte high) => high >= 0xF0 && high <= 0xFB;
	}
}
