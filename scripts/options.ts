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
  basePath: string;
}

const basePath = path.resolve(__dirname, "..");

const defaultOptions: BuildOptions = {
  basePath,
  project: path.join(basePath, "Core.sln"),
  flavor: Flavor.Debug,
  platform: Platform.x64,
  targets: ["Build"],
  publishDir: path.join(basePath, "publish/"),
  packageDir: path.join(basePath, "publish/packages"),
};

export function buildOptions(opts?: Partial<BuildOptions>) {
  return { ...defaultOptions, ...opts };
}
