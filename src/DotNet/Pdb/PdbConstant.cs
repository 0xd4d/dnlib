// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A constant in a method scope, eg. "const int SomeConstant = 123;"
	/// </summary>
	public struct PdbConstant {
		string name;
		TypeSig type;
		object value;

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets/sets the type of the constant
		/// </summary>
		public TypeSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		/// Gets/sets the value of the constant
		/// </summary>
		public object Value {
			get { return value; }
			set { this.value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of constant</param>
		/// <param name="type">Type of constant</param>
		/// <param name="value">Constant value</param>
		public PdbConstant(string name, TypeSig type, object value) {
			this.name = name;
			this.type = type;
			this.value = value;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			var type = Type;
			return (type == null ? "" : type.ToString()) + " " + Name + " = " + (Value == null ? "null" : Value.ToString() + " (" + Value.GetType().FullName + ")");
		}
	}
}
