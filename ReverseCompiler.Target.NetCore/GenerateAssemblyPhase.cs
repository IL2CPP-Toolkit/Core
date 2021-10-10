using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Il2CppToolkit.Model;

namespace Il2CppToolkit.ReverseCompiler.Target.NetCore
{
    public class GenerateAssemblyPhase : CompilePhase
    {
        public override string Name => "Generate Assembly";

        private CompileContext m_context;
        private IReadOnlyDictionary<TypeDescriptor, Type> m_generatedTypes;
        private readonly Dictionary<string, Type> m_generatedTypeByFullName = new();
        private readonly Dictionary<string, List<Type>> m_generatedTypeByClassName = new();
        private ModuleBuilder m_module;
        private string m_outputPath;

        public override async Task Initialize(CompileContext context)
        {
            m_context = context;
            m_outputPath = m_context.Artifacts.Get(ArtifactSpecs.OutputPath);
            m_generatedTypes = await m_context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedTypes);
            m_module = await m_context.Artifacts.GetAsync(NetCoreArtifactSpecs.GeneratedModule);

            foreach ((TypeDescriptor descriptor, Type type) in m_generatedTypes)
            {
                m_generatedTypeByFullName.Add(descriptor.FullName, type);
                if (!m_generatedTypeByClassName.ContainsKey(descriptor.Name))
                {
                    m_generatedTypeByClassName.Add(descriptor.Name, new List<Type>());
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
                Lokad.ILPack.AssemblyGenerator generator = new();
                string outputFile = m_outputPath;
                if (Path.IsPathRooted(outputFile) && !Directory.Exists(Path.GetDirectoryName(outputFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                if (Path.GetExtension(outputFile) != ".dll")
                {
                    outputFile = Path.Join(m_outputPath, $"{m_module.Assembly.GetName().Name}.dll");
                }
                generator.GenerateAssembly(m_module.Assembly, outputFile);
            }
            finally
            {
                currentDomain.TypeResolve -= resolveHandler;
            }
        }

        private static readonly Stack<Type> s_resolutionStack = new();

        public Assembly ResolveEvent(object _, ResolveEventArgs args)
        {
            if (s_resolutionStack.Count > 0 && m_generatedTypeByFullName.TryGetValue($"{s_resolutionStack.Peek().FullName}+{args.Name}", out Type type))
            {
                ResolveType(type);
                return type.Assembly;
            }
            else if (m_generatedTypeByFullName.TryGetValue(args.Name, out type))
            {
                ResolveType(type);
                return type.Assembly;
            }
            else
            {
                if (m_generatedTypeByClassName.TryGetValue(args.Name, out List<Type> types))
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

        private void ResolveType(Type type)
        {
            s_resolutionStack.Push(type);
            if (type is TypeBuilder tb)
            {
                try
                {
                    tb.CreateType();
                }
                catch (InvalidOperationException)
                {
                    // This is needed to throw away InvalidOperationException.
                    // Loader might send the TypeResolve event more than once
                    // and the type might be complete already.
                }
                catch (Exception ex)
                {
                    CompilerError.ResolveTypeError.Raise($"Failed to resolve type. Exception={ex}");
                }
            }
            s_resolutionStack.Pop();
        }
    }
}
