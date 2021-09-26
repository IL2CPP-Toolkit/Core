using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Il2CppToolkit.Common.Errors;

namespace Il2CppToolkit.Model
{
    public class Loader
    {
        public LoaderOptions Options { get; set; } = new();

        public Metadata Metadata { get; private set; }
        public Il2Cpp Il2Cpp { get; private set; }

        public string ModuleName { get; private set; }

        public Loader() { }
        public Loader(LoaderOptions options)
        {
            Options = options;
        }

        public void Init(string il2cppPath, string metadataPath)
        {
            ModuleName = Path.GetFileName(il2cppPath);

            Trace.WriteLine("Initializing Metadata...");
            byte[] metadataBytes = File.ReadAllBytes(metadataPath);
            Metadata = new Metadata(new MemoryStream(metadataBytes));
            Trace.WriteLine($"Metadata Version: {Metadata.Version}");

            Trace.WriteLine("Initializing il2cpp file...");
            byte[] il2cppBytes = File.ReadAllBytes(il2cppPath);
            uint il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
            MemoryStream il2CppMemory = new(il2cppBytes);
            switch (il2cppMagic)
            {
                default:
                    throw new NotSupportedException("ERROR: il2cpp file not supported.");
                case 0x6D736100:
                    WebAssembly web = new(il2CppMemory);
                    Il2Cpp = web.CreateMemory();
                    break;
                case 0x304F534E:
                    NSO nso = new(il2CppMemory);
                    Il2Cpp = nso.UnCompress();
                    break;
                case 0x905A4D: //PE
                    Il2Cpp = new PE(il2CppMemory);
                    break;
                case 0x464c457f: //ELF
                    if (il2cppBytes[4] == 2) //ELF64
                    {
                        Il2Cpp = new Elf64(il2CppMemory);
                    }
                    else
                    {
                        Il2Cpp = new Elf(il2CppMemory);
                    }
                    break;
                case 0xCAFEBABE: //FAT Mach-O
                case 0xBEBAFECA:
                    MachoFat machofat = new(new MemoryStream(il2cppBytes));

                    LoaderOptions.ResolveFatPlatformEventArgs eventArgs = new(machofat.fats);
                    Options.FireResolveFatPlatform(this, eventArgs);
                    if (eventArgs.ResolveToIndex == -1)
                    {
                        MetadataError.ConfigurationError.Raise("ResolveFatPlatform was unhandled");
                    }

                    int index = eventArgs.ResolveToIndex;
                    uint magic = machofat.fats[index % 2].magic;
                    il2cppBytes = machofat.GetMacho(index % 2);
                    il2CppMemory = new MemoryStream(il2cppBytes);
                    if (magic == 0xFEEDFACF)
                        goto case 0xFEEDFACF;
                    else
                        goto case 0xFEEDFACE;
                case 0xFEEDFACF: // 64bit Mach-O
                    Il2Cpp = new Macho64(il2CppMemory);
                    break;
                case 0xFEEDFACE: // 32bit Mach-O
                    Il2Cpp = new Macho(il2CppMemory);
                    break;
            }
            double version = Options.ForceVersion ?? Metadata.Version;
            Il2Cpp.SetProperties(version, Metadata.maxMetadataUsages);
            Trace.WriteLine($"Il2Cpp Version: {Il2Cpp.Version}");
            if (Il2Cpp.Version >= 27 && Il2Cpp is ElfBase { IsDumped: true })
            {
                if (!Options.GlobalMetadataDumpAddress.HasValue)
                {
                    MetadataError.ConfigurationError.Raise("global-Metadata.data dump address must be provided");
                }
                Metadata.Address = Options.GlobalMetadataDumpAddress.Value;
            }


            Trace.WriteLine("Searching...");
            bool flag = Il2Cpp.PlusSearch(Metadata.methodDefs.Count(x => x.methodIndex >= 0), Metadata.typeDefs.Length, Metadata.imageDefs.Length);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!flag && Il2Cpp is PE)
                {
                    Trace.WriteLine("Use custom PE loader");
                    Il2Cpp = PELoader.Load(il2cppPath);
                    Il2Cpp.SetProperties(version, Metadata.maxMetadataUsages);
                    flag = Il2Cpp.PlusSearch(Metadata.methodDefs.Count(x => x.methodIndex >= 0), Metadata.typeDefs.Length, Metadata.imageDefs.Length);
                }
            }
            if (!flag)
            {
                flag = Il2Cpp.Search();
            }
            if (!flag)
            {
                flag = Il2Cpp.SymbolSearch();
            }
            if (!flag)
            {
                MetadataError.UnknownFormat.Raise("Can't use auto mode to process file, try manual mode.");
            }
        }
    }
}
