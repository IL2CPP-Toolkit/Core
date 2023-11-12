using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppToolkit.Target.TSDef;

public class TSTypeName
{
	public TSTypeName(string name)
	{
		Name = name.Split('`').First();
	}
	public string Name { get; set; }
	public override string ToString()
	{
		return Name;
	}

	public void Emit(StringBuilder sb)
	{
		sb.Append(Name);
	}

	public static implicit operator TSTypeName(string name) => new(name);
}

public abstract class TSType
{
	public TSType(TSTypeName name)
	{
		Name = name;
	}
	public TSTypeName Name { get; set; }
	public override string ToString()
	{
		return Name.ToString();
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

	protected TSTypeReference(TSTypeName name)
		: base(name)
	{
	}

	public TSTypeReference? Definition { get; }

	public override void Emit(StringBuilder sb)
	{
		sb.Append(Name);
	}
}

public class TSExternalReference : TSTypeReference
{
	public TSExternalReference(TSTypeName name)
		: base(name)
	{ }
}

public class TSTemplateReference : TSTypeReference
{
	public TSTypeReference[] Parameters { get; }
	public TSTemplateReference(TSTypeName name, params TSTypeReference[] parameters)
		: base(name)
	{
		Parameters = parameters;
	}

	public override string ToString()
	{
		return $"{Name}<{string.Join<TSTypeReference>(", ", Parameters)}>";
	}

	public override void Emit(StringBuilder sb)
	{
		sb.Append(Name);
		sb.Append('<');
		bool first = true;
		foreach (TSTypeReference p in Parameters)
		{
			if (!first)
				sb.Append(", ");
			first = false;
			p.Emit(sb);
		}
		sb.Append('>');
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

public class TSDictionaryTypeReference : TSTemplateReference
{
	public TSTypeReference KeyType { get; }
	public TSTypeReference ValueType { get; }
	public TSDictionaryTypeReference(TSTypeReference keyType, TSTypeReference valueType)
		: base("Record", keyType, valueType)
	{
		KeyType = keyType;
		ValueType = valueType;
	}
}

public abstract class TSTypeDefinition : TSType
{
	protected TSTypeDefinition(TSTypeName name)
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
	public TSInterface(TSTypeName name)
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
		if (TypeParameters.Count > 0)
		{
			sb.Append('<');
			bool first = true;
			foreach(string param in TypeParameters)
			{
				if (!first)
				{
					sb.Append(", ");
				}
				first = true;
				sb.Append(param);
			}
			sb.Append('>');
		}
		if (Parent != null)
		{
			sb.Append(" extends");
			Parent.AsReference().Emit(sb);
		}
		sb.AppendLine(" {");
		foreach (TSField field in Fields)
		{
			field.Emit(sb);
		}
		sb.AppendLine("}");
	}
}

public class TSGenericInstance : TSTypeReference
{
	public TSGenericInstance(TSTypeReference genericType, List<TSTypeReference> genericArguments)
		: base(genericType)
	{
		GenericType = genericType;
		GenericArguments = genericArguments;
	}

	public TSTypeReference GenericType { get; }
	public List<TSTypeReference> GenericArguments { get; }

	override public string ToString()
	{
		return $"{GenericType.Name}<{string.Join(", ", GenericArguments)}>";
	}
	
	public override void Emit(StringBuilder sb)
	{
		sb.Append(Name);
		sb.Append('<');
		bool first = true;
		foreach (TSTypeReference p in GenericArguments)
		{
			if (!first)
				sb.Append(", ");
			first = false;
			p.Emit(sb);
		}
		sb.Append('>');
	}
}

public class TSEnum : TSTypeDefinition
{
	public TSEnum(TSTypeName name)
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
	public TSTypeReference Type { get; set; }

	public void Emit(StringBuilder sb)
	{
		string localName = Name.Split('.').Last();
		TSTypeReference fieldType = Type;
		sb.Append($"  {localName}");
		// nullable?
		if (fieldType is TSGenericInstance genericInst && fieldType.Name.Name == "Nullable")
		{
			sb.Append('?');
			fieldType = genericInst.GenericArguments[0];
		}
		sb.Append($": ");
		fieldType.Emit(sb);
		sb.AppendLine(";");
	}
}