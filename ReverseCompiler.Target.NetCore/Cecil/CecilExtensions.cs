using System;

namespace Mono.Cecil
{
	public static class CecilExtensions
	{
		public static string GetSafeName(this TypeReference self)
		{
			if (self.HasGenericParameters) 
				return self.Name.Split('`')[0];
			return self.Name;
		}

		public static GenericInstanceType MakeGenericType(this TypeReference self, params TypeReference[] arguments)
		{
			if (self.GenericParameters.Count != arguments.Length)
				throw new ArgumentOutOfRangeException(nameof(self));

			GenericInstanceType instance = new(self);

			foreach (var argument in arguments)
				instance.GenericArguments.Add(argument);

			return instance;
		}

		public static MethodReference GetConstructor(this TypeReference typeReference)
		{
			MethodReference methodRef = new(".ctor", typeReference.Module.TypeSystem.Void)
			{
				DeclaringType = typeReference,
				HasThis = true,
				ExplicitThis = false,
				CallingConvention = MethodCallingConvention.Default,
			};
			return methodRef;
		}

		public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
		{
			MethodReference reference = new(self.Name, self.ReturnType)
			{
				DeclaringType = self.DeclaringType.MakeGenericType(arguments),
				HasThis = self.HasThis,
				ExplicitThis = self.ExplicitThis,
				CallingConvention = self.CallingConvention,
			};

			foreach (ParameterDefinition parameter in self.Parameters)
				reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

			foreach (GenericParameter generic_parameter in self.GenericParameters)
				reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

			return reference;
		}
	}
}