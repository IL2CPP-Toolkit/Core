using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Il2CppToolkit.Model;
using Vestris.ResourceLib;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public class GenerateAssemblyPhase : CompilePhase
    {
        public override string Name => "Generate Assembly";

        private CompileContext m_context;
        private IReadOnlyDictionary<TypeDescriptor, GeneratedType> m_generatedTypes;
        private readonly Dictionary<string, GeneratedType> m_generatedTypeByFullName = new();
        private readonly Dictionary<string, List<GeneratedType>> m_generatedTypeByClassName = new();
        private ModuleBuilder m_module;
        private string m_outputPath;

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_outputPath = m_context.Artifacts.Get(ArtifactSpecs.OutputPath);
            m_generatedTypes = await m_context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedTypes);
            m_module = await m_context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedModule);

            foreach (var kvp in m_generatedTypes)
            {
                TypeDescriptor descriptor = kvp.Key;
                GeneratedType type = kvp.Value;
                if (!m_generatedTypeByFullName.TryAdd(descriptor.FullName, type))
                    continue;
                if (!m_generatedTypeByClassName.ContainsKey(descriptor.Name))
                {
                    m_generatedTypeByClassName.Add(descriptor.Name, new List<GeneratedType>());
                }
                m_generatedTypeByClassName[descriptor.Name].Add(type);
            }
        }

        public override async Task Execute()
        {
            await m_context.WaitForPhase<BuildTypesPhase>();

            AppDomain currentDomain = Thread.GetDomain();
            ResolveEventHandler resolveHandler = ResolveEvent;
            currentDomain.TypeResolve += resolveHandler;

            try
            {
                string outputFile = m_outputPath;
                if (Path.IsPathRooted(outputFile) && !Directory.Exists(Path.GetDirectoryName(outputFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                if (Path.GetExtension(outputFile) != ".dll")
                {
                    outputFile = Path.Combine(m_outputPath, $"{m_module.Assembly.GetName().Name}.dll");
                }
#if NET472
                if (m_module.Assembly is AssemblyBuilder ab)
                {
                    foreach (var gt in m_generatedTypeByFullName.Values)
                    {
                        ResolveType(gt);
                    }

                    if (File.Exists(Path.GetFileName(outputFile)))
                        File.Delete(Path.GetFileName(outputFile));

                    ab.Save(Path.GetFileName(outputFile));

                    if (File.Exists(outputFile))
                        File.Delete(outputFile);

                    File.Move(Path.GetFileName(outputFile), outputFile);
                }
#else
                Lokad.ILPack.AssemblyGenerator generator = new();
                generator.GenerateAssembly(m_module.Assembly, outputFile);
#endif

                // set file version
                VersionResource vi = new();
                {
                    vi.FileVersion = m_module.Assembly.GetName().Version.ToString();
                    vi.SaveTo(outputFile);
                }
                using (ResourceInfo ri = new())
                {
                    GenericResource stringTableRes = new(new ResourceId("METADATA"), new ResourceId("STRINGTABLE"), ResourceUtil.USENGLISHLANGID);
                    stringTableRes.Data = Guid.NewGuid().ToByteArray();
                    stringTableRes.SaveTo(outputFile);
                }
            }
            finally
            {
                currentDomain.TypeResolve -= resolveHandler;
            }
        }

        private static readonly Stack<Type> s_resolutionStack = new();

        public Assembly ResolveEvent(object _, ResolveEventArgs args)
        {
            if (s_resolutionStack.Count > 0 && m_generatedTypeByFullName.TryGetValue($"{s_resolutionStack.Peek().FullName}+{args.Name}", out GeneratedType type))
            {
                ResolveType(type);
                return type.Type.Assembly;
            }
            else if (m_generatedTypeByFullName.TryGetValue(args.Name, out type))
            {
                ResolveType(type);
                return type.Type.Assembly;
            }
            else
            {
                if (m_generatedTypeByClassName.TryGetValue(args.Name, out List<GeneratedType> types))
                {
                    types.ForEach(ResolveType);
                }
                else
                {
                    CompilerError.LoadTypeError.Raise($"Failed to load type. Type={args.Name}");
                }
            }

            // Complete the type.
            return m_module.Assembly;
        }

        private void ResolveType(GeneratedType type)
        {
            s_resolutionStack.Push(type.Type);
            type.Create();
            s_resolutionStack.Pop();
        }
    }
}
