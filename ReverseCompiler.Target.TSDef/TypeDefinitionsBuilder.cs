using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Il2CppToolkit.Common.Errors;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Il2CppToolkit.Target.TSDef;

public class TypeDefinitionsBuilder
{
	protected enum TypeDefinitionState
	{
		Created,
		Existing,
		Excluded
	}

	private readonly ICompileContext Context;
	private readonly IReadOnlyList<Func<TypeDescriptor, ArtifactSpecs.TypeSelectorResult>> TypeSelectors;
	private Queue<Il2CppTypeDefinition> TypeDefinitionQueue = new();
	private readonly HashSet<Il2CppTypeDefinition> VisitedTypes = new();
	private readonly HashSet<string> UsedTypeNames = new();
	private readonly Dictionary<Il2CppTypeDefinition, TSTypeDefinition> TypeDefinitions = new();
	private readonly IReadOnlyDictionary<Il2CppTypeDefinition, ArtifactSpecs.TypeSelectorResult> IncludedDescriptors;
	private readonly Dictionary<Il2CppTypeEnum, TSValueType> BuiltInTypes = new();
	private readonly Queue<TSTypeDefinition> TypesToEmit = new();
	private readonly TSExternalReference Record = new("Record");
	private readonly TSExternalReference Array = new("Array");
	private readonly TSExternalReference Nullable = new("Nullable");
	private readonly TSExternalReference Date = new("Date");
	private readonly TSExternalReference Number = new("number");

	private Il2Cpp Il2Cpp => Context.Model.Il2Cpp;
	private Metadata Metadata => Context.Model.Metadata;


	#region Progress
	private int Completed = 0;
	private int Total = 0;
	private int UpdateCounter = 0;
	private string? CurrentAction;
	public event EventHandler<ProgressUpdatedEventArgs>? ProgressUpdated;

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
	#endregion

	public TypeDefinitionsBuilder(ICompileContext context)
	{
		Context = context;
		TypeSelectors = Context.Artifacts.Get(ArtifactSpecs.TypeSelectors);
		IncludedDescriptors = FilterTypes(TypeSelectors);

		AddBuiltInTypes();
	}

	protected IReadOnlyDictionary<Il2CppTypeDefinition, ArtifactSpecs.TypeSelectorResult> FilterTypes(IReadOnlyList<Func<TypeDescriptor, ArtifactSpecs.TypeSelectorResult>> typeSelectors)
	{
		return Context.Model.TypeDescriptors.GroupBy(
			descriptor => descriptor.TypeDef,
			descriptor => typeSelectors.Select(selector => selector(descriptor)).Aggregate((a, b) => a | b))
			.ToDictionary(group => group.Key, group => group.Aggregate((a, b) => a | b));
	}

	public string Generate()
	{
		StringBuilder sb = new();
		while (TypesToEmit.TryDequeue(out TSTypeDefinition? tsDef))
		{
			CompleteWork();
			Context.Logger?.LogInfo($"[{tsDef}] Emitting");
			tsDef.Emit(sb);
			sb.AppendLine();
		}
		return sb.ToString();
	}

	internal void ProcessDescriptors()
	{
		foreach (var kvp in IncludedDescriptors)
		{
			if (!kvp.Value.HasFlag(ArtifactSpecs.TypeSelectorResult.Include))
				continue;
			IncludeTypeDefinition(kvp.Key);
		}
	}

	public void IncludeTypeDefinition(Il2CppTypeDefinition cppTypeDef)
	{
		if (TryUseTypeDefinition(cppTypeDef, out TSTypeDefinition? typeDef) != TypeDefinitionState.Excluded && typeDef != null)
			TypeDefinitionQueue.Enqueue(cppTypeDef);
	}

	public void BuildDefinitionQueue()
	{
		Queue<Il2CppTypeDefinition> typesToBuild = new();

		do
		{
			SetAction("Processing");
			do
			{
				Queue<Il2CppTypeDefinition> currentQueue = TypeDefinitionQueue;
				TypeDefinitionQueue = new();
				while (currentQueue.TryDequeue(out Il2CppTypeDefinition? cppTypeDef))
				{
					CompleteWork();
					if (TryUseTypeDefinition(cppTypeDef, out TSTypeDefinition? tsDef) == TypeDefinitionState.Excluded)
						continue;

					if (tsDef == null)
						throw new StructuredException<CompilerError>(CompilerError.InternalError, "Type definition is null");

					Context.Logger?.LogInfo($"[{tsDef}] Dequeued");

					Context.Logger?.LogInfo($"[{tsDef}] Init Type");
					InitializeTypeDefinition(cppTypeDef, tsDef);

					Context.Logger?.LogInfo($"[{tsDef}] Marked->Build");
					if (VisitedTypes.Add(cppTypeDef))
						typesToBuild.Enqueue(cppTypeDef);
					AddWork();
				}
			}
			while (TypeDefinitionQueue.Count > 0);

			Context.Logger?.LogInfo($"Building marked types");
			SetAction("Compiling");
			while (typesToBuild.TryDequeue(out Il2CppTypeDefinition? cppTypeDef))
			{
				CompleteWork();
				if (TryUseTypeDefinition(cppTypeDef, out TSTypeDefinition? tsDef) == TypeDefinitionState.Excluded || tsDef == null)
					continue;
				Context.Logger?.LogInfo($"[{tsDef}] Building");

				// skip declaring type

				if (tsDef is TSInterface tsInterface)
				{
					DefineProperties(cppTypeDef, tsInterface);
					DefineFields(cppTypeDef, tsInterface);
				}
				else if (tsDef is TSEnum tsEnum)
				{
					DefineConstants(cppTypeDef, tsEnum);
				}
			}
		} while (TypeDefinitionQueue.Count > 0 || typesToBuild.Count > 0);
	}

