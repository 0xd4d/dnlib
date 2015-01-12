// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics.SymbolStore;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiVariable : ISymbolVariable {
		public uint Addr1 { get; private set; }
		public string Name { get; private set; }
		public ushort Flags { get; private set; }

		public void Read(IImageStream stream) {
			Addr1 = stream.ReadUInt32();
			stream.Position += 10;
			Flags = stream.ReadUInt16();
			Name = PdbReader.ReadCString(stream);
		}

		#region ISymbolVariable

		public int AddressField1 {
			get { return (int)Addr1; }
		}

		public SymAddressKind AddressKind {
			get { return SymAddressKind.ILOffset; }
		}

		public object Attributes {
			get {
				const int fCompGenx = 4;
				const int VAR_IS_COMP_GEN = 1;
				if ((Flags & fCompGenx) != 0)
					return VAR_IS_COMP_GEN;
				else
					return 0;
			}
		}

		public int AddressField2 {
			get { throw new NotImplementedException(); }
		}

		public int AddressField3 {
			get { throw new NotImplementedException(); }
		}

		public int EndOffset {
			get { throw new NotImplementedException(); }
		}

		public byte[] GetSignature() {
			throw new NotImplementedException();
		}

		public int StartOffset {
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}