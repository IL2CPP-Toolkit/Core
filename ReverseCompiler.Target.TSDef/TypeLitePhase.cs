using Il2CppToolkit.ReverseCompiler;
using Il2CppToolkit.ReverseCompiler.Target.NetCore;
using System;
using System.IO;
using System.Reflection.Emit;
using System.Threading.Tasks;
using TypeLitePlus;

namespace Il2CppToolkit.Target.TSDef
{
    class TypeLitePhase : CompilePhase
    {
        public override string Name => "TypeLite";
        private ModuleBuilder m_module;
        private string m_assemblyName;
        private string m_outputPath;

        public override async Task Initialize(CompileContext context)
        {
            m_outputPath = context.Artifacts.Get(ArtifactSpecs.OutputPath);
            m_module = await context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedModule);
            m_assemblyName = context.Artifacts.Get(ArtifactSpecs.AssemblyName);
        }

        public override Task Execute()
        {
            TsModelBuilder moduleBuilder = new();
            foreach (Type type in m_module.Assembly.GetTypes())
            {
                moduleBuilder.Add(type);
            }
            TsGenerator tsg = new TsGenerator();
            TsModel tsModel = moduleBuilder.Build();
            string tsd = tsg.Generate(tsModel);
            string outputFile = m_outputPath;
            if (Path.IsPathRooted(outputFile) && !Directory.Exists(Path.GetDirectoryName(outputFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            }
            if (Path.GetExtension(outputFile) != ".ts.d")
            {
                outputFile = Path.Join(m_outputPath, $"{m_assemblyName}.ts.d");
            }
            File.WriteAllText(outputFile, tsd);
            return base.Execute();
        }
    }
}
