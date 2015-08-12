// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using SR = System.Reflection;
using System.Reflection.Emit;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Reads code from a DynamicMethod
	/// </summary>
	public class DynamicMethodBodyReader : MethodBodyReaderBase, ISignatureReaderHelper {
		static readonly ReflectionFieldInfo rtdmOwnerFieldInfo = new ReflectionFieldInfo("m_owner");
		static readonly ReflectionFieldInfo dmResolverFieldInfo = new ReflectionFieldInfo("m_resolver");
		static readonly ReflectionFieldInfo rslvCodeFieldInfo = new ReflectionFieldInfo("m_code");
		static readonly ReflectionFieldInfo rslvDynamicScopeFieldInfo = new ReflectionFieldInfo("m_scope");
		static readonly ReflectionFieldInfo rslvMethodFieldInfo = new ReflectionFieldInfo("m_method");
		static readonly ReflectionFieldInfo rslvLocalsFieldInfo = new ReflectionFieldInfo("m_localSignature");
		static readonly ReflectionFieldInfo rslvMaxStackFieldInfo = new ReflectionFieldInfo("m_stackSize");
		static readonly ReflectionFieldInfo rslvExceptionsFieldInfo = new ReflectionFieldInfo("m_exceptions");
		static readonly ReflectionFieldInfo rslvExceptionHeaderFieldInfo = new ReflectionFieldInfo("m_exceptionHeader");
		static readonly ReflectionFieldInfo scopeTokensFieldInfo = new ReflectionFieldInfo("m_tokens");
		static readonly ReflectionFieldInfo gfiFieldHandleFieldInfo = new ReflectionFieldInfo("m_field", "m_fieldHandle");
		static readonly ReflectionFieldInfo gfiContextFieldInfo = new ReflectionFieldInfo("m_context");
		static readonly ReflectionFieldInfo gmiMethodHandleFieldInfo = new ReflectionFieldInfo("m_method", "m_methodHandle");
		static readonly ReflectionFieldInfo gmiContextFieldInfo = new ReflectionFieldInfo("m_context");
		static readonly ReflectionFieldInfo ehCatchAddrFieldInfo = new ReflectionFieldInfo("m_catchAddr");
		static readonly ReflectionFieldInfo ehCatchClassFieldInfo = new ReflectionFieldInfo("m_catchClass");
		static readonly ReflectionFieldInfo ehCatchEndAddrFieldInfo = new ReflectionFieldInfo("m_catchEndAddr");
		static readonly ReflectionFieldInfo ehCurrentCatchFieldInfo = new ReflectionFieldInfo("m_currentCatch");
		static readonly ReflectionFieldInfo ehTypeFieldInfo = new ReflectionFieldInfo("m_type");
		static readonly ReflectionFieldInfo ehStartAddrFieldInfo = new ReflectionFieldInfo("m_startAddr");
		static readonly ReflectionFieldInfo ehEndAddrFieldInfo = new ReflectionFieldInfo("m_endAddr");
		static readonly ReflectionFieldInfo ehEndFinallyFieldInfo = new ReflectionFieldInfo("m_endFinally");
		static readonly ReflectionFieldInfo vamMethodFieldInfo = new ReflectionFieldInfo("m_method");
		static readonly ReflectionFieldInfo vamDynamicMethodFieldInfo = new ReflectionFieldInfo("m_dynamicMethod");

		readonly ModuleDef module;
		readonly Importer importer;
		readonly GenericParamContext gpContext;
		readonly MethodDef method;
		readonly int codeSize;
		readonly int maxStack;
		readonly List<object> tokens;
		readonly IList<object> ehInfos;
		readonly byte[] ehHeader;

		class ReflectionFieldInfo {
			SR.FieldInfo fieldInfo;
			readonly string fieldName1;
			readonly string fieldName2;

			public ReflectionFieldInfo(string fieldName) {
				this.fieldName1 = fieldName;
			}

			public ReflectionFieldInfo(string fieldName1, string fieldName2) {
				this.fieldName1 = fieldName1;
				this.fieldName2 = fieldName2;
			}

			public object Read(object instance) {
				if (fieldInfo == null)
					InitializeField(instance.GetType());
				if (fieldInfo == null)
					throw new Exception(string.Format("Couldn't find field '{0}' or '{1}'", fieldName1, fieldName2));

				return fieldInfo.GetValue(instance);
			}

			public bool Exists(object instance) {
				InitializeField(instance.GetType());
				return fieldInfo != null;
			}

			void InitializeField(Type type) {
				if (fieldInfo != null)
					return;

				var flags = SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				fieldInfo = type.GetField(fieldName1, flags);
				if (fieldInfo == null && fieldName2 != null)
					fieldInfo = type.GetField(fieldName2, flags);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module that will own the method body</param>
		/// <param name="obj">This can be one of several supported types: the delegate instance
		/// created by DynamicMethod.CreateDelegate(), a DynamicMethod instance, a RTDynamicMethod
		/// instance or a DynamicResolver instance.</param>
		public DynamicMethodBodyReader(ModuleDef module, object obj)
			: this(module, obj, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module that will own the method body</param>
		/// <param name="obj">This can be one of several supported types: the delegate instance
		/// created by DynamicMethod.CreateDelegate(), a DynamicMethod instance, a RTDynamicMethod
		/// instance or a DynamicResolver instance.</param>
		/// <param name="gpContext">Generic parameter context</param>
		public DynamicMethodBodyReader(ModuleDef module, object obj, GenericParamContext gpContext) {
			this.module = module;
			this.importer = new Importer(module, ImporterOptions.TryToUseDefs, gpContext);
			this.gpContext = gpContext;

			if (obj == null)
				throw new ArgumentNullException("obj");

			var del = obj as Delegate;
			if (del != null) {
				obj = del.Method;
				if (obj == null)
					throw new Exception("Delegate.Method == null");
			}

			if (obj.GetType().ToString() == "System.Reflection.Emit.DynamicMethod+RTDynamicMethod") {
				obj = rtdmOwnerFieldInfo.Read(obj) as DynamicMethod;
				if (obj == null)
					throw new Exception("RTDynamicMethod.m_owner is null or invalid");
			}

			if (obj is DynamicMethod) {
				obj = dmResolverFieldInfo.Read(obj);
				if (obj == null)
					throw new Exception("No resolver found");
			}

			if (obj.GetType().ToString() != "System.Reflection.Emit.DynamicResolver")
				throw new Exception("Couldn't find DynamicResolver");

			var code = rslvCodeFieldInfo.Read(obj) as byte[];
			if (code == null)
				throw new Exception("No code");
			codeSize = code.Length;
			var delMethod = rslvMethodFieldInfo.Read(obj) as SR.MethodBase;
			if (delMethod == null)
				throw new Exception("No method");
			maxStack = (int)rslvMaxStackFieldInfo.Read(obj);

			var scope = rslvDynamicScopeFieldInfo.Read(obj);
			if (scope == null)
				throw new Exception("No scope");
			var tokensList = scopeTokensFieldInfo.Read(scope) as System.Collections.IList;
			if (tokensList == null)
				throw new Exception("No tokens");
			tokens = new List<object>(tokensList.Count);
			for (int i = 0; i < tokensList.Count; i++)
				tokens.Add(tokensList[i]);

			ehInfos = (IList<object>)rslvExceptionsFieldInfo.Read(obj);
			ehHeader = rslvExceptionHeaderFieldInfo.Read(obj) as byte[];

			UpdateLocals(rslvLocalsFieldInfo.Read(obj) as byte[]);
			this.reader = MemoryImageStream.Create(code);
			this.method = CreateMethodDef(delMethod);
			this.parameters = this.method.Parameters;
		}

		class ExceptionInfo {
			public int[] CatchAddr;
			public Type[] CatchClass;
			public int[] CatchEndAddr;
			public int CurrentCatch;
			public int[] Type;
			public int StartAddr;
			public int EndAddr;
			public int EndFinally;
		}

		static List<ExceptionInfo> CreateExceptionInfos(IList<object> ehInfos) {
			if (ehInfos == null)
				return new List<ExceptionInfo>();

			var infos = new List<ExceptionInfo>(ehInfos.Count);

			foreach (var ehInfo in ehInfos) {
				var eh = new ExceptionInfo {
					CatchAddr = (int[])ehCatchAddrFieldInfo.Read(ehInfo),
					CatchClass = (Type[])ehCatchClassFieldInfo.Read(ehInfo),
					CatchEndAddr = (int[])ehCatchEndAddrFieldInfo.Read(ehInfo),
					CurrentCatch = (int)ehCurrentCatchFieldInfo.Read(ehInfo),
					Type = (int[])ehTypeFieldInfo.Read(ehInfo),
					StartAddr = (int)ehStartAddrFieldInfo.Read(ehInfo),
					EndAddr = (int)ehEndAddrFieldInfo.Read(ehInfo),
					EndFinally = (int)ehEndFinallyFieldInfo.Read(ehInfo),
				};
				infos.Add(eh);
			}

			return infos;
		}

		void UpdateLocals(byte[] localsSig) {
			if (localsSig == null || localsSig.Length == 0)
				return;

			var sig = SignatureReader.ReadSig(this, module.CorLibTypes, localsSig, gpContext) as LocalSig;
			if (sig == null)
				return;

			foreach (var local in sig.Locals)
				locals.Add(new Local(local));
		}

		MethodDef CreateMethodDef(SR.MethodBase delMethod) {
			bool isStatic = true;
			var method = new MethodDefUser();

			var retType = GetReturnType(delMethod);
			var pms = GetParameters(delMethod);
			if (isStatic)
				method.Signature = MethodSig.CreateStatic(retType, pms.ToArray());
			else
				method.Signature = MethodSig.CreateInstance(retType, pms.ToArray());

			method.Parameters.UpdateParameterTypes();
			method.ImplAttributes = MethodImplAttributes.IL;
			method.Attributes = MethodAttributes.PrivateScope;
			if (isStatic)
				method.Attributes |= MethodAttributes.Static;

			return module.UpdateRowId(method);
		}

		TypeSig GetReturnType(SR.MethodBase mb) {
			var mi = mb as SR.MethodInfo;
			if (mi != null)
				return importer.ImportAsTypeSig(mi.ReturnType);
			return module.CorLibTypes.Void;
		}

		List<TypeSig> GetParameters(SR.MethodBase delMethod) {
			var pms = new List<TypeSig>();
			foreach (var param in delMethod.GetParameters())
				pms.Add(importer.ImportAsTypeSig(param.ParameterType));
			return pms;
		}

		/// <summary>
		/// Reads the code
		/// </summary>
		/// <returns></returns>
		public bool Read() {
			ReadInstructionsNumBytes((uint)codeSize);
			CreateExceptionHandlers();

			return true;
		}

		void CreateExceptionHandlers() {
			if (ehHeader != null) {
				var reader = new BinaryReader(new MemoryStream(ehHeader));
				byte b = (byte)reader.ReadByte();
				if ((b & 0x40) == 0) { // DynamicResolver only checks bit 6
					// Calculate num ehs exactly the same way that DynamicResolver does
					int numHandlers = (ushort)((reader.ReadByte() - 2) / 12);
					reader.ReadInt16();
					for (int i = 0; i < numHandlers; i++) {
						var eh = new ExceptionHandler();
						eh.HandlerType = (ExceptionHandlerType)reader.ReadInt16();
						int offs = reader.ReadUInt16();
						eh.TryStart = GetInstructionThrow((uint)offs);
						eh.TryEnd = GetInstruction((uint)(reader.ReadSByte() + offs));
						offs = reader.ReadUInt16();
						eh.HandlerStart = GetInstructionThrow((uint)offs);
						eh.HandlerEnd = GetInstruction((uint)(reader.ReadSByte() + offs));

						if (eh.HandlerType == ExceptionHandlerType.Catch)
							eh.CatchType = ReadToken(reader.ReadUInt32()) as ITypeDefOrRef;
						else if (eh.HandlerType == ExceptionHandlerType.Filter)
							eh.FilterStart = GetInstruction(reader.ReadUInt32());
						else
							reader.ReadUInt32();

						exceptionHandlers.Add(eh);
					}
				}
				else {
					reader.BaseStream.Position--;
					int numHandlers = (ushort)(((reader.ReadUInt32() >> 8) - 4) / 24);
					for (int i = 0; i < numHandlers; i++) {
						var eh = new ExceptionHandler();
						eh.HandlerType = (ExceptionHandlerType)reader.ReadInt32();
						int offs = reader.ReadInt32();
						eh.TryStart = GetInstructionThrow((uint)offs);
						eh.TryEnd = GetInstruction((uint)(reader.ReadInt32() + offs));
						offs = reader.ReadInt32();
						eh.HandlerStart = GetInstructionThrow((uint)offs);
						eh.HandlerEnd = GetInstruction((uint)(reader.ReadInt32() + offs));

						if (eh.HandlerType == ExceptionHandlerType.Catch)
							eh.CatchType = ReadToken(reader.ReadUInt32()) as ITypeDefOrRef;
						else if (eh.HandlerType == ExceptionHandlerType.Filter)
							eh.FilterStart = GetInstruction(reader.ReadUInt32());
						else
							reader.ReadUInt32();

						exceptionHandlers.Add(eh);
					}
				}
			}
			else if (ehInfos != null) {
				foreach (var ehInfo in CreateExceptionInfos(ehInfos)) {
					var tryStart = GetInstructionThrow((uint)ehInfo.StartAddr);
					var tryEnd = GetInstruction((uint)ehInfo.EndAddr);
					var endFinally = ehInfo.EndFinally < 0 ? null : GetInstruction((uint)ehInfo.EndFinally);
					for (int i = 0; i < ehInfo.CurrentCatch; i++) {
						var eh = new ExceptionHandler();
						eh.HandlerType = (ExceptionHandlerType)ehInfo.Type[i];
						eh.TryStart = tryStart;
						eh.TryEnd = eh.HandlerType == ExceptionHandlerType.Finally ? endFinally : tryEnd;
						eh.FilterStart = null;	// not supported by DynamicMethod.ILGenerator
						eh.HandlerStart = GetInstructionThrow((uint)ehInfo.CatchAddr[i]);
						eh.HandlerEnd = GetInstruction((uint)ehInfo.CatchEndAddr[i]);
						eh.CatchType = importer.Import(ehInfo.CatchClass[i]);
						exceptionHandlers.Add(eh);
					}
				}
			}
		}

		/// <summary>
		/// Returns the created method. Must be called after <see cref="Read()"/>.
		/// </summary>
		/// <returns>A new <see cref="CilBody"/> instance</returns>
		public MethodDef GetMethod() {
			bool initLocals = true;
			var cilBody = new CilBody(initLocals, instructions, exceptionHandlers, locals);
			cilBody.MaxStack = (ushort)Math.Min(maxStack, ushort.MaxValue);
			instructions = null;
			exceptionHandlers = null;
			locals = null;
			method.Body = cilBody;
			return method;
		}

		/// <inheritdoc/>
		protected override IField ReadInlineField(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as IField;
		}

		/// <inheritdoc/>
		protected override IMethod ReadInlineMethod(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as IMethod;
		}

		/// <inheritdoc/>
		protected override MethodSig ReadInlineSig(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as MethodSig;
		}

		/// <inheritdoc/>
		protected override string ReadInlineString(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as string ?? string.Empty;
		}

		/// <inheritdoc/>
		protected override ITokenOperand ReadInlineTok(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as ITokenOperand;
		}

		/// <inheritdoc/>
		protected override ITypeDefOrRef ReadInlineType(Instruction instr) {
			return ReadToken(reader.ReadUInt32()) as ITypeDefOrRef;
		}

		object ReadToken(uint token) {
			uint rid = token & 0x00FFFFFF;
			switch (token >> 24) {
			case 0x02:
				return ImportType(rid);

			case 0x04:
				return ImportField(rid);

			case 0x06:
			case 0x0A:
				return ImportMethod(rid);

			case 0x11:
				return ImportSignature(rid);

			case 0x70:
				return Resolve(rid) as string;

			default:
				return null;
			}
		}

		IMethod ImportMethod(uint rid) {
			var obj = Resolve(rid);
			if (obj == null)
				return null;

			if (obj is RuntimeMethodHandle)
				return importer.Import(SR.MethodBase.GetMethodFromHandle((RuntimeMethodHandle)obj));

			if (obj.GetType().ToString() == "System.Reflection.Emit.GenericMethodInfo") {
				var context = (RuntimeTypeHandle)gmiContextFieldInfo.Read(obj);
				var method = SR.MethodBase.GetMethodFromHandle((RuntimeMethodHandle)gmiMethodHandleFieldInfo.Read(obj), context);
				return importer.Import(method);
			}

			if (obj.GetType().ToString() == "System.Reflection.Emit.VarArgMethod") {
				var method = GetVarArgMethod(obj);
				if (!(method is DynamicMethod))
					return importer.Import(method);
				obj = method;
			}

			var dm = obj as DynamicMethod;
			if (dm != null)
				throw new Exception("DynamicMethod calls another DynamicMethod");

			return null;
		}

		SR.MethodInfo GetVarArgMethod(object obj) {
			if (vamDynamicMethodFieldInfo.Exists(obj)) {
				// .NET 4.0+
				var method = vamMethodFieldInfo.Read(obj) as SR.MethodInfo;
				var dynMethod = vamDynamicMethodFieldInfo.Read(obj) as DynamicMethod;
				return dynMethod ?? method;
			}
			else {
				// .NET 2.0
				// This is either a DynamicMethod or a MethodInfo
				return vamMethodFieldInfo.Read(obj) as SR.MethodInfo;
			}
		}

		IField ImportField(uint rid) {
			var obj = Resolve(rid);
			if (obj == null)
				return null;

			if (obj is RuntimeFieldHandle)
				return importer.Import(SR.FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)obj));

			if (obj.GetType().ToString() == "System.Reflection.Emit.GenericFieldInfo") {
				var context = (RuntimeTypeHandle)gfiContextFieldInfo.Read(obj);
				var field = SR.FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)gfiFieldHandleFieldInfo.Read(obj), context);
				return importer.Import(field);
			}

			return null;
		}

		ITypeDefOrRef ImportType(uint rid) {
			var obj = Resolve(rid);
			if (obj is RuntimeTypeHandle)
				return importer.Import(Type.GetTypeFromHandle((RuntimeTypeHandle)obj));

			return null;
		}

		CallingConventionSig ImportSignature(uint rid) {
			var sig = Resolve(rid) as byte[];
			if (sig == null)
				return null;

			return SignatureReader.ReadSig(this, module.CorLibTypes, sig, gpContext);
		}

		object Resolve(uint index) {
			if (index >= (uint)tokens.Count)
				return null;
			return tokens[(int)index];
		}

		ITypeDefOrRef ISignatureReaderHelper.ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:
			case Table.TypeRef:
			case Table.TypeSpec:
				return ImportType(rid);
			}
			return null;
		}

		TypeSig ISignatureReaderHelper.ConvertRTInternalAddress(IntPtr address) {
			return importer.ImportAsTypeSig(MethodTableToTypeConverter.Convert(address));
		}
	}
}
