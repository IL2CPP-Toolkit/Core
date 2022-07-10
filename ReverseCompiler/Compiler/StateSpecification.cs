namespace Il2CppToolkit.ReverseCompiler
{
    public interface IStateSpecification
    {
        string Name { get; }
        object DefaultValue { get; }
    }

    public interface IStateSpecificationValue
    {
        IStateSpecification Specification { get; }
        object Value { get; }
    }

    public interface ISynchronousState : IStateSpecification { }
    public interface ITypedSpecification<T> : IStateSpecification { }
    public interface ITypedSynchronousState<T> : ISynchronousState { }

    public class SynchronousVariableSpecificationValue<T> : IStateSpecificationValue
    {
        public IStateSpecification Specification { get; }
        public object Value { get; }
        public object DefaultValue => Specification.DefaultValue;
        public SynchronousVariableSpecificationValue(SynchronousVariableSpecification<T> specification, T value)
        {
            Specification = specification;
            Value = value;
        }
    }

    public class BuildArtifactSpecification<T> : ITypedSpecification<T>
    {
        public string Name { get; }
        public object DefaultValue { get; } = null;
        public BuildArtifactSpecification(string name) => Name = name;
	}

    public class SynchronousVariableSpecification<T> : ITypedSpecification<T>, ITypedSynchronousState<T>
    {
        public string Name { get; }
        public object DefaultValue { get; }
        public SynchronousVariableSpecification(string name) => Name = name;
        public SynchronousVariableSpecification(string name, object defaultValue) : this(name) => DefaultValue = defaultValue;

        public SynchronousVariableSpecificationValue<T> MakeValue(T value)
        {
            return new SynchronousVariableSpecificationValue<T>(this, value);
        }
    }
}