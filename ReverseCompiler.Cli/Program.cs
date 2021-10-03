
using System;
using System.Linq;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using CommandLine;
using System.Collections.Generic;

namespace Il2CppToolkit.ReverseCompiler.Cli
{
    internal class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('n', "name", Required = true, HelpText = "Output assembly name")]
            public string AssemblyName { get; set; }

            [Option('g', "game-assembly", Required = true, HelpText = "Path to GameAssembly.dll")]
            public string GameAssemblyPath { get; set; }

            [Option('m', "metadata", Required = true, HelpText = "Path to global-metadata.dat")]
            public string MetadataPath { get; set; }

            [Option('i', "include", Required = false, Separator = ',', HelpText = "Types to include")]
            public IEnumerable<string> IncludeTypes { get; set; }

            [Option('o', "out-path", Required = true, HelpText = "Output file path")]
            public string OutputPath { get; set; }

            [Option('w', "warnings-as-errors", Required = false, HelpText = "Treat warnings as errors")]
            public bool WarningsAsErrors { get; set; }
        }

        private static int Main(string[] args)
        {
            ErrorHandler.OnError += HandleError;

#if DEBUG
            try
            {
                if (args.Length == 0)
                {
                    args = System.IO.File.ReadAllLines(@".\test.config");
                }
            }
            catch { }
#endif

            int result = 0;
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(opts =>
            {
                if (opts.WarningsAsErrors)
                {
                    ErrorHandler.ErrorThreshhold = ErrorSeverity.Warning;
                }
                Loader loader = new();
                loader.Init(opts.GameAssemblyPath, opts.MetadataPath);
                TypeModel model = new(loader);
                AssemblyGenerator asmGen = new(model);
                asmGen.TypeSelectors.Add(td => opts.IncludeTypes == null || opts.IncludeTypes.Contains(td.Name));
                asmGen.AssemblyName = opts.AssemblyName;
                asmGen.OutputPath = opts.OutputPath;
                try
                {
                    asmGen.GenerateAssembly().Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Build failed: {ex}");
                    result = 1;
                }
            });
            return result;
        }

        private static void HandleError(object sender, ErrorHandler.ErrorEventArgs e)
        {
            Console.WriteLine($"Il2Cpp.ReverseCompiler : {e.Exception}");
        }

    }
}