	protected TypeDefinitionState EnsureTypeDefinition(Il2CppTypeDefinition cppTypeDef, out TSTypeDefinition tsDef)
	{
		if (TypeDefinitions.TryGetValue(cppTypeDef, out tsDef))
			return TypeDefinitionState.Existing;

		tsDef = CreateTypeDefinition(cppTypeDef);
		if (TypeDefinitions.TryAdd(cppTypeDef, tsDef))
		{
			TypeDefinitionQueue.Enqueue(cppTypeDef);
			return TypeDefinitionState.Created;
		}
		return TypeDefinitionState.Existing;
	}

	protected TSTypeDefinition CreateTypeDefinition(Il2CppTypeDefinition cppTypeDef)
	{
		if (TypeDefinitions.ContainsKey(cppTypeDef))
			throw new Exception("Type already defined");

		string typeName = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
		int nName = 0;
		while (UsedTypeNames.Contains(typeName))
			typeName = $"{typeName}_{++nName}";
		UsedTypeNames.Add(typeName);

		TSTypeDefinition tsDef;
		if (IncludedDescriptors.TryGetValue(cppTypeDef, out var typeSelectorResult) && typeSelectorResult.HasFlag(ArtifactSpecs.TypeSelectorResult.Nominal))
		{
			tsDef = new TSNominalType(typeName);
		}
		else
		{
			TSTypeDefinition? parentType = null;
			if (cppTypeDef.parentIndex != -1 && cppTypeDef.parentIndex < Metadata.typeDefs.Length)
			{
				TryUseTypeDefinition(Metadata.typeDefs[cppTypeDef.parentIndex], out parentType);
			}

			if (cppTypeDef.IsEnum)
			{
				tsDef = new TSEnum(typeName);
			}
			else
			{
				tsDef = new TSInterface(typeName) { Parent = parentType as TSInterface };
			}
		}
		TypesToEmit.Enqueue(tsDef);
		return tsDef;
	}

	protected TypeDefinitionState TryUseTypeDefinition(Il2CppTypeDefinition cppTypeDef, out TSTypeDefinition? tsDef)
	{
		string typeNamespace = Metadata.GetStringFromIndex(cppTypeDef.namespaceIndex);
		if (!IncludedDescriptors.TryGetValue(cppTypeDef, out var typeSelectorResult) || typeSelectorResult.HasFlag(ArtifactSpecs.TypeSelectorResult.Exclude))
		{
			if (Regex.IsMatch(typeNamespace, @"^System(\.|$)") && !typeSelectorResult.HasFlag(ArtifactSpecs.TypeSelectorResult.Nominal))
			{
				tsDef = null;
				return TypeDefinitionState.Excluded;
			}

			string typeName = Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
			Metadata.GetStringFromIndex(cppTypeDef.nameIndex);
			Context.Logger?.LogInfo($"Excluding '{typeName}' based on exclusion rule");
			tsDef = null;
			return TypeDefinitionState.Excluded;
		}

		return EnsureTypeDefinition(cppTypeDef, out tsDef);
	}

	private T AssertDefined<T>(T? value)
	{
		if (value == null)
			throw new StructuredException<CompilerError>(CompilerError.InternalError, "Value is null");
		return value;
	}

	protected bool TryUseTypeReference(Il2CppTypeDefinition cppTypeDef, [NotNullWhen(true)] out TSTypeReference? tsRef)
	{
		if (!IncludedDescriptors.TryGetValue(cppTypeDef, out var typeSelectorResult) || !typeSelectorResult.HasFlag(ArtifactSpecs.TypeSelectorResult.Nominal))
		{
			if (Context.Model.TryGetTypeDescriptor(cppTypeDef, out TypeDescriptor? typeDescriptor)
				&& TryReplaceType(typeDescriptor, out tsRef))
			{
				return true;
			}
		}

		if (TryUseTypeDefinition(cppTypeDef, out TSTypeDefinition? tsDef) == TypeDefinitionState.Excluded)
		{
			tsRef = null;
			return false;
		}
		tsRef = AssertDefined(tsDef).AsReference();
		return true;
	}

