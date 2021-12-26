# Il2Cpp-Toolkit

## Cli Usage

| Parameter              | Short Form | Arguments             | Description                                 |
| ---------------------- | ---------- | --------------------- | ------------------------------------------- |
| `--verbose`            | `-v`       |                       | Set output to verbose messages.             |
| `--name`               | `-n`       | `<name>`              | Output assembly name                        |
| `--assembly-version`   | `-a`       | `<n>[.n[.n]]`         | Output assembly version                     |
| `--game-assembly`      | `-g`       | `<path>`              | Path to GameAssembly.dll                    |
| `--metadata`           | `-m`       | `<path>`              | Path to global-metadata.dat                 |
| `--include`            | `-i`       | `<type>[,..type]`     | List of type names to include in the output |
| `--out-path`           | `-o`       | `<path>`              | Output file path                            |
| `--warnings-as-errors` | `-w`       |                       | Treat warnings as errors                    |
| `--target`             | `-t`       | `<Target>[,..Target]` | List of compile targets.                    |

### Example

The following command will load the game metadata from the provided paths and process `UnityEngine.GameObject`, `Sample.App.GlobalState`, and all (supported) referenced type dependencies. These types will be exported using the `NetCore` target to generate a DLL with assembly name `Sample, 1.1.0` as `Sample.dll` in the current working directory:

```bat
Il2CppToolkit.ReverseCompiler.Cli.exe  --game-assembly "%AppData%\Sample\GameAssembly.dll" --metadata "%AppData%\Sample_Data\metadata\global-metadata.dat" --include UnityEngine.GameObject,Sample.App.GlobalState --target NetCore --name Sample --assembly-version 1.1.0 --out-path .\Sample.dll
```

## API Usage

To dynamically generate an assembly at runtime, use the classes provided in the `Il2CppToolkit.ReverseCompiler` namespace to construct and execute your desired compilation. First, a `Loader` must be constructed and initialized with your GameAssembly and GlobalMetadata paths:

```cs
Loader loader = new();
loader.Init(gasmPath, metadataPath);
```

Next, a type model must be constructed with the loader to provide to the compiler:

```cs
TypeModel model = new(loader);
Compiler compiler = new(model);
```

Lastly, add the desired targets, and assign your configuration values. Each input value supported by the targets is provided in an `ArtifactSpecs` static class (either in the shared library, or in the target's if it is target-specific). These specs can be used to construct and provide a value to the compiler.

```cs
compiler.AddTarget(new NetCoreTarget());
compiler.AddConfiguration(
    ArtifactSpecs.TypeSelectors.MakeValue(new List<Func<TypeDescriptor, bool>>{
        {td => td.Name.StartsWith("UnityEngine.")}, // extract all types in the UnityEngine namespace
        {td => td.Name == "Sample.App.GlobalState"}, // extract Sample.App.GlobalState
    }),
    ArtifactSpecs.AssemblyName.MakeValue(outputAssemblyName),
    ArtifactSpecs.AssemblyVersion.MakeValue(currentInteropVersion),
    ArtifactSpecs.OutputPath.MakeValue(dllPath)
);
```

For a more advanced API example, see [SampleLoader.cs](./Samples/SampleLoader.cs) for a well-structured approach to loading and regenerating assemblies during runtime within the current process, allowing for dynamic updates without shipping a new version of your application/library.

## Building the repo

### Pre-requisites

- [ ] Install the latest .net 5.0 SDK from <https://dotnet.microsoft.com/download/dotnet/5.0>
- [ ] Install VS 2019 16.8.2 or later

## Projects using Il2CppToolkit

- [Raid Toolkit](https://github.com/raid-toolkit/raid-toolkit-sdk) - Background application and framework for extracting useful game data from Raid: Shadow Legends. Supports other 3rd party developer's tools atop a game-specific API framework.
