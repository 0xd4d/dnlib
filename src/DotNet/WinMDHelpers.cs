// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet {
	enum ClrAssembly {
		Mscorlib,
		SystemNumericsVectors,
		SystemObjectModel,
		SystemRuntime,
		SystemRuntimeInteropServicesWindowsRuntime,
		SystemRuntimeWindowsRuntime,
		SystemRuntimeWindowsRuntimeUIXaml,
	}

	static class WinMDHelpers {
		struct ClassName : IEquatable<ClassName> {
			public readonly UTF8String Namespace;
			public readonly UTF8String Name;
			// Not used when comparing for equality etc
			public readonly bool IsValueType;

			public ClassName(UTF8String ns, UTF8String name, bool isValueType = false) {
				this.Namespace = ns;
				this.Name = name;
				this.IsValueType = isValueType;
			}

			public ClassName(string ns, string name, bool isValueType = false) {
				this.Namespace = ns;
				this.Name = name;
				this.IsValueType = isValueType;
			}

			public static bool operator ==(ClassName a, ClassName b) {
				return a.Equals(b);
			}

			public static bool operator !=(ClassName a, ClassName b) {
				return !a.Equals(b);
			}

			public bool Equals(ClassName other) {
				// Don't check IsValueType
				return UTF8String.Equals(Namespace, other.Namespace) &&
					UTF8String.Equals(Name, other.Name);
			}

			public override bool Equals(object obj) {
				if (!(obj is ClassName))
					return false;
				return Equals((ClassName)obj);
			}

			public override int GetHashCode() {
				// Don't use IsValueType
				return UTF8String.GetHashCode(Namespace) ^ UTF8String.GetHashCode(Name);
			}

			public override string ToString() {
				return string.Format("{0}.{1}", Namespace, Name);
			}
		}

		sealed class ProjectedClass {
			public readonly ClassName WinMDClass;
			public readonly ClassName ClrClass;
			public readonly ClrAssembly ClrAssembly;
			public readonly ClrAssembly ContractAssembly;

			public ProjectedClass(string mdns, string mdname, string clrns, string clrname, ClrAssembly clrAsm, ClrAssembly contractAsm, bool winMDValueType, bool clrValueType) {
				this.WinMDClass = new ClassName(mdns, mdname, winMDValueType);
				this.ClrClass = new ClassName(clrns, clrname, clrValueType);
				this.ClrAssembly = clrAsm;
				this.ContractAssembly = contractAsm;
			}

			public override string ToString() {
				return string.Format("{0} <-> {1}, {2}", WinMDClass, ClrClass, CreateAssembly(null, ContractAssembly));
			}
		}

		// See https://github.com/dotnet/coreclr/blob/master/src/inc/winrtprojectedtypes.h
		// To generate this code replace the contents of src/inc/winrtprojectedtypes.h with:
		//	DEFINE_PROJECTED_ENUM
		//	=>	DEFINE_PROJECTED_STRUCT
		//	^DEFINE_PROJECTED\w*_STRUCT\s*\(("[^"]+"),\s*("[^"]+"),\s*("[^"]+"),\s*("[^"]+"),\s*(\w+),\s*(\w+).*$
		//	=>	\t\t\tnew ProjectedClass(\1, \2, \3, \4, ClrAssembly.\5, ClrAssembly.\6, true, true),
		//	^DEFINE_PROJECTED\w+\s*\(("[^"]+"),\s*("[^"]+"),\s*("[^"]+"),\s*("[^"]+"),\s*(\w+),\s*(\w+).*$
		//	=>	\t\t\tnew ProjectedClass(\1, \2, \3, \4, ClrAssembly.\5, ClrAssembly.\6, false, false),
		// Sometimes the types aren't both structs or both classes. Known cases:
		//		IReference`1 (class)	vs	Nullable`1 (struct)
		//		IKeyValuePair`2 (class)	vs	KeyValuePair`2 (struct)
		//		TypeName (struct)		vs	Type (class)
		//		HResult (struct)		vs	Exception (class)
		// See md/winmd/adapter.cpp WinMDAdapter::RewriteTypeInSignature() or check the types
		// in a decompiler.
		static readonly ProjectedClass[] ProjectedClasses = new ProjectedClass[] {
			new ProjectedClass("Windows.Foundation.Metadata", "AttributeUsageAttribute", "System", "AttributeUsageAttribute", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Metadata", "AttributeTargets", "System", "AttributeTargets", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, true, true),

			new ProjectedClass("Windows.UI", "Color", "Windows.UI", "Color", ClrAssembly.SystemRuntimeWindowsRuntime, ClrAssembly.SystemRuntimeWindowsRuntime, true, true),

			new ProjectedClass("Windows.Foundation", "DateTime", "System", "DateTimeOffset", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "EventHandler`1", "System", "EventHandler`1", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation", "EventRegistrationToken", "System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntimeInteropServicesWindowsRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "HResult", "System", "Exception", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, true, false),
			new ProjectedClass("Windows.Foundation", "IReference`1", "System", "Nullable`1", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, true),
			new ProjectedClass("Windows.Foundation", "Point", "Windows.Foundation", "Point", ClrAssembly.SystemRuntimeWindowsRuntime, ClrAssembly.SystemRuntimeWindowsRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "Rect", "Windows.Foundation", "Rect", ClrAssembly.SystemRuntimeWindowsRuntime, ClrAssembly.SystemRuntimeWindowsRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "Size", "Windows.Foundation", "Size", ClrAssembly.SystemRuntimeWindowsRuntime, ClrAssembly.SystemRuntimeWindowsRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "TimeSpan", "System", "TimeSpan", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, true, true),
			new ProjectedClass("Windows.Foundation", "Uri", "System", "Uri", ClrAssembly.SystemRuntime, ClrAssembly.SystemRuntime, false, false),

			new ProjectedClass("Windows.Foundation", "IClosable", "System", "IDisposable", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),

			new ProjectedClass("Windows.Foundation.Collections", "IIterable`1", "System.Collections.Generic", "IEnumerable`1", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Collections", "IVector`1", "System.Collections.Generic", "IList`1", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Collections", "IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Collections", "IMap`2", "System.Collections.Generic", "IDictionary`2", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Collections", "IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.Foundation.Collections", "IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, true),

			new ProjectedClass("Windows.UI.Xaml.Input", "ICommand", "System.Windows.Input", "ICommand", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),

			new ProjectedClass("Windows.UI.Xaml.Interop", "IBindableIterable", "System.Collections", "IEnumerable", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),
			new ProjectedClass("Windows.UI.Xaml.Interop", "IBindableVector", "System.Collections", "IList", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, false, false),

			new ProjectedClass("Windows.UI.Xaml.Interop", "INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),
			new ProjectedClass("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),
			new ProjectedClass("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),
			new ProjectedClass("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, true, true),

			new ProjectedClass("Windows.UI.Xaml.Data", "INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),
			new ProjectedClass("Windows.UI.Xaml.Data", "PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),
			new ProjectedClass("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs", ClrAssembly.SystemObjectModel, ClrAssembly.SystemObjectModel, false, false),

			new ProjectedClass("Windows.UI.Xaml", "CornerRadius", "Windows.UI.Xaml", "CornerRadius", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml", "Duration", "Windows.UI.Xaml", "Duration", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml", "DurationType", "Windows.UI.Xaml", "DurationType", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml", "GridLength", "Windows.UI.Xaml", "GridLength", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml", "GridUnitType", "Windows.UI.Xaml", "GridUnitType", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml", "Thickness", "Windows.UI.Xaml", "Thickness", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),

			new ProjectedClass("Windows.UI.Xaml.Interop", "TypeName", "System", "Type", ClrAssembly.Mscorlib, ClrAssembly.SystemRuntime, true, false),

			new ProjectedClass("Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),

			new ProjectedClass("Windows.UI.Xaml.Media", "Matrix", "Windows.UI.Xaml.Media", "Matrix", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),

			new ProjectedClass("Windows.UI.Xaml.Media.Animation", "KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml.Media.Animation", "RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),
			new ProjectedClass("Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),

			new ProjectedClass("Windows.UI.Xaml.Media.Media3D", "Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml, true, true),

			new ProjectedClass("Windows.Foundation.Numerics", "Vector2", "System.Numerics", "Vector2", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Vector3", "System.Numerics", "Vector3", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Vector4", "System.Numerics", "Vector4", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Matrix3x2", "System.Numerics", "Matrix3x2", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Matrix4x4", "System.Numerics", "Matrix4x4", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Plane", "System.Numerics", "Plane", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
			new ProjectedClass("Windows.Foundation.Numerics", "Quaternion", "System.Numerics", "Quaternion", ClrAssembly.SystemNumericsVectors, ClrAssembly.SystemNumericsVectors, true, true),
		};

		static readonly Dictionary<ClassName, ProjectedClass> winMDToCLR = new Dictionary<ClassName, ProjectedClass>();

		static WinMDHelpers() {
			foreach (var projClass in ProjectedClasses)
				winMDToCLR.Add(projClass.WinMDClass, projClass);
		}

		static AssemblyRef ToCLR(ModuleDef module, ref UTF8String ns, ref UTF8String name) {
			ProjectedClass pc;
			if (!winMDToCLR.TryGetValue(new ClassName(ns, name), out pc))
				return null;

			ns = pc.ClrClass.Namespace;
			name = pc.ClrClass.Name;
			return CreateAssembly(module, pc.ContractAssembly);
		}

		static AssemblyRef CreateAssembly(ModuleDef module, ClrAssembly clrAsm) {
			var mscorlib = module == null ? null : module.CorLibTypes.AssemblyRef;

			var asm = new AssemblyRefUser(GetName(clrAsm), contractAsmVersion, new PublicKeyToken(GetPublicKeyToken(clrAsm)), UTF8String.Empty);

			if (mscorlib != null && mscorlib.Name == mscorlibName && mscorlib.Version != invalidWinMDVersion)
				asm.Version = mscorlib.Version;
			var mod = module as ModuleDefMD;
			if (mod != null) {
				Version ver = null;
				foreach (var asmRef in mod.GetAssemblyRefs()) {
					if (asmRef.IsContentTypeWindowsRuntime)
						continue;
					if (asmRef.Name != asm.Name)
						continue;
					if (asmRef.Culture != asm.Culture)
						continue;
					if (!PublicKeyBase.TokenEquals(asmRef.PublicKeyOrToken, asm.PublicKeyOrToken))
						continue;
					if (asmRef.Version == invalidWinMDVersion)
						continue;

					if (ver == null || asmRef.Version > ver)
						ver = asmRef.Version;
				}
				if (ver != null)
					asm.Version = ver;
			}

			return asm;
		}
		static readonly Version contractAsmVersion = new Version(4, 0, 0, 0);
		static readonly Version invalidWinMDVersion = new Version(255, 255, 255, 255);
		static readonly UTF8String mscorlibName = new UTF8String("mscorlib");

		static UTF8String GetName(ClrAssembly clrAsm) {
			switch (clrAsm) {
			case ClrAssembly.Mscorlib: return clrAsmName_Mscorlib;
			case ClrAssembly.SystemNumericsVectors: return clrAsmName_SystemNumericsVectors;
			case ClrAssembly.SystemObjectModel: return clrAsmName_SystemObjectModel;
			case ClrAssembly.SystemRuntime: return clrAsmName_SystemRuntime;
			case ClrAssembly.SystemRuntimeInteropServicesWindowsRuntime: return clrAsmName_SystemRuntimeInteropServicesWindowsRuntime;
			case ClrAssembly.SystemRuntimeWindowsRuntime: return clrAsmName_SystemRuntimeWindowsRuntime;
			case ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml: return clrAsmName_SystemRuntimeWindowsRuntimeUIXaml;
			default: throw new InvalidOperationException();
			}
		}
		static readonly UTF8String clrAsmName_Mscorlib = new UTF8String("mscorlib");
		static readonly UTF8String clrAsmName_SystemNumericsVectors = new UTF8String("System.Numerics.Vectors");
		static readonly UTF8String clrAsmName_SystemObjectModel = new UTF8String("System.ObjectModel");
		static readonly UTF8String clrAsmName_SystemRuntime = new UTF8String("System.Runtime");
		static readonly UTF8String clrAsmName_SystemRuntimeInteropServicesWindowsRuntime = new UTF8String("System.Runtime.InteropServices.WindowsRuntime");
		static readonly UTF8String clrAsmName_SystemRuntimeWindowsRuntime = new UTF8String("System.Runtime.WindowsRuntime");
		static readonly UTF8String clrAsmName_SystemRuntimeWindowsRuntimeUIXaml = new UTF8String("System.Runtime.WindowsRuntime.UI.Xaml");

		static byte[] GetPublicKeyToken(ClrAssembly clrAsm) {
			switch (clrAsm) {
			case ClrAssembly.Mscorlib: return neutralPublicKey;
			case ClrAssembly.SystemNumericsVectors: return contractPublicKeyToken;
			case ClrAssembly.SystemObjectModel: return contractPublicKeyToken;
			case ClrAssembly.SystemRuntime: return contractPublicKeyToken;
			case ClrAssembly.SystemRuntimeInteropServicesWindowsRuntime: return contractPublicKeyToken;
			case ClrAssembly.SystemRuntimeWindowsRuntime: return neutralPublicKey;
			case ClrAssembly.SystemRuntimeWindowsRuntimeUIXaml: return neutralPublicKey;
			default: throw new InvalidOperationException();
			}
		}
		static readonly byte[] contractPublicKeyToken = new byte[] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A };
		static readonly byte[] neutralPublicKey = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };

		/// <summary>
		/// Converts WinMD type <paramref name="td"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="td">Type</param>
		/// <returns></returns>
		public static TypeRef ToCLR(ModuleDef module, TypeDef td) {
			bool isClrValueType;
			return ToCLR(module, td, out isClrValueType);
		}

		/// <summary>
		/// Converts WinMD type <paramref name="td"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="td">Type</param>
		/// <param name="isClrValueType"><c>true</c> if the returned type is a value type</param>
		/// <returns></returns>
		public static TypeRef ToCLR(ModuleDef module, TypeDef td, out bool isClrValueType) {
			isClrValueType = false;
			if (td == null || !td.IsWindowsRuntime)
				return null;
			var asm = td.DefinitionAssembly;
			if (asm == null || !asm.IsContentTypeWindowsRuntime)
				return null;

			ProjectedClass pc;
			if (!winMDToCLR.TryGetValue(new ClassName(td.Namespace, td.Name), out pc))
				return null;

			isClrValueType = pc.ClrClass.IsValueType;
			return new TypeRefUser(module, pc.ClrClass.Namespace, pc.ClrClass.Name, CreateAssembly(module, pc.ContractAssembly));
		}

		/// <summary>
		/// Converts WinMD type <paramref name="tr"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="tr">Type</param>
		/// <returns></returns>
		public static TypeRef ToCLR(ModuleDef module, TypeRef tr) {
			bool isClrValueType;
			return ToCLR(module, tr, out isClrValueType);
		}

		/// <summary>
		/// Converts WinMD type <paramref name="tr"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="tr">Type</param>
		/// <param name="isClrValueType"><c>true</c> if the returned type is a value type</param>
		/// <returns></returns>
		public static TypeRef ToCLR(ModuleDef module, TypeRef tr, out bool isClrValueType) {
			isClrValueType = false;
			if (tr == null)
				return null;
			var defAsm = tr.DefinitionAssembly;
			if (defAsm == null || !defAsm.IsContentTypeWindowsRuntime)
				return null;
			if (tr.DeclaringType != null)
				return null;

			ProjectedClass pc;
			if (!winMDToCLR.TryGetValue(new ClassName(tr.Namespace, tr.Name), out pc))
				return null;

			isClrValueType = pc.ClrClass.IsValueType;
			return new TypeRefUser(module, pc.ClrClass.Namespace, pc.ClrClass.Name, CreateAssembly(module, pc.ContractAssembly));
		}

		/// <summary>
		/// Converts WinMD type <paramref name="et"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="et">Type</param>
		/// <returns></returns>
		public static ExportedType ToCLR(ModuleDef module, ExportedType et) {
			if (et == null)
				return null;
			var defAsm = et.DefinitionAssembly;
			if (defAsm == null || !defAsm.IsContentTypeWindowsRuntime)
				return null;
			if (et.DeclaringType != null)
				return null;

			ProjectedClass pc;
			if (!winMDToCLR.TryGetValue(new ClassName(et.TypeNamespace, et.TypeName), out pc))
				return null;

			return new ExportedTypeUser(module, 0, pc.ClrClass.Namespace, pc.ClrClass.Name, et.Attributes, CreateAssembly(module, pc.ContractAssembly));
		}

		/// <summary>
		/// Converts WinMD type <paramref name="ts"/> to a CLR type. Returns <c>null</c>
		/// if it's not a CLR compatible WinMD type.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="ts">Type</param>
		/// <returns></returns>
		public static TypeSig ToCLR(ModuleDef module, TypeSig ts) {
			if (ts == null)
				return null;
			var et = ts.ElementType;
			if (et != ElementType.Class && et != ElementType.ValueType)
				return null;

			var tdr = ((ClassOrValueTypeSig)ts).TypeDefOrRef;

			TypeDef td;
			TypeRef tr, newTr;
			bool isClrValueType;
			if ((td = tdr as TypeDef) != null) {
				newTr = ToCLR(module, td, out isClrValueType);
				if (newTr == null)
					return null;
			}
			else if ((tr = tdr as TypeRef) != null) {
				newTr = ToCLR(module, tr, out isClrValueType);
				if (newTr == null)
					return null;
			}
			else
				return null;

			return isClrValueType ?
				(TypeSig)new ValueTypeSig(newTr) :
				new ClassSig(newTr);
		}

		/// <summary>
		/// Converts WinMD member reference <paramref name="mr"/> to a CLR member reference. Returns
		/// <c>null</c> if it's not a CLR compatible WinMD member reference.
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="mr">Member reference</param>
		/// <returns></returns>
		public static MemberRef ToCLR(ModuleDef module, MemberRef mr) {
			// See WinMDAdapter::CheckIfMethodImplImplementsARedirectedInterface
			// in coreclr: md/winmd/adapter.cpp
			if (mr == null)
				return null;
			if (mr.Name != CloseName)
				return null;

			var msig = mr.MethodSig;
			if (msig == null)
				return null;

			var cl = mr.Class;
			IMemberRefParent newCl;
			TypeRef tr;
			TypeSpec ts;
			if ((tr = cl as TypeRef) != null) {
				var newTr = ToCLR(module, tr);
				if (newTr == null || !IsIDisposable(newTr))
					return null;

				newCl = newTr;
			}
			else if ((ts = cl as TypeSpec) != null) {
				var gis = ts.TypeSig as GenericInstSig;
				if (gis == null || !(gis.GenericType is ClassSig))
					return null;
				tr = gis.GenericType.TypeRef;
				if (tr == null)
					return null;

				bool isClrValueType;
				var newTr = ToCLR(module, tr, out isClrValueType);
				if (newTr == null || !IsIDisposable(newTr))
					return null;

				newCl = new TypeSpecUser(new GenericInstSig(isClrValueType ?
								(ClassOrValueTypeSig)new ValueTypeSig(newTr) :
								new ClassSig(newTr), gis.GenericArguments));
			}
			else
				return null;

			return new MemberRefUser(mr.Module, DisposeName, msig, newCl);
		}
		static readonly UTF8String CloseName = new UTF8String("Close");
		static readonly UTF8String DisposeName = new UTF8String("Dispose");

		static bool IsIDisposable(TypeRef tr) {
			return tr.Name == IDisposableName && tr.Namespace == IDisposableNamespace;
		}
		static readonly UTF8String IDisposableNamespace = new UTF8String("System");
		static readonly UTF8String IDisposableName = new UTF8String("IDisposable");

		/// <summary>
		/// Converts WinMD method <paramref name="md"/> to a CLR member reference. Returns
		/// <c>null</c> if it's not a CLR compatible WinMD method
		/// </summary>
		/// <param name="module">Owner module or <c>null</c></param>
		/// <param name="md">Method</param>
		/// <returns></returns>
		public static MemberRef ToCLR(ModuleDef module, MethodDef md) {
			if (md == null)
				return null;
			if (md.Name != CloseName)
				return null;
			var declType = md.DeclaringType;
			if (declType == null)
				return null;

			var tr = ToCLR(module, declType);
			if (tr == null || !IsIDisposable(tr))
				return null;

			return new MemberRefUser(md.Module, DisposeName, md.MethodSig, tr);
		}
	}
}
