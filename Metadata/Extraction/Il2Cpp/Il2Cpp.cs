using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppToolkit.Model
{
    public abstract class Il2Cpp : BinaryStream
    {
        private Il2CppMetadataRegistration m_metadataRegistration;
        private Il2CppCodeRegistration m_codeRegistration;
        private Il2CppGenericMethodFunctionsDefinitions[] m_genericMethodTable;
        public Il2CppTypeDefinitionSizes[] TypeDefinitionSizes;
        public ulong[] MethodPointers;
        public ulong[] GenericMethodPointers;
        public ulong[] InvokerPointers;
        public ulong[] CustomAttributeGenerators;
        public ulong[] ReversePInvokeWrappers;
        public ulong[] UnresolvedVirtualCallPointers;
        public ulong[] FieldOffsets;
        public Il2CppType[] Types;
        private Dictionary<ulong, Il2CppType> m_typeDic = new Dictionary<ulong, Il2CppType>();
        public ulong[] MetadataUsages;
        public ulong[] GenericInstPointers;
        public Il2CppGenericInst[] GenericInsts;
        public Il2CppMethodSpec[] MethodSpecs;
        public Dictionary<int, List<Il2CppMethodSpec>> MethodDefinitionMethodSpecs = new Dictionary<int, List<Il2CppMethodSpec>>();
        public Dictionary<Il2CppMethodSpec, ulong> MethodSpecGenericMethodPointers = new Dictionary<Il2CppMethodSpec, ulong>();
        public bool FieldOffsetsArePointers;
        protected long m_maxMetadataUsages;
        public Dictionary<string, Il2CppCodeGenModule> CodeGenModules;
        public Dictionary<string, ulong[]> CodeGenModuleMethodPointers;
        public Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>> RGCTXDictionary;

        public abstract ulong MapVATR(ulong addr);
        public abstract ulong MapRTVA(ulong addr);
        public abstract bool Search();
        public abstract bool PlusSearch(int methodCount, int typeDefinitionsCount, int imageCount);
        public abstract bool SymbolSearch();
        public abstract SectionHelper GetSectionHelper(int methodCount, int typeDefinitionsCount, int imageCount);

        protected Il2Cpp(Stream stream) : base(stream) { }

        public void SetProperties(double version, long maxMetadataUsages)
        {
            Version = version;
            this.m_maxMetadataUsages = maxMetadataUsages;
        }

        protected bool AutoPlusInit(ulong codeRegistration, ulong metadataRegistration)
        {
            if (codeRegistration != 0 && metadataRegistration != 0)
            {
                if (Version == 24.2)
                {
                    m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
                    if (m_codeRegistration.reversePInvokeWrapperCount > 0x30000) //TODO
                    {
                        Version = 24.4;
                        codeRegistration -= PointerSize * 3;
                        Console.WriteLine($"Change il2cpp version to: {Version}");
                    }
                    else
                    {
                        m_metadataRegistration = MapVATR<Il2CppMetadataRegistration>(metadataRegistration);
                        m_genericMethodTable = MapVATR<Il2CppGenericMethodFunctionsDefinitions>(m_metadataRegistration.genericMethodTable, m_metadataRegistration.genericMethodTableCount);
                        var genericMethodPointersCount = m_genericMethodTable.Max(x => x.indices.methodIndex) + 1;
                        if (m_codeRegistration.reversePInvokeWrapperCount == genericMethodPointersCount)
                        {
                            Version = 24.3;
                            codeRegistration -= Is32Bit ? 8u : 16u;
                            Console.WriteLine($"Change il2cpp version to: {Version}");
                        }
                    }
                }
                Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
                Console.WriteLine("MetadataRegistration : {0:x}", metadataRegistration);
                Init(codeRegistration, metadataRegistration);
                return true;
            }
            Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
            Console.WriteLine("MetadataRegistration : {0:x}", metadataRegistration);
            return false;
        }

        public virtual void Init(ulong codeRegistration, ulong metadataRegistration)
        {
            m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
            if (Version == 27)
            {
                if (m_codeRegistration.reversePInvokeWrapperCount > 0x30000) //TODO
                {
                    Version = 27.1;
                    codeRegistration -= PointerSize;
                    Console.WriteLine($"Change il2cpp version to: {Version}");
                    Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
                    m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
                }
            }
            if (Version == 24.2)
            {
                if (m_codeRegistration.reversePInvokeWrapperCount > 0x30000) //TODO
                {
                    Version = 24.4;
                    codeRegistration -= PointerSize * 3;
                    Console.WriteLine($"Change il2cpp version to: {Version}");
                    Console.WriteLine("CodeRegistration : {0:x}", codeRegistration);
                    m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
                }
                else
                {
                    if (m_codeRegistration.codeGenModules == 0) //TODO
                    {
                        Version = 24.3;
                        Console.WriteLine($"Change il2cpp version to: {Version}");
                        m_codeRegistration = MapVATR<Il2CppCodeRegistration>(codeRegistration);
                    }
                }
            }
            m_metadataRegistration = MapVATR<Il2CppMetadataRegistration>(metadataRegistration);
            GenericMethodPointers = MapVATR<ulong>(m_codeRegistration.genericMethodPointers, m_codeRegistration.genericMethodPointersCount);
            InvokerPointers = MapVATR<ulong>(m_codeRegistration.invokerPointers, m_codeRegistration.invokerPointersCount);
            if (Version < 27)
            {
                CustomAttributeGenerators = MapVATR<ulong>(m_codeRegistration.customAttributeGenerators, m_codeRegistration.customAttributeCount);
            }
            if (Version > 16 && Version < 27)
            {
                MetadataUsages = MapVATR<ulong>(m_metadataRegistration.metadataUsages, m_maxMetadataUsages);
            }
            if (Version >= 22)
            {
                if (m_codeRegistration.reversePInvokeWrapperCount != 0)
                    ReversePInvokeWrappers = MapVATR<ulong>(m_codeRegistration.reversePInvokeWrappers, m_codeRegistration.reversePInvokeWrapperCount);
                if (m_codeRegistration.unresolvedVirtualCallCount != 0)
                    UnresolvedVirtualCallPointers = MapVATR<ulong>(m_codeRegistration.unresolvedVirtualCallPointers, m_codeRegistration.unresolvedVirtualCallCount);
            }
            GenericInstPointers = MapVATR<ulong>(m_metadataRegistration.genericInsts, m_metadataRegistration.genericInstsCount);
            GenericInsts = Array.ConvertAll(GenericInstPointers, MapVATR<Il2CppGenericInst>);
            FieldOffsetsArePointers = Version > 21;
            if (Version == 21)
            {
                var fieldTest = MapVATR<uint>(m_metadataRegistration.fieldOffsets, 6);
                FieldOffsetsArePointers = fieldTest[0] == 0 && fieldTest[1] == 0 && fieldTest[2] == 0 && fieldTest[3] == 0 && fieldTest[4] == 0 && fieldTest[5] > 0;
            }
            if (FieldOffsetsArePointers)
            {
                FieldOffsets = MapVATR<ulong>(m_metadataRegistration.fieldOffsets, m_metadataRegistration.fieldOffsetsCount);
            }
            else
            {
                FieldOffsets = Array.ConvertAll(MapVATR<uint>(m_metadataRegistration.fieldOffsets, m_metadataRegistration.fieldOffsetsCount), x => (ulong)x);
            }

            ulong[] typeSizePtrs = MapVATR<ulong>(m_metadataRegistration.typeDefinitionsSizes, m_metadataRegistration.typeDefinitionsSizesCount);
            TypeDefinitionSizes = new Il2CppTypeDefinitionSizes[m_metadataRegistration.typeDefinitionsSizesCount];
            for (int i = 0; i < typeSizePtrs.Length; i++)
            {
                // Skip null ptrs
                if (typeSizePtrs[i] == 0)
                    continue;

                Position = MapVATR(typeSizePtrs[i]);
                TypeDefinitionSizes[i] = ReadClass<Il2CppTypeDefinitionSizes>();
            }

            var pTypes = MapVATR<ulong>(m_metadataRegistration.types, m_metadataRegistration.typesCount);
            Types = new Il2CppType[m_metadataRegistration.typesCount];
            for (var i = 0; i < m_metadataRegistration.typesCount; ++i)
            {
                Types[i] = MapVATR<Il2CppType>(pTypes[i]);
                Types[i].Init();
                m_typeDic.Add(pTypes[i], Types[i]);
            }
            if (Version >= 24.2)
            {
                var pCodeGenModules = MapVATR<ulong>(m_codeRegistration.codeGenModules, m_codeRegistration.codeGenModulesCount);
                CodeGenModules = new Dictionary<string, Il2CppCodeGenModule>(pCodeGenModules.Length, StringComparer.Ordinal);
                CodeGenModuleMethodPointers = new Dictionary<string, ulong[]>(pCodeGenModules.Length, StringComparer.Ordinal);
                RGCTXDictionary = new Dictionary<string, Dictionary<uint, Il2CppRGCTXDefinition[]>>(pCodeGenModules.Length, StringComparer.Ordinal);
                foreach (var pCodeGenModule in pCodeGenModules)
                {
                    var codeGenModule = MapVATR<Il2CppCodeGenModule>(pCodeGenModule);
                    var moduleName = ReadStringToNull(MapVATR(codeGenModule.moduleName));
                    CodeGenModules.Add(moduleName, codeGenModule);
                    ulong[] methodPointers;
                    try
                    {
                        methodPointers = MapVATR<ulong>(codeGenModule.methodPointers, codeGenModule.methodPointerCount);
                    }
                    catch
                    {
                        methodPointers = new ulong[codeGenModule.methodPointerCount];
                    }
                    CodeGenModuleMethodPointers.Add(moduleName, methodPointers);

                    var rgctxsDefDictionary = new Dictionary<uint, Il2CppRGCTXDefinition[]>();
                    RGCTXDictionary.Add(moduleName, rgctxsDefDictionary);
                    if (codeGenModule.rgctxsCount > 0)
                    {
                        var rgctxs = MapVATR<Il2CppRGCTXDefinition>(codeGenModule.rgctxs, codeGenModule.rgctxsCount);
                        var rgctxRanges = MapVATR<Il2CppTokenRangePair>(codeGenModule.rgctxRanges, codeGenModule.rgctxRangesCount);
                        foreach (var rgctxRange in rgctxRanges)
                        {
                            var rgctxDefs = new Il2CppRGCTXDefinition[rgctxRange.range.length];
                            Array.Copy(rgctxs, rgctxRange.range.start, rgctxDefs, 0, rgctxRange.range.length);
                            rgctxsDefDictionary.Add(rgctxRange.token, rgctxDefs);
                        }
                    }
                }
            }
            else
            {
                MethodPointers = MapVATR<ulong>(m_codeRegistration.methodPointers, m_codeRegistration.methodPointersCount);
            }
            m_genericMethodTable = MapVATR<Il2CppGenericMethodFunctionsDefinitions>(m_metadataRegistration.genericMethodTable, m_metadataRegistration.genericMethodTableCount);
            MethodSpecs = MapVATR<Il2CppMethodSpec>(m_metadataRegistration.methodSpecs, m_metadataRegistration.methodSpecsCount);
            foreach (var table in m_genericMethodTable)
            {
                var methodSpec = MethodSpecs[table.genericMethodIndex];
                var methodDefinitionIndex = methodSpec.methodDefinitionIndex;
                if (!MethodDefinitionMethodSpecs.TryGetValue(methodDefinitionIndex, out var list))
                {
                    list = new List<Il2CppMethodSpec>();
                    MethodDefinitionMethodSpecs.Add(methodDefinitionIndex, list);
                }
                list.Add(methodSpec);
                MethodSpecGenericMethodPointers.Add(methodSpec, GenericMethodPointers[table.indices.methodIndex]);
            }
        }

        public T MapVATR<T>(ulong addr) where T : new()
        {
            return ReadClass<T>(MapVATR(addr));
        }

        public T[] MapVATR<T>(ulong addr, long count) where T : new()
        {
            return ReadClassArray<T>(MapVATR(addr), count);
        }

        public Il2CppType GetIl2CppType(ulong pointer)
        {
            return m_typeDic[pointer];
        }

        public ulong GetMethodPointer(string imageName, Il2CppMethodDefinition methodDef)
        {
            if (Version >= 24.2)
            {
                var methodToken = methodDef.token;
                var ptrs = CodeGenModuleMethodPointers[imageName];
                var methodPointerIndex = methodToken & 0x00FFFFFFu;
                return ptrs[methodPointerIndex - 1];
            }
            else
            {
                var methodIndex = methodDef.methodIndex;
                if (methodIndex >= 0)
                {
                    return MethodPointers[methodIndex];
                }
            }
            return 0;
        }

        public virtual ulong GetRVA(ulong pointer)
        {
            return pointer;
        }
    }
}
