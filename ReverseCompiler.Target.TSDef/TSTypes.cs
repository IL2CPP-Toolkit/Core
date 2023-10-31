using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppToolkit.Target.TSDef;

public abstract class TSType
{
	public TSType(string name)
	{
		Name = name;
	}
	public string Name { get; set; }
	public override string ToString()
	{
		return Name;
	}

	public abstract void Emit(StringBuilder sb);
}

public class TSTypeReference : TSType
{
	public TSTypeReference(TSTypeDefinition tsType)
		: base(tsType.Name)
	{
		Definition = tsType.AsReference();
	}

	public TSTypeReference(TSTypeReference tsTypeRef)
		: base(tsTypeRef.Name)
	{
		Definition = tsTypeRef;
	}

	public TSTypeReference? Definition { get; }

	public override void Emit(StringBuilder sb)
	{
		sb.Append(Name);
	}
}

public class TSArrayTypeReference : TSTypeReference
{
	public TSArrayTypeReference(TSTypeReference tsType)
		: base(tsType)
	{ }

	override public string ToString()
	{
		return $"{base.ToString()}[]";
	}

	public override void Emit(StringBuilder sb)
	{
		sb.Append(ToString());
	}
}

public abstract class TSTypeDefinition : TSType
{
	protected TSTypeDefinition(string name)
		: base(name)
	{
		_ref = new(this);
	}

	private readonly TSTypeReference _ref;
	public TSTypeReference AsReference()
	{
		return _ref;
	}
}

public class TSValueType : TSTypeDefinition
{
	public TSValueType(string name)
		: base(name)
	{ }

	public override void Emit(StringBuilder sb)
	{
		sb.Append(Name);
	}
}

public class TSInterface : TSTypeDefinition
{
	public TSInterface(string name)
		: base(name)
	{
		Fields = new();
		TypeParameters = new();
	}
	public List<string> TypeParameters { get; set; }
	public TSInterface? Parent { get; set; }
	public List<TSField> Fields { get; set; }

	public override void Emit(StringBuilder sb)
	{
		sb.Append($"export interface {Name}");
		if (Parent != null)
		{
			sb.Append(" extends ");
			Parent.AsReference().Emit(sb);
		}
		sb.AppendLine("{");
		foreach (TSField field in Fields)
		{
			sb.Append($"  {field.Name}: ");
			field.Type.Emit(sb);
			sb.AppendLine(";");
		}
		sb.AppendLine("}");
	}
}

public class TSGenericInstance : TSTypeReference
{
	public TSGenericInstance(TSInterface genericType, List<TSTypeReference> genericArguments)
		: base(genericType)
	{
		GenericType = genericType;
		GenericArguments = genericArguments;
	}

	public TSInterface GenericType { get; }
	public List<TSTypeReference> GenericArguments { get; }
	override public string ToString()
	{
		return $"{GenericType.Name}<{string.Join(", ", GenericArguments)}>";
	}

	public override void Emit(StringBuilder sb)
	{
		sb.Append(ToString());
	}
}

public class TSEnum : TSTypeDefinition
{
	public TSEnum(string name)
		: base(name)
	{
		Values = new();
	}
	public List<TSConstant> Values { get; set; }

	public override void Emit(StringBuilder sb)
	{
		sb.AppendLine($"export enum {Name} {{");
		foreach (TSConstant value in Values)
		{
			sb.AppendLine($"  {value.Name} = {value.Value},");
		}
		sb.AppendLine("}");
	}
}

public class TSConstant
{
	public TSConstant(string name, string value)
	{
		Name = name;
		Value = value;
	}
	public string Name { get; set; }
	public string Value { get; set; }
}

public class TSField
{
	public TSField(string name, TSTypeReference type)
	{
		Name = name;
		Type = type;
	}
	public string Name { get; set; }
	public TSType Type { get; set; }
}