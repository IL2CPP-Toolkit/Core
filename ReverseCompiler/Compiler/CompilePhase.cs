using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public abstract class CompilePhase
    {
        public abstract string Name { get; }
        public abstract Task Initialize(CompileContext context);
        public virtual Task Execute() { return Task.CompletedTask; }
        public virtual Task Finalize() { return Task.CompletedTask; }
    }
}
