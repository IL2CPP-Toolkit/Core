namespace Il2CppToolkit.ReverseCompiler
{
    public interface IStateSpecification
    {
        string Name { get; }
    }

    public interface ISynchronousState : IStateSpecification { }
    public interface ITypedSpecification<T> : IStateSpecification { }
    public interface ITypedSynchronousState<T> : ISynchronousState { }

    public class BuildArtifactSpecification<T> : ITypedSpecification<T>
    {
        public string Name { get; }
        public BuildArtifactSpecification(string name) => Name = name;
    }

    public class SynchronousVariableSpecification<T> : ITypedSpecification<T>, ITypedSynchronousState<T>
    {
        public string Name { get; }
        public SynchronousVariableSpecification(string name) => Name = name;
    }
}