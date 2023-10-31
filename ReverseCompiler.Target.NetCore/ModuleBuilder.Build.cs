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
		private int Completed = 0;
		private int Total = 0;
		private int UpdateCounter = 0;
		private string CurrentAction;
		public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

		private void AddWork(int count = 1)
		{
			++Total;
			OnWorkUpdated();
		}
		private void CompleteWork(int count = 1)
		{
			++Completed;
			OnWorkUpdated();
		}
		private void SetAction(string actionName)
		{
			if (CurrentAction == actionName)
				return;
			CurrentAction = actionName;
			UpdateCounter = -1; // force update when action changes
			OnWorkUpdated();
		}
		private void OnWorkUpdated()
		{
			if (++UpdateCounter % 50 != 0 || Total < 10)
				return;
			ProgressUpdated?.Invoke(this, new() { Total = Math.Max(Total, 1), Completed = Completed, DisplayName = CurrentAction });
		}

		public void Build()
		{
			BuildDefinitionQueue();
		}

		private void BuildDefinitionQueue()
		{
			Queue<Il2CppTypeDefinition> typesToBuild = new();
			do
			{
				SetAction("Processing");
				do
				{
					Queue<Il2CppTypeDefinition> currentQueue = TypeDefinitionQueue;
					TypeDefinitionQueue = new();
					while (currentQueue.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
					{
						CompleteWork();
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
						else
						{
							typeDef.BaseType = ImportReference(typeof(Enum));
						}
						Context.Logger?.LogInfo($"[{typeRef.FullName}] Marked->Build");
						typesToBuild.Enqueue(cppTypeDef);
						AddWork();
					}
				}
				while (TypeDefinitionQueue.Count > 0);

				Context.Logger?.LogInfo($"Building marked types");
				SetAction("Compiling");
				while (typesToBuild.TryDequeue(out Il2CppTypeDefinition cppTypeDef))
				{
					CompleteWork();
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

					Context.Logger?.LogInfo($"[{typeRef.FullName}] Fields");
					DefineFields(cppTypeDef, typeDef);
					Context.Logger?.LogInfo($"[{typeRef.FullName}] Methods");
					DefineMethods(cppTypeDef, typeDef);
					Context.Logger?.LogInfo($"[{typeRef.FullName}] Properties");
					DefineProperties(cppTypeDef, typeDef);
				}
			} while (TypeDefinitionQueue.Count > 0 || typesToBuild.Count > 0);
		}
	}
}