	protected bool TryUseTypeReference(Il2CppType cppType, [NotNullWhen(true)] out TSTypeReference? tsRef)
	{
		if (BuiltInTypes.TryGetValue(cppType.type, out TSValueType? valueType))
		{
			tsRef = valueType.AsReference();
			return true;
		}

		switch (cppType.type)
		{
			case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
			case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
				{
					Il2CppTypeDefinition cppTypeDef = Context.Model.GetTypeDefinitionFromIl2CppType(cppType);
					return TryUseTypeReference(cppTypeDef, out tsRef);
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
				{
					Il2CppArrayType cppArrayType = Context.Model.Il2Cpp.MapVATR<Il2CppArrayType>(cppType.data.array);
					Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppArrayType.etype);
					if (!TryUseTypeReference(cppElementType, out TSTypeReference? tsArrayTypeRef))
					{
						tsRef = null;
						return false;
					}
					// TODO: ccpArrayType.rank?
					tsRef = new TSArrayTypeReference(tsArrayTypeRef);
					return true;
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
				{
					Il2CppGenericClass cppGenericClass = Context.Model.Il2Cpp.MapVATR<Il2CppGenericClass>(cppType.data.generic_class);
					Il2CppTypeDefinition cppTypeDef = Context.Model.GetGenericClassTypeDefinition(cppGenericClass);

					if (!TryUseTypeReference(cppTypeDef, out TSTypeReference? tsGeneric))
					{
						tsRef = null;
						return false;
					}

					Il2CppGenericInst cppGenericInst = Context.Model.Il2Cpp.MapVATR<Il2CppGenericInst>(cppGenericClass.context.class_inst);
					ulong[] pointers = Context.Model.Il2Cpp.MapVATR<ulong>(cppGenericInst.type_argv, cppGenericInst.type_argc);
					List<TSTypeReference> genericArguments = new();
					foreach (ulong pointer in pointers)
					{
						Il2CppType cppArgType = Context.Model.Il2Cpp.GetIl2CppType(pointer);
						if (!TryUseTypeReference(cppArgType, out TSTypeReference? tsArgRef))
						{
							tsRef = null;
							return false;
						}
						genericArguments.Add(tsArgRef);
					}
					tsRef = new TSGenericInstance(tsGeneric, genericArguments);
					return true;
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
				{
					Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppType.data.type);
					if (!TryUseTypeReference(cppElementType, out TSTypeReference? tsArrayTypeRef))
					{
						tsRef = null;
						return false;
					}
					tsRef = new TSArrayTypeReference(tsArrayTypeRef);
					return true;
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
				{
					// TODO?
					tsRef = null;
					return false;
					// return memberReference switch
					// {
					// 	MethodDefinition methodDefinition => CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), methodDefinition.DeclaringType),
					// 	TypeDefinition typeDefinition => CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), typeDefinition),
					// 	_ => throw new NotSupportedException()
					// };
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
				{
					// TODO?
					tsRef = null;
					return false;
					// if (memberReference is not MethodDefinition methodDefinition)
					// 	throw new NotSupportedException();
					// return CreateGenericParameter(Context.Model.GetGenericParameterFromIl2CppType(cppType), methodDefinition);
				}
			case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
				{
					Il2CppType cppElementType = Context.Model.Il2Cpp.GetIl2CppType(cppType.data.type);
					return TryUseTypeReference(cppElementType, out tsRef);
				}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void DefineConstants(Il2CppTypeDefinition cppTypeDef, TSEnum tsEnum)
	{
		int fieldEnd = cppTypeDef.fieldStart + cppTypeDef.field_count;
		for (var i = cppTypeDef.fieldStart; i < fieldEnd; ++i)
		{
			Il2CppFieldDefinition cppFieldDef = Metadata.fieldDefs[i];
			Il2CppType cppFieldType = Il2Cpp.Types[cppFieldDef.typeIndex];
			FieldAttributes fieldAttrs = (FieldAttributes)cppFieldType.attrs;
			string name = Metadata.GetStringFromIndex(cppFieldDef.nameIndex);
			if ((fieldAttrs.HasFlag(FieldAttributes.Literal) || fieldAttrs.HasFlag(FieldAttributes.HasDefault))
				&& Metadata.GetFieldDefaultValueFromIndex(i, out Il2CppFieldDefaultValue cppDefaultValue)
				&& cppDefaultValue.dataIndex != -1
				&& Context.Model.TryGetDefaultValue(cppDefaultValue, out object defaultValue)
				&& defaultValue != null)
			{
				tsEnum.Values.Add(new(name, defaultValue.ToString()));
			}
			// TODO assert otherwise?
		}
	}

	private void DefineProperties(Il2CppTypeDefinition cppTypeDef, TSInterface tsInterface)
	{
		int propertyEnd = cppTypeDef.propertyStart + cppTypeDef.property_count;
		for (var i = cppTypeDef.propertyStart; i < propertyEnd; ++i)
		{
			Il2CppPropertyDefinition cppPropertyDef = Metadata.propertyDefs[i];
			if (cppPropertyDef.get < 0)
				continue;

			string name = Metadata.GetStringFromIndex(cppPropertyDef.nameIndex);
			Il2CppMethodDefinition cppMethodDef = Metadata.methodDefs[cppTypeDef.methodStart + cppPropertyDef.get];
			Il2CppType cppReturnType = Il2Cpp.Types[cppMethodDef.returnType];
			MethodAttributes attrs = (MethodAttributes)cppMethodDef.flags;

			if (!attrs.HasFlag(MethodAttributes.Public) || (attrs & MethodAttributes.Static) != 0)
				continue;

			if (!TryUseTypeReference(cppReturnType, out TSTypeReference? tsRef))
				continue;

			tsInterface.Fields.Add(new(name, tsRef));
		}
	}

	private void DefineFields(Il2CppTypeDefinition cppTypeDef, TSInterface tsInterface)
	{
		int fieldEnd = cppTypeDef.fieldStart + cppTypeDef.field_count;
		for (var i = cppTypeDef.fieldStart; i < fieldEnd; ++i)
		{

			Il2CppFieldDefinition cppFieldDef = Metadata.fieldDefs[i];
			Il2CppType cppFieldType = Il2Cpp.Types[cppFieldDef.typeIndex];
			string fieldName = Metadata.GetStringFromIndex(cppFieldDef.nameIndex);
			FieldAttributes fieldAttrs = (FieldAttributes)cppFieldType.attrs;
			if (!fieldAttrs.HasFlag(FieldAttributes.Public)
				|| (fieldAttrs & (FieldAttributes.Static | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName | FieldAttributes.Literal)) != 0)
				continue;

			if (!TryUseTypeReference(cppFieldType, out TSTypeReference? tsRef))
				continue;

			tsInterface.Fields.Add(new(fieldName, tsRef));
		}
	}

	private void InitializeTypeDefinition(Il2CppTypeDefinition cppTypeDef, TSType tsType)
	{
		if (tsType is not TSInterface tsInterface)
			return;

		// skip nested types
		// skip interface implementations

		// genericParameters
		if (cppTypeDef.genericContainerIndex >= 0)
		{
			var genericContainer = Metadata.genericContainers[cppTypeDef.genericContainerIndex];
			for (int i = 0; i < genericContainer.type_argc; i++)
			{
				var genericParameterIndex = genericContainer.genericParameterStart + i;
				var param = Metadata.genericParameters[genericParameterIndex];
				string genericName = Context.Model.Metadata.GetStringFromIndex(param.nameIndex);
				tsInterface.TypeParameters.Add(genericName);
				// TODO: add constraints
			}
		}
	}

	public bool TryReplaceType(TypeDescriptor td, [NotNullWhen(true)] out TSTypeReference? typeRef)
	{
		if (td.Name == "System.Collections.Generic.Dictionary`2" ||
			td.Name == "System.Collections.Generic.IReadOnlyDictionary`2")
		{
			typeRef = Record;
			return true;
		}
		if (td.Name == "System.Nullable`1")
		{
			typeRef = Nullable;
			return true;
		}
		if (td.Name == "System.DateTime")
		{
			typeRef = Date;
			return true;
		}
		if (td.Name == "System.Collections.Generic.List`1" ||
			td.Name == "System.Collections.Generic.Queue`1" ||
			td.Name == "System.Collections.Generic.IReadOnlyList`1" ||
			td.Name == "System.Collections.Generic.IEnumerable`1" ||
			td.Name == "System.Collections.Generic.HashSet`1")
		{
			typeRef = Array;
			return true;
		}
		typeRef = null;
		return false;
	}

	private void AddBuiltInTypes()
	{
		TSValueType str = new("string");
		TSValueType num = new("number");
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_OBJECT, new("Object"));
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_VOID, new("void"));
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN, new("boolean"));
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_CHAR, str);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I1, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U1, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I2, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U2, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I4, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U4, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_I8, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_U8, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R4, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_R8, num);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_STRING, str);
		BuiltInTypes.Add(Il2CppTypeEnum.IL2CPP_TYPE_TYPEDBYREF, new("unknown"));
	}

}