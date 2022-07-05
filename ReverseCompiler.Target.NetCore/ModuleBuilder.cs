using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		const MethodAttributes kRTObjGetterAttrs = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.NewSlot;
		const MethodAttributes kCtorAttrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		private readonly ICompileContext Context;
		private readonly AssemblyDefinition AssemblyDefinition;
		private readonly Dictionary<Il2CppTypeDefinition, TypeDefinition> TypeDefinitions = new();
		private readonly Dictionary<Il2CppGenericParameter, GenericParameter> GenericParameters = new();
		private readonly Dictionary<Il2CppTypeEnum, TypeReference> BuiltInTypes = new();
		private readonly Dictionary<Type, TypeReference> m_importedTypes = new();
		private readonly Dictionary<int, MethodDefinition> m_methodDefs = new();

		private Il2Cpp Il2Cpp => Context.Model.Il2Cpp;
		private Metadata Metadata => Context.Model.Metadata;

		private readonly TypeReference RuntimeObjectTypeRef;
		private readonly TypeReference IRuntimeObjectTypeRef;
		private readonly TypeReference IMemorySourceTypeRef;
		private readonly MethodReference ObjectCtorMethodRef;
		private ModuleDefinition Module => AssemblyDefinition.MainModule;
		private readonly AssemblyNameReference SystemRuntimeRef;

		public ModuleBuilder(ICompileContext context, AssemblyDefinition assemblyDefinition)
		{
			Context = context;
			AssemblyDefinition = assemblyDefinition;
			Module.AssemblyReferences.Add(new AssemblyNameReference("Il2CppToolkit.Runtime", new Version(1, 0, 0, 0)));
#if NET5_0_OR_GREATER
			SystemRuntimeRef = new AssemblyNameReference("System.Runtime", new Version(5, 0, 0, 0))
			{
				// b03f5f7f11d50a3a
				PublicKeyToken = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }
			};
			Module.AssemblyReferences.Add(SystemRuntimeRef);
#endif
			AddBuiltInTypes(Module);
			RuntimeObjectTypeRef = ImportReference(typeof(RuntimeObject));
			IRuntimeObjectTypeRef = ImportReference(typeof(IRuntimeObject));
			IMemorySourceTypeRef = ImportReference(typeof(IMemorySource));
			ObjectCtorMethodRef = ImportReference(typeof(object)).GetConstructor();
		}

	}
}