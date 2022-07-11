using System;
using System.Collections.Generic;
using Il2CppToolkit.Model;
using Il2CppToolkit.Runtime;
using Mono.Cecil;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public partial class ModuleBuilder
	{
		private readonly ICompileContext Context;
		private readonly AssemblyDefinition AssemblyDefinition;
		private readonly Dictionary<int, MethodDefinition> MethodDefs = new();

		private Il2Cpp Il2Cpp => Context.Model.Il2Cpp;
		private Metadata Metadata => Context.Model.Metadata;

		private readonly TypeReference RuntimeObjectTypeRef;
		private readonly TypeReference IRuntimeObjectTypeRef;
		private readonly TypeReference IMemorySourceTypeRef;
		private readonly MethodReference ObjectCtorMethodRef;
		private ModuleDefinition Module => AssemblyDefinition.MainModule;
		private readonly AssemblyNameReference SystemRuntimeRef;
		private readonly bool IncludeCompilerGeneratedTypes;

		public ModuleBuilder(ICompileContext context, AssemblyDefinition assemblyDefinition, bool includeCompilerGeneratedTypes)
		{
			Context = context;
			AssemblyDefinition = assemblyDefinition;
			Module.AssemblyReferences.Add(new AssemblyNameReference("Il2CppToolkit.Runtime", new Version(2, 0, 0, 0)));
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
			IncludeCompilerGeneratedTypes = includeCompilerGeneratedTypes;
		}

	}
}