import { spawn } from "child-process-promise";
import path from "path";
import { BuildOptions } from "./options";

export function build(opts: BuildOptions) {
  const cmdArgs = [
    opts.project,
    "-m:1",
    `-p:Configuration=${opts.flavor}`,
    `-p:Platform=${opts.platform}`,
    "-p:TargetFramework=net5.0",
    `-t:${opts.targets.join(";")}`,
    "-p:IncludeSymbols=true",
    "-p:SymbolPackageFormat=snupkg",
    `-p:PackageOutputPath=${path.format(path.posix.parse(opts.packageDir))}`,
    `-p:PublishDir=${path.format(path.posix.parse(opts.publishDir))}`,
  ];
  return spawn(
    `${process.env.VSInstallDir}/MSBuild/Current/Bin/amd64/MSBuild.exe`,
    cmdArgs,
    { stdio: "inherit", cwd: opts.basePath }
  );
}
