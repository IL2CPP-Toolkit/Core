
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    args = System.IO.File.ReadAllLines(@".\test.config");
                }
            }
            catch { }
            Loader loader = new Loader();
            loader.Init(args[0], args[1]);
            TypeModel model = new TypeModel(loader);
            AssemblyGenerator asmGen = new AssemblyGenerator(model);
            asmGen.TypeSelectors.Add(td => td.Name == args[2]);
            asmGen.Execute().Wait();
        }
    }
}