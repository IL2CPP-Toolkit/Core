using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Xunit;

using Il2CppToolkit.ReverseCompiler.Target.NetCore;
using Il2CppToolkit.ReverseCompiler;
using Moq;

namespace Il2CppToolkit.Test.ReverseCompiler.Target.NetCore
{
	public class SortDependenciesPhaseTest
	{
		[Fact]
		public async Task FilterType()
		{
			TestCompileContext context = new();
			context.TypeDescriptors.Add(new TypeDescriptor("Foo", new(), new()));
			context.TypeDescriptors.Add(new TypeDescriptor("Bar", new(), new()));
			context.TypeSelectors.Add(descriptor => descriptor.Name == "Foo");

			SortDependenciesPhase phase = new();
			await phase.Initialize(context);
			await phase.Execute();
			await phase.Finalize();

			IReadOnlyList<TypeDescriptor> sortedDescriptors = await context.Artifacts.GetAsync(NetCoreArtifactSpecs.SortedTypeDescriptors);
			Assert.Equal(1, sortedDescriptors.Count);
			Assert.Equal("Foo", sortedDescriptors[0].Name);
		}

		[Fact]
		public async Task ParentTypesFirst()
		{
			TestCompileContext context = new();
			TypeDescriptor foo = new("Foo", new(), new());
			TypeDescriptor innerFoo = new("InnerFoo", new(), new()) { DeclaringParent = foo };
			context.TypeDescriptors.Add(foo);
			context.TypeDescriptors.Add(innerFoo);
			context.TypeSelectors.Add(descriptor => descriptor.Name == "InnerFoo");

			SortDependenciesPhase phase = new();
			await phase.Initialize(context);
			await phase.Execute();
			await phase.Finalize();

			IReadOnlyList<TypeDescriptor> sortedDescriptors = await context.Artifacts.GetAsync(NetCoreArtifactSpecs.SortedTypeDescriptors);
			Assert.Equal(2, sortedDescriptors.Count);
			Assert.Equal("Foo", sortedDescriptors[0].Name);
			Assert.Equal("InnerFoo", sortedDescriptors[1].Name);
		}
	}
}
