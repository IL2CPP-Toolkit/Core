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
		private readonly HashSet<Il2CppTypeDefinition> EnqueuedTypes = new();
		private Queue<Il2CppTypeDefinition> TypeDefinitionQueue = new();

		public void Build()
		{
			BuildDefinitionQueue();
		}

		private void BuildDefinitionQueue()
		{
			Queue<Il2CppTypeDefinition> typesToBuild = new();
			do
			{
				do
				{
					Queue<Il2CppTypeDefinition> currentQueue = TypeDefinitionQueue;
					TypeDefinitionQueue = new();
					while (currentQueue.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
					{
						TypeReference typeRef = UseTypeDefinition(cppTypeDef);
						if (typeRef == null)
							continue;

						Context.Logger?.LogInfo($"[{typeRef.FullName}] Dequeued");
						if (typeRef is not TypeDefinition typeDef)
							continue;

						if (!cppTypeDef.IsEnum)
						{
							Context.Logger?.LogInfo($"[{typeRef.FullName}] Init Type");
							InitializeTypeDefinition(cppTypeDef, typeDef);
							DefineConstructors(typeDef);
						}
						Context.Logger?.LogInfo($"[{typeRef.FullName}] Marked->Build");
						typesToBuild.Enqueue(cppTypeDef);
					}
				}
				while (TypeDefinitionQueue.Count > 0);

				Context.Logger?.LogInfo($"Building marked types");
				while (typesToBuild.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
				{
					TypeReference typeRef = UseTypeDefinition(cppTypeDef);
					if (typeRef == null)
						continue;
					Context.Logger?.LogInfo($"[{typeRef.FullName}] Building");
					if (typeRef is not TypeDefinition typeDef)
						continue;

					// declaring type
					if (cppTypeDef.declaringTypeIndex >= 0)
					{
						Il2CppTypeDefinition declaringType = Context.Model.GetTypeDefinitionFromIl2CppType(Il2Cpp.Types[cppTypeDef.declaringTypeIndex]);
						Debug.Assert(declaringType != null);
						// trigger build for containing type
						if (declaringType != null)
							UseTypeDefinition(declaringType);
					}

					Context.Logger?.LogInfo($"[{typeRef.FullName}] Add TypeInfo");
					using TypeInfoBuilder typeInfo = new(typeDef, Module, this);
					DefineMethods(cppTypeDef, typeDef);
					DefineFields(cppTypeDef, typeDef, typeInfo);
				}
			} while (TypeDefinitionQueue.Count > 0 || typesToBuild.Count > 0);
		}
	}
}