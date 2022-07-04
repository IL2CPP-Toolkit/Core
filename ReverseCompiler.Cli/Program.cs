
using System;
using System.Linq;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using CommandLine;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Il2CppToolkit.ReverseCompiler.Target;

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

			[Option('a', "assembly-version", Required = false, HelpText = "Output assembly version")]
			public string AssemblyVersion { get; set; } = "1.0";

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

			[Option('t', "target", Required = false, Separator = ',', HelpText = "List of compile targets")]
			public IEnumerable<string> Targets { get; set; }
		}
		class ConsoleLogger : ICompilerLogger
		{
			private bool Verbose;
			public ConsoleLogger(bool verbose)
			{
				Verbose = verbose;
			}
			public void LogInfo(string message)
			{
				if (!Verbose)
					return;
				Console.WriteLine(message);
			}

			public void LogError(string message)
			{
				Console.Error.WriteLine($"ERR: {message}");
				Console.WriteLine(message);
			}

			public void LogMessage(string message)
			{
				Console.WriteLine(message);
			}

			public void LogWarning(string message)
			{
				Console.Error.WriteLine($"WARN: {message}");
			}
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
				Compiler compiler = new(model, new ConsoleLogger(opts.Verbose));
				foreach (string target in opts.Targets)
				{
					Assembly targetAsm = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"Il2CppToolkit.Target.{target}.dll"));
					Type targetType = targetAsm.GetTypes().Single(type => type.IsAssignableTo(typeof(ICompilerTarget)));
					compiler.AddTarget((ICompilerTarget)Activator.CreateInstance(targetType));
				}
				bool allTypes = opts.IncludeTypes.Count() == 0;
				compiler.AddConfiguration(
					ArtifactSpecs.TypeSelectors.MakeValue(new List<Func<TypeDescriptor, bool>>{
						{td => !td.FullName.StartsWith("<")
							&& !td.FullName.StartsWith("System.")
							&& !(td.TypeDef.IsEnum && td.GenericParameterNames?.Length > 0)
							&& (allTypes || opts.IncludeTypes.Contains(td.Name))}
					}),
					ArtifactSpecs.AssemblyName.MakeValue(opts.AssemblyName),
					ArtifactSpecs.AssemblyVersion.MakeValue(Version.Parse(opts.AssemblyVersion)),
					ArtifactSpecs.OutputPath.MakeValue(opts.OutputPath)
				);
				try
				{
					compiler.Compile().Wait();
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