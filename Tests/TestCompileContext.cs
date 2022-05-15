using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Moq;

namespace Il2CppToolkit.ReverseCompiler
{
	public class TestCompileContext : ICompileContext
	{
		public TestCompileContext()
		{
			ModelMock.Setup(mock => mock.TypeDescriptors).Returns(TypeDescriptors);
			ContextMock.Setup(mock => mock.Model).Returns(ModelMock.Object);
			ContextMock.Setup(mock => mock.Artifacts).Returns(Artifacts);
			AddConfiguration(ArtifactSpecs.TypeSelectors.MakeValue(TypeSelectors));
		}

		public void AddConfiguration(params IStateSpecificationValue[] configuration)
		{
			foreach (IStateSpecificationValue value in configuration)
			{
				Artifacts.Set(value.Specification, value.Value);
			}
		}

		public List<TypeDescriptor> TypeDescriptors { get; } = new();
		public List<Func<TypeDescriptor, bool>> TypeSelectors { get; } = new();
		public ArtifactContainer Artifacts { get; } = new();
		public Mock<ITypeModelMetadata> ModelMock { get; } = new();
		public Mock<ICompileContext> ContextMock { get; } = new();
		ITypeModelMetadata ICompileContext.Model => ModelMock.Object;

		public void AddPhase<T>(T compilePhase) where T : CompilePhase => ContextMock.Object.AddPhase(compilePhase);
		public Task Execute() => ContextMock.Object.Execute();
		public IEnumerable<CompilePhase> GetPhases() => ContextMock.Object.GetPhases();
		public Task WaitForPhase<T>() where T : CompilePhase => ContextMock.Object.WaitForPhase<T>();
	}
}