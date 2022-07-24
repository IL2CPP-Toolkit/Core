import { spawn } from "child-process-promise";
import path from "path";

export enum Flavor {
  Debug = "Debug",
  Release = "Release",
}

export enum Platform {
  x64 = "x64",
}

export interface BuildOptions {
  project: string;
  flavor: Flavor;
  platform: Platform;
  targets: string[];
  publishDir: string;
  packageDir: string;
}

const basePath = path.resolve(__dirname, "..");

const defaultOptions: BuildOptions = {
  project: path.join(basePath, "Core.sln"),
  flavor: Flavor.Debug,
  platform: Platform.x64,
  targets: ["Build"],
  publishDir: path.join(basePath, "publish/"),
  packageDir: path.join(basePath, "nuget-local/"),
};

export function build(options: Partial<BuildOptions> = {}) {
  const opts = { ...defaultOptions, ...options };
  const cmdArgs = [
    opts.project,
    "-m:1",
    `-p:Configuration=${opts.flavor}`,
    `-p:Platform=${opts.platform}`,
    "-p:TargetFramework=net5.0",
    `-t:${opts.targets.join(";")}`,
    `-p:PackageOutputPath=${path.format(path.posix.parse(opts.packageDir))}`,
    `-p:PublishDir=${path.format(path.posix.parse(opts.publishDir))}`,
  ];
  console.log({ cmdArgs }, cmdArgs.join(" "));
  // throw new Error();
  return spawn(
    `${process.env.VSInstallDir}/MSBuild/Current/Bin/amd64/MSBuild.exe`,
    cmdArgs,
    { stdio: "inherit", cwd: basePath }
  );
}
