#!/usr/bin/env bash
# Regenerates src/BannedSymbols.txt (used by Microsoft.CodeAnalysis.BannedApiAnalyzers) from the net10.0 XML doc
# file. dnlib must not throw when it reads invalid metadata, so it must not use:
#   - any *OrThrow property (the Resolve*OrThrow/Find*OrThrow methods throw by design and aren't banned)
#   - the Importer.Import*/ModuleDef.Import overloads that throw if the import fails
#   - the methods that use the byte order of the CPU (BitConverter etc.), see host_byte_order() below
# Run ./build.sh first. CI fails if the file changes, ie. if an OrThrow property was added but this script
# wasn't run.
set -euo pipefail

cd "$(dirname "$0")"
xml=src/bin/Release/net10.0/dnlib.xml
if [ ! -f "$xml" ]; then
	echo "$xml doesn't exist, run ./build.sh first" >&2
	exit 1
fi

# All files are little endian, and dnlib must read and write them on a big endian CPU too (eg. s390x). So it must
# not use the methods that convert between bytes and numbers with the byte order of the CPU. The methods that
# don't exist in a target framework are ignored there. Pointer casts (eg. *(uint*)p) can't be banned: only use
# them if BitConverter.IsLittleEndian is true, or for memory that isn't file data.
host_byte_order() {
	local why=";It uses the byte order of the CPU but all files are little endian, read/write the bytes one by one (eg. DataReader, DataWriter, Utils.ReadUInt32LittleEndian(), Utils.GetLittleEndianBytes())"
	local t ints="Int16 UInt16 Int32 UInt32 Int64 UInt64 Int128 UInt128" floats="Half Single Double"
	for t in Boolean Char $ints $floats; do
		echo "M:System.BitConverter.GetBytes(System.$t)$why"
		echo "M:System.BitConverter.TryWriteBytes(System.Span{System.Byte},System.$t)$why"
	done
	for t in Char $ints $floats; do
		echo "M:System.BitConverter.To$t(System.Byte[],System.Int32)$why"
		echo "M:System.BitConverter.To$t(System.ReadOnlySpan{System.Byte})$why"
	done
	# They read/write memory of native code in dnlib's COM classes, that's not file data (it's allowed there)
	for t in Int16 Int32 Int64; do
		echo "M:System.Runtime.InteropServices.Marshal.Read$t(System.IntPtr)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Read$t(System.IntPtr,System.Int32)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Read$t(System.Object,System.Int32)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Write$t(System.IntPtr,System.$t)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Write$t(System.IntPtr,System.Int32,System.$t)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Write$t(System.Object,System.Int32,System.$t)$why"
	done
	echo "M:System.Runtime.InteropServices.Marshal.WriteInt16(System.IntPtr,System.Char)$why"
	echo "M:System.Runtime.InteropServices.Marshal.WriteInt16(System.IntPtr,System.Int32,System.Char)$why"
	echo "M:System.Runtime.InteropServices.Marshal.WriteInt16(System.Object,System.Int32,System.Char)$why"
	# Copies between memory and an array of numbers (the byte[] overloads are fine)
	for t in Char Int16 Int32 Int64 Single Double; do
		echo "M:System.Runtime.InteropServices.Marshal.Copy(System.IntPtr,System.$t[],System.Int32,System.Int32)$why"
		echo "M:System.Runtime.InteropServices.Marshal.Copy(System.$t[],System.Int32,System.IntPtr,System.Int32)$why"
	done
	echo "M:System.Runtime.InteropServices.Marshal.PtrToStructure(System.IntPtr,System.Type)$why"
	echo "M:System.Runtime.InteropServices.Marshal.PtrToStructure(System.IntPtr,System.Object)$why"
	echo "M:System.Runtime.InteropServices.Marshal.PtrToStructure\`\`1(System.IntPtr)$why"
	echo "M:System.Runtime.InteropServices.Marshal.PtrToStructure\`\`1(System.IntPtr,\`\`0)$why"
	echo "M:System.Runtime.InteropServices.Marshal.StructureToPtr(System.Object,System.IntPtr,System.Boolean)$why"
	echo "M:System.Runtime.InteropServices.Marshal.StructureToPtr\`\`1(\`\`0,System.IntPtr,System.Boolean)$why"
	# The bytes of an array of numbers
	echo "M:System.Buffer.BlockCopy(System.Array,System.Int32,System.Array,System.Int32,System.Int32)$why"
	echo "M:System.Buffer.GetByte(System.Array,System.Int32)$why"
	echo "M:System.Buffer.SetByte(System.Array,System.Int32,System.Byte)$why"
	# A string from UTF-16 code units in memory
	echo "M:System.String.#ctor(System.Char*)$why"
	echo "M:System.String.#ctor(System.Char*,System.Int32,System.Int32)$why"
	# Types whose methods reinterpret bytes as numbers or structs
	echo "T:System.Runtime.InteropServices.MemoryMarshal$why"
	echo "T:System.Runtime.CompilerServices.Unsafe$why"
}

ids=$(grep -o 'name="[^"]*"' "$xml" | sed 's/^name="//;s/"$//')
{
	echo "$ids" | grep -E '^P:.*OrThrow$' |
		sed 's/$/;dnlib must not throw when reading invalid metadata, use the member without the OrThrow suffix/'
	echo "$ids" | grep -E '^M:dnlib\.DotNet\.(Importer\.(Import|ImportAsTypeSig|ImportAsOperand)|ModuleDef\.Import)\(' |
		sed 's|$|;It throws if the import fails, use Importer.ImportOrNull()/ImportAsTypeSigOrNull()/ImportAsOperandOrNull()|'
	host_byte_order
} | LC_ALL=C sort > src/BannedSymbols.txt
