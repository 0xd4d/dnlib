// C++/CLI code that's compiled to a mixed mode assembly by the tests (see CppCliExportTests): a DLL that
// exports managed and native functions. Native code calls an exported managed function through a small piece
// of native code (a stub) that jumps to an address in the file's vtable fixups, where the CLR puts the
// address of the method.

#pragma managed

extern "C" __declspec(dllexport) int ManagedAdd(int a, int b) {
	return a + b;
}

extern "C" __declspec(dllexport) int ManagedMultiply(int a, int b) {
	return a * b;
}

// Not exported
int ManagedHelper(int a) {
	return a + 1;
}

extern "C" __declspec(dllexport) int ManagedLength(const wchar_t* text) {
	System::String^ managedText = gcnew System::String(text);
	return ManagedHelper(managedText->Length) - 1;
}

#pragma unmanaged

extern "C" __declspec(dllexport) int NativeTwice(int a) {
	return a * 2;
}

// Native code that calls managed code
extern "C" __declspec(dllexport) int NativeCallsManaged(int a, int b) {
	return ManagedAdd(a, b) + ManagedMultiply(a, b);
}
