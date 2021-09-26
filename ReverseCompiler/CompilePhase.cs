using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler
{
    public abstract class CompilePhase
    {
        public abstract string Name { get; }
        public virtual Task Prologue(CompileContext context) { return Task.CompletedTask; }
        public abstract Task Execute(CompileContext context);
        public virtual Task Epilogue(CompileContext context) { return Task.CompletedTask; }
    }
}
