# dnlib  [![Build status](https://ci.appveyor.com/api/projects/status/h7dqmfac6qjh4hap/branch/master?svg=true)](https://ci.appveyor.com/project/0xd4d/dnlib/branch/master)  [![NuGet](https://img.shields.io/nuget/v/dnlib.svg)](https://www.nuget.org/packages/dnlib/)

.NET module/assembly reader/writer library

Compiling
---------

v3.0 requires VS2019 or later to build it. .NET Core SDK 2.1 or later is also required. See below for breaking changes going from 2.1 to 3.0

An [older v2.1 branch](https://github.com/0xd4d/dnlib/tree/v2.1_VS2010) can be used to build with older VS versions. This branch won't get any new updates.

v3.0 breaking changes
---------------------
- VS2019 is required to compile it
- It targets .NET Framework 3.5 or later and netstandard 2.0 or later (.NET Framework 2.0 and 3.0 aren't supported)
- `*MetaData*` -> `*Metadata*`
- `IMetaData` interface is an abstract class `Metadata`
- `_32Bit*` -> `Bit32*`
- `IAssemblyResolver` only has a `Resolve` method. The other methods are still implemented by the default assembly resolver (`AssemblyResolver`)
- Raw table rows, eg. `RawMethodRow`
	- They are immutable structs and the methods to read them have been renamed from eg. `ReadMethodRow` -> `TryReadMethodRow`
	- An indexer replaces their `Read()` method
	- The `IRawRow` interface has been removed
- The `Constant` table info (`TableInfo`) has an extra padding byte column
- `ModuleWriterOptionsBase.Listener` is obsolete, use the new event `ModuleWriterOptionsBase.WriterEvent` instead
- Module writer events related to the current progress have been removed. Use the new event `ModuleWriterOptionsBase.ProgressUpdated` instead
- `StrongNameKey`, `PublicKey`, `PublicKeyToken` are immutable classes
- `RidList` is a struct
- `IBinaryReader`, `IImageStream` have been removed and replaced with new classes
	- `MemoryImageStream` -> `ByteArrayDataReaderFactory`
		- It has two static factory methods, `Create` and `CreateReader`
	- `BinaryReaderChunk` -> `DataReaderChunk`
	- To get a reader, call `CreateReader` on `IPEImage`, `DataReaderFactory`, or #Blob stream
	- The reader is a struct called `DataReader` and it's not disposable
	- The reader has `Slice` methods to get another reader (replaces the older `Create` methods)
	- Since the reader is a struct, pass it by reference to methods if its position should be updated when the method returns
	- `DataReader.Position` is now a `uint` and not a `long` so expressions that were `long` could now be `uint` and possibly overflow/underflow
		- `reader.Position + 0xFFFFFFFF`
		- `reader.Position + someRandomValue`
		- `var pos = reader.Position;` <-- `pos` is a `uint` and not a `long`
	- `DataReader.Position` only accepts valid values and will throw (an `IOException`) if you set it to an invalid position
- `FileOffset` is `uint`, used to be `long`
- `MethodBodyWriterBase` uses `ArrayWriter` instead of `BinaryWriter` (all virtual methods)
- `ModuleWriter` and `NativeModuleWriter` use `DataWriter` instead of `BinaryWriter`
- The native module writer now tries to fit the new metadata, method bodies, resources and other data in the old locations. This results in smaller files. It can be disabled by creating your own `NativeModuleWriterOptions`
- `MetadataOptions`' `OtherHeaps` and `OtherHeapsEnd` have been removed. Use `CustomHeaps`, `MetadataHeapsAdded` and `PreserveHeapOrder()` instead.
- `Instruction.GetLocal()` returns a local if the instruction is a `ldloca` or `ldloca.s` instruction (it used to return null)
- `ModuleCreationOptions.PdbImplementation` has been removed and replaced with `PdbOptions`
- Renamed
	- `ITokenCreator` -> `ITokenProvider`
	- `MetadataCreator` -> `MetadataFactory`
	- `ResourceDataCreator` -> `ResourceDataFactory`
	- `FullNameCreator` -> `FullNameFactory`

Examples
--------

All examples use C#, but since it's a .NET library, you can use any .NET
language (eg. VB.NET).

See the Examples project for several examples.

Opening a .NET assembly/module
------------------------------

First of all, the important namespaces are `dnlib.DotNet` and
`dnlib.DotNet.Emit`. `dnlib.DotNet.Emit` is only needed if you intend to
read/write method bodies. All the examples below assume you have the
appropriate using statements at the top of each source file:

```csharp
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
```

ModuleDefMD is the class that is created when you open a .NET module. It has
several `Load()` methods that will create a ModuleDefMD instance. If it's not a
.NET module/assembly, a `BadImageFormatException` will be thrown.

Read a .NET module from a file:

```csharp
    ModuleDefMD module = ModuleDefMD.Load(@"C:\path\to\file.exe");
```

Read a .NET module from a byte array:

```csharp
    byte[] data = System.IO.File.ReadAllBytes(@"C:\path\of\file.dll");
    ModuleDefMD module = ModuleDefMD.Load(data);
```

You can also pass in a Stream instance, an address in memory (HINSTANCE) or
even a System.Reflection.Module instance:

```csharp
    System.Reflection.Module reflectionModule = typeof(void).Module;	// Get mscorlib.dll's module
    ModuleDefMD module = ModuleDefMD.Load(reflectionModule);
```

To get the assembly, use its Assembly property:

```csharp
    AssemblyDef asm = module.Assembly;
    Console.WriteLine("Assembly: {0}", asm);
```

Saving a .NET assembly/module
-----------------------------

Use `module.Write()`. It can save the assembly to a file or a Stream.

```csharp
    module.Write(@"C:\saved-assembly.dll");
```

If it's a C++/CLI assembly, you should use `NativeWrite()`

```csharp
    module.NativeWrite(@"C:\saved-assembly.dll");
```

To detect it at runtime, use this code:

```csharp
    if (module.IsILOnly) {
    	// This assembly has only IL code, and no native code (eg. it's a C# or VB assembly)
    	module.Write(@"C:\saved-assembly.dll");
    }
    else {
    	// This assembly has native code (eg. C++/CLI)
    	module.NativeWrite(@"C:\saved-assembly.dll");
    }
```

PDB files
---------

PDB files are read from disk by default. You can change this behaviour by
creating a `ModuleCreationOptions` and passing it in to the code that creates
a module.

To save a PDB file, create a `ModuleWriterOptions` /
`NativeModuleWriterOptions` and set its `WritePdb` property to `true`. By
default, it will create a PDB file with the same name as the output assembly
but with a `.pdb` extension. You can override this by writing the PDB file
name to `PdbFileName` or writing your own stream to `PdbStream`. If
`PdbStream` is initialized, `PdbFileName` should also be initialized because
the name of the PDB file will be written to the PE file.

```csharp
    var mod = ModuleDefMD.Load(@"C:\myfile.dll");
    // ...
    var wopts = new dnlib.DotNet.Writer.ModuleWriterOptions(mod);
    wopts.WritePdb = true;
    // wopts.PdbFileName = @"C:\out2.pdb";	// Set other file name
    mod.Write(@"C:\out.dll", wopts);
```

dnlib supports Windows PDBs, portable PDBs and embedded portable PDBs.

Windows PDBs
------------

It's only possible to write Windows PDBs on Windows (portable PDBs can be written on any OS). dnlib has a managed Windows PDB reader that supports all OSes.

There are two *native* Windows PDB reader and writer implementations, the old `diasymreader.dll` that ships with .NET Framework and `Microsoft.DiaSymReader.Native` which has been updated with more features and bug fixes.

dnlib will use `Microsoft.DiaSymReader.Native` if it exists and fall back to `diasymreader.dll` if needed. `PdbReaderOptions` and `PdbWriterOptions` can be used to disable one of them.

`Microsoft.DiaSymReader.Native` is a NuGet package with 32-bit and 64-bit native DLLs. You have to add a reference to this NuGet package if you want dnlib to use it. dnlib doesn't add a reference to it.

Strong name signing an assembly
-------------------------------

Use the following code to strong name sign the assembly when saving it:

```csharp
    using dnlib.DotNet.Writer;
    ...
    // Open or create an assembly
    ModuleDef mod = ModuleDefMD.Load(.....);
    
    // Create writer options
    var opts = new ModuleWriterOptions(mod);
    
    // Open or create the strong name key
    var signatureKey = new StrongNameKey(@"c:\my\file.snk");
    
    // This method will initialize the required properties
    opts.InitializeStrongNameSigning(mod, signatureKey);
    
    // Write and strong name sign the assembly
    mod.Write(@"C:\out\file.dll", opts);
```

Enhanced strong name signing an assembly
----------------------------------------

See this [MSDN article](http://msdn.microsoft.com/en-us/library/hh415055.aspx)
for info on enhanced strong naming.

Enhanced strong name signing without key migration:

```csharp
    using dnlib.DotNet.Writer;
    ...
    // Open or create an assembly
    ModuleDef mod = ModuleDefMD.Load(....);
    
    // Open or create the signature keys
    var signatureKey = new StrongNameKey(....);
    var signaturePubKey = new StrongNamePublicKey(....);
    
    // Create module writer options
    var opts = new ModuleWriterOptions(mod);
    
    // This method will initialize the required properties
    opts.InitializeEnhancedStrongNameSigning(mod, signatureKey, signaturePubKey);
    
    // Write and strong name sign the assembly
    mod.Write(@"C:\out\file.dll", opts);
```

Enhanced strong name signing with key migration:

```csharp
    using dnlib.DotNet.Writer;
    ...
    // Open or create an assembly
    ModuleDef mod = ModuleDefMD.Load(....);
    
    // Open or create the identity and signature keys
    var signatureKey = new StrongNameKey(....);
    var signaturePubKey = new StrongNamePublicKey(....);
    var identityKey = new StrongNameKey(....);
    var identityPubKey = new StrongNamePublicKey(....);
    
    // Create module writer options
    var opts = new ModuleWriterOptions(mod);
    
    // This method will initialize the required properties and add
    // the required attribute to the assembly.
    opts.InitializeEnhancedStrongNameSigning(mod, signatureKey, signaturePubKey, identityKey, identityPubKey);
    
    // Write and strong name sign the assembly
    mod.Write(@"C:\out\file.dll", opts);
```

Exporting managed methods (DllExport)
-------------------------------------

dnlib supports exporting managed methods so the managed DLL file can be loaded by native code and then executed. .NET Framework supports this feature, but there's no guarantee that other CLRs (eg. .NET Core or Mono/Unity) support this feature.

The `MethodDef` class has an `ExportInfo` property. If it gets initialized, the method gets exported when saving the module. At most 65536 (2^16) methods can be exported. This is a PE file limitation, not a dnlib limitation.

Exported methods should not be generic.

The method's calling convention should be changed to eg. stdcall, or cdecl, by adding an optional modifier to `MethodDef.MethodSig.RetType`. It must be a `System.Runtime.CompilerServices.CallConvCdecl`, `System.Runtime.CompilerServices.CallConvStdcall`, `System.Runtime.CompilerServices.CallConvThiscall`, or a `System.Runtime.CompilerServices.CallConvFastcall`, eg.:

```C#
var type = method.MethodSig.RetType;
type = new CModOptSig(module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CallConvCdecl"), type);
method.MethodSig.RetType = type;
```

Requirements:

- The assembly platform must be x86, x64, IA-64 or ARM (ARM64 isn't supported at the moment). AnyCPU assemblies are not supported. This is as simple as changing (if needed) `ModuleWriterOptions.PEHeadersOptions.Machine` when saving the file. x86 files should set `32-bit required` flag and clear `32-bit preferred` flag in the COR20 header.
- `ModuleWriterOptions.Cor20HeaderOptions.Flags`: The `IL Only` bit must be cleared.
- It must be a DLL file (see `ModuleWriterOptions.PEHeadersOptions.Characteristics`). The file will fail to load at runtime if it's an EXE file.

NOTE: VS' debugger crashes if there's a `DebuggableAttribute` attribute and if the first ctor arg is 0x107. The workaround is to clear the `EnableEditAndContinue` bit:

```C#
var ca = module.Assembly.CustomAttributes.Find("System.Diagnostics.DebuggableAttribute");
if (!(ca is null) && ca.ConstructorArguments.Count == 1) {
    var arg = ca.ConstructorArguments[0];
    // VS' debugger crashes if value == 0x107, so clear EnC bit
    if (arg.Type.FullName == "System.Diagnostics.DebuggableAttribute/DebuggingModes" && arg.Value is int value && value == 0x107) {
        arg.Value = value & ~(int)DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
        ca.ConstructorArguments[0] = arg;
    }
}
```

See the following issues: [#271](https://github.com/0xd4d/dnlib/issues/271), [#172](https://github.com/0xd4d/dnlib/issues/172)

Type classes
------------

The metadata has three type tables: `TypeRef`, `TypeDef`, and `TypeSpec`. The
classes dnlib use are called the same. These three classes all implement
`ITypeDefOrRef`.

There's also type signature classes. The base class is `TypeSig`. You'll find
`TypeSig`s in method signatures (return type and parameter types) and locals.
The `TypeSpec` class also has a `TypeSig` property.

All of these types implement `IType`.

`TypeRef` is a reference to a type in (usually) another assembly.

`TypeDef` is a type definition and it's a type defined in some module. This
class does *not* derive from `TypeRef`. :)

`TypeSpec` can be a generic type, an array type, etc.

`TypeSig` is the base class of all type signatures (found in method sigs and
locals). It has a `Next` property that points to the next `TypeSig`. Eg. a
Byte[] would first contain a `SZArraySig`, and its `Next` property would point
to Byte signature.

`CorLibTypeSig` is a simple corlib type. You don't create these directly. Use
eg. `module.CorLibTypes.Int32` to get a System.Int32 type signature.

`ValueTypeSig` is used when the next class is a value type.

`ClassSig` is used when the next class is a reference type.

`GenericInstSig` is a generic instance type. It has a reference to the generic
type (a `TypeDef` or a `TypeRef`) and the generic arguments.

`PtrSig` is a pointer sig.

`ByRefSig` is a by reference type.

`ArraySig` is a multi-dimensional array type. Most likely when you create an
array, you should use `SZArraySig`, and *not* `ArraySig`.

`SZArraySig` is a single dimension, zero lower bound array. In C#, a `byte[]`
is a `SZArraySig`, and *not* an `ArraySig`.

`GenericVar` is a generic type variable.

`GenericMVar` is a generic method variable.

Some examples if you're not used to the way type signatures are represented
in metadata:

```csharp
    ModuleDef mod = ....;
    
    // Create a byte[]
    SZArraySig array1 = new SZArraySig(mod.CorLibTypes.Byte);
    
    // Create an int[][]
    SZArraySig array2 = new SZArraySig(new SZArraySig(mod.CorLibTypes.Int32));
    
    // Create an int[,]
    ArraySig array3 = new ArraySig(mod.CorLibTypes.Int32, 2);
    
    // Create an int[*] (one-dimensional array)
    ArraySig array4 = new ArraySig(mod.CorLibTypes.Int32, 1);
    
    // Create a Stream[]. Stream is a reference class so it must be enclosed in a ClassSig.
    // If it were a value type, you would use ValueTypeSig instead.
    TypeRef stream = new TypeRefUser(mod, "System.IO", "Stream", mod.CorLibTypes.AssemblyRef);
    SZArraySig array5 = new SZArraySig(new ClassSig(stream));
```

Sometimes you must convert an `ITypeDefOrRef` (`TypeRef`, `TypeDef`, or
`TypeSpec`) to/from a `TypeSig`. There's extension methods you can use:

```csharp
    // array5 is defined above
    ITypeDefOrRef type1 = array5.ToTypeDefOrRef();
    TypeSig type2 = type1.ToTypeSig();
```

Naming conventions of metadata table classes
--------------------------------------------

For most tables in the metadata, there's a corresponding dnlib class with the
exact same or a similar name. Eg. the metadata has a `TypeDef` table, and dnlib
has a `TypeDef` class. Some tables don't have a class because they're
referenced by other classes, and that information is part of some other class.
Eg. the `TypeDef` class contains all its properties and events, even though the
`TypeDef` table has no property or event column.

For each of these table classes, there's an abstract base class, and two sub
classes. These sub classes are named the same as the base class but ends in
either `MD` (for classes created from the metadata) or `User` (for classes
created by the user). Eg. `TypeDef` is the base class, and it has two sub
classes `TypeDefMD` which is auto-created from metadata, and `TypeRefUser`
which is created by the user when adding user types. Most of the XyzMD classes
are internal and can't be referenced directly by the user. They're created by
`ModuleDefMD` (which is the only public `MD` class). All XyzUser classes are
public.

Metadata table classes
----------------------

Here's a list of the most common metadata table classes

`AssemblyDef` is the assembly class.

`AssemblyRef` is an assembly reference.

`EventDef` is an event definition. Owned by a `TypeDef`.

`FieldDef` is a field definition. Owned by a `TypeDef`.

`GenericParam` is a generic parameter (owned by a `MethodDef` or a `TypeDef`)

`MemberRef` is what you create if you need a field reference or a method
reference.

`MethodDef` is a method definition. It usually has a `CilBody` with CIL
instructions. Owned by a `TypeDef`.

`MethodSpec` is a instantiated generic method.

`ModuleDef` is the base module class. When you read an existing module, a
`ModuleDefMD` is created.

`ModuleRef` is a module reference.

`PropertyDef` is a property definition. Owned by a `TypeDef`.

`TypeDef` is a type definition. It contains a lot of interesting stuff,
including methods, fields, properties, etc.

`TypeRef` is a type reference. Usually to a type in another assembly.

`TypeSpec` is a type specification, eg. an array, generic type, etc.

Method classes
--------------

The following are the method classes: `MethodDef`, `MemberRef` (method ref) and
`MethodSpec`. They all implement `IMethod`.

Field classes
-------------

The following are the field classes: `FieldDef` and `MemberRef` (field ref).
They both implement `IField`.

Comparing types, methods, fields, etc
-------------------------------------

dnlib has a `SigComparer` class that can compare any type with any other type.
Any method with any other method, etc. It also has several pre-created
`IEqualityComparer<T>` classes (eg. `TypeEqualityComparer`,
`FieldEqualityComparer`, etc) which you can use if you intend to eg. use a type
as a key in a `Dictionary<TKey, TValue>`.

The `SigComparer` class can also compare types with `System.Type`, methods with
`System.Reflection.MethodBase`, etc.

It has many options you can set, see `SigComparerOptions`. The default options
is usually good enough, though.

```csharp
    // Compare two types
    TypeRef type1 = ...;
    TypeDef type2 = ...;
    if (new SigComparer().Equals(type1, type2))
    	Console.WriteLine("They're equal");

    // Use the type equality comparer
    Dictionary<IType, int> dict = new Dictionary<IType, int>(TypeEqualityComparer.Instance);
    TypeDef type1 = ...;
    dict.Add(type1, 10);

    // Compare a `TypeRef` with a `System.Type`
    TypeRef type1 = ...;
    if (new SigComparer().Equals(type1, typeof(int)))
    	Console.WriteLine("They're equal");
```

It has many `Equals()` and `GetHashCode()` overloads.

.NET Resources
--------------

There's three types of .NET resource, and they all derive from the common base
class `Resource`. `ModuleDef.Resources` is a list of all resources the module
owns.

`EmbeddedResource` is a resource that has data embedded in the owner module.
This is the most common type of resource and it's probably what you want.

`AssemblyLinkedResource` is a reference to a resource in another assembly.

`LinkedResource` is a reference to a resource on disk.

Win32 resources
---------------

`ModuleDef.Win32Resources` can be null or a `Win32Resources` instance. You can
add/remove any Win32 resource blob. dnlib doesn't try to parse these blobs.

Parsing method bodies
---------------------

This is usually only needed if you have decrypted a method body. If it's a
standard method body, you can use `MethodBodyReader.Create()`. If it's similar
to a standard method body, you can derive a class from `MethodBodyReaderBase`
and override the necessary methods.

Resolving references
--------------------

`TypeRef.Resolve()` and `MemberRef.Resolve()` both use
`module.Context.Resolver` to resolve the type, field or method. The custom
attribute parser code may also resolve type references.

If you call Resolve() or read custom attributes, you should initialize
module.Context to a `ModuleContext`. It should normally be shared between all
modules you open.

```csharp
    AssemblyResolver asmResolver = new AssemblyResolver();
    ModuleContext modCtx = new ModuleContext(asmResolver);
    
    // All resolved assemblies will also get this same modCtx
    asmResolver.DefaultModuleContext = modCtx;
    
    // Enable the TypeDef cache for all assemblies that are loaded
    // by the assembly resolver. Only enable it if all auto-loaded
    // assemblies are read-only.
    asmResolver.EnableTypeDefCache = true;
```

All assemblies that you yourself open should be added to the assembly resolver
cache.

```csharp
    ModuleDefMD mod = ModuleDefMD.Load(...);
    mod.Context = modCtx;	// Use the previously created (and shared) context
    // This code assumes you're using the default assembly resolver
    ((AssemblyResolver)mod.Context.AssemblyResolver).AddToCache(mod);
```

Resolving types, methods, etc from metadata tokens
--------------------------------------------------

`ModuleDefMD` has several `ResolveXXX()` methods, eg. `ResolveTypeDef()`,
`ResolveMethod()`, etc.

Creating mscorlib type references
---------------------------------

Every module has a `CorLibTypes` property. It has references to a few of the
simplest types such as all integer types, floating point types, Object, String,
etc. If you need a type that's not there, you must create it yourself, eg.:

```csharp
    TypeRef consoleRef = new TypeRefUser(mod, "System", "Console", mod.CorLibTypes.AssemblyRef);
```

Importing runtime types, methods, fields
----------------------------------------

To import a `System.Type`, `System.Reflection.MethodInfo`,
`System.Reflection.FieldInfo`, etc into a module, use the `Importer` class.

```csharp
    Importer importer = new Importer(mod);
    ITypeDefOrRef consoleRef = importer.Import(typeof(System.Console));
    IMethod writeLine = importer.Import(typeof(System.Console).GetMethod("WriteLine"));
```

You can also use it to import types, methods etc from another `ModuleDef`.

All imported types, methods etc will be references to the original assembly.
I.e., it won't add the imported `TypeDef` to the target module. It will just
create a `TypeRef` to it.

Using decrypted methods
-----------------------

If `ModuleDefMD.MethodDecrypter` is initialized, `ModuleDefMD` will call it and
check whether the method has been decrypted. If it has, it calls
`IMethodDecrypter.GetMethodBody()` which you should implement. Return the new
`MethodBody`. `GetMethodBody()` should usually call `MethodBodyReader.Create()`
which does the actual parsing of the CIL code.

It's also possible to override `ModuleDefMD.ReadUserString()`. This method is
called by the CIL parser when it finds a `Ldstr` instruction. If
`ModuleDefMD.StringDecrypter` is not null, its `ReadUserString()` method is
called with the string token. Return the decrypted string or null if it should
be read from the `#US` heap.

Low level access to the metadata
--------------------------------

The low level classes are in the `dnlib.DotNet.MD` namespace.

Open an existing .NET module/assembly and you get a ModuleDefMD. It has several
properties, eg. `StringsStream` is the #Strings stream.

The `Metadata` property gives you full access to the metadata.

To get a list of all valid TypeDef rids (row IDs), use this code:

```csharp
    using dnlib.DotNet.MD;
    // ...
    ModuleDefMD mod = ModuleDefMD.Load(...);
    RidList typeDefRids = mod.Metadata.GetTypeDefRidList();
    for (int i = 0; i < typeDefRids.Count; i++)
    	Console.WriteLine("rid: {0}", typeDefRids[i]);
```

You don't need to create a `ModuleDefMD`, though. See `MetadataFactory`.

Credits
-------

Big thanks to [Ki](https://github.com/yck1509) for writing the managed Windows PDB reader!

[List of all contributors](https://github.com/0xd4d/dnlib/graphs/contributors)
