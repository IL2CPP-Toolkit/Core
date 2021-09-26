
using System;
using System.Diagnostics;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            ErrorHandler.OnError += HandleError;
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
            asmGen.AssemblyName = args[3];
            asmGen.Execute().Wait();
        }

        private static void HandleError(object sender, ErrorHandler.ErrorEventArgs e)
        {
            Console.WriteLine($"ReverseCompiler : {e.Exception.ToString()}");
        }

    }
}