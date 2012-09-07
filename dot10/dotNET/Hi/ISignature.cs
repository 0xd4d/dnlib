using System.Collections.Generic;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// All signatures implement this interface
	/// </summary>
	public interface ISignature {
	}

	/// <summary>
	/// A field signature
	/// </summary>
	public class FieldSig : ISignature {
		CallingConvention callingConvention;
		ITypeSig type;

		/// <summary>
		/// Gets/sets the <see cref="dot10.dotNET.Hi.CallingConvention"/>
		/// </summary>
		public CallingConvention CallingConvention {
			get { return callingConvention; }
			set {
				callingConvention = (callingConvention & CallingConvention.Mask) | (value & ~CallingConvention.Mask);
			}
		}

		/// <summary>
		/// The field type
		/// </summary>
		public ITypeSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldSig() {
			this.callingConvention = CallingConvention.Field;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Field type</param>
		public FieldSig(ITypeSig type) {
			this.callingConvention = CallingConvention.Field;
			this.type = type;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Field type</param>
		/// <param name="callingConvention">The calling convention</param>
		public FieldSig(ITypeSig type, CallingConvention callingConvention) {
			this.callingConvention = CallingConvention.Field | (callingConvention & ~CallingConvention.Mask);
			this.type = type;
		}

		/// <inheritdoc/>
		public override string ToString() {
			if (type == null)
				return "<<<NULL>>>";
			return type.FullName;
		}
	}

	/// <summary>
	/// A method signature
	/// </summary>
	public class MethodSig : ISignature {
		CallingConvention callingConvention;
		ITypeSig retType;
		IList<ITypeSig> parameters;
		uint genParamCount;
		IList<ITypeSig> paramsAfterSentinel;

		/// <summary>
		/// Gets/sets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get { return callingConvention; }
			set { callingConvention = value; }
		}

		/// <summary>
		/// Gets/sets the return type
		/// </summary>
		public ITypeSig RetType {
			get { return retType; }
			set { retType = value; }
		}

		/// <summary>
		/// Gets the parameters. This is never <c>null</c>
		/// </summary>
		public IList<ITypeSig> Params {
			get { return parameters; }
		}

		/// <summary>
		/// Gets/sets the generic param count
		/// </summary>
		public uint GenParamCount {
			get { return genParamCount; }
			set { genParamCount = value; }
		}

		/// <summary>
		/// Gets the parameters that are present after the sentinel. Note that this is <c>null</c>
		/// if there's no sentinel. It can still be empty even if it's not <c>null</c>.
		/// </summary>
		public IList<ITypeSig> ParamsAfterSentinel {
			get { return paramsAfterSentinel; }
			set { paramsAfterSentinel = value; }
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateStatic(ITypeSig retType) {
			return new MethodSig(CallingConvention.Default, 0, retType);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateStatic(ITypeSig retType, ITypeSig argType1) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateStatic(ITypeSig retType, ITypeSig argType1, ITypeSig argType2) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateStatic(ITypeSig retType, ITypeSig argType1, ITypeSig argType2, ITypeSig argType3) {
			return new MethodSig(CallingConvention.Default, 0, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates a static MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateStatic(ITypeSig retType, params ITypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default, 0, retType, argTypes);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateInstance(ITypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateInstance(ITypeSig retType, ITypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateInstance(ITypeSig retType, ITypeSig argType1, ITypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateInstance(ITypeSig retType, ITypeSig argType1, ITypeSig argType2, ITypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates an instance MethodSig
		/// </summary>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateInstance(ITypeSig retType, params ITypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis, 0, retType, argTypes);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, ITypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2, ITypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates a static generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateStaticGeneric(uint genParamCount, ITypeSig retType, params ITypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.Generic, genParamCount, retType, argTypes);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, ITypeSig retType) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1, argType2);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2, ITypeSig argType3) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argType1, argType2, argType3);
		}

		/// <summary>
		/// Creates an instance generic MethodSig
		/// </summary>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public static MethodSig CreateInstanceGeneric(uint genParamCount, ITypeSig retType, params ITypeSig[] argTypes) {
			return new MethodSig(CallingConvention.Default | CallingConvention.HasThis | CallingConvention.Generic, genParamCount, retType, argTypes);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, ITypeSig retType) {
			this.callingConvention = callingConvention;
			this.retType = retType;
			this.parameters = new List<ITypeSig>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, ITypeSig retType, ITypeSig argType1) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = new List<ITypeSig> { argType1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = new List<ITypeSig> { argType1, argType2 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argType1">Arg type #1</param>
		/// <param name="argType2">Arg type #2</param>
		/// <param name="argType3">Arg type #3</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, ITypeSig retType, ITypeSig argType1, ITypeSig argType2, ITypeSig argType3) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = new List<ITypeSig> { argType1, argType2, argType3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callingConvention">Calling convention</param>
		/// <param name="genParamCount">Number of generic parameters</param>
		/// <param name="retType">Return type</param>
		/// <param name="argTypes">Argument types</param>
		public MethodSig(CallingConvention callingConvention, uint genParamCount, ITypeSig retType, params ITypeSig[] argTypes) {
			this.callingConvention = callingConvention;
			this.genParamCount = genParamCount;
			this.retType = retType;
			this.parameters = new List<ITypeSig>(argTypes);
		}
	}
}
