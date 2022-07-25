import { spawn } from "child-process-promise";
import path from "path";
import fs from "fs/promises";
import { BuildOptions } from "./options";

export async function push(opts: BuildOptions) {
  const token = await fs.readFile(path.join(__dirname, "../.nuget-token"), {
    encoding: "utf8",
  });

  function pushPattern(pattern: string) {
    return spawn(
      "dotnet.exe",
      [
        "nuget",
        "push",
        path.join(opts.packageDir, pattern),
        "-k",
        token,
        "-s",
        "http://localhost:8090/v3/index.json",
      ],
      {
        stdio: "inherit",
        cwd: opts.basePath,
        shell: true,
      }
    );
  }

  await pushPattern("*.nupkg");
  await pushPattern("*.snupkg");
}
