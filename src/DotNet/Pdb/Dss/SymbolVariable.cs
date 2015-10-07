// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolVariable : ISymbolVariable {
		readonly ISymUnmanagedVariable variable;

		public SymbolVariable(ISymUnmanagedVariable variable) {
			this.variable = variable;
		}

		public int AddressField1 {
			get {
				uint result;
				variable.GetAddressField1(out result);
				return (int)result;
			}
		}

		public int AddressField2 {
			get {
				uint result;
				variable.GetAddressField2(out result);
				return (int)result;
			}
		}

		public int AddressField3 {
			get {
				uint result;
				variable.GetAddressField3(out result);
				return (int)result;
			}
		}

		public SymAddressKind AddressKind {
			get {
				uint result;
				variable.GetAddressKind(out result);
				return (SymAddressKind)result;
			}
		}

		public object Attributes {
			get {
				uint result;
				variable.GetAttributes(out result);
				return (int)result;
			}
		}

		public int EndOffset {
			get {
				uint result;
				variable.GetEndOffset(out result);
				return (int)result;
			}
		}

		public string Name {
			get {
				uint count;
				variable.GetName(0, out count, null);
				var chars = new char[count];
				variable.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public int StartOffset {
			get {
				uint result;
				variable.GetStartOffset(out result);
				return (int)result;
			}
		}

		public byte[] GetSignature() {
			uint bufSize;
			variable.GetSignature(0, out bufSize, null);
			var buffer = new byte[bufSize];
			variable.GetSignature((uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}
	}
}
