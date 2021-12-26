using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Il2CppToolkit.Common.Errors;
using Il2CppToolkit.Model;
using Il2CppToolkit.ReverseCompiler;
using Il2CppToolkit.ReverseCompiler.Target.NetCore;

namespace Il2CppToolkit
{
    public interface IGameLocator
    {
        string GetGameAssemblyPath();
        string GetGlobalMetadataPath();
        string GetGameVersionTag();
    }

    public class SampleLoader
    {
        private const string OutputAssemblyName = "Sample.Interop";
        private const string OutputFilename = "Sample.Interop.dll";
        private static readonly Version CurrentInteropVersion;
        private static readonly string[] IncludeTypes = new[] {
            "UnityEngine.GameObject"
        };

        // initialize CurrentInteropVersion in static ctor to ensure initialization ordering
        static SampleLoader()
        {
            int hashCode = GetStableHashCode(string.Join(";", IncludeTypes));
            CurrentInteropVersion = new(1, 3, 0, Math.Abs(hashCode % 999));
        }

        private readonly IGameLocator GameLocator;

        public string GameVersion { get; private set; }
        public Version InteropVersion { get; private set; }

        public SampleLoader(IGameLocator locator)
        {
            GameLocator = locator;
        }

        private class CollectibleAssemblyLoadContext : AssemblyLoadContext
        {
            public CollectibleAssemblyLoadContext() : base(isCollectible: true) { }
            protected override Assembly Load(AssemblyName assemblyName) => null;
        }

        internal Assembly Load(bool force = false)
        {
            GameVersion = GameLocator.GetGameVersionTag();

            string executingPath = Process.GetCurrentProcess().MainModule.FileName;
            string dllPath = Path.Join(Path.GetDirectoryName(executingPath), GameVersion, OutputFilename);

            bool shouldGenerate = force;
            try
            {
                if (File.Exists(dllPath))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(dllPath);
                    Version onDiskVersion = new(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
                    if (onDiskVersion != CurrentInteropVersion)
                    {
                        shouldGenerate = true;
                    }
                }
                else
                {
                    shouldGenerate = true;
                }
            }
            catch (Exception)
            {
                shouldGenerate = true;
            }

            if (shouldGenerate)
            {
                GenerateAssembly(dllPath);
            }

            return Assembly.LoadFrom(dllPath);
        }

        private void GenerateAssembly(string dllPath)
        {
            // separated into separate method to ensure we can GC the generated ASM
            BuildAssembly(dllPath);
            GC.Collect();
            var loadedAsm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName == OutputAssemblyName);
            ErrorHandler.Assert(loadedAsm == null, "Expected generated assembly to be unloaded!");
        }

        private void BuildAssembly(string dllPath)
        {
            string metadataPath = GameLocator.GetGlobalMetadataPath();
            string gasmPath = GameLocator.GetGameAssemblyPath();

            //
            // NB: Make sure to update CurrentInteropVersion when changing the codegen arguments!!
            //
            Loader loader = new();
            loader.Init(gasmPath, metadataPath);
            TypeModel model = new(loader);
            Compiler compiler = new(model);
            compiler.AddTarget(new NetCoreTarget());
            compiler.AddConfiguration(
                ArtifactSpecs.TypeSelectors.MakeValue(new List<Func<TypeDescriptor, bool>>{
                    {td => IncludeTypes.Contains(td.Name)},
                }),
                ArtifactSpecs.AssemblyName.MakeValue(OutputAssemblyName),
                ArtifactSpecs.AssemblyVersion.MakeValue(CurrentInteropVersion),
                ArtifactSpecs.OutputPath.MakeValue(dllPath)
            );

            compiler.Compile().Wait();
        }

        private static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

    }
}
