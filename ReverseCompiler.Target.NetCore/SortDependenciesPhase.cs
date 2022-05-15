using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
	public class SortDependenciesPhase : CompilePhase
	{
		public override string Name => "Sort Dependencies";

		private ICompileContext m_context;
		private IReadOnlyList<Func<TypeDescriptor, bool>> m_typeSelectors;
		private IReadOnlyList<TypeDescriptor> m_sortedDescriptors;

		public override Task Initialize(ICompileContext context)
		{
			m_context = context;
			m_typeSelectors = m_context.Artifacts.Get((ITypedSynchronousState<IReadOnlyList<Func<TypeDescriptor, bool>>>)ArtifactSpecs.TypeSelectors);
			return Task.CompletedTask;
		}

		public override Task Execute()
		{
			m_sortedDescriptors = SortTypes(FilterTypes(m_typeSelectors)).ToList();
			return Task.CompletedTask;
		}

		public override Task Finalize()
		{
			m_context.Artifacts.Set(NetCoreArtifactSpecs.SortedTypeDescriptors, m_sortedDescriptors);
			return Task.CompletedTask;
		}

		private IEnumerable<TypeDescriptor> FilterTypes(IReadOnlyList<Func<TypeDescriptor, bool>> typeSelectors)
		{
			if (typeSelectors == null || typeSelectors.Count == 0)
			{
				return m_context.Model.TypeDescriptors;
			}
			return m_context.Model.TypeDescriptors.Where(descriptor =>
			{
				foreach (Func<TypeDescriptor, bool> selector in typeSelectors)
				{
					if (selector(descriptor))
					{
						return true;
					}
				}
				return false;
			});
		}

		private IEnumerable<TypeDescriptor> SortTypes(IEnumerable<TypeDescriptor> types)
		{
			HashSet<TypeDescriptor> queuedSet = new();
			Queue<TypeDescriptor> reopenList = new(types);
			do
			{
				Queue<TypeDescriptor> openList = reopenList;
				reopenList = new();
				while (openList.TryDequeue(out TypeDescriptor td))
				{
					ErrorHandler.VerifyElseThrow(!queuedSet.Contains(td), CompilerError.InternalError, "Internal error");
					if (td.DeclaringParent != null && !queuedSet.Contains(td.DeclaringParent))
					{
						reopenList.Enqueue(td.DeclaringParent);
						reopenList.Enqueue(td);
						continue;
					}
					if (td.GenericParent != null && !queuedSet.Contains(td.GenericParent))
					{
						reopenList.Enqueue(td.GenericParent);
						reopenList.Enqueue(td);
						continue;
					}
					yield return td;
					queuedSet.Add(td);
				}
			}
			while (reopenList.Count > 0);
		}
	}
